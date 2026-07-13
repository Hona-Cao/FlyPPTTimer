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
}
