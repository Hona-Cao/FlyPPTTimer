using FlyPPTTimer.Core.Timing;

namespace FlyPPTTimer.Core.Tests.Timing;

public sealed class TimerEngineTests
{
    [Fact]
    public void CountdownStopsAtDurationAndRaisesFinishedOnce()
    {
        var clock = new ManualClock();
        var engine = new TimerEngine(clock, new TimerConfiguration(
            TimeSpan.FromSeconds(10),
            TimerDirection.Countdown,
            ContinueOvertime: false));
        var finishedCount = 0;
        engine.Finished += (_, _) => finishedCount++;

        engine.Start();
        clock.Advance(TimeSpan.FromSeconds(10));

        var snapshot = engine.Update();
        engine.Update();

        Assert.Equal(TimerRunState.Finished, snapshot.State);
        Assert.Equal(TimeSpan.Zero, snapshot.Display);
        Assert.Equal(TimeSpan.FromSeconds(10), snapshot.Elapsed);
        Assert.Equal(1, finishedCount);
    }

    [Fact]
    public void PauseFreezesElapsedTimeUntilResume()
    {
        var clock = new ManualClock();
        var engine = new TimerEngine(clock);

        engine.Start();
        clock.Advance(TimeSpan.FromSeconds(2));
        engine.Pause();
        clock.Advance(TimeSpan.FromSeconds(5));

        Assert.Equal(TimeSpan.FromSeconds(2), engine.CreateSnapshot().Elapsed);

        engine.Resume();
        clock.Advance(TimeSpan.FromSeconds(1));

        Assert.Equal(TimeSpan.FromSeconds(3), engine.Update().Elapsed);
    }

    [Fact]
    public void CountdownCanContinueIntoOvertime()
    {
        var clock = new ManualClock();
        var engine = new TimerEngine(clock, new TimerConfiguration(
            TimeSpan.FromSeconds(3),
            TimerDirection.Countdown,
            ContinueOvertime: true));

        engine.Start();
        clock.Advance(TimeSpan.FromSeconds(5));

        var snapshot = engine.Update();

        Assert.Equal(TimerRunState.Running, snapshot.State);
        Assert.True(snapshot.IsOvertime);
        Assert.Equal(TimeSpan.FromSeconds(2), snapshot.Display);
        Assert.True(engine.FinishRaised);
    }

    [Fact]
    public void ConfigureCanPreserveFinishRaisedForLegacyHostCompatibility()
    {
        var clock = new ManualClock();
        var engine = new TimerEngine(clock, new TimerConfiguration(
            TimeSpan.FromSeconds(3),
            TimerDirection.Countdown,
            ContinueOvertime: true));
        var finishedCount = 0;
        engine.Finished += (_, _) => finishedCount++;

        engine.Start();
        clock.Advance(TimeSpan.FromSeconds(4));
        engine.Update();

        engine.Configure(new TimerConfiguration(
            TimeSpan.FromSeconds(2),
            TimerDirection.CountUp,
            ContinueOvertime: true),
            resetFinishRaised: false);
        engine.Update();

        Assert.True(engine.FinishRaised);
        Assert.Equal(1, finishedCount);
        Assert.Equal(TimerDirection.CountUp, engine.Configuration.Direction);
        Assert.Equal(TimeSpan.FromSeconds(2), engine.Configuration.Duration);
    }

    private sealed class ManualClock : IMonotonicClock
    {
        public long Frequency => TimeSpan.TicksPerSecond;
        private long Timestamp { get; set; }

        public long GetTimestamp() => Timestamp;

        public void Advance(TimeSpan value) => Timestamp += value.Ticks;
    }
}
