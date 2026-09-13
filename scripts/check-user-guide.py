"""Check current user-guide links and image paths, without running product tests."""
from pathlib import Path
import re
from urllib.parse import unquote, urlsplit

files = [Path('README.md'), Path('README.zh-CN.md'), Path('docs/USER_GUIDE.en.md'), Path('docs/USER_GUIDE.zh-CN.md')]
errors = []
for file in files:
    text = file.read_text(encoding='utf-8')
    links = [a or b for a, b in re.findall(r'\]\(([^)]+)\)|(?:src|href)="([^"]+)"', text)]
    for link in links:
        parsed = urlsplit(link)
        if parsed.scheme or parsed.netloc:
            continue
        target = (file.parent / unquote(parsed.path)) if parsed.path else file
        if not target.exists():
            errors.append(f'{file}: missing {link}')
        elif parsed.fragment and target.suffix == '.md':
            target_text = target.read_text(encoding='utf-8')
            if f'id="{unquote(parsed.fragment)}"' not in target_text:
                errors.append(f'{file}: missing explicit anchor {link}')
        if '-dark-' in parsed.path and parsed.path.endswith('.png'):
            errors.append(f'{file}: mixed screenshot themes: {link}')
    for phrase in ['v0.30.2', 'Clippy', 'RC3.', '已验收', '离屏', 'accepted executable']:
        if phrase in text:
            errors.append(f'{file}: development commentary: {phrase}')
if errors:
    raise SystemExit('\n'.join(errors))
print('Four current user documents: local links, anchors, images and one screenshot theme passed.')
