use crate::presentation::PresentationState;
use serde::Serialize;
use std::{collections::BTreeMap, time::Duration};

#[derive(Clone, Debug, Default, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct SlideTiming {
    pub presentation_name: String,
    pub current_slide: i32,
    pub current_seconds: u64,
    pub total_slides: i32,
    pub pages: Vec<PageTiming>,
}
#[derive(Clone, Debug, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct PageTiming {
    pub slide: i32,
    pub seconds: u64,
}

#[derive(Default)]
pub struct SlideTimer {
    path: String,
    name: String,
    current: i32,
    total: i32,
    visit: Duration,
    pages: BTreeMap<i32, Duration>,
    last: Option<Duration>,
    running: bool,
}
impl SlideTimer {
    /// `now` is monotonic. Only actual slideshow pages count, not an idle editor.
    pub fn observe(&mut self, now: Duration, state: &PresentationState) {
        if state.state_unavailable {
            self.last = None;
            return;
        }
        if let Some(last) = self.last.take() {
            let elapsed = now.saturating_sub(last);
            self.visit += elapsed;
            *self.pages.entry(self.current).or_default() += elapsed;
        }
        let active = state.slide_show_running && state.current_slide > 0 && state.total_slides > 0;
        if !active {
            self.running = false;
            self.current = 0;
            self.visit = Duration::ZERO;
            return;
        }
        if !self.running || !self.path.eq_ignore_ascii_case(&state.presentation_path) {
            self.pages.clear();
            self.visit = Duration::ZERO;
            self.current = 0;
            self.path = state.presentation_path.clone();
        }
        if self.current != state.current_slide {
            self.visit = Duration::ZERO;
            self.current = state.current_slide;
        }
        self.name = state.presentation_name.clone();
        self.total = state.total_slides;
        self.pages.entry(self.current).or_default();
        self.running = true;
        self.last = Some(now);
    }
    pub fn snapshot(&self) -> SlideTiming {
        SlideTiming {
            presentation_name: self.name.clone(),
            current_slide: self.current,
            current_seconds: self.visit.as_secs(),
            total_slides: self.total,
            pages: self
                .pages
                .iter()
                .map(|(&slide, time)| PageTiming {
                    slide,
                    seconds: time.as_secs(),
                })
                .collect(),
        }
    }
}
pub fn seconds_label(enabled: bool, seconds: u64) -> String {
    if enabled {
        format!("{seconds:02}")
    } else {
        String::new()
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    fn page(n: i32) -> PresentationState {
        PresentationState {
            slide_show_running: true,
            current_slide: n,
            total_slides: 12,
            presentation_name: "Talk".into(),
            presentation_path: "C:\\Talk.pptx".into(),
            ..Default::default()
        }
    }
    #[test]
    fn slide_changes_reset_visit_but_accumulate_revisits_and_keep_final_totals() {
        let mut timer = SlideTimer::default();
        timer.observe(Duration::ZERO, &page(1));
        timer.observe(Duration::from_secs(4), &page(2));
        assert_eq!(timer.snapshot().current_seconds, 0);
        assert_eq!(timer.snapshot().pages[0].seconds, 4);
        timer.observe(Duration::from_secs(7), &page(1));
        timer.observe(Duration::from_secs(9), &page(1));
        assert_eq!(timer.snapshot().current_seconds, 2);
        assert_eq!(timer.snapshot().pages[0].seconds, 6);
        timer.observe(Duration::from_secs(10), &PresentationState::default());
        assert_eq!(timer.snapshot().current_seconds, 0);
        assert_eq!(timer.snapshot().pages[0].seconds, 7);
        timer.observe(Duration::from_secs(20), &page(1));
        assert_eq!(timer.snapshot().pages[0].seconds, 0);
    }
    #[test]
    fn unknown_samples_do_not_reset_a_round_or_count_unobserved_time() {
        let mut timer = SlideTimer::default();
        timer.observe(Duration::ZERO, &page(1));
        timer.observe(Duration::from_secs(2), &page(1));
        timer.observe(
            Duration::from_secs(3),
            &PresentationState {
                state_unavailable: true,
                ..Default::default()
            },
        );
        timer.observe(Duration::from_secs(8), &page(1));
        assert_eq!(timer.snapshot().current_seconds, 2);
        assert_eq!(seconds_label(true, 0), "00");
        assert_eq!(seconds_label(true, 125), "125");
        assert_eq!(seconds_label(false, 9), "");
    }
}
