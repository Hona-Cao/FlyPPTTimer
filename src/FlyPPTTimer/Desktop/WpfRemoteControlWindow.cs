using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Automation;
using System.Windows.Forms.Integration;
using System.Windows.Interop;
using FlyPPTTimer.Forms;
using FlyPPTTimer.Models;
using FlyPPTTimer.Services;
using QRCoder;
using Wpf = System.Windows;
using WpfControls = System.Windows.Controls;
using WpfMedia = System.Windows.Media;
using WpfMediaImaging = System.Windows.Media.Imaging;
using WpfThreading = System.Windows.Threading;

namespace FlyPPTTimer.Desktop;

/// <summary>Formal in-process WPF dashboard for remote connectivity and presentation control.</summary>
public sealed class WpfRemoteControlWindow : Wpf.Window
{
    private readonly RemoteDashboardService _dashboard;
    private readonly WpfThreading.DispatcherTimer _refreshTimer = new() { Interval = TimeSpan.FromMilliseconds(500) };
    private readonly WpfControls.TextBlock _serviceStatus = Text("未启动");
    private readonly WpfControls.TextBlock _clientStatus = Text("0 个设备");
    private readonly WpfControls.Button _serviceToggle = Button("启动服务", "RemoteServiceToggle");
    private readonly WpfControls.CheckBox _randomPort = new() { Content = "使用随机端口" };
    private readonly WpfControls.TextBox _port = TextBox("RemotePort");
    private readonly WpfControls.ComboBox _address = new();
    private readonly WpfControls.TextBox _url = TextBox("RemoteAccessUrl", true);
    private readonly WpfControls.Image _qr = new() { Width = 190, Height = 190, Stretch = WpfMedia.Stretch.Uniform };
    private readonly WpfControls.ListBox _presentations = new();
    private readonly WpfControls.TextBlock _presentationStatus = Text("请选择演示文稿。");
    private readonly WpfControls.TextBlock _selectedName = Text("未选择");
    private readonly WpfControls.TextBox _selectedPath = TextBox("PresentationPath", true);
    private readonly WpfControls.TextBox _duration = TextBox("PresentationDuration");
    private readonly WpfControls.ComboBox _mode = new();
    private readonly WpfControls.CheckBox _ruleEnabled = new() { Content = "启用规则" };
    private readonly WpfControls.TextBox _slideNumber = TextBox("SlideNumber");
    private readonly Dictionary<PresentationCommandKind, WpfControls.Button> _commandButtons = [];
    private RemoteDashboardSnapshot? _snapshot;
    private string _selectedPresentationPath = "";
    private string _selectedPresentationId = "";
    private bool _updating;
    private bool _closingPermanently;
    private bool _modelessInteropEnabled;

    public WpfRemoteControlWindow(RemoteDashboardService dashboard)
    {
        _dashboard = dashboard ?? throw new ArgumentNullException(nameof(dashboard));
        Title = "FlyPPTTimer 远程控制";
        Width = 1040;
        Height = 760;
        MinWidth = 700;
        MinHeight = 510;
        WindowStartupLocation = Wpf.WindowStartupLocation.CenterScreen;
        Background = Brush("#F5F7FB");
        FontFamily = new WpfMedia.FontFamily("Microsoft YaHei UI");
        AutomationProperties.SetAutomationId(this, "RemoteDashboardWindow");
        AutomationProperties.SetName(this, "FlyPPTTimer WPF Remote Control");

        Content = BuildWindow();
        RestorePlacement();
        _refreshTimer.Tick += (_, _) => RefreshSnapshot();
        Loaded += (_, _) =>
        {
            _refreshTimer.Start();
            RefreshSnapshot();
        };
        IsVisibleChanged += (_, _) =>
        {
            if (IsVisible) _refreshTimer.Start();
            else _refreshTimer.Stop();
        };
        Closing += (_, e) =>
        {
            SavePlacement();
            if (_closingPermanently) return;
            e.Cancel = true;
            Hide();
        };
        SizeChanged += (_, _) => ApplyResponsiveColumns();
    }

    public bool IsDisposed { get; private set; }

    public void ShowModeless()
    {
        if (IsDisposed) return;
        if (!_modelessInteropEnabled)
        {
            ElementHost.EnableModelessKeyboardInterop(this);
            _modelessInteropEnabled = true;
        }
        Show();
        Activate();
        RefreshSnapshot();
    }

    public void ReloadConfig(AppConfig _) => RefreshSnapshot();

    public void ClosePermanently()
    {
        if (IsDisposed) return;
        _closingPermanently = true;
        IsDisposed = true;
        _refreshTimer.Stop();
        Close();
    }

    private Wpf.FrameworkElement BuildWindow()
    {
        var root = new WpfControls.Grid();
        root.RowDefinitions.Add(new WpfControls.RowDefinition { Height = Wpf.GridLength.Auto });
        root.RowDefinitions.Add(new WpfControls.RowDefinition());
        var header = new WpfControls.Border
        {
            Background = WpfMedia.Brushes.White,
            BorderBrush = Brush("#E4E8F0"),
            BorderThickness = new Wpf.Thickness(0, 0, 0, 1),
            Padding = new Wpf.Thickness(24, 18, 24, 16),
            Child = new WpfControls.StackPanel
            {
                Children =
                {
                    Text("FlyPPTTimer 远程控制", 24, Wpf.FontWeights.SemiBold),
                    Text("局域网连接、演示文稿规则与放映控制", 13, Wpf.FontWeights.Normal, "#667085")
                }
            }
        };
        root.Children.Add(header);

        var tabs = new WpfControls.TabControl { Margin = new Wpf.Thickness(18) };
        AutomationProperties.SetAutomationId(tabs, "RemoteDashboardTabs");
        tabs.Items.Add(new WpfControls.TabItem { Header = "远程连接", Content = BuildConnectionPage() });
        tabs.Items.Add(new WpfControls.TabItem { Header = "演示文稿", Content = BuildPresentationPage() });
        WpfControls.Grid.SetRow(tabs, 1);
        root.Children.Add(tabs);
        return root;
    }

    private Wpf.FrameworkElement BuildConnectionPage()
    {
        var panel = Stack();
        _serviceToggle.Click += (_, _) =>
        {
            var running = _snapshot?.IsRunning ?? false;
            _dashboard.SetServiceEnabled(!running);
            RefreshSnapshot();
        };
        panel.Children.Add(Card(Stack(
            TitleText("服务状态"),
            Row(_serviceStatus, _clientStatus, _serviceToggle,
                Click(Button("刷新网络", "RefreshNetworks"), RefreshSnapshot),
                Click(Button("断开所有设备", "DisconnectAll"), DisconnectAll)))));

        _randomPort.Margin = FieldMargin;
        AutomationProperties.SetAutomationId(_randomPort, "RemoteRandomPort");
        _port.Width = 110;
        var saveEndpoint = Click(Button("应用并重启", "ApplyEndpoint"), ApplyEndpoint);
        panel.Children.Add(Card(Stack(
            TitleText("监听设置"),
            Row(_randomPort, Text("端口"), _port, saveEndpoint),
            Text("默认端口为 4080；随机端口适合临时测试，固定端口更便于手机收藏。", 12, Wpf.FontWeights.Normal, "#667085"))));

        _address.MinWidth = 280;
        _address.Margin = FieldMargin;
        _address.SelectionChanged += (_, _) => UpdateAccessLink();
        AutomationProperties.SetAutomationId(_address, "RemoteAddress");
        _url.MinWidth = 420;
        _url.Margin = FieldMargin;
        var copy = Click(Button("复制完整链接", "CopyRemoteUrl"), CopyAccessUrl);
        var open = Click(Button("浏览器打开", "OpenRemoteUrl"), OpenAccessUrl);
        var linkPanel = Stack(
            TitleText("手机访问"),
            Row(Text("地址"), _address),
            Row(_url, copy, open),
            Text("界面隐藏 token；复制和二维码使用完整访问链接。手机与电脑需处于同一 Wi-Fi 或局域网。", 12, Wpf.FontWeights.Normal, "#667085"));
        var qrCard = Card(_qr);
        var accessGrid = new WpfControls.Grid();
        accessGrid.ColumnDefinitions.Add(new WpfControls.ColumnDefinition());
        accessGrid.ColumnDefinitions.Add(new WpfControls.ColumnDefinition { Width = new Wpf.GridLength(220) });
        accessGrid.Children.Add(Card(linkPanel));
        WpfControls.Grid.SetColumn(qrCard, 1);
        accessGrid.Children.Add(qrCard);
        panel.Children.Add(accessGrid);
        panel.Children.Add(Card(Stack(
            TitleText("网络与防火墙"),
            Text("若手机无法访问，请确认网络为“专用网络”，并允许 FlyPPTTimer.exe 通过 Windows 防火墙。代理/TUN 和虚拟网卡地址不会标记为推荐。", 12, Wpf.FontWeights.Normal, "#667085"))));
        return Scroll(panel);
    }

    private Wpf.FrameworkElement BuildPresentationPage()
    {
        var root = new WpfControls.Grid();
        root.ColumnDefinitions.Add(new WpfControls.ColumnDefinition { Width = new Wpf.GridLength(320) });
        root.ColumnDefinitions.Add(new WpfControls.ColumnDefinition());

        var add = Click(Button("添加文件…", "AddPresentationRules"), AddRules);
        var clear = Click(Button("清空规则", "ClearPresentationRules"), ClearRules);
        _presentations.Margin = new Wpf.Thickness(0, 10, 0, 0);
        _presentations.HorizontalContentAlignment = Wpf.HorizontalAlignment.Stretch;
        AutomationProperties.SetAutomationId(_presentations, "PresentationList");
        _presentations.SelectionChanged += (_, _) => SelectPresentation();
        var left = Card(new WpfControls.DockPanel
        {
            Children =
            {
                new WpfControls.StackPanel { Orientation = WpfControls.Orientation.Horizontal, Children = { add, clear } },
                _presentations
            }
        });
        WpfControls.DockPanel.SetDock(((WpfControls.DockPanel)left.Child).Children[0], WpfControls.Dock.Top);
        root.Children.Add(left);

        _selectedPath.TextWrapping = Wpf.TextWrapping.Wrap;
        _duration.Width = 120;
        _mode.Width = 120;
        _mode.ItemsSource = new[] { TimerMode.Countdown, TimerMode.CountUp };
        _ruleEnabled.Margin = FieldMargin;
        _slideNumber.Width = 80;
        var save = Click(Button("保存规则", "SavePresentationRule"), SaveRule);
        var delete = Click(Button("删除规则", "DeletePresentationRule"), DeleteRule);
        var details = Stack(
            _presentationStatus,
            Card(Stack(TitleText("文稿详情"), _selectedName, _selectedPath,
                Row(Text("时长"), _duration, Text("模式"), _mode, _ruleEnabled, save, delete))),
            Card(Stack(TitleText("放映控制"),
                CommandRow(
                    CommandButton("打开", PresentationCommandKind.OpenPresentation),
                    CommandButton("从头放映", PresentationCommandKind.StartFromBeginning),
                    CommandButton("当前页放映", PresentationCommandKind.StartFromCurrent)),
                CommandRow(
                    CommandButton("上一页", PresentationCommandKind.Previous),
                    CommandButton("下一页", PresentationCommandKind.Next),
                    _slideNumber,
                    Click(Button("跳转", "GoToSlide"), GoToSlide)),
                CommandRow(
                    CommandButton("黑屏 / 恢复", PresentationCommandKind.ToggleBlackScreen),
                    CommandButton("白屏 / 恢复", PresentationCommandKind.ToggleWhiteScreen),
                    CommandButton("结束放映", PresentationCommandKind.EndShow)))),
            Card(Stack(TitleText("关闭与退出"),
                CommandRow(
                    CommandButton("关闭当前文稿", PresentationCommandKind.CloseActivePresentation),
                    CommandButton("关闭最后打开", PresentationCommandKind.CloseCurrentPresentation),
                    Click(Button("强制退出演示软件", "ForceQuitPresentation"), ForceQuit)))));
        var right = Scroll(details);
        right.Margin = new Wpf.Thickness(12, 0, 0, 0);
        WpfControls.Grid.SetColumn(right, 1);
        root.Children.Add(right);
        root.Tag = right;
        return root;
    }

    private void RefreshSnapshot()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(RefreshSnapshot);
            return;
        }

        var snapshot = _dashboard.GetSnapshot();
        _snapshot = snapshot;
        _updating = true;
        try
        {
            _serviceStatus.Text = snapshot.StatusText;
            _clientStatus.Text = $"{snapshot.ConnectedClients} 个设备";
            _serviceToggle.Content = snapshot.IsRunning ? "停止服务" : "启动服务";
            _randomPort.IsChecked = snapshot.Config.RemoteControl.UseRandomPort;
            if (!_port.IsKeyboardFocusWithin)
                _port.Text = snapshot.Config.RemoteControl.Port.ToString(CultureInfo.InvariantCulture);
            RefreshAddresses(snapshot);
            RefreshPresentations(snapshot);
        }
        finally { _updating = false; }
        if (string.IsNullOrWhiteSpace(_selectedPresentationId)) SelectPresentation();
        UpdateAccessLink();
        UpdateCommandAvailability(snapshot.PresentationState);
    }

    private void RefreshAddresses(RemoteDashboardSnapshot snapshot)
    {
        var selected = (_address.SelectedItem as AddressChoice)?.Address;
        var choices = snapshot.Addresses.Select(item => new AddressChoice(
            item.Address,
            $"{item.Address} · {item.Type}{(item.Recommended ? " · 推荐" : "")}" )).ToList();
        _address.ItemsSource = choices;
        _address.DisplayMemberPath = nameof(AddressChoice.Label);
        _address.SelectedItem = choices.FirstOrDefault(item => item.Address == selected)
            ?? choices.FirstOrDefault(item => snapshot.Addresses.First(source => source.Address == item.Address).Recommended)
            ?? choices.FirstOrDefault();
    }

    private void RefreshPresentations(RemoteDashboardSnapshot snapshot)
    {
        var items = PresentationRuleValidator.MergeRulesAndOpenPresentations(
                snapshot.Config.Rules,
                snapshot.PresentationState.Presentations)
            .Select(entry => new DashboardPresentationItem(
                entry.Path,
                entry.Rule?.FileName ?? entry.Presentation?.Name ?? Path.GetFileName(entry.Path),
                BuildPresentationSummary(entry),
                entry.Rule,
                entry.Presentation))
            .ToList();
        _presentations.ItemsSource = items;
        _presentations.DisplayMemberPath = nameof(DashboardPresentationItem.Display);
        _presentations.SelectedItem = items.FirstOrDefault(item => SamePath(item.Path, _selectedPresentationPath));
        if (_presentations.SelectedItem is null && items.Count > 0 && string.IsNullOrWhiteSpace(_selectedPresentationPath))
            _presentations.SelectedIndex = 0;

        if (!string.IsNullOrWhiteSpace(snapshot.PresentationState.OperationMessage))
            _presentationStatus.Text = snapshot.PresentationState.OperationMessage;
        else if (!string.IsNullOrWhiteSpace(snapshot.PresentationState.Error))
            _presentationStatus.Text = snapshot.PresentationState.Error;
        else
            _presentationStatus.Text = snapshot.PresentationState.IsSlideShowRunning
                ? $"放映中 · {snapshot.PresentationState.CurrentSlide} / {snapshot.PresentationState.TotalSlides} · {snapshot.PresentationState.ScreenMode}"
                : "演示控制已就绪。";
    }

    private void SelectPresentation()
    {
        if (_updating || _presentations.SelectedItem is not DashboardPresentationItem item) return;
        _selectedPresentationPath = item.Path;
        _selectedPresentationId = item.Presentation?.Id ?? PresentationRuleValidator.IdForPath(item.Path);
        _selectedName.Text = item.Name;
        _selectedPath.Text = item.Path;
        _duration.Text = item.Rule?.Duration ?? _snapshot?.Config.Timer.DefaultDuration ?? "00:08:00";
        _mode.SelectedItem = item.Rule?.Mode ?? _snapshot?.Config.Timer.Mode ?? TimerMode.Countdown;
        _ruleEnabled.IsChecked = item.Rule?.Enabled ?? false;
        UpdateCommandAvailability(_snapshot?.PresentationState ?? new PresentationState());
    }

    private void UpdateCommandAvailability(PresentationState state)
    {
        var selected = !string.IsNullOrWhiteSpace(_selectedPresentationId);
        SetEnabled(PresentationCommandKind.OpenPresentation, selected);
        SetEnabled(PresentationCommandKind.StartFromBeginning, selected && !state.IsOperationBusy);
        SetEnabled(PresentationCommandKind.StartFromCurrent, selected && !state.IsOperationBusy);
        SetEnabled(PresentationCommandKind.Previous, state.IsSlideShowRunning);
        SetEnabled(PresentationCommandKind.Next, state.IsSlideShowRunning);
        SetEnabled(PresentationCommandKind.ToggleBlackScreen, state.IsSlideShowRunning);
        SetEnabled(PresentationCommandKind.ToggleWhiteScreen, state.IsSlideShowRunning);
        SetEnabled(PresentationCommandKind.EndShow, state.IsSlideShowRunning && (!state.WpsDetected || state.PowerPointRunning || state.WpsCapabilities.CanEndSlideShow));
        SetEnabled(PresentationCommandKind.CloseActivePresentation, state.HasPresentation && (!state.WpsDetected || state.PowerPointRunning || state.WpsCapabilities.CanClosePresentation));
        SetEnabled(PresentationCommandKind.CloseCurrentPresentation, state.OpenPresentationCount > 0 && (!state.WpsDetected || state.PowerPointRunning || state.WpsCapabilities.CanClosePresentation));
    }

    private void SetEnabled(PresentationCommandKind kind, bool value)
    {
        if (_commandButtons.TryGetValue(kind, out var button)) button.IsEnabled = value;
    }

    private void ApplyEndpoint()
    {
        if (!int.TryParse(_port.Text, NumberStyles.None, CultureInfo.InvariantCulture, out var port)) port = 0;
        if (!_dashboard.TryApplyEndpoint(_randomPort.IsChecked == true, port, out var error))
        {
            _serviceStatus.Text = error;
            return;
        }
        RefreshSnapshot();
    }

    private void DisconnectAll()
    {
        if (Wpf.MessageBox.Show(this, "断开后旧链接立即失效，是否继续？", "断开所有设备",
                Wpf.MessageBoxButton.YesNo, Wpf.MessageBoxImage.Warning) != Wpf.MessageBoxResult.Yes) return;
        _dashboard.DisconnectAll();
        RefreshSnapshot();
    }

    private void UpdateAccessLink()
    {
        var full = _dashboard.BuildAccessUrl((_address.SelectedItem as AddressChoice)?.Address ?? "");
        _url.Text = string.IsNullOrWhiteSpace(full) ? "未检测到可用地址" : RemoteUrlPrivacy.MaskToken(full);
        _url.Tag = full;
        _qr.Source = string.IsNullOrWhiteSpace(full) ? null : CreateQr(full);
    }

    private void CopyAccessUrl()
    {
        if (_url.Tag is string { Length: > 0 } value) Wpf.Clipboard.SetText(value);
    }

    private void OpenAccessUrl()
    {
        if (_url.Tag is not string { Length: > 0 } value) return;
        try { Process.Start(new ProcessStartInfo(value) { UseShellExecute = true }); }
        catch { _serviceStatus.Text = "无法打开浏览器。"; }
    }

    private void AddRules()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "添加演示文稿",
            Filter = "PowerPoint (*.ppt;*.pptx;*.pptm;*.pps;*.ppsx)|*.ppt;*.pptx;*.pptm;*.pps;*.ppsx|所有文件 (*.*)|*.*",
            Multiselect = true
        };
        if (dialog.ShowDialog(this) != true) return;
        _dashboard.AddRules(dialog.FileNames);
        RefreshSnapshot();
    }

    private void SaveRule()
    {
        if (string.IsNullOrWhiteSpace(_selectedPresentationPath))
        {
            _presentationStatus.Text = "请先选择。";
            return;
        }
        if (!_dashboard.TryUpdateRule(
                _selectedPresentationPath,
                _duration.Text,
                _mode.SelectedItem is TimerMode mode ? mode : TimerMode.Countdown,
                _ruleEnabled.IsChecked == true,
                out var error))
        {
            _presentationStatus.Text = error;
            return;
        }
        _presentationStatus.Text = "已保存。";
        RefreshSnapshot();
    }

    private void DeleteRule()
    {
        if (!_dashboard.RemoveRule(_selectedPresentationPath))
        {
            _presentationStatus.Text = "请先选择。";
            return;
        }
        _selectedPresentationPath = _selectedPresentationId = "";
        RefreshSnapshot();
    }

    private void ClearRules()
    {
        if (Wpf.MessageBox.Show(this, "确定清空全部文件规则？", "清空规则",
                Wpf.MessageBoxButton.YesNo, Wpf.MessageBoxImage.Warning) != Wpf.MessageBoxResult.Yes) return;
        _dashboard.ClearRules();
        _selectedPresentationPath = _selectedPresentationId = "";
        RefreshSnapshot();
    }

    private void ExecutePresentation(PresentationCommandKind kind)
    {
        var needsSelection = kind is PresentationCommandKind.OpenPresentation
            or PresentationCommandKind.StartFromBeginning
            or PresentationCommandKind.StartFromCurrent;
        var result = _dashboard.Execute(new PresentationCommand(
            kind,
            needsSelection ? _selectedPresentationId : null));
        _presentationStatus.Text = result.Message;
        RefreshSnapshot();
    }

    private void GoToSlide()
    {
        if (!int.TryParse(_slideNumber.Text, out var number) || number <= 0)
        {
            _presentationStatus.Text = "请输入有效页码。";
            return;
        }
        var result = _dashboard.Execute(new PresentationCommand(PresentationCommandKind.GoToSlide, SlideNumber: number));
        _presentationStatus.Text = result.Message;
        RefreshSnapshot();
    }

    private void ForceQuit()
    {
        if (Wpf.MessageBox.Show(this, "强制退出会丢失所有未保存内容，请再次确认。", "强制退出演示软件",
                Wpf.MessageBoxButton.YesNo, Wpf.MessageBoxImage.Warning) != Wpf.MessageBoxResult.Yes) return;
        var result = _dashboard.Queue(new PresentationCommand(PresentationCommandKind.ForceQuitAll, Confirmed: true));
        _presentationStatus.Text = result.Message;
    }

    private void ApplyResponsiveColumns()
    {
        if (Content is not WpfControls.Grid root || root.Children.OfType<WpfControls.TabControl>().FirstOrDefault() is not { } tabs) return;
        if (tabs.Items[1] is not WpfControls.TabItem { Content: WpfControls.Grid presentationGrid }) return;
        presentationGrid.ColumnDefinitions[0].Width = new Wpf.GridLength(ActualWidth < 900 ? 250 : 320);
    }

    private void RestorePlacement()
    {
        var placement = _dashboard.GetSnapshot().Config.RemoteControl.Window;
        if (!placement.HasValue) return;
        Width = Math.Max(MinWidth, placement.WidthDip);
        Height = Math.Max(MinHeight, placement.HeightDip);
        var screens = System.Windows.Forms.Screen.AllScreens.Select(RemoteScreenDpiProvider.FromScreen).ToArray();
        var plan = RemoteWindowLayoutService.CreateRestorePlan(placement, screens, null, System.Drawing.Size.Empty);
        Left = plan.WindowBoundsPhysical.Left * 96d / plan.Screen.Dpi;
        Top = plan.WindowBoundsPhysical.Top * 96d / plan.Screen.Dpi;
        if (plan.Maximized) WindowState = Wpf.WindowState.Maximized;
    }

    private void SavePlacement()
    {
        if (WindowState == Wpf.WindowState.Minimized) return;
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero || !FlyPPTTimer.Native.NativeMethods.GetWindowRect(handle, out var rect)) return;
        var screen = System.Windows.Forms.Screen.FromRectangle(new System.Drawing.Rectangle(rect.Left, rect.Top, rect.Width, rect.Height));
        var metrics = RemoteScreenDpiProvider.FromScreen(screen);
        var bounds = WindowState == Wpf.WindowState.Maximized
            ? new System.Drawing.Rectangle(
                (int)Math.Round(RestoreBounds.Left * metrics.Dpi / 96d),
                (int)Math.Round(RestoreBounds.Top * metrics.Dpi / 96d),
                (int)Math.Round(RestoreBounds.Width * metrics.Dpi / 96d),
                (int)Math.Round(RestoreBounds.Height * metrics.Dpi / 96d))
            : new System.Drawing.Rectangle(rect.Left, rect.Top, rect.Width, rect.Height);
        var client = new System.Drawing.Size(
            (int)Math.Round((WindowState == Wpf.WindowState.Maximized ? RestoreBounds.Width : ActualWidth) * metrics.Dpi / 96d),
            (int)Math.Round((WindowState == Wpf.WindowState.Maximized ? RestoreBounds.Height : ActualHeight) * metrics.Dpi / 96d));
        _dashboard.SaveWindowPlacement(RemoteWindowLayoutService.CapturePlacement(
            bounds,
            client,
            metrics,
            WindowState == Wpf.WindowState.Maximized));
    }

    private WpfControls.Button CommandButton(string label, PresentationCommandKind kind)
    {
        var button = Click(Button(label, "Presentation" + kind), () => ExecutePresentation(kind));
        _commandButtons[kind] = button;
        return button;
    }

    private static WpfControls.WrapPanel CommandRow(params Wpf.UIElement[] children) => Row(children);

    private static WpfControls.Button Click(WpfControls.Button button, Action action)
    {
        button.Click += (_, _) => action();
        return button;
    }

    private static WpfControls.Button Button(string text, string automationId)
    {
        var button = new WpfControls.Button
        {
            Content = text,
            Margin = FieldMargin,
            Padding = new Wpf.Thickness(12, 7, 12, 7),
            MinHeight = 34
        };
        AutomationProperties.SetAutomationId(button, automationId);
        return button;
    }

    private static WpfControls.TextBox TextBox(string automationId, bool readOnly = false)
    {
        var box = new WpfControls.TextBox
        {
            Margin = FieldMargin,
            Padding = new Wpf.Thickness(8, 6, 8, 6),
            IsReadOnly = readOnly,
            Background = readOnly ? Brush("#F2F4F7") : WpfMedia.Brushes.White,
            BorderBrush = Brush("#D0D5DD")
        };
        AutomationProperties.SetAutomationId(box, automationId);
        return box;
    }

    private static WpfControls.TextBlock TitleText(string value) => Text(value, 16, Wpf.FontWeights.SemiBold);

    private static WpfControls.TextBlock Text(
        string value,
        double size = 13,
        Wpf.FontWeight? weight = null,
        string color = "#344054") => new()
    {
        Text = value,
        FontSize = size,
        FontWeight = weight ?? Wpf.FontWeights.Normal,
        Foreground = Brush(color),
        Margin = new Wpf.Thickness(6, 4, 6, 4),
        TextWrapping = Wpf.TextWrapping.Wrap,
        VerticalAlignment = Wpf.VerticalAlignment.Center
    };

    private static WpfControls.StackPanel Stack(params Wpf.UIElement[] children)
    {
        var panel = new WpfControls.StackPanel();
        foreach (var child in children) panel.Children.Add(child);
        return panel;
    }

    private static WpfControls.WrapPanel Row(params Wpf.UIElement[] children)
    {
        var panel = new WpfControls.WrapPanel { VerticalAlignment = Wpf.VerticalAlignment.Center };
        foreach (var child in children)
        {
            if (child is Wpf.FrameworkElement element && element.Margin == default) element.Margin = FieldMargin;
            panel.Children.Add(child);
        }
        return panel;
    }

    private static WpfControls.Border Card(Wpf.UIElement child) => new()
    {
        Child = child,
        Background = WpfMedia.Brushes.White,
        BorderBrush = Brush("#E4E7EC"),
        BorderThickness = new Wpf.Thickness(1),
        CornerRadius = new Wpf.CornerRadius(10),
        Padding = new Wpf.Thickness(16),
        Margin = new Wpf.Thickness(6)
    };

    private static WpfControls.ScrollViewer Scroll(Wpf.UIElement child) => new()
    {
        Content = child,
        VerticalScrollBarVisibility = WpfControls.ScrollBarVisibility.Auto,
        HorizontalScrollBarVisibility = WpfControls.ScrollBarVisibility.Disabled
    };

    private static WpfMedia.Brush Brush(string value) =>
        (WpfMedia.Brush)new WpfMedia.BrushConverter().ConvertFromString(value)!;

    private static WpfMediaImaging.BitmapImage CreateQr(string value)
    {
        using var data = QRCodeGenerator.GenerateQrCode(value, QRCodeGenerator.ECCLevel.Q);
        var bytes = new PngByteQRCode(data).GetGraphic(8);
        using var stream = new MemoryStream(bytes);
        var image = new WpfMediaImaging.BitmapImage();
        image.BeginInit();
        image.CacheOption = WpfMediaImaging.BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }

    private static string BuildPresentationSummary(PresentationListEntry entry)
    {
        var states = new List<string>();
        if (entry.Presentation?.IsSlideShowRunning == true) states.Add("放映中");
        else if (entry.Presentation?.IsOpen == true) states.Add("已打开");
        if (entry.Presentation?.IsManaged == true) states.Add("受管只读");
        if (entry.Rule is { Enabled: false }) states.Add("规则已禁用");
        return states.Count == 0 ? "未打开" : string.Join(" · ", states);
    }

    private static bool SamePath(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right)) return false;
        return string.Equals(PresentationRuleValidator.NormalizePath(left),
            PresentationRuleValidator.NormalizePath(right), StringComparison.OrdinalIgnoreCase);
    }

    private static readonly Wpf.Thickness FieldMargin = new(5);

    private sealed record AddressChoice(string Address, string Label);
    private sealed record DashboardPresentationItem(
        string Path,
        string Name,
        string Summary,
        FileRule? Rule,
        PresentationOption? Presentation)
    {
        public string Display => $"{Name}\n{Summary}";
    }
}
