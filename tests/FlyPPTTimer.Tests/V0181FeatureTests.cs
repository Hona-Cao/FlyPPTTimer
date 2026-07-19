using FlyPPTTimer.Models;
using FlyPPTTimer.Services;

namespace FlyPPTTimer.Tests;

public sealed class V0181FeatureTests
{
    [Fact]
    public void PromptSpeech_UsesRequiredFixedPhrasesInQueueOrder()
    {
        var playback = new RecordingPlaybackEngine();
        using var alerts = new AlertService(TestLog.Create(), playback);
        var config = new AppConfig();
        config.Behavior.Prompt1.Enabled = true;
        config.Behavior.Prompt2.Enabled = true;
        config.Behavior.Prompt1.TriggerBeforeEndSeconds = 120;
        config.Behavior.Prompt2.TriggerBeforeEndSeconds = 120;
        var snapshot = new TimerSnapshot(TimerState.Running, TimerMode.Countdown,
            TimeSpan.FromMinutes(7), TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(8), false);

        alerts.CheckPrompts(config, snapshot);
        alerts.TriggerEnd(config, snapshot);

        Assert.Equal(["时间即将结束", "时间即将结束", "预设时间到"], playback.Requests.Select(x => x.SpeechText));
    }

    [Fact]
    public void SourceContracts_KeepSpeechSynchronousAndPowerPointManagedDeckClean()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var alerts = File.ReadAllText(Path.Combine(root, "src", "FlyPPTTimer", "Services", "AlertService.cs"));
        var ppt = File.ReadAllText(Path.Combine(root, "src", "FlyPPTTimer", "Services", "PowerPointControlService.cs"));
        var settings = File.ReadAllText(Path.Combine(root, "src", "FlyPPTTimer", "Forms", "SettingsForm.cs"));
        Assert.Contains("BlockingCollection<AlertPlaybackRequest>", alerts);
        Assert.Contains("Speak(text, 0)", alerts);
        Assert.Contains("RestoreSlideShowSettings(deck, showSettings", ppt);
        Assert.Contains("!restored || !savedStateKnown || !wasSaved", ppt);
        Assert.DoesNotContain("打开设置快捷键", settings);
        Assert.DoesNotContain("选择并复制 MP3", settings);
        Assert.Contains("黑屏并显示“时间到”", settings);
    }

    [Fact]
    public void ConfigurationDefaultsToNoAutomaticEndAction()
    {
        var config = new AppConfig();
        Assert.Equal(TimerEndAction.None, config.Timer.EndAction);
        Assert.Equal("时间即将结束", config.Behavior.Prompt1.Text);
        Assert.Equal("时间即将结束", config.Behavior.Prompt2.Text);
        Assert.Equal("预设时间到", config.Behavior.EndPrompt.Text);
        Assert.DoesNotContain("openSettings", config.Controls.Hotkeys.Keys);
    }

    private sealed class RecordingPlaybackEngine : IAlertPlaybackEngine
    {
        public List<AlertPlaybackRequest> Requests { get; } = [];
        public void Enqueue(AlertPlaybackRequest request) => Requests.Add(request);
        public void Dispose() { }
    }
}
