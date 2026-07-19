using System.Drawing;
using FlyPPTTimer.Forms;
using FlyPPTTimer.Models;
using FlyPPTTimer.Services;

namespace FlyPPTTimer.Tests;

public sealed class V0182FeatureTests
{
    [Fact]
    public void SelectedSound_IsAutomaticallyEnabledDuringUpgrade()
    {
        var config = new AppConfig { Version = "0.18.1" };
        config.Behavior.Prompt1.SoundFile = @"C:\alerts\prompt1.wav";
        config.Behavior.Prompt1.PlaySound = false;

        ConfigService.Normalize(config);

        Assert.True(config.Behavior.Prompt1.PlaySound);
        Assert.Equal("0.20.0", config.Version);
    }

    [Fact]
    public void EmptySelectedSound_IsDisabledDuringUpgrade()
    {
        var config = new AppConfig { Version = "0.18.1" };
        config.Behavior.EndPrompt.SoundFile = "";
        config.Behavior.EndPrompt.PlaySound = true;

        ConfigService.Normalize(config);

        Assert.False(config.Behavior.EndPrompt.PlaySound);
    }

    [Fact]
    public void PreviousDefaultOverlaySize_MigratesToCurrentDefault()
    {
        var config = new AppConfig { Version = "0.18.1" };
        config.Appearance.Width = 160;
        config.Appearance.Height = 60;

        ConfigService.Normalize(config);

        Assert.Equal(100, config.Appearance.Width);
        Assert.Equal(35, config.Appearance.Height);
    }

    [Fact]
    public void ResizeAroundCenter_KeepsHorizontalAndVerticalCenter()
    {
        var center = new Point(900, 500);
        var area = new Rectangle(0, 0, 1920, 1080);

        var small = TimerOverlayForm.LocationFromCenter(center, new Size(140, 50), area);
        var large = TimerOverlayForm.LocationFromCenter(center, new Size(320, 120), area);

        Assert.Equal(center, new Point(small.X + 70, small.Y + 25));
        Assert.Equal(center, new Point(large.X + 160, large.Y + 60));
    }

    [Fact]
    public void ThreePrompts_HaveIndependentFlashSettings()
    {
        var config = new AppConfig();
        config.Behavior.Prompt1.FlashStyle = "闪烁文字";
        config.Behavior.Prompt2.FlashStyle = "实线边框";
        config.Behavior.EndPrompt.FlashStyle = "边框+背景";

        Assert.Equal("闪烁文字", config.Behavior.Prompt1.FlashStyle);
        Assert.Equal("实线边框", config.Behavior.Prompt2.FlashStyle);
        Assert.Equal("边框+背景", config.Behavior.EndPrompt.FlashStyle);
    }

    [Fact]
    public void SelectedSound_ReplacesDefaultSpeechForThatPrompt()
    {
        var playback = new RecordingPlaybackEngine();
        using var alerts = new AlertService(TestLog.Create(), playback);
        var config = new AppConfig();
        config.Behavior.Prompt1.SoundFile = @"C:\alerts\prompt1.mp3";
        config.Behavior.Prompt1.PlaySound = true;
        config.Behavior.Prompt1.Speak = true;

        alerts.CheckPrompts(config, new TimerSnapshot(TimerState.Running, TimerMode.Countdown,
            TimeSpan.FromMinutes(7), TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(8), false));

        var request = Assert.Single(playback.Requests);
        Assert.Empty(request.SpeechText);
        Assert.Equal(@"C:\alerts\prompt1.mp3", request.SoundFile);
    }

    [Fact]
    public void SettingsText_UsesClearTriggerWordingAndNoSoundEnableCheckboxes()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var settings = File.ReadAllText(Path.Combine(root, "src", "FlyPPTTimer", "Forms", "SettingsForm.cs"));

        Assert.Contains("距离预设时间还剩（秒）", settings);
        Assert.DoesNotContain("提示1启用", settings);
        Assert.DoesNotContain("提示2启用", settings);
        Assert.DoesNotContain("自选提示音\", new CheckBox", settings);
        Assert.DoesNotContain("p1SoundEnabled", settings);
        Assert.DoesNotContain("p2SoundEnabled", settings);
        Assert.DoesNotContain("endSoundEnabled", settings);
        Assert.Contains("恢复默认", settings);
    }

    private sealed class RecordingPlaybackEngine : IAlertPlaybackEngine
    {
        public List<AlertPlaybackRequest> Requests { get; } = [];
        public void Enqueue(AlertPlaybackRequest request) => Requests.Add(request);
        public void Dispose() { }
    }
}
