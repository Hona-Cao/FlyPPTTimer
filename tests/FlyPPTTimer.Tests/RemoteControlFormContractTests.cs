namespace FlyPPTTimer.Tests;

public sealed class RemoteControlFormContractTests
{
    [Fact]
    public void FlatAddressMenu_IsOwnedUntilTheControlIsDisposed()
    {
        var source = ReadRemoteForm();
        Assert.Contains("private ContextMenuStrip? _menu;", source);
        Assert.Contains("private void RebuildMenuItems", source);
        Assert.Contains("if (disposing) _menu?.Dispose();", source);
        Assert.DoesNotContain("menu.Closed +=", source);
        Assert.DoesNotContain("_menu?.Dispose();\n        var menu", source);
    }

    [Fact]
    public void PcRemoteControl_ContainsPresentationManagementSurface()
    {
        var source = ReadRemoteForm();
        foreach (var member in new[] { "AddPresentationRules", "DeleteSelectedRule", "SaveRulesImmediately", "ppt.openPresentation", "ppt.startFromBeginning", "ppt.endShow", "ppt.closeCurrentPresentation", "ppt.exitApplication", "ppt.forceQuitAll" })
            Assert.Contains(member, source);
    }

    [Fact]
    public void PresentationLayout_UsesScrollableCardsAndRedactsVisibleTokens()
    {
        var source = ReadRemoteForm();
        Assert.Contains("AutoScroll = true", source);
        Assert.Contains("RemoteUrlPrivacy.MaskToken(url)", source);
        Assert.Contains("更多操作", source);
        Assert.Contains("强制退出 PowerPoint/WPS", source);
        Assert.Contains("UseCompatibleTextRendering = false", source);
    }

    [Fact]
    public void PresentationActivation_UsesComAndWindowApisWithoutInputSimulation()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "FlyPPTTimer"));
        var service = File.ReadAllText(Path.Combine(root, "Services", "PowerPointControlService.cs"));
        Assert.Contains("ActivatePresentationWindow", service);
        Assert.Contains("ActivateSlideShowWindow", service);
        Assert.Contains("FindSlideShowWindow", service);
        Assert.DoesNotContain("SendKeys", service);
        Assert.DoesNotContain("Alt+Tab", service);
    }

    private static string ReadRemoteForm()
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "FlyPPTTimer", "Forms", "RemoteControlForm.cs"));
        return File.ReadAllText(path);
    }
}
