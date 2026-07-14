using System.Text.Json.Serialization;

namespace FlyPPTTimer.Core.Models;

public sealed class AppConfig
{
    public string Version { get; set; } = "0.13.0";
    public TimerSettings Timer { get; set; } = new();
    public BehaviorSettings Behavior { get; set; } = new();
    public AppearanceSettings Appearance { get; set; } = new();
    public ControlSettings Controls { get; set; } = new();
    public RemoteControlSettings RemoteControl { get; set; } = new();
    public WindowPlacement Placement { get; set; } = new();
    public ThemeSettings Theme { get; set; } = new();
    public List<FileRule> Rules { get; set; } = [];
}

public sealed class TimerSettings
{
    public string DefaultDuration { get; set; } = "00:08:00";
    public TimerMode Mode { get; set; } = TimerMode.Countdown;
    public bool ContinueOvertime { get; set; } = true;
}

public enum TimerMode { Countdown, CountUp }
public enum TimerState { Stopped, Running, Paused, Finished }

public sealed class BehaviorSettings
{
    public bool AutoStartOnFullscreen { get; set; } = true;
    public bool StopWhenLeavingFullscreen { get; set; } = true;
    public bool ResetWhenLeavingFullscreen { get; set; } = true;
    public bool FlashPausedTime { get; set; }
    public PromptSettings Prompt1 { get; set; } = new() { Enabled = true, TriggerBeforeEndSeconds = 120, Text = "还剩 {remaining}", FlashText = true };
    public PromptSettings Prompt2 { get; set; } = new() { TriggerBeforeEndSeconds = 30, Text = "即将结束 {remaining}", FlashBackground = true };
    public EndPromptSettings EndPrompt { get; set; } = new();
}

public class PromptSettings
{
    public bool Enabled { get; set; }
    public int TriggerBeforeEndSeconds { get; set; }
    public string Text { get; set; } = "";
    public bool Speak { get; set; }
    public bool Beep { get; set; } = true;
    public bool FlashText { get; set; }
    public bool FlashBackground { get; set; }
    public bool FlashBorder { get; set; }
    public string SoundFile { get; set; } = "";
}

public sealed class EndPromptSettings : PromptSettings
{
    public EndPromptSettings() { Enabled = true; Text = "计时结束"; Speak = true; FlashBackground = true; }
    public int FlashSeconds { get; set; } = 8;
}

public sealed class AppearanceSettings
{
    public string FontFamily { get; set; } = "Microsoft YaHei UI";
    public double FontSize { get; set; } = 20;
    public string TextColor { get; set; } = "#FFFFFF";
    public string BackgroundColor { get; set; } = "#202733";
    public string TimeoutTextColor { get; set; } = "#FFFFFF";
    public string TimeoutBackgroundColor { get; set; } = "#B00020";
    public string FlashBackgroundColor { get; set; } = "#FFC107";
    public int Width { get; set; } = 200;
    public int Height { get; set; } = 60;
    public int BackgroundOpacity { get; set; } = 88;
    public int TextOpacity { get; set; } = 100;
    public int FlashOnMs { get; set; } = 350;
    public int FlashOffMs { get; set; } = 350;
    public string OvertimePrefix { get; set; } = "-";
    public bool AlwaysOnTop { get; set; } = true;
}

public sealed class ThemeSettings
{
    public AppTheme Theme { get; set; } = AppTheme.System;
    public bool UseMica { get; set; } = true;
    public bool ReduceMotion { get; set; }
}

public enum AppTheme { System, Light, Dark }

public sealed class ControlSettings
{
    public Dictionary<string, string> Hotkeys { get; set; } = DefaultHotkeys();
    public bool ClickThrough { get; set; }
    public bool LockPosition { get; set; }
    public bool MinimizeToTray { get; set; } = true;
    public static Dictionary<string, string> DefaultHotkeys() => new()
    {
        ["startPause"] = "F3", ["stopReset"] = "F4", ["toggleWindow"] = "F5",
        ["openSettings"] = "F6", ["flash"] = "F7", ["toggleMute"] = "F8"
    };
}

public sealed class WindowPlacement
{
    public bool Visible { get; set; } = true;
    public bool ShowOnAllScreens { get; set; } = true;
    public string TargetScreenDeviceName { get; set; } = "";
    public OverlayAnchor Anchor { get; set; } = OverlayAnchor.TopCenter;
    public decimal OffsetXPercent { get; set; }
    public decimal OffsetYPercent { get; set; } = 0.5m;
}

public enum OverlayAnchor { TopLeft, TopCenter, TopRight, MiddleLeft, Center, MiddleRight, BottomLeft, BottomCenter, BottomRight }

public sealed class FileRule
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string Duration { get; set; } = "00:08:00";
    public bool Enabled { get; set; } = true;
    public int Order { get; set; }
    [JsonIgnore] public bool Exists => File.Exists(FilePath);
    [JsonIgnore] public long FileSize => Exists ? new FileInfo(FilePath).Length : 0;
    [JsonIgnore] public DateTime? LastModified => Exists ? File.GetLastWriteTime(FilePath) : null;
}

public sealed class RemoteControlSettings
{
    public bool Enabled { get; set; } = true;
    public bool UseRandomPort { get; set; } = true;
    public int Port { get; set; }
    public string Token { get; set; } = "";
}
