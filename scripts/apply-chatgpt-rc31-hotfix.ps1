$ErrorActionPreference = "Stop"

function Replace-Exact {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Old,
        [Parameter(Mandatory = $true)][string]$New
    )

    $text = [IO.File]::ReadAllText($Path)
    $first = $text.IndexOf($Old, [StringComparison]::Ordinal)
    if ($first -lt 0) {
        throw "Expected source block not found in $Path"
    }
    $second = $text.IndexOf($Old, $first + $Old.Length, [StringComparison]::Ordinal)
    if ($second -ge 0) {
        throw "Expected source block is not unique in $Path"
    }
    $updated = $text.Substring(0, $first) + $New + $text.Substring($first + $Old.Length)
    [IO.File]::WriteAllText($Path, $updated, [Text.UTF8Encoding]::new($false))
}

Replace-Exact "src/window.rs" @'
pub fn escape_key_down() -> bool {
    use windows_sys::Win32::UI::Input::KeyboardAndMouse::{GetAsyncKeyState, VK_ESCAPE};
    unsafe { (GetAsyncKeyState(VK_ESCAPE as i32) as u16 & 0x8000) != 0 }
}
'@ @'
pub fn escape_key_down() -> bool {
    use windows_sys::Win32::UI::Input::KeyboardAndMouse::{GetAsyncKeyState, VK_ESCAPE};
    let state = unsafe { GetAsyncKeyState(VK_ESCAPE as i32) as u16 };
    // The high bit reports the current state; the low bit records a press
    // since the previous query. Reading both prevents an ordinary short tap
    // from disappearing between the app's 100 ms refresh samples.
    state & 0x8001 != 0
}
'@

Replace-Exact "src/config.rs" @'
            title_pattern: String::new(),
            feature: String::new(),
        }
    }
}

pub(crate) fn parse_duration(value: &str) -> Option<Duration> {
'@ @'
            title_pattern: String::new(),
            feature: String::new(),
        }
    }
}

pub(crate) fn next_mobile_order(rules: &[FileRule]) -> i32 {
    rules
        .iter()
        .map(|rule| rule.mobile_order)
        .max()
        .unwrap_or(-1)
        .saturating_add(1)
}

pub(crate) fn parse_duration(value: &str) -> Option<Duration> {
'@

Replace-Exact "src/config.rs" @'
        assert!(!c.rules[0].mobile_hidden);
        assert_eq!(c.rules[0].mobile_order, 0);
    }

    #[test]
    fn save_updates_version_and_can_be_loaded_again() {
'@ @'
        assert!(!c.rules[0].mobile_hidden);
        assert_eq!(c.rules[0].mobile_order, 0);
    }

    #[test]
    fn next_mobile_order_appends_after_existing_custom_order() {
        let rules = vec![
            FileRule {
                mobile_order: 4,
                ..FileRule::default()
            },
            FileRule {
                mobile_order: 1,
                ..FileRule::default()
            },
        ];
        assert_eq!(next_mobile_order(&[]), 0);
        assert_eq!(next_mobile_order(&rules), 5);
    }

    #[test]
    fn save_updates_version_and_can_be_loaded_again() {
'@

Replace-Exact "src/app.rs" @'
                    config.rules.push(crate::config::FileRule {
                        file_name: std::path::Path::new(&full)
                            .file_name()
                            .map(|name| name.to_string_lossy().into_owned())
                            .unwrap_or_else(|| full.clone()),
                        file_path: full,
                        duration: default_duration.clone(),
                        mode: default_mode,
                        enabled: true,
                        ..crate::config::FileRule::default()
                    });
'@ @'
                    let mobile_order = crate::config::next_mobile_order(&config.rules);
                    config.rules.push(crate::config::FileRule {
                        mobile_order,
                        file_name: std::path::Path::new(&full)
                            .file_name()
                            .map(|name| name.to_string_lossy().into_owned())
                            .unwrap_or_else(|| full.clone()),
                        file_path: full,
                        duration: default_duration.clone(),
                        mode: default_mode,
                        enabled: true,
                        ..crate::config::FileRule::default()
                    });
'@

Replace-Exact "src/settings.rs" @'
                            let full = path.to_string_lossy().to_string();
                            draft.borrow_mut().rules.push(FileRule {
                                file_name: path
                                    .file_name()
                                    .unwrap_or_default()
                                    .to_string_lossy()
                                    .to_string(),
                                file_path: full,
                                duration: duration.clone(),
                                mode,
                                enabled: true,
                                ..FileRule::default()
                            });
'@ @'
                            let full = path.to_string_lossy().to_string();
                            let mobile_order = crate::config::next_mobile_order(&draft.borrow().rules);
                            draft.borrow_mut().rules.push(FileRule {
                                mobile_order,
                                file_name: path
                                    .file_name()
                                    .unwrap_or_default()
                                    .to_string_lossy()
                                    .to_string(),
                                file_path: full,
                                duration: duration.clone(),
                                mode,
                                enabled: true,
                                ..FileRule::default()
                            });
'@

Replace-Exact "src/presentation.rs" @'
        end_other_shows(&app, &path)?;
        if !matching_show_views(&app, &path, true)?.is_empty() {
            return Ok("目标文稿已在放映".into());
        }
        let presentations = dispatch(get(&app, "Presentations")?)?;
'@ @'
        end_other_shows(&app, &path)?;
        let already_showing = !matching_show_views(&app, &path, true)?.is_empty();
        let presentations = dispatch(get(&app, "Presentations")?)?;
'@

Replace-Exact "src/presentation.rs" @'
        let _ = put(&app, "Visible", VARIANT::from(true));
        Ok(format!(
            "已打开 {}",
            Path::new(&path)
                .file_name()
                .unwrap_or_default()
                .to_string_lossy()
        ))
'@ @'
        let _ = put(&app, "Visible", VARIANT::from(true));
        if already_showing {
            Ok("目标文稿已在放映".into())
        } else {
            Ok(format!(
                "已打开 {}",
                Path::new(&path)
                    .file_name()
                    .unwrap_or_default()
                    .to_string_lossy()
            ))
        }
'@

Replace-Exact "src/presentation.rs" @'
        end_show_for(&app, &path)?;
        if self.managed_paths.contains(&normalize_path(&path)) {
            let _ = put(&presentation, "Saved", VARIANT::from(true));
        }
        call(&presentation, "Close", &[])?;
        self.remove_managed(&path);
'@ @'
        end_show_for(&app, &path)?;
        close_without_saving(&presentation)?;
        self.remove_managed(&path);
'@

Replace-Exact "src/presentation.rs" @'
            if let Some(presentation) = find_presentation(&presentations, &path)? {
                end_show_for(&app, &path)?;
                let _ = put(&presentation, "Saved", VARIANT::from(true));
                call(&presentation, "Close", &[])?;
                self.managed_paths.remove(&path);
'@ @'
            if let Some(presentation) = find_presentation(&presentations, &path)? {
                end_show_for(&app, &path)?;
                close_without_saving(&presentation)?;
                self.managed_paths.remove(&path);
'@

Replace-Exact "src/presentation.rs" @'
fn end_show_for(app: &IDispatch, path: &str) -> Result<(), String> {
    for view in matching_show_views(app, path, true)? {
        call(&view, "Exit", &[])?;
    }
    Ok(())
}

fn get(object: &IDispatch, name: &str) -> Result<VARIANT, String> {
'@ @'
fn end_show_for(app: &IDispatch, path: &str) -> Result<(), String> {
    for view in matching_show_views(app, path, true)? {
        call(&view, "Exit", &[])?;
    }
    Ok(())
}

fn close_without_saving(presentation: &IDispatch) -> Result<(), String> {
    // Remote close is an explicitly confirmed "discard changes" action.
    // Mark Saved before Close so Office cannot block the single STA worker on
    // a save-confirmation dialog. If Close itself fails, restore the dirty bit
    // so an open document is never silently left looking saved.
    let was_saved = int(get(presentation, "Saved")?)? != 0;
    put(presentation, "Saved", VARIANT::from(true))?;
    match call(presentation, "Close", &[]).map(|_| ()) {
        Ok(()) => Ok(()),
        Err(close_error) => {
            if !was_saved
                && let Err(restore_error) = put(presentation, "Saved", VARIANT::from(false))
            {
                return Err(format!(
                    "{close_error}; 关闭失败后无法恢复未保存状态：{restore_error}"
                ));
            }
            Err(close_error)
        }
    }
}

fn get(object: &IDispatch, name: &str) -> Result<VARIANT, String> {
'@

Write-Host "Applied focused RC3.1 hotfix replacements."
