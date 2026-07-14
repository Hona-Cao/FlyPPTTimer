using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;

namespace FlyPPTTimer.WinUI;

public partial class App : Application
{
    private MainWindow? _window;
    public static AppServices Services { get; private set; } = null!;

    public App()
    {
        InitializeComponent();
        UnhandledException += (_, args) => { Services?.Log.Error("WinUI 未处理异常。", args.Exception); args.Handled = true; };
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        var key = AppInstance.FindOrRegisterForKey("FlyPPTTimer.v0.13");
        if (!key.IsCurrent)
        {
            await key.RedirectActivationToAsync(AppInstance.GetCurrent().GetActivatedEventArgs());
            Environment.Exit(0); return;
        }
        key.Activated += (_, _) => _window?.DispatcherQueue.TryEnqueue(() => { _window.ShowAndActivate(); });
        Services = new AppServices();
        _window = new MainWindow();
        Services.AttachWindow(_window);
        _window.Activate();
        Services.Start();
    }
}
