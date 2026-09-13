"""Prepare a disposable documentation renderer; never package this build.

The production Slint layouts and settings callbacks are retained. A bundled
CJK font is selected explicitly only in this temporary renderer because the
hosted headless renderer does not resolve the Windows CJK fallback correctly.
The downloaded font and renderer executable are not committed or distributed.
"""
from pathlib import Path

ui = Path('ui/app-window.slint')
text = ui.read_text(encoding='utf-8')
text = 'import "../doc-font.otf";\n' + text
text = text.replace('inherits Window {', 'inherits Window {\n    default-font-family: "Noto Sans CJK SC";')
text = text.replace('"Microsoft YaHei UI"', '"Noto Sans CJK SC"')
ui.write_text(text, encoding='utf-8', newline='\n')

capture = Path('src/capture.rs')
text = capture.read_text(encoding='utf-8')
text = text.replace('    control.set_next_port("4080".into());',
    '    control.set_qr_image(crate::remote::qr_image("http://192.0.2.10:4080/?token=documentation-example"));\n'
    '    control.set_next_port("4080".into());', 1)
text = text.replace('http://192.168.1.100:4080/?token=\u2022\u2022\u2022\u2022\u2022\u2022',
                    'http://192.0.2.10:4080/?token=documentation-example')
capture.write_text(text, encoding='utf-8', newline='\n')
