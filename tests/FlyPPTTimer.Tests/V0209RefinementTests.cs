namespace FlyPPTTimer.Tests;

public sealed class V0209RefinementTests
{
    [Fact]
    public void RemoteNavigationHasASeparateDivider()
    {
        var source = Read("src", "FlyPPTTimer", "Forms", "RemoteControlForm.cs");
        var theme = Read("src", "FlyPPTTimer", "Forms", "RemoteDashboardTheme.cs");

        Assert.Contains("navigationArea = new Panel", source);
        Assert.Contains("new Pen(RemoteDashboardTheme.Border, 1F)", source);
        Assert.DoesNotContain("Localization.T(\"页面导航\")", source);
        Assert.DoesNotContain("RemoteDashboardTheme.NavigationBar", source);
        Assert.Contains("NavigationHeight = 40", theme);
    }

    [Fact]
    public void ConnectionGuidanceAppearsOnceAndMentionsHotspots()
    {
        var source = Read("src", "FlyPPTTimer", "Forms", "RemoteControlForm.cs");

        Assert.DoesNotContain("手机与电脑需连接同一网络。", source);
        Assert.DoesNotContain("同一网络下的手机或电脑均可访问。", source);
        Assert.Equal(
            1,
            source.Split("手机与电脑需连接同一局域网；也可通过手机热点或电脑热点创建局域网进行控制。").Length - 1);
    }

    [Fact]
    public void AppearanceSettingsSeparateTimerAndFullScreenModes()
    {
        var source = Read("src", "FlyPPTTimer", "Forms", "SettingsForm.cs");
        var timer = source.IndexOf("Section(grid, \"计时器窗口\")", StringComparison.Ordinal);
        var colors = source.IndexOf("Section(grid, \"配色\")", timer, StringComparison.Ordinal);
        var fullScreen = source.IndexOf("Section(grid, \"大屏计时模式\")", colors, StringComparison.Ordinal);

        Assert.True(timer >= 0 && timer < colors);
        Assert.True(fullScreen > colors);
        Assert.Contains("Row(grid, \"单屏显示屏幕\"", source);
        Assert.Contains("singleScreenTarget.Enabled = !showOnAllScreens.Checked", source);
    }

    [Fact]
    public void SettingsComboBoxesAreRoundedCenteredAndWheelResponsive()
    {
        var settings = Read("src", "FlyPPTTimer", "Forms", "SettingsForm.cs");
        var theme = Read("src", "FlyPPTTimer", "Forms", "ModernTheme.cs");

        Assert.Contains("ComboBox => Padding.Empty", settings);
        Assert.Contains("if (root is ComboBox) return;", settings);
        Assert.DoesNotContain("BlockMouseWheel", settings);
        Assert.Contains("ApplyComboRegion()", theme);
        Assert.Contains("TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter", theme);
    }

    private static string Read(params string[] parts) =>
        File.ReadAllText(Path.Combine(RepoRoot(), Path.Combine(parts)));

    private static string RepoRoot() => Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "..", "..", "..", "..", ".."));
}
