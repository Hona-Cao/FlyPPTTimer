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
        Assert.Equal("0.14.2", AppVersion.Current);
    }

    [Fact]
    public void PresentationState_ExposesOperationAndWpsCapabilities()
    {
        var state = new PresentationState();
        Assert.Equal("Idle", state.Operation);
        Assert.False(state.IsOperationBusy);
        Assert.False(state.WpsCapabilities.CanClosePresentation);
    }
}
