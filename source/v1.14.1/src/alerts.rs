use std::{collections::HashSet, time::Duration};

use crate::{
    config::{AppConfig, PromptSettings},
    timer::{TimerSnapshot, TimerState},
};

#[derive(Debug, Clone, Copy, PartialEq, Eq, Hash)]
pub enum AlertKind {
    Prompt1,
    Prompt2,
    End,
}

#[derive(Debug, Clone)]
pub struct AlertEvent {
    pub prompt: PromptSettings,
    pub speech: String,
}

#[derive(Default)]
pub struct AlertTracker {
    triggered: HashSet<AlertKind>,
}

impl AlertTracker {
    pub fn reset(&mut self) {
        self.triggered.clear();
    }

    pub fn check(&mut self, snapshot: &TimerSnapshot, config: &AppConfig) -> Vec<AlertEvent> {
        let mut events = Vec::new();
        if snapshot.state == TimerState::Running && snapshot.elapsed < snapshot.duration {
            self.try_prompt(
                AlertKind::Prompt1,
                &config.behavior.prompt1,
                snapshot,
                &mut events,
            );
            self.try_prompt(
                AlertKind::Prompt2,
                &config.behavior.prompt2,
                snapshot,
                &mut events,
            );
        }
        events
    }

    pub fn end(&mut self, snapshot: &TimerSnapshot, config: &AppConfig) -> Option<AlertEvent> {
        let prompt = &config.behavior.end_prompt;
        if !prompt.enabled || !self.triggered.insert(AlertKind::End) {
            return None;
        }
        Some(event(prompt, snapshot))
    }

    fn try_prompt(
        &mut self,
        kind: AlertKind,
        prompt: &PromptSettings,
        snapshot: &TimerSnapshot,
        events: &mut Vec<AlertEvent>,
    ) {
        if prompt.enabled
            && snapshot.remaining.as_secs_f64()
                <= f64::from(prompt.trigger_before_end_seconds.max(0))
            && self.triggered.insert(kind)
        {
            events.push(event(prompt, snapshot));
        }
    }
}

fn event(prompt: &PromptSettings, snapshot: &TimerSnapshot) -> AlertEvent {
    AlertEvent {
        prompt: prompt.clone(),
        speech: expand_text(&prompt.text, snapshot),
    }
}

fn expand_text(template: &str, snapshot: &TimerSnapshot) -> String {
    template
        .replace("{time}", &clock_text(snapshot.display))
        .replace("{remaining}", &clock_text(snapshot.remaining))
        .replace("{elapsed}", &clock_text(snapshot.elapsed))
        .replace("{title}", "")
        .replace("{current}", "")
        .replace("{total}", "")
}

fn clock_text(duration: Duration) -> String {
    let seconds = duration.as_secs();
    let hours = seconds / 3_600;
    if hours > 0 {
        format!(
            "{hours:02}:{:02}:{:02}",
            (seconds % 3_600) / 60,
            seconds % 60
        )
    } else {
        format!("{:02}:{:02}", seconds / 60, seconds % 60)
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::timer::TimerMode;

    fn snapshot(elapsed: u64, duration: u64, state: TimerState) -> TimerSnapshot {
        TimerSnapshot {
            state,
            mode: TimerMode::Countdown,
            elapsed: Duration::from_secs(elapsed),
            remaining: Duration::from_secs(duration.saturating_sub(elapsed)),
            display: Duration::from_secs(duration.saturating_sub(elapsed)),
            duration: Duration::from_secs(duration),
            is_overtime: elapsed > duration,
        }
    }

    #[test]
    fn prompts_and_end_trigger_once_per_round() {
        let mut config = AppConfig::default();
        config.behavior.prompt1.enabled = true;
        config.behavior.prompt1.trigger_before_end_seconds = 120;
        config.behavior.prompt2.enabled = true;
        config.behavior.prompt2.trigger_before_end_seconds = 30;
        let mut tracker = AlertTracker::default();

        assert_eq!(
            tracker
                .check(&snapshot(360, 480, TimerState::Running), &config)
                .len(),
            1
        );
        assert!(
            tracker
                .check(&snapshot(361, 480, TimerState::Running), &config)
                .is_empty()
        );
        assert_eq!(
            tracker
                .check(&snapshot(450, 480, TimerState::Running), &config)
                .len(),
            1
        );
        assert!(
            tracker
                .check(&snapshot(451, 480, TimerState::Running), &config)
                .is_empty()
        );
        assert!(
            tracker
                .end(&snapshot(480, 480, TimerState::Finished), &config)
                .is_some()
        );
        assert!(
            tracker
                .end(&snapshot(481, 480, TimerState::Finished), &config)
                .is_none()
        );
    }

    #[test]
    fn reset_allows_a_new_round_to_trigger() {
        let config = AppConfig::default();
        let mut tracker = AlertTracker::default();
        let at_prompt = snapshot(360, 480, TimerState::Running);
        assert_eq!(tracker.check(&at_prompt, &config).len(), 1);
        tracker.reset();
        assert_eq!(tracker.check(&at_prompt, &config).len(), 1);
    }
}
