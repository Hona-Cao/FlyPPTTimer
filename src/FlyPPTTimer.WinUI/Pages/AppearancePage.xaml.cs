using System.Text.RegularExpressions;
using FlyPPTTimer.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FlyPPTTimer.WinUI.Pages;

public sealed partial class AppearancePage : Page
{
    public AppearancePage()
    {
        InitializeComponent(); var c = App.Services.Controller.Config;
        ThemeBox.SelectedIndex = (int)c.Theme.Theme; MicaSwitch.IsOn = c.Theme.UseMica; MotionSwitch.IsOn = c.Theme.ReduceMotion;
        AllScreensSwitch.IsOn = c.Placement.ShowOnAllScreens; VisibleSwitch.IsOn = c.Placement.Visible; AnchorBox.SelectedIndex = (int)c.Placement.Anchor;
        OffsetXBox.Value = (double)c.Placement.OffsetXPercent; OffsetYBox.Value = (double)c.Placement.OffsetYPercent;
        WidthBox.Value = c.Appearance.Width; HeightBox.Value = c.Appearance.Height; FontFamilyBox.Text = c.Appearance.FontFamily; FontBox.Value = c.Appearance.FontSize;
        BackgroundOpacity.Value = c.Appearance.BackgroundOpacity; TextOpacity.Value = c.Appearance.TextOpacity; TextColorBox.Text = c.Appearance.TextColor;
        BackgroundColorBox.Text = c.Appearance.BackgroundColor; FlashColorBox.Text = c.Appearance.FlashBackgroundColor; AlwaysOnTopSwitch.IsOn = c.Appearance.AlwaysOnTop;
    }
    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidColor(TextColorBox.Text) || !ValidColor(BackgroundColorBox.Text) || !ValidColor(FlashColorBox.Text)) { Show("颜色必须使用 #RRGGBB 格式。"); return; }
        var c = App.Services.Controller.CreateEditableConfig(); c.Theme.Theme = (AppTheme)ThemeBox.SelectedIndex; c.Theme.UseMica = MicaSwitch.IsOn; c.Theme.ReduceMotion = MotionSwitch.IsOn;
        c.Placement.ShowOnAllScreens = AllScreensSwitch.IsOn; c.Placement.Visible = VisibleSwitch.IsOn; c.Placement.Anchor = (OverlayAnchor)AnchorBox.SelectedIndex;
        c.Placement.OffsetXPercent = (decimal)OffsetXBox.Value; c.Placement.OffsetYPercent = (decimal)OffsetYBox.Value;
        c.Appearance.Width = (int)WidthBox.Value; c.Appearance.Height = (int)HeightBox.Value; c.Appearance.FontFamily = FontFamilyBox.Text; c.Appearance.FontSize = FontBox.Value;
        c.Appearance.BackgroundOpacity = (int)BackgroundOpacity.Value; c.Appearance.TextOpacity = (int)TextOpacity.Value; c.Appearance.TextColor = TextColorBox.Text;
        c.Appearance.BackgroundColor = BackgroundColorBox.Text; c.Appearance.FlashBackgroundColor = FlashColorBox.Text; c.Appearance.AlwaysOnTop = AlwaysOnTopSwitch.IsOn;
        App.Services.ViewModel.ApplyConfig(c); if (App.Services.MainWindow.Content is FrameworkElement root) root.RequestedTheme = c.Theme.Theme == AppTheme.Dark ? ElementTheme.Dark : c.Theme.Theme == AppTheme.Light ? ElementTheme.Light : ElementTheme.Default;
    }
    private static bool ValidColor(string value) => Regex.IsMatch(value ?? "", "^#[0-9A-Fa-f]{6}$");
    private void Show(string message) { ValidationBar.Message = message; ValidationBar.Severity = InfoBarSeverity.Error; ValidationBar.IsOpen = true; }
}
