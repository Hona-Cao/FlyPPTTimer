using FlyPPTTimer.Core.Timing;
using FlyPPTTimer.Models;
using FlyPPTTimer.Services;

namespace FlyPPTTimer.Tests;

public sealed class TimerServiceTests
{
    [Fact]
    public void CountdownFinish_PublishesFinishedFinalSnapshotToBothEvents()
    {
        var clock = new ManualClock();
        var timer = CreateTimer(clock, duration: TimeSpan.FromSeconds(3));
        TimerSnapshot? updated = null, finished = null;
        timer.Updated += (_, value) => updated = value;
        timer.Finished += (_, value) => finished = value;

        timer.Start();
        clock.Advance(TimeSpan.FromSeconds(3));
        timer.ProcessTickForTest();

        Assert.NotNull(updated);
        Assert.NotNull(finished);
        Assert.Equal(TimerState.Finished, updated!.State);
        Assert.Equal(TimeSpan.FromSeconds(3), updated.Elapsed);
        Assert.Equal(TimeSpan.Zero, updated.Display);
        Assert.Equal(updated, finished);
    }

    [Fact]
    public void CountdownOvertime_ContinuesRunningAndCanStillPause()
    {
        var clock = new ManualClock();
        var timer = CreateTimer(
            clock,
            duration: TimeSpan.FromSeconds(3),
            continueOvertime: true);
        var finishedCount = 0;
        timer.Finished += (_, _) => finishedCount++;

        timer.Start();
        clock.Advance(TimeSpan.FromSeconds(5));
        timer.ProcessTickForTest();
        var overtime = timer.CreateSnapshot();

        Assert.Equal(TimerState.Running, overtime.State);
        Assert.True(overtime.IsOvertime);
        Assert.Equal(TimeSpan.FromSeconds(2), overtime.Display);
        Assert.Equal(1, finishedCount);

        timer.Pause();
        clock.Advance(TimeSpan.FromSeconds(10));

        Assert.Equal(TimerState.Paused, timer.State);
        Assert.Equal(overtime.Elapsed, timer.CreateSnapshot().Elapsed);
        Assert.True(timer.CreateSnapshot().IsOvertime);
    }

    [Fact]
    public void CountUp_ReachesSameFinishAndOvertimeLifecycleAsCountdown()
    {
        var clock = new ManualClock();
        var timer = CreateTimer(
            clock,
            duration: TimeSpan.FromSeconds(3),
            mode: TimerMode.CountUp,
            continueOvertime: true);
        var finishedCount = 0;
        timer.Finished += (_, _) => finishedCount++;

        timer.Start();
        clock.Advance(TimeSpan.FromSeconds(5));
        timer.ProcessTickForTest();

        var snapshot = timer.CreateSnapshot();
        Assert.Equal(TimerState.Running, snapshot.State);
        Assert.True(snapshot.IsOvertime);
        Assert.Equal(TimeSpan.FromSeconds(5), snapshot.Display);
        Assert.Equal(1, finishedCount);
    }

    [Fact]
    public void Configure_PreservesAlreadyRaisedFinishStateLikeLegacyService()
    {
        var clock = new ManualClock();
        var timer = CreateTimer(
            clock,
            duration: TimeSpan.FromSeconds(3),
            continueOvertime: true);
        var finishedCount = 0;
        timer.Finished += (_, _) => finishedCount++;

        timer.Start();
        clock.Advance(TimeSpan.FromSeconds(4));
        timer.ProcessTickForTest();

        var config = CreateConfig(
            duration: TimeSpan.FromSeconds(2),
            mode: TimerMode.CountUp,
            continueOvertime: true);
        timer.Configure(config);
        timer.ProcessTickForTest();

        Assert.True(timer.FinishRaised);
        Assert.Equal(1, finishedCount);
        Assert.Equal(TimerMode.CountUp, timer.Mode);
        Assert.Equal(TimeSpan.FromSeconds(2), timer.Duration);
    }

    [Fact]
    public void SetDuration_RearmsFinishAndStopWithoutResetPreservesElapsed()
    {
        var clock = new ManualClock();
        var timer = CreateTimer(
            clock,
            duration: TimeSpan.FromSeconds(3),
            continueOvertime: true);

        timer.Start();
        clock.Advance(TimeSpan.FromSeconds(4));
        timer.ProcessTickForTest();
        Assert.True(timer.FinishRaised);

        timer.SetDuration(TimeSpan.FromSeconds(10));
        Assert.False(timer.FinishRaised);

        timer.Stop(reset: false);
        var stopped = timer.CreateSnapshot();
        clock.Advance(TimeSpan.FromSeconds(5));

        Assert.Equal(TimerState.Stopped, stopped.State);
        Assert.Equal(stopped.Elapsed, timer.CreateSnapshot().Elapsed);

        timer.Reset();
        Assert.Equal(TimeSpan.Zero, timer.CreateSnapshot().Elapsed);
    }

    private static TimerService CreateTimer(
        ManualClock clock,
        TimeSpan duration,
        TimerMode mode = TimerMode.Countdown,
        bool continueOvertime = false)
    {
        var timer = new TimerService(TestLog.Create(), clock);
        timer.Configure(CreateConfig(duration, mode, continueOvertime));
        return timer;
    }

    private static AppConfig CreateConfig(
        TimeSpan duration,
        TimerMode mode,
        bool continueOvertime)
    {
        var config = new AppConfig();
        config.Timer.DefaultDuration = duration.ToString(@"hh\:mm\:ss");
        config.Timer.Mode = mode;
        config.Timer.EndAction = TimerEndAction.None;
        config.Timer.ContinueOvertime = continueOvertime;
        return config;
    }

    private sealed class ManualClock : IMonotonicClock
    {
        public long Frequency => TimeSpan.TicksPerSecond;
        private long Timestamp { get; set; }

        public long GetTimestamp() => Timestamp;

        public void Advance(TimeSpan value) => Timestamp += value.Ticks;
    }
}
