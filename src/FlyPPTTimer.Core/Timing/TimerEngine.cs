using System.Diagnostics;

namespace FlyPPTTimer.Core.Timing;

public interface IMonotonicClock
{
    long Frequency { get; }
    long GetTimestamp();
}

public sealed class SystemMonotonicClock : IMonotonicClock
{
    public static SystemMonotonicClock Instance { get; } = new();

    private SystemMonotonicClock() { }

    public long Frequency => Stopwatch.Frequency;

    public long GetTimestamp() => Stopwatch.GetTimestamp();
}

public enum TimerRunState
{
    Stopped,
    Running,
    Paused,
    Finished
}

public enum TimerDirection
{
    Countdown,
    CountUp
}

public sealed record TimerConfiguration(
    TimeSpan Duration,
    TimerDirection Direction,
    bool ContinueOvertime)
{
    public static TimerConfiguration Default { get; } = new(
        TimeSpan.FromMinutes(8),
        TimerDirection.Countdown,
        false);
}

public sealed record TimerSnapshot(
    TimerRunState State,
    TimerDirection Direction,
    TimeSpan Elapsed,
    TimeSpan Remaining,
    TimeSpan Display,
    TimeSpan Duration,
    bool IsOvertime);

/// <summary>
/// Platform-independent timer state machine. A desktop host decides how often to call
/// <see cref="Update"/>; the engine itself does not depend on WinForms, WPF, or a UI thread.
/// </summary>
public sealed class TimerEngine
{
    private readonly IMonotonicClock _clock;
    private TimerConfiguration _configuration;
    private long _accumulatedTicks;
    private long _runStartedAt;
    private bool _finishRaised;

    public TimerEngine(
        IMonotonicClock? clock = null,
        TimerConfiguration? configuration = null)
    {
        _clock = clock ?? SystemMonotonicClock.Instance;
        if (_clock.Frequency <= 0)
            throw new ArgumentOutOfRangeException(nameof(clock), "Clock frequency must be positive.");

        _configuration = Validate(configuration ?? TimerConfiguration.Default);
    }

    public event EventHandler<TimerSnapshot>? Updated;
    public event EventHandler<TimerSnapshot>? Finished;

    public TimerRunState State { get; private set; } = TimerRunState.Stopped;
    public TimerConfiguration Configuration => _configuration;
    public bool FinishRaised => _finishRaised;

    public void Configure(
        TimerConfiguration configuration,
        bool resetFinishRaised = true)
    {
        _configuration = Validate(configuration);
        if (resetFinishRaised)
            _finishRaised = false;
        RaiseUpdated();
    }

    public void Start()
    {
        _accumulatedTicks = 0;
        _runStartedAt = _clock.GetTimestamp();
        _finishRaised = false;
        State = TimerRunState.Running;
        RaiseUpdated();
    }

    public void Pause()
    {
        if (State != TimerRunState.Running) return;

        _accumulatedTicks = CurrentElapsedTicks();
        State = TimerRunState.Paused;
        RaiseUpdated();
    }

    public void Resume()
    {
        if (State != TimerRunState.Paused) return;

        _runStartedAt = _clock.GetTimestamp();
        State = TimerRunState.Running;
        RaiseUpdated();
    }

    public void Stop(bool reset)
    {
        if (State == TimerRunState.Running)
            _accumulatedTicks = CurrentElapsedTicks();

        if (reset)
        {
            _accumulatedTicks = 0;
            _finishRaised = false;
        }

        State = TimerRunState.Stopped;
        RaiseUpdated();
    }

    public void Reset()
    {
        _accumulatedTicks = 0;
        _finishRaised = false;
        State = TimerRunState.Stopped;
        RaiseUpdated();
    }

    public TimerSnapshot Update()
    {
        var snapshot = CreateSnapshot();
        if (!_finishRaised && snapshot.Elapsed >= snapshot.Duration)
        {
            _finishRaised = true;
            if (!_configuration.ContinueOvertime)
            {
                _accumulatedTicks = ToClockTicks(snapshot.Duration);
                State = TimerRunState.Finished;
                snapshot = CreateSnapshot();
            }

            Updated?.Invoke(this, snapshot);
            Finished?.Invoke(this, snapshot);
            return snapshot;
        }

        Updated?.Invoke(this, snapshot);
        return snapshot;
    }

    public TimerSnapshot CreateSnapshot()
    {
        var elapsed = ToTimeSpan(CurrentElapsedTicks());
        var duration = _configuration.Duration;
        var remaining = elapsed >= duration ? TimeSpan.Zero : duration - elapsed;
        var isOvertime = _configuration.ContinueOvertime && elapsed > duration;

        var display = _configuration.Direction switch
        {
            TimerDirection.Countdown when elapsed <= duration => duration - elapsed,
            TimerDirection.Countdown when isOvertime => elapsed - duration,
            TimerDirection.Countdown => TimeSpan.Zero,
            TimerDirection.CountUp when !_configuration.ContinueOvertime && elapsed > duration => duration,
            _ => elapsed
        };

        return new TimerSnapshot(
            State,
            _configuration.Direction,
            elapsed,
            remaining,
            display,
            duration,
            isOvertime);
    }

    private void RaiseUpdated() => Updated?.Invoke(this, CreateSnapshot());

    private long CurrentElapsedTicks()
    {
        if (State != TimerRunState.Running) return _accumulatedTicks;

        var delta = _clock.GetTimestamp() - _runStartedAt;
        return delta <= 0 ? _accumulatedTicks : checked(_accumulatedTicks + delta);
    }

    private TimeSpan ToTimeSpan(long ticks) =>
        ticks <= 0
            ? TimeSpan.Zero
            : TimeSpan.FromSeconds(ticks / (double)_clock.Frequency);

    private long ToClockTicks(TimeSpan value) =>
        checked((long)Math.Round(
            value.TotalSeconds * _clock.Frequency,
            MidpointRounding.AwayFromZero));

    private static TimerConfiguration Validate(TimerConfiguration configuration)
    {
        if (configuration.Duration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(configuration), "Timer duration must be positive.");

        return configuration;
    }
}
