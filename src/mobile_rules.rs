use crate::{
    config::{AppConfig, FileRule},
    presentation::PresentationState,
    remote::{RemoteCommand, id_for_path},
};

// Only edits configuration. No Office commands or filesystem mutations belong here.
pub fn execute(
    config: &mut AppConfig,
    state: &PresentationState,
    command: &RemoteCommand,
) -> Result<(), String> {
    let id = command
        .presentation_id
        .as_deref()
        .ok_or("请先选择演示文稿。")?;
    let index = config
        .rules
        .iter()
        .position(|rule| id_for_path(&rule.file_path) == id_for_path(id));
    if command.command == "rules.addOpen" {
        let item = state
            .presentations
            .iter()
            .find(|item| id_for_path(&item.path) == id_for_path(id))
            .ok_or("只能添加当前已打开的演示文稿。")?;
        if index.is_none() {
            // Normalize the existing order before appending, including legacy equal keys.
            normalize_order(config);
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
    let index = index.ok_or("该条目不是已保存的文件规则。")?;
    match command.command.as_str() {
        "rules.hide" => config.rules[index].mobile_hidden = true,
        "rules.restore" => config.rules[index].mobile_hidden = false,
        "rules.delete" => {
            if command.confirmed != Some(true) {
                return Err("请确认只删除列表规则，不删除磁盘文件。".into());
            }
            config.rules.remove(index);
        }
        "rules.moveUp" | "rules.moveDown" => {
            normalize_order(config);
            let index = config
                .rules
                .iter()
                .position(|rule| id_for_path(&rule.file_path) == id_for_path(id))
                .unwrap();
            let other = if command.command == "rules.moveUp" {
                index.checked_sub(1)
            } else {
                (index + 1 < config.rules.len()).then_some(index + 1)
            };
            if let Some(other) = other {
                config.rules.swap(index, other);
            }
            for (index, rule) in config.rules.iter_mut().enumerate() {
                rule.mobile_order = index as i32;
            }
        }
        _ => return Err("命令不被允许。".into()),
    }
    Ok(())
}

fn normalize_order(config: &mut AppConfig) {
    config.rules.sort_by_key(|rule| rule.mobile_order);
    for (index, rule) in config.rules.iter_mut().enumerate() {
        rule.mobile_order = index as i32;
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    #[test]
    fn managed_list_changes_never_touch_the_document() {
        let path = std::env::temp_dir().join(format!("fly-list-{}.pptx", std::process::id()));
        std::fs::write(&path, b"disposable document bytes").unwrap();
        let id = path.to_string_lossy().into_owned();
        let mut config = AppConfig {
            rules: vec![
                FileRule {
                    file_path: id.clone(),
                    ..FileRule::default()
                },
                FileRule {
                    file_path: "B.pptx".into(),
                    ..FileRule::default()
                },
            ],
            ..AppConfig::default()
        };
        let state = PresentationState::default();
        let mut command = RemoteCommand {
            presentation_id: Some(id),
            ..RemoteCommand::default()
        };
        for name in ["rules.hide", "rules.moveDown", "rules.restore"] {
            command.command = name.into();
            execute(&mut config, &state, &command).unwrap();
        }
        assert!(!config.rules[1].mobile_hidden);
        assert!(config.rules[1].enabled);
        assert_eq!(config.rules[1].mobile_order, 1);
        command.command = "rules.delete".into();
        assert!(execute(&mut config, &state, &command).is_err());
        assert_eq!(config.rules.len(), 2);
        command.confirmed = Some(true);
        execute(&mut config, &state, &command).unwrap();
        assert_eq!(config.rules.len(), 1);
        assert_eq!(std::fs::read(&path).unwrap(), b"disposable document bytes");
        command.command = "rules.addOpen".into();
        assert!(execute(&mut config, &state, &command).is_err());
        std::fs::remove_file(path).unwrap();
    }
}
