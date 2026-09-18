use crate::{
    config::{AppConfig, FileRule},
    presentation::PresentationState,
    remote::{RemoteCommand, id_for_path},
};
use std::cmp::Ordering;

// Read-only metadata: sorting/removal never renames, deletes, closes or saves a deck.
pub fn file_metadata(path: &str) -> (Option<u64>, Option<u64>) {
    let Ok(meta) = std::fs::metadata(path) else {
        return (None, None);
    };
    if !meta.is_file() {
        return (None, None);
    }
    let modified = meta
        .modified()
        .ok()
        .and_then(|t| t.duration_since(std::time::UNIX_EPOCH).ok())
        .map(|d| d.as_millis().min(u128::from(u64::MAX)) as u64);
    (Some(meta.len()), modified)
}

// The same Windows logical comparison as the Shell: numbers compare numerically,
// case is ignored, and the OS locale supplies the language collation.
#[link(name = "shlwapi")]
unsafe extern "system" {
    fn StrCmpLogicalW(left: *const u16, right: *const u16) -> i32;
}
fn logical_compare(left: &str, right: &str) -> Ordering {
    let left: Vec<u16> = left.encode_utf16().chain(Some(0)).collect();
    let right: Vec<u16> = right.encode_utf16().chain(Some(0)).collect();
    unsafe { StrCmpLogicalW(left.as_ptr(), right.as_ptr()) }.cmp(&0)
}
fn rule_name(rule: &FileRule) -> String {
    if !rule.file_name.is_empty() {
        return rule.file_name.clone();
    }
    std::path::Path::new(&rule.file_path)
        .file_name()
        .unwrap_or_default()
        .to_string_lossy()
        .into_owned()
}
fn compare_optional(left: Option<u64>, right: Option<u64>, descending: bool) -> Ordering {
    match (left, right) {
        (Some(a), Some(b)) => {
            if descending {
                b.cmp(&a)
            } else {
                a.cmp(&b)
            }
        }
        (Some(_), None) => Ordering::Less,
        (None, Some(_)) => Ordering::Greater,
        _ => Ordering::Equal,
    }
}

pub fn ordered_indices(config: &AppConfig) -> Vec<usize> {
    let mut order: Vec<usize> = (0..config.rules.len()).collect();
    let sort = config.remote_control.list_sort.as_str();
    let descending = config.remote_control.list_sort_descending;
    if !matches!(sort, "name" | "size" | "modified") {
        order.sort_by_key(|&i| config.rules[i].mobile_order);
        return order;
    }
    let names: Vec<_> = config.rules.iter().map(rule_name).collect();
    let metadata: Vec<_> = config
        .rules
        .iter()
        .map(|r| {
            if sort == "name" {
                (None, None)
            } else {
                file_metadata(&r.file_path)
            }
        })
        .collect();
    order.sort_by(|&a, &b| {
        let by_name = logical_compare(&names[a], &names[b]);
        let primary = match sort {
            "size" => compare_optional(metadata[a].0, metadata[b].0, descending),
            "modified" => compare_optional(metadata[a].1, metadata[b].1, descending),
            _ => {
                if descending {
                    by_name.reverse()
                } else {
                    by_name
                }
            }
        };
        primary
            .then(by_name)
            .then_with(|| logical_compare(&config.rules[a].file_path, &config.rules[b].file_path))
            .then(a.cmp(&b))
    });
    order
}
fn write_order(config: &mut AppConfig, order: &[usize]) {
    for (rank, &index) in order.iter().enumerate() {
        config.rules[index].mobile_order = rank as i32;
    }
}
pub fn is_controlled(config: &AppConfig, path: &str) -> bool {
    !path.is_empty()
        && config
            .rules
            .iter()
            .any(|r| id_for_path(&r.file_path) == id_for_path(path))
}

pub fn validate_presentation_command(
    config: &AppConfig,
    state: &PresentationState,
    command: &RemoteCommand,
) -> Result<(), String> {
    let name = command.command.as_str();
    if name == "ppt.refresh" {
        return Ok(());
    }
    if matches!(
        name,
        "ppt.closeActivePresentation" | "ppt.closeCurrentPresentation" | "ppt.forceQuitAll"
    ) && command.confirmed != Some(true)
    {
        return Err("请先确认关闭并放弃未保存修改。".into());
    }
    if name == "ppt.forceQuitAll" {
        if state.state_unavailable
            || state.presentations.is_empty()
            || state
                .presentations
                .iter()
                .any(|p| !is_controlled(config, &p.path))
        {
            return Err("存在未受控或状态未知的文稿，不允许远程退出全部演示软件。".into());
        }
        return Ok(());
    }
    if name == "ppt.closeCurrentPresentation" {
        return if state
            .presentations
            .iter()
            .any(|p| p.managed && is_controlled(config, &p.path))
        {
            Ok(())
        } else {
            Err("当前没有可关闭的受控文稿。".into())
        };
    }
    let target = if matches!(
        name,
        "ppt.openPresentation" | "ppt.startFromBeginning" | "ppt.startFromCurrent"
    ) {
        command
            .presentation_id
            .as_deref()
            .filter(|p| !p.is_empty())
            .unwrap_or(&state.presentation_path)
    } else {
        &state.presentation_path
    };
    if !is_controlled(config, target) {
        return Err("文件不在受控列表中，请先重新加入。".into());
    }
    Ok(())
}

pub fn execute(
    config: &mut AppConfig,
    state: &PresentationState,
    command: &RemoteCommand,
) -> Result<(), String> {
    if command.command == "rules.sort" {
        let sort = command.sort_by.as_deref().ok_or("请选择排序方式。")?;
        if !matches!(sort, "manual" | "name" | "size" | "modified") {
            return Err("排序方式无效。".into());
        }
        let order = ordered_indices(config);
        write_order(config, &order);
        config.remote_control.list_sort = sort.into();
        config.remote_control.list_sort_descending = command.descending.unwrap_or(false);
        let order = ordered_indices(config);
        write_order(config, &order);
        return Ok(());
    }
    let id = command
        .presentation_id
        .as_deref()
        .ok_or("请先选择演示文稿。")?;
    let index = config
        .rules
        .iter()
        .position(|r| id_for_path(&r.file_path) == id_for_path(id));
    if command.command == "rules.addOpen" {
        let item = state
            .presentations
            .iter()
            .find(|p| id_for_path(&p.path) == id_for_path(id))
            .ok_or("只能添加当前已打开的演示文稿。")?;
        if !crate::settings::is_supported_presentation_path(std::path::Path::new(&item.path)) {
            return Err("只能添加 PowerPoint 文件（.ppt、.pptx、.pptm）。".into());
        }
        if index.is_none() {
            let order = ordered_indices(config);
            write_order(config, &order);
            config.rules.push(FileRule {
                file_name: item.name.clone(),
                file_path: item.path.clone(),
                duration: config.timer.default_duration.clone(),
                mode: config.timer.mode,
                mobile_order: config.rules.len() as i32,
                ..FileRule::default()
            });
        }
        return Ok(());
    }
    let index = index.ok_or("文件已移除，请刷新列表。")?;
    match command.command.as_str() {
        "rules.hide" => config.rules[index].mobile_hidden = true,
        "rules.restore" => config.rules[index].mobile_hidden = false,
        "rules.delete" => {
            if command.confirmed != Some(true) {
                return Err("请确认移除文件，不删除磁盘文件。".into());
            }
            config.rules.remove(index);
            let order = ordered_indices(config);
            write_order(config, &order);
        }
        "rules.move" | "rules.moveUp" | "rules.moveDown" => {
            let mut order = ordered_indices(config);
            if let Some(expected) = &command.expected_order {
                let current: Vec<_> = order
                    .iter()
                    .map(|&i| id_for_path(&config.rules[i].file_path))
                    .collect();
                let expected: Vec<_> = expected.iter().map(|p| id_for_path(p)).collect();
                if current != expected {
                    return Err("列表已被其他设备修改，请刷新后重试。".into());
                }
            }
            let rank = order.iter().position(|&i| i == index).unwrap();
            if command.command == "rules.move" {
                if command.expected_order.is_none() {
                    return Err("拖动缺少列表版本，请刷新后重试。".into());
                }
                let before = command.before_presentation_id.as_deref();
                if before.is_some_and(|p| id_for_path(p) == id_for_path(id)) {
                    return Ok(());
                }
                order.remove(rank);
                let destination = if let Some(before) = before {
                    order
                        .iter()
                        .position(|&i| {
                            id_for_path(&config.rules[i].file_path) == id_for_path(before)
                        })
                        .ok_or("目标文件已移除，请刷新后重试。")?
                } else {
                    order.len()
                };
                order.insert(destination, index);
            } else {
                let visible = |&i: &usize| {
                    command.include_hidden == Some(true) || !config.rules[i].mobile_hidden
                };
                let neighbor = if command.command == "rules.moveUp" {
                    (0..rank).rev().find(|&r| visible(&order[r]))
                } else {
                    (rank + 1..order.len()).find(|&r| visible(&order[r]))
                };
                if let Some(other) = neighbor {
                    order.swap(rank, other);
                }
            }
            write_order(config, &order);
            config.remote_control.list_sort = "manual".into();
            config.remote_control.list_sort_descending = false;
        }
        _ => return Err("命令不被允许。".into()),
    }
    Ok(())
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::presentation::OpenPresentation;
    fn config() -> AppConfig {
        AppConfig {
            rules: ["Talk10.pptx", "Talk2.pptx", "Talk1.pptx"]
                .iter()
                .enumerate()
                .map(|(i, p)| FileRule {
                    file_path: (*p).into(),
                    mobile_order: i as i32,
                    ..FileRule::default()
                })
                .collect(),
            ..AppConfig::default()
        }
    }
    fn names(c: &AppConfig) -> Vec<String> {
        ordered_indices(c)
            .iter()
            .map(|&i| c.rules[i].file_path.clone())
            .collect()
    }
    #[test]
    fn mobile_add_open_rejects_non_powerpoint_files() {
        let mut config = AppConfig::default();
        let state = PresentationState {
            presentations: vec![crate::presentation::OpenPresentation {
                name: "handout.pdf".into(),
                path: "handout.pdf".into(),
                ..Default::default()
            }],
            ..Default::default()
        };
        let command = RemoteCommand {
            command: "rules.addOpen".into(),
            presentation_id: Some("handout.pdf".into()),
            ..Default::default()
        };
        assert!(execute(&mut config, &state, &command).is_err());
        assert!(config.rules.is_empty());
    }

    #[test]
    fn logical_name_sort_and_manual_drag_are_persisted() {
        let mut c = config();
        let state = PresentationState::default();
        execute(
            &mut c,
            &state,
            &RemoteCommand {
                command: "rules.sort".into(),
                sort_by: Some("name".into()),
                ..RemoteCommand::default()
            },
        )
        .unwrap();
        assert_eq!(names(&c), ["Talk1.pptx", "Talk2.pptx", "Talk10.pptx"]);
        let expected = names(&c);
        execute(
            &mut c,
            &state,
            &RemoteCommand {
                command: "rules.move".into(),
                presentation_id: Some("Talk10.pptx".into()),
                before_presentation_id: Some("Talk1.pptx".into()),
                expected_order: Some(expected),
                ..RemoteCommand::default()
            },
        )
        .unwrap();
        assert_eq!(names(&c), ["Talk10.pptx", "Talk1.pptx", "Talk2.pptx"]);
        let restored = AppConfig::from_json(&serde_json::to_string(&c).unwrap()).unwrap();
        assert_eq!(names(&restored), names(&c));
        assert_eq!(restored.remote_control.list_sort, "manual");
        let mut ranks: Vec<_> = c.rules.iter().map(|r| r.mobile_order).collect();
        ranks.sort();
        assert_eq!(ranks, [0, 1, 2]);
    }
    #[test]
    fn missing_metadata_is_last_in_both_directions() {
        for desc in [false, true] {
            assert_eq!(compare_optional(None, Some(12), desc), Ordering::Greater);
            assert_eq!(compare_optional(Some(12), None, desc), Ordering::Less);
        }
        assert_eq!(compare_optional(Some(2), Some(10), false), Ordering::Less);
        assert_eq!(compare_optional(Some(2), Some(10), true), Ordering::Greater);
    }
    #[test]
    fn metadata_sort_reads_actual_bytes_and_modified_times() {
        let dir = std::env::temp_dir().join(format!("fly-rc32-sort-{}", std::process::id()));
        std::fs::create_dir_all(&dir).unwrap();
        let a = dir.join("A.pptx");
        let b = dir.join("B.pptx");
        std::fs::write(&a, b"1234567890").unwrap();
        std::fs::write(&b, b"12").unwrap();
        let old = std::time::UNIX_EPOCH + std::time::Duration::from_secs(1_700_000_000);
        let new = old + std::time::Duration::from_secs(60);
        std::fs::File::options()
            .write(true)
            .open(&a)
            .unwrap()
            .set_modified(old)
            .unwrap();
        std::fs::File::options()
            .write(true)
            .open(&b)
            .unwrap()
            .set_modified(new)
            .unwrap();
        let mut c = AppConfig {
            rules: vec![
                FileRule {
                    file_path: a.display().to_string(),
                    ..FileRule::default()
                },
                FileRule {
                    file_path: b.display().to_string(),
                    ..FileRule::default()
                },
            ],
            ..AppConfig::default()
        };
        c.remote_control.list_sort = "size".into();
        assert_eq!(ordered_indices(&c), [1, 0]);
        c.remote_control.list_sort = "modified".into();
        assert_eq!(ordered_indices(&c), [0, 1]);
        c.remote_control.list_sort_descending = true;
        assert_eq!(ordered_indices(&c), [1, 0]);
        assert_eq!(std::fs::read(&a).unwrap(), b"1234567890");
        std::fs::remove_dir_all(dir).unwrap();
    }
    #[test]
    fn stale_drag_rejected_and_hidden_neighbor_skipped() {
        let mut c = config();
        c.rules[1].mobile_hidden = true;
        let state = PresentationState::default();
        let before = serde_json::to_string(&c).unwrap();
        let stale = RemoteCommand {
            command: "rules.move".into(),
            presentation_id: Some("Talk10.pptx".into()),
            expected_order: Some(vec![]),
            ..RemoteCommand::default()
        };
        assert!(execute(&mut c, &state, &stale).is_err());
        assert_eq!(serde_json::to_string(&c).unwrap(), before);
        execute(
            &mut c,
            &state,
            &RemoteCommand {
                command: "rules.moveDown".into(),
                presentation_id: Some("Talk10.pptx".into()),
                ..RemoteCommand::default()
            },
        )
        .unwrap();
        assert_eq!(names(&c), ["Talk1.pptx", "Talk2.pptx", "Talk10.pptx"]);
        assert!(c.rules[1].mobile_hidden);
    }
    #[test]
    fn removal_revokes_control_but_does_not_touch_disk_or_open_document() {
        let path =
            std::env::temp_dir().join(format!("fly-rc32-remove-{}.pptx", std::process::id()));
        std::fs::write(&path, b"unchanged deck").unwrap();
        let id = path.display().to_string();
        let mut c = AppConfig {
            rules: vec![FileRule {
                file_path: id.clone(),
                ..FileRule::default()
            }],
            ..AppConfig::default()
        };
        let state = PresentationState {
            presentation_path: id.clone(),
            has_presentation: true,
            presentations: vec![OpenPresentation {
                path: id.clone(),
                managed: true,
                ..OpenPresentation::default()
            }],
            ..PresentationState::default()
        };
        let command = RemoteCommand {
            command: "rules.delete".into(),
            presentation_id: Some(id.clone()),
            ..RemoteCommand::default()
        };
        assert!(execute(&mut c, &state, &command).is_err());
        assert_eq!(c.rules.len(), 1);
        execute(
            &mut c,
            &state,
            &RemoteCommand {
                confirmed: Some(true),
                ..command
            },
        )
        .unwrap();
        assert!(c.rules.is_empty());
        assert_eq!(state.presentations.len(), 1);
        assert_eq!(std::fs::read(&path).unwrap(), b"unchanged deck");
        for name in [
            "ppt.openPresentation",
            "ppt.closeActivePresentation",
            "ppt.closeCurrentPresentation",
            "ppt.startFromBeginning",
            "ppt.next",
            "ppt.forceQuitAll",
        ] {
            assert!(
                validate_presentation_command(
                    &c,
                    &state,
                    &RemoteCommand {
                        command: name.into(),
                        presentation_id: Some(id.clone()),
                        confirmed: Some(true),
                        ..RemoteCommand::default()
                    }
                )
                .is_err(),
                "{name}"
            );
        }
        execute(
            &mut c,
            &state,
            &RemoteCommand {
                command: "rules.addOpen".into(),
                presentation_id: Some(id.clone()),
                ..RemoteCommand::default()
            },
        )
        .unwrap();
        assert!(is_controlled(&c, &id));
        assert!(
            validate_presentation_command(
                &c,
                &state,
                &RemoteCommand {
                    command: "ppt.closeActivePresentation".into(),
                    ..RemoteCommand::default()
                }
            )
            .is_err()
        );
        std::fs::remove_file(path).unwrap();
    }
}
