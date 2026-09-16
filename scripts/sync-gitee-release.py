"""Copy a published GitHub release and its two ZIPs to Gitee without rebuilding."""
from __future__ import annotations
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import tempfile
from urllib.parse import quote
import requests


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('tag')
    parser.add_argument('--assets', type=Path)
    parser.add_argument('--preflight', action='store_true')
    args = parser.parse_args()
    if not re.fullmatch(r'v\d+\.\d+\.\d+', args.tag):
        raise RuntimeError('Use a stable version tag such as v1.14.1')
    token = os.environ.get('GITEE_TOKEN', '')
    if not token:
        raise RuntimeError('Set repository Actions secret GITEE_TOKEN with repository/release write access.')
    owner = os.environ.get('GITEE_OWNER', 'hona-cao')
    repo = os.environ.get('GITEE_REPO', 'fly-ppttimer')
    github_repo = os.environ.get('GITHUB_REPOSITORY', 'Hona-Cao/FlyPPTTimer')
    api = f'https://gitee.com/api/v5/repos/{owner}/{repo}'
    session = requests.Session()
    session.headers.update({'Authorization': f'Bearer {token}', 'User-Agent': 'FlyPPTTimer-release-sync'})

    def request(method: str, suffix: str, *, missing_ok: bool = False, **kwargs):
        print(f"Gitee API: {method} {suffix}", flush=True)
        response = session.request(method, api + suffix, timeout=(20, 180), **kwargs)
        if missing_ok and response.status_code == 404:
            return None
        if not response.ok:
            try:
                detail = response.json().get('message', '')
            except (ValueError, AttributeError):
                detail = 'non-JSON API response'
            raise RuntimeError(f'Gitee {method} {suffix}: HTTP {response.status_code}; {str(detail)[:400]}')
        return response.json() if response.content else None

    info = request('GET', '')
    print(f'Gitee credentials can read {info.get("full_name", owner + "/" + repo)}.')
    if args.preflight:
        print('Gitee preflight passed. Writes/uploads will be verified during synchronization.')
        return

    def gh_json(*command: str):
        return json.loads(subprocess.check_output(['gh', *command], text=True))

    release = gh_json('release', 'view', args.tag, '--repo', github_repo,
                      '--json', 'name,body,isDraft,isPrerelease,assets,url')
    if release['isDraft'] or release['isPrerelease']:
        raise RuntimeError('Only published stable releases are synchronized')
    expected = {f'FlyPPTTimer-{args.tag}-{edition}-win-x64.zip' for edition in ('portable', 'setup')}
    if {a['name'] for a in release['assets']} != expected:
        raise RuntimeError('Release must have exactly the portable ZIP and setup ZIP; no checksum attachments')
    assets = args.assets or Path('release-assets')
    assets.mkdir(parents=True, exist_ok=True)
    if args.assets is None:
        subprocess.run(['gh', 'release', 'download', args.tag, '--repo', github_repo,
                        '--dir', str(assets), '--pattern', '*.zip', '--clobber'], check=True)
    if any(not (assets / name).is_file() for name in expected):
        raise RuntimeError('A required release ZIP is missing locally')

    # Git mirroring alone never transfers release descriptions or attachments.
    # Push only the authorized main/tag refs, and never force divergent history.
    subprocess.run(['git', 'fetch', 'origin', f'refs/tags/{args.tag}:refs/tags/{args.tag}'], check=True)
    sha = subprocess.check_output(['git', 'rev-parse', f'{args.tag}^{{commit}}'], text=True).strip()
    with tempfile.TemporaryDirectory() as directory:
        askpass = Path(directory) / 'askpass.py'
        askpass.write_text('#!/usr/bin/env python3\nimport os,sys\nprint(os.environ["GITEE_OWNER"] if "username" in sys.argv[1].lower() else os.environ["GITEE_TOKEN"])\n')
        askpass.chmod(0o700)
        env = dict(os.environ, GIT_ASKPASS=str(askpass), GIT_TERMINAL_PROMPT='0', GITEE_OWNER=owner)
        result = subprocess.run(['git', '-c', 'credential.helper=', 'push',
                                 f'https://gitee.com/{owner}/{repo}.git',
                                 f'{sha}:refs/tags/{args.tag}'], env=env, capture_output=True, text=True)
        if result.returncode:
            tags = request('GET', '/tags', params={'per_page': 100})
            same = next((t for t in tags if t.get('name') == args.tag), None)
            tag_sha = (same or {}).get('commit', {}).get('sha')
            if tag_sha != sha:
                raise RuntimeError('Gitee rejected the tag push and the matching mirrored tag is unavailable: ' + result.stderr[-600:])
        branch = info.get('default_branch') or 'main'
        branch_push = subprocess.run(['git', '-c', 'credential.helper=', 'push',
                                     f'https://gitee.com/{owner}/{repo}.git',
                                     f'{sha}:refs/heads/{branch}'], env=env, capture_output=True, text=True)
        if branch_push.returncode:
            print('Gitee default branch was not fast-forwarded; its configured mirror may update it separately. No force push used.')

    url = f'https://gitee.com/{owner}/{repo}/releases/tag/{args.tag}'
    payload = dict(tag_name=args.tag, name=release['name'], body=release['body'],
                   target_commitish=sha, prerelease=False)
    mirror = request('GET', '/releases/tags/' + quote(args.tag, safe=''), missing_ok=True)
    if mirror and mirror.get('id'):
        mirror = request('PATCH', f'/releases/{mirror["id"]}', json=payload)
    else:
        mirror = request('POST', '/releases', json=payload)
    release_id = mirror['id']
    endpoint = f'/releases/{release_id}/attach_files'
    current = request('GET', endpoint)
    if not isinstance(current, list):
        raise RuntimeError('Unexpected Gitee attachment listing')

    def attachment_digest(item) -> str | None:
        link = item.get('browser_download_url') or item.get('download_url')
        if not link:
            return None
        if link.startswith('/'):
            link = 'https://gitee.com' + link
        # Public bytes are fetched without sending repository credentials to a CDN.
        response = requests.get(link, timeout=(20, 180))
        if not response.ok:
            return None
        return hashlib.sha256(response.content).hexdigest()

    verified = []
    for name in sorted(expected):
        path = assets / name
        digest = hashlib.sha256(path.read_bytes()).hexdigest()
        previous = next((a for a in current if a.get('name') == name), None)
        if previous and attachment_digest(previous) == digest:
            print(f'Already synchronized: {name}')
        else:
            if previous:
                raise RuntimeError(f'Existing Gitee attachment {name} cannot be verified as identical; not deleting it automatically.')
            # curl separates connection timeout from the full multipart transfer.
            # Credentials go through stdin, never command arguments or logs.
            auth = f'header = "Authorization: Bearer {token}"\nform-string = "access_token={token}"\n'
            upload = subprocess.run([
                'curl', '--config', '-', '--fail-with-body', '--silent', '--show-error',
                '--connect-timeout', '30', '--max-time', '900', '--header', 'Expect:',
                '--form-string', f'owner={owner}', '--form-string', f'repo={repo}',
                '--form-string', f'release_id={release_id}',
                '--form', f'file=@{path};type=application/zip', api + endpoint,
            ], input=auth, capture_output=True, text=True)
            if upload.returncode:
                raise RuntimeError(f'Gitee upload failed for {name}: ' + upload.stderr[-600:] + upload.stdout[-400:])
            print(f'Uploaded: {name}')
        verified.append({'name': name, 'bytes': path.stat().st_size})
    final = request('GET', endpoint)
    if {a.get('name') for a in final} != expected:
        raise RuntimeError('Gitee release does not contain exactly the two required ZIPs')
    for item in final:
        actual = attachment_digest(item)
        wanted = hashlib.sha256((assets / item['name']).read_bytes()).hexdigest()
        if actual != wanted:
            raise RuntimeError('Public attachment download is not identical: ' + item['name'])
    result = dict(tag=args.tag, source=sha, url=url, assets=verified,
                  public_downloads_match=True, default_branch_updated=branch_push.returncode == 0)
    evidence = Path('artifacts/publication')
    evidence.mkdir(parents=True, exist_ok=True)
    (evidence/'gitee-release.json').write_text(json.dumps(result, ensure_ascii=False, indent=2)+'\n')
    print(f'Gitee release and both public downloads verified: {url}')


if __name__ == '__main__':
    try:
        main()
    except Exception as error:
        message = str(error)
        token = os.environ.get('GITEE_TOKEN')
        if token:
            message = message.replace(token, '[REDACTED]')
        raise SystemExit(message) from None
