using FlyPPTTimer;
using FlyPPTTimer.Models;

namespace FlyPPTTimer.Tests;

public sealed class VersionAndPresentationContractTests
{
    [Fact]
    public void DefaultModels_UseCurrentAppVersion()
    {
        Assert.Equal(AppVersion.Current, new AppConfig().Version);
        Assert.Equal(AppVersion.Current, new RemoteState().Version);
        Assert.False(string.IsNullOrWhiteSpace(AppVersion.Current));
    }

    [Fact]
    public void PresentationState_ExposesOperationAndWpsCapabilities()
    {
        var state = new PresentationState();
        Assert.Equal("Idle", state.Operation);
        Assert.False(state.IsOperationBusy);
        Assert.False(state.WpsCapabilities.CanClosePresentation);
    }

    [Fact]
    public void V0185_SettingsRuleRowRemovesRedundantRuleStatus()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var row = File.ReadAllText(Path.Combine(root, "src", "FlyPPTTimer", "Forms", "PresentationRuleRow.cs"));
        Assert.DoesNotContain("规则已启用", row);
    }
}
