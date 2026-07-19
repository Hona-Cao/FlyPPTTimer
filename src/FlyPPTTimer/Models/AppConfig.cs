using System.Drawing;

namespace FlyPPTTimer.Models;

public sealed class AppConfig
{
    public string Version { get; set; } = AppVersion.Current;
    public UpdateSettings Update { get; set; } = new();
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
    public TimerEndAction EndAction { get; set; } = TimerEndAction.None;
}

public sealed class UpdateSettings
{
    public bool CheckOnStartup { get; set; }
}

public enum TimerEndAction
{
    None,
    BlackScreen,
    ExitSlideShow
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
    public PromptSettings Prompt1 { get; set; } = new() { Enabled = true, TriggerBeforeEndSeconds = 120, Text = "时间即将结束", Speak = true, Beep = false, FlashBackground = true };
    public PromptSettings Prompt2 { get; set; } = new() { Enabled = false, TriggerBeforeEndSeconds = 30, Text = "时间即将结束", Speak = true, Beep = false, FlashBackground = true };
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
    public bool Beep { get; set; }
    public bool FlashText { get; set; }
    public bool FlashBackground { get; set; }
    public bool PlaySound { get; set; }
    public string SoundFile { get; set; } = "";
    public string FlashStyle { get; set; } = "闪烁背景";
    public int FlashOnMs { get; set; } = 350;
    public int FlashOffMs { get; set; } = 350;
    public int FlashSeconds { get; set; } = 3;
}

public sealed class EndPromptSettings : PromptSettings
{
    public EndPromptSettings()
    {
        Enabled = true;
        Text = "预设时间到";
        Speak = true;
        Beep = false;
        FlashBackground = true;
        FlashSeconds = 8;
    }
}

public sealed class AppearanceSettings
{
    public string ColorScheme { get; set; } = "医疗卫生（蓝白）";
    public string FontFamily { get; set; } = "Microsoft YaHei UI";
    public float FontSize { get; set; } = 18;
    public string FontStyle { get; set; } = "Bold";
    public string TextColor { get; set; } = "#0B3A66";
    public string BackgroundColor { get; set; } = "#F3F8FC";
    public string TimeoutTextColor { get; set; } = "#FFFFFF";
    public string TimeoutBackgroundColor { get; set; } = "#B00020";
    public string FlashBackgroundColor { get; set; } = "#4EA3D8";
    public int Width { get; set; } = 100;
    public int Height { get; set; } = 35;
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
        ["preset15"] = "Ctrl+Alt+5"
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
    public TimerMode Mode { get; set; } = TimerMode.Countdown;
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
    public RemoteWindowPlacement Window { get; set; } = new();
}

public sealed class RemoteWindowPlacement
{
    public bool HasValue { get; set; }
    public string ScreenDeviceName { get; set; } = "";
    public double LeftRatio { get; set; }
    public double TopRatio { get; set; }
    public int WidthDip { get; set; } = 1180;
    public int HeightDip { get; set; } = 760;
    public bool Maximized { get; set; }
}

public sealed class RemoteCommand
{
    public string Command { get; set; } = "";
    public string? Duration { get; set; }
    public long? DurationMs { get; set; }
    public string? Mode { get; set; }
    public int? SlideNumber { get; set; }
    public string? PresentationId { get; set; }
    public bool? Confirmed { get; set; }
    public bool? SyncAllRules { get; set; }
    public string? OperationId { get; set; }
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
    public bool IsOvertime { get; set; }
    public bool WindowVisible { get; set; }
    public bool Muted { get; set; }
    public bool TimeUpBlackoutActive { get; set; }
    public int RuleCount { get; set; }
    public int ConnectedClients { get; set; }
    public string Version { get; set; } = AppVersion.Current;
    public long Revision { get; set; }
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
    public bool IsOvertime { get; set; }
    public bool ContinueOvertime { get; set; }
    public bool WindowVisible { get; set; }
    public bool Muted { get; set; }
    public bool TimeUpBlackoutActive { get; set; }
    public int RuleCount { get; set; }
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
    public DateTime UpdatedAt { get; set; }
    public string Error { get; set; } = "";
    public List<PresentationOption> Presentations { get; set; } = [];
    public string Operation { get; set; } = "Idle";
    public string OperationMessage { get; set; } = "";
    public DateTime? OperationStartedAt { get; set; }
    public string OperationId { get; set; } = "";
    public bool IsOperationBusy { get; set; }
    public bool IsCurrentPresentationManaged { get; set; }
    public int OpenPresentationCount { get; set; }
    public bool WpsDetected { get; set; }
    public WpsCapabilities WpsCapabilities { get; set; } = new();
}

public sealed class WpsCapabilities
{
    public bool CanEndSlideShow { get; set; }
    public bool CanClosePresentation { get; set; }
    public bool CanExitApplication { get; set; }
    public bool CanForceExit { get; set; }
    public string Message { get; set; } = "WPS 演示未检测到。";
}

public sealed class PresentationOption
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Directory { get; set; } = "";
    public bool IsOpen { get; set; }
    public bool IsActive { get; set; }
    public bool IsSlideShowRunning { get; set; }
    public bool IsManaged { get; set; }
}

public sealed record PresentationCommandResult(bool Success, string Message, PresentationState State);
