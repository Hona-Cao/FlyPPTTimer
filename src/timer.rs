use std::time::{Duration, Instant};

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum TimerState {
    Stopped,
    Running,
    Paused,
    Finished,
}

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum TimerMode {
    Countdown,
    CountUp,
}

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub struct TimerSnapshot {
    pub state: TimerState,
    pub mode: TimerMode,
    pub elapsed: Duration,
    pub remaining: Duration,
    pub display: Duration,
    pub duration: Duration,
    pub is_overtime: bool,
}

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub struct TimerUpdate {
    pub snapshot: TimerSnapshot,
    pub just_finished: bool,
}

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub struct InvalidDuration;

impl std::fmt::Display for InvalidDuration {
    fn fmt(&self, formatter: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        formatter.write_str("timer duration must be greater than zero")
    }
}

impl std::error::Error for InvalidDuration {}

pub trait MonotonicClock {
    fn now(&self) -> Duration;
}

#[derive(Debug)]
pub struct SystemClock {
    origin: Instant,
}

impl SystemClock {
    pub fn new() -> Self {
        Self {
            origin: Instant::now(),
        }
    }
}

impl Default for SystemClock {
    fn default() -> Self {
        Self::new()
    }
}

impl MonotonicClock for SystemClock {
    fn now(&self) -> Duration {
        self.origin.elapsed()
    }
}

pub struct Timer<C: MonotonicClock = SystemClock> {
    clock: C,
    state: TimerState,
    mode: TimerMode,
    duration: Duration,
    continue_overtime: bool,
    accumulated: Duration,
    run_started_at: Option<Duration>,
    finish_raised: bool,
}

impl<C: MonotonicClock> Timer<C> {
    pub fn new(
        clock: C,
        duration: Duration,
        mode: TimerMode,
        continue_overtime: bool,
    ) -> Result<Self, InvalidDuration> {
        if duration.is_zero() {
            return Err(InvalidDuration);
        }

        Ok(Self {
            clock,
            state: TimerState::Stopped,
            mode,
            duration,
            continue_overtime,
            accumulated: Duration::ZERO,
            run_started_at: None,
            finish_raised: false,
        })
    }

    pub fn state(&self) -> TimerState {
        self.state
    }

    pub fn duration(&self) -> Duration {
        self.duration
    }

    pub fn mode(&self) -> TimerMode {
        self.mode
    }

    pub fn start(&mut self) {
        self.accumulated = Duration::ZERO;
        self.run_started_at = Some(self.clock.now());
        self.finish_raised = false;
        self.state = TimerState::Running;
    }

    pub fn pause(&mut self) {
        if self.state != TimerState::Running {
            return;
        }

        self.accumulated = self.elapsed();
        self.run_started_at = None;
        self.state = TimerState::Paused;
    }

    pub fn resume(&mut self) {
        if self.state != TimerState::Paused {
            return;
        }

        self.run_started_at = Some(self.clock.now());
        self.state = TimerState::Running;
    }

    pub fn stop(&mut self) {
        if self.state == TimerState::Running {
            self.accumulated = self.elapsed();
        }
        self.run_started_at = None;
        self.state = TimerState::Stopped;
    }

    pub fn reset(&mut self) {
        self.accumulated = Duration::ZERO;
        self.run_started_at = None;
        self.finish_raised = false;
        self.state = TimerState::Stopped;
    }

    pub fn stop_and_reset(&mut self) {
        self.reset();
    }

    pub fn restart(&mut self) {
        self.start();
    }

    pub fn set_duration(&mut self, duration: Duration) -> Result<(), InvalidDuration> {
        if duration.is_zero() {
            return Err(InvalidDuration);
        }

        self.duration = duration;
        self.finish_raised = false;
        Ok(())
    }

    pub fn set_mode(&mut self, mode: TimerMode) {
        self.mode = mode;
        self.finish_raised = false;
    }

    pub fn set_continue_overtime(&mut self, continue_overtime: bool) {
        self.continue_overtime = continue_overtime;
        self.finish_raised = false;
    }

    pub fn update(&mut self) -> TimerUpdate {
        let elapsed = self.elapsed();
        let can_finish = matches!(self.state, TimerState::Running | TimerState::Paused);
        let just_finished = can_finish && !self.finish_raised && elapsed >= self.duration;

        if just_finished {
            self.finish_raised = true;
            if !self.continue_overtime {
                self.accumulated = self.duration;
                self.run_started_at = None;
                self.state = TimerState::Finished;
            }
        }

        TimerUpdate {
            snapshot: self.snapshot(),
            just_finished,
        }
    }

    pub fn snapshot(&self) -> TimerSnapshot {
        let elapsed = self.elapsed();
        let remaining = self.duration.saturating_sub(elapsed);
        let is_overtime = self.continue_overtime && elapsed > self.duration;
        let display = match self.mode {
            TimerMode::Countdown if elapsed <= self.duration => self.duration - elapsed,
            TimerMode::Countdown if is_overtime => elapsed - self.duration,
            TimerMode::Countdown => Duration::ZERO,
            TimerMode::CountUp if !self.continue_overtime && elapsed > self.duration => {
                self.duration
            }
            TimerMode::CountUp => elapsed,
        };

        TimerSnapshot {
            state: self.state,
            mode: self.mode,
            elapsed,
            remaining,
            display,
            duration: self.duration,
            is_overtime,
        }
    }

    fn elapsed(&self) -> Duration {
        if self.state != TimerState::Running {
            return self.accumulated;
        }

        let since_start = self
            .run_started_at
            .map(|started| self.clock.now().saturating_sub(started))
            .unwrap_or_default();
        self.accumulated.saturating_add(since_start)
    }
}

#[cfg(test)]
mod tests {
    use std::{cell::Cell, rc::Rc};

    use super::*;

    #[derive(Clone, Default)]
    struct TestClock(Rc<Cell<Duration>>);

    impl TestClock {
        fn advance(&self, duration: Duration) {
            self.0.set(self.0.get() + duration);
        }
    }

    impl MonotonicClock for TestClock {
        fn now(&self) -> Duration {
            self.0.get()
        }
    }

    fn timer(
        duration: Duration,
        mode: TimerMode,
        continue_overtime: bool,
    ) -> (TestClock, Timer<TestClock>) {
        let clock = TestClock::default();
        let timer = Timer::new(clock.clone(), duration, mode, continue_overtime).unwrap();
        (clock, timer)
    }

    #[test]
    fn countdown_uses_real_elapsed_time() {
        let (clock, mut timer) = timer(Duration::from_secs(10), TimerMode::Countdown, true);
        timer.start();
        clock.advance(Duration::from_millis(3_250));

        let snapshot = timer.snapshot();
        assert_eq!(snapshot.elapsed, Duration::from_millis(3_250));
        assert_eq!(snapshot.remaining, Duration::from_millis(6_750));
        assert_eq!(snapshot.display, Duration::from_millis(6_750));
    }

    #[test]
    fn count_up_uses_real_elapsed_time() {
        let (clock, mut timer) = timer(Duration::from_secs(10), TimerMode::CountUp, true);
        timer.start();
        clock.advance(Duration::from_millis(3_250));

        assert_eq!(timer.snapshot().display, Duration::from_millis(3_250));
    }

    #[test]
    fn pause_and_resume_exclude_paused_time() {
        let (clock, mut timer) = timer(Duration::from_secs(20), TimerMode::Countdown, true);
        timer.start();
        clock.advance(Duration::from_secs(4));
        timer.pause();
        clock.advance(Duration::from_secs(30));
        assert_eq!(timer.snapshot().elapsed, Duration::from_secs(4));

        timer.resume();
        clock.advance(Duration::from_secs(3));
        assert_eq!(timer.snapshot().elapsed, Duration::from_secs(7));
    }

    #[test]
    fn reset_and_stop_reset_return_to_initial_state() {
        let (clock, mut timer) = timer(Duration::from_secs(10), TimerMode::Countdown, true);
        timer.start();
        clock.advance(Duration::from_secs(4));
        timer.reset();
        assert_eq!(timer.state(), TimerState::Stopped);
        assert_eq!(timer.snapshot().elapsed, Duration::ZERO);
        assert_eq!(timer.snapshot().display, Duration::from_secs(10));

        timer.start();
        clock.advance(Duration::from_secs(2));
        timer.stop_and_reset();
        assert_eq!(timer.snapshot().elapsed, Duration::ZERO);
    }

    #[test]
    fn restart_starts_a_fresh_round() {
        let (clock, mut timer) = timer(Duration::from_secs(10), TimerMode::Countdown, true);
        timer.start();
        clock.advance(Duration::from_secs(12));
        assert!(timer.update().just_finished);

        timer.restart();
        assert_eq!(timer.state(), TimerState::Running);
        assert_eq!(timer.snapshot().elapsed, Duration::ZERO);
        clock.advance(Duration::from_secs(1));
        assert_eq!(timer.snapshot().display, Duration::from_secs(9));
    }

    #[test]
    fn overtime_continues_and_finishes_only_once() {
        let (clock, mut timer) = timer(Duration::from_secs(10), TimerMode::Countdown, true);
        timer.start();
        clock.advance(Duration::from_secs(12));

        let first = timer.update();
        assert!(first.just_finished);
        assert!(first.snapshot.is_overtime);
        assert_eq!(first.snapshot.display, Duration::from_secs(2));
        assert_eq!(first.snapshot.state, TimerState::Running);
        assert!(!timer.update().just_finished);
    }

    #[test]
    fn disabled_overtime_stops_at_duration() {
        let (clock, mut timer) = timer(Duration::from_secs(10), TimerMode::Countdown, false);
        timer.start();
        clock.advance(Duration::from_secs(12));

        let update = timer.update();
        assert!(update.just_finished);
        assert_eq!(update.snapshot.state, TimerState::Finished);
        assert_eq!(update.snapshot.elapsed, Duration::from_secs(10));
        assert_eq!(update.snapshot.display, Duration::ZERO);
        assert!(!update.snapshot.is_overtime);
    }

    #[test]
    fn stop_preserves_current_elapsed_time() {
        let (clock, mut timer) = timer(Duration::from_secs(10), TimerMode::CountUp, true);
        timer.start();
        clock.advance(Duration::from_secs(4));
        timer.stop();
        clock.advance(Duration::from_secs(10));

        assert_eq!(timer.state(), TimerState::Stopped);
        assert_eq!(timer.snapshot().elapsed, Duration::from_secs(4));
    }

    #[test]
    fn duration_mode_and_overtime_policy_can_be_changed() {
        let (_, mut timer) = timer(Duration::from_secs(10), TimerMode::Countdown, true);
        timer.set_duration(Duration::from_secs(20)).unwrap();
        timer.set_mode(TimerMode::CountUp);
        timer.set_continue_overtime(false);

        assert_eq!(timer.duration(), Duration::from_secs(20));
        assert_eq!(timer.mode(), TimerMode::CountUp);
        assert_eq!(timer.snapshot().display, Duration::ZERO);
    }
}
