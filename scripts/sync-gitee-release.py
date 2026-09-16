"""Mirror a published stable GitHub Release and its two ZIP assets to Gitee.

The operation is idempotent and self-healing: rerunning the same tag updates release
text and replaces a same-name Gitee attachment only when its public bytes do not
match the corresponding GitHub asset. It never rebuilds application binaries.
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
        raise RuntimeError("Use a stable version tag such as v1.14.1")

    token = os.environ.get("GITEE_TOKEN", "")
    if not token:
        raise RuntimeError(
            "Set repository Actions secret GITEE_TOKEN with repository/release write access."
        )

    owner = os.environ.get("GITEE_OWNER", "hona-cao")
    repo = os.environ.get("GITEE_REPO", "fly-ppttimer")
    github_repo = os.environ.get("GITHUB_REPOSITORY", "Hona-Cao/FlyPPTTimer")
    github_wait_seconds = env_int("GITHUB_ASSET_WAIT_SECONDS", 240)
    github_poll_seconds = env_int("GITHUB_ASSET_POLL_SECONDS", 10)
    public_verify_attempts = env_int("GITEE_PUBLIC_VERIFY_ATTEMPTS", 6)
    upload_attempts = env_int("GITEE_UPLOAD_ATTEMPTS", 3)

    api = f"https://gitee.com/api/v5/repos/{owner}/{repo}"
    session = requests.Session()
    session.headers.update(
        {
            "Authorization": f"Bearer {token}",
            "User-Agent": "FlyPPTTimer-release-sync",
        }
    )

    def request(
        method: str,
        suffix: str,
        *,
        missing_ok: bool = False,
        attempts: int = 3,
        **kwargs,
    ):
        last_error: Exception | None = None
        for attempt in range(1, attempts + 1):
            try:
                print(f"Gitee API: {method} {suffix} (attempt {attempt}/{attempts})", flush=True)
                response = session.request(method, api + suffix, timeout=(20, 180), **kwargs)
                if missing_ok and response.status_code == 404:
                    return None
                if response.ok:
                    return response.json() if response.content else None
                try:
                    detail = response.json().get("message", "")
                except (ValueError, AttributeError):
                    detail = "non-JSON API response"
                last_error = RuntimeError(
                    f"Gitee {method} {suffix}: HTTP {response.status_code}; {str(detail)[:400]}"
                )
                if response.status_code in {400, 401, 403, 404, 409, 422}:
                    raise last_error
            except (requests.RequestException, RuntimeError) as error:
                last_error = error
                if isinstance(error, RuntimeError):
                    message = str(error)
                    if any(f"HTTP {code}" in message for code in (400, 401, 403, 404, 409, 422)):
                        raise
            if attempt < attempts:
                time.sleep(3 * attempt)
        assert last_error is not None
        raise last_error

    info = request("GET", "")
    print(f'Gitee credentials can read {info.get("full_name", owner + "/" + repo)}.')
    if args.preflight:
        print("Gitee preflight passed. Writes/uploads will be verified during synchronization.")
        return

    def gh_json(*command: str):
        return json.loads(subprocess.check_output(["gh", *command], text=True))

    expected = {
        f"FlyPPTTimer-{args.tag}-{edition}-win-x64.zip"
        for edition in ("portable", "setup")
    }

    # The release:published event can arrive while `gh release create ... assets` is
    # still finishing its uploads. Poll instead of racing the two expected ZIPs.
    deadline = time.monotonic() + github_wait_seconds
    while True:
        release = gh_json(
            "release",
            "view",
            args.tag,
            "--repo",
            github_repo,
            "--json",
            "name,body,isDraft,isPrerelease,assets,url",
        )
        if release["isDraft"] or release["isPrerelease"]:
            raise RuntimeError("Only published stable releases are synchronized")
        names = {asset["name"] for asset in release["assets"]}
        unexpected = names - expected
        if unexpected:
            raise RuntimeError(
                "Release contains unexpected uploaded assets: " + ", ".join(sorted(unexpected))
            )
        if names == expected:
            break
        if time.monotonic() >= deadline:
            missing = expected - names
            raise RuntimeError(
                "GitHub release assets were not ready before timeout. Missing: "
                + ", ".join(sorted(missing))
            )
        print(
            "Waiting for GitHub release ZIPs: " + ", ".join(sorted(expected - names)),
            flush=True,
        )
        time.sleep(github_poll_seconds)

    assets = args.assets or Path("release-assets")
    assets.mkdir(parents=True, exist_ok=True)
    if args.assets is None:
        subprocess.run(
            [
                "gh",
                "release",
                "download",
                args.tag,
                "--repo",
                github_repo,
                "--dir",
                str(assets),
                "--pattern",
                "*.zip",
                "--clobber",
            ],
            check=True,
        )

    for name in expected:
        path = assets / name
        if not path.is_file():
            raise RuntimeError(f"Required release ZIP is missing locally: {name}")
        if not zipfile.is_zipfile(path):
            raise RuntimeError(f"Downloaded release asset is not a valid ZIP: {name}")

    # Git mirroring alone does not carry release descriptions or attachments. Push
    # the authorized tag so Gitee can attach a Release to the same commit. Never force.
    subprocess.run(
        ["git", "fetch", "origin", f"refs/tags/{args.tag}:refs/tags/{args.tag}"],
        check=True,
    )
    sha = subprocess.check_output(
        ["git", "rev-parse", f"{args.tag}^{{commit}}"], text=True
    ).strip()

    with tempfile.TemporaryDirectory() as directory:
        askpass = Path(directory) / "askpass.py"
        askpass.write_text(
            '#!/usr/bin/env python3\nimport os,sys\nprint(os.environ["GITEE_OWNER"] if "username" in sys.argv[1].lower() else os.environ["GITEE_TOKEN"])\n'
        )
        askpass.chmod(0o700)
        env = dict(
            os.environ,
            GIT_ASKPASS=str(askpass),
            GIT_TERMINAL_PROMPT="0",
            GITEE_OWNER=owner,
        )
        tag_push = subprocess.run(
            [
                "git",
                "-c",
                "credential.helper=",
                "push",
                f"https://gitee.com/{owner}/{repo}.git",
                f"{sha}:refs/tags/{args.tag}",
            ],
            env=env,
            capture_output=True,
            text=True,
        )
        if tag_push.returncode:
            tags = request("GET", "/tags", params={"per_page": 100})
            same = next((tag for tag in tags if tag.get("name") == args.tag), None)
            tag_sha = (same or {}).get("commit", {}).get("sha")
            if tag_sha != sha:
                raise RuntimeError(
                    "Gitee rejected the tag push and the matching mirrored tag is unavailable: "
                    + tag_push.stderr[-600:]
                )

        branch = info.get("default_branch") or "main"
        branch_push = subprocess.run(
            [
                "git",
                "-c",
                "credential.helper=",
                "push",
                f"https://gitee.com/{owner}/{repo}.git",
                f"{sha}:refs/heads/{branch}",
            ],
            env=env,
            capture_output=True,
            text=True,
        )
        if branch_push.returncode:
            print(
                "Gitee default branch was not fast-forwarded; its configured mirror may update it separately. No force push used."
            )

    release_url = f"https://gitee.com/{owner}/{repo}/releases/tag/{args.tag}"
    payload = {
        "tag_name": args.tag,
        "name": release["name"],
        "body": release["body"],
        "target_commitish": sha,
        "prerelease": False,
    }
    mirror = request(
        "GET", "/releases/tags/" + quote(args.tag, safe=""), missing_ok=True
    )
    if mirror and mirror.get("id"):
        mirror = request("PATCH", f'/releases/{mirror["id"]}', json=payload)
    else:
        mirror = request("POST", "/releases", json=payload)

    release_id = mirror["id"]
    endpoint = f"/releases/{release_id}/attach_files"

    def attachment_link(item) -> str | None:
        link = item.get("browser_download_url") or item.get("download_url")
        if link and link.startswith("/"):
            return "https://gitee.com" + link
        return link

    def attachment_digest(item, attempts: int = public_verify_attempts) -> str | None:
        link = attachment_link(item)
        if not link:
            return None
        for attempt in range(1, attempts + 1):
            try:
                response = requests.get(link, timeout=(20, 180))
                if response.ok:
                    return hashlib.sha256(response.content).hexdigest()
            except requests.RequestException:
                pass
            if attempt < attempts:
                time.sleep(5 * attempt)
        return None

    def list_attachments():
        attachments = request("GET", endpoint)
        if not isinstance(attachments, list):
            raise RuntimeError("Unexpected Gitee attachment listing")
        return attachments

    def delete_attachment(item) -> None:
        attach_id = item.get("id")
        if not attach_id:
            raise RuntimeError("Cannot replace a Gitee attachment without its id")
        request("DELETE", f"{endpoint}/{attach_id}")

    def upload_attachment(path: Path, wanted_digest: str) -> None:
        # curl separates connection timeout from full multipart transfer. Secret
        # values are supplied through stdin and never appear in command arguments.
        for attempt in range(1, upload_attempts + 1):
            auth = (
                f'header = "Authorization: Bearer {token}"\n'
                f'form-string = "access_token={token}"\n'
            )
            upload = subprocess.run(
                [
                    "curl",
                    "--config",
                    "-",
                    "--fail-with-body",
                    "--silent",
                    "--show-error",
                    "--connect-timeout",
                    "30",
                    "--max-time",
                    "900",
                    "--header",
                    "Expect:",
                    "--form-string",
                    f"owner={owner}",
                    "--form-string",
                    f"repo={repo}",
                    "--form-string",
                    f"release_id={release_id}",
                    "--form",
                    f"file=@{path};type=application/zip",
                    api + endpoint,
                ],
                input=auth,
                capture_output=True,
                text=True,
            )
            item = next(
                (entry for entry in list_attachments() if entry.get("name") == path.name),
                None,
            )
            if item and attachment_digest(item) == wanted_digest:
                print(f"Uploaded and verified: {path.name}")
                return
            if item:
                print(f"Removing incomplete/mismatched upload before retry: {path.name}")
                delete_attachment(item)
            if attempt == upload_attempts:
                detail = upload.stderr[-600:] + upload.stdout[-400:]
                raise RuntimeError(f"Gitee upload failed for {path.name}: {detail}")
            time.sleep(5 * attempt)

    verified = []
    for name in sorted(expected):
        path = assets / name
        wanted = hashlib.sha256(path.read_bytes()).hexdigest()
        current = list_attachments()
        previous = next((item for item in current if item.get("name") == name), None)
        if previous and attachment_digest(previous) == wanted:
            print(f"Already synchronized: {name}")
        else:
            if previous:
                print(f"Replacing stale/mismatched Gitee attachment: {name}")
                delete_attachment(previous)
            upload_attachment(path, wanted)
        verified.append({"name": name, "bytes": path.stat().st_size})

    final = list_attachments()
    final_names = {item.get("name") for item in final}
    if final_names != expected:
        raise RuntimeError(
            "Gitee release must contain exactly the two required ZIPs; found: "
            + ", ".join(sorted(name for name in final_names if name))
        )
    for item in final:
        actual = attachment_digest(item)
        wanted = hashlib.sha256((assets / item["name"]).read_bytes()).hexdigest()
        if actual != wanted:
            raise RuntimeError(
                "Public Gitee attachment download is not identical: " + item["name"]
            )

    result = {
        "tag": args.tag,
        "source": sha,
        "url": release_url,
        "github_release": release["url"],
        "assets": verified,
        "public_downloads_match": True,
        "default_branch_updated": branch_push.returncode == 0,
    }
    evidence = Path("artifacts/publication")
    evidence.mkdir(parents=True, exist_ok=True)
    (evidence / "gitee-release.json").write_text(
        json.dumps(result, ensure_ascii=False, indent=2) + "\n"
    )
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
