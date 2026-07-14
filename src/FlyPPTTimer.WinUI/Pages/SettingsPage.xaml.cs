using FlyPPTTimer.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;

namespace FlyPPTTimer.WinUI.Pages;

public sealed partial class SettingsPage : Page
{
    private AppConfig _edit = null!;
    public SettingsPage() { InitializeComponent(); Reload(); }

    private void Reload()
    {
        _edit = App.Services.Controller.CreateEditableConfig(); var behavior = _edit.Behavior;
        AutoStart.IsOn = behavior.AutoStartOnFullscreen; StopOnExit.IsOn = behavior.StopWhenLeavingFullscreen; ResetOnExit.IsOn = behavior.ResetWhenLeavingFullscreen; FlashPause.IsOn = behavior.FlashPausedTime;
        LoadPrompt(behavior.Prompt1, Prompt1Enabled, Prompt1Seconds, Prompt1Text, Prompt1Beep, Prompt1Speak, Prompt1FlashText, Prompt1FlashBackground, Prompt1FlashBorder, Prompt1SoundFile);
        LoadPrompt(behavior.Prompt2, Prompt2Enabled, Prompt2Seconds, Prompt2Text, Prompt2Beep, Prompt2Speak, Prompt2FlashText, Prompt2FlashBackground, Prompt2FlashBorder, Prompt2SoundFile);
        EndPromptText.Text = behavior.EndPrompt.Text; EndFlashSeconds.Value = behavior.EndPrompt.FlashSeconds; EndBeep.IsChecked = behavior.EndPrompt.Beep; EndSpeak.IsChecked = behavior.EndPrompt.Speak;
        EndFlashText.IsChecked = behavior.EndPrompt.FlashText; EndFlashBackground.IsChecked = behavior.EndPrompt.FlashBackground; EndFlashBorder.IsChecked = behavior.EndPrompt.FlashBorder; EndSoundFile.Text = behavior.EndPrompt.SoundFile;
        ClickThrough.IsOn = _edit.Controls.ClickThrough; LockPosition.IsOn = _edit.Controls.LockPosition; MinimizeTray.IsOn = _edit.Controls.MinimizeToTray;
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var behavior = _edit.Behavior; behavior.AutoStartOnFullscreen = AutoStart.IsOn; behavior.StopWhenLeavingFullscreen = StopOnExit.IsOn; behavior.ResetWhenLeavingFullscreen = ResetOnExit.IsOn; behavior.FlashPausedTime = FlashPause.IsOn;
            SavePrompt(behavior.Prompt1, Prompt1Enabled, Prompt1Seconds, Prompt1Text, Prompt1Beep, Prompt1Speak, Prompt1FlashText, Prompt1FlashBackground, Prompt1FlashBorder, Prompt1SoundFile);
            SavePrompt(behavior.Prompt2, Prompt2Enabled, Prompt2Seconds, Prompt2Text, Prompt2Beep, Prompt2Speak, Prompt2FlashText, Prompt2FlashBackground, Prompt2FlashBorder, Prompt2SoundFile);
            behavior.EndPrompt.Text = EndPromptText.Text; behavior.EndPrompt.FlashSeconds = (int)EndFlashSeconds.Value; behavior.EndPrompt.Beep = EndBeep.IsChecked == true; behavior.EndPrompt.Speak = EndSpeak.IsChecked == true;
            behavior.EndPrompt.FlashText = EndFlashText.IsChecked == true; behavior.EndPrompt.FlashBackground = EndFlashBackground.IsChecked == true; behavior.EndPrompt.FlashBorder = EndFlashBorder.IsChecked == true; behavior.EndPrompt.SoundFile = EndSoundFile.Text;
            _edit.Controls.ClickThrough = ClickThrough.IsOn; _edit.Controls.LockPosition = LockPosition.IsOn; _edit.Controls.MinimizeToTray = MinimizeTray.IsOn;
            App.Services.ViewModel.ApplyConfig(_edit); Show("设置已应用", InfoBarSeverity.Success);
        }
        catch (Exception ex) { Show(ex.Message, InfoBarSeverity.Error); }
    }

    private static void LoadPrompt(PromptSettings value, ToggleSwitch enabled, NumberBox seconds, TextBox text, CheckBox beep, CheckBox speak, CheckBox flashText, CheckBox flashBackground, CheckBox flashBorder, TextBox sound)
    { enabled.IsOn = value.Enabled; seconds.Value = value.TriggerBeforeEndSeconds; text.Text = value.Text; beep.IsChecked = value.Beep; speak.IsChecked = value.Speak; flashText.IsChecked = value.FlashText; flashBackground.IsChecked = value.FlashBackground; flashBorder.IsChecked = value.FlashBorder; sound.Text = value.SoundFile; }
    private static void SavePrompt(PromptSettings value, ToggleSwitch enabled, NumberBox seconds, TextBox text, CheckBox beep, CheckBox speak, CheckBox flashText, CheckBox flashBackground, CheckBox flashBorder, TextBox sound)
    { value.Enabled = enabled.IsOn; value.TriggerBeforeEndSeconds = (int)seconds.Value; value.Text = text.Text; value.Beep = beep.IsChecked == true; value.Speak = speak.IsChecked == true; value.FlashText = flashText.IsChecked == true; value.FlashBackground = flashBackground.IsChecked == true; value.FlashBorder = flashBorder.IsChecked == true; value.SoundFile = sound.Text; }

    private void Cancel_Click(object sender, RoutedEventArgs e) { Reload(); Show("未保存的修改已丢弃", InfoBarSeverity.Informational); }
    private async void Reset_Click(object sender, RoutedEventArgs e) { var dialog = new ContentDialog { Title = "恢复默认设置？", Content = "文件规则和自定义设置将恢复默认。", PrimaryButtonText = "恢复", CloseButtonText = "取消", XamlRoot = XamlRoot }; if (await dialog.ShowAsync() == ContentDialogResult.Primary) { _edit = new(); App.Services.ViewModel.ApplyConfig(_edit); Reload(); } }
    private async void Import_Click(object sender, RoutedEventArgs e) { var picker = new FileOpenPicker(); Init(picker); picker.FileTypeFilter.Add(".json"); var file = await picker.PickSingleFileAsync(); if (file is not null) try { App.Services.Controller.ImportConfig(file.Path); App.Services.ViewModel.RefreshAll(); Reload(); Show("配置已导入", InfoBarSeverity.Success); } catch (Exception ex) { Show(ex.Message, InfoBarSeverity.Error); } }
    private async void Export_Click(object sender, RoutedEventArgs e) { var picker = new FileSavePicker(); Init(picker); picker.FileTypeChoices.Add("JSON", [".json"]); picker.SuggestedFileName = "FlyPPTTimer.config"; var file = await picker.PickSaveFileAsync(); if (file is not null) { App.Services.Controller.ExportConfig(file.Path); Show("配置已导出", InfoBarSeverity.Success); } }
    private static void Init(object picker) => WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(App.Services.MainWindow));
    private void Show(string text, InfoBarSeverity severity) { ValidationBar.Message = text; ValidationBar.Severity = severity; ValidationBar.IsOpen = true; }
}
