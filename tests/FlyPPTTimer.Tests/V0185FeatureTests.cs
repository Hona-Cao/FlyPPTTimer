using FlyPPTTimer.Forms;
using FlyPPTTimer.Models;
using FlyPPTTimer.Services;

namespace FlyPPTTimer.Tests;

public sealed class V0185FeatureTests
{
    [Fact]
    public void Normalize_FixesTimerFontAndDisablesPerSlidePlaceholder()
    {
        var config = new AppConfig();
        config.Appearance.FontFamily = "Arial";
        config.Timer.EnablePerSlideTimer = true;

        ConfigService.Normalize(config);

        Assert.Equal("Microsoft YaHei UI", config.Appearance.FontFamily);
        Assert.False(config.Timer.EnablePerSlideTimer);
    }

    [Theory]
    [InlineData(TimerEndAction.BlackScreen)]
    [InlineData(TimerEndAction.ExitSlideShow)]
    public void NonPromptEndActions_DoNotContinueOvertime(TimerEndAction endAction)
    {
        var timer = new TimerService(TestLog.Create());
        var config = new AppConfig();
        config.Timer.DefaultDuration = "00:00:00.030";
        config.Timer.ContinueOvertime = true;
        config.Timer.EndAction = endAction;
        timer.Configure(config);

        timer.Start();
        Thread.Sleep(60);
        timer.ProcessTickForTest();

        Assert.Equal(TimerState.Finished, timer.State);
        Assert.False(timer.CreateSnapshot().IsOvertime);
    }

    [Fact]
    public void FileRuleStoresAnIndependentTimerMode()
    {
        var rule = new FileRule { Duration = "00:12:00", Mode = TimerMode.CountUp };
        Assert.Equal(TimerMode.CountUp, rule.Mode);
    }

    [Fact]
    public void TimerWidthHasNoLegacyEightyPixelFloor()
    {
        using var font = new Font("Microsoft YaHei UI", 8F);
        Assert.True(TimerOverlayForm.CalculateRequiredWidth(":", font) < 80);
    }

    [Fact]
    public void PowerPointOpenPathActivatesAReadOnlyMaximizedWindow()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var source = File.ReadAllText(Path.Combine(root, "src", "FlyPPTTimer", "Services", "PowerPointControlService.cs"));
        Assert.Contains("presentations).Open(path, true, false, true)", source);
        Assert.Contains("WindowState = NativeMethods.SwMaximize", source);
        Assert.Contains("ActivatePresentationProcessWindow", source);
        Assert.Contains("\"POWERPNT\", \"wps\", \"wpp\"", source);
    }
}
