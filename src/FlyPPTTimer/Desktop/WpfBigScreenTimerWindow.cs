using System.Drawing;
using System.Windows.Automation;
using System.Windows.Forms.Integration;
using System.Windows.Interop;
using FlyPPTTimer.Models;
using FlyPPTTimer.Native;
using FlyPPTTimer.Services;
using Wpf = System.Windows;
using WpfControls = System.Windows.Controls;
using WpfInput = System.Windows.Input;
using WpfMedia = System.Windows.Media;

namespace FlyPPTTimer.Desktop;

/// <summary>Resizable WPF timer window restricted to a non-primary display.</summary>
public sealed class WpfBigScreenTimerWindow : Wpf.Window
{
    private readonly WpfControls.Grid _surface;
    private readonly WpfControls.TextBlock _timeText;
    private AppConfig _config = new();
    private Screen _targetScreen = Screen.PrimaryScreen!;
    private bool _modelessInteropEnabled;

    public WpfBigScreenTimerWindow()
    {
        Title = "FlyPPTTimer Big Screen";
        WindowStyle = Wpf.WindowStyle.SingleBorderWindow;
        ResizeMode = Wpf.ResizeMode.CanResize;
        WindowStartupLocation = Wpf.WindowStartupLocation.Manual;
        ShowInTaskbar = true;
        MinWidth = 640;
        MinHeight = 360;
        AutomationProperties.SetAutomationId(this, "BigScreenTimerWindow");
        _timeText = new WpfControls.TextBlock
        {
            TextAlignment = Wpf.TextAlignment.Center,
            HorizontalAlignment = Wpf.HorizontalAlignment.Center,
            VerticalAlignment = Wpf.VerticalAlignment.Center,
            FontWeight = Wpf.FontWeights.Bold
        };
        AutomationProperties.SetAutomationId(_timeText, "BigScreenTimerDisplayText");
        _surface = new WpfControls.Grid { Children = { _timeText } };
        Content = _surface;
        SizeChanged += (_, _) => UpdateDisplayFont();
        KeyDown += (_, e) =>
        {
            if (e.Key == WpfInput.Key.Escape) Hide();
        };
        SourceInitialized += (_, _) => PlaceOnTargetScreen();
        Closed += (_, _) => IsDisposed = true;
    }

    public bool IsDisposed { get; private set; }

    public void ApplyConfig(AppConfig config, Screen screen)
    {
        if (screen.Primary) throw new ArgumentException("The big-screen timer cannot use the primary display.", nameof(screen));
        _config = config;
        _targetScreen = screen;
        Topmost = config.Appearance.AlwaysOnTop;
        _timeText.FontFamily = new WpfMedia.FontFamily(
            string.IsNullOrWhiteSpace(config.Appearance.FontFamily) ? "Microsoft YaHei UI" : config.Appearance.FontFamily);
        PlaceOnTargetScreen();
    }

    public void ShowModeless()
    {
        if (IsDisposed || IsVisible) return;
        if (!_modelessInteropEnabled)
        {
            ElementHost.EnableModelessKeyboardInterop(this);
            _modelessInteropEnabled = true;
        }
        Show();
        PlaceOnTargetScreen();
        WindowState = Wpf.WindowState.Maximized;
    }

    public void UpdateTime(TimerSnapshot snapshot)
    {
        var content = TimerDisplayFormatter.Format(snapshot, _config);
        _timeText.Text = content.Text;
        _timeText.Foreground = ParseBrush(
            content.IsTimeout ? _config.Appearance.TimeoutTextColor : _config.Appearance.TextColor,
            content.IsTimeout ? WpfMedia.Colors.White : WpfMedia.Colors.Black);
        _surface.Background = ParseBrush(
            content.IsTimeout ? _config.Appearance.TimeoutBackgroundColor : _config.Appearance.BackgroundColor,
            content.IsTimeout ? WpfMedia.Colors.DarkRed : WpfMedia.Colors.White);
    }

    public new void Close()
    {
        if (IsDisposed) return;
        IsDisposed = true;
        base.Close();
    }

    private void PlaceOnTargetScreen()
    {
        if (new WindowInteropHelper(this).Handle == IntPtr.Zero) return;
        WindowState = Wpf.WindowState.Normal;
        var handle = new WindowInteropHelper(this).Handle;
        var area = _targetScreen.WorkingArea;
        NativeMethods.SetWindowPos(handle, IntPtr.Zero, area.Left, area.Top, area.Width, area.Height, NativeMethods.SwpShowWindow);
    }

    private void UpdateDisplayFont()
    {
        if (WindowState == Wpf.WindowState.Minimized || ActualHeight <= 0) return;
        _timeText.FontSize = Math.Clamp(ActualHeight * 0.30, 48, 360);
    }

    private static WpfMedia.Brush ParseBrush(string? value, WpfMedia.Color fallback)
    {
        try
        {
            if (WpfMedia.ColorConverter.ConvertFromString(value) is WpfMedia.Color parsed)
                return new WpfMedia.SolidColorBrush(parsed);
        }
        catch (FormatException) { }
        return new WpfMedia.SolidColorBrush(fallback);
    }
}
