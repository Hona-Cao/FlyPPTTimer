using System.Diagnostics;
using FlyPPTTimer.Forms;
using FlyPPTTimer.Models;
using FlyPPTTimer.Native;
using FlyPPTTimer.Services;

namespace FlyPPTTimer;

public sealed class FlyPPTTimerContext : ApplicationContext
{
    private readonly LogService _log = new();
    private readonly ConfigService _configService;
    private readonly TimerService _timer;
    private readonly AlertService _alerts;
    private readonly FullscreenDetector _fullscreen;
    private readonly HotkeyService _hotkeys;
    private readonly NetworkAddressService _networkAddresses = new();
    private readonly AppCommandService _commands;
    private readonly RemoteControlService _remoteControl;
    private readonly NotifyIcon _tray;
    private readonly ContextMenuStrip _overlayMenu;
    private readonly ContextMenuStrip _trayMenu;
    private readonly List<TimerOverlayForm> _overlays = [];
    private readonly System.Windows.Forms.Timer _screenTimer = new() { Interval = 1500 };
    private readonly System.Windows.Forms.Timer _menuCloseTimer = new() { Interval = 40 };
    private readonly Form _menuOwner = new()
    {
        ShowInTaskbar = false,
        FormBorderStyle = FormBorderStyle.None,
        StartPosition = FormStartPosition.Manual,
        Size = new Size(1, 1),
        Opacity = 0,
        Location = new Point(-32000, -32000)
    };
    private ContextMenuStrip? _activeMenu;
    private SettingsForm? _settings;
    private RemoteControlForm? _remoteControlWindow;
    private AppConfig _config;
    private bool _autoStartedFromFullscreen;
    private string _screenSignature = "";

    public FlyPPTTimerContext()
    {
        _log.Info("Application started.");
        _configService = new ConfigService(_log);
        _config = _configService.Load();
        _timer = new TimerService(_log);
        _alerts = new AlertService(_log);
        _fullscreen = new FullscreenDetector(_log);
        _hotkeys = new HotkeyService(_log);
        _overlayMenu = BuildCommandMenu();
        _trayMenu = BuildCommandMenu();
        _commands = new AppCommandService(
            _timer,
            _alerts,
            () => _config,
            SaveConfigOnly,
            ShowOverlay,
            HideOverlay,
            ToggleOverlay,
            () => _overlays.Any(x => x.Visible),
            FlashOverlay,
            ShowSettings,
            _log);
        _remoteControl = new RemoteControlService(() => _config, SaveConfigOnly, _commands, _log);

        _timer.Configure(_config);
        RebuildOverlays();

        _timer.Updated += (_, snapshot) =>
        {
            foreach (var overlay in _overlays) overlay.UpdateTime(snapshot);
            _alerts.CheckPrompts(_config, snapshot);
        };
        _timer.Finished += (_, snapshot) => _alerts.TriggerEnd(_config, snapshot);
        _alerts.PromptVisualRequested += (_, prompt) =>
        {
            foreach (var overlay in _overlays) overlay.Flash(prompt, prompt is EndPromptSettings end ? end.FlashSeconds : 3);
        };
        _fullscreen.Configure(_config);
        _fullscreen.StateChanged += OnFullscreenChanged;
        _fullscreen.Start();
        if (_config.RemoteControl.Enabled) _remoteControl.Start();
        _screenSignature = GetScreenSignature();
        _screenTimer.Tick += (_, _) => CheckScreenChanges();
        _screenTimer.Start();
        _menuCloseTimer.Tick += (_, _) => CloseMenuIfClickedOutside();

        _tray = new NotifyIcon
        {
            Text = "演讲计时器",
            Icon = LoadAppIcon(),
            Visible = true
        };
        _tray.DoubleClick += (_, _) => ShowSettings();
        _tray.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Right) ShowCommandMenuAtCursor(_trayMenu);
        };

        RegisterHotkeys();
        foreach (var overlay in _overlays.Where(x => _config.Placement.Visible)) overlay.Show();
    }

    private ContextMenuStrip BuildCommandMenu()
    {
        var menu = new ContextMenuStrip
        {
            Renderer = new ModernContextMenuRenderer(),
            BackColor = Color.White,
            ForeColor = Color.FromArgb(32, 46, 52),
            Padding = new Padding(6),
            AutoClose = true
        };
        menu.Opened += (_, _) =>
        {
            ModernTheme.ApplyRoundedRegion(menu, 8);
            _activeMenu = menu;
            _menuCloseTimer.Start();
        };
        menu.Items.Add("开始/暂停", null, (_, _) => _commands.StartOrPause());
        menu.Items.Add("停止/重置", null, (_, _) => _commands.StopReset());
        menu.Items.Add("显示/隐藏计时窗口", null, (_, _) => _commands.ToggleOverlay());
        menu.Items.Add("触发闪烁", null, (_, _) => _commands.FlashOverlay());
        menu.Items.Add("静音/取消静音", null, (_, _) => _commands.ToggleMute());
        menu.Items.Add("远程控制", null, (_, _) => ShowRemoteControl());
        menu.Items.Add("设置", null, (_, _) => _commands.OpenSettings());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => Exit());
        return menu;
    }

    private void ShowCommandMenuAtCursor(ContextMenuStrip menu)
    {
        _overlayMenu.Close();
        _trayMenu.Close();
        _activeMenu = menu;
        if (!_menuOwner.IsHandleCreated) _menuOwner.CreateControl();
        menu.PerformLayout();
        var menuSize = menu.GetPreferredSize(Size.Empty);
        if (menuSize.Width <= 0 || menuSize.Height <= 0) menuSize = new Size(220, 320);
        var cursor = Cursor.Position;
        var area = Screen.FromPoint(cursor).WorkingArea;
        var x = Math.Clamp(cursor.X - menuSize.Width, area.Left, Math.Max(area.Left, area.Right - menuSize.Width));
        var y = Math.Clamp(cursor.Y - menuSize.Height, area.Top, Math.Max(area.Top, area.Bottom - menuSize.Height));
        _menuOwner.Location = new Point(x, y);
        NativeMethods.SetForegroundWindow(_menuOwner.Handle);
        menu.Show(_menuOwner, Point.Empty);
        NativeMethods.PostMessage(_menuOwner.Handle, 0, IntPtr.Zero, IntPtr.Zero);
        _menuCloseTimer.Start();
    }

    private void CloseMenuIfClickedOutside()
    {
        if (_activeMenu is not { Visible: true } menu)
        {
            _menuCloseTimer.Stop();
            _activeMenu = null;
            return;
        }

        if (Control.MouseButtons == MouseButtons.None) return;
        if (menu.Bounds.Contains(Cursor.Position)) return;
        menu.Close(ToolStripDropDownCloseReason.AppClicked);
        _menuCloseTimer.Stop();
        _activeMenu = null;
    }

    private void RegisterHotkeys()
    {
        var actions = new Dictionary<string, Action>
        {
            ["startPause"] = _commands.StartOrPause,
            ["start"] = _commands.Start,
            ["pause"] = _commands.Pause,
            ["resume"] = _commands.Resume,
            ["stopReset"] = _commands.StopReset,
            ["stop"] = _commands.Stop,
            ["reset"] = _commands.Reset,
            ["toggleWindow"] = _commands.ToggleOverlay,
            ["showWindow"] = _commands.ShowOverlay,
            ["hideWindow"] = _commands.HideOverlay,
            ["flash"] = _commands.FlashOverlay,
            ["toggleMute"] = _commands.ToggleMute,
            ["toggleMode"] = _commands.ToggleMode,
            ["addMinute"] = () => _commands.AddDuration(TimeSpan.FromMinutes(1)),
            ["subtractMinute"] = () => _commands.AddDuration(TimeSpan.FromMinutes(-1)),
            ["preset3"] = () => _commands.SetPresetDuration(TimeSpan.FromMinutes(3)),
            ["preset5"] = () => _commands.SetPresetDuration(TimeSpan.FromMinutes(5)),
            ["preset8"] = () => _commands.SetPresetDuration(TimeSpan.FromMinutes(8)),
            ["preset10"] = () => _commands.SetPresetDuration(TimeSpan.FromMinutes(10)),
            ["preset15"] = () => _commands.SetPresetDuration(TimeSpan.FromMinutes(15)),
            ["openSettings"] = _commands.OpenSettings
        };
        _hotkeys.RegisterAll(_config.Controls.Hotkeys, actions);
    }

    private void ToggleOverlay()
    {
        _config.Placement.Visible = !_overlays.Any(x => x.Visible);
        foreach (var overlay in _overlays) overlay.Visible = _config.Placement.Visible;
        _configService.Save(_config);
    }

    private void ShowOverlay()
    {
        _config.Placement.Visible = true;
        foreach (var overlay in _overlays) overlay.Visible = true;
        _configService.Save(_config);
    }

    private void HideOverlay()
    {
        _config.Placement.Visible = false;
        foreach (var overlay in _overlays) overlay.Visible = false;
        _configService.Save(_config);
    }

    private void FlashOverlay(int seconds)
    {
        var prompt = new PromptSettings { Enabled = true, FlashBackground = true, Text = "闪烁测试" };
        foreach (var overlay in _overlays) overlay.Flash(prompt, seconds);
    }

    private void ShowSettings()
    {
        if (_settings is null || _settings.IsDisposed)
        {
            _settings = new SettingsForm(_config, _remoteControl, _networkAddresses);
            _settings.ConfigApplied += (_, cfg) => ApplyConfig(cfg);
            _settings.ResetRequested += (_, _) => ApplyConfig(new AppConfig());
            _settings.ExportRequested += (_, _) => ExportConfig();
            _settings.ImportRequested += (_, _) => ImportConfig();
            _settings.OpenConfigRequested += (_, _) => OpenPath(Path.GetDirectoryName(AppPaths.ConfigPath)!);
            _settings.OpenLogRequested += (_, _) => OpenPath(AppPaths.LogDirectory);
        }
        _settings.Show();
        _settings.Activate();
    }

    private void ShowRemoteControl()
    {
        _config.RemoteControl.Enabled = true;
        SaveConfigOnly(_config);
        if (!_remoteControl.IsRunning) _remoteControl.Start();
        if (_remoteControlWindow is null || _remoteControlWindow.IsDisposed)
        {
            _remoteControlWindow = new RemoteControlForm(_config, _remoteControl, _networkAddresses, SaveConfigOnly);
        }

        _remoteControlWindow.Show();
        _remoteControlWindow.Activate();
    }

    private void ApplyConfig(AppConfig config)
    {
        _config = config;
        _timer.Configure(_config);
        RebuildOverlays();
        _fullscreen.Configure(_config);
        _configService.Save(_config);
        if (_config.RemoteControl.Enabled)
        {
            if (!_remoteControl.IsRunning) _remoteControl.Start();
        }
        else
        {
            _remoteControl.Stop();
        }
        RegisterHotkeys();
    }

    private void SaveConfigOnly(AppConfig config)
    {
        _config = config;
        _configService.Save(_config);
    }

    private void ExportConfig()
    {
        using var dialog = new SaveFileDialog { Filter = "JSON (*.json)|*.json", FileName = "FlyPPTTimer.config.json" };
        if (dialog.ShowDialog() == DialogResult.OK) _configService.Export(_config, dialog.FileName);
    }

    private void ImportConfig()
    {
        using var dialog = new OpenFileDialog { Filter = "JSON (*.json)|*.json" };
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            ApplyConfig(_configService.Import(dialog.FileName));
        }
    }

    private void OpenPath(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
            Process.Start(new ProcessStartInfo("explorer.exe", path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _log.Error($"Failed to open path: {path}", ex);
        }
    }

    private void OnFullscreenChanged(object? sender, FullscreenState state)
    {
        if (state.IsFullscreen && _config.Behavior.AutoStartOnFullscreen)
        {
            _alerts.ResetTriggers();
            ApplyPresentationRuleDuration(state.PresentationPath);
            _autoStartedFromFullscreen = true;
            _commands.StopReset();
            _commands.Start();
        }
        else if (!state.IsFullscreen && _autoStartedFromFullscreen && _config.Behavior.StopWhenLeavingFullscreen && _timer.State is TimerState.Running or TimerState.Paused)
        {
            _commands.StopReset();
            _autoStartedFromFullscreen = false;
        }
    }

    private void RebuildOverlays()
    {
        foreach (var overlay in _overlays)
        {
            overlay.PositionChangedByUser -= OverlayMoved;
            overlay.Close();
            overlay.Dispose();
        }
        _overlays.Clear();

        var screens = GetTargetScreens();
        foreach (var screen in screens)
        {
            var overlay = new TimerOverlayForm(_overlayMenu, ShowCommandMenuAtCursor);
            overlay.PositionChangedByUser += OverlayMoved;
            overlay.ApplyConfig(_config, screen);
            overlay.UpdateTime(_timer.CreateSnapshot());
            _overlays.Add(overlay);
            if (_config.Placement.Visible) overlay.Show();
        }
    }

    private IEnumerable<Screen> GetTargetScreens()
    {
        if (_config.Placement.ShowOnAllScreens)
        {
            return Screen.AllScreens.OrderBy(x => x.Bounds.Left).ThenBy(x => x.Bounds.Top).ToArray();
        }

        var target = Screen.AllScreens.FirstOrDefault(x => string.Equals(x.DeviceName, _config.Placement.TargetScreenDeviceName, StringComparison.OrdinalIgnoreCase));
        return new[] { target ?? Screen.PrimaryScreen! };
    }

    private void OverlayMoved(object? sender, OverlayMovedEventArgs e)
    {
        var basePoint = TimerOverlayForm.CalculateLocation(e.Screen, ((TimerOverlayForm)sender!).Size, _config.Placement.Anchor, 0, 0);
        var area = e.Screen.WorkingArea;
        _config.Placement.OffsetXPercent = Math.Round((decimal)(e.Location.X - basePoint.X) * 100m / Math.Max(1, area.Width), 2);
        _config.Placement.OffsetYPercent = Math.Round((decimal)(e.Location.Y - basePoint.Y) * 100m / Math.Max(1, area.Height), 2);
        _config.Placement.TargetScreenDeviceName = e.Screen.DeviceName;
        _config.Placement.ScreenDeviceName = e.Screen.DeviceName;
        _config.Placement.X = e.Location.X;
        _config.Placement.Y = e.Location.Y;
        _config.Placement.HasCustomPlacement = true;
    }

    private void ApplyPresentationRuleDuration(string presentationPath)
    {
        var durationText = _config.Timer.DefaultDuration;
        if (!string.IsNullOrWhiteSpace(presentationPath))
        {
            var matched = _config.Rules.FirstOrDefault(rule =>
                rule.Enabled
                && !string.IsNullOrWhiteSpace(rule.FilePath)
                && SameFilePath(rule.FilePath, presentationPath));
            if (matched is not null) durationText = matched.Duration;
        }

        if (TimeSpan.TryParse(durationText, out var duration))
        {
            _timer.SetDuration(duration);
        }
    }

    private static bool SameFilePath(string left, string right)
    {
        try
        {
            return string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }
    }

    private void CheckScreenChanges()
    {
        var signature = GetScreenSignature();
        if (signature == _screenSignature) return;
        _screenSignature = signature;
        _log.Info($"Screen layout changed: {signature}");
        RebuildOverlays();
    }

    private static string GetScreenSignature()
    {
        return string.Join("|", Screen.AllScreens.Select(s => $"{s.DeviceName}:{s.Bounds.Left},{s.Bounds.Top},{s.Bounds.Width},{s.Bounds.Height}:{s.Primary}"));
    }

    private Icon LoadAppIcon()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "app.ico");
        if (File.Exists(iconPath))
        {
            try { return new Icon(iconPath); }
            catch (Exception ex) { _log.Error("Failed to load app icon.", ex); }
        }
        return SystemIcons.Application;
    }

    private void Exit()
    {
        if (_overlays.FirstOrDefault() is { } overlay)
        {
            _config.Placement.X = overlay.Left;
            _config.Placement.Y = overlay.Top;
            _config.Placement.Visible = overlay.Visible;
        }
        _configService.Save(_config);
        _log.Info("Application exiting.");
        _tray.Visible = false;
        _tray.Dispose();
        _trayMenu.Dispose();
        _menuOwner.Dispose();
        _hotkeys.Dispose();
        _fullscreen.Dispose();
        _remoteControl.Dispose();
        _screenTimer.Dispose();
        _menuCloseTimer.Dispose();
        foreach (var timerWindow in _overlays) timerWindow.Dispose();
        _settings?.Dispose();
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _tray.Dispose();
            _trayMenu.Dispose();
            _menuOwner.Dispose();
            _hotkeys.Dispose();
            _fullscreen.Dispose();
            _screenTimer.Dispose();
            _menuCloseTimer.Dispose();
            foreach (var overlay in _overlays) overlay.Dispose();
            _settings?.Dispose();
        }
        base.Dispose(disposing);
    }
}
