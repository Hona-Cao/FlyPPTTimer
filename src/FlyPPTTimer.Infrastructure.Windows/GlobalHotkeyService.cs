using System.Runtime.InteropServices;
using System.Collections.Concurrent;
using FlyPPTTimer.Core.Abstractions;

namespace FlyPPTTimer.Infrastructure.Windows;

public sealed class GlobalHotkeyService : IDisposable
{
    private const uint WmHotkey = 0x0312, WmClose = 0x0010, WmConfigure = 0x8001;
    private readonly ILogService _log;
    private readonly Thread _thread;
    private readonly AutoResetEvent _ready = new(false);
    private readonly Dictionary<int, Action> _actions = [];
    private readonly ConcurrentQueue<Action> _pending = new();
    private readonly WndProc _wndProc;
    private IntPtr _window;

    public GlobalHotkeyService(ILogService log)
    {
        _log = log; _wndProc = WindowProc; _thread = new(ThreadMain) { IsBackground = true, Name = "FlyPPTTimer hotkeys" }; _thread.SetApartmentState(ApartmentState.STA); _thread.Start(); _ready.WaitOne(TimeSpan.FromSeconds(5));
    }
    public void Configure(IReadOnlyDictionary<string, string> bindings, IReadOnlyDictionary<string, Action> actions)
    {
        if (_window == IntPtr.Zero) return;
        var requestedBindings = bindings.ToDictionary(x => x.Key, x => x.Value);
        var requestedActions = actions.ToDictionary(x => x.Key, x => x.Value);
        _pending.Enqueue(() =>
        {
            UnregisterAll(); var id = 100;
            foreach (var pair in requestedBindings)
            {
                if (!requestedActions.TryGetValue(pair.Key, out var action) || !TryParse(pair.Value, out var modifiers, out var key)) continue;
                if (RegisterHotKey(_window, id, modifiers | 0x4000, key)) _actions[id++] = action; else _log.Warn($"快捷键注册失败：{pair.Value}");
            }
        });
        PostMessage(_window, WmConfigure, IntPtr.Zero, IntPtr.Zero);
    }
    private void ThreadMain()
    {
        var className = $"FlyPPTTimerHotkeys_{Environment.ProcessId}"; var module = GetModuleHandle(null);
        var wc = new WndClass { Instance = module, ClassName = className, WindowProcedure = Marshal.GetFunctionPointerForDelegate(_wndProc) }; RegisterClass(ref wc);
        _window = CreateWindowEx(0, className, "", 0, 0, 0, 0, 0, new(-3), IntPtr.Zero, module, IntPtr.Zero); _ready.Set();
        while (GetMessage(out var message, IntPtr.Zero, 0, 0) > 0) { TranslateMessage(ref message); DispatchMessage(ref message); }
    }
    private IntPtr WindowProc(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam)
    {
        if (message == WmHotkey && _actions.TryGetValue(wParam.ToInt32(), out var action)) { try { action(); } catch (Exception ex) { _log.Error("快捷键操作失败。", ex); } return IntPtr.Zero; }
        if (message == WmConfigure) { while (_pending.TryDequeue(out var configure)) configure(); return IntPtr.Zero; }
        if (message == WmClose) { DestroyWindow(hwnd); PostQuitMessage(0); return IntPtr.Zero; }
        return DefWindowProc(hwnd, message, wParam, lParam);
    }
    private void UnregisterAll() { foreach (var id in _actions.Keys) UnregisterHotKey(_window, id); _actions.Clear(); }
    private static bool TryParse(string text, out uint modifiers, out uint key)
    {
        modifiers = 0; key = 0; if (string.IsNullOrWhiteSpace(text)) return false;
        foreach (var part in text.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (part.Equals("Ctrl", StringComparison.OrdinalIgnoreCase)) modifiers |= 2;
            else if (part.Equals("Alt", StringComparison.OrdinalIgnoreCase)) modifiers |= 1;
            else if (part.Equals("Shift", StringComparison.OrdinalIgnoreCase)) modifiers |= 4;
            else if (part.Equals("Win", StringComparison.OrdinalIgnoreCase)) modifiers |= 8;
            else if (part.Length > 1 && part[0] is 'F' or 'f' && int.TryParse(part[1..], out var f) && f is >= 1 and <= 24) key = (uint)(0x70 + f - 1);
            else if (part.Equals("Up", StringComparison.OrdinalIgnoreCase)) key = 0x26;
            else if (part.Equals("Down", StringComparison.OrdinalIgnoreCase)) key = 0x28;
            else if (part.Length == 1) key = char.ToUpperInvariant(part[0]);
            else return false;
        }
        return key != 0;
    }
    public void Dispose() { if (_window != IntPtr.Zero) { UnregisterAll(); PostMessage(_window, WmClose, IntPtr.Zero, IntPtr.Zero); _thread.Join(TimeSpan.FromSeconds(2)); } _ready.Dispose(); }

    private delegate IntPtr WndProc(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] private struct WndClass { public uint Style; public IntPtr WindowProcedure; public int ClassExtra, WindowExtra; public IntPtr Instance, Icon, Cursor, Background; public string? MenuName, ClassName; }
    [StructLayout(LayoutKind.Sequential)] private struct Msg { public IntPtr Hwnd; public uint Message; public IntPtr WParam, LParam; public uint Time; public int X, Y; }
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern ushort RegisterClass(ref WndClass value);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr CreateWindowEx(uint exStyle, string className, string title, uint style, int x, int y, int width, int height, IntPtr parent, IntPtr menu, IntPtr instance, IntPtr parameter);
    [DllImport("user32.dll")] private static extern bool DestroyWindow(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern IntPtr DefWindowProc(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] private static extern sbyte GetMessage(out Msg message, IntPtr hwnd, uint min, uint max);
    [DllImport("user32.dll")] private static extern bool TranslateMessage(ref Msg message);
    [DllImport("user32.dll")] private static extern IntPtr DispatchMessage(ref Msg message);
    [DllImport("user32.dll")] private static extern void PostQuitMessage(int code);
    [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint key);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hwnd, int id);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr GetModuleHandle(string? name);
}
