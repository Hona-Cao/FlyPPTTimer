using FlyPPTTimer.Models;
using FlyPPTTimer.Native;
using System.Runtime.InteropServices;

namespace FlyPPTTimer.Services;

public sealed class FullscreenDetector : IDisposable
{
    private readonly LogService _log;
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 500 };
    private bool _lastFullscreen;
    private string _lastProcess = "";
    private string _lastPresentationPath = "";
    private AppConfig _config = new();

    public FullscreenDetector(LogService log)
    {
        _log = log;
        _timer.Tick += (_, _) => Check();
    }

    public event EventHandler<FullscreenState>? StateChanged;

    public void Configure(AppConfig config) => _config = config;
    public void Start() => _timer.Start();
    public void Stop() => _timer.Stop();

    private void Check()
    {
        var state = DetectPresentationState();
        if (state.IsFullscreen != _lastFullscreen
            || !string.Equals(state.ProcessName, _lastProcess, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(state.PresentationPath, _lastPresentationPath, StringComparison.OrdinalIgnoreCase))
        {
            _lastFullscreen = state.IsFullscreen;
            _lastProcess = state.ProcessName;
            _lastPresentationPath = state.PresentationPath;
            _log.Info($"Fullscreen state changed: fullscreen={state.IsFullscreen}, process={state.ProcessName}");
            StateChanged?.Invoke(this, state);
        }
    }

    private FullscreenState DetectPresentationState()
    {
        var powerPointPath = GetPowerPointSlideShowPath();
        if (powerPointPath is not null)
        {
            return new FullscreenState(true, "POWERPNT.EXE", powerPointPath);
        }

        FullscreenState? matched = null;
        NativeMethods.EnumWindows((hwnd, _) =>
        {
            if (!NativeMethods.IsWindowVisible(hwnd)) return true;
            var process = NativeMethods.GetProcessName(hwnd);
            if (string.IsNullOrWhiteSpace(process)) return true;
            var whitelisted = _config.Behavior.FullscreenProcessWhitelist.Any(x => string.Equals(x, process, StringComparison.OrdinalIgnoreCase));
            if (!whitelisted || !IsFullscreen(hwnd)) return true;

            matched = new FullscreenState(true, process, "");
            return false;
        }, IntPtr.Zero);

        if (matched is not null) return matched;

        var foreground = NativeMethods.GetForegroundWindow();
        var foregroundProcess = foreground == IntPtr.Zero ? "" : NativeMethods.GetProcessName(foreground);
        return new FullscreenState(false, foregroundProcess, "");
    }

    private static bool IsFullscreen(IntPtr hwnd)
    {
        if (!NativeMethods.GetWindowRect(hwnd, out var rect)) return false;
        var screen = Screen.FromHandle(hwnd);
        var b = screen.Bounds;
        return rect.Left <= b.Left + 2
            && rect.Top <= b.Top + 2
            && rect.Right >= b.Right - 2
            && rect.Bottom >= b.Bottom - 2;
    }

    private string? GetPowerPointSlideShowPath()
    {
        try
        {
            if (CLSIDFromProgID("PowerPoint.Application", out var clsid) != 0) return null;
            GetActiveObject(ref clsid, IntPtr.Zero, out var appObject);
            try
            {
                dynamic app = appObject;
                if (app.SlideShowWindows.Count <= 0) return null;
                try
                {
                    return (string)app.SlideShowWindows[1].Presentation.FullName;
                }
                catch
                {
                    try { return (string)app.ActivePresentation.FullName; }
                    catch { return ""; }
                }
            }
            finally
            {
                if (Marshal.IsComObject(appObject)) Marshal.ReleaseComObject(appObject);
            }
        }
        catch
        {
            return null;
        }
    }

    [DllImport("ole32.dll", CharSet = CharSet.Unicode)]
    private static extern int CLSIDFromProgID(string lpszProgID, out Guid lpclsid);

    [DllImport("oleaut32.dll", PreserveSig = false)]
    private static extern void GetActiveObject(ref Guid rclsid, IntPtr pvReserved, [MarshalAs(UnmanagedType.IUnknown)] out object ppunk);

    public void Dispose() => _timer.Dispose();
}

public sealed record FullscreenState(bool IsFullscreen, string ProcessName, string PresentationPath);
