using System.ComponentModel;
using System.Windows;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using FlyPPTTimer.Services;
using FlyPPTTimer.Desktop.ViewModels;
using FlyPPTTimer.Models;
using AppLocalization = FlyPPTTimer.Services.Localization;

namespace FlyPPTTimer.Desktop.Views;

public partial class MainWindow : Window
{
    private readonly SettingsViewModel _viewModel;
    private bool _allowClose;

    public MainWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        LocalizeVisualTree(this);
        _viewModel = viewModel;
        DataContext = viewModel;
        viewModel.SyncRuleDurationsRequested = ConfirmRuleDurationSync;
        viewModel.CloseRequested += OnCloseRequested;
        Closing += OnClosing;
        Loaded += (_, _) => Dispatcher.BeginInvoke(() => LocalizeVisualTree(this));
        Closed += (_, _) => viewModel.CloseRequested -= OnCloseRequested;
    }

    private void OnCloseRequested(bool discardChanges)
    {
        _allowClose = true;
        Close();
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_allowClose || !_viewModel.IsDirty) return;

        var choice = MessageBox.Show(
            this,
            "设置尚未保存。是否保存后关闭？",
            "FlyPPTTimer 设置",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);
        if (choice == MessageBoxResult.Cancel)
        {
            e.Cancel = true;
            return;
        }
        if (choice == MessageBoxResult.Yes && !_viewModel.TrySave())
            e.Cancel = true;
    }

    private void SettingsTabs_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (!ReferenceEquals(e.OriginalSource, sender)) return;
        Dispatcher.BeginInvoke(() => LocalizeVisualTree(this));
    }

    private bool ConfirmRuleDurationSync(string duration, int count) => MessageBox.Show(
        this,
        $"全局默认时长将改为 {duration}。\n\n是否同步应用到全部 {count} 个待控演示文稿？\n\n选择“否”将保留各文件规则原来的时长。",
        "同步文件规则时长",
        MessageBoxButton.YesNo,
        MessageBoxImage.Question) == MessageBoxResult.Yes;

    private void AddRules_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择 PPT 文件", Filter = "演示文稿 (*.ppt;*.pptx;*.pptm)|*.ppt;*.pptx;*.pptm|所有文件 (*.*)|*.*", Multiselect = true
        };
        if (dialog.ShowDialog(this) == true) _viewModel.AddRulePaths(dialog.FileNames);
    }

    private void ApplyBatch_Click(object sender, RoutedEventArgs e) =>
        _viewModel.ApplyBatchToSelectedRules(BatchDuration.Text, BatchMode.SelectedIndex == 1 ? TimerMode.CountUp : TimerMode.Countdown);

    private async void ChooseSound_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: PromptDraft prompt }) return;
        var dialog = new OpenFileDialog { Title = "选择提示音文件", Filter = "音频文件 (*.mp3;*.wav;*.wma;*.m4a)|*.mp3;*.wav;*.wma;*.m4a", CheckFileExists = true };
        if (dialog.ShowDialog(this) != true) return;
        var slot = ReferenceEquals(prompt, _viewModel.Prompt1) ? "prompt1" : ReferenceEquals(prompt, _viewModel.Prompt2) ? "prompt2" : "end";
        try { prompt.SoundFile = await Task.Run(() => AlertSoundStorage.ImportSound(dialog.FileName, slot)); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "FlyPPTTimer 设置", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private void ClearSound_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: PromptDraft prompt }) prompt.SoundFile = "";
    }

    private async void ImportConfig_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Title = "导入 FlyPPTTimer 配置", Filter = "JSON 配置 (*.json)|*.json|所有文件 (*.*)|*.*", CheckFileExists = true };
        if (dialog.ShowDialog(this) == true) await _viewModel.ImportAsync(dialog.FileName);
    }

    private async void ExportConfig_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { Title = "导出 FlyPPTTimer 配置", Filter = "JSON 配置 (*.json)|*.json", FileName = "FlyPPTTimer.config.json" };
        if (dialog.ShowDialog(this) == true) await _viewModel.ExportAsync(dialog.FileName);
    }

    private async void ResetConfig_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(this, "确定恢复全部默认设置？当前配置会保留时间戳备份。", "恢复默认", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            await _viewModel.ResetAsync();
    }

    private void OpenConfigFolder_Click(object sender, RoutedEventArgs e) => OpenDirectory(_viewModel.ConfigDirectory);
    private void OpenLogFolder_Click(object sender, RoutedEventArgs e) => OpenDirectory(_viewModel.LogDirectory);

    private static void OpenDirectory(string directory)
    {
        Directory.CreateDirectory(directory);
        Process.Start(new ProcessStartInfo(directory) { UseShellExecute = true });
    }

    private static void LocalizeVisualTree(DependencyObject root)
    {
        if (!AppLocalization.IsEnglish) return;
        if (root is System.Windows.Controls.TextBlock textBlock) textBlock.Text = AppLocalization.T(textBlock.Text);
        if (root is System.Windows.Controls.ContentControl { Content: string content } contentControl)
            contentControl.Content = AppLocalization.T(content);
        if (root is System.Windows.Controls.HeaderedContentControl { Header: string header } headerControl)
            headerControl.Header = AppLocalization.T(header);
        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>()) LocalizeVisualTree(child);
    }
}
