using FlyPPTTimer.Models;
using FlyPPTTimer.Services;

namespace FlyPPTTimer.Tests;

public sealed class PresentationLifecycleTests
{
    [Fact]
    public void AutoStartDisabled_DoesNotResetOrStartTimer()
    {
        var config = new AppConfig();
        config.Behavior.AutoStartOnFullscreen = false;
        var calls = new List<string>();
        var sut = Create(config, calls);
        sut.Observe(true, @"C:\slides\demo.pptx", "test");
        Assert.Empty(calls);
    }

    [Theory]
    [InlineData(true, true, "stop-reset")]
    [InlineData(true, false, "stop")]
    [InlineData(false, true, "reset")]
    [InlineData(false, false, "none")]
    public void LeavingPresentation_UsesConfiguredStopResetCombination(bool stop, bool reset, string expected)
    {
        var config = new AppConfig();
        config.Behavior.StopWhenLeavingFullscreen = stop;
        config.Behavior.ResetWhenLeavingFullscreen = reset;
        var calls = new List<string>();
        var sut = Create(config, calls);
        sut.Observe(true, @"C:\slides\demo.pptx", "test");
        calls.Clear();
        sut.Observe(false, "", "test");
        Assert.Equal(expected, calls.Count == 0 ? "none" : calls.Single());
    }

    private static PresentationLifecycleController Create(AppConfig config, List<string> calls) => new(
        () => config, _ => calls.Add("duration"), () => calls.Add("alerts"),
        () => calls.Add("stop-reset"), () => calls.Add("start"),
        reset => calls.Add(reset ? "stop-reset" : "stop"), () => calls.Add("reset"),
        TestLog.Create());
}

internal static class TestLog
{
    public static LogService Create() => new(Path.Combine(Path.GetTempPath(), "FlyPPTTimerTests", Guid.NewGuid().ToString("N")));
}
