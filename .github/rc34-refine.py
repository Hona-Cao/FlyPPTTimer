"""Apply the review diff to the tested RC3.3.1 source before Windows validation."""
from pathlib import Path
import base64
import lzma
import subprocess

payload = ''.join(Path(f'.github/rc34-patch-{index}.b64').read_text().strip() for index in range(3))
patch = lzma.decompress(base64.b64decode(payload))
subprocess.run(['git', 'apply', '--whitespace=nowarn', '-'], input=patch, check=True)

# Reuse the existing translation rather than duplicate its match arm.
p = Path('src/settings.rs')
s = p.read_text(encoding='utf-8').replace('        "跟随系统" => "System",\n', '', 1)
p.write_text(s, encoding='utf-8', newline='\n')

# The capture fixture should start with the same saved port as a real Remote window.
p = Path('src/capture.rs')
s = p.read_text(encoding='utf-8').replace(
    '    let control = crate::app::PresentationWindow::new()?;\n',
    '    let control = crate::app::PresentationWindow::new()?;\n'
    '    control.set_next_port("4080".into());\n'
    '    control.set_saved_port("4080".into());\n'
    '    control.set_saved_text("已保存".into());\n'
    '    control.set_unsaved_text("有未保存的修改".into());\n',
    1,
)
p.write_text(s, encoding='utf-8', newline='\n')
print('RC3.4 source changes applied')
