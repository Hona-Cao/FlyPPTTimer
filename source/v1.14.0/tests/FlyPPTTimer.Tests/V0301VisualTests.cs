namespace FlyPPTTimer.Tests;

public sealed class V0301VisualTests
{
    [Fact]
    public void RemoteNavigationUsesOnlyAHorizontalDivider()
    {
        var form = Read("src", "FlyPPTTimer", "Forms", "RemoteControlForm.cs");
        var theme = Read("src", "FlyPPTTimer", "Forms", "RemoteDashboardTheme.cs");

        Assert.Contains("navigationArea.Paint +=", form);
        Assert.Contains("new Pen(RemoteDashboardTheme.Border, 1F)", form);
        Assert.DoesNotContain("navigationBar.FillColor", form);
        Assert.DoesNotContain("页面导航", form);
        Assert.DoesNotContain("NavigationBar", theme);
    }

    [Fact]
    public void ComboBoxAndDropDownListRemoveNativeBordersAndUseRoundedRegions()
    {
        var theme = Read("src", "FlyPPTTimer", "Forms", "ModernTheme.cs");

        Assert.Contains("parameters.Style &= ~WsBorder", theme);
        Assert.Contains("parameters.ExStyle &= ~WsExClientEdge", theme);
        Assert.Contains("GetComboBoxInfo", theme);
        Assert.Contains("SetWindowLong(info.ListHandle, GwlStyle, style)", theme);
        Assert.Contains("CreateRoundRectRgn", theme);
        Assert.Contains("SetWindowRgn(info.ListHandle, region, true)", theme);
        Assert.Contains("private const int ComboRadius = 4", theme);
    }

    private static string Read(params string[] parts) =>
        File.ReadAllText(Path.Combine(RepoRoot(), Path.Combine(parts)));

    private static string RepoRoot() => Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "..", "..", "..", "..", ".."));
}
