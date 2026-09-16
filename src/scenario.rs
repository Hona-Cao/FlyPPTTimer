use crate::config::{AppConfig, ScenarioPreset, ScenarioSnapshot, SCENARIO_BADGE_COLORS};

pub fn make_id(existing: &[ScenarioPreset]) -> String {
    let mut n = 1u32;
    loop {
        let id = format!("scenario-{n}");
        if !existing.iter().any(|item| item.id == id) { return id; }
        n += 1;
    }
}

pub fn add_from_current(config: &mut AppConfig, name: String) -> Result<String, String> {
    if config.scenarios.len() >= 8 { return Err("最多只能保存 8 个情景模式。".into()); }
    let id = make_id(&config.scenarios);
    let color = SCENARIO_BADGE_COLORS[config.scenarios.len()];
    let snapshot = ScenarioSnapshot::capture(config);
    config.scenarios.push(ScenarioPreset {
        id: id.clone(), name, hotkey: String::new(), badge_color: color.into(), snapshot,
    });
    config.active_scenario_id = id.clone();
    Ok(id)
}

pub fn save_current(config: &mut AppConfig, id: &str) -> Result<(), String> {
    let snapshot = ScenarioSnapshot::capture(config);
    let Some(item) = config.scenarios.iter_mut().find(|item| item.id == id) else { return Err("情景模式不存在。".into()); };
    item.snapshot = snapshot;
    config.active_scenario_id = id.to_owned();
    Ok(())
}

pub fn apply(config: &mut AppConfig, id: &str) -> Result<(), String> {
    let Some(item) = config.scenarios.iter().find(|item| item.id == id).cloned() else { return Err("情景模式不存在。".into()); };
    item.snapshot.apply_to(config);
    if config.timer.unlimited { config.timer.mode = crate::config::TimerMode::CountUp; }
    config.active_scenario_id = id.to_owned();
    Ok(())
}

pub fn remove(config: &mut AppConfig, id: &str) -> bool {
    let before = config.scenarios.len();
    config.scenarios.retain(|item| item.id != id);
    if config.active_scenario_id == id { config.active_scenario_id.clear(); }
    before != config.scenarios.len()
}

#[cfg(test)]
mod tests {
    use super::*;
    #[test]
    fn captures_applies_and_limits_presets() {
        let mut c=AppConfig::default(); c.timer.default_duration="00:03:00".into();
        let id=add_from_current(&mut c,"竞赛模式".into()).unwrap();
        c.timer.default_duration="00:20:00".into(); apply(&mut c,&id).unwrap();
        assert_eq!(c.timer.default_duration,"00:03:00");
        for n in 1..8 { add_from_current(&mut c,format!("模式{n}")).unwrap(); }
        assert!(add_from_current(&mut c,"第九个".into()).is_err());
    }
}
