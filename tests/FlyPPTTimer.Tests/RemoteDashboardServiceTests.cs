using FlyPPTTimer.Models;
using FlyPPTTimer.Services;

namespace FlyPPTTimer.Tests;

public sealed class RemoteDashboardServiceTests
{
    [Fact]
    public void EndpointValidationRejectsInvalidFixedPortWithoutSaving()
    {
        var (sut, _, saves) = Create();

        Assert.False(sut.TryApplyEndpoint(false, 0, out var error));

        Assert.Equal("端口必须在 1 到 65535 之间。", error);
        Assert.Empty(saves);
    }

    [Fact]
    public void RuleCrudPreservesDefaultsAndRejectsDuplicatePaths()
    {
        var (sut, config, saves) = Create();
        var path = Path.Combine(Path.GetTempPath(), "RemoteDashboard", "deck.pptx");

        Assert.Equal(1, sut.AddRules([path, path.ToUpperInvariant()]));
        Assert.Single(config().Rules);
        Assert.Equal("00:08:00", config().Rules[0].Duration);
        Assert.True(sut.TryUpdateRule(path, "00:12:30", TimerMode.CountUp, false, out var error));
        Assert.Equal("", error);
        Assert.Equal("00:12:30", config().Rules[0].Duration);
        Assert.Equal(TimerMode.CountUp, config().Rules[0].Mode);
        Assert.False(config().Rules[0].Enabled);
        Assert.True(sut.RemoveRule(path));
        Assert.Empty(config().Rules);
        Assert.Equal(3, saves.Count);
    }

    [Fact]
    public void AccessUrlUsesCurrentListenerPortAndKeepsTokenPrivateFromSnapshotMutation()
    {
        var (sut, config, _) = Create();
        config().RemoteControl.Token = "fixture-token";
        config().RemoteControl.Port = 4080;

        var snapshot = sut.GetSnapshot();
        snapshot.Config.RemoteControl.Token = "changed-copy";

        Assert.Equal("fixture-token", config().RemoteControl.Token);
        Assert.Equal("http://192.168.1.8:4080/?token=fixture-token", sut.BuildAccessUrl("192.168.1.8"));
    }

    private static (RemoteDashboardService Service, Func<AppConfig> Config, List<AppConfig> Saves) Create()
    {
        var current = new AppConfig();
        current.RemoteControl.Enabled = false;
        var saves = new List<AppConfig>();
        void Save(AppConfig next)
        {
            current = ConfigService.Clone(next);
            saves.Add(ConfigService.Clone(next));
        }

        var log = TestLog.Create();
        var timer = new TimerService(log);
        timer.Configure(current);
        var commands = new AppCommandService(
            timer, new AlertService(log), () => current, Save,
            () => { }, () => { }, () => { }, () => false, _ => { }, () => { }, log);
        var remote = new RemoteControlService(() => current, Save, commands, null, log);
        var service = new RemoteDashboardService(() => current, Save, remote, new NetworkAddressService());
        return (service, () => current, saves);
    }
}
