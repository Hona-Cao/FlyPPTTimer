using FlyPPTTimer.Models;
using FlyPPTTimer.Services;

namespace FlyPPTTimer.Tests;

public sealed class V019FeatureTests
{
    [Fact]
    public void MobileDurationCanSynchronizeEveryRuleOrKeepExistingRuleDurations()
    {
        var log = TestLog.Create();
        var config = new AppConfig
        {
            Rules =
            [
                new FileRule { FilePath = "a.pptx", Duration = "00:03:00" },
                new FileRule { FilePath = "b.pptx", Duration = "00:05:00" }
            ]
        };
        var timer = new TimerService(log);
        timer.Configure(config);
        var commands = new AppCommandService(
            timer, new AlertService(log, new FakePlaybackEngine()), () => config, saved => config = saved,
            () => { }, () => { }, () => { }, () => true, _ => { }, () => { }, log);

        Assert.Equal(2, commands.GetRemoteState().TimerState.RuleCount);
        Assert.True(commands.ExecuteRemoteCommand(new RemoteCommand
        {
            Command = "timer.setDuration", DurationMs = 600_000, SyncAllRules = false
        }));
        Assert.Equal(["00:03:00", "00:05:00"], config.Rules.Select(x => x.Duration));

        Assert.True(commands.ExecuteRemoteCommand(new RemoteCommand
        {
            Command = "timer.setDuration", DurationMs = 720_000, SyncAllRules = true
        }));
        Assert.All(config.Rules, rule => Assert.Equal("00:12:00", rule.Duration));
    }

    [Fact]
    public void MobileAssetsSupportSwipeAndUpdatedPresentationActions()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "FlyPPTTimer", "Web"));
        var script = File.ReadAllText(Path.Combine(root, "app.js"));
        var markup = File.ReadAllText(Path.Combine(root, "index.html"));

        Assert.Contains("touchstart", script);
        Assert.Contains("touchend", script);
        Assert.Contains("syncAllRules", script);
        Assert.Contains("openPresentationCount", script);
        Assert.Contains("关闭最后打开的文稿", markup);
        Assert.Contains("退出演示软件", markup);
        Assert.DoesNotContain("ppt.exitApplication", markup);
    }

    [Fact]
    public void PowerPointCloseUsesLastOpenDocumentAndSuppressesSavePrompt()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var source = File.ReadAllText(Path.Combine(root, "src", "FlyPPTTimer", "Services", "PowerPointControlService.cs"));

        Assert.Contains("presentation = ((dynamic)presentations)[count]", source);
        Assert.Contains("((dynamic)presentation).Saved = true", source);
        Assert.DoesNotContain("当前文稿不是由 FlyPPTTimer 打开的", source);
        Assert.DoesNotContain("\"ppt.exitApplication\" =>", source);
    }

    [Fact]
    public void ProjectInformationIncludesBothRepositoriesAndEmployer()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var source = File.ReadAllText(Path.Combine(root, "src", "FlyPPTTimer", "Forms", "SettingsForm.cs"));

        Assert.Contains("江苏省人民医院宿迁医院", source);
        Assert.Contains("https://github.com/Hona-Cao/FlyPPTTimer", source);
        Assert.Contains("https://gitee.com/hona-cao/fly-ppttimer", source);
    }

    private sealed class FakePlaybackEngine : IAlertPlaybackEngine
    {
        public void Enqueue(AlertPlaybackRequest request) { }
        public void Dispose() { }
    }
}
