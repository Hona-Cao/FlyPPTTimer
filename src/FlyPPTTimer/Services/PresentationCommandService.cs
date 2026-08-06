using FlyPPTTimer.Models;

namespace FlyPPTTimer.Services;

/// <summary>
/// Application use-case boundary for presentation operations. Desktop and remote
/// callers use typed commands here; only this service translates them to the
/// stable v0.30.2 remote protocol understood by the PowerPoint/WPS adapter.
/// </summary>
public sealed class PresentationCommandService
{
    private readonly IPresentationControlService _controller;

    public PresentationCommandService(IPresentationControlService controller)
    {
        _controller = controller ?? throw new ArgumentNullException(nameof(controller));
    }

    public event EventHandler? StateChanged
    {
        add => _controller.StateChanged += value;
        remove => _controller.StateChanged -= value;
    }

    public event EventHandler<string>? SlideShowStarted
    {
        add => _controller.SlideShowStarted += value;
        remove => _controller.SlideShowStarted -= value;
    }

    public event EventHandler? SlideShowEnded
    {
        add => _controller.SlideShowEnded += value;
        remove => _controller.SlideShowEnded -= value;
    }

    public event EventHandler? SlideShowWindowActivated
    {
        add => _controller.SlideShowWindowActivated += value;
        remove => _controller.SlideShowWindowActivated -= value;
    }

    public PresentationState GetState() => _controller.GetState();

    public PresentationCommandResult Queue(PresentationCommand command) =>
        _controller.Queue(ToRemoteCommand(command));

    public PresentationCommandResult Execute(PresentationCommand command) =>
        _controller.Execute(ToRemoteCommand(command));

    public PresentationCommandResult QueueRemote(RemoteCommand command)
    {
        if (!TryFromRemoteCommand(command, out var presentationCommand))
            return new PresentationCommandResult(false, "命令不在演示控制白名单中。", GetState());

        return Queue(presentationCommand);
    }

    public static bool TryFromRemoteCommand(RemoteCommand command, out PresentationCommand result)
    {
        ArgumentNullException.ThrowIfNull(command);
        var kind = command.Command switch
        {
            "ppt.refresh" => PresentationCommandKind.Refresh,
            "ppt.startFromBeginning" => PresentationCommandKind.StartFromBeginning,
            "ppt.startFromCurrent" => PresentationCommandKind.StartFromCurrent,
            "ppt.previous" => PresentationCommandKind.Previous,
            "ppt.next" => PresentationCommandKind.Next,
            "ppt.gotoSlide" => PresentationCommandKind.GoToSlide,
            "ppt.endShow" => PresentationCommandKind.EndShow,
            "ppt.blackScreenToggle" => PresentationCommandKind.ToggleBlackScreen,
            "ppt.whiteScreenToggle" => PresentationCommandKind.ToggleWhiteScreen,
            "ppt.openPresentation" => PresentationCommandKind.OpenPresentation,
            "ppt.closeActivePresentation" => PresentationCommandKind.CloseActivePresentation,
            "ppt.closeCurrentPresentation" => PresentationCommandKind.CloseCurrentPresentation,
            "ppt.forceQuitAll" => PresentationCommandKind.ForceQuitAll,
            _ => (PresentationCommandKind?)null
        };

        if (kind is null)
        {
            result = default;
            return false;
        }

        result = new PresentationCommand(
            kind.Value,
            command.PresentationId,
            command.SlideNumber,
            command.Confirmed);
        return true;
    }

    public static RemoteCommand ToRemoteCommand(PresentationCommand command) => new()
    {
        Command = command.Kind switch
        {
            PresentationCommandKind.Refresh => "ppt.refresh",
            PresentationCommandKind.StartFromBeginning => "ppt.startFromBeginning",
            PresentationCommandKind.StartFromCurrent => "ppt.startFromCurrent",
            PresentationCommandKind.Previous => "ppt.previous",
            PresentationCommandKind.Next => "ppt.next",
            PresentationCommandKind.GoToSlide => "ppt.gotoSlide",
            PresentationCommandKind.EndShow => "ppt.endShow",
            PresentationCommandKind.ToggleBlackScreen => "ppt.blackScreenToggle",
            PresentationCommandKind.ToggleWhiteScreen => "ppt.whiteScreenToggle",
            PresentationCommandKind.OpenPresentation => "ppt.openPresentation",
            PresentationCommandKind.CloseActivePresentation => "ppt.closeActivePresentation",
            PresentationCommandKind.CloseCurrentPresentation => "ppt.closeCurrentPresentation",
            PresentationCommandKind.ForceQuitAll => "ppt.forceQuitAll",
            _ => throw new ArgumentOutOfRangeException(nameof(command), command.Kind, "未知演示命令。")
        },
        PresentationId = command.PresentationId,
        SlideNumber = command.SlideNumber,
        Confirmed = command.Confirmed
    };
}

public enum PresentationCommandKind
{
    Refresh,
    StartFromBeginning,
    StartFromCurrent,
    Previous,
    Next,
    GoToSlide,
    EndShow,
    ToggleBlackScreen,
    ToggleWhiteScreen,
    OpenPresentation,
    CloseActivePresentation,
    CloseCurrentPresentation,
    ForceQuitAll
}

public readonly record struct PresentationCommand(
    PresentationCommandKind Kind,
    string? PresentationId = null,
    int? SlideNumber = null,
    bool? Confirmed = null);
