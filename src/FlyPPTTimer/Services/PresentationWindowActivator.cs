using System.Runtime.InteropServices;
using FlyPPTTimer.Native;

namespace FlyPPTTimer.Services;

internal readonly record struct NativeWindowCallResult(bool Success, int Error);

internal interface IPresentationWindowNativeApi
{
    NativeWindowCallResult ShowMaximized(IntPtr hwnd);
    NativeWindowCallResult BringToTop(IntPtr hwnd);
    NativeWindowCallResult SetForeground(IntPtr hwnd);
    void PulseTopmost(IntPtr hwnd);
    bool IsMaximized(IntPtr hwnd);
}

internal sealed class WindowsPresentationWindowNativeApi : IPresentationWindowNativeApi
{
    public NativeWindowCallResult ShowMaximized(IntPtr hwnd)
    {
        var success = NativeMethods.ShowWindow(hwnd, NativeMethods.SwMaximize);
        return new NativeWindowCallResult(success, Marshal.GetLastWin32Error());
    }

    public NativeWindowCallResult BringToTop(IntPtr hwnd)
    {
        var success = NativeMethods.BringWindowToTop(hwnd);
        return new NativeWindowCallResult(success, Marshal.GetLastWin32Error());
    }

    public NativeWindowCallResult SetForeground(IntPtr hwnd)
    {
        var success = NativeMethods.SetForegroundWindow(hwnd);
        return new NativeWindowCallResult(success, Marshal.GetLastWin32Error());
    }

    public void PulseTopmost(IntPtr hwnd)
    {
        var flags = NativeMethods.SwpNoMove | NativeMethods.SwpNoSize | NativeMethods.SwpShowWindow;
        NativeMethods.SetWindowPos(hwnd, NativeMethods.HwndTopmost, 0, 0, 0, 0, flags);
        NativeMethods.SetWindowPos(hwnd, NativeMethods.HwndNoTopmost, 0, 0, 0, 0, flags);
    }

    public bool IsMaximized(IntPtr hwnd) => NativeMethods.IsZoomed(hwnd);
}

internal sealed record PresentationWindowActivationResult(
    bool Success,
    string Message,
    string Path,
    IntPtr Hwnd)
{
    public static PresentationWindowActivationResult Succeeded(string message, string path, IntPtr hwnd) =>
        new(true, message, path, hwnd);

    public static PresentationWindowActivationResult Failed(string message, string path, IntPtr hwnd) =>
        new(false, $"；{message}", path, hwnd);
}

/// <summary>
/// Applies the Windows maximize/foreground policy without knowing anything about
/// PowerPoint or WPS COM objects. The provider remains responsible for locating
/// the correct HWND; this class only activates the supplied window.
/// </summary>
internal sealed class PresentationWindowActivator
{
    private readonly IPresentationWindowNativeApi _native;
    private readonly Action<string>? _warn;

    public PresentationWindowActivator(
        IPresentationWindowNativeApi? native = null,
        Action<string>? warn = null)
    {
        _native = native ?? new WindowsPresentationWindowNativeApi();
        _warn = warn;
    }

    public PresentationWindowActivationResult Activate(
        IntPtr hwnd,
        string path,
        string label,
        string failurePrefix = "；文稿已打开但最大化或置前失败")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        if (hwnd == IntPtr.Zero)
            return PresentationWindowActivationResult.Failed($"未找到目标{label}", path, hwnd);

        var show = _native.ShowMaximized(hwnd);
        var bring = _native.BringToTop(hwnd);
        var foreground = _native.SetForeground(hwnd);

        if (!bring.Success || !foreground.Success)
        {
            _native.PulseTopmost(hwnd);
            bring = _native.BringToTop(hwnd);
            foreground = _native.SetForeground(hwnd);
        }

        if (_native.IsMaximized(hwnd))
        {
            var message = bring.Success && foreground.Success
                ? "；已最大化并置前"
                : "；已最大化（Windows 未允许强制置前）";
            return PresentationWindowActivationResult.Succeeded(message, path, hwnd);
        }

        var detail = $"{label} HWND=0x{hwnd.ToInt64():X}; " +
                     $"ShowWindow={show.Success}/错误{show.Error}; " +
                     $"BringWindowToTop={bring.Success}/错误{bring.Error}; " +
                     $"SetForegroundWindow={foreground.Success}/错误{foreground.Error}";
        _warn?.Invoke($"Presentation window activation incomplete: path={path}; {detail}");
        return PresentationWindowActivationResult.Failed(failurePrefix + $"（{detail}）", path, hwnd);
    }
}
