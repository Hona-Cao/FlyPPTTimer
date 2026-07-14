using FlyPPTTimer.Core.Models;

namespace FlyPPTTimer.Core.Services;

public sealed class ApplicationStateStore
{
    private readonly object _sync = new();
    private ApplicationState _state;

    public ApplicationStateStore(AppConfig config, TimerSnapshot timer) => _state = new(config, timer, new(), 0, false, "", DateTime.Now);
    public event EventHandler<ApplicationState>? Changed;
    public ApplicationState Current { get { lock (_sync) return _state; } }
    public void UpdateConfig(AppConfig value) => Update(s => s with { Config = value });
    public void UpdateTimer(TimerSnapshot value) => Update(s => s with { Timer = value });
    public void UpdatePresentation(PresentationState value) => Update(s => s with { Presentation = value });
    public void UpdateRemote(int clients) => Update(s => s with { ConnectedClients = clients });
    public void UpdateMessage(string message) => Update(s => s with { LastMessage = message });
    public void ToggleMuted() => Update(s => s with { Muted = !s.Muted });
    private void Update(Func<ApplicationState, ApplicationState> change)
    {
        ApplicationState next;
        lock (_sync) { next = change(_state) with { UpdatedAt = DateTime.Now }; _state = next; }
        Changed?.Invoke(this, next);
    }
}
