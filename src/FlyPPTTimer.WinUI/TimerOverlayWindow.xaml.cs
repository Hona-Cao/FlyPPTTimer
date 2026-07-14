using System.Runtime.InteropServices;
using FlyPPTTimer.Core.Models;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;

namespace FlyPPTTimer.WinUI;

public sealed partial class TimerOverlayWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly AppConfig _config;
    private readonly DispatcherTimer _flashTimer = new();
    private bool _flashOn;
    private int _manualFlashTicks;
    private PromptSettings? _activePrompt;
    public TimerOverlayWindow(MainViewModel viewModel, AppConfig config, RectInt32 monitor)
    {
        InitializeComponent(); _viewModel = viewModel; _config = config; Root.DataContext = viewModel;
        var appearance = config.Appearance; TimerText.FontSize = appearance.FontSize; ApplyColors(false);
        if (AppWindow.Presenter is OverlappedPresenter presenter) { presenter.SetBorderAndTitleBar(false, false); presenter.IsAlwaysOnTop = appearance.AlwaysOnTop; presenter.IsResizable = false; presenter.IsMaximizable = false; presenter.IsMinimizable = false; }
        ExtendsContentIntoTitleBar = true; if (!config.Controls.LockPosition) SetTitleBar(Root);
        var position = CalculatePosition(monitor, appearance.Width, appearance.Height, config.Placement); AppWindow.MoveAndResize(new(position.X, position.Y, appearance.Width, appearance.Height));
        AppWindow.Show(); ApplyClickThrough(config.Controls.ClickThrough);
        _flashTimer.Interval = TimeSpan.FromMilliseconds(Math.Max(100, appearance.FlashOnMs));
        _flashTimer.Tick += (_, _) =>
        {
            _flashOn = !_flashOn;
            ApplyColors(_flashOn);
            if (_manualFlashTicks > 0 && --_manualFlashTicks == 0) UpdateFlash();
        };
        _viewModel.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(MainViewModel.TimerStatus)) UpdateFlash(); };
    }
    private void UpdateFlash() { if (_config.Behavior.FlashPausedTime && _viewModel.TimerStatus == "暂停") { _activePrompt = new PromptSettings { FlashText = true }; _flashTimer.Start(); } else { _flashTimer.Stop(); _activePrompt = null; _flashOn = false; ApplyColors(false); } }
    public void Flash(PromptSettings? prompt = null)
    {
        _activePrompt = prompt ?? new PromptSettings { Enabled = true, FlashBackground = true };
        _manualFlashTicks = Math.Max(4, _config.Behavior.EndPrompt.FlashSeconds * 2);
        _flashOn = true;
        ApplyColors(true);
        _flashTimer.Start();
    }
    private void ApplyColors(bool flash)
    {
        var prompt = _activePrompt;
        var flashBackground = flash && (prompt?.FlashBackground ?? true);
        var flashText = flash && prompt?.FlashText == true;
        var flashBorder = flash && prompt?.FlashBorder == true;
        var background = Parse(flashBackground ? _config.Appearance.FlashBackgroundColor : _config.Appearance.BackgroundColor, (byte)(_config.Appearance.BackgroundOpacity * 255 / 100));
        var foreground = Parse(flashText ? _config.Appearance.FlashBackgroundColor : _config.Appearance.TextColor, (byte)(_config.Appearance.TextOpacity * 255 / 100));
        TimerSurface.Background = new SolidColorBrush(background); TimerText.Foreground = new SolidColorBrush(foreground);
        TimerSurface.BorderBrush = flashBorder ? new SolidColorBrush(Parse(_config.Appearance.FlashBackgroundColor, 255)) : null;
        TimerSurface.BorderThickness = flashBorder ? new Thickness(3) : new Thickness(0);
    }
    private void ApplyClickThrough(bool enabled)
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this); var style = GetWindowLongPtr(hwnd, -20).ToInt64();
        style = enabled ? style | 0x20 | 0x80000 : style & ~0x20; SetWindowLongPtr(hwnd, -20, new(style));
    }
    private static PointInt32 CalculatePosition(RectInt32 area, int width, int height, WindowPlacement placement)
    {
        var x = placement.Anchor switch { OverlayAnchor.TopCenter or OverlayAnchor.Center or OverlayAnchor.BottomCenter => area.X + (area.Width - width) / 2, OverlayAnchor.TopRight or OverlayAnchor.MiddleRight or OverlayAnchor.BottomRight => area.X + area.Width - width, _ => area.X };
        var y = placement.Anchor switch { OverlayAnchor.MiddleLeft or OverlayAnchor.Center or OverlayAnchor.MiddleRight => area.Y + (area.Height - height) / 2, OverlayAnchor.BottomLeft or OverlayAnchor.BottomCenter or OverlayAnchor.BottomRight => area.Y + area.Height - height, _ => area.Y };
        x += (int)(area.Width * placement.OffsetXPercent / 100m); y += (int)(area.Height * placement.OffsetYPercent / 100m); return new(x, y);
    }
    private static Windows.UI.Color Parse(string hex, byte alpha)
    {
        try { hex = hex.TrimStart('#'); return Windows.UI.Color.FromArgb(alpha, Convert.ToByte(hex[..2], 16), Convert.ToByte(hex.Substring(2, 2), 16), Convert.ToByte(hex.Substring(4, 2), 16)); }
        catch { return Windows.UI.Color.FromArgb(alpha, 32, 39, 51); }
    }
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] private static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr value);
}
