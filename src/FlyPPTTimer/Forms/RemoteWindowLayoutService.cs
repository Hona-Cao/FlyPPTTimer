using FlyPPTTimer.Models;
using System.Runtime.InteropServices;

namespace FlyPPTTimer.Forms;

internal enum RemoteLayoutMode
{
    Standard,
    Compact
}

internal readonly record struct RemoteScreenMetrics(
    string DeviceName,
    Rectangle WorkingArea,
    int Dpi,
    bool Primary);

internal readonly record struct RemoteWindowRestorePlan(
    RemoteScreenMetrics Screen,
    Size ClientSizePhysical,
    Rectangle WindowBoundsPhysical,
    bool Maximized);

/// <summary>
/// Pure 96-DPI layout and placement calculations for the remote-control window.
/// WinForms owns control autoscaling; this service only converts window/client bounds.
/// </summary>
internal static class RemoteWindowLayoutService
{
    public const int DesignClientWidthDip = 1180;
    public const int DesignClientHeightDip = 760;
    public const int MinimumClientWidthDip = 1000;
    public const int MinimumClientHeightDip = 660;
    public const int StandardMinimumWidthDip = 1120;
    public const int StandardMinimumHeightDip = 720;
    public const int WorkingAreaSafetyMarginPhysical = 32;

    public static int DipToPhysical(int dip, int dpi) =>
        (int)Math.Round(dip * Math.Max(96, dpi) / 96D, MidpointRounding.AwayFromZero);

    public static int PhysicalToDip(int physical, int dpi) =>
        (int)Math.Round(physical * 96D / Math.Max(96, dpi), MidpointRounding.AwayFromZero);

    public static Size DipToPhysical(Size dip, int dpi) => new(
        DipToPhysical(dip.Width, dpi),
        DipToPhysical(dip.Height, dpi));

    public static Size PhysicalToDip(Size physical, int dpi) => new(
        PhysicalToDip(physical.Width, dpi),
        PhysicalToDip(physical.Height, dpi));

    public static Size GetLogicalClientSize(Size clientPhysical, int dpi) =>
        PhysicalToDip(clientPhysical, dpi);

    public static RemoteLayoutMode GetLayoutMode(Size clientPhysical, int dpi)
    {
        var logical = GetLogicalClientSize(clientPhysical, dpi);
        return logical.Width >= StandardMinimumWidthDip && logical.Height >= StandardMinimumHeightDip
            ? RemoteLayoutMode.Standard
            : RemoteLayoutMode.Compact;
    }

    public static float GetBodyFontSize(RemoteLayoutMode mode) => 9.5F;

    public static float GetPageTitleFontSize(RemoteLayoutMode mode) =>
        mode == RemoteLayoutMode.Standard ? 19F : 17F;

    public static Size GetClientSizePhysical(RemoteWindowPlacement placement, RemoteScreenMetrics screen)
    {
        var widthDip = placement.HasValue ? Math.Max(MinimumClientWidthDip, placement.WidthDip) : DesignClientWidthDip;
        var heightDip = placement.HasValue ? Math.Max(MinimumClientHeightDip, placement.HeightDip) : DesignClientHeightDip;
        var desired = DipToPhysical(new Size(widthDip, heightDip), screen.Dpi);
        return LimitClientSizeToWorkingArea(desired, screen.WorkingArea);
    }

    public static Size GetMinimumClientSizePhysical(RemoteScreenMetrics screen)
    {
        var desired = DipToPhysical(new Size(MinimumClientWidthDip, MinimumClientHeightDip), screen.Dpi);
        return LimitClientSizeToWorkingArea(desired, screen.WorkingArea);
    }

    public static RemoteScreenMetrics SelectScreen(
        IReadOnlyList<RemoteScreenMetrics> screens,
        string? savedDeviceName,
        string? preferredDeviceName = null)
    {
        if (screens.Count == 0)
            return new RemoteScreenMetrics("", new Rectangle(0, 0, 1280, 720), 96, true);

        if (!string.IsNullOrWhiteSpace(savedDeviceName))
        {
            var saved = screens.FirstOrDefault(screen => string.Equals(
                screen.DeviceName,
                savedDeviceName,
                StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(saved.DeviceName)) return saved;
        }

        if (!string.IsNullOrWhiteSpace(preferredDeviceName))
        {
            var preferred = screens.FirstOrDefault(screen => string.Equals(
                screen.DeviceName,
                preferredDeviceName,
                StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(preferred.DeviceName)) return preferred;
        }

        return screens.FirstOrDefault(screen => screen.Primary) is { DeviceName.Length: > 0 } primary
            ? primary
            : screens[0];
    }

    public static RemoteWindowRestorePlan CreateRestorePlan(
        RemoteWindowPlacement placement,
        IReadOnlyList<RemoteScreenMetrics> screens,
        string? preferredDeviceName,
        Size nonClientSizePhysical)
    {
        var screen = SelectScreen(screens, placement.HasValue ? placement.ScreenDeviceName : null, preferredDeviceName);
        var client = GetClientSizePhysical(placement, screen);
        var outer = new Size(
            Math.Min(screen.WorkingArea.Width, client.Width + Math.Max(0, nonClientSizePhysical.Width)),
            Math.Min(screen.WorkingArea.Height, client.Height + Math.Max(0, nonClientSizePhysical.Height)));
        var leftRatio = placement.HasValue ? ClampRatio(placement.LeftRatio) : 0.5D;
        var topRatio = placement.HasValue ? ClampRatio(placement.TopRatio) : 0.5D;
        var left = screen.WorkingArea.Left + (int)Math.Round(
            Math.Max(0, screen.WorkingArea.Width - outer.Width) * leftRatio,
            MidpointRounding.AwayFromZero);
        var top = screen.WorkingArea.Top + (int)Math.Round(
            Math.Max(0, screen.WorkingArea.Height - outer.Height) * topRatio,
            MidpointRounding.AwayFromZero);
        var bounds = ClampToWorkingArea(new Rectangle(left, top, outer.Width, outer.Height), screen.WorkingArea);
        return new RemoteWindowRestorePlan(screen, client, bounds, placement.HasValue && placement.Maximized);
    }

    public static RemoteWindowPlacement CapturePlacement(
        Rectangle normalWindowBoundsPhysical,
        Size normalClientSizePhysical,
        RemoteScreenMetrics screen,
        bool maximized)
    {
        var availableX = Math.Max(0, screen.WorkingArea.Width - normalWindowBoundsPhysical.Width);
        var availableY = Math.Max(0, screen.WorkingArea.Height - normalWindowBoundsPhysical.Height);
        return new RemoteWindowPlacement
        {
            HasValue = true,
            ScreenDeviceName = screen.DeviceName,
            LeftRatio = availableX == 0
                ? 0D
                : ClampRatio((normalWindowBoundsPhysical.Left - screen.WorkingArea.Left) / (double)availableX),
            TopRatio = availableY == 0
                ? 0D
                : ClampRatio((normalWindowBoundsPhysical.Top - screen.WorkingArea.Top) / (double)availableY),
            WidthDip = Math.Max(MinimumClientWidthDip, PhysicalToDip(normalClientSizePhysical.Width, screen.Dpi)),
            HeightDip = Math.Max(MinimumClientHeightDip, PhysicalToDip(normalClientSizePhysical.Height, screen.Dpi)),
            Maximized = maximized
        };
    }

    public static Rectangle ClampToWorkingArea(Rectangle bounds, Rectangle workingArea)
    {
        var width = Math.Min(Math.Max(1, bounds.Width), Math.Max(1, workingArea.Width));
        var height = Math.Min(Math.Max(1, bounds.Height), Math.Max(1, workingArea.Height));
        var left = Math.Clamp(bounds.Left, workingArea.Left, workingArea.Right - width);
        var top = Math.Clamp(bounds.Top, workingArea.Top, workingArea.Bottom - height);
        return new Rectangle(left, top, width, height);
    }

    public static bool CanSave(FormWindowState state) => state != FormWindowState.Minimized;

    private static Size LimitClientSizeToWorkingArea(Size desired, Rectangle workingArea) => new(
        Math.Max(1, Math.Min(desired.Width, Math.Max(1, workingArea.Width - WorkingAreaSafetyMarginPhysical))),
        Math.Max(1, Math.Min(desired.Height, Math.Max(1, workingArea.Height - WorkingAreaSafetyMarginPhysical))));

    private static double ClampRatio(double value) =>
        double.IsFinite(value) ? Math.Clamp(value, 0D, 1D) : 0.5D;
}

internal static class RemoteScreenDpiProvider
{
    private const uint MonitorDefaultToNearest = 2;

    public static RemoteScreenMetrics FromScreen(Screen screen)
    {
        var center = new Point(
            screen.Bounds.Left + screen.Bounds.Width / 2,
            screen.Bounds.Top + screen.Bounds.Height / 2);
        return new RemoteScreenMetrics(screen.DeviceName, screen.WorkingArea, GetDpi(center), screen.Primary);
    }

    private static int GetDpi(Point point)
    {
        try
        {
            var monitor = MonitorFromPoint(new NativePoint(point.X, point.Y), MonitorDefaultToNearest);
            if (monitor != IntPtr.Zero && GetDpiForMonitor(monitor, 0, out var dpiX, out _) == 0)
                return (int)Math.Max(96, dpiX);
        }
        catch (DllNotFoundException) { }
        catch (EntryPointNotFoundException) { }
        return 96;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct NativePoint(int x, int y)
    {
        public readonly int X = x;
        public readonly int Y = y;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(NativePoint point, uint flags);

    [DllImport("Shcore.dll")]
    private static extern int GetDpiForMonitor(IntPtr monitor, int dpiType, out uint dpiX, out uint dpiY);
}
