using FlyPPTTimer.Core.Timing;
using FlyPPTTimer.Models;
using CoreTimerSnapshot = FlyPPTTimer.Core.Timing.TimerSnapshot;

namespace FlyPPTTimer.Services;

public sealed class TimerService
{
    private readonly TimerEngine _engine;
    private readonly System.Windows.Forms.Timer _uiTimer = new() { Interval = 100 };
    private readonly bool _useUiTicker;

    public TimerService(LogService log)
        : this(log, SystemMonotonicClock.Instance, useUiTicker: true)
    {
    }

    internal TimerService(
        LogService log,
        IMonotonicClock clock,
        bool useUiTicker = false)
    {
        Log = log;
        _engine = new TimerEngine(clock);
        _useUiTicker = useUiTicker;
        _uiTimer.Tick += (_, _) => Tick();
    }

    public event EventHandler<TimerSnapshot>? Updated;
    public event EventHandler<TimerSnapshot>? Finished;
    public LogService Log { get; }
    public TimerState State => MapState(_engine.State);
    public bool FinishRaised => _engine.FinishRaised;
    public TimeSpan Duration => _engine.Configuration.Duration;
    public TimerMode Mode => MapMode(_engine.Configuration.Direction);

    public void Configure(AppConfig config)
    {
        var configuration = new TimerConfiguration(
            ParseDuration(config.Timer.DefaultDuration),
            MapMode(config.Timer.Mode),
            config.Timer.EndAction == TimerEndAction.None && config.Timer.ContinueOvertime);

        _engine.Configure(configuration, resetFinishRaised: false);
        if (State == TimerState.Stopped)
            RaiseUpdate();
    }

    public void SetDuration(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero) return;

        _engine.Configure(_engine.Configuration with { Duration = duration });
        Log.Info($"Timer duration set: {duration}");
        RaiseUpdate();
    }

    public void SetMode(TimerMode mode)
    {
        _engine.Configure(_engine.Configuration with { Direction = MapMode(mode) });
        Log.Info($"Timer mode set: {mode}");
        RaiseUpdate();
    }

    public void Start()
    {
        _engine.Start();
        if (_useUiTicker)
            _uiTimer.Start();
        Log.Info("Timer started.");
        RaiseUpdate();
    }

    public void Pause()
    {
        if (State != TimerState.Running) return;

        _engine.Pause();
        Log.Info("Timer paused.");
        RaiseUpdate();
    }

    public void Resume()
    {
        if (State != TimerState.Paused) return;

        _engine.Resume();
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
        _engine.Stop(reset);
        _uiTimer.Stop();
        Log.Info(reset ? "Timer stopped and reset." : "Timer stopped.");
        RaiseUpdate();
    }

    public void Reset()
    {
        _engine.Reset();
        _uiTimer.Stop();
        Log.Info("Timer reset.");
        RaiseUpdate();
    }

    private void Tick()
    {
        var finishWasRaised = _engine.FinishRaised;
        var coreSnapshot = _engine.Update();
        var snapshot = MapSnapshot(coreSnapshot);

        if (!finishWasRaised && _engine.FinishRaised)
        {
            if (_engine.State == TimerRunState.Finished)
                _uiTimer.Stop();

            Log.Info("Timer finished.");
            Updated?.Invoke(this, snapshot);
            Finished?.Invoke(this, snapshot);
            return;
        }

        Updated?.Invoke(this, snapshot);
    }

    private void RaiseUpdate() => Updated?.Invoke(this, CreateSnapshot());

    internal void ProcessTickForTest() => Tick();

    public TimerSnapshot CreateSnapshot() => MapSnapshot(_engine.CreateSnapshot());

    public static TimeSpan ParseDuration(string value)
    {
        return TimeSpan.TryParse(value, out var parsed) && parsed > TimeSpan.Zero
            ? parsed
            : TimeSpan.FromMinutes(8);
    }

    private static TimerState MapState(TimerRunState state) => state switch
    {
        TimerRunState.Running => TimerState.Running,
        TimerRunState.Paused => TimerState.Paused,
        TimerRunState.Finished => TimerState.Finished,
        _ => TimerState.Stopped
    };

    private static TimerMode MapMode(TimerDirection direction) =>
        direction == TimerDirection.CountUp ? TimerMode.CountUp : TimerMode.Countdown;

    private static TimerDirection MapMode(TimerMode mode) =>
        mode == TimerMode.CountUp ? TimerDirection.CountUp : TimerDirection.Countdown;

    private static TimerSnapshot MapSnapshot(CoreTimerSnapshot snapshot) => new(
        MapState(snapshot.State),
        MapMode(snapshot.Direction),
        snapshot.Elapsed,
        snapshot.Remaining,
        snapshot.Display,
        snapshot.Duration,
        snapshot.IsOvertime);
}

public enum TimerState
{
    Stopped,
    Running,
    Paused,
    Finished
}

public sealed record TimerSnapshot(
    TimerState State,
    TimerMode Mode,
    TimeSpan Elapsed,
    TimeSpan Remaining,
    TimeSpan Display,
    TimeSpan Duration,
    bool IsOvertime);
