using System.IO;
using System.Windows;
using FlyPPTTimer.Desktop.ViewModels;
using FlyPPTTimer.Desktop.Views;
using FlyPPTTimer.Services;
using AppLocalization = FlyPPTTimer.Services.Localization;

namespace FlyPPTTimer.Desktop;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var log = new LogService();
        var configPath = Path.Combine(AppContext.BaseDirectory, "FlyPPTTimer.config.json");
        var configService = new ConfigService(log, configPath);
        var config = configService.Load();
        AppLocalization.Initialize(config.Language);
        var screens = System.Windows.Forms.Screen.AllScreens
            .Select(screen => new SettingsChoice<string>(screen.Primary ? "" : screen.DeviceName, screen.Primary ? AppLocalization.T("主屏幕") : screen.DeviceName))
            .ToArray();
        var viewModel = new SettingsViewModel(config, configService, screens);
        MainWindow = new MainWindow(viewModel);
        MainWindow.Show();
    }
}
