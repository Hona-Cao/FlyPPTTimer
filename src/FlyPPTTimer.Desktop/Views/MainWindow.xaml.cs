using System.Windows;
using FlyPPTTimer.Desktop.ViewModels;

namespace FlyPPTTimer.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
}
