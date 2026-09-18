namespace FlyPPTTimer.Tests;

public sealed class V0300FeatureTests
{
    [Fact]
    public void BigScreenTimerIsResizableAndNeverUsesThePrimaryDisplay()
    {
        var form = Read("src", "FlyPPTTimer", "Forms", "BigScreenTimerForm.cs");
        var context = Read("src", "FlyPPTTimer", "FlyPPTTimerContext.cs");

        Assert.Contains("FormBorderStyle = FormBorderStyle.Sizable", form);
        Assert.Contains("MinimizeBox = true", form);
        Assert.Contains("MaximizeBox = true", form);
        Assert.Contains("WindowState = FormWindowState.Maximized", form);
        Assert.Contains("if (screen.Primary)", form);
        Assert.Contains("Screen.AllScreens.Where(candidate => !candidate.Primary)", context);
        Assert.Contains("?? extendedScreens[0]", context);
    }

    [Fact]
    public void BigScreenOwnsOnlyTheFontItCreates()
    {
        var form = Read("src", "FlyPPTTimer", "Forms", "BigScreenTimerForm.cs");

        Assert.Contains("private Font? _timerFont", form);
        Assert.Contains("previousFont?.Dispose()", form);
        Assert.DoesNotContain("_display.Font?.Dispose()", form);
    }

    [Fact]
    public void BigScreenSettingsRequireAnExtendedDisplay()
    {
        var settings = Read("src", "FlyPPTTimer", "Forms", "SettingsForm.cs");

        Assert.Contains("GetExtendedScreenItems()", settings);
        Assert.Contains("Enabled = hasExtendedScreen", settings);
        Assert.Contains("if (hasExtendedScreen)", settings);
        Assert.Contains("UpdateBigScreenTargetState", settings);
        Assert.DoesNotContain("Combo(GetScreenItems(), string.IsNullOrWhiteSpace(_config.Placement.BigScreenDeviceName)", settings);
    }

    [Fact]
    public void DisabledControlsChangeTheirExistingSurfaceInsteadOfAddingAnOverlay()
    {
        var settings = Read("src", "FlyPPTTimer", "Forms", "SettingsForm.cs");
        var theme = Read("src", "FlyPPTTimer", "Forms", "ModernTheme.cs");

        Assert.Contains("host.FillColor = fill", settings);
        Assert.Contains("control.BackColor = fill", settings);
        Assert.Contains("return combo;", settings);
        Assert.Contains("if (m.Msg == WmNcPaint) return;", theme);
        Assert.Contains("BackColor = Enabled ? ModernTheme.ControlFill : ModernTheme.ReadOnlyFill", theme);
    }

    private static string Read(params string[] parts) =>
        File.ReadAllText(Path.Combine(RepoRoot(), Path.Combine(parts)));

    private static string RepoRoot() => Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "..", "..", "..", "..", ".."));
}
