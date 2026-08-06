using System.Drawing;
using System.Globalization;
using System.Windows.Automation;
using System.Windows.Forms.Integration;
using System.Windows.Interop;
using FlyPPTTimer.Forms;
using FlyPPTTimer.Models;
using FlyPPTTimer.Native;
using FlyPPTTimer.Services;
using Wpf = System.Windows;
using WpfControls = System.Windows.Controls;
using WpfInput = System.Windows.Input;
using WpfMedia = System.Windows.Media;
using WpfThreading = System.Windows.Threading;

namespace FlyPPTTimer.Desktop;

/// <summary>Formal WPF timer overlay. Business commands are supplied by the application host.</summary>
public sealed class WpfTimerOverlayWindow : Wpf.Window
{
    private readonly WpfControls.Border _surface;
    private readonly WpfControls.Border _flashBorder;
    private readonly WpfControls.TextBlock _timeText;
    private readonly WpfThreading.DispatcherTimer _flashTimer = new();
    private AppConfig _config = new();
    private Screen _targetScreen = Screen.PrimaryScreen!;
    private PointF? _preservedCenter;
    private bool _modelessInteropEnabled;
    private bool _applyingBounds;
    private bool _dragging;
    private bool _flashVisible = true;
    private bool _pauseFlashActive;
    private DateTime _flashUntil = DateTime.MinValue;
    private string _activeFlashStyle = "闪烁背景";
    private int _activeFlashOnMs = 350;
    private int _activeFlashOffMs = 350;
    private WpfMedia.Brush _normalBackground = WpfMedia.Brushes.Transparent;
    private WpfMedia.Brush _normalText = WpfMedia.Brushes.White;

    public WpfTimerOverlayWindow(
        Action resetPosition,
        Action toggleMute,
        Action showRemote,
        Action showSettings,
        Action showClassicSettings,
        Action exit)
    {
        WindowStyle = Wpf.WindowStyle.None;
        ResizeMode = Wpf.ResizeMode.NoResize;
        AllowsTransparency = true;
        Background = WpfMedia.Brushes.Transparent;
        ShowInTaskbar = false;
        WindowStartupLocation = Wpf.WindowStartupLocation.Manual;
        SizeToContent = Wpf.SizeToContent.Manual;
        AutomationProperties.SetAutomationId(this, "TimerOverlayWindow");
        AutomationProperties.SetName(this, "FlyPPTTimer WPF Timer");

        _timeText = new WpfControls.TextBlock
        {
            TextAlignment = Wpf.TextAlignment.Center,
            HorizontalAlignment = Wpf.HorizontalAlignment.Center,
            VerticalAlignment = Wpf.VerticalAlignment.Center
        };
        AutomationProperties.SetAutomationId(_timeText, "TimerDisplayText");
        _flashBorder = new WpfControls.Border
        {
            BorderThickness = new Wpf.Thickness(0),
            Child = _timeText
        };
        _surface = new WpfControls.Border { Child = _flashBorder };
        Content = _surface;
        ContextMenu = BuildContextMenu(resetPosition, toggleMute, showRemote, showSettings, showClassicSettings, exit);

        SourceInitialized += (_, _) =>
        {
            ApplyNativeStyles();
            PlaceWindow();
        };
        LocationChanged += (_, _) => RaisePositionChanged();
        MouseLeftButtonDown += OnMouseLeftButtonDown;
        MouseLeftButtonUp += (_, _) => _dragging = false;
        MouseRightButtonUp += (_, e) =>
        {
            if (_config.Controls.ClickThrough || ContextMenu is null) return;
            ContextMenu.PlacementTarget = this;
            ContextMenu.IsOpen = true;
            e.Handled = true;
        };
        _flashTimer.Tick += (_, _) => UpdateFlash();
        Closed += (_, _) => IsDisposed = true;
    }

    public event EventHandler<OverlayMovedEventArgs>? PositionChangedByUser;
    public event EventHandler<OverlaySizeExpansionEventArgs>? SizeExpansionRequested;

    public Screen TargetScreen => _targetScreen;
    public bool Visible => IsVisible;
    public bool IsDisposed { get; private set; }

    public PointF CenterPoint
    {
        get
        {
            var bounds = PhysicalBounds;
            return new PointF(bounds.Left + bounds.Width / 2f, bounds.Top + bounds.Height / 2f);
        }
    }

    public Rectangle PhysicalBounds
    {
        get
        {
            var handle = new WindowInteropHelper(this).Handle;
            if (handle != IntPtr.Zero && NativeMethods.GetWindowRect(handle, out var rect))
                return new Rectangle(rect.Left, rect.Top, rect.Width, rect.Height);
            var dpi = RemoteScreenDpiProvider.FromScreen(_targetScreen).Dpi;
            var size = RemoteWindowLayoutService.DipToPhysical(
                new Size(Math.Max(1, _config.Appearance.Width), Math.Max(1, _config.Appearance.Height)), dpi);
            var origin = OverlayPlacementService.CalculateOrigin(
                _targetScreen.WorkingArea, dpi, _config.Placement.Anchor,
                _config.Placement.OffsetXPercent, _config.Placement.OffsetYPercent);
            var location = OverlayPlacementService.LocationFromCenter(_preservedCenter ?? origin, size);
            return new Rectangle(location, size);
        }
    }

    public void ApplyConfig(AppConfig config, Screen screen, PointF? preservedCenter = null)
    {
        _config = config;
        _targetScreen = screen;
        _preservedCenter = preservedCenter;
        Width = Math.Max(1, config.Appearance.Width);
        Height = Math.Max(1, config.Appearance.Height);
        Topmost = config.Appearance.AlwaysOnTop;
        _normalBackground = ParseBrush(config.Appearance.BackgroundColor, WpfMedia.Colors.White,
            config.Appearance.BackgroundOpacity);
        _normalText = ParseBrush(config.Appearance.TextColor, WpfMedia.Colors.Black,
            config.Appearance.TextOpacity);
        _surface.Background = _normalBackground;
        _timeText.Foreground = _normalText;
        _timeText.FontFamily = new WpfMedia.FontFamily(
            string.IsNullOrWhiteSpace(config.Appearance.FontFamily) ? "Microsoft YaHei UI" : config.Appearance.FontFamily);
        _timeText.FontSize = Math.Max(1, config.Appearance.FontSize * 96d / 72d);
        _timeText.FontWeight = config.Appearance.FontStyle.Contains("Bold", StringComparison.OrdinalIgnoreCase)
            ? Wpf.FontWeights.Bold
            : Wpf.FontWeights.Normal;
        _timeText.FontStyle = config.Appearance.FontStyle.Contains("Italic", StringComparison.OrdinalIgnoreCase)
            ? Wpf.FontStyles.Italic
            : Wpf.FontStyles.Normal;
        _surface.CornerRadius = config.Appearance.Shape.Contains("圆角")
            ? new Wpf.CornerRadius(config.Appearance.Shape.Contains("大") ? 14 : 7)
            : new Wpf.CornerRadius(0);
        _surface.ClipToBounds = true;
        if (new WindowInteropHelper(this).Handle != IntPtr.Zero)
        {
            ApplyNativeStyles();
            PlaceWindow();
        }
        if (config.Placement.Visible) ShowModeless();
        else Hide();
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
        ApplyNativeStyles();
        PlaceWindow();
    }

    public void SetVisible(bool visible)
    {
        if (visible) ShowModeless();
        else Hide();
    }

    public void UpdateTime(TimerSnapshot snapshot)
    {
        var content = TimerDisplayFormatter.Format(snapshot, _config);
        _timeText.Text = content.Text;
        _timeText.Visibility = Wpf.Visibility.Visible;
        if (content.IsTimeout)
        {
            _surface.Background = ParseBrush(_config.Appearance.TimeoutBackgroundColor, WpfMedia.Colors.DarkRed,
                _config.Appearance.BackgroundOpacity);
            _timeText.Foreground = ParseBrush(_config.Appearance.TimeoutTextColor, WpfMedia.Colors.White,
                _config.Appearance.TextOpacity);
        }
        else if (_flashUntil < DateTime.Now)
        {
            RestoreNormalColors();
        }

        if (snapshot.State == TimerState.Paused && _config.Behavior.FlashPausedTime) StartPauseFlash();
        else if (_pauseFlashActive) StopFlash();
        RequestExpansionIfNeeded(content.Text);
    }

    public void ReassertTopMost()
    {
        if (!IsVisible || !_config.Appearance.AlwaysOnTop) return;
        Topmost = false;
        Topmost = true;
    }

    public void Flash(PromptSettings prompt, int seconds)
    {
        _activeFlashStyle = string.IsNullOrWhiteSpace(prompt.FlashStyle) ? "闪烁背景" : prompt.FlashStyle;
        if (_activeFlashStyle == "无" || seconds <= 0) return;
        _pauseFlashActive = false;
        _flashUntil = DateTime.Now.AddSeconds(Math.Max(1, seconds));
        _activeFlashOnMs = Math.Max(50, prompt.FlashOnMs);
        _activeFlashOffMs = Math.Max(50, prompt.FlashOffMs);
        _flashTimer.Interval = TimeSpan.FromMilliseconds(_activeFlashOnMs);
        _flashTimer.Start();
    }

    public new void Close()
    {
        if (IsDisposed) return;
        IsDisposed = true;
        _flashTimer.Stop();
        base.Close();
    }

    private static WpfControls.ContextMenu BuildContextMenu(
        Action resetPosition,
        Action toggleMute,
        Action showRemote,
        Action showSettings,
        Action showClassicSettings,
        Action exit)
    {
        var menu = new WpfControls.ContextMenu();
        AddMenuItem(menu, "ResetTimerPosition", "重置计时窗口位置", resetPosition);
        AddMenuItem(menu, "ToggleMute", "静音/取消静音", toggleMute);
        AddMenuItem(menu, "OpenRemoteControl", "远程控制", showRemote);
        AddMenuItem(menu, "OpenWpfSettings", "设置", showSettings);
        AddMenuItem(menu, "OpenClassicSettings", "经典设置", showClassicSettings);
        menu.Items.Add(new WpfControls.Separator());
        AddMenuItem(menu, "ExitApplication", "退出", exit);
        return menu;
    }

    private static void AddMenuItem(WpfControls.ContextMenu menu, string automationId, string text, Action action)
    {
        var item = new WpfControls.MenuItem { Header = Localization.T(text) };
        AutomationProperties.SetAutomationId(item, automationId);
        item.Click += (_, _) => action();
        menu.Items.Add(item);
    }

    private void OnMouseLeftButtonDown(object sender, WpfInput.MouseButtonEventArgs e)
    {
        if (_config.Controls.LockPosition || _config.Controls.ClickThrough || e.ButtonState != WpfInput.MouseButtonState.Pressed)
            return;
        _dragging = true;
        try { DragMove(); }
        catch (InvalidOperationException) { }
        finally { _dragging = false; }
    }

    private void RaisePositionChanged()
    {
        if (_applyingBounds || !_dragging) return;
        var bounds = PhysicalBounds;
        PositionChangedByUser?.Invoke(this, new OverlayMovedEventArgs(bounds.Location, _targetScreen));
    }

    private void PlaceWindow()
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero) return;
        var dpi = RemoteScreenDpiProvider.FromScreen(_targetScreen).Dpi;
        var size = RemoteWindowLayoutService.DipToPhysical(
            new Size(Math.Max(1, _config.Appearance.Width), Math.Max(1, _config.Appearance.Height)), dpi);
        var origin = OverlayPlacementService.CalculateOrigin(
            _targetScreen.WorkingArea, dpi, _config.Placement.Anchor,
            _config.Placement.OffsetXPercent, _config.Placement.OffsetYPercent);
        var location = OverlayPlacementService.LocationFromCenter(_preservedCenter ?? origin, size);
        _applyingBounds = true;
        try
        {
            NativeMethods.SetWindowPos(
                handle,
                _config.Appearance.AlwaysOnTop ? NativeMethods.HwndTopmost : NativeMethods.HwndNoTopmost,
                location.X, location.Y, size.Width, size.Height,
                IsVisible ? NativeMethods.SwpShowWindow : 0);
        }
        finally { _applyingBounds = false; }
    }

    private void ApplyNativeStyles()
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero) return;
        var style = NativeMethods.GetWindowLong(handle, NativeMethods.GwlExStyle) | NativeMethods.WsExLayered;
        style = _config.Controls.ClickThrough
            ? style | NativeMethods.WsExTransparent
            : style & ~NativeMethods.WsExTransparent;
        NativeMethods.SetWindowLong(handle, NativeMethods.GwlExStyle, style);
    }

    private void RequestExpansionIfNeeded(string text)
    {
        if (new WindowInteropHelper(this).Handle == IntPtr.Zero || string.IsNullOrEmpty(text)) return;
        var dpi = WpfMedia.VisualTreeHelper.GetDpi(this);
        var formatted = new WpfMedia.FormattedText(
            text,
            CultureInfo.CurrentUICulture,
            Wpf.FlowDirection.LeftToRight,
            new WpfMedia.Typeface(_timeText.FontFamily, _timeText.FontStyle, _timeText.FontWeight, _timeText.FontStretch),
            _timeText.FontSize,
            _timeText.Foreground,
            dpi.PixelsPerDip);
        var requiredWidth = (int)Math.Ceiling((formatted.Width + 4) * dpi.DpiScaleX);
        var requiredHeight = (int)Math.Ceiling((formatted.Height + 2) * dpi.DpiScaleY);
        var bounds = PhysicalBounds;
        if (requiredWidth > bounds.Width || requiredHeight > bounds.Height)
            SizeExpansionRequested?.Invoke(this, new OverlaySizeExpansionEventArgs(
                Math.Max(requiredWidth, bounds.Width), Math.Max(requiredHeight, bounds.Height)));
    }

    private void StartPauseFlash()
    {
        if (_pauseFlashActive) return;
        _pauseFlashActive = true;
        _flashUntil = DateTime.MaxValue;
        _activeFlashStyle = _config.Appearance.FlashStyle;
        _activeFlashOnMs = Math.Max(50, _config.Appearance.FlashOnMs);
        _activeFlashOffMs = Math.Max(50, _config.Appearance.FlashOffMs);
        _flashTimer.Interval = TimeSpan.FromMilliseconds(_activeFlashOnMs);
        _flashTimer.Start();
    }

    private void StopFlash()
    {
        _pauseFlashActive = false;
        _flashTimer.Stop();
        _flashUntil = DateTime.MinValue;
        _flashVisible = true;
        _timeText.Visibility = Wpf.Visibility.Visible;
        _flashBorder.BorderThickness = new Wpf.Thickness(0);
        RestoreNormalColors();
    }

    private void UpdateFlash()
    {
        if (DateTime.Now >= _flashUntil)
        {
            StopFlash();
            return;
        }
        _flashVisible = !_flashVisible;
        var flashBrush = ParseBrush(_config.Appearance.FlashBackgroundColor, WpfMedia.Colors.Gold, 100);
        if (_activeFlashStyle.Contains("文字"))
            _timeText.Visibility = _flashVisible ? Wpf.Visibility.Visible : Wpf.Visibility.Hidden;
        if (_activeFlashStyle.Contains("背景"))
            _surface.Background = _flashVisible ? flashBrush : _normalBackground;
        if (_activeFlashStyle.Contains("边框"))
        {
            _flashBorder.BorderBrush = flashBrush;
            _flashBorder.BorderThickness = _flashVisible ? new Wpf.Thickness(3) : new Wpf.Thickness(0);
        }
        _flashTimer.Interval = TimeSpan.FromMilliseconds(_flashVisible ? _activeFlashOnMs : _activeFlashOffMs);
    }

    private void RestoreNormalColors()
    {
        _surface.Background = _normalBackground;
        _timeText.Foreground = _normalText;
    }

    private static WpfMedia.Brush ParseBrush(string? value, WpfMedia.Color fallback, int opacityPercent)
    {
        var color = fallback;
        try
        {
            if (WpfMedia.ColorConverter.ConvertFromString(value) is WpfMedia.Color parsed) color = parsed;
        }
        catch (FormatException) { }
        color.A = (byte)Math.Round(Math.Clamp(opacityPercent, 0, 100) * 2.55, MidpointRounding.AwayFromZero);
        return new WpfMedia.SolidColorBrush(color);
    }
}
