using FlyPPTTimer.Forms;
using FlyPPTTimer.Models;

namespace FlyPPTTimer.Tests;

public sealed class V0186FeatureTests
{
    [Fact]
    public void DifferentWindowSizesKeepTheSameConfiguredOrigin()
    {
        var screen = Screen.PrimaryScreen!;
        var large = new Size(140, 50);
        var small = new Size(32, 12);
        var largeLocation = TimerOverlayForm.CalculateLocation(screen, large, OverlayAnchor.TopCenter, 0, 0.5m);
        var smallLocation = TimerOverlayForm.CalculateLocation(screen, small, OverlayAnchor.TopCenter, 0, 0.5m);

        Assert.Equal(largeLocation.X + large.Width / 2f, smallLocation.X + small.Width / 2f);
        Assert.Equal(largeLocation.Y + large.Height / 2f, smallLocation.Y + small.Height / 2f);
    }

    [Fact]
    public void SettingsAndOverlayDoNotRetainLegacyMinimumSizeFloors()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var settings = File.ReadAllText(Path.Combine(root, "src", "FlyPPTTimer", "Forms", "SettingsForm.cs"));
        var overlay = File.ReadAllText(Path.Combine(root, "src", "FlyPPTTimer", "Forms", "TimerOverlayForm.cs"));

        Assert.Contains("ReadInt(\"height\", 1, 1000", settings);
        Assert.Contains("Math.Max(1, config.Appearance.Height)", overlay);
        Assert.DoesNotContain("Math.Max(48, config.Appearance.Height)", overlay);
    }

    [Fact]
    public void WpsWindowIsMaximizedBeforeItBecomesVisible()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var source = File.ReadAllText(Path.Combine(root, "src", "FlyPPTTimer", "Services", "PowerPointControlService.cs"));
        var preMaximize = source.IndexOf("app.WindowState = NativeMethods.SwMaximize", StringComparison.Ordinal);
        var visible = source.IndexOf("app.Visible = true", preMaximize, StringComparison.Ordinal);

        Assert.True(preMaximize >= 0);
        Assert.True(visible > preMaximize);
        Assert.Contains("PreparePresentationWindowForFirstDisplay(presentation)", source);
    }
}
