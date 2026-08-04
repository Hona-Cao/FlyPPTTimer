using FlyPPTTimer.Models;

namespace FlyPPTTimer.Services;

/// <summary>
/// Transitional adapter around the existing PowerPoint/WPS integration.
/// It lets the rest of FlyPPTTimer depend on a stable presentation boundary
/// while the large legacy service is split into provider-specific components.
/// </summary>
public sealed class PowerPointPresentationAdapter : IPresentationControlService
{
    private readonly PowerPointControlService _inner;

    public PowerPointPresentationAdapter(PowerPointControlService inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public event EventHandler<string>? SlideShowStarted
    {
        add => _inner.SlideShowStarted += value;
        remove => _inner.SlideShowStarted -= value;
    }

    public event EventHandler? SlideShowEnded
    {
        add => _inner.SlideShowEnded += value;
        remove => _inner.SlideShowEnded -= value;
    }

    public event EventHandler? SlideShowWindowActivated
    {
        add => _inner.SlideShowWindowActivated += value;
        remove => _inner.SlideShowWindowActivated -= value;
    }

    public event EventHandler? StateChanged
    {
        add => _inner.StateChanged += value;
        remove => _inner.StateChanged -= value;
    }

    public PresentationState GetState() => _inner.GetState();

    public PresentationCommandResult Queue(RemoteCommand command) => _inner.Queue(command);

    public PresentationCommandResult Execute(RemoteCommand command) => _inner.Execute(command);

    public void Dispose() => _inner.Dispose();
}
