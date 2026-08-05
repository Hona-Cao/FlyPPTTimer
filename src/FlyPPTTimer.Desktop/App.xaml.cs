using System.IO;
using System.Windows;
using FlyPPTTimer.Desktop.ViewModels;
using FlyPPTTimer.Desktop.Views;
using FlyPPTTimer.Services;

namespace FlyPPTTimer.Desktop;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var log = new LogService();
        var configPath = Path.Combine(AppContext.BaseDirectory, "FlyPPTTimer.config.json");
        var configService = new ConfigService(log, configPath);
        var viewModel = new SettingsViewModel(configService.Load(), configService);
        MainWindow = new MainWindow(viewModel);
        MainWindow.Show();
    }
}
