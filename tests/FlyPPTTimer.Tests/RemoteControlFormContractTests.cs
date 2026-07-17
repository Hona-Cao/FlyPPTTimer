namespace FlyPPTTimer.Tests;

public sealed class RemoteControlFormContractTests
{
    [Fact]
    public void V016_RemoteControlUsesTextFirstLayout()
    {
        var source = ReadRemoteForm();
        var button = ReadRemoteButton();

        Assert.Contains("v0.16 text-first remote-control workspace", source);
        Assert.Contains("BuildSidebar", source);
        Assert.Contains("BuildConnectionPage", source);
        Assert.Contains("BuildPresentationWorkspace", source);
        Assert.Contains("TextFormatFlags.SingleLine", button);

        Assert.DoesNotContain("Text = \"●\"", source);
        Assert.DoesNotContain("CreateTileButton", source);
        Assert.DoesNotContain("连接手机或浏览器", source);
        Assert.DoesNotContain("规则、状态与放映", source);
    }

    [Fact]
    public void V016_NavigationAndActionsUseShortSingleLineText()
    {
        var source = ReadRemoteForm();

        foreach (var text in new[]
        {
            "\"远程连接\"",
            "\"演示文稿\"",
            "\"重启\"",
            "\"关闭\"",
            "\"复制链接\"",
            "\"浏览器打开\"",
            "\"放行命令\"",
            "\"添加\"",
            "\"删除\"",
            "\"刷新\"",
            "\"打开\"",
            "\"从头放映\"",
            "\"当前页放映\"",
            "\"结束放映\"",
            "\"强制退出\""
        })
        {
            Assert.Contains(text, source);
        }

        Assert.DoesNotContain("在只读模式下打开目标文稿", source);
        Assert.DoesNotContain("从第一张幻灯片开始", source);
        Assert.DoesNotContain("停止当前放映并返回", source);
    }

    [Fact]
    public void V016_PresentationWorkspaceHasOnlyListScroll()
    {
        var source = ReadRemoteForm();

        Assert.Contains("BuildPresentationList", source);
        Assert.Contains("BuildPresentationDetails", source);
        Assert.Contains("RemotePresentationRow", source);
        Assert.Contains("private readonly FlowLayoutPanel _ruleList", source);
        Assert.Contains("AutoScroll = true", source);
        Assert.DoesNotContain("BuildPresentationDetails()\n    {\n        var scroll", source);
        Assert.DoesNotContain("CollapsibleHeader", source);
        Assert.DoesNotContain("RoutePresentationWheel", source);
    }

    [Fact]
    public void V016_PresentationRowsAlwaysUseCurrentData()
    {
        var source = ReadRemoteForm();
        var row = ReadRemoteRow();

        Assert.Contains("row.CurrentRule", source);
        Assert.Contains("row.CurrentPresentation", source);
        Assert.Contains("row.CurrentPath", source);
        Assert.Contains("CurrentRule = rule", row);
        Assert.Contains("CurrentPresentation = option", row);
        Assert.Contains("CurrentPath =", row);
    }

    [Fact]
    public void V016_PreservesRemoteConnectionActions()
    {
        var source = ReadRemoteForm();

        foreach (var member in new[]
        {
            "RestartService",
            "ToggleService",
            "CurrentUrl",
            "BuildFirewallCommand",
            "RemoteUrlPrivacy.MaskToken(url)",
            "Clipboard.SetText(text)"
        })
        {
            Assert.Contains(member, source);
        }
    }

    [Fact]
    public void V016_PreservesPresentationCommands()
    {
        var source = ReadRemoteForm();

        foreach (var member in new[]
        {
            "AddPresentationRules",
            "DeleteSelectedRule",
            "SaveRulesImmediately",
            "ppt.openPresentation",
            "ppt.startFromBeginning",
            "ppt.startFromCurrent",
            "ppt.endShow",
            "ppt.closeCurrentPresentation",
            "ppt.exitApplication",
            "ppt.forceQuitAll"
        })
        {
            Assert.Contains(member, source);
        }
    }

    [Fact]
    public void V016_DangerDialogDefaultsToCancel()
    {
        var source = ReadRemoteForm();

        Assert.Contains("RemoteConfirmDialog", source);
        Assert.Contains("CancelButton = cancel", source);
        Assert.Contains("ActiveControl = cancel", source);
        Assert.DoesNotContain("AcceptButton = confirm", source);
    }

    [Fact]
    public void V016_DoesNotMutateSharedTheme()
    {
        var source = ReadRemoteForm();
        var theme = ReadRemoteTheme();

        Assert.Contains("internal static class RemoteDashboardTheme", theme);
        Assert.DoesNotContain("ModernTheme.", source);
        Assert.DoesNotContain("ModernTheme.", theme);
    }

    [Fact]
    public void V016_RefinedControlsAvoidNativeButtonAndComboBoxChrome()
    {
        var source = ReadRemoteForm();
        var theme = ReadRemoteTheme();
        var button = ReadRemoteButton();
        var selector = ReadAddressSelector();

        Assert.Contains("internal sealed class RemoteTextButton : Control, IButtonControl", button);
        Assert.DoesNotContain("FlatStyle", button);
        Assert.DoesNotContain("FlatAppearance", button);
        Assert.DoesNotContain("Region =", button);
        Assert.DoesNotContain("ControlPaint.DrawButton", button);
        Assert.DoesNotContain("VisualStyleRenderer", button);
        Assert.Contains("TextFormatFlags.SingleLine", button);
        Assert.Contains("TextFormatFlags.VerticalCenter", button);
        Assert.Contains("TextFormatFlags.HorizontalCenter", button);
        Assert.DoesNotContain("ComboBox", source);
        Assert.DoesNotContain("ComboBox", selector);
        Assert.Contains("RemoteAddressSelector", source);
        Assert.Contains("ContextMenuStrip", selector);
        Assert.DoesNotContain("Region =", theme);
    }

    [Fact]
    public void V0161_UsesSafeTextAndSingleLineServiceLayout()
    {
        var source = ReadRemoteForm();
        var theme = ReadRemoteTheme();
        var button = ReadRemoteButton();

        Assert.Equal("0.16.1", FlyPPTTimer.AppVersion.Current);
        Assert.Contains("ColumnCount = 5", source);
        Assert.Contains("RowCount = 1", source);
        Assert.DoesNotContain("_stateDescription", source);
        Assert.Contains("GetSafeTextHeight", theme);
        Assert.Contains("国Ag端口", theme);
        Assert.Contains("TextFormatFlags.GlyphOverhangPadding", theme);
        Assert.Contains("TextFormatFlags.GlyphOverhangPadding", button);
        Assert.DoesNotContain("TextFormatFlags.NoPadding", button);
    }

    [Fact]
    public void V0161_RefinedDetailsUseExplicitSpacerRows()
    {
        var source = ReadRemoteForm();
        var theme = ReadRemoteTheme();

        Assert.Contains("RowCount = 7", source);
        Assert.Contains("new RowStyle(SizeType.Absolute, 222)", source);
        Assert.Contains("new RowStyle(SizeType.Absolute, 106)", source);
        Assert.Contains("new RowStyle(SizeType.Absolute, 92)", source);
        Assert.Contains("new RowStyle(SizeType.Absolute, RemoteDashboardTheme.SectionGap)", source);
        Assert.Contains("new RowStyle(SizeType.Percent, 100)", source);
        Assert.DoesNotContain("BuildPresentationDetails()\n    {\n        var scroll", source);
        Assert.Contains("public const int CardGap = 14", theme);
        Assert.Contains("public const int SectionGap = 12", theme);
        Assert.Contains("public const int ControlGap = 10", theme);
    }

    [Fact]
    public void V0161_BordersAreInsetAndPlaybackTextStaysComplete()
    {
        var source = ReadRemoteForm();
        var theme = ReadRemoteTheme();
        var button = ReadRemoteButton();

        Assert.Contains("new RectangleF(", theme);
        Assert.Contains("0.5F", theme);
        Assert.Contains("PenAlignment.Inset", theme);
        Assert.Contains("new RectangleF(", button);
        Assert.Contains("PenAlignment.Inset", button);
        Assert.Contains("\"当前页放映\"", source);
        Assert.Contains("button.Padding = new Padding(6, 0, 6, 0)", source);
        Assert.DoesNotContain("ComboBox", source);
        Assert.DoesNotContain("Region =", theme);
    }

    private static string ReadRemoteForm() => File.ReadAllText(Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "..", "..", "..", "..", "..",
        "src", "FlyPPTTimer", "Forms", "RemoteControlForm.cs")));

    private static string ReadRemoteTheme() => File.ReadAllText(Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "..", "..", "..", "..", "..",
        "src", "FlyPPTTimer", "Forms", "RemoteDashboardTheme.cs")));

    private static string ReadRemoteRow() => File.ReadAllText(Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "..", "..", "..", "..", "..",
        "src", "FlyPPTTimer", "Forms", "RemotePresentationRow.cs")));

    private static string ReadRemoteButton() => File.ReadAllText(Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "..", "..", "..", "..", "..",
        "src", "FlyPPTTimer", "Forms", "RemoteTextButton.cs")));

    private static string ReadAddressSelector() => File.ReadAllText(Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "..", "..", "..", "..", "..",
        "src", "FlyPPTTimer", "Forms", "RemoteAddressSelector.cs")));
}
