using FlyPPTTimer.Forms;

namespace FlyPPTTimer.Tests;

public sealed class V0184FeatureTests
{
    [Fact]
    public void OverlayUsesExplicitPerMonitorDpiSizeWithoutShowTimeAutoscaling()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var overlay = File.ReadAllText(Path.Combine(root, "src", "FlyPPTTimer", "Forms", "TimerOverlayForm.cs"));

        Assert.Contains("AutoScaleMode = AutoScaleMode.None", overlay);
        Assert.Contains("RemoteScreenDpiProvider.FromScreen(screen).Dpi", overlay);
        Assert.Contains("RemoteWindowLayoutService.DipToPhysical", overlay);
    }

    [Fact]
    public void ApplyingSettingsClonesDraftBeforeComparingPlacement()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var context = File.ReadAllText(Path.Combine(root, "src", "FlyPPTTimer", "FlyPPTTimerContext.cs"));

        Assert.Contains("var nextConfig = ConfigService.Clone(config)", context);
        Assert.Contains("SamePlacement(_config.Placement, nextConfig.Placement)", context);
        Assert.Contains("_config = nextConfig", context);
    }

    [Fact]
    public void LogicalOverlaySizeConvertsToExpectedPhysicalSizeAt125Percent()
    {
        Assert.Equal(175, RemoteWindowLayoutService.DipToPhysical(140, 120));
        Assert.Equal(63, RemoteWindowLayoutService.DipToPhysical(50, 120));
        Assert.Equal(140, RemoteWindowLayoutService.PhysicalToDip(175, 120));
    }
}
