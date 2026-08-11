using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Media;
using System.Windows.Threading;
using FlyPPTTimer.Desktop;
using FlyPPTTimer.Models;
using FlyPPTTimer.Services;
using WpfControls = System.Windows.Controls;

namespace FlyPPTTimer.Tests;

[Collection(WpfUiTestCollection.Name)]
public sealed class WpfRemoteDashboardTests
{
    [Fact]
    public void RealWpfDashboardControlsBindResizeAndDispatcherResponds()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var current = new AppConfig();
                current.RemoteControl.Enabled = false;
                current.Rules.Add(new FileRule
                {
                    FileName = "fixture.pptx",
                    FilePath = Path.Combine(Path.GetTempPath(), "fixture.pptx"),
                    Duration = "00:08:00",
                    Enabled = true
                });
                void Save(AppConfig next) => current = ConfigService.Clone(next);
                var log = TestLog.Create();
                var timer = new TimerService(log);
                timer.Configure(current);
                var commands = new AppCommandService(
                    timer, new AlertService(log), () => current, Save,
                    () => { }, () => { }, () => { }, () => false, _ => { }, () => { }, log);
                using var remote = new RemoteControlService(() => current, Save, commands, null, log);
                var dashboard = new RemoteDashboardService(
                    () => current, Save, remote, new NetworkAddressService());
                var window = new WpfRemoteControlWindow(dashboard);

                var stopwatch = Stopwatch.StartNew();
                window.ShowModeless();
                Pump(window.Dispatcher);
                Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(3), "WPF remote window startup exceeded 3 seconds.");
                Assert.Equal("RemoteDashboardWindow", AutomationProperties.GetAutomationId(window));

                var tabs = FindByAutomationId<WpfControls.TabControl>(window, "RemoteDashboardTabs");
                var serviceToggle = FindByAutomationId<WpfControls.Button>(window, "RemoteServiceToggle");
                var randomPort = FindByAutomationId<WpfControls.CheckBox>(window, "RemoteRandomPort");
                var port = FindByAutomationId<WpfControls.TextBox>(window, "RemotePort");
                var apply = FindByAutomationId<WpfControls.Button>(window, "ApplyEndpoint");
                Assert.NotNull(tabs);
                Assert.NotNull(serviceToggle);
                Assert.NotNull(randomPort);
                Assert.NotNull(port);
                Assert.NotNull(apply);

                stopwatch.Restart();
                randomPort.IsChecked = true;
                apply.RaiseEvent(new RoutedEventArgs(WpfControls.Button.ClickEvent));
                Pump(window.Dispatcher);
                Assert.True(current.RemoteControl.UseRandomPort);
                serviceToggle.RaiseEvent(new RoutedEventArgs(WpfControls.Button.ClickEvent));
                Pump(window.Dispatcher);
                Assert.True(current.RemoteControl.Enabled);
                Assert.True(remote.IsRunning);
                serviceToggle.RaiseEvent(new RoutedEventArgs(WpfControls.Button.ClickEvent));
                Pump(window.Dispatcher);
                Assert.False(current.RemoteControl.Enabled);
                Assert.False(remote.IsRunning);
                Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(3), "Remote listener toggle exceeded 3 seconds.");

                stopwatch.Restart();
                randomPort.IsChecked = false;
                port.Text = "5091";
                apply.RaiseEvent(new RoutedEventArgs(WpfControls.Button.ClickEvent));
                Pump(window.Dispatcher);
                Assert.Equal(5091, current.RemoteControl.Port);
                Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(3), "Endpoint binding exceeded 3 seconds.");

                stopwatch.Restart();
                window.Width = 780;
                window.Height = 600;
                tabs.SelectedIndex = 1;
                Pump(window.Dispatcher);
                Assert.NotNull(FindByAutomationId<WpfControls.ListBox>(window, "PresentationList"));
                Assert.NotNull(FindByAutomationId<WpfControls.TextBox>(window, "PresentationDuration"));
                Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(3), "Responsive tab interaction exceeded 3 seconds.");

                var dispatched = false;
                window.Dispatcher.BeginInvoke(() => dispatched = true, DispatcherPriority.Background);
                Pump(window.Dispatcher);
                Assert.True(dispatched);
                window.ClosePermanently();
            }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(10)), "The WPF remote dashboard test exceeded 10 seconds.");
        Assert.Null(failure);
    }

    private static void Pump(Dispatcher dispatcher)
    {
        var frame = new DispatcherFrame();
        dispatcher.BeginInvoke(() => frame.Continue = false, DispatcherPriority.ApplicationIdle);
        Dispatcher.PushFrame(frame);
    }

    private static T? FindByAutomationId<T>(DependencyObject root, string automationId)
        where T : DependencyObject
    {
        if (root is T match && AutomationProperties.GetAutomationId(match) == automationId) return match;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var found = FindByAutomationId<T>(VisualTreeHelper.GetChild(root, index), automationId);
            if (found is not null) return found;
        }
        return null;
    }
}
