using FlyPPTTimer.Models;
using FlyPPTTimer.Services;

namespace FlyPPTTimer.Tests;

public sealed class AppCommandServiceTests
{
    [Fact]
    public void MobileTimerCommands_SaveDurationAndModeAndExposeOvertimeState()
    {
        var log = TestLog.Create();
        var config = new AppConfig();
        var timer = new TimerService(log);
        timer.Configure(config);
        var saveCount = 0;
        var commands = new AppCommandService(
            timer,
            new AlertService(log),
            () => config,
            saved => { config = saved; saveCount++; },
            () => { },
            () => { },
            () => { },
            () => true,
            _ => { },
            () => { },
            log);

        Assert.True(commands.ExecuteRemoteCommand(new RemoteCommand
        {
            Command = "timer.setDuration",
            DurationMs = 1000
        }));
        Assert.True(commands.ExecuteRemoteCommand(new RemoteCommand
        {
            Command = "timer.setMode",
            Mode = "countdown"
        }));

        Assert.Equal("00:00:01", config.Timer.DefaultDuration);
        Assert.Equal(TimerMode.Countdown, config.Timer.Mode);
        Assert.Equal(2, saveCount);

        timer.SetDuration(TimeSpan.FromMilliseconds(30));
        commands.Start();
        Thread.Sleep(60);
        timer.ProcessTickForTest();
        var state = commands.GetRemoteState().TimerState;

        Assert.True(state.Running);
        Assert.True(state.IsOvertime);
        Assert.True(state.ContinueOvertime);
    }

    [Fact]
    public void MobileMuteCommand_ControlsComputerAudioAndReportsCurrentState()
    {
        var log = TestLog.Create();
        var config = new AppConfig();
        var timer = new TimerService(log);
        timer.Configure(config);
        var audio = new FakeSystemAudioService();
        var commands = new AppCommandService(
            timer, new AlertService(log, new FakePlaybackEngine()), () => config, _ => { },
            () => { }, () => { }, () => { }, () => true, _ => { }, () => { }, log, audio);

        Assert.True(commands.ExecuteRemoteCommand(new RemoteCommand { Command = "mute.toggle" }));
        Assert.True(audio.IsMuted);
        Assert.True(commands.GetRemoteState().Muted);
        Assert.Equal("电脑已静音", commands.LastCommandMessage);

        commands.ExecuteRemoteCommand(new RemoteCommand { Command = "mute.toggle" });
        Assert.False(audio.IsMuted);
        Assert.Equal("电脑声音已恢复", commands.LastCommandMessage);
    }

    [Fact]
    public void MobileTimerSettings_UpdateSelectedPresentationRule()
    {
        var log = TestLog.Create();
        var path = Path.Combine(Path.GetTempPath(), "v0186-rule.pptx");
        var config = new AppConfig
        {
            Rules = [new FileRule { FilePath = path, Duration = "00:08:00", Mode = TimerMode.Countdown }]
        };
        var timer = new TimerService(log);
        timer.Configure(config);
        var commands = new AppCommandService(
            timer, new AlertService(log, new FakePlaybackEngine()), () => config, saved => config = saved,
            () => { }, () => { }, () => { }, () => true, _ => { }, () => { }, log);
        var id = PresentationRuleValidator.IdForPath(path);

        Assert.True(commands.ExecuteRemoteCommand(new RemoteCommand
        {
            Command = "timer.setMode", Mode = "countup", PresentationId = id
        }));
        Assert.True(commands.ExecuteRemoteCommand(new RemoteCommand
        {
            Command = "timer.setDuration", DurationMs = 125000, PresentationId = id
        }));

        Assert.Equal(TimerMode.CountUp, config.Rules[0].Mode);
        Assert.Equal("00:02:05", config.Rules[0].Duration);
    }

    [Fact]
    public void MobileCanDismissTimeUpBlackoutAndObserveItsState()
    {
        var log = TestLog.Create();
        var config = new AppConfig();
        var timer = new TimerService(log);
        timer.Configure(config);
        var active = true;
        var dismissCount = 0;
        var commands = new AppCommandService(
            timer, new AlertService(log, new FakePlaybackEngine()), () => config, _ => { },
            () => { }, () => { }, () => { }, () => true, _ => { }, () => { }, log, null,
            () => active, () => { active = false; dismissCount++; });

        Assert.True(commands.GetRemoteState().TimerState.TimeUpBlackoutActive);
        Assert.True(commands.ExecuteRemoteCommand(new RemoteCommand { Command = "timeup.dismiss" }));
        Assert.Equal(1, dismissCount);
        Assert.False(commands.GetRemoteState().TimeUpBlackoutActive);
    }

    private sealed class FakeSystemAudioService : ISystemAudioService
    {
        public bool IsMuted { get; private set; }
        public bool ToggleMute() => IsMuted = !IsMuted;
    }

    private sealed class FakePlaybackEngine : IAlertPlaybackEngine
    {
        public void Enqueue(AlertPlaybackRequest request) { }
        public void Dispose() { }
    }
}
