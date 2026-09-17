namespace FlyPPTTimer.Tests;

public sealed class V0302PolishTests
{
    [Fact]
    public void NavigationUsesFullHeightAndPresentationListIsVerticalOnly()
    {
        var form = Read("src", "FlyPPTTimer", "Forms", "RemoteControlForm.cs");
        var row = Read("src", "FlyPPTTimer", "Forms", "RemotePresentationRow.cs");
        var theme = Read("src", "FlyPPTTimer", "Forms", "RemoteDashboardTheme.cs");

        Assert.Contains("navigation.Dock = DockStyle.Fill", form);
        Assert.DoesNotContain("navigation.Height = RemoteDashboardTheme.NavigationButtonHeight", form);
        Assert.Contains("_ruleList.HideHorizontalScrollBar()", form);
        Assert.Contains("class VerticalFlowLayoutPanel", theme);
        Assert.Contains("ShowScrollBar(Handle, HorizontalScrollBar, false)", theme);
        Assert.Contains("MinimumSize = new Size(0, RemoteDashboardTheme.PresentationRowHeight)", row);
        Assert.Contains("AutoEllipsis = true", row);
    }

    [Fact]
    public void LiveResizeDefersExpensiveRegionAndResponsiveReflowWork()
    {
        var settings = Read("src", "FlyPPTTimer", "Forms", "SettingsForm.cs");
        var remote = Read("src", "FlyPPTTimer", "Forms", "RemoteControlForm.cs");

        Assert.Contains("protected override void OnResizeBegin", settings);
        Assert.Contains("protected override void OnResizeEnd", settings);
        Assert.DoesNotContain("SizeChanged += (_, _) => ApplyWindowChromeRegion()", settings);
        Assert.DoesNotContain("Resize += (_, _) => LayoutNavigation()", settings);
        Assert.Contains("if (_interactiveResize || WindowState == FormWindowState.Maximized)", settings);

        Assert.Contains("_presentationRefreshTimer.Enabled = false", remote);
        Assert.Contains("if (!_interactiveResize)", remote);
        Assert.Contains("if (_interactiveResize || IsDisposed", remote);
    }

    private static string Read(params string[] parts) =>
        File.ReadAllText(Path.Combine(RepoRoot(), Path.Combine(parts)));

    private static string RepoRoot() => Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "..", "..", "..", "..", ".."));
}
