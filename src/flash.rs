use std::time::{Duration, Instant};

use crate::config::PromptSettings;

#[derive(Debug, Clone, Copy, Default)]
pub struct FlashFrame {
    pub text_visible: bool,
    pub background_active: bool,
    pub border_active: bool,
}

pub struct FlashController {
    active: Option<ActiveFlash>,
}

struct ActiveFlash {
    style: String,
    started: Instant,
    until: Option<Instant>,
    on: Duration,
    off: Duration,
    pause_flash: bool,
}

impl FlashController {
    pub fn new() -> Self {
        Self { active: None }
    }

    pub fn start_prompt(&mut self, prompt: &PromptSettings) {
        if prompt.flash_style == "无" || prompt.flash_seconds <= 0 {
            return;
        }
        let now = Instant::now();
        self.active = Some(ActiveFlash {
            style: prompt.flash_style.clone(),
            started: now,
            until: Some(now + Duration::from_secs(prompt.flash_seconds.max(1) as u64)),
            on: Duration::from_millis(prompt.flash_on_ms.max(50) as u64),
            off: Duration::from_millis(prompt.flash_off_ms.max(50) as u64),
            pause_flash: false,
        });
    }

    pub fn ensure_pause(&mut self, style: &str, on_ms: i32, off_ms: i32) {
        if self.active.as_ref().is_some_and(|flash| !flash.pause_flash) {
            return;
        }
        if style == "无" {
            self.active = None;
            return;
        }
        if self.active.is_none() {
            self.active = Some(ActiveFlash {
                style: style.to_owned(),
                started: Instant::now(),
                until: None,
                on: Duration::from_millis(on_ms.max(50) as u64),
                off: Duration::from_millis(off_ms.max(50) as u64),
                pause_flash: true,
            });
        }
    }

    pub fn stop_pause(&mut self) {
        if self.active.as_ref().is_some_and(|flash| flash.pause_flash) {
            self.active = None;
        }
    }

    pub fn frame(&mut self) -> FlashFrame {
        let Some(active) = &self.active else {
            return FlashFrame {
                text_visible: true,
                ..FlashFrame::default()
            };
        };
        let now = Instant::now();
        if active.until.is_some_and(|until| now >= until) {
            self.active = None;
            return FlashFrame {
                text_visible: true,
                ..FlashFrame::default()
            };
        }
        let cycle = active.on + active.off;
        let elapsed = now.saturating_duration_since(active.started);
        let phase = elapsed.as_millis() % cycle.as_millis().max(1);
        let visible = phase < active.on.as_millis();
        let text = active.style.contains("文字");
        let background = active.style.contains("背景") || active.style.contains("边框");
        FlashFrame {
            text_visible: !text || visible,
            background_active: background && visible,
            border_active: active.style.contains("边框"),
        }
    }
}
