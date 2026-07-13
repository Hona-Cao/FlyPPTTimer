using System.Drawing;

namespace FlyPPTTimer.Models;

public sealed class AppConfig
{
    public string Version { get; set; } = "0.11.0";
    public TimerSettings Timer { get; set; } = new();
    public BehaviorSettings Behavior { get; set; } = new();
    public AppearanceSettings Appearance { get; set; } = new();
    public ControlSettings Controls { get; set; } = new();
    public RemoteControlSettings RemoteControl { get; set; } = new();
    public WindowPlacement Placement { get; set; } = new();
    public List<FileRule> Rules { get; set; } = [];
}

public sealed class TimerSettings
{
    public string DefaultDuration { get; set; } = "00:08:00";
    public TimerMode Mode { get; set; } = TimerMode.Countdown;
    public bool EnablePerSlideTimer { get; set; }
    public bool ContinueOvertime { get; set; } = true;
}

public enum TimerMode
{
    Countdown,
    CountUp
}

public sealed class BehaviorSettings
{
    public bool AutoStartOnFullscreen { get; set; } = true;
    public bool StopWhenLeavingFullscreen { get; set; } = true;
    public bool ResetWhenLeavingFullscreen { get; set; } = true;
    public bool FlashOnPauseResume { get; set; } = true;
    public bool FlashPausedTime { get; set; } = false;
    public PromptSettings Prompt1 { get; set; } = new() { Enabled = true, TriggerBeforeEndSeconds = 120, Text = "还剩 {remaining}", FlashText = true };
    public PromptSettings Prompt2 { get; set; } = new() { Enabled = false, TriggerBeforeEndSeconds = 30, Text = "即将结束 {remaining}", FlashBackground = true };
    public EndPromptSettings EndPrompt { get; set; } = new();
    public string[] FullscreenProcessWhitelist { get; set; } =
    [
        "POWERPNT.EXE",
        "WPSOffice.exe",
        "wpp.exe",
        "Acrobat.exe",
        "AcroRd32.exe",
        "chrome.exe",
        "msedge.exe"
    ];
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
    public bool PlaySound { get; set; }
    public string SoundFile { get; set; } = "";
}

public sealed class EndPromptSettings : PromptSettings
{
    public EndPromptSettings()
    {
        Enabled = true;
        Text = "计时结束";
        Speak = true;
        Beep = true;
        FlashBackground = true;
    }

    public int FlashSeconds { get; set; } = 8;
}

public sealed class AppearanceSettings
{
    public string ColorScheme { get; set; } = "默认";
    public string FontFamily { get; set; } = "Microsoft YaHei UI";
    public float FontSize { get; set; } = 20;
    public string FontStyle { get; set; } = "Bold";
    public string TextColor { get; set; } = "#FFFFFF";
    public string BackgroundColor { get; set; } = "#202733";
    public string TimeoutTextColor { get; set; } = "#FFFFFF";
    public string TimeoutBackgroundColor { get; set; } = "#B00020";
    public string FlashBackgroundColor { get; set; } = "#FFC107";
    public int Width { get; set; } = 200;
    public int Height { get; set; } = 60;
    public int BackgroundOpacity { get; set; } = 88;
    public int TextOpacity { get; set; } = 100;
    public string Shape { get; set; } = "圆角矩形（小）";
    public string FlashStyle { get; set; } = "闪烁背景";
    public int FlashOnMs { get; set; } = 350;
    public int FlashOffMs { get; set; } = 350;
    public string OvertimePrefix { get; set; } = "-";
    public bool Borderless { get; set; } = true;
    public bool AlwaysOnTop { get; set; } = true;
}

public sealed class ControlSettings
{
    public string StartPauseHotkey { get; set; } = "F3";
    public string StopResetHotkey { get; set; } = "F4";
    public string ToggleWindowHotkey { get; set; } = "F5";
    public string OpenSettingsHotkey { get; set; } = "F6";
    public Dictionary<string, string> Hotkeys { get; set; } = DefaultHotkeys();
    public bool ClickThrough { get; set; } = false;
    public bool LockPosition { get; set; } = false;
    public bool MinimizeToTray { get; set; } = true;
    public CloseButtonBehavior CloseButtonBehavior { get; set; } = CloseButtonBehavior.MinimizeToTray;

    public static Dictionary<string, string> DefaultHotkeys() => new()
    {
        ["startPause"] = "F3",
        ["start"] = "",
        ["pause"] = "",
        ["resume"] = "",
        ["stopReset"] = "F4",
        ["stop"] = "",
        ["reset"] = "",
        ["toggleWindow"] = "F5",
        ["showWindow"] = "",
        ["hideWindow"] = "",
        ["flash"] = "F7",
        ["toggleMute"] = "F8",
        ["toggleMode"] = "",
        ["addMinute"] = "Ctrl+Alt+Up",
        ["subtractMinute"] = "Ctrl+Alt+Down",
        ["preset3"] = "Ctrl+Alt+1",
        ["preset5"] = "Ctrl+Alt+2",
        ["preset8"] = "Ctrl+Alt+3",
        ["preset10"] = "Ctrl+Alt+4",
        ["preset15"] = "Ctrl+Alt+5",
        ["openSettings"] = "F6"
    };
}

public enum CloseButtonBehavior
{
    Exit,
    MinimizeToTray
}

public sealed class WindowPlacement
{
    public bool Visible { get; set; } = true;
    public bool ShowOnAllScreens { get; set; } = true;
    public string TargetScreenDeviceName { get; set; } = "";
    public OverlayAnchor Anchor { get; set; } = OverlayAnchor.TopCenter;
    public decimal OffsetXPercent { get; set; }
    public decimal OffsetYPercent { get; set; } = 0.5m;
    public int X { get; set; } = 80;
    public int Y { get; set; } = 80;
    public string ScreenDeviceName { get; set; } = "";
    public bool HasCustomPlacement { get; set; }
}

public enum OverlayAnchor
{
    TopLeft,
    TopCenter,
    TopRight,
    MiddleLeft,
    Center,
    MiddleRight,
    BottomLeft,
    BottomCenter,
    BottomRight
}

public sealed class FileRule
{
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string Duration { get; set; } = "00:08:00";
    public bool Enabled { get; set; } = true;
    public string TitlePattern { get; set; } = "";
    public string Feature { get; set; } = "";
}

public sealed class RemoteControlSettings
{
    public bool Enabled { get; set; } = true;
    public bool UseRandomPort { get; set; } = true;
    public int Port { get; set; }
    public string Token { get; set; } = "";
}

public sealed class RemoteCommand
{
    public string Command { get; set; } = "";
    public string? Duration { get; set; }
    public long? DurationMs { get; set; }
    public string? Mode { get; set; }
    public int? SlideNumber { get; set; }
    public string? PresentationId { get; set; }
}

public sealed class RemoteState
{
    public bool Ok { get; set; } = true;
    public string Message { get; set; } = "";
    public TimerRemoteState TimerState { get; set; } = new();
    public PresentationState PresentationState { get; set; } = new();
    // Flat timer fields remain for v0.10 clients.
    public string Mode { get; set; } = "";
    public string State { get; set; } = "";
    public bool Running { get; set; }
    public long DurationMs { get; set; }
    public long ElapsedMs { get; set; }
    public long RemainingMs { get; set; }
    public string DisplayText { get; set; } = "";
    public bool WindowVisible { get; set; }
    public bool Muted { get; set; }
    public int ConnectedClients { get; set; }
    public string Version { get; set; } = "0.11.0";
}

public sealed class TimerRemoteState
{
    public string Mode { get; set; } = "";
    public string State { get; set; } = "";
    public bool Running { get; set; }
    public long DurationMs { get; set; }
    public long ElapsedMs { get; set; }
    public long RemainingMs { get; set; }
    public string DisplayText { get; set; } = "";
    public bool WindowVisible { get; set; }
    public bool Muted { get; set; }
}

public sealed class PresentationState
{
    public bool PowerPointInstalled { get; set; }
    public bool PowerPointRunning { get; set; }
    public bool HasPresentation { get; set; }
    public bool IsSlideShowRunning { get; set; }
    public string PresentationName { get; set; } = "";
    public string PresentationPath { get; set; } = "";
    public int CurrentSlide { get; set; }
    public int TotalSlides { get; set; }
    public string ScreenMode { get; set; } = "正常";
    public string Error { get; set; } = "";
    public List<PresentationOption> Presentations { get; set; } = [];
}

public sealed class PresentationOption
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsOpen { get; set; }
    public bool IsActive { get; set; }
}

public sealed record PresentationCommandResult(bool Success, string Message, PresentationState State);
