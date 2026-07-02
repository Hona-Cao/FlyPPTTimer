using System.Windows.Forms;
using FlyPPTTimer.Native;

namespace FlyPPTTimer.Services;

public sealed class HotkeyService : NativeWindow, IDisposable
{
    private readonly LogService _log;
    private readonly Dictionary<int, Action> _actions = [];

    public HotkeyService(LogService log)
    {
        _log = log;
        CreateHandle(new CreateParams());
    }

    public void RegisterAll(string startPause, string stopReset, string toggle, string settings, Action onStartPause, Action onStopReset, Action onToggle, Action onSettings)
    {
        UnregisterAll();
        Register(1, startPause, onStartPause);
        Register(2, stopReset, onStopReset);
        Register(3, toggle, onToggle);
        Register(4, settings, onSettings);
    }

    public void RegisterAll(Dictionary<string, string> hotkeys, Dictionary<string, Action> actions)
    {
        UnregisterAll();
        var id = 1;
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in hotkeys)
        {
            if (string.IsNullOrWhiteSpace(pair.Value)) continue;
            if (!actions.TryGetValue(pair.Key, out var action)) continue;
            if (!used.Add(pair.Value))
            {
                _log.Warn($"Duplicate hotkey skipped: {pair.Value}");
                continue;
            }
            Register(id++, pair.Value, action);
        }
    }

    private void Register(int id, string hotkey, Action action)
    {
        if (!TryParse(hotkey, out var modifiers, out var key))
        {
            _log.Warn($"Invalid hotkey: {hotkey}");
            return;
        }

        if (NativeMethods.RegisterHotKey(Handle, id, modifiers, (int)key))
        {
            _actions[id] = action;
            _log.Info($"Hotkey registered: {hotkey}");
        }
        else
        {
            _log.Warn($"Hotkey registration failed: {hotkey}");
            MessageBox.Show($"快捷键注册失败：{hotkey}", "FlyPPTTimer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    public void UnregisterAll()
    {
        foreach (var id in _actions.Keys.ToArray())
        {
            NativeMethods.UnregisterHotKey(Handle, id);
        }
        _actions.Clear();
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WmHotkey && _actions.TryGetValue(m.WParam.ToInt32(), out var action))
        {
            action();
            return;
        }
        base.WndProc(ref m);
    }

    private static bool TryParse(string hotkey, out int modifiers, out Keys key)
    {
        modifiers = 0;
        key = Keys.None;
        foreach (var part in hotkey.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (part.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) || part.Equals("Control", StringComparison.OrdinalIgnoreCase)) modifiers |= NativeMethods.ModControl;
            else if (part.Equals("Alt", StringComparison.OrdinalIgnoreCase)) modifiers |= NativeMethods.ModAlt;
            else if (part.Equals("Shift", StringComparison.OrdinalIgnoreCase)) modifiers |= NativeMethods.ModShift;
            else if (part.Equals("Win", StringComparison.OrdinalIgnoreCase)) modifiers |= NativeMethods.ModWin;
            else if (!Enum.TryParse(part, true, out key)) return false;
        }
        return key != Keys.None;
    }

    public void Dispose()
    {
        UnregisterAll();
        DestroyHandle();
    }
}
