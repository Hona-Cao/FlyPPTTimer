using FlyPPTTimer.Models;
using FlyPPTTimer.Services;

namespace FlyPPTTimer.Tests;

public sealed class TimerServiceTests
{
    [Fact]
    public void CountdownFinish_PublishesFinishedFinalSnapshotToBothEvents()
    {
        var timer = new TimerService(TestLog.Create());
        var config = new AppConfig();
        config.Timer.DefaultDuration = "00:00:00.030";
        config.Timer.ContinueOvertime = false;
        timer.Configure(config);
        TimerSnapshot? updated = null, finished = null;
        timer.Updated += (_, value) => updated = value;
        timer.Finished += (_, value) => finished = value;
        timer.Start();
        Thread.Sleep(60);
        timer.ProcessTickForTest();
        Assert.NotNull(updated);
        Assert.NotNull(finished);
        Assert.Equal(TimerState.Finished, updated!.State);
        Assert.Equal(updated, finished);
    }

    [Fact]
    public void CountdownOvertime_ContinuesRunningAndCanStillPause()
    {
        var timer = new TimerService(TestLog.Create());
        var config = new AppConfig();
        config.Timer.DefaultDuration = "00:00:00.030";
        config.Timer.ContinueOvertime = true;
        timer.Configure(config);
        var finishedCount = 0;
        timer.Finished += (_, _) => finishedCount++;

        timer.Start();
        Thread.Sleep(60);
        timer.ProcessTickForTest();
        var overtime = timer.CreateSnapshot();

        Assert.Equal(TimerState.Running, overtime.State);
        Assert.True(overtime.IsOvertime);
        Assert.True(overtime.Display > TimeSpan.Zero);
        Assert.Equal(1, finishedCount);

        timer.Pause();
        Assert.Equal(TimerState.Paused, timer.State);
        Assert.True(timer.CreateSnapshot().IsOvertime);
    }

    [Fact]
    public void CountUp_ReachesSameFinishAndOvertimeLifecycleAsCountdown()
    {
        var timer = new TimerService(TestLog.Create());
        var config = new AppConfig();
        config.Timer.Mode = TimerMode.CountUp;
        config.Timer.DefaultDuration = "00:00:00.030";
        config.Timer.ContinueOvertime = true;
        timer.Configure(config);
        var finishedCount = 0;
        timer.Finished += (_, _) => finishedCount++;

        timer.Start();
        Thread.Sleep(60);
        timer.ProcessTickForTest();

        var snapshot = timer.CreateSnapshot();
        Assert.Equal(TimerState.Running, snapshot.State);
        Assert.True(snapshot.IsOvertime);
        Assert.Equal(1, finishedCount);
    }
}
