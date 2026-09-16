"""Mirror a stable GitHub Release and its two ZIP assets to Gitee.

The operation is idempotent: it updates the Gitee release text, reuses identical
attachments, replaces stale ones, and never rebuilds application binaries.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import tempfile
import time
from urllib.parse import quote
import zipfile

import requests


def env_int(name: str, default: int) -> int:
    value = os.environ.get(name, "").strip()
    return int(value) if value else default


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("tag")
    parser.add_argument("--assets", type=Path)
    parser.add_argument("--preflight", action="store_true")
    args = parser.parse_args()

    if not re.fullmatch(r"v\d+\.\d+\.\d+", args.tag):
        raise RuntimeError("Use a stable version tag such as v1.15.0")

    token = os.environ.get("GITEE_TOKEN", "")
    if not token:
        raise RuntimeError("Set Actions secret GITEE_TOKEN with Gitee repository/release write access.")

    owner = os.environ.get("GITEE_OWNER", "hona-cao")
    repo = os.environ.get("GITEE_REPO", "fly-ppttimer")
    github_repo = os.environ.get("GITHUB_REPOSITORY", "Hona-Cao/FlyPPTTimer")
    github_wait_seconds = env_int("GITHUB_ASSET_WAIT_SECONDS", 240)
    github_poll_seconds = env_int("GITHUB_ASSET_POLL_SECONDS", 10)
    upload_attempts = env_int("GITEE_UPLOAD_ATTEMPTS", 2)
    upload_max_seconds = env_int("GITEE_UPLOAD_MAX_SECONDS", 300)
    attachment_poll_seconds = env_int("GITEE_ATTACHMENT_POLL_SECONDS", 60)
    public_verify_attempts = env_int("GITEE_PUBLIC_VERIFY_ATTEMPTS", 2)
    public_verify_max_seconds = env_int("GITEE_PUBLIC_VERIFY_MAX_SECONDS", 90)

    api = f"https://gitee.com/api/v5/repos/{owner}/{repo}"
    session = requests.Session()
    session.headers.update({
        "Authorization": f"Bearer {token}",
        "User-Agent": "FlyPPTTimer-release-sync",
    })

    def request(method: str, suffix: str, *, missing_ok: bool = False, attempts: int = 2, **kwargs):
        last_error: Exception | None = None
        for attempt in range(1, attempts + 1):
            try:
                print(f"Gitee API: {method} {suffix} (attempt {attempt}/{attempts})", flush=True)
                response = session.request(method, api + suffix, timeout=(10, 45), **kwargs)
                if missing_ok and response.status_code == 404:
                    return None
                if response.ok:
                    return response.json() if response.content else None
                try:
                    detail = response.json().get("message", "")
                except (ValueError, AttributeError):
                    detail = "non-JSON API response"
                error = RuntimeError(
                    f"Gitee {method} {suffix}: HTTP {response.status_code}; {str(detail)[:300]}"
                )
                if response.status_code in {400, 401, 403, 404, 409, 422}:
                    raise error
                last_error = error
            except requests.RequestException as error:
                last_error = error
            if attempt < attempts:
                time.sleep(2 * attempt)
        assert last_error is not None
        raise last_error

    if args.preflight:
        request("GET", "/releases", params={"page": 1, "per_page": 1})
        print("Gitee release API preflight passed.")
        return

    def gh_json(*command: str):
        return json.loads(subprocess.check_output(["gh", *command], text=True))

    expected = {
        f"FlyPPTTimer-{args.tag}-{edition}-win-x64.zip"
        for edition in ("portable", "setup")
    }

    deadline = time.monotonic() + github_wait_seconds
    while True:
        release = gh_json(
            "release", "view", args.tag, "--repo", github_repo,
            "--json", "name,body,isDraft,isPrerelease,assets,url",
        )
        if release["isDraft"] or release["isPrerelease"]:
            raise RuntimeError("Only published stable releases are synchronized")
        names = {asset["name"] for asset in release["assets"]}
        unexpected = names - expected
        if unexpected:
            raise RuntimeError("Release contains unexpected uploaded assets: " + ", ".join(sorted(unexpected)))
        if names == expected:
            break
        if time.monotonic() >= deadline:
            raise RuntimeError("GitHub release ZIPs were not ready: " + ", ".join(sorted(expected - names)))
        print("Waiting for GitHub release ZIPs: " + ", ".join(sorted(expected - names)), flush=True)
        time.sleep(github_poll_seconds)

    assets = args.assets or Path("release-assets")
    assets.mkdir(parents=True, exist_ok=True)
    if args.assets is None:
        subprocess.run([
            "gh", "release", "download", args.tag, "--repo", github_repo,
            "--dir", str(assets), "--pattern", "*.zip", "--clobber",
        ], check=True)
    for name in expected:
        path = assets / name
        if not path.is_file() or not zipfile.is_zipfile(path):
            raise RuntimeError(f"Required GitHub release asset is missing or invalid: {name}")

    subprocess.run(["git", "fetch", "origin", f"refs/tags/{args.tag}:refs/tags/{args.tag}"], check=True)
    sha = subprocess.check_output(["git", "rev-parse", f"{args.tag}^{{commit}}"], text=True).strip()
    with tempfile.TemporaryDirectory() as directory:
        askpass = Path(directory) / "askpass.py"
        askpass.write_text(
            '#!/usr/bin/env python3\nimport os,sys\nprint(os.environ["GITEE_OWNER"] if "username" in sys.argv[1].lower() else os.environ["GITEE_TOKEN"])\n'
        )
        askpass.chmod(0o700)
        env = dict(
            os.environ,
            GIT_ASKPASS=str(askpass), GIT_TERMINAL_PROMPT="0", GITEE_OWNER=owner,
            GIT_HTTP_LOW_SPEED_LIMIT="1024", GIT_HTTP_LOW_SPEED_TIME="30",
        )
        pushed = subprocess.run([
            "git", "-c", "credential.helper=", "push",
            f"https://gitee.com/{owner}/{repo}.git", f"{sha}:refs/tags/{args.tag}",
        ], env=env, capture_output=True, text=True, timeout=120)
        if pushed.returncode:
            raise RuntimeError("Gitee tag push failed; refusing to force: " + pushed.stderr[-500:])

    release_url = f"https://gitee.com/{owner}/{repo}/releases/tag/{args.tag}"
    payload = {
        "tag_name": args.tag,
        "name": release["name"],
        "body": release["body"],
        "target_commitish": sha,
        "prerelease": False,
    }
    mirror = request("GET", "/releases/tags/" + quote(args.tag, safe=""), missing_ok=True)
    if mirror and mirror.get("id"):
        mirror = request("PATCH", f'/releases/{mirror["id"]}', json=payload)
    else:
        mirror = request("POST", "/releases", json=payload)
    release_id = mirror["id"]
    endpoint = f"/releases/{release_id}/attach_files"

    def list_attachments():
        value = request("GET", endpoint)
        if not isinstance(value, list):
            raise RuntimeError("Unexpected Gitee attachment listing")
        return value

    def delete_attachment(item) -> None:
        attach_id = item.get("id")
        if not attach_id:
            raise RuntimeError("Cannot replace a Gitee attachment without its id")
        request("DELETE", f"{endpoint}/{attach_id}")

    def attachment_url(item) -> str | None:
        url = item.get("browser_download_url") or item.get("download_url")
        if url and url.startswith("/"):
            url = "https://gitee.com" + url
        return url

    def public_digest(item) -> str | None:
        url = attachment_url(item)
        if not url:
            return None
        for attempt in range(1, public_verify_attempts + 1):
            with tempfile.NamedTemporaryFile(delete=False) as output:
                output_path = Path(output.name)
            try:
                result = subprocess.run([
                    "curl", "--http1.1", "--location", "--fail", "--silent", "--show-error",
                    "--connect-timeout", "15", "--max-time", str(public_verify_max_seconds),
                    "--speed-time", "30", "--speed-limit", "1024",
                    "--output", str(output_path), url,
                ], capture_output=True, text=True)
                if result.returncode == 0:
                    return hashlib.sha256(output_path.read_bytes()).hexdigest()
            finally:
                output_path.unlink(missing_ok=True)
            if attempt < public_verify_attempts:
                time.sleep(5 * attempt)
        return None

    def find_attachment(name: str):
        return next((item for item in list_attachments() if item.get("name") == name), None)

    def wait_for_attachment(name: str, wanted_size: int):
        deadline = time.monotonic() + attachment_poll_seconds
        while True:
            item = find_attachment(name)
            if item:
                remote_size = item.get("size") or item.get("file_size")
                if remote_size is None or int(remote_size) == wanted_size:
                    return item
                return item
            if time.monotonic() >= deadline:
                return None
            time.sleep(5)

    def upload_once(path: Path) -> tuple[bool, str]:
        # Gitee's multipart upload can legitimately spend well over 20 seconds
        # writing a 20-30 MB ZIP from an overseas Actions runner. Use the bounded
        # transfer budget for both socket writes and the eventual response.
        try:
            with path.open("rb") as stream:
                response = session.post(
                    api + endpoint,
                    files={"file": (path.name, stream, "application/zip")},
                    timeout=(upload_max_seconds, upload_max_seconds),
                )
            detail = f"HTTP {response.status_code}"
            if not response.ok:
                detail += "; " + response.text[:300]
            return response.ok, detail
        except requests.RequestException as error:
            return False, str(error)

    def ensure_attachment(path: Path) -> None:
        wanted_digest = hashlib.sha256(path.read_bytes()).hexdigest()
        wanted_size = path.stat().st_size
        existing = find_attachment(path.name)
        if existing:
            remote_size = existing.get("size") or existing.get("file_size")
            if remote_size is not None and int(remote_size) != wanted_size:
                print(f"Replacing size-mismatched Gitee attachment: {path.name}")
                delete_attachment(existing)
            elif public_digest(existing) == wanted_digest:
                print(f"Already synchronized: {path.name}")
                return
            else:
                print(f"Replacing unverifiable/mismatched Gitee attachment: {path.name}")
                delete_attachment(existing)

        last_detail = ""
        for attempt in range(1, upload_attempts + 1):
            print(f"Uploading {path.name} with Gitee multipart API (attempt {attempt}/{upload_attempts})", flush=True)
            upload_ok, last_detail = upload_once(path)
            item = wait_for_attachment(path.name, wanted_size)
            if item:
                remote_size = item.get("size") or item.get("file_size")
                if remote_size is not None and int(remote_size) != wanted_size:
                    delete_attachment(item)
                elif public_digest(item) == wanted_digest:
                    print(f"Uploaded and publicly verified: {path.name}")
                    return
                else:
                    print(f"Attachment appeared but public verification did not match: {path.name}")
                    delete_attachment(item)
            elif upload_ok:
                print(f"Upload returned success but attachment is not listed yet: {path.name}")
            else:
                print(f"Upload ended before confirmation; checking/retrying: {path.name}: {last_detail}")
            if attempt < upload_attempts:
                time.sleep(5 * attempt)
        raise RuntimeError(f"Gitee upload could not be verified for {path.name}: {last_detail}")

    for name in sorted(expected):
        ensure_attachment(assets / name)

    final = list_attachments()
    final_names = {item.get("name") for item in final}
    if final_names != expected:
        raise RuntimeError(
            "Gitee release must contain exactly the two required ZIPs; found: "
            + ", ".join(sorted(name for name in final_names if name))
        )
    for item in final:
        wanted = hashlib.sha256((assets / item["name"]).read_bytes()).hexdigest()
        if public_digest(item) != wanted:
            raise RuntimeError("Public Gitee attachment differs from GitHub asset: " + item["name"])

    evidence = Path("artifacts/publication")
    evidence.mkdir(parents=True, exist_ok=True)
    result = {
        "tag": args.tag,
        "source": sha,
        "url": release_url,
        "github_release": release["url"],
        "assets": [{"name": name, "bytes": (assets / name).stat().st_size} for name in sorted(expected)],
        "public_downloads_match": True,
    }
    (evidence / "gitee-release.json").write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n")
    print(f"Gitee release and both public downloads verified: {release_url}")


if __name__ == "__main__":
    try:
        main()
    except Exception as error:
        message = str(error)
        token = os.environ.get("GITEE_TOKEN")
        if token:
            message = message.replace(token, "[REDACTED]")
        raise SystemExit(message) from None
