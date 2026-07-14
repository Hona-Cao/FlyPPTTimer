using System.Runtime.InteropServices;
using FlyPPTTimer.Core.Models;
using Windows.Graphics;

namespace FlyPPTTimer.WinUI;

public sealed class OverlayManager(MainViewModel viewModel, Func<AppConfig> getConfig) : IDisposable
{
    private readonly List<TimerOverlayWindow> _windows = [];
    public bool IsVisible => _windows.Count > 0;
    public void Rebuild()
    {
        DisposeWindows(); var config = getConfig(); if (!config.Placement.Visible) return;
        var monitors = EnumerateMonitors(); if (!config.Placement.ShowOnAllScreens && monitors.Count > 0) monitors = [monitors[0]];
        foreach (var monitor in monitors) _windows.Add(new(viewModel, config, monitor));
    }
    public void Toggle()
    {
        if (IsVisible) DisposeWindows();
        else Rebuild();
    }
    public void Flash(PromptSettings? prompt = null) { foreach (var window in _windows) window.Flash(prompt); }
    private static List<RectInt32> EnumerateMonitors()
    {
        var result = new List<RectInt32>(); EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (monitor, _, _, _) => { var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() }; if (GetMonitorInfo(monitor, ref info)) result.Add(new(info.Work.Left, info.Work.Top, info.Work.Right - info.Work.Left, info.Work.Bottom - info.Work.Top)); return true; }, IntPtr.Zero); return result;
    }
    private void DisposeWindows() { foreach (var window in _windows) window.Close(); _windows.Clear(); }
    public void Dispose() => DisposeWindows();
    private delegate bool MonitorCallback(IntPtr monitor, IntPtr hdc, IntPtr rect, IntPtr data);
    [DllImport("user32.dll")] private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, MonitorCallback callback, IntPtr data);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] private struct MonitorInfo { public int Size; public NativeRect Monitor; public NativeRect Work; public int Flags; }
}
