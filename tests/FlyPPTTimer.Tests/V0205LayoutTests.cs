using FlyPPTTimer.Forms;
using FlyPPTTimer.Services;

namespace FlyPPTTimer.Tests;

public sealed class V0205LayoutTests
{
    [Fact]
    public void RemoteWindowUsesTopIndependentNavigationAndEightPointSpacing()
    {
        var form = Read("src", "FlyPPTTimer", "Forms", "RemoteControlForm.cs");
        var theme = Read("src", "FlyPPTTimer", "Forms", "RemoteDashboardTheme.cs");

        Assert.Contains("BuildTopNavigation", form);
        Assert.DoesNotContain("BuildSidebar", form);
        Assert.Contains("WrapContents = false", form);
        Assert.Contains("MinimumSize = new Size(104, RemoteDashboardTheme.NavigationButtonHeight)", form);
        Assert.Contains("Padding = new Padding(12, 0, 12, 0)", form);
        Assert.Contains("CornerRadius = RemoteDashboardTheme.NavigationRadius", form);
        Assert.Contains("public const int NavigationButtonHeight = 32", theme);
        Assert.Contains("public const int NavigationButtonGap = 12", theme);
        Assert.Contains("public const int NavigationRadius = 5", theme);
        Assert.Contains("public const int PagePadding = 14", theme);
        Assert.Contains("public const int CardGap = 10", theme);
        Assert.Contains("public const int ControlGap = 8", theme);
    }

    [Fact]
    public void RemotePagesUseRequiredLabelsAndDoNotUseIconsOrConnectedTabs()
    {
        var form = Read("src", "FlyPPTTimer", "Forms", "RemoteControlForm.cs");

        foreach (var text in new[]
        {
            "通过手机或浏览器控制演示",
            "手机扫码连接",
            "本机 IP",
            "访问链接",
            "允许远程控制",
            "手机与电脑需连接同一局域网；也可通过手机热点或电脑热点创建局域网进行控制。",
            "演示文稿列表",
            "未选择演示文稿",
            "规则设置",
            "放映控制",
            "退出软件"
        })
            Assert.Contains(text, form);

        Assert.DoesNotContain("ImageList", form);
        Assert.DoesNotContain("TabControl", form);
        Assert.DoesNotContain("BuildSidebar", form);
    }

    [Fact]
    public void ReadOnlyAndDisabledControlsHaveWholeControlStateStyling()
    {
        var form = Read("src", "FlyPPTTimer", "Forms", "RemoteControlForm.cs");
        var theme = Read("src", "FlyPPTTimer", "Forms", "RemoteDashboardTheme.cs");
        var button = Read("src", "FlyPPTTimer", "Forms", "RemoteTextButton.cs");

        Assert.Contains("RemoteDashboardTheme.ReadOnlyField", form);
        Assert.Contains("RemoteDashboardTheme.DisabledField", form);
        Assert.Contains("control.EnabledChanged += (_, _) => ApplyState()", form);
        Assert.Contains("if (!Enabled)", button);
        Assert.Contains("ReadOnlyField", theme);
        Assert.Contains("DisabledBorder", theme);
    }

    [Theory]
    [InlineData(96)]
    [InlineData(120)]
    [InlineData(144)]
    [InlineData(168)]
    public void MinimumWindowAndStandardLayoutAreStableFrom100To175Percent(int dpi)
    {
        var minimum = RemoteWindowLayoutService.DipToPhysical(new Size(700, 510), dpi);
        Assert.Equal(new Size(700, 510), RemoteWindowLayoutService.GetLogicalClientSize(minimum, dpi));

        var standard = RemoteWindowLayoutService.DipToPhysical(new Size(700, 510), dpi);
        Assert.Equal(RemoteLayoutMode.Standard, RemoteWindowLayoutService.GetLayoutMode(standard, dpi));
    }

    [Theory]
    [InlineData("通过手机或浏览器控制演示", "Mobile or browser access")]
    [InlineData("手机扫码连接", "Scan with your phone")]
    [InlineData("本机 IP", "IP address")]
    [InlineData("访问链接", "Access link")]
    [InlineData("允许远程控制", "Allow remote control")]
    [InlineData("未选择演示文稿", "Not selected")]
    [InlineData("规则设置", "Rule settings")]
    [InlineData("退出软件", "Quit software")]
    public void NewRemoteTextHasCompleteEnglishTranslation(string chinese, string english)
    {
        Localization.Initialize(Localization.English);
        Assert.Equal(english, Localization.T(chinese));
    }

    [Fact]
    public void SettingsNavigationPaintsAnInsetRoundedRectangleAndHasExtraVerticalRoom()
    {
        var settings = Read("src", "FlyPPTTimer", "Forms", "SettingsForm.cs");
        var theme = Read("src", "FlyPPTTimer", "Forms", "ModernTheme.cs");

        Assert.Contains("new Rectangle(2, 2, Math.Max(1, Width - 4), Math.Max(1, Height - 4))", settings);
        Assert.Contains("RowStyles.Add(new RowStyle(SizeType.Absolute, 64))", settings);
        Assert.Contains("ReadOnlyFill", settings);
        Assert.Contains("public static readonly Color ControlFill = Color.FromArgb(241, 248, 252)", theme);
    }

    private static string Read(params string[] parts) =>
        File.ReadAllText(Path.Combine(
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..")),
            Path.Combine(parts)));
}
