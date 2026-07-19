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
        Assert.Equal("0.20.0", AppVersion.Current);
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
        Assert.Contains("CheckedChangedByUser", row);
    }

    [Fact]
    public void V017_SettingsExposeOvertimePolicyAndProjectInformation()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var settings = File.ReadAllText(Path.Combine(root, "src", "FlyPPTTimer", "Forms", "SettingsForm.cs"));
        var context = File.ReadAllText(Path.Combine(root, "src", "FlyPPTTimer", "FlyPPTTimerContext.cs"));

        Assert.Contains("继续显示超时", settings);
        Assert.Contains("_config.Timer.ContinueOvertime", settings);
        Assert.Contains("SyncTimerSettings", settings);
        Assert.Contains("caohunan@smail.nju.edu.cn", settings);
        Assert.Contains("https://github.com/Hona-Cao/FlyPPTTimer", settings);
        Assert.Contains("_settings?.SyncTimerSettings(_config.Timer)", context);
    }
}
