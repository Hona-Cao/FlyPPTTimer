using System.Diagnostics;
using FlyPPTTimer.Models;

namespace FlyPPTTimer.Services;

public sealed class TimerService
{
    private readonly Stopwatch _stopwatch = new();
    private readonly System.Windows.Forms.Timer _uiTimer = new() { Interval = 100 };
    private TimeSpan _duration;
    private TimerMode _mode;
    private bool _continueOvertime;

    public TimerService(LogService log)
    {
        Log = log;
        _uiTimer.Tick += (_, _) => Tick();
    }

    public event EventHandler<TimerSnapshot>? Updated;
    public event EventHandler<TimerSnapshot>? Finished;
    public LogService Log { get; }
    public TimerState State { get; private set; } = TimerState.Stopped;
    public bool FinishRaised { get; private set; }
    public TimeSpan Duration => _duration;
    public TimerMode Mode => _mode;

    public void Configure(AppConfig config)
    {
        _mode = config.Timer.Mode;
        _duration = ParseDuration(config.Timer.DefaultDuration);
        _continueOvertime = config.Timer.ContinueOvertime;
        if (State == TimerState.Stopped)
        {
            RaiseUpdate();
        }
    }

    public void SetDuration(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero) return;
        _duration = duration;
        FinishRaised = false;
        Log.Info($"Timer duration set: {duration}");
        RaiseUpdate();
    }

    public void SetMode(TimerMode mode)
    {
        _mode = mode;
        FinishRaised = false;
        Log.Info($"Timer mode set: {mode}");
        RaiseUpdate();
    }

    public void Start()
    {
        FinishRaised = false;
        _stopwatch.Restart();
        _uiTimer.Start();
        State = TimerState.Running;
        Log.Info("Timer started.");
        RaiseUpdate();
    }

    public void Pause()
    {
        if (State != TimerState.Running) return;
        _stopwatch.Stop();
        State = TimerState.Paused;
        Log.Info("Timer paused.");
        RaiseUpdate();
    }

    public void Resume()
    {
        if (State != TimerState.Paused) return;
        _stopwatch.Start();
        State = TimerState.Running;
        Log.Info("Timer resumed.");
        RaiseUpdate();
    }

    public void ToggleStartPause()
    {
        if (State == TimerState.Running) Pause();
        else if (State == TimerState.Paused) Resume();
        else Start();
    }

    public void Stop(bool reset)
    {
        _stopwatch.Stop();
        _uiTimer.Stop();
        if (reset)
        {
            _stopwatch.Reset();
            FinishRaised = false;
        }
        State = TimerState.Stopped;
        Log.Info(reset ? "Timer stopped and reset." : "Timer stopped.");
        RaiseUpdate();
    }

    public void Reset()
    {
        _stopwatch.Reset();
        FinishRaised = false;
        State = TimerState.Stopped;
        _uiTimer.Stop();
        Log.Info("Timer reset.");
        RaiseUpdate();
    }

    private void Tick()
    {
        var snapshot = CreateSnapshot();
        if (_mode == TimerMode.Countdown && !FinishRaised && snapshot.Remaining <= TimeSpan.Zero)
        {
            FinishRaised = true;
            State = TimerState.Finished;
            if (!_continueOvertime)
            {
                _stopwatch.Stop();
                _uiTimer.Stop();
            }
            Log.Info("Timer finished.");
            var finalSnapshot = CreateSnapshot();
            Updated?.Invoke(this, finalSnapshot);
            Finished?.Invoke(this, finalSnapshot);
            return;
        }
        Updated?.Invoke(this, snapshot);
    }

    private void RaiseUpdate() => Updated?.Invoke(this, CreateSnapshot());

    public TimerSnapshot CreateSnapshot()
    {
        var elapsed = _stopwatch.Elapsed;
        var remaining = _mode == TimerMode.Countdown ? _duration - elapsed : TimeSpan.Zero;
        var display = _mode == TimerMode.Countdown ? remaining : elapsed;
        if (_mode == TimerMode.Countdown && remaining < TimeSpan.Zero)
        {
            display = _continueOvertime ? elapsed - _duration : TimeSpan.Zero;
            remaining = TimeSpan.Zero;
        }
        return new TimerSnapshot(State, _mode, elapsed, remaining, display, _duration);
    }

    public static TimeSpan ParseDuration(string value)
    {
        return TimeSpan.TryParse(value, out var parsed) && parsed > TimeSpan.Zero ? parsed : TimeSpan.FromMinutes(8);
    }
}

public enum TimerState
{
    Stopped,
    Running,
    Paused,
    Finished
}

public sealed record TimerSnapshot(TimerState State, TimerMode Mode, TimeSpan Elapsed, TimeSpan Remaining, TimeSpan Display, TimeSpan Duration);
