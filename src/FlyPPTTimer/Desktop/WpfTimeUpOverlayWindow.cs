using System.Drawing;
using System.Windows.Automation;
using System.Windows.Forms.Integration;
using System.Windows.Interop;
using FlyPPTTimer.Native;
using FlyPPTTimer.Services;
using Wpf = System.Windows;
using WpfControls = System.Windows.Controls;
using WpfInput = System.Windows.Input;
using WpfMedia = System.Windows.Media;

namespace FlyPPTTimer.Desktop;

/// <summary>
/// Formal full-screen WPF "time's up" window shown on every display. It replaces the
/// legacy WinForms TimeUpBlackoutForm: borderless, top-most, black, no-activate, and it
/// dismisses itself (and notifies the host) on any click.
/// </summary>
public sealed class WpfTimeUpOverlayWindow : Wpf.Window
{
    private readonly WpfControls.TextBlock _label;
    private readonly Action _dismiss;

    public WpfTimeUpOverlayWindow(Screen screen, Action dismiss)
    {
        _dismiss = dismiss;
        Title = "FlyPPTTimer Time Up";
        WindowStyle = Wpf.WindowStyle.None;
        ResizeMode = Wpf.ResizeMode.NoResize;
        WindowStartupLocation = Wpf.WindowStartupLocation.Manual;
        ShowInTaskbar = false;
        Topmost = true;
        ShowActivated = false;
        Background = WpfMedia.Brushes.Black;
        Width = Math.Max(1, screen.Bounds.Width);
        Height = Math.Max(1, screen.Bounds.Height);
        Left = screen.Bounds.Left;
        Top = screen.Bounds.Top;
        AutomationProperties.SetAutomationId(this, "TimeUpBlackoutWindow");

        _label = new WpfControls.TextBlock
        {
            Text = Localization.T("时间到"),
            Foreground = WpfMedia.Brushes.White,
            Background = WpfMedia.Brushes.Black,
            TextAlignment = Wpf.TextAlignment.Center,
            HorizontalAlignment = Wpf.HorizontalAlignment.Center,
            VerticalAlignment = Wpf.VerticalAlignment.Center,
            FontFamily = new WpfMedia.FontFamily("Microsoft YaHei UI"),
            FontSize = 72,
            FontWeight = Wpf.FontWeights.Bold
        };
        AutomationProperties.SetAutomationId(_label, "TimeUpLabel");
        Content = _label;

        SourceInitialized += (_, _) => MakeNonActivating(screen);
        MouseLeftButtonDown += (_, _) => Close();
        MouseRightButtonDown += (_, _) => Close();
        KeyDown += (_, e) =>
        {
            if (e.Key == WpfInput.Key.Escape || e.Key == WpfInput.Key.Enter || e.Key == WpfInput.Key.Space)
                Close();
        };
        Closed += (_, _) => IsDisposed = true;
    }

    public bool IsDisposed { get; private set; }

    private void MakeNonActivating(Screen screen)
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero) return;
        const int wsExNoActivate = 0x08000000;
        var exStyle = NativeMethods.GetWindowLong(handle, NativeMethods.GwlExStyle);
        NativeMethods.SetWindowLong(handle, NativeMethods.GwlExStyle, exStyle | wsExNoActivate);
        Left = screen.Bounds.Left;
        Top = screen.Bounds.Top;
    }
}
