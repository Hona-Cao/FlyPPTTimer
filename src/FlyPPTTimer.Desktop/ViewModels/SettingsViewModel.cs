using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FlyPPTTimer.Models;
using FlyPPTTimer.Services;

namespace FlyPPTTimer.Desktop.ViewModels;

public sealed record SettingsChoice<T>(T Value, string Label);

public sealed class SettingsViewModel : INotifyPropertyChanged
{
    private readonly AppConfig _config;
    private readonly ConfigService _configService;
    private string _defaultDuration;
    private TimerMode _selectedTimerMode;
    private bool _continueOvertime;
    private TimerEndAction _selectedEndAction;
    private bool _autoStartOnFullscreen;
    private bool _stopWhenLeavingFullscreen;
    private bool _resetWhenLeavingFullscreen;
    private bool _flashOnPauseResume;
    private bool _flashPausedTime;
    private string _fontSizeText;
    private string _widthText;
    private string _heightText;
    private string _backgroundOpacityText;
    private string _textOpacityText;
    private bool _alwaysOnTop;
    private bool _borderless;
    private string _startPauseHotkey;
    private string _stopResetHotkey;
    private string _toggleWindowHotkey;
    private bool _minimizeToTray;
    private bool _checkOnStartup;
    private bool _isDirty;
    private string _errorMessage = "";

    public SettingsViewModel(AppConfig config, ConfigService configService)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));

        _defaultDuration = config.Timer.DefaultDuration;
        _selectedTimerMode = config.Timer.Mode;
        _continueOvertime = config.Timer.ContinueOvertime;
        _selectedEndAction = config.Timer.EndAction;
        _autoStartOnFullscreen = config.Behavior.AutoStartOnFullscreen;
        _stopWhenLeavingFullscreen = config.Behavior.StopWhenLeavingFullscreen;
        _resetWhenLeavingFullscreen = config.Behavior.ResetWhenLeavingFullscreen;
        _flashOnPauseResume = config.Behavior.FlashOnPauseResume;
        _flashPausedTime = config.Behavior.FlashPausedTime;
        _fontSizeText = config.Appearance.FontSize.ToString(CultureInfo.InvariantCulture);
        _widthText = config.Appearance.Width.ToString(CultureInfo.InvariantCulture);
        _heightText = config.Appearance.Height.ToString(CultureInfo.InvariantCulture);
        _backgroundOpacityText = config.Appearance.BackgroundOpacity.ToString(CultureInfo.InvariantCulture);
        _textOpacityText = config.Appearance.TextOpacity.ToString(CultureInfo.InvariantCulture);
        _alwaysOnTop = config.Appearance.AlwaysOnTop;
        _borderless = config.Appearance.Borderless;
        _startPauseHotkey = config.Controls.StartPauseHotkey;
        _stopResetHotkey = config.Controls.StopResetHotkey;
        _toggleWindowHotkey = config.Controls.ToggleWindowHotkey;
        _minimizeToTray = config.Controls.MinimizeToTray;
        _checkOnStartup = config.Update.CheckOnStartup;

        SaveAndCloseCommand = new RelayCommand(() =>
        {
            if (TrySave()) CloseRequested?.Invoke(false);
        });
        CancelCommand = new RelayCommand(() => CloseRequested?.Invoke(true));
    }

    public IReadOnlyList<SettingsChoice<TimerMode>> TimerModeOptions { get; } =
    [
        new(TimerMode.Countdown, "倒计时"),
        new(TimerMode.CountUp, "正计时")
    ];

    public IReadOnlyList<SettingsChoice<TimerEndAction>> EndActionOptions { get; } =
    [
        new(TimerEndAction.None, "无"),
        new(TimerEndAction.BlackScreen, "黑屏"),
        new(TimerEndAction.ExitSlideShow, "结束放映")
    ];

    public ICommand SaveAndCloseCommand { get; }
    public ICommand CancelCommand { get; }
    public event Action<bool>? CloseRequested;

    public string DefaultDuration { get => _defaultDuration; set => Set(ref _defaultDuration, value); }
    public TimerMode SelectedTimerMode { get => _selectedTimerMode; set => Set(ref _selectedTimerMode, value); }
    public bool ContinueOvertime { get => _continueOvertime; set => Set(ref _continueOvertime, value); }
    public TimerEndAction SelectedEndAction { get => _selectedEndAction; set => Set(ref _selectedEndAction, value); }
    public bool AutoStartOnFullscreen { get => _autoStartOnFullscreen; set => Set(ref _autoStartOnFullscreen, value); }
    public bool StopWhenLeavingFullscreen { get => _stopWhenLeavingFullscreen; set => Set(ref _stopWhenLeavingFullscreen, value); }
    public bool ResetWhenLeavingFullscreen { get => _resetWhenLeavingFullscreen; set => Set(ref _resetWhenLeavingFullscreen, value); }
    public bool FlashOnPauseResume { get => _flashOnPauseResume; set => Set(ref _flashOnPauseResume, value); }
    public bool FlashPausedTime { get => _flashPausedTime; set => Set(ref _flashPausedTime, value); }
    public string FontSizeText { get => _fontSizeText; set => Set(ref _fontSizeText, value); }
    public string WidthText { get => _widthText; set => Set(ref _widthText, value); }
    public string HeightText { get => _heightText; set => Set(ref _heightText, value); }
    public string BackgroundOpacityText { get => _backgroundOpacityText; set => Set(ref _backgroundOpacityText, value); }
    public string TextOpacityText { get => _textOpacityText; set => Set(ref _textOpacityText, value); }
    public bool AlwaysOnTop { get => _alwaysOnTop; set => Set(ref _alwaysOnTop, value); }
    public bool Borderless { get => _borderless; set => Set(ref _borderless, value); }
    public string StartPauseHotkey { get => _startPauseHotkey; set => Set(ref _startPauseHotkey, value); }
    public string StopResetHotkey { get => _stopResetHotkey; set => Set(ref _stopResetHotkey, value); }
    public string ToggleWindowHotkey { get => _toggleWindowHotkey; set => Set(ref _toggleWindowHotkey, value); }
    public bool MinimizeToTray { get => _minimizeToTray; set => Set(ref _minimizeToTray, value); }
    public bool CheckOnStartup { get => _checkOnStartup; set => Set(ref _checkOnStartup, value); }

    public bool IsDirty
    {
        get => _isDirty;
        private set
        {
            if (_isDirty == value) return;
            _isDirty = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(UnsavedStatus));
        }
    }

    public string UnsavedStatus => IsDirty ? "有未保存的更改" : "所有更改已保存";

    public string ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (string.Equals(_errorMessage, value, StringComparison.Ordinal)) return;
            _errorMessage = value;
            OnPropertyChanged();
        }
    }

    public bool TrySave()
    {
        ErrorMessage = "";
        if (!TryParseDuration(DefaultDuration, out var duration) || duration <= TimeSpan.Zero)
            return Fail("默认时长必须是大于零的有效 hh:mm:ss / TimeSpan 值。");
        if (!TryParseFloat(FontSizeText, out var fontSize) || fontSize is < 8 or > 96)
            return Fail("字体大小必须是 8–96 之间的数字。");
        if (!TryParseInt(WidthText, out var width) || width is < 40 or > 2000)
            return Fail("窗口宽度必须是 40–2000 之间的整数。");
        if (!TryParseInt(HeightText, out var height) || height is < 20 or > 1000)
            return Fail("窗口高度必须是 20–1000 之间的整数。");
        if (!TryParseInt(BackgroundOpacityText, out var backgroundOpacity) || backgroundOpacity is < 0 or > 100)
            return Fail("背景透明度必须是 0–100 之间的整数。");
        if (!TryParseInt(TextOpacityText, out var textOpacity) || textOpacity is < 0 or > 100)
            return Fail("文字透明度必须是 0–100 之间的整数。");

        _config.Timer.DefaultDuration = DefaultDuration.Trim();
        _config.Timer.Mode = SelectedTimerMode;
        _config.Timer.ContinueOvertime = ContinueOvertime;
        _config.Timer.EndAction = SelectedEndAction;
        _config.Behavior.AutoStartOnFullscreen = AutoStartOnFullscreen;
        _config.Behavior.StopWhenLeavingFullscreen = StopWhenLeavingFullscreen;
        _config.Behavior.ResetWhenLeavingFullscreen = ResetWhenLeavingFullscreen;
        _config.Behavior.FlashOnPauseResume = FlashOnPauseResume;
        _config.Behavior.FlashPausedTime = FlashPausedTime;
        _config.Appearance.FontSize = fontSize;
        _config.Appearance.Width = width;
        _config.Appearance.Height = height;
        _config.Appearance.BackgroundOpacity = backgroundOpacity;
        _config.Appearance.TextOpacity = textOpacity;
        _config.Appearance.AlwaysOnTop = AlwaysOnTop;
        _config.Appearance.Borderless = Borderless;
        _config.Controls.StartPauseHotkey = StartPauseHotkey.Trim();
        _config.Controls.StopResetHotkey = StopResetHotkey.Trim();
        _config.Controls.ToggleWindowHotkey = ToggleWindowHotkey.Trim();
        _config.Controls.MinimizeToTray = MinimizeToTray;
        _config.Update.CheckOnStartup = CheckOnStartup;

        try
        {
            _configService.Save(_config);
            IsDirty = false;
            return true;
        }
        catch (Exception ex)
        {
            return Fail($"保存配置失败：{ex.Message}");
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool Fail(string message)
    {
        ErrorMessage = message;
        return false;
    }

    private void Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        OnPropertyChanged(propertyName);
        IsDirty = true;
        ErrorMessage = "";
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private static bool TryParseDuration(string value, out TimeSpan duration) =>
        TimeSpan.TryParse(value, CultureInfo.CurrentCulture, out duration)
        || TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out duration);

    private static bool TryParseFloat(string value, out float number) =>
        float.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out number)
        || float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number);

    private static bool TryParseInt(string value, out int number) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.CurrentCulture, out number)
        || int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out number);

    private sealed class RelayCommand(Action execute) : ICommand
    {
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => execute();
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }
}
