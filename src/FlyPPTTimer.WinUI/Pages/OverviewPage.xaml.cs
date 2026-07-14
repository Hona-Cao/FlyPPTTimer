using Microsoft.UI.Xaml.Controls;
namespace FlyPPTTimer.WinUI.Pages;
public sealed partial class OverviewPage : Page { public OverviewPage() { InitializeComponent(); DataContext = App.Services.ViewModel; } }
