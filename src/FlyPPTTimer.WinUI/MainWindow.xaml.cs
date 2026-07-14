using FlyPPTTimer.WinUI.Pages;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using CommunityToolkit.Mvvm.Input;
using H.NotifyIcon;
using H.NotifyIcon.Core;

namespace FlyPPTTimer.WinUI;

public sealed partial class MainWindow : Window
{
    private bool _exitRequested;
    private readonly TaskbarIcon _trayIcon;
    public MainWindow()
    {
        InitializeComponent(); Root.DataContext = App.Services.ViewModel;
        ExtendsContentIntoTitleBar = true; SetTitleBar(AppTitleBar); AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall; AppWindow.SetIcon("Assets/AppIcon.ico");
        AppWindow.Resize(new(1180, 780)); AppWindow.Closing += OnClosing; NavFrame.Navigate(typeof(OverviewPage));
        _trayIcon = CreateTrayIcon(); _trayIcon.ForceCreate(false);
    }
    public void ShowAndActivate() { AppWindow.Show(); Activate(); }
    private void OnClosing(AppWindow sender, AppWindowClosingEventArgs args) { if (_exitRequested || !App.Services.Controller.Config.Controls.MinimizeToTray) return; args.Cancel = true; AppWindow.Hide(); }
    private void PaneButton_Click(object sender, RoutedEventArgs e) => NavView.IsPaneOpen = !NavView.IsPaneOpen;
    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item) return;
        var page = item.Tag?.ToString() switch
        {
            "overview" => typeof(OverviewPage), "timer" => typeof(TimerPage), "powerpoint" => typeof(PowerPointPage),
            "files" => typeof(FilesPage), "remote" => typeof(RemotePage), "appearance" => typeof(AppearancePage),
            "hotkeys" => typeof(HotkeysPage), "settings" => typeof(SettingsPage), "diagnostics" => typeof(DiagnosticsPage), _ => typeof(OverviewPage)
        };
        if (NavFrame.CurrentSourcePageType != page) NavFrame.Navigate(page);
    }
    private void TrayOpen_Click(object sender, RoutedEventArgs e) => ShowAndActivate();
    private void TrayTimer_Click(object sender, RoutedEventArgs e) => App.Services.Controller.Timer.StartOrPause();
    private void TrayStop_Click(object sender, RoutedEventArgs e) => App.Services.Controller.Timer.Stop(true);
    private void TrayExit_Click(object sender, RoutedEventArgs e) { _exitRequested = true; _trayIcon.Dispose(); App.Services.Dispose(); Close(); }
    private TaskbarIcon CreateTrayIcon()
    {
        var menu = new MenuFlyout();
        var open = new MenuFlyoutItem { Text = "打开 FlyPPTTimer" }; open.Click += TrayOpen_Click; menu.Items.Add(open); menu.Items.Add(new MenuFlyoutSeparator());
        var timer = new MenuFlyoutItem { Text = "开始 / 暂停" }; timer.Click += TrayTimer_Click; menu.Items.Add(timer);
        var stop = new MenuFlyoutItem { Text = "停止并重置" }; stop.Click += TrayStop_Click; menu.Items.Add(stop);
        var exit = new MenuFlyoutItem { Text = "退出" }; exit.Click += TrayExit_Click; menu.Items.Add(exit);
        return new TaskbarIcon { ToolTipText = "FlyPPTTimer", IconSource = new BitmapImage(new Uri("ms-appx:///Assets/AppIcon.ico")), ContextFlyout = menu, MenuActivation = PopupActivationMode.RightClick, LeftClickCommand = new RelayCommand(ShowAndActivate) };
    }
}
