using System.Reflection;
using System.Text;
using FlyPPTTimer.Core.Services;
using FlyPPTTimer.Infrastructure.Windows;

namespace FlyPPTTimer.WinUI;

public sealed class AppServices : IDisposable
{
    private MainWindow? _window;
    public AppServices()
    {
        Log = new(); Config = new(Log); ApplicationController? holder = null;
        PowerPoint = new(() => holder?.Config ?? new(), Log); Controller = holder = new(Config, Log, PowerPoint);
        Network = new(); Remote = new(Controller, LoadWebAsset, Log); Hotkeys = new(Log); Alerts = new(() => Controller.Config, () => Controller.State.Current.Muted, Log); ViewModel = new MainViewModel(this);
        Controller.Timer.Updated += (_, snapshot) => Alerts.Handle(snapshot);
        Controller.Timer.Finished += (_, snapshot) => Alerts.HandleFinished(snapshot);
        Alerts.VisualRequested += (_, prompt) => MainWindow.DispatcherQueue.TryEnqueue(() => Overlays?.Flash(prompt));
    }
    public LogService Log { get; }
    public ConfigService Config { get; }
    public NetworkAddressService Network { get; }
    public PowerPointControlService PowerPoint { get; }
    public ApplicationController Controller { get; }
    public RemoteControlService Remote { get; }
    public GlobalHotkeyService Hotkeys { get; }
    public AlertService Alerts { get; }
    public MainViewModel ViewModel { get; }
    public OverlayManager? Overlays { get; private set; }
    public MainWindow MainWindow => _window ?? throw new InvalidOperationException("主窗口尚未初始化。");
    public void AttachWindow(MainWindow window) { _window = window; ViewModel.AttachDispatcher(window.DispatcherQueue); Overlays = new OverlayManager(ViewModel, () => Controller.Config); }
    public void Start() { if (Controller.Config.RemoteControl.Enabled) Remote.Start(); ConfigureHotkeys(); Overlays?.Rebuild(); ViewModel.RefreshAll(); }
    public void ConfigureHotkeys() => Hotkeys.Configure(Controller.Config.Controls.Hotkeys, new Dictionary<string, Action>
    {
        ["startPause"] = Controller.Timer.StartOrPause, ["stopReset"] = () => Controller.Timer.Stop(true),
        ["toggleWindow"] = () => MainWindow.DispatcherQueue.TryEnqueue(() => Overlays?.Toggle()),
        ["openSettings"] = () => MainWindow.DispatcherQueue.TryEnqueue(ActivateMainWindow),
        ["flash"] = () => MainWindow.DispatcherQueue.TryEnqueue(() => Overlays?.Flash()),
        ["toggleMute"] = Controller.State.ToggleMuted
    });
    public void ActivateMainWindow() => _window?.ShowAndActivate();
    private static string LoadWebAsset(string name)
    {
        var assembly = Assembly.GetExecutingAssembly(); var resource = assembly.GetManifestResourceNames().First(x => x.EndsWith($"Web.{name}", StringComparison.OrdinalIgnoreCase));
        using var stream = assembly.GetManifestResourceStream(resource)!; using var reader = new StreamReader(stream, Encoding.UTF8); return reader.ReadToEnd();
    }
    public void Dispose() { Hotkeys.Dispose(); Remote.Dispose(); Overlays?.Dispose(); Controller.Dispose(); }
}
