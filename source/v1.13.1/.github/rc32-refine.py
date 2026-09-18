from pathlib import Path


def replace_once(path: str, old: str, new: str) -> None:
    p = Path(path)
    text = p.read_text(encoding='utf-8')
    if text.count(old) != 1:
        raise RuntimeError(f'Expected one source block in {path}')
    p.write_text(text.replace(old, new, 1), encoding='utf-8', newline='\n')


replace_once('src/remote.rs', '    config::{AppConfig, FileRule},', '    config::AppConfig,')
replace_once('src/remote.rs', 'const INDEX_HTML:', '#[cfg(test)]\nuse crate::config::FileRule;\n\nconst INDEX_HTML:')
replace_once('ui/app-window.slint', 'component TimerReadout inherits Rectangle {', '''component TimerReadout inherits Rectangle {
    // Content measurements are outputs, not constraints fed into the host window.
    // Keep every inferred layout constraint independent of fullscreen font fit.
    min-width: 0px;
    min-height: 0px;
    max-width: 65535px;
    max-height: 65535px;
    horizontal-stretch: 1;
    vertical-stretch: 1;
    preferred-width: 1px;
    preferred-height: 1px;''')
replace_once('ui/app-window.slint', 'export component BigScreenWindow inherits Window {', '''export component BigScreenWindow inherits Window {
    max-width: 65535px;
    max-height: 65535px;''')
replace_once('src/presentation.rs', '                PresentationCommand::CloseLastOpened => return self.close_last_opened_scoped(Some(paths)),', '''                PresentationCommand::CloseActive => return self.close_active_scoped(Some(paths)),
                PresentationCommand::CloseLastOpened => return self.close_last_opened_scoped(Some(paths)),''')
replace_once('src/presentation.rs', '    fn close_active(&mut self) -> Result<String, String> {', '''    fn close_active(&mut self) -> Result<String, String> {
        self.close_active_scoped(None)
    }

    fn close_active_scoped(&mut self, allowed: Option<&[String]>) -> Result<String, String> {''')
replace_once('src/presentation.rs', '''        let path = string(get(&presentation, "FullName")?)?;
        end_show_for(&app, &path)?;
        close_without_saving(&presentation)?;''', '''        let path = string(get(&presentation, "FullName")?)?;
        // Validate the very object that will be closed, not an earlier active
        // window lookup which may change while the user switches Office focus.
        if allowed.is_some_and(|paths| !paths.iter().any(|p| crate::remote::id_for_path(p) == crate::remote::id_for_path(&path))) {
            return Err("当前文稿不在受控列表中，请先重新加入。".into());
        }
        end_show_for(&app, &path)?;
        close_without_saving(&presentation)?;''')
print('RC3.2 native verification corrections applied')
