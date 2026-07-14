using System.Diagnostics;
using FlyPPTTimer.Core.Models;

namespace FlyPPTTimer.Core.Services;

public sealed class TimerService : IDisposable
{
    private readonly object _sync = new();
    private readonly Stopwatch _stopwatch = new();
    private readonly Timer _pulse;
    private TimeSpan _duration = TimeSpan.FromMinutes(8);
    private TimerMode _mode;
    private bool _continueOvertime = true;
    private bool _finishRaised;
    private TimerState _state;

    public TimerService() => _pulse = new Timer(_ => Tick(), null, Timeout.Infinite, 100);
    public event EventHandler<TimerSnapshot>? Updated;
    public event EventHandler<TimerSnapshot>? Finished;
    public TimerState State { get { lock (_sync) return _state; } }

    public void Configure(AppConfig config)
    {
        lock (_sync)
        {
            _duration = ParseDuration(config.Timer.DefaultDuration);
            _mode = config.Timer.Mode;
            _continueOvertime = config.Timer.ContinueOvertime;
        }
        Publish();
    }

    public void SetDuration(TimeSpan duration) { if (duration <= TimeSpan.Zero) return; lock (_sync) { _duration = duration; _finishRaised = false; } Publish(); }
    public void SetMode(TimerMode mode) { lock (_sync) { _mode = mode; _finishRaised = false; } Publish(); }
    public void Start() { lock (_sync) { _finishRaised = false; _stopwatch.Restart(); _state = TimerState.Running; _pulse.Change(0, 100); } Publish(); }
    public void Pause() { lock (_sync) { if (_state != TimerState.Running) return; _stopwatch.Stop(); _state = TimerState.Paused; } Publish(); }
    public void Resume() { lock (_sync) { if (_state != TimerState.Paused) return; _stopwatch.Start(); _state = TimerState.Running; _pulse.Change(0, 100); } Publish(); }
    public void Stop(bool reset) { lock (_sync) { _stopwatch.Stop(); _pulse.Change(Timeout.Infinite, 100); if (reset) { _stopwatch.Reset(); _finishRaised = false; } _state = TimerState.Stopped; } Publish(); }
    public void Reset() => Stop(true);
    public void StartOrPause() { if (State == TimerState.Running) Pause(); else if (State == TimerState.Paused) Resume(); else Start(); }

    public TimerSnapshot Snapshot()
    {
        lock (_sync)
        {
            var elapsed = _stopwatch.Elapsed;
            var remaining = _mode == TimerMode.Countdown ? _duration - elapsed : TimeSpan.Zero;
            var display = _mode == TimerMode.Countdown ? remaining : elapsed;
            if (_mode == TimerMode.Countdown && remaining < TimeSpan.Zero)
            {
                display = _continueOvertime ? elapsed - _duration : TimeSpan.Zero;
                remaining = TimeSpan.Zero;
            }
            return new(_state, _mode, elapsed, remaining, display, _duration);
        }
    }

    private void Tick()
    {
        TimerSnapshot snapshot;
        var justFinished = false;
        lock (_sync)
        {
            snapshot = Snapshot();
            if (_mode == TimerMode.Countdown && !_finishRaised && snapshot.Remaining <= TimeSpan.Zero)
            {
                _finishRaised = true; _state = TimerState.Finished;
                justFinished = true;
                if (!_continueOvertime) { _stopwatch.Stop(); _pulse.Change(Timeout.Infinite, 100); }
                snapshot = Snapshot();
            }
        }
        Updated?.Invoke(this, snapshot);
        if (justFinished) Finished?.Invoke(this, snapshot);
    }

    private void Publish() => Updated?.Invoke(this, Snapshot());
    public static TimeSpan ParseDuration(string value) => TimeSpan.TryParse(value, out var parsed) && parsed > TimeSpan.Zero ? parsed : TimeSpan.FromMinutes(8);
    public void Dispose() => _pulse.Dispose();
}
