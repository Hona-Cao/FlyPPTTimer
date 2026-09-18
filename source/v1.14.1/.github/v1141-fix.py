from pathlib import Path

p = Path('src/app.rs')
s = p.read_text(encoding='utf-8')
old = '''assert!(!config.borrow().remote_control.allow_file_browsing); // Explicit Apply is still required.
        settings.invoke_navigate(2);'''
new = '''assert!(!config.borrow().remote_control.allow_file_browsing); // Explicit Apply is still required.
        settings.invoke_field_edited(browse_row, "".into(), false, 0);
        settings.invoke_navigate(2);'''
if old in s:
    s = s.replace(old, new, 1)
p.write_text(s, encoding='utf-8', newline='\n')

p = Path('ui/app-window.slint')
s = p.read_text(encoding='utf-8')
old = '''background: item.key == "remote.browse" && root.file-access-notice != "" ? DesktopTheme.selected : transparent;
                        border-radius: 6px;'''
new = '''background: item.key == "remote.browse" && root.file-access-notice != "" ? DesktopTheme.selected : (item.kind == 0 ? DesktopTheme.canvas : DesktopTheme.surface);'''
if old in s:
    s = s.replace(old, new, 1)
p.write_text(s, encoding='utf-8', newline='\n')
