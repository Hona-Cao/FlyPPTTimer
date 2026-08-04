using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FlyPPTTimer.Desktop.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private string _statusText = "WPF 桌面框架已就绪，尚未接管正式版功能。";

    public string ProductName => "FlyPPTTimer";
    public string VersionText => "4.0.0-alpha.1";
    public string TimerText => "08:00";

    public string StatusText
    {
        get => _statusText;
        set
        {
            if (string.Equals(_statusText, value, StringComparison.Ordinal)) return;
            _statusText = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
