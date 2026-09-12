"""Apply the review diff to the tested RC3.3.1 source before Windows validation."""
from pathlib import Path
import base64
import lzma
import subprocess

payload = ''.join(Path(f'.github/rc34-patch-{index}.b64').read_text().strip() for index in range(3))
patch = lzma.decompress(base64.b64decode(payload))
subprocess.run(['git', 'apply', '--whitespace=nowarn', '-'], input=patch, check=True)
print('RC3.4 source changes applied')
