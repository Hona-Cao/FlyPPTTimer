using FlyPPTTimer.Models;
using FlyPPTTimer.Native;

namespace FlyPPTTimer.Services;

public sealed class FullscreenDetector : IDisposable
{
    private readonly Func<PresentationState> _getPresentationState;
    private readonly LogService _log;
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 500 };
    private FullscreenState _last = new(false, "", "");
    private AppConfig _config = new();

    public FullscreenDetector(Func<PresentationState> getPresentationState, LogService log)
    {
        _getPresentationState = getPresentationState;
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
        if (state == _last) return;
        _last = state;
        _log.Info($"Fullscreen state changed: fullscreen={state.IsFullscreen}, process={state.ProcessName}");
        StateChanged?.Invoke(this, state);
    }

    private FullscreenState DetectPresentationState()
    {
        var presentation = _getPresentationState();
        if (presentation.IsSlideShowRunning)
            return new FullscreenState(true, "POWERPNT.EXE", presentation.PresentationPath);

        FullscreenState? matched = null;
        NativeMethods.EnumWindows((hwnd, _) =>
        {
            if (!NativeMethods.IsWindowVisible(hwnd)) return true;
            var process = NativeMethods.GetProcessName(hwnd);
            if (string.IsNullOrWhiteSpace(process) || process.Equals("POWERPNT.EXE", StringComparison.OrdinalIgnoreCase)) return true;
            var whitelisted = _config.Behavior.FullscreenProcessWhitelist.Any(x => string.Equals(x, process, StringComparison.OrdinalIgnoreCase));
            if (!whitelisted || !IsFullscreen(hwnd)) return true;
            matched = new FullscreenState(true, process, "");
            return false;
        }, IntPtr.Zero);

        if (matched is not null) return matched;
        var foreground = NativeMethods.GetForegroundWindow();
        return new FullscreenState(false, foreground == IntPtr.Zero ? "" : NativeMethods.GetProcessName(foreground), "");
    }

    private static bool IsFullscreen(IntPtr hwnd)
    {
        if (!NativeMethods.GetWindowRect(hwnd, out var rect)) return false;
        var bounds = Screen.FromHandle(hwnd).Bounds;
        return rect.Left <= bounds.Left + 2 && rect.Top <= bounds.Top + 2
            && rect.Right >= bounds.Right - 2 && rect.Bottom >= bounds.Bottom - 2;
    }

    public void Dispose() => _timer.Dispose();
}

public sealed record FullscreenState(bool IsFullscreen, string ProcessName, string PresentationPath);
