from pathlib import Path
import subprocess
# Slint height is already a fixed constraint; min/max cannot accompany it.
ui = Path('ui/app-window.slint')
u = ui.read_text(encoding='utf-8')
constraint = '                    min-height: 54px;\n                    max-height: 54px;\n'
assert u.count(constraint) == 1
ui.write_text(u.replace(constraint, ''), encoding='utf-8', newline='\n')
p = Path('src/capture.rs')
if subprocess.check_output(['git','rev-parse','HEAD:src/capture.rs'], text=True).strip() != '751cac8057082eacf2d44b360aece2a8c505e024':
    raise SystemExit('capture source changed')
s = p.read_text(encoding='utf-8')
s = s.replace('    ComponentHandle, PhysicalSize,', '    ComponentHandle, Model, PhysicalSize,', 1)
needle = '        preview.hide()?;\n        drop(preview);'
assert s.count(needle) == 1
s = s.replace(needle, '''        // Reuse the production info callback, rather than a hand-written mock.
        preview.invoke_navigate(5);
        let rows = preview.get_items();
        for index in 0..rows.row_count() {
            if let Some(row) = rows.row_data(index)
                && (row.label.as_str() == "作者的话" || row.label.as_str() == "From the author")
            {
                preview.invoke_field_action(index as i32);
                slint::platform::update_timers_and_animations();
                HEADLESS_WINDOW.with(|window| window.request_redraw());
                let pixels = preview.window().take_snapshot()?;
                write_png(output.join(format!("author-dialog-{language_name}.png")), &pixels)?;
                preview.set_dialog_open(false);
                break;
            }
        }
        preview.hide()?;
        drop(preview);''', 1)
p.write_text(s, encoding='utf-8', newline='\n')
