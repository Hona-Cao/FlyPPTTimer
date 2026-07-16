using FlyPPTTimer.Models;
using FlyPPTTimer.Services;
using QRCoder;
using System.Diagnostics;
using System.Globalization;

namespace FlyPPTTimer.Forms;

public sealed class RemoteControlForm : Form
{
    private AppConfig _config;
    private readonly RemoteControlService _remoteControl;
    private readonly PowerPointControlService? _powerPoint;
    private readonly NetworkAddressService _networkAddressService;
    private readonly Action<AppConfig> _saveConfig;

    private readonly PictureBox _qr = new() { SizeMode = PictureBoxSizeMode.Zoom, Dock = DockStyle.Fill };
    private readonly FlatSelectBox _address = new();
    private readonly TextBox _url = new() { ReadOnly = true, BorderStyle = BorderStyle.None };
    private readonly TextBox _port = new() { BorderStyle = BorderStyle.None, TextAlign = HorizontalAlignment.Center };
    private readonly Label _state = new()
    {
        AutoSize = false,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft
    };
    private readonly Label _hint = new()
    {
        AutoSize = false,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.TopLeft,
        ForeColor = Color.FromArgb(80, 96, 104)
    };
    private readonly Button _toggle = new();
    private readonly ToolTip _toolTip = new()
    {
        AutoPopDelay = 30000,
        InitialDelay = 300,
        ReshowDelay = 100,
        ShowAlways = true
    };
    private ContextMenuStrip? _moreActionsMenu;
    private readonly FlowLayoutPanel _ruleList = new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = false,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        BackColor = ModernTheme.ControlFill,
        Padding = new Padding(8)
    };
    private readonly Label _presentationStatus = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = ModernTheme.MutedText };
    private readonly TextBox _ruleDuration = new() { BorderStyle = BorderStyle.None };
    private readonly Button _ruleEnabled = new() { Text = "已启用", UseCompatibleTextRendering = false };
    private readonly Label _rulePath = new() { Dock = DockStyle.Fill, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft };
    private FileRule? _selectedRule;
    private string? _selectedPresentationId;
    private string? _selectedPresentationPath;
    private bool _updatingRuleEditor;
    private bool _durationDirty;
    private readonly Dictionary<string, PresentationRuleRow> _ruleRows = new(StringComparer.OrdinalIgnoreCase);
    private Panel? _contentHost;
    private Control? _connectionPage;
    private Control? _presentationPage;
    private Panel? _presentationScroll;
    private TableLayoutPanel? _presentationLayout;
    private Control? _presentationToolbar;
    private Control? _presentationEditor;
    private Control? _presentationActions;
    private Button? _connectionNav;
    private Button? _presentationNav;
    private readonly System.Windows.Forms.Timer _presentationRefreshTimer = new() { Interval = 1000 };

    public RemoteControlForm(AppConfig config, RemoteControlService remoteControl, NetworkAddressService networkAddressService, Action<AppConfig> saveConfig)
    {
        _config = config;
        _remoteControl = remoteControl;
        _powerPoint = remoteControl.PresentationController;
        _networkAddressService = networkAddressService;
        _saveConfig = saveConfig;

        Text = "远程控制";
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        _state.Font = Font;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = false;
        ClientSize = new Size(1000, 720);
        MinimumSize = new Size(760, 600);
        BackColor = ModernTheme.Surface;

        Build();
        RefreshState();
        VisibleChanged += (_, _) => UpdatePresentationRefreshState();
    }

    public void ReloadConfig(AppConfig config)
    {
        _config = config;
        if (!IsDisposed) RefreshState();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _presentationRefreshTimer.Dispose();
        _moreActionsMenu?.Dispose();
        _qr.Image?.Dispose();
        base.OnFormClosed(e);
    }

    private void UpdatePresentationRefreshState()
    {
        _presentationRefreshTimer.Enabled = Visible && _presentationTabActive;
        if (_presentationRefreshTimer.Enabled) RefreshPresentationPanel();
    }

    private bool _presentationTabActive;

    private void Build()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(18),
            BackColor = ModernTheme.Surface
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        root.Controls.Add(Header(), 0, 0);
        root.Controls.Add(StatusStrip(), 0, 1);
        root.Controls.Add(BuildContentPages(), 0, 2);
    }

    private Control BuildContentPages()
    {
        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = ModernTheme.Surface,
            Margin = Padding.Empty
        };
        shell.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var navCard = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = ModernTheme.HeaderFill,
            Padding = new Padding(8, 5, 8, 5),
            Margin = new Padding(0, 0, 0, 8)
        };
        ModernTheme.StyleRounded(navCard, ModernTheme.CardRadius);
        _connectionNav = NavigationButton("远程连接", (_, _) => ShowContentPage(false));
        _presentationNav = NavigationButton("演示文稿", (_, _) => ShowContentPage(true));
        navCard.Controls.Add(_connectionNav);
        navCard.Controls.Add(_presentationNav);
        shell.Controls.Add(navCard, 0, 0);

        _contentHost = new Panel { Dock = DockStyle.Fill, BackColor = ModernTheme.Surface, AutoScroll = false };
        _connectionPage = ConnectionPanel();
        _presentationPage = PresentationPanel();
        _contentHost.Controls.Add(_connectionPage);
        _contentHost.Controls.Add(_presentationPage);
        shell.Controls.Add(_contentHost, 0, 1);
        _presentationRefreshTimer.Tick += (_, _) => RefreshPresentationPanel();
        ShowContentPage(false);
        return shell;
    }

    private Button NavigationButton(string text, EventHandler handler)
    {
        var button = NewActionButton(text, handler, 104, primary: false);
        button.Height = ModernTheme.StandardControlHeight;
        button.Margin = new Padding(0, 0, 8, 0);
        return button;
    }

    private void ShowContentPage(bool presentation)
    {
        if (_connectionPage is null || _presentationPage is null || _connectionNav is null || _presentationNav is null) return;
        _connectionPage.Visible = !presentation;
        _presentationPage.Visible = presentation;
        _connectionPage.BringToFront();
        if (presentation) _presentationPage.BringToFront();
        _connectionNav.BackColor = presentation ? ModernTheme.ControlFill : Color.White;
        _connectionNav.ForeColor = presentation ? ModernTheme.Text : ModernTheme.AccentStrong;
        _presentationNav.BackColor = presentation ? Color.White : ModernTheme.ControlFill;
        _presentationNav.ForeColor = presentation ? ModernTheme.AccentStrong : ModernTheme.Text;
        _presentationTabActive = presentation;
        UpdatePresentationRefreshState();
        if (presentation) ReflowPresentationLayout();
    }

    private Control Header()
    {
        var panel = Card(new Padding(16, 6, 16, 6));
        panel.Dock = DockStyle.Top;
        panel.AutoSize = false;
        panel.Height = SectionHeight();
        panel.MinimumSize = new Size(0, panel.Height);
        panel.BackColor = ModernTheme.HeaderFill;
        panel.ColumnCount = 2;
        panel.RowCount = 1;
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 76));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.Controls.Add(new Label
        {
            Text = "手机遥控",
            Dock = DockStyle.Fill,
            Font = new Font(Font, FontStyle.Bold),
            ForeColor = ModernTheme.Text,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = Padding.Empty
        }, 0, 0);
        _hint.AutoSize = false;
        _hint.Dock = DockStyle.Fill;
        _hint.AutoEllipsis = true;
        _hint.TextAlign = ContentAlignment.MiddleLeft;
        _hint.Text = "同一网络下扫码即可控制开始、暂停、重置和显示状态。";
        panel.Controls.Add(_hint, 1, 0);

        return panel;
    }

    private Control StatusStrip()
    {
        var panel = Card(new Padding(16, 6, 16, 6));
        panel.Dock = DockStyle.Top;
        panel.AutoSize = false;
        panel.Height = SectionHeight();
        panel.MinimumSize = new Size(0, panel.Height);
        panel.ColumnCount = 4;
        panel.RowCount = 1;
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118));

        panel.Controls.Add(_state, 0, 0);
        panel.Controls.Add(PortPanel(), 1, 0);
        panel.Controls.Add(FillButton("重启服务", (_, _) => RestartService()), 2, 0);
        _toggle.Text = "关闭服务";
        _toggle.Width = 110;
        _toggle.Height = ModernTheme.StandardControlHeight;
        _toggle.Click += (_, _) => ToggleService();
        ModernTheme.StyleRounded(_toggle, ModernTheme.ButtonRadius);
        panel.Controls.Add(Center(_toggle, ModernTheme.Card), 3, 0);
        return panel;
    }

    private static int SectionHeight() => ModernTheme.StandardControlHeight + 12;

    private Control PortPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.White,
            Padding = new Padding(8, 0, 8, 0)
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 62));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.Controls.Add(new Label
        {
            Text = "端口",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = Color.FromArgb(80, 96, 104)
        }, 0, 0);
        panel.Controls.Add(Host(_port, new Padding(10, 8, 10, 6), Color.FromArgb(242, 246, 248)), 1, 0);
        return panel;
    }

    private Control ConnectionPanel()
    {
        var scroll = new Panel { Dock = DockStyle.Fill, BackColor = ModernTheme.Surface, AutoScroll = true };
        var panel = Card(new Padding(24, 16, 24, 16));
        panel.Dock = DockStyle.Top;
        panel.MinimumSize = new Size(720, 380);
        panel.Height = 380;
        panel.ColumnCount = 2;
        panel.RowCount = 1;
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 330));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var qrCard = new Panel { Dock = DockStyle.Fill, BackColor = ModernTheme.ControlFill, Padding = new Padding(14) };
        ModernTheme.StyleRounded(qrCard, ModernTheme.CardRadius);
        qrCard.Controls.Add(_qr);
        panel.Controls.Add(qrCard, 0, 0);

        var side = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 9,
            ColumnCount = 1,
            BackColor = Color.White,
            Padding = new Padding(24, 0, 0, 0)
        };
        side.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        side.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        side.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        side.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        side.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        side.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        side.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        side.RowStyles.Add(new RowStyle(SizeType.Absolute, 6));
        side.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        side.Controls.Add(Label("选择本机地址"), 0, 0);
        _address.SelectedIndexChanged += (_, _) => UpdateUrlAndQr();
        side.Controls.Add(Host(_address, new Padding(10, 7, 10, 6), Color.FromArgb(242, 246, 248)), 0, 1);
        side.Controls.Add(Label("访问链接"), 0, 2);
        side.Controls.Add(Host(_url, new Padding(10, 8, 10, 6), Color.FromArgb(242, 246, 248)), 0, 3);
        side.Controls.Add(FillButton("复制链接", (_, _) => Clipboard.SetText(CurrentUrl()), primary: true), 0, 4);
        side.Controls.Add(FillButton("在本机浏览器打开", (_, _) => OpenUrl(CurrentUrl())), 0, 5);
        side.Controls.Add(FillButton("复制放行命令", (_, _) => Clipboard.SetText(BuildFirewallCommand())), 0, 6);
        side.Controls.Add(new Label
        {
            Text = "手机无法访问时，请先确认手机和电脑在同一 Wi-Fi，再复制放行命令添加防火墙规则。",
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(80, 96, 104),
            TextAlign = ContentAlignment.TopLeft
        }, 0, 8);

        panel.Controls.Add(side, 1, 0);
        scroll.Controls.Add(panel);
        // The page height follows its content; filling the viewport vertically
        // leaves a large empty card after resize.
        void Reflow() => panel.Width = Math.Max(720, scroll.ClientSize.Width);
        scroll.Resize += (_, _) => Reflow();
        scroll.HandleCreated += (_, _) => BeginInvoke((MethodInvoker)Reflow);
        return scroll;
    }

    private Control PresentationPanel()
    {
        var scroll = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ModernTheme.Surface,
            AutoScroll = true
        };
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = false,
            MinimumSize = new Size(720, 0),
            ColumnCount = 1,
            RowCount = 4,
            BackColor = ModernTheme.Surface,
            Padding = new Padding(0, 0, 4, 0)
        };
        _ruleList.AutoScroll = false;
        _ruleList.Dock = DockStyle.Top;
        _ruleList.MinimumSize = new Size(0, PresentationRuleRow.MinimumHeightFor(Font) * 3 + _ruleList.Padding.Vertical);
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, _ruleList.MinimumSize.Height));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _presentationToolbar = PresentationToolbar();
        _presentationEditor = BuildRuleEditor();
        _presentationActions = BuildPresentationActions();
        root.Controls.Add(_presentationToolbar, 0, 0);

        _ruleList.BackColor = ModernTheme.Card;
        _ruleList.Padding = new Padding(12);
        ModernTheme.StyleRounded(_ruleList, ModernTheme.CardRadius);
        _ruleList.Margin = new Padding(0, 0, 0, 10);
        root.Controls.Add(_ruleList, 0, 1);
        root.Controls.Add(_presentationEditor, 0, 2);
        root.Controls.Add(_presentationActions, 0, 3);
        _presentationScroll = scroll;
        _presentationLayout = root;
        _ruleList.MouseWheel += RoutePresentationWheel;
        scroll.Controls.Add(root);
        scroll.Resize += (_, _) => ReflowPresentationLayout();
        scroll.HandleCreated += (_, _) => BeginInvoke((MethodInvoker)ReflowPresentationLayout);
        return scroll;
    }

    private void ReflowPresentationLayout()
    {
        if (_presentationScroll is null || _presentationLayout is null || _presentationToolbar is null || _presentationEditor is null || _presentationActions is null) return;

        var width = Math.Max(_presentationLayout.MinimumSize.Width, _presentationScroll.ClientSize.Width);
        _presentationLayout.Width = width;
        _presentationLayout.PerformLayout();

        var minimumListHeight = PresentationRuleRow.MinimumHeightFor(Font) * 3 + _ruleList.Padding.Vertical;
        var rulesHeight = _ruleList.Padding.Vertical + _ruleList.Controls.Cast<Control>().Where(control => control.Visible).Sum(control => control.Height + control.Margin.Vertical);
        var fixedHeight = _presentationToolbar.GetPreferredSize(new Size(width, 0)).Height
            + _presentationEditor.GetPreferredSize(new Size(width, 0)).Height
            + _presentationActions.GetPreferredSize(new Size(width, 0)).Height
            + _presentationLayout.Padding.Vertical;
        var viewportHeight = Math.Max(0, _presentationScroll.ClientSize.Height - fixedHeight);
        var listHeight = Math.Max(minimumListHeight, Math.Max(rulesHeight, viewportHeight));

        _ruleList.Height = listHeight;
        _presentationLayout.RowStyles[1].SizeType = SizeType.Absolute;
        _presentationLayout.RowStyles[1].Height = listHeight;
        _presentationLayout.Height = fixedHeight + listHeight;
        _presentationLayout.PerformLayout();
    }

    private void RoutePresentationWheel(object? sender, MouseEventArgs e)
    {
        if (_presentationScroll is null) return;
        var maximum = Math.Max(0, _presentationScroll.VerticalScroll.Maximum - _presentationScroll.VerticalScroll.LargeChange + 1);
        var step = Math.Max(Font.Height * 3, 48);
        var current = -_presentationScroll.AutoScrollPosition.Y;
        var next = Math.Clamp(current + (e.Delta < 0 ? step : -step), 0, maximum);
        _presentationScroll.AutoScrollPosition = new Point(0, next);
        if (e is HandledMouseEventArgs handled) handled.Handled = true;
    }

    private Control PresentationToolbar()
    {
        var card = Card(new Padding(12, 8, 12, 8));
        card.Dock = DockStyle.Top;
        card.AutoSize = true;
        card.ColumnCount = 1;
        card.RowCount = 1;
        card.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var buttons = NewWrappingActions();
        buttons.Controls.Add(NewActionButton("添加 PPT", (_, _) => AddPresentationRules(), 104, primary: true));
        buttons.Controls.Add(NewActionButton("删除规则", (_, _) => DeleteSelectedRule(), 104, danger: true));
        buttons.Controls.Add(NewActionButton("刷新状态", (_, _) => RefreshPresentationPanel(), 104));
        _presentationStatus.AutoSize = true;
        _presentationStatus.Dock = DockStyle.None;
        _presentationStatus.AutoEllipsis = true;
        _presentationStatus.Margin = new Padding(4, 0, 0, 8);
        buttons.Controls.Add(_presentationStatus);
        card.Controls.Add(buttons, 0, 0);
        return card;
    }

    private Control BuildRuleEditor()
    {
        var card = Card(new Padding(14, 10, 14, 10));
        card.Dock = DockStyle.Top;
        card.AutoSize = true;
        card.Margin = new Padding(0, 0, 0, 10);
        card.ColumnCount = 1;
        card.RowCount = 2;
        card.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        card.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var body = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, RowCount = 2, Margin = new Padding(0, 6, 0, 0) };
        body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var pathRow = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, RowCount = 1, Margin = new Padding(0, 0, 0, 6) };
        pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 52));
        pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        pathRow.Controls.Add(new Label { Text = "路径", AutoSize = true, Anchor = AnchorStyles.Left, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        _toolTip.SetToolTip(_rulePath, "");
        pathRow.Controls.Add(Host(_rulePath, new Padding(10, 8, 10, 8), ModernTheme.ControlFill), 1, 0);
        body.Controls.Add(pathRow, 0, 0);
        var editActions = NewWrappingActions();
        editActions.Controls.Add(new Label { Text = "时长", AutoSize = false, Width = 52, Height = ModernTheme.StandardControlHeight, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(0, 0, 4, 0) });
        var durationHost = Host(_ruleDuration, new Padding(10, 8, 10, 8), ModernTheme.ControlFill);
        durationHost.Width = 130;
        durationHost.Height = ModernTheme.StandardControlHeight;
        editActions.Controls.Add(durationHost);
        ModernTheme.StyleRounded(_ruleEnabled, ModernTheme.ControlRadius);
        _ruleEnabled.Height = ModernTheme.StandardControlHeight;
        _ruleEnabled.MinimumSize = new Size(76, ModernTheme.StandardControlHeight);
        _ruleEnabled.Click += (_, _) => ToggleSelectedRuleEnabled();
        editActions.Controls.Add(_ruleEnabled);
        editActions.Controls.Add(NewActionButton("保存时长", (_, _) => SaveSelectedDuration(), 108, primary: true));
        editActions.Controls.Add(MoreActionsButton());
        body.Controls.Add(editActions, 0, 1);
        card.Controls.Add(CollapsibleHeader("规则编辑", body), 0, 0);
        card.Controls.Add(body, 0, 1);
        _ruleDuration.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            SaveSelectedDuration();
        };
        _ruleDuration.TextChanged += (_, _) => { if (!_updatingRuleEditor) _durationDirty = true; };
        _ruleDuration.Leave += (_, _) => SaveSelectedDuration();
        return card;
    }

    private Control BuildPresentationActions()
    {
        var card = Card(new Padding(14, 10, 14, 10));
        card.Dock = DockStyle.Top;
        card.AutoSize = true;
        card.ColumnCount = 1;
        card.RowCount = 2;
        card.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        card.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var body = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, RowCount = 3, Margin = new Padding(0, 6, 0, 0) };
        body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var main = NewWrappingActions();
        main.Controls.Add(NewActionButton("只读打开/切换", (_, _) => SendPresentationCommand("ppt.openPresentation"), 140, primary: true));
        main.Controls.Add(NewActionButton("从头放映", (_, _) => SendPresentationCommand("ppt.startFromBeginning"), 104));
        main.Controls.Add(NewActionButton("从当前页放映", (_, _) => SendPresentationCommand("ppt.startFromCurrent"), 124));
        main.Controls.Add(NewActionButton("结束放映", (_, _) => SendPresentationCommand("ppt.endShow"), 104));
        body.Controls.Add(main, 0, 0);
        body.Controls.Add(SectionTitle("危险操作", ModernTheme.Danger), 0, 1);
        var danger = NewWrappingActions();
        danger.Margin = new Padding(0, 4, 0, 0);
        danger.Controls.Add(NewActionButton("退出 PowerPoint", (_, _) => SendPresentationCommand("ppt.exitApplication"), 132, danger: true));
        danger.Controls.Add(NewActionButton("强制退出 PowerPoint/WPS", (_, _) => ConfirmForceQuit(), 196, danger: true));
        body.Controls.Add(danger, 0, 2);
        card.Controls.Add(CollapsibleHeader("放映操作", body), 0, 0);
        card.Controls.Add(body, 0, 1);
        return card;
    }

    private Button CollapsibleHeader(string title, Control body)
    {
        var button = NewActionButton($"{title}  展开", (_, _) => { }, 120);
        button.Dock = DockStyle.Top;
        button.Margin = Padding.Empty;
        button.BackColor = ModernTheme.SectionFill;
        button.ForeColor = ModernTheme.AccentStrong;
        button.FlatAppearance.BorderColor = ModernTheme.SectionFill;
        body.Visible = false;
        button.Click += (_, _) =>
        {
            body.Visible = !body.Visible;
            button.Text = body.Visible ? $"{title}  收起" : $"{title}  展开";
            button.Parent?.PerformLayout();
            ReflowPresentationLayout();
        };
        return button;
    }

    private FlowLayoutPanel NewWrappingActions() => new()
    {
        Dock = DockStyle.Top,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        FlowDirection = FlowDirection.LeftToRight,
        WrapContents = true,
        BackColor = Color.Transparent,
        Margin = Padding.Empty,
        Padding = Padding.Empty
    };

    private Label SectionTitle(string text, Color? color = null) => new()
    {
        Text = text,
        AutoSize = true,
        Font = new Font(Font, FontStyle.Bold),
        ForeColor = color ?? ModernTheme.AccentStrong,
        Margin = new Padding(0, 0, 0, 4)
    };

    private Button NewActionButton(string text, EventHandler handler, int minimumWidth, bool primary = false, bool danger = false)
    {
        var measured = TextRenderer.MeasureText(text, Font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix).Width + 30;
        var button = new Button { Text = text, Height = ModernTheme.StandardControlHeight, MinimumSize = new Size(0, ModernTheme.StandardControlHeight), Width = Math.Max(minimumWidth, measured), Margin = new Padding(0, 0, 8, 8), UseCompatibleTextRendering = false, AutoSize = false };
        button.Click += handler;
        ModernTheme.StyleRounded(button, ModernTheme.ButtonRadius);
        button.BackColor = primary ? ModernTheme.AccentStrong : danger ? ModernTheme.DangerSoft : ModernTheme.AccentSoft;
        button.ForeColor = primary ? Color.White : danger ? ModernTheme.Danger : ModernTheme.Text;
        button.FlatAppearance.BorderColor = button.BackColor;
        button.FlatAppearance.MouseOverBackColor = primary ? ModernTheme.Accent : danger ? Color.FromArgb(244, 214, 216) : ModernTheme.ControlHover;
        button.FlatAppearance.MouseDownBackColor = primary ? Color.FromArgb(9, 69, 62) : danger ? Color.FromArgb(238, 200, 203) : Color.FromArgb(214, 227, 231);
        return button;
    }

    private TableLayoutPanel Card(Padding padding)
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = padding,
            Margin = new Padding(0, 0, 0, 12),
            BackColor = Color.White
        };
        ModernTheme.StyleRounded(panel, ModernTheme.CardRadius);
        return panel;
    }

    private static Control Center(Control control, Color? fill = null)
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = fill ?? ModernTheme.Card };
        panel.Controls.Add(control, 0, 0);
        control.Anchor = AnchorStyles.None;
        return panel;
    }

    private static Label Label(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Dock = DockStyle.Top,
        ForeColor = Color.FromArgb(80, 96, 104),
        TextAlign = ContentAlignment.MiddleLeft,
        Margin = new Padding(0, 0, 0, 4)
    };

    private static Button FillButton(string text, EventHandler handler, bool primary = false)
    {
        var button = new Button
        {
            Text = text,
            Dock = DockStyle.Fill,
            Height = ModernTheme.StandardControlHeight,
            MinimumSize = new Size(0, ModernTheme.StandardControlHeight),
            Margin = new Padding(0, 2, 0, 2),
            AutoSize = false,
            Padding = new Padding(12, 0, 12, 0),
            UseCompatibleTextRendering = false,
            BackColor = primary ? ModernTheme.AccentStrong : ModernTheme.AccentSoft,
            ForeColor = primary ? Color.White : ModernTheme.Text
        };
        button.Click += handler;
        ModernTheme.StyleRounded(button, ModernTheme.ButtonRadius);
        if (primary)
        {
            button.BackColor = ModernTheme.AccentStrong;
            button.ForeColor = Color.White;
            button.FlatAppearance.MouseOverBackColor = ModernTheme.Accent;
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(9, 69, 62);
        }
        return button;
    }

    private static Button Button(string text, EventHandler handler, int width)
    {
        var button = new Button
        {
            Text = text,
            Width = width,
            MinimumSize = new Size(width, ModernTheme.StandardControlHeight),
            Margin = new Padding(6, 0, 0, 0),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(12, 0, 12, 0),
            UseCompatibleTextRendering = false,
            BackColor = ModernTheme.Card
        };
        button.Click += handler;
        ModernTheme.StyleRounded(button, ModernTheme.ButtonRadius);
        return button;
    }

    private static Control Host(Control control, Padding padding, Color fill)
    {
        var host = new RoundedHostPanel
        {
            Padding = padding,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 2, 0, 8),
            FillColor = fill,
            Height = ModernTheme.StandardControlHeight,
            MinimumSize = new Size(0, ModernTheme.StandardControlHeight)
        };
        control.Dock = DockStyle.Fill;
        control.Margin = Padding.Empty;
        control.BackColor = fill;
        host.Controls.Add(control);
        return host;
    }

    private void AddPresentationRules()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "添加演示文稿规则",
            Filter = "PowerPoint 演示文稿 (*.ppt;*.pptx;*.pptm;*.pps;*.ppsx)|*.ppt;*.pptx;*.pptm;*.pps;*.ppsx|所有文件 (*.*)|*.*",
            Multiselect = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        foreach (var path in dialog.FileNames)
        {
            var normalized = NormalizePath(path);
            if (_config.Rules.Any(rule => string.Equals(NormalizePath(rule.FilePath), normalized, StringComparison.OrdinalIgnoreCase))) continue;
            _config.Rules.Add(new FileRule { FileName = Path.GetFileName(path), FilePath = normalized, Duration = _config.Timer.DefaultDuration, Enabled = true });
        }
        SaveRulesImmediately();
        RefreshPresentationPanel();
    }

    private void DeleteSelectedRule()
    {
        if (_selectedRule is null) return;
        _config.Rules.Remove(_selectedRule);
        _selectedRule = null;
        _selectedPresentationId = null;
        _selectedPresentationPath = null;
        SaveRulesImmediately();
        RefreshPresentationPanel();
    }

    private void RefreshPresentationPanel()
    {
        if (IsDisposed) return;
        var state = _powerPoint?.GetState() ?? new PresentationState { Error = "PowerPoint 控制服务不可用。" };
        _presentationStatus.Text = !string.IsNullOrWhiteSpace(state.OperationMessage) ? state.OperationMessage : state.Error;
        RenderRuleRows(state);
        RefreshRuleEditor();
        ReflowPresentationLayout();
    }

    private void RenderRuleRows(PresentationState state)
    {
        var selectedPath = _selectedPresentationPath ?? _selectedRule?.FilePath ?? "";
        var items = PresentationRuleValidator.MergeRulesAndOpenPresentations(_config.Rules, state.Presentations);

        var scroll = _presentationScroll?.AutoScrollPosition ?? Point.Empty;
        _ruleList.SuspendLayout();
        foreach (var obsolete in _ruleRows.Keys.Except(items.Select(item => item.Path), StringComparer.OrdinalIgnoreCase).ToArray())
        {
            var row = _ruleRows[obsolete];
            _ruleList.Controls.Remove(row);
            row.Dispose();
            _ruleRows.Remove(obsolete);
        }

        var index = 0;
        foreach (var item in items)
        {
            var key = item.Path;
            if (!_ruleRows.TryGetValue(key, out var row))
            {
                row = new PresentationRuleRow();
                row.Selected += (_, _) => SelectPresentation(item.Rule, item.Presentation, item.Path);
                row.ContentMouseWheel += RoutePresentationWheel;
                row.EnabledChangedByUser += (_, enabled) =>
                {
                    if (item.Rule is null) return;
                    item.Rule.Enabled = enabled;
                    SaveRulesImmediately();
                    RefreshPresentationPanel();
                };
                _ruleRows.Add(key, row);
                _ruleList.Controls.Add(row);
            }
            row.Width = Math.Max(420, _ruleList.ClientSize.Width - _ruleList.Padding.Horizontal - 8);
            row.Update(item.Rule, item.Presentation, SamePath(item.Path, selectedPath), File.Exists(item.Path));
            _ruleList.Controls.SetChildIndex(row, index++);
        }
        _ruleList.ResumeLayout();
        if (_presentationScroll is not null) _presentationScroll.AutoScrollPosition = new Point(-scroll.X, -scroll.Y);
    }

    private void SelectPresentation(FileRule? rule, PresentationOption? option, string path)
    {
        _selectedRule = rule;
        _selectedPresentationPath = path;
        _selectedPresentationId = option?.Id ?? PresentationRuleValidator.IdForPath(path);
        RefreshPresentationPanel();
    }

    private void RefreshRuleEditor()
    {
        var rule = _selectedRule;
        if (rule is not null && !_config.Rules.Contains(rule)) rule = _selectedRule = null;
        _updatingRuleEditor = true;
        _rulePath.Text = rule?.FilePath ?? _selectedPresentationPath ?? "未选择规则";
        _toolTip.SetToolTip(_rulePath, rule?.FilePath ?? _selectedPresentationPath ?? "");
        _ruleDuration.Text = rule?.Duration ?? "";
        _durationDirty = false;
        SetRuleEnabledButton(rule?.Enabled == true);
        _ruleDuration.Enabled = _ruleEnabled.Enabled = rule is not null;
        _updatingRuleEditor = false;
    }

    private void SetRuleEnabledButton(bool enabled)
    {
        _ruleEnabled.Text = enabled ? "已启用" : "已禁用";
        _ruleEnabled.BackColor = enabled ? ModernTheme.SuccessSoft : ModernTheme.ControlFill;
        _ruleEnabled.ForeColor = enabled ? ModernTheme.Success : ModernTheme.MutedText;
    }

    private void ToggleSelectedRuleEnabled()
    {
        if (_updatingRuleEditor || _selectedRule is null) return;
        _selectedRule.Enabled = !_selectedRule.Enabled;
        SetRuleEnabledButton(_selectedRule.Enabled);
        SaveRulesImmediately();
        RefreshPresentationPanel();
    }

    private Button MoreActionsButton()
    {
        var button = NewActionButton("更多操作", (_, _) => { }, 104);
        button.Click += (_, _) => ShowMoreActions(button);
        return button;
    }

    private void ShowMoreActions(Button? button)
    {
        if (button is null || IsDisposed) return;
        _moreActionsMenu ??= CreateMoreActionsMenu();
        if (_moreActionsMenu.Visible) return;
        _moreActionsMenu.Show(button, new Point(0, button.Height));
    }

    private ContextMenuStrip CreateMoreActionsMenu()
    {
        var menu = new ContextMenuStrip
        {
            Renderer = new ModernContextMenuRenderer(),
            BackColor = Color.White,
            ForeColor = ModernTheme.Text,
            Font = Font,
            ShowImageMargin = false,
            ShowCheckMargin = false,
            AutoClose = true,
            Padding = new Padding(6)
        };
        menu.Items.Add("复制路径", null, (_, _) => CopySelectedPath());
        menu.Items.Add("在资源管理器中显示", null, (_, _) => ShowSelectedPath());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("关闭当前受控文稿", null, (_, _) => SendPresentationCommand("ppt.closeCurrentPresentation"));
        foreach (ToolStripItem item in menu.Items.OfType<ToolStripMenuItem>())
        {
            item.AutoSize = false;
            item.Height = ModernTheme.StandardControlHeight;
        }
        return menu;
    }

    private void SaveSelectedDuration()
    {
        if (_updatingRuleEditor || _selectedRule is null || !_durationDirty) return;
        if (!PresentationRuleValidator.TryNormalizeDuration(_ruleDuration.Text, out var duration, out var error))
        {
            _presentationStatus.Text = error;
            return;
        }
        if (string.Equals(_selectedRule.Duration, duration, StringComparison.Ordinal)) return;
        _selectedRule.Duration = duration;
        _ruleDuration.Text = duration;
        _durationDirty = false;
        SaveRulesImmediately();
        _presentationStatus.Text = "计时时长已保存并同步手机端。";
        RefreshPresentationPanel();
    }

    private void SaveRulesImmediately()
    {
        _saveConfig(_config);
        _remoteControl.NotifyStateChanged();
    }

    private void CopySelectedPath()
    {
        if (_selectedRule is not null) Clipboard.SetText(_selectedRule.FilePath);
    }

    private void ShowSelectedPath()
    {
        if (_selectedRule is null || !File.Exists(_selectedRule.FilePath)) return;
        Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{_selectedRule.FilePath}\"") { UseShellExecute = true });
    }

    private void SendPresentationCommand(string command)
    {
        if (_powerPoint is null) { _presentationStatus.Text = "PowerPoint 控制服务不可用。"; return; }
        var needsSelection = command is "ppt.openPresentation" or "ppt.startFromBeginning" or "ppt.startFromCurrent";
        if (needsSelection && string.IsNullOrWhiteSpace(_selectedPresentationId))
        {
            _presentationStatus.Text = "请先从列表中选择要操作的演示文稿。";
            return;
        }
        var result = _powerPoint.Execute(new RemoteCommand { Command = command, PresentationId = needsSelection ? _selectedPresentationId : null });
        _presentationStatus.Text = result.Message;
        RefreshPresentationPanel();
    }

    private void ConfirmForceQuit()
    {
        if (!RemoteConfirmDialog.Confirm(this, Font, "强制退出会终止电脑端全部 PowerPoint/WPS/演示软件，未保存内容将丢失。确定继续吗？")) return;
        if (_powerPoint is null) return;
        var result = _powerPoint.Queue(new RemoteCommand { Command = "ppt.forceQuitAll", Confirmed = true });
        _presentationStatus.Text = result.Message;
        RefreshPresentationPanel();
    }

    private static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "";
        try { return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar); }
        catch { return path.Trim(); }
    }

    private static bool SamePath(string? left, string? right) => string.Equals(NormalizePath(left), NormalizePath(right), StringComparison.OrdinalIgnoreCase);

    private void ToggleService()
    {
        _config.RemoteControl.Enabled = !_remoteControl.IsRunning;
        _config.RemoteControl.UseRandomPort = false;
        _config.RemoteControl.Port = ReadPort();
        _saveConfig(_config);
        if (_config.RemoteControl.Enabled) _remoteControl.Restart();
        else _remoteControl.Stop();
        RefreshState();
    }

    private void RestartService()
    {
        _config.RemoteControl.Enabled = true;
        _config.RemoteControl.UseRandomPort = false;
        _config.RemoteControl.Port = ReadPort();
        _saveConfig(_config);
        _remoteControl.Restart();
        RefreshState();
    }

    private void RefreshState()
    {
        if (_config.RemoteControl.Enabled && !_remoteControl.IsRunning) _remoteControl.Start();
        _port.Text = Math.Clamp(_remoteControl.CurrentPort > 0 ? _remoteControl.CurrentPort : _config.RemoteControl.Port <= 0 ? 1 : _config.RemoteControl.Port, 1, 65535).ToString(CultureInfo.InvariantCulture);
        _address.Items.Clear();
        foreach (var item in _networkAddressService.GetIPv4Addresses())
        {
            _address.Items.Add(item.Address);
        }

        if (_address.Items.Count == 0) _address.Items.Add("127.0.0.1");
        _address.SelectedIndex = 0;
        _toggle.Text = _remoteControl.IsRunning ? "关闭服务" : "启动服务";
        _toggle.BackColor = _remoteControl.IsRunning ? ModernTheme.SuccessSoft : ModernTheme.AccentSoft;
        _state.Text = _remoteControl.IsRunning ? "服务已启动，可以扫码连接" : "服务未启动";
        _state.ForeColor = _remoteControl.IsRunning ? ModernTheme.Success : Color.FromArgb(160, 64, 64);
        UpdateUrlAndQr();
    }

    private void UpdateUrlAndQr()
    {
        var url = CurrentUrl();
        _url.Text = RemoteUrlPrivacy.MaskToken(url);
        _toolTip.SetToolTip(_url, "访问链接已隐藏 token；复制链接和二维码仍使用完整有效地址。");
        _qr.Image?.Dispose();
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        using var code = new QRCode(data);
        _qr.Image = code.GetGraphic(8, Color.Black, Color.White, true);
    }

    private string CurrentUrl()
    {
        var address = _address.SelectedItem?.ToString();
        if (string.IsNullOrWhiteSpace(address)) address = "127.0.0.1";
        var port = _remoteControl.CurrentPort > 0 ? _remoteControl.CurrentPort : ReadPort();
        return $"http://{address}:{port}/?token={_config.RemoteControl.Token}";
    }

    private static string DisplayUrl(string url)
    {
        var marker = "token=";
        var index = url.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        return index < 0 ? url : url[..(index + marker.Length)] + "••••";
    }

    private string BuildFirewallCommand()
    {
        var port = _remoteControl.CurrentPort > 0 ? _remoteControl.CurrentPort : ReadPort();
        var exe = Path.Combine(AppContext.BaseDirectory, "FlyPPTTimer.exe");
        return $"netsh advfirewall firewall add rule name=\"FlyPPTTimer Remote {port}\" dir=in action=allow program=\"{exe}\" protocol=TCP localport={port}";
    }

    private int ReadPort()
    {
        if (!int.TryParse(_port.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var port)
            && !int.TryParse(_port.Text.Trim(), out port))
        {
            port = _config.RemoteControl.Port <= 0 ? 1 : _config.RemoteControl.Port;
        }

        port = Math.Clamp(port, 1, 65535);
        _port.Text = port.ToString(CultureInfo.InvariantCulture);
        return port;
    }

    private static void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { }
    }

    private static void BlockMouseWheel(object? sender, MouseEventArgs e)
    {
        if (e is HandledMouseEventArgs handled) handled.Handled = true;
    }
}

internal sealed class RemoteConfirmDialog : Form
{
    private RemoteConfirmDialog(Font font, string message)
    {
        Text = "演讲计时器";
        Font = font;
        AutoScaleMode = AutoScaleMode.Dpi;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        ShowInTaskbar = false;
        MinimizeBox = false;
        MaximizeBox = false;
        ClientSize = new Size(520, 170);
        BackColor = ModernTheme.Surface;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 1,
            RowCount = 2,
            BackColor = ModernTheme.Surface
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, ModernTheme.StandardControlHeight));
        root.Controls.Add(new Label
        {
            Text = message,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = ModernTheme.Text
        }, 0, 0);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Margin = Padding.Empty
        };
        var confirm = DialogButton("强制退出电脑端程序", DialogResult.OK, ModernTheme.DangerSoft, ModernTheme.Danger, 190);
        var cancel = DialogButton("取消", DialogResult.Cancel, ModernTheme.ControlFill, ModernTheme.Text, 104);
        actions.Controls.Add(confirm);
        actions.Controls.Add(cancel);
        root.Controls.Add(actions, 0, 1);
        Controls.Add(root);
        AcceptButton = confirm;
        CancelButton = cancel;
    }

    public static bool Confirm(IWin32Window owner, Font font, string message)
    {
        using var dialog = new RemoteConfirmDialog(font, message);
        return dialog.ShowDialog(owner) == DialogResult.OK;
    }

    private static Button DialogButton(string text, DialogResult result, Color fill, Color foreground, int width)
    {
        var button = new Button
        {
            Text = text,
            DialogResult = result,
            Width = width,
            Height = ModernTheme.StandardControlHeight,
            Margin = new Padding(8, 0, 0, 0),
            BackColor = fill,
            ForeColor = foreground,
            UseCompatibleTextRendering = false
        };
        ModernTheme.StyleRounded(button, ModernTheme.ButtonRadius);
        button.BackColor = fill;
        button.ForeColor = foreground;
        button.FlatAppearance.BorderColor = fill;
        return button;
    }
}

internal sealed class PresentationRuleRow : UserControl
{
    private readonly TableLayoutPanel _layout;
    private readonly Label _title;
    private readonly Label _path;
    private readonly Label _status;
    private readonly Button _enabled;
    private FileRule? _rule;
    private bool _updating;
    public event EventHandler? Selected;
    public event EventHandler<bool>? EnabledChangedByUser;
    public event MouseEventHandler? ContentMouseWheel;

    public PresentationRuleRow()
    {
        Height = MinimumHeightFor(Font);
        Margin = new Padding(0, 0, 0, 5);
        Cursor = Cursors.Hand;
        ModernTheme.StyleRounded(this, ModernTheme.CardRadius);

        _layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 2, Padding = new Padding(12, 4, 10, 4) };
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 94));
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _title = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
        _path = new Label { Dock = DockStyle.Fill, ForeColor = ModernTheme.MutedText, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
        _status = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
        _enabled = new Button { Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, UseCompatibleTextRendering = false, Margin = Padding.Empty };
        ModernTheme.StyleRounded(_status, ModernTheme.ControlRadius);
        ModernTheme.StyleRounded(_enabled, ModernTheme.ControlRadius);
        _enabled.Click += (_, _) =>
        {
            if (_updating || _rule is null) return;
            EnabledChangedByUser?.Invoke(this, !_rule.Enabled);
        };
        _layout.Controls.Add(_title, 0, 0);
        _layout.Controls.Add(_status, 1, 0);
        _layout.Controls.Add(_enabled, 2, 0);
        _layout.Controls.Add(_path, 0, 1);
        _layout.SetColumnSpan(_path, 3);
        Controls.Add(_layout);
        Click += (_, _) => Selected?.Invoke(this, EventArgs.Empty);
        MouseWheel += (_, e) => ContentMouseWheel?.Invoke(this, e);
        foreach (Control child in _layout.Controls)
        {
            child.Click += (_, _) => Selected?.Invoke(this, EventArgs.Empty);
            child.MouseWheel += (_, e) => ContentMouseWheel?.Invoke(this, e);
        }
        ApplyTextMetrics();
    }

    public static int MinimumHeightFor(Font font)
    {
        var titleHeight = TextRenderer.MeasureText("演示文稿", font, Size.Empty, TextFormatFlags.SingleLine).Height;
        var pathHeight = TextRenderer.MeasureText("C:\\演示文稿\\文件.pptx", font, Size.Empty, TextFormatFlags.SingleLine).Height;
        return ModernTheme.StandardControlHeight + pathHeight + 12;
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        if (_layout is not null) ApplyTextMetrics();
    }

    private void ApplyTextMetrics()
    {
        var titleHeight = ModernTheme.StandardControlHeight;
        var pathHeight = Math.Max(TextRenderer.MeasureText("C:\\演示文稿\\文件.pptx", _path.Font, Size.Empty, TextFormatFlags.SingleLine).Height + 4, 24);
        _layout.RowStyles[0].SizeType = SizeType.Absolute;
        _layout.RowStyles[0].Height = titleHeight;
        _layout.RowStyles[1].SizeType = SizeType.Absolute;
        _layout.RowStyles[1].Height = pathHeight;
        Height = _layout.Padding.Vertical + titleHeight + pathHeight;
    }

    public void Update(FileRule? rule, PresentationOption? option, bool selected, bool exists)
    {
        _updating = true;
        _rule = rule;
        var isShowing = option?.IsSlideShowRunning == true;
        var isOpen = option?.IsOpen == true;
        var statusText = !exists ? "文件不存在" : rule is null ? "已打开（无规则）" : !rule.Enabled ? "已禁用" : isShowing ? "正在放映" : option?.IsActive == true ? "当前活动" : isOpen ? "已打开" : "规则已启用";
        BackColor = selected ? ModernTheme.AccentSoft : ModernTheme.Card;
        _layout.BackColor = BackColor;
        _title.Text = rule is null
            ? option?.Name ?? "演示文稿"
            : $"{rule.FileName}   {rule.Duration}";
        _path.Text = rule?.FilePath ?? Path.Combine(option?.Directory ?? "", option?.Name ?? "");
        _status.Text = statusText;
        _status.BackColor = !exists ? ModernTheme.DangerSoft : isShowing || selected ? ModernTheme.AccentSoft : ModernTheme.ControlFill;
        _status.ForeColor = !exists ? ModernTheme.Danger : isShowing || selected ? ModernTheme.AccentStrong : ModernTheme.MutedText;
        _enabled.Visible = rule is not null;
        _enabled.Text = rule?.Enabled == true ? "已启用" : "已禁用";
        _enabled.BackColor = rule?.Enabled == true ? ModernTheme.SuccessSoft : ModernTheme.ControlFill;
        _enabled.ForeColor = rule?.Enabled == true ? ModernTheme.Success : ModernTheme.MutedText;
        _updating = false;
    }
}

internal sealed class FlatSelectBox : Control
{
    private int _selectedIndex = -1;
    private ContextMenuStrip? _menu;

    public FlatSelectBox()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        Height = ModernTheme.StandardControlHeight;
        BackColor = ModernTheme.ControlFill;
        ForeColor = ModernTheme.Text;
    }

    public List<string> Items { get; } = [];
    public event EventHandler? SelectedIndexChanged;

    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            var next = Items.Count == 0 ? -1 : Math.Clamp(value, 0, Items.Count - 1);
            if (_selectedIndex == next) return;
            _selectedIndex = next;
            Invalidate();
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public object? SelectedItem => _selectedIndex >= 0 && _selectedIndex < Items.Count ? Items[_selectedIndex] : null;

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        ShowMenu();
    }

    private void ShowMenu()
    {
        if (Items.Count == 0 || IsDisposed) return;
        var menu = EnsureMenu();
        if (menu.Visible) return;
        RebuildMenuItems(menu);
        var preferred = menu.GetPreferredSize(Size.Empty);
        var screen = PointToScreen(new Point(0, Height + 4));
        var area = Screen.FromPoint(screen).WorkingArea;
        screen.X = Math.Clamp(screen.X, area.Left, Math.Max(area.Left, area.Right - preferred.Width));
        screen.Y = Math.Clamp(screen.Y, area.Top, Math.Max(area.Top, area.Bottom - preferred.Height));
        menu.Show(screen);
    }

    private ContextMenuStrip EnsureMenu()
    {
        if (_menu is not null) return _menu;
        _menu = new ContextMenuStrip
        {
            Renderer = new ModernContextMenuRenderer(),
            BackColor = Color.White,
            ForeColor = ModernTheme.Text,
            Font = Font,
            Padding = new Padding(7),
            ShowImageMargin = false,
            ShowCheckMargin = false,
            AutoClose = true
        };
        return _menu;
    }

    private void RebuildMenuItems(ContextMenuStrip menu)
    {
        menu.Items.Clear();
        for (var i = 0; i < Items.Count; i++)
        {
            var index = i;
            var item = menu.Items.Add(Items[i], null, (_, _) => SelectedIndex = index);
            item.AutoSize = false;
            item.Size = new Size(Math.Max(Width, 220), ModernTheme.StandardControlHeight);
            item.Padding = new Padding(10, 0, 10, 0);
        }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (Items.Count > 0 && (keyData == Keys.Space || keyData == Keys.Enter || keyData == (Keys.Alt | Keys.Down)))
        {
            ShowMenu();
            return true;
        }
        if (Items.Count > 0 && keyData == Keys.Down)
        {
            SelectedIndex = Math.Min(Items.Count - 1, Math.Max(0, SelectedIndex + 1));
            return true;
        }
        if (Items.Count > 0 && keyData == Keys.Up)
        {
            SelectedIndex = Math.Max(0, SelectedIndex - 1);
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _menu?.Dispose();
        base.Dispose(disposing);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var fill = new SolidBrush(BackColor);
        e.Graphics.FillRectangle(fill, ClientRectangle);

        var textRect = new Rectangle(2, 0, Math.Max(0, Width - 34), Height);
        TextRenderer.DrawText(
            e.Graphics,
            SelectedItem?.ToString() ?? "",
            Font,
            textRect,
            ForeColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        var cx = Width - 18;
        var cy = Height / 2 + 1;
        using var brush = new SolidBrush(Color.FromArgb(80, 96, 104));
        e.Graphics.FillPolygon(brush, new Point[] { new(cx - 5, cy - 3), new(cx + 5, cy - 3), new(cx, cy + 3) });
    }
}
