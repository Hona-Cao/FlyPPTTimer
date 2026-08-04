using FlyPPTTimer.Models;

namespace FlyPPTTimer.Services;

/// <summary>
/// FlyPPTTimer-facing boundary for presentation software integration.
/// Implementations may use Microsoft PowerPoint COM, WPS capabilities,
/// a test double, or another compatible presentation provider.
/// </summary>
public interface IPresentationControlService : IDisposable
{
    event EventHandler<string>? SlideShowStarted;
    event EventHandler? SlideShowEnded;
    event EventHandler? SlideShowWindowActivated;
    event EventHandler? StateChanged;

    PresentationState GetState();
    PresentationCommandResult Queue(RemoteCommand command);
    PresentationCommandResult Execute(RemoteCommand command);
}
