from pathlib import Path
import re

p = Path('src/settings.rs')
s = p.read_text(encoding='utf-8')
s = re.sub(r'    pub fn yes_no_with_icon\(.*?\n    \}\n\n', '', s, count=1, flags=re.S)
p.write_text(s, encoding='utf-8', newline='\n')

p = Path('src/FlyPPTTimer/Web/app.js')
s = p.read_text(encoding='utf-8')
s = s.replace("if(generation!==browseGeneration||$('fileBrowser').hidden)return;", "if(generation!==browseGeneration||$('fileBrowser').hidden||!lastState?.fileBrowsingEnabled)return;")
s = s.replace("const host=$('browseEntries');host.replaceChildren();", "const host=$('browseEntries');host.replaceChildren();if(!lastState?.fileBrowsingEnabled)return;")
s = s.replace(".replace('File browsing is disabled.',", ".replace('Computer file browsing is disabled in desktop Remote settings.',")
p.write_text(s, encoding='utf-8', newline='\n')
