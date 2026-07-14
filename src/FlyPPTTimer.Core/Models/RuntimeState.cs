namespace FlyPPTTimer.Core.Models;

public sealed record TimerSnapshot(TimerState State, TimerMode Mode, TimeSpan Elapsed, TimeSpan Remaining, TimeSpan Display, TimeSpan Duration)
{
    public string DisplayText => Format(Display, Duration >= TimeSpan.FromHours(1));
    private static string Format(TimeSpan value, bool hours) => hours
        ? $"{(int)value.TotalHours:00}:{value.Minutes:00}:{value.Seconds:00}"
        : $"{(int)value.TotalMinutes:00}:{value.Seconds:00}";
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
}

public sealed class PresentationOption
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Directory { get; set; } = "";
    public bool IsOpen { get; set; }
    public bool IsActive { get; set; }
    public bool IsSlideShow { get; set; }
    public string Duration { get; set; } = "";
    public bool Exists { get; set; } = true;
}
public sealed record PresentationCommand(string Command, string? PresentationId = null, int? SlideNumber = null);
public sealed record PresentationCommandResult(bool Success, string Message, PresentationState State);

public sealed record ApplicationState(
    AppConfig Config,
    TimerSnapshot Timer,
    PresentationState Presentation,
    int ConnectedClients,
    bool Muted,
    string LastMessage,
    DateTime UpdatedAt);
