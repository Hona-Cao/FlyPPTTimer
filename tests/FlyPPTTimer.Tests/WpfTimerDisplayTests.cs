using System.Drawing;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Threading;
using FlyPPTTimer.Desktop;
using FlyPPTTimer.Models;
using FlyPPTTimer.Services;

namespace FlyPPTTimer.Tests;

public sealed class WpfTimerDisplayTests
{
    [Theory]
    [InlineData(OverlayAnchor.TopLeft, 105f, 137.5f)]
    [InlineData(OverlayAnchor.TopCenter, 500f, 137.5f)]
    [InlineData(OverlayAnchor.TopRight, 895f, 137.5f)]
    [InlineData(OverlayAnchor.Center, 500, 400)]
    [InlineData(OverlayAnchor.BottomLeft, 105f, 662.5f)]
    [InlineData(OverlayAnchor.BottomRight, 895f, 662.5f)]
    public void OverlayPlacementUsesAnchorAndPhysicalDpi(
        OverlayAnchor anchor,
        float expectedX,
        float expectedY)
    {
        var origin = OverlayPlacementService.CalculateOrigin(
            new Rectangle(0, 100, 1000, 600),
            144,
            anchor,
            0,
            0);

        Assert.Equal(expectedX, origin.X);
        Assert.Equal(expectedY, origin.Y);
    }

    [Fact]
    public void OverlayPlacementPreservesNegativeCoordinatesAndOffsets()
    {
        var origin = OverlayPlacementService.CalculateOrigin(
            new Rectangle(-1920, -200, 1920, 1080),
            96,
            OverlayAnchor.Center,
            10,
            -5);

        Assert.Equal(-768, origin.X);
        Assert.Equal(286, origin.Y);
        Assert.Equal(new System.Drawing.Point(-818, 268),
            OverlayPlacementService.LocationFromCenter(origin, new System.Drawing.Size(100, 36)));
    }

    [Fact]
    public void WpfOverlayUsesRealControlsAndProcessesDispatcherUpdates()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var window = new WpfTimerOverlayWindow(
                    () => { }, () => { }, () => { }, () => { }, () => { }, () => { });
                var config = new AppConfig();
                config.Placement.Visible = false;
                window.ApplyConfig(config, Screen.PrimaryScreen!);
                window.UpdateTime(new TimerSnapshot(
                    TimerState.Running,
                    TimerMode.Countdown,
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(479),
                    TimeSpan.FromSeconds(479),
                    TimeSpan.FromMinutes(8),
                    false));

                var text = FindDescendant<TextBlock>((DependencyObject)window.Content);
                Assert.NotNull(text);
                Assert.Equal("07:59", text.Text);

                var resetInvoked = false;
                var menuWindow = new WpfTimerOverlayWindow(
                    () => resetInvoked = true, () => { }, () => { }, () => { }, () => { }, () => { });
                var resetItem = menuWindow.ContextMenu!.Items
                    .OfType<System.Windows.Controls.MenuItem>()
                    .Single(item => AutomationProperties.GetAutomationId(item) == "ResetTimerPosition");
                resetItem.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.MenuItem.ClickEvent));
                Assert.True(resetInvoked);
                menuWindow.Close();

                var completed = false;
                window.Dispatcher.BeginInvoke(() => completed = true, DispatcherPriority.Background);
                var frame = new DispatcherFrame();
                window.Dispatcher.BeginInvoke(() => frame.Continue = false, DispatcherPriority.ApplicationIdle);
                Dispatcher.PushFrame(frame);
                Assert.True(completed);
                window.Close();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(3)), "The WPF control test exceeded 3 seconds.");
        Assert.Null(failure);
    }

    [Fact]
    public void BigScreenWindowRejectsPrimaryDisplay()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var window = new WpfBigScreenTimerWindow();
                Assert.Throws<ArgumentException>(() => window.ApplyConfig(new AppConfig(), Screen.PrimaryScreen!));
                Assert.IsAssignableFrom<Window>(window);
                Assert.NotNull(FindDescendant<TextBlock>((DependencyObject)window.Content));
                window.Close();
            }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(3)), "The WPF big-screen control test exceeded 3 seconds.");
        Assert.Null(failure);
    }

    private static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T match) return match;
        for (var index = 0; index < System.Windows.Media.VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var found = FindDescendant<T>(System.Windows.Media.VisualTreeHelper.GetChild(root, index));
            if (found is not null) return found;
        }
        return null;
    }
}
