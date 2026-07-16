namespace FlyPPTTimer.Tests;

public sealed class RemoteControlFormContractTests
{
    [Fact]
    public void RemoteControl_IsASeparateDashboardWithoutChangingSharedTheme()
    {
        var source = ReadRemoteForm();
        var theme = ReadRemoteTheme();

        Assert.Contains("BuildSidebar", source);
        Assert.Contains("BuildConnectionPage", source);
        Assert.Contains("BuildPresentationWorkspace", source);
        Assert.Contains("RemoteDashboardTheme", source);
        Assert.Contains("internal static class RemoteDashboardTheme", theme);
        Assert.DoesNotContain("ModernTheme.StandardControlHeight", source);
    }

    [Fact]
    public void RemoteConnection_PreservesAllExistingActions()
    {
        var source = ReadRemoteForm();

        foreach (var member in new[]
        {
            "RestartService",
            "ToggleService",
            "CurrentUrl",
            "BuildFirewallCommand",
            "复制链接",
            "在本机浏览器打开",
            "复制放行命令",
            "RemoteUrlPrivacy.MaskToken(url)"
        })
        {
            Assert.Contains(member, source);
        }
    }

    [Fact]
    public void PresentationWorkspace_UsesMasterDetailInsteadOfOneLongPage()
    {
        var source = ReadRemoteForm();

        Assert.Contains("BuildPresentationListCard", source);
        Assert.Contains("BuildPresentationDetails", source);
        Assert.Contains("PresentationRuleCard", source);
        Assert.Contains("AutoScroll = true", source);
        Assert.DoesNotContain("CollapsibleHeader", source);
        Assert.DoesNotContain("RoutePresentationWheel", source);
        Assert.DoesNotContain("ReflowPresentationLayout", source);
    }

    [Fact]
    public void PresentationRows_ReadCurrentDataAfterRefresh()
    {
        var source = ReadRemoteForm();

        Assert.Contains("row.CurrentRule", source);
        Assert.Contains("row.CurrentPresentation", source);
        Assert.Contains("row.CurrentPath", source);
        Assert.Contains("CurrentRule = rule", source);
        Assert.Contains("CurrentPresentation = option", source);
    }

    [Fact]
    public void PresentationWorkspace_PreservesManagementAndPlaybackCommands()
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
    public void DangerousExit_UsesCustomConfirmationAndDoesNotDefaultToConfirm()
    {
        var source = ReadRemoteForm();

        Assert.Contains("RemoteConfirmDialog", source);
        Assert.Contains("CancelButton = cancel", source);
        Assert.Contains("ActiveControl = cancel", source);
        Assert.DoesNotContain("AcceptButton = confirm", source);
    }

    [Fact]
    public void AddressSelector_IsKeyboardAccessibleAndOwnedUntilDisposed()
    {
        var source = ReadRemoteForm();

        Assert.Contains("AccessibleRole = AccessibleRole.ComboBox", source);
        Assert.Contains("Keys.Alt | Keys.Down", source);
        Assert.Contains("if (disposing) _menu?.Dispose()", source);
        Assert.DoesNotContain("menu.Closed +=", source);
    }

    private static string ReadRemoteForm()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "FlyPPTTimer", "Forms", "RemoteControlForm.cs"));
        return File.ReadAllText(path);
    }

    private static string ReadRemoteTheme()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "FlyPPTTimer", "Forms", "RemoteDashboardTheme.cs"));
        return File.ReadAllText(path);
    }
}
