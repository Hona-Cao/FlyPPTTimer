using System.ComponentModel;
using System.Windows;
using FlyPPTTimer.Desktop.ViewModels;

namespace FlyPPTTimer.Desktop.Views;

public partial class MainWindow : Window
{
    private readonly SettingsViewModel _viewModel;
    private bool _allowClose;

    public MainWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        viewModel.CloseRequested += OnCloseRequested;
        Closing += OnClosing;
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
}
