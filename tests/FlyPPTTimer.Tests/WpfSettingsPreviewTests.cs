using FlyPPTTimer.Desktop.ViewModels;
using FlyPPTTimer.Models;
using FlyPPTTimer.Services;
using System.Diagnostics;

namespace FlyPPTTimer.Tests;

public sealed class WpfSettingsPreviewTests
{
    [Fact]
    public void ViewModelLoadsEveryPreviewFieldFromConfiguration()
    {
        using var environment = new TestEnvironment();
        var config = CreateDistinctConfig();

        var viewModel = new SettingsViewModel(config, environment.ConfigService);

        Assert.Equal("01:02:03", viewModel.DefaultDuration);
        Assert.Equal(TimerMode.CountUp, viewModel.SelectedTimerMode);
        Assert.False(viewModel.ContinueOvertime);
        Assert.Equal(TimerEndAction.ExitSlideShow, viewModel.SelectedEndAction);
        Assert.False(viewModel.AutoStartOnFullscreen);
        Assert.False(viewModel.StopWhenLeavingFullscreen);
        Assert.False(viewModel.ResetWhenLeavingFullscreen);
        Assert.False(viewModel.FlashOnPauseResume);
        Assert.True(viewModel.FlashPausedTime);
        Assert.Equal("26.5", viewModel.FontSizeText);
        Assert.Equal("640", viewModel.WidthText);
        Assert.Equal("240", viewModel.HeightText);
        Assert.Equal("72", viewModel.BackgroundOpacityText);
        Assert.Equal("83", viewModel.TextOpacityText);
        Assert.False(viewModel.AlwaysOnTop);
        Assert.False(viewModel.Borderless);
        Assert.Equal("Ctrl+F3", viewModel.StartPauseHotkey);
        Assert.Equal("Ctrl+F4", viewModel.StopResetHotkey);
        Assert.Equal("Ctrl+F5", viewModel.ToggleWindowHotkey);
        Assert.False(viewModel.MinimizeToTray);
        Assert.True(viewModel.CheckOnStartup);
        Assert.False(viewModel.IsDirty);
    }

    [Fact]
    public void SaveUpdatesPreviewFieldsAndPreservesUnexposedConfiguration()
    {
        using var environment = new TestEnvironment();
        var config = new AppConfig();
        config.RemoteControl.Token = "preserve-this-token";
        config.Rules =
        [
            new FileRule { FileName = "deck.pptx", FilePath = "C:\\Slides\\deck.pptx", Duration = "00:12:00" }
        ];
        config.Behavior.Prompt1.Text = "保留未暴露提示";
        var viewModel = new SettingsViewModel(config, environment.ConfigService)
        {
            DefaultDuration = "00:15:30",
            SelectedTimerMode = TimerMode.CountUp,
            ContinueOvertime = false,
            SelectedEndAction = TimerEndAction.BlackScreen,
            AutoStartOnFullscreen = false,
            StopWhenLeavingFullscreen = false,
            ResetWhenLeavingFullscreen = false,
            FlashOnPauseResume = false,
            FlashPausedTime = true,
            FontSizeText = "28.5",
            WidthText = "720",
            HeightText = "260",
            BackgroundOpacityText = "65",
            TextOpacityText = "90",
            AlwaysOnTop = false,
            Borderless = false,
            StartPauseHotkey = "F6",
            StopResetHotkey = "F7",
            ToggleWindowHotkey = "F8",
            MinimizeToTray = false,
            CheckOnStartup = true
        };

        Assert.True(viewModel.TrySave(), viewModel.ErrorMessage);
        var saved = environment.ConfigService.Load();

        Assert.Equal("00:15:30", saved.Timer.DefaultDuration);
        Assert.Equal(TimerMode.CountUp, saved.Timer.Mode);
        Assert.False(saved.Timer.ContinueOvertime);
        Assert.Equal(TimerEndAction.BlackScreen, saved.Timer.EndAction);
        Assert.False(saved.Behavior.AutoStartOnFullscreen);
        Assert.False(saved.Behavior.StopWhenLeavingFullscreen);
        Assert.False(saved.Behavior.ResetWhenLeavingFullscreen);
        Assert.False(saved.Behavior.FlashOnPauseResume);
        Assert.True(saved.Behavior.FlashPausedTime);
        Assert.Equal(28.5F, saved.Appearance.FontSize);
        Assert.Equal(720, saved.Appearance.Width);
        Assert.Equal(260, saved.Appearance.Height);
        Assert.Equal(65, saved.Appearance.BackgroundOpacity);
        Assert.Equal(90, saved.Appearance.TextOpacity);
        Assert.False(saved.Appearance.AlwaysOnTop);
        Assert.False(saved.Appearance.Borderless);
        Assert.Equal("F6", saved.Controls.StartPauseHotkey);
        Assert.Equal("F7", saved.Controls.StopResetHotkey);
        Assert.Equal("F8", saved.Controls.ToggleWindowHotkey);
        Assert.False(saved.Controls.MinimizeToTray);
        Assert.True(saved.Update.CheckOnStartup);
        Assert.Equal("preserve-this-token", saved.RemoteControl.Token);
        var rule = Assert.Single(saved.Rules);
        Assert.Equal("C:\\Slides\\deck.pptx", rule.FilePath);
        Assert.Equal("保留未暴露提示", saved.Behavior.Prompt1.Text);
        Assert.False(viewModel.IsDirty);
    }

    [Theory]
    [InlineData("duration", "not-a-duration")]
    [InlineData("duration", "00:00:00")]
    [InlineData("font", "7")]
    [InlineData("font", "97")]
    [InlineData("width", "39")]
    [InlineData("width", "2001")]
    [InlineData("height", "19")]
    [InlineData("height", "1001")]
    [InlineData("background", "-1")]
    [InlineData("background", "101")]
    [InlineData("text", "-1")]
    [InlineData("text", "101")]
    public void InvalidDurationOrOutOfRangeNumberCannotSave(string field, string value)
    {
        using var environment = new TestEnvironment();
        var viewModel = new SettingsViewModel(new AppConfig(), environment.ConfigService);
        switch (field)
        {
            case "duration": viewModel.DefaultDuration = value; break;
            case "font": viewModel.FontSizeText = value; break;
            case "width": viewModel.WidthText = value; break;
            case "height": viewModel.HeightText = value; break;
            case "background": viewModel.BackgroundOpacityText = value; break;
            case "text": viewModel.TextOpacityText = value; break;
        }

        Assert.False(viewModel.TrySave());
        Assert.NotEmpty(viewModel.ErrorMessage);
        Assert.False(File.Exists(environment.ConfigPath));
    }

    [Fact]
    public void ConsecutiveChangesHaveBoundedSynchronousNotifications()
    {
        using var environment = new TestEnvironment();
        var viewModel = new SettingsViewModel(new AppConfig(), environment.ConfigService);
        var notifications = new List<string?>();
        viewModel.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);
        var stopwatch = Stopwatch.StartNew();

        viewModel.DefaultDuration = "00:09:30";
        viewModel.SelectedTimerMode = TimerMode.CountUp;
        viewModel.ContinueOvertime = !viewModel.ContinueOvertime;
        viewModel.WidthText = "680";
        viewModel.HeightText = "260";
        stopwatch.Stop();

        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(3), $"Changes took {stopwatch.Elapsed}.");
        Assert.True(viewModel.IsDirty);
        Assert.Equal("有未保存的更改", viewModel.UnsavedStatus);
        Assert.InRange(notifications.Count, 7, 9);
        Assert.Equal(1, notifications.Count(name => name == nameof(SettingsViewModel.IsDirty)));
        Assert.Equal(1, notifications.Count(name => name == nameof(SettingsViewModel.UnsavedStatus)));
        Assert.Equal(1, notifications.Count(name => name == nameof(SettingsViewModel.DefaultDuration)));
        Assert.Equal(1, notifications.Count(name => name == nameof(SettingsViewModel.SelectedTimerMode)));
        Assert.Equal(1, notifications.Count(name => name == nameof(SettingsViewModel.ContinueOvertime)));
        Assert.Equal(1, notifications.Count(name => name == nameof(SettingsViewModel.WidthText)));
        Assert.Equal(1, notifications.Count(name => name == nameof(SettingsViewModel.HeightText)));
    }

    [Fact]
    public void MainApplicationKeepsClassicSettingsAndReloadsAfterWpfExit()
    {
        var source = File.ReadAllText(SourcePath("src", "FlyPPTTimer", "FlyPPTTimerContext.cs"));

        Assert.Contains("Path.Combine(AppContext.BaseDirectory, \"FlyPPTTimer.Settings.exe\")", source);
        Assert.Contains("private void ShowClassicSettings()", source);
        Assert.Contains("设置（WPF 预览）", source);
        Assert.Contains("经典设置", source);
        Assert.Contains("ActivateWpfSettings(_wpfSettingsProcess)", source);
        Assert.Contains("RunOnUi(() =>", source);
        Assert.Contains("var reloadedConfig = _configService.Load();", source);
        Assert.Contains("ApplyConfig(reloadedConfig);", source);
        Assert.Contains("falling back to classic settings", source);
        Assert.Contains("ShowClassicSettings();", source);
    }

    [Fact]
    public void CiArtifactContractPublishesAndVerifiesBothExecutables()
    {
        var project = File.ReadAllText(SourcePath("src", "FlyPPTTimer.Desktop", "FlyPPTTimer.Desktop.csproj"));
        var workflow = File.ReadAllText(SourcePath(".github", "workflows", "windows-ci.yml"));
        var interactionScript = File.ReadAllText(
            SourcePath("tests", "FlyPPTTimer.Tests", "WpfSettingsInteractionSmoke.ps1"));

        Assert.Contains("<AssemblyName>FlyPPTTimer.Settings</AssemblyName>", project);
        Assert.Contains("..\\FlyPPTTimer\\FlyPPTTimer.csproj", project);
        Assert.Contains("FlyPPTTimer.Settings.exe", workflow);
        Assert.Contains("Test-Path artifacts/publish/FlyPPTTimer.exe", workflow);
        Assert.Contains("Test-Path artifacts/publish/FlyPPTTimer.Settings.exe", workflow);
        Assert.Contains("Get-FileHash", workflow);
        Assert.Contains("WpfSettingsInteractionSmoke.ps1", workflow);
        Assert.Contains("[int]$LaunchTimeoutSeconds = 20", interactionScript);
        Assert.Contains("[int]$OperationTimeoutSeconds = 3", interactionScript);
        Assert.Contains("AutomationElement]::ProcessIdProperty", interactionScript);
        Assert.Contains("AutomationElement]::RootElement.FindFirst", interactionScript);
        Assert.Contains("Settings window startup:", interactionScript);
        Assert.Contains("-LaunchTimeoutSeconds 20 -OperationTimeoutSeconds 3", workflow);
    }

    private static AppConfig CreateDistinctConfig()
    {
        var config = new AppConfig();
        config.Timer.DefaultDuration = "01:02:03";
        config.Timer.Mode = TimerMode.CountUp;
        config.Timer.ContinueOvertime = false;
        config.Timer.EndAction = TimerEndAction.ExitSlideShow;
        config.Behavior.AutoStartOnFullscreen = false;
        config.Behavior.StopWhenLeavingFullscreen = false;
        config.Behavior.ResetWhenLeavingFullscreen = false;
        config.Behavior.FlashOnPauseResume = false;
        config.Behavior.FlashPausedTime = true;
        config.Appearance.FontSize = 26.5F;
        config.Appearance.Width = 640;
        config.Appearance.Height = 240;
        config.Appearance.BackgroundOpacity = 72;
        config.Appearance.TextOpacity = 83;
        config.Appearance.AlwaysOnTop = false;
        config.Appearance.Borderless = false;
        config.Controls.StartPauseHotkey = "Ctrl+F3";
        config.Controls.StopResetHotkey = "Ctrl+F4";
        config.Controls.ToggleWindowHotkey = "Ctrl+F5";
        config.Controls.MinimizeToTray = false;
        config.Update.CheckOnStartup = true;
        return config;
    }

    private static string SourcePath(params string[] segments)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            var path = Path.Combine([directory.FullName, .. segments]);
            if (File.Exists(path)) return path;
        }

        throw new DirectoryNotFoundException("Unable to locate the repository source files.");
    }

    private sealed class TestEnvironment : IDisposable
    {
        private readonly string _directory = Path.Combine(Path.GetTempPath(), $"FlyPPTTimer-WpfSettings-{Guid.NewGuid():N}");

        public TestEnvironment()
        {
            Directory.CreateDirectory(_directory);
            ConfigPath = Path.Combine(_directory, "FlyPPTTimer.config.json");
            ConfigService = new ConfigService(new LogService(Path.Combine(_directory, "logs")), ConfigPath);
        }

        public string ConfigPath { get; }
        public ConfigService ConfigService { get; }

        public void Dispose()
        {
            if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
        }
    }
}
