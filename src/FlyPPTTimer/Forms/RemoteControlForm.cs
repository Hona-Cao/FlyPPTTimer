using FlyPPTTimer.Models;
using FlyPPTTimer.Services;
using QRCoder;
using System.Diagnostics;
using System.Globalization;

namespace FlyPPTTimer.Forms;

/// <summary>
/// Redesigned remote-control workspace.
/// The service, presentation, rule, token and command logic remains connected to the existing services;
/// this file replaces only the window composition and user interaction surface.
/// </summary>
public sealed class RemoteControlForm : Form
{
    private AppConfig _config;
    private readonly RemoteControlService _remoteControl;
    private readonly PowerPointControlService? _powerPoint;
    private readonly NetworkAddressService _networkAddressService;
    private readonly Action<AppConfig> _saveConfig;

    private readonly PictureBox _qr = new() { SizeMode = PictureBoxSizeMode.Zoom, Dock = DockStyle.Fill };
    private readonly RemoteFlatSelectBox _address = new();
    private readonly TextBox _url = new() { ReadOnly = true, BorderStyle = BorderStyle.None, TabStop = false };
    private readonly TextBox _port = new() { BorderStyle = BorderStyle.None, TextAlign = HorizontalAlignment.Center };
    private readonly Label _state = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft, AutoEllipsis = true };
    private readonly Label _stateDescription = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.TopLeft, AutoEllipsis = true };
    private readonly Label _connectionFeedback = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
    private readonly Button _toggle = new();
    private readonly ToolTip _toolTip = new()
    {
        AutoPopDelay = 30000,
        InitialDelay = 300,
        ReshowDelay = 100,
        ShowAlways = true
    };

    private readonly Label _pageTitle = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft };
    private readonly Label _pageSubtitle = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.TopLeft };
    private Button? _connectionNav;
    private Button? _presentationNav;
    private Panel? _contentHost;
    private Control? _connectionPage;
    private Control? _presentationPage;

    private readonly FlowLayoutPanel _ruleList = new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = true,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        BackColor = RemoteDashboardTheme.Card,
        Padding = new Padding(0, 4, 4, 4)
    };
    private readonly Label _ruleCount = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Label _presentationStatus = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
    private readonly Label _detailTitle = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
    private readonly TextBox _ruleDuration = new() { BorderStyle = BorderStyle.None, TextAlign = HorizontalAlignment.Center };
    private readonly Button _ruleEnabled = new() { Text = "已启用", UseCompatibleTextRendering = false };
    private readonly TextBox _rulePath = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        BorderStyle = BorderStyle.None,
        Multiline = true,
        WordWrap = true,
        ScrollBars = ScrollBars.None,
        TabStop = false
    };
    private readonly Dictionary<string, PresentationRuleCard> _ruleRows = new(StringComparer.OrdinalIgnoreCase);
    private readonly System.Windows.Forms.Timer _presentationRefreshTimer = new() { Interval = 1000 };
    private ContextMenuStrip? _moreActionsMenu;
    private ToolStripMenuItem? _copyPathMenuItem;
    private ToolStripMenuItem? _showPathMenuItem;

    private Button? _deleteRuleButton;
    private Button? _saveDurationButton;
    private Button? _moreActionsButton;
    private Button? _openPresentationButton;
    private Button? _startFromBeginningButton;
    private Button? _startFromCurrentButton;

    private FileRule? _selectedRule;
    private string? _selectedPresentationId;
    private string? _selectedPresentationPath;
    private bool _updatingRuleEditor;
    private bool _durationDirty;
    private bool _presentationTabActive;

    public RemoteControlForm(
        AppConfig config,
        RemoteControlService remoteControl,
        NetworkAddressService networkAddressService,
        Action<AppConfig> saveConfig)
    {
        _config = config;
        _remoteControl = remoteControl;
        _powerPoint = remoteControl.PresentationController;
        _networkAddressService = networkAddressService;
        _saveConfig = saveConfig;

        Text = "远程控制";
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = RemoteDashboardTheme.CreateFont(9.5F);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = true;
        ClientSize = new Size(1180, 780);
        MinimumSize = new Size(980, 680);
        BackColor = RemoteDashboardTheme.Window;

        _state.Font = RemoteDashboardTheme.CreateFont(12F, FontStyle.Bold);
        _state.ForeColor = RemoteDashboardTheme.Success;
        _stateDescription.Font = RemoteDashboardTheme.CreateFont(9F);
        _stateDescription.ForeColor = RemoteDashboardTheme.MutedText;
        _connectionFeedback.Font = RemoteDashboardTheme.CreateFont(9F);
        _connectionFeedback.ForeColor = RemoteDashboardTheme.Info;
        _pageTitle.Font = RemoteDashboardTheme.CreateFont(19F, FontStyle.Bold);
        _pageTitle.ForeColor = RemoteDashboardTheme.Text;
        _pageSubtitle.Font = RemoteDashboardTheme.CreateFont(9.5F);
        _pageSubtitle.ForeColor = RemoteDashboardTheme.MutedText;
        _presentationStatus.Font = RemoteDashboardTheme.CreateFont(9F);
        _presentationStatus.ForeColor = RemoteDashboardTheme.Info;
        _ruleCount.Font = RemoteDashboardTheme.CreateFont(9F);
        _ruleCount.ForeColor = RemoteDashboardTheme.MutedText;
        _detailTitle.Font = RemoteDashboardTheme.CreateFont(11F, FontStyle.Bold);
        _detailTitle.ForeColor = RemoteDashboardTheme.Text;

        Build();
        RefreshState();
        VisibleChanged += (_, _) => UpdatePresentationRefreshState();
    }

    public void ReloadConfig(AppConfig config)
    {
        _config = config;
        if (!IsDisposed)
        {
            RefreshState();
            if (_presentationTabActive) RefreshPresentationPanel();
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _presentationRefreshTimer.Dispose();
        _moreActionsMenu?.Dispose();
        _qr.Image?.Dispose();
        base.OnFormClosed(e);
    }

    private void Build()
    {
        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = RemoteDashboardTheme.Window,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, RemoteDashboardTheme.SidebarWidth));
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(shell);

        shell.Controls.Add(BuildSidebar(), 0, 0);
        shell.Controls.Add(BuildWorkspace(), 1, 0);
    }

    private Control BuildSidebar()
    {
        var sidebar = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = RemoteDashboardTheme.Sidebar,
            Padding = new Padding(14, 18, 14, 18),
            Margin = Padding.Empty
        };

        var border = new Panel
        {
            Dock = DockStyle.Right,
            Width = 1,
            BackColor = RemoteDashboardTheme.Border
        };
        sidebar.Controls.Add(border);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, RemoteDashboardTheme.NavigationHeight + 8));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, RemoteDashboardTheme.NavigationHeight + 8));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var brand = new Label
        {
            Text = "FlyPPTTimer\r\n远程控制",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = RemoteDashboardTheme.CreateFont(11F, FontStyle.Bold),
            ForeColor = RemoteDashboardTheme.Text,
            Padding = new Padding(12, 0, 0, 0)
        };
        layout.Controls.Add(brand, 0, 0);

        _connectionNav = CreateNavigationButton("远程连接", "连接手机或浏览器", (_, _) => ShowContentPage(false));
        _presentationNav = CreateNavigationButton("演示文稿", "规则、状态与放映", (_, _) => ShowContentPage(true));
        layout.Controls.Add(_connectionNav, 0, 1);
        layout.Controls.Add(_presentationNav, 0, 2);
        sidebar.Controls.Add(layout);
        layout.BringToFront();
        return sidebar;
    }

    private Button CreateNavigationButton(string title, string description, EventHandler click)
    {
        var button = new Button
        {
            Text = title + "\r\n" + description,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 4, 0, 4),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(16, 0, 8, 0),
            Font = RemoteDashboardTheme.CreateFont(9.5F),
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 0 },
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
            UseCompatibleTextRendering = false
        };
        button.Click += click;
        RemoteDashboardTheme.ApplyRoundedRegion(button, RemoteDashboardTheme.ControlRadius);
        button.SizeChanged += (_, _) => RemoteDashboardTheme.ApplyRoundedRegion(button, RemoteDashboardTheme.ControlRadius);
        return button;
    }

    private Control BuildWorkspace()
    {
        var workspace = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = RemoteDashboardTheme.Window,
            Padding = new Padding(RemoteDashboardTheme.PagePadding, 18, RemoteDashboardTheme.PagePadding, RemoteDashboardTheme.PagePadding),
            Margin = Padding.Empty
        };
        workspace.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
        workspace.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        workspace.Controls.Add(BuildPageHeader(), 0, 0);

        _contentHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = RemoteDashboardTheme.Window,
            Margin = Padding.Empty
        };
        _connectionPage = BuildConnectionPage();
        _presentationPage = BuildPresentationPage();
        _contentHost.Controls.Add(_connectionPage);
        _contentHost.Controls.Add(_presentationPage);
        workspace.Controls.Add(_contentHost, 0, 1);

        _presentationRefreshTimer.Tick += (_, _) => RefreshPresentationPanel();
        ShowContentPage(false);
        return workspace;
    }

    private Control BuildPageHeader()
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = RemoteDashboardTheme.Window,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        header.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
        header.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
        header.Controls.Add(_pageTitle, 0, 0);
        header.Controls.Add(_pageSubtitle, 0, 1);
        return header;
    }

    private void ShowContentPage(bool presentation)
    {
        if (_connectionPage is null || _presentationPage is null || _connectionNav is null || _presentationNav is null) return;

        _connectionPage.Visible = !presentation;
        _presentationPage.Visible = presentation;
        if (presentation) _presentationPage.BringToFront();
        else _connectionPage.BringToFront();

        SetNavigationState(_connectionNav, !presentation);
        SetNavigationState(_presentationNav, presentation);
        _pageTitle.Text = presentation ? "演示文稿" : "远程连接";
        _pageSubtitle.Text = presentation
            ? "管理演示文稿规则、状态与播放控制"
            : "通过手机或浏览器远程控制您的演示";

        _presentationTabActive = presentation;
        UpdatePresentationRefreshState();
    }

    private static void SetNavigationState(Button button, bool selected)
    {
        button.BackColor = selected ? RemoteDashboardTheme.AccentSoft : RemoteDashboardTheme.Sidebar;
        button.ForeColor = selected ? RemoteDashboardTheme.Accent : RemoteDashboardTheme.MutedText;
        button.FlatAppearance.MouseOverBackColor = selected ? RemoteDashboardTheme.AccentSoft : RemoteDashboardTheme.CardMuted;
        button.FlatAppearance.MouseDownBackColor = RemoteDashboardTheme.AccentSoft;
    }

    private void UpdatePresentationRefreshState()
    {
        _presentationRefreshTimer.Enabled = Visible && _presentationTabActive;
        if (_presentationRefreshTimer.Enabled) RefreshPresentationPanel();
    }

    private Control BuildConnectionPage()
    {
        var scroll = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = RemoteDashboardTheme.Window,
            Margin = Padding.Empty
        };
        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = false,
            Height = 640,
            MinimumSize = new Size(740, 640),
            ColumnCount = 1,
            RowCount = 2,
            BackColor = RemoteDashboardTheme.Window,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        body.Controls.Add(BuildServiceStatusCard(), 0, 0);
        body.Controls.Add(BuildConnectionColumns(), 0, 1);
        scroll.Controls.Add(body);

        void Reflow()
        {
            body.Width = Math.Max(body.MinimumSize.Width, scroll.ClientSize.Width - (scroll.VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0));
        }
        scroll.Resize += (_, _) => Reflow();
        scroll.HandleCreated += (_, _) => BeginInvoke((MethodInvoker)Reflow);
        return scroll;
    }

    private Control BuildServiceStatusCard()
    {
        var card = new RemoteCard
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 14),
            Padding = new Padding(18, 12, 18, 12)
        };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = Padding.Empty
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 46));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 128));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 132));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 132));

        var indicator = new Label
        {
            Text = "●",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = RemoteDashboardTheme.CreateFont(20F),
            ForeColor = RemoteDashboardTheme.Success
        };
        layout.Controls.Add(indicator, 0, 0);

        var statusText = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent,
            Padding = new Padding(4, 0, 8, 0)
        };
        statusText.RowStyles.Add(new RowStyle(SizeType.Percent, 54));
        statusText.RowStyles.Add(new RowStyle(SizeType.Percent, 46));
        statusText.Controls.Add(_state, 0, 0);
        statusText.Controls.Add(_stateDescription, 0, 1);
        layout.Controls.Add(statusText, 1, 0);

        var portBlock = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent,
            Padding = new Padding(8, 0, 8, 0)
        };
        portBlock.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        portBlock.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        portBlock.Controls.Add(new Label
        {
            Text = "端口",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.BottomCenter,
            ForeColor = RemoteDashboardTheme.MutedText
        }, 0, 0);
        var portHost = CreateInputHost(_port, new Padding(10, 9, 10, 7));
        portHost.Margin = new Padding(0, 2, 0, 0);
        portBlock.Controls.Add(portHost, 0, 1);
        layout.Controls.Add(portBlock, 2, 0);

        var restart = CreateActionButton("重启服务", (_, _) => RestartService(), ButtonKind.Secondary);
        restart.Margin = new Padding(8, 14, 4, 14);
        restart.Dock = DockStyle.Fill;
        layout.Controls.Add(restart, 3, 0);

        _toggle.Text = "关闭服务";
        RemoteDashboardTheme.StyleButton(_toggle, ButtonKind.DangerOutline);
        _toggle.Margin = new Padding(4, 14, 0, 14);
        _toggle.Dock = DockStyle.Fill;
        _toggle.Click += (_, _) => ToggleService();
        layout.Controls.Add(_toggle, 4, 0);

        card.Controls.Add(layout);
        return card;
    }

    private Control BuildConnectionColumns()
    {
        var columns = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = RemoteDashboardTheme.Window,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 41));
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 59));
        columns.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        columns.Controls.Add(BuildQrCard(), 0, 0);
        columns.Controls.Add(BuildBrowserAccessCard(), 1, 0);
        return columns;
    }

    private Control BuildQrCard()
    {
        var card = new RemoteCard
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 7, 0),
            Padding = new Padding(22, 18, 22, 18)
        };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Color.Transparent,
            Margin = Padding.Empty
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));
        layout.Controls.Add(CreateSectionTitle("手机扫码连接"), 0, 0);
        layout.Controls.Add(CreateSecondaryText("使用手机浏览器扫描二维码访问"), 0, 1);

        var qrOuter = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            ColumnCount = 1,
            RowCount = 1,
            Padding = new Padding(18)
        };
        var qrCard = new RemoteCard
        {
            Size = new Size(286, 286),
            Anchor = AnchorStyles.None,
            FillColor = Color.White,
            BorderColor = RemoteDashboardTheme.BorderStrong,
            Padding = new Padding(16)
        };
        qrCard.Controls.Add(_qr);
        qrOuter.Controls.Add(qrCard, 0, 0);
        layout.Controls.Add(qrOuter, 0, 2);

        var tip = new RemoteCard
        {
            Dock = DockStyle.Fill,
            FillColor = RemoteDashboardTheme.InfoSoft,
            BorderColor = Color.FromArgb(191, 219, 254),
            CornerRadius = RemoteDashboardTheme.ControlRadius,
            Padding = new Padding(14, 10, 14, 10)
        };
        tip.Controls.Add(new Label
        {
            Text = "手机无法访问时，请确认手机与电脑连接到同一 Wi-Fi，再检查防火墙设置。",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = RemoteDashboardTheme.Info,
            Font = RemoteDashboardTheme.CreateFont(9F)
        });
        layout.Controls.Add(tip, 0, 3);
        card.Controls.Add(layout);
        return card;
    }

    private Control BuildBrowserAccessCard()
    {
        var card = new RemoteCard
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(7, 0, 0, 0),
            Padding = new Padding(24, 18, 24, 18)
        };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 12,
            BackColor = Color.Transparent,
            Margin = Padding.Empty
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));

        layout.Controls.Add(CreateSectionTitle("通过浏览器访问"), 0, 0);
        layout.Controls.Add(CreateSecondaryText("选择电脑的网络地址，手机请使用相同网络访问。"), 0, 1);
        layout.Controls.Add(CreateFieldLabel("选择本机地址"), 0, 2);
        _address.SelectedIndexChanged += (_, _) => UpdateUrlAndQr();
        layout.Controls.Add(CreateInputHost(_address, new Padding(12, 8, 12, 7)), 0, 3);
        layout.Controls.Add(CreateSecondaryText("优先选择 Wi-Fi 或有线局域网地址。"), 0, 4);
        layout.Controls.Add(CreateFieldLabel("访问链接"), 0, 5);
        layout.Controls.Add(CreateInputHost(_url, new Padding(12, 9, 12, 7)), 0, 6);

        var copy = CreateActionButton("复制链接", (_, _) => CopyText(CurrentUrl(), "访问链接已复制，可粘贴到手机或分享给他人。"), ButtonKind.Primary);
        copy.Dock = DockStyle.Fill;
        copy.Margin = new Padding(0, 4, 0, 4);
        layout.Controls.Add(copy, 0, 7);

        var open = CreateActionButton("在本机浏览器打开", (_, _) => OpenCurrentUrl(), ButtonKind.Secondary);
        open.Dock = DockStyle.Fill;
        open.Margin = new Padding(0, 4, 0, 4);
        layout.Controls.Add(open, 0, 8);

        var firewall = CreateActionButton("复制放行命令", (_, _) => CopyText(BuildFirewallCommand(), "防火墙放行命令已复制。请以管理员身份运行终端后粘贴执行。"), ButtonKind.Secondary);
        firewall.Dock = DockStyle.Fill;
        firewall.Margin = new Padding(0, 4, 0, 4);
        layout.Controls.Add(firewall, 0, 9);

        var feedback = new RemoteCard
        {
            Dock = DockStyle.Fill,
            FillColor = RemoteDashboardTheme.InfoSoft,
            BorderColor = Color.FromArgb(191, 219, 254),
            CornerRadius = RemoteDashboardTheme.ControlRadius,
            Padding = new Padding(14, 8, 14, 8)
        };
        _connectionFeedback.Text = "请确保手机与电脑连接到同一 Wi-Fi，才能正常访问和控制。";
        feedback.Controls.Add(_connectionFeedback);
        layout.Controls.Add(feedback, 0, 11);
        card.Controls.Add(layout);
        return card;
    }

    private Control BuildPresentationPage()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = RemoteDashboardTheme.Window,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(BuildPresentationToolbar(), 0, 0);
        root.Controls.Add(BuildPresentationStatusBanner(), 0, 1);
        root.Controls.Add(BuildPresentationWorkspace(), 0, 2);
        return root;
    }

    private Control BuildPresentationToolbar()
    {
        var card = new RemoteCard
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 10),
            Padding = new Padding(12, 8, 12, 8)
        };
        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = false,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        var add = CreateActionButton("添加 PPT", (_, _) => AddPresentationRules(), ButtonKind.Primary, 112);
        _deleteRuleButton = CreateActionButton("删除规则", (_, _) => DeleteSelectedRule(), ButtonKind.DangerOutline, 112);
        var refresh = CreateActionButton("刷新状态", (_, _) => RefreshPresentationPanel(), ButtonKind.Secondary, 112);
        actions.Controls.Add(add);
        actions.Controls.Add(_deleteRuleButton);
        actions.Controls.Add(refresh);
        card.Controls.Add(actions);
        return card;
    }

    private Control BuildPresentationStatusBanner()
    {
        var card = new RemoteCard
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 10),
            FillColor = RemoteDashboardTheme.InfoSoft,
            BorderColor = Color.FromArgb(191, 219, 254),
            CornerRadius = RemoteDashboardTheme.ControlRadius,
            Padding = new Padding(14, 7, 14, 7)
        };
        _presentationStatus.Text = "选择左侧演示文稿以编辑规则或执行放映操作。";
        card.Controls.Add(_presentationStatus);
        return card;
    }

    private Control BuildPresentationWorkspace()
    {
        var split = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = RemoteDashboardTheme.Window,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 43));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 57));
        split.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        split.Controls.Add(BuildPresentationListCard(), 0, 0);
        split.Controls.Add(BuildPresentationDetails(), 1, 0);
        return split;
    }

    private Control BuildPresentationListCard()
    {
        var card = new RemoteCard
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 7, 0),
            Padding = new Padding(14, 12, 10, 10)
        };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Transparent,
            Margin = Padding.Empty
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.Controls.Add(CreateSectionTitle("演示文稿列表"), 0, 0);
        _ruleList.SizeChanged += (_, _) => UpdateRuleRowWidths();
        layout.Controls.Add(_ruleList, 0, 1);
        _ruleCount.Text = "共 0 项";
        layout.Controls.Add(_ruleCount, 0, 2);
        card.Controls.Add(layout);
        return card;
    }

    private Control BuildPresentationDetails()
    {
        var scroll = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = RemoteDashboardTheme.Window,
            Margin = new Padding(7, 0, 0, 0)
        };
        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = false,
            Height = 594,
            MinimumSize = new Size(460, 594),
            ColumnCount = 1,
            RowCount = 3,
            BackColor = RemoteDashboardTheme.Window,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 252));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 218));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 124));
        stack.Controls.Add(BuildRuleEditorCard(), 0, 0);
        stack.Controls.Add(BuildPresentationOperationCard(), 0, 1);
        stack.Controls.Add(BuildDangerCard(), 0, 2);
        scroll.Controls.Add(stack);

        void Reflow()
        {
            stack.Width = Math.Max(stack.MinimumSize.Width, scroll.ClientSize.Width - (scroll.VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0));
        }
        scroll.Resize += (_, _) => Reflow();
        scroll.HandleCreated += (_, _) => BeginInvoke((MethodInvoker)Reflow);
        return scroll;
    }

    private Control BuildRuleEditorCard()
    {
        var card = new RemoteCard
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 10),
            Padding = new Padding(18, 14, 18, 14)
        };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = Color.Transparent,
            Margin = Padding.Empty
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _detailTitle.Text = "规则编辑（尚未选择演示文稿）";
        layout.Controls.Add(_detailTitle, 0, 0);
        layout.Controls.Add(CreateFieldLabel("文件路径"), 0, 1);
        var pathHost = CreateInputHost(_rulePath, new Padding(12, 8, 12, 8), 64);
        pathHost.Margin = new Padding(0, 2, 0, 6);
        layout.Controls.Add(pathHost, 0, 2);
        layout.Controls.Add(CreateFieldLabel("自动播放时长与规则状态"), 0, 3);

        var editor = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 104));
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
        var durationHost = CreateInputHost(_ruleDuration, new Padding(10, 9, 10, 7));
        durationHost.Margin = new Padding(0, 4, 8, 4);
        editor.Controls.Add(durationHost, 0, 0);

        RemoteDashboardTheme.StyleButton(_ruleEnabled, ButtonKind.Secondary);
        _ruleEnabled.Margin = new Padding(0, 4, 8, 4);
        _ruleEnabled.Dock = DockStyle.Fill;
        _ruleEnabled.Click += (_, _) => ToggleSelectedRuleEnabled();
        editor.Controls.Add(_ruleEnabled, 1, 0);

        _moreActionsButton = CreateActionButton("更多操作", (_, _) => ShowMoreActions(_moreActionsButton), ButtonKind.Secondary, 108);
        _moreActionsButton.Margin = new Padding(0, 4, 8, 4);
        _moreActionsButton.Dock = DockStyle.Left;
        editor.Controls.Add(_moreActionsButton, 2, 0);

        _saveDurationButton = CreateActionButton("保存时长", (_, _) => SaveSelectedDuration(), ButtonKind.Primary, 108);
        _saveDurationButton.Margin = new Padding(0, 4, 0, 4);
        _saveDurationButton.Dock = DockStyle.Fill;
        editor.Controls.Add(_saveDurationButton, 3, 0);
        layout.Controls.Add(editor, 0, 4);

        _ruleDuration.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            SaveSelectedDuration();
        };
        _ruleDuration.TextChanged += (_, _) => { if (!_updatingRuleEditor) _durationDirty = true; };
        _ruleDuration.Leave += (_, _) => SaveSelectedDuration();
        card.Controls.Add(layout);
        return card;
    }

    private Control BuildPresentationOperationCard()
    {
        var card = new RemoteCard
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 10),
            Padding = new Padding(18, 14, 18, 14)
        };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent,
            Margin = Padding.Empty
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(CreateSectionTitle("演示操作"), 0, 0);

        var tiles = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            BackColor = Color.Transparent,
            Margin = Padding.Empty
        };
        tiles.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        tiles.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        tiles.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        tiles.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        _openPresentationButton = CreateTileButton("只读打开 / 切换", "在只读模式下打开目标文稿", (_, _) => SendPresentationCommand("ppt.openPresentation"));
        _startFromBeginningButton = CreateTileButton("从头放映", "从第一张幻灯片开始", (_, _) => SendPresentationCommand("ppt.startFromBeginning"));
        _startFromCurrentButton = CreateTileButton("从当前页放映", "从当前幻灯片开始", (_, _) => SendPresentationCommand("ppt.startFromCurrent"));
        var end = CreateTileButton("结束放映", "停止当前放映并返回", (_, _) => SendPresentationCommand("ppt.endShow"), danger: true);
        tiles.Controls.Add(_openPresentationButton, 0, 0);
        tiles.Controls.Add(_startFromBeginningButton, 1, 0);
        tiles.Controls.Add(_startFromCurrentButton, 0, 1);
        tiles.Controls.Add(end, 1, 1);
        layout.Controls.Add(tiles, 0, 1);
        card.Controls.Add(layout);
        return card;
    }

    private Control BuildDangerCard()
    {
        var card = new RemoteCard
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 12, 18, 12),
            FillColor = Color.White,
            BorderColor = Color.FromArgb(254, 202, 202)
        };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent,
            Margin = Padding.Empty
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var title = CreateSectionTitle("危险操作");
        title.ForeColor = RemoteDashboardTheme.Danger;
        layout.Controls.Add(title, 0, 0);

        var buttons = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = Padding.Empty
        };
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        var exit = CreateActionButton("退出 PowerPoint", (_, _) => SendPresentationCommand("ppt.exitApplication"), ButtonKind.DangerOutline);
        exit.Dock = DockStyle.Fill;
        exit.Margin = new Padding(0, 3, 5, 3);
        var force = CreateActionButton("强制退出 PowerPoint / WPS", (_, _) => ConfirmForceQuit(), ButtonKind.DangerOutline);
        force.Dock = DockStyle.Fill;
        force.Margin = new Padding(5, 3, 0, 3);
        buttons.Controls.Add(exit, 0, 0);
        buttons.Controls.Add(force, 1, 0);
        layout.Controls.Add(buttons, 0, 1);
        card.Controls.Add(layout);
        return card;
    }

    private static Label CreateSectionTitle(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        Font = RemoteDashboardTheme.CreateFont(11F, FontStyle.Bold),
        ForeColor = RemoteDashboardTheme.Text,
        AutoEllipsis = true
    };

    private static Label CreateFieldLabel(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.BottomLeft,
        Font = RemoteDashboardTheme.CreateFont(9F, FontStyle.Bold),
        ForeColor = RemoteDashboardTheme.Text,
        AutoEllipsis = true
    };

    private static Label CreateSecondaryText(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        Font = RemoteDashboardTheme.CreateFont(9F),
        ForeColor = RemoteDashboardTheme.MutedText,
        AutoEllipsis = true
    };

    private static Button CreateActionButton(string text, EventHandler click, ButtonKind kind, int minimumWidth = 0)
    {
        using var measureFont = RemoteDashboardTheme.CreateFont(9.5F);
        var measured = TextRenderer.MeasureText(text, measureFont, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix).Width + 34;
        var button = new Button
        {
            Text = text,
            Width = Math.Max(minimumWidth, measured),
            Margin = new Padding(0, 0, 8, 0),
            AutoSize = false
        };
        RemoteDashboardTheme.StyleButton(button, kind);
        button.Click += click;
        return button;
    }

    private static Button CreateTileButton(string title, string description, EventHandler click, bool danger = false)
    {
        var button = new Button
        {
            Text = title + "\r\n" + description,
            Dock = DockStyle.Fill,
            Margin = new Padding(4),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(16, 4, 10, 4),
            Font = RemoteDashboardTheme.CreateFont(9F),
            AutoSize = false
        };
        RemoteDashboardTheme.StyleButton(button, danger ? ButtonKind.DangerOutline : ButtonKind.Secondary);
        button.Click += click;
        return button;
    }

    private static RemoteCard CreateInputHost(Control control, Padding padding, int height = RemoteDashboardTheme.InputHeight)
    {
        var host = new RemoteCard
        {
            Dock = DockStyle.Fill,
            Height = height,
            MinimumSize = new Size(0, height),
            FillColor = RemoteDashboardTheme.CardMuted,
            BorderColor = RemoteDashboardTheme.Border,
            CornerRadius = RemoteDashboardTheme.ControlRadius,
            Padding = padding
        };
        control.Dock = DockStyle.Fill;
        control.Margin = Padding.Empty;
        control.BackColor = RemoteDashboardTheme.CardMuted;
        control.ForeColor = RemoteDashboardTheme.Text;
        control.Font = RemoteDashboardTheme.CreateFont(9.5F);
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
            _config.Rules.Add(new FileRule
            {
                FileName = Path.GetFileName(path),
                FilePath = normalized,
                Duration = _config.Timer.DefaultDuration,
                Enabled = true
            });
        }
        SaveRulesImmediately();
        RefreshPresentationPanel();
    }

    private void DeleteSelectedRule()
    {
        if (_selectedRule is null)
        {
            SetPresentationFeedback("请先选择一条规则。", FeedbackKind.Warning);
            return;
        }
        _config.Rules.Remove(_selectedRule);
        _selectedRule = null;
        _selectedPresentationId = null;
        _selectedPresentationPath = null;
        SaveRulesImmediately();
        SetPresentationFeedback("规则已删除。", FeedbackKind.Info);
        RefreshPresentationPanel();
    }

    private void RefreshPresentationPanel()
    {
        if (IsDisposed) return;
        var state = _powerPoint?.GetState() ?? new PresentationState { Error = "PowerPoint 控制服务不可用。" };
        var message = !string.IsNullOrWhiteSpace(state.OperationMessage) ? state.OperationMessage : state.Error;
        if (!string.IsNullOrWhiteSpace(message)) SetPresentationFeedback(message, string.IsNullOrWhiteSpace(state.Error) ? FeedbackKind.Info : FeedbackKind.Warning);
        RenderRuleRows(state);
        RefreshRuleEditor();
    }

    private void RenderRuleRows(PresentationState state)
    {
        var selectedPath = _selectedPresentationPath ?? _selectedRule?.FilePath ?? string.Empty;
        var items = PresentationRuleValidator.MergeRulesAndOpenPresentations(_config.Rules, state.Presentations);
        var scrollY = -_ruleList.AutoScrollPosition.Y;

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
                row = new PresentationRuleCard();
                row.Selected += (_, _) => SelectPresentation(row.CurrentRule, row.CurrentPresentation, row.CurrentPath);
                row.EnabledChangedByUser += (_, enabled) =>
                {
                    var currentRule = row.CurrentRule;
                    if (currentRule is null) return;
                    currentRule.Enabled = enabled;
                    SaveRulesImmediately();
                    SetPresentationFeedback(enabled ? "规则已启用。" : "规则已禁用。", FeedbackKind.Info);
                    RefreshPresentationPanel();
                };
                _ruleRows.Add(key, row);
                _ruleList.Controls.Add(row);
            }
            row.Update(item.Rule, item.Presentation, SamePath(item.Path, selectedPath), File.Exists(item.Path));
            _ruleList.Controls.SetChildIndex(row, index++);
        }
        _ruleList.ResumeLayout();
        _ruleCount.Text = $"共 {items.Count} 项";
        UpdateRuleRowWidths();
        if (scrollY > 0) _ruleList.AutoScrollPosition = new Point(0, scrollY);
    }

    private void UpdateRuleRowWidths()
    {
        var width = Math.Max(320, _ruleList.ClientSize.Width - _ruleList.Padding.Horizontal - ( _ruleList.VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0) - 2);
        foreach (Control control in _ruleList.Controls) control.Width = width;
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
        var hasSelection = !string.IsNullOrWhiteSpace(_selectedPresentationId);
        var hasRule = rule is not null;

        _updatingRuleEditor = true;
        var selectedName = rule?.FileName ?? (!string.IsNullOrWhiteSpace(_selectedPresentationPath) ? Path.GetFileName(_selectedPresentationPath) : null);
        _detailTitle.Text = selectedName is null ? "规则编辑（尚未选择演示文稿）" : $"规则编辑（{selectedName}）";
        _rulePath.Text = rule?.FilePath ?? _selectedPresentationPath ?? "请从左侧列表选择演示文稿";
        _toolTip.SetToolTip(_rulePath, rule?.FilePath ?? _selectedPresentationPath ?? string.Empty);
        _ruleDuration.Text = rule?.Duration ?? string.Empty;
        _durationDirty = false;
        SetRuleEnabledButton(rule?.Enabled == true);
        _ruleDuration.Enabled = hasRule;
        _ruleEnabled.Enabled = hasRule;
        _updatingRuleEditor = false;

        if (_deleteRuleButton is not null) _deleteRuleButton.Enabled = hasRule;
        if (_saveDurationButton is not null) _saveDurationButton.Enabled = hasRule;
        if (_moreActionsButton is not null) _moreActionsButton.Enabled = hasSelection;
        if (_openPresentationButton is not null) _openPresentationButton.Enabled = hasSelection;
        if (_startFromBeginningButton is not null) _startFromBeginningButton.Enabled = hasSelection;
        if (_startFromCurrentButton is not null) _startFromCurrentButton.Enabled = hasSelection;
    }

    private void SetRuleEnabledButton(bool enabled)
    {
        _ruleEnabled.Text = enabled ? "规则已启用" : "规则已禁用";
        _ruleEnabled.BackColor = enabled ? RemoteDashboardTheme.SuccessSoft : RemoteDashboardTheme.CardMuted;
        _ruleEnabled.ForeColor = enabled ? RemoteDashboardTheme.Success : RemoteDashboardTheme.MutedText;
        _ruleEnabled.FlatAppearance.BorderColor = enabled ? Color.FromArgb(167, 231, 196) : RemoteDashboardTheme.BorderStrong;
    }

    private void ToggleSelectedRuleEnabled()
    {
        if (_updatingRuleEditor || _selectedRule is null) return;
        _selectedRule.Enabled = !_selectedRule.Enabled;
        SetRuleEnabledButton(_selectedRule.Enabled);
        SaveRulesImmediately();
        SetPresentationFeedback(_selectedRule.Enabled ? "规则已启用。" : "规则已禁用。", FeedbackKind.Info);
        RefreshPresentationPanel();
    }

    private void ShowMoreActions(Button? button)
    {
        if (button is null || IsDisposed) return;
        _moreActionsMenu ??= CreateMoreActionsMenu();
        if (_moreActionsMenu.Visible) return;
        if (_copyPathMenuItem is not null) _copyPathMenuItem.Enabled = !string.IsNullOrWhiteSpace(_selectedPresentationPath);
        if (_showPathMenuItem is not null) _showPathMenuItem.Enabled = !string.IsNullOrWhiteSpace(_selectedPresentationPath) && File.Exists(_selectedPresentationPath);
        _moreActionsMenu.Show(button, new Point(0, button.Height + 4));
    }

    private ContextMenuStrip CreateMoreActionsMenu()
    {
        var menu = new ContextMenuStrip
        {
            Renderer = new RemoteMenuRenderer(),
            BackColor = Color.White,
            ForeColor = RemoteDashboardTheme.Text,
            Font = RemoteDashboardTheme.CreateFont(9.5F),
            ShowImageMargin = false,
            ShowCheckMargin = false,
            AutoClose = true,
            Padding = new Padding(6)
        };
        _copyPathMenuItem = new ToolStripMenuItem("复制文件路径", null, (_, _) => CopySelectedPath());
        _showPathMenuItem = new ToolStripMenuItem("在资源管理器中显示", null, (_, _) => ShowSelectedPath());
        menu.Items.Add(_copyPathMenuItem);
        menu.Items.Add(_showPathMenuItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("关闭当前受控文稿", null, (_, _) => SendPresentationCommand("ppt.closeCurrentPresentation"));
        foreach (ToolStripItem item in menu.Items.OfType<ToolStripMenuItem>())
        {
            item.AutoSize = false;
            item.Height = 40;
            item.Padding = new Padding(10, 0, 10, 0);
        }
        return menu;
    }

    private void SaveSelectedDuration()
    {
        if (_updatingRuleEditor || _selectedRule is null || !_durationDirty) return;
        if (!PresentationRuleValidator.TryNormalizeDuration(_ruleDuration.Text, out var duration, out var error))
        {
            SetPresentationFeedback(error, FeedbackKind.Warning);
            return;
        }
        if (string.Equals(_selectedRule.Duration, duration, StringComparison.Ordinal))
        {
            _durationDirty = false;
            return;
        }
        _selectedRule.Duration = duration;
        _ruleDuration.Text = duration;
        _durationDirty = false;
        SaveRulesImmediately();
        SetPresentationFeedback("计时时长已保存并同步手机端。", FeedbackKind.Success);
        RefreshPresentationPanel();
    }

    private void SaveRulesImmediately()
    {
        _saveConfig(_config);
        _remoteControl.NotifyStateChanged();
    }

    private void CopySelectedPath()
    {
        var path = _selectedRule?.FilePath ?? _selectedPresentationPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            SetPresentationFeedback("请先选择演示文稿。", FeedbackKind.Warning);
            return;
        }
        CopyText(path, "文件路径已复制。", presentationFeedback: true);
    }

    private void ShowSelectedPath()
    {
        var path = _selectedRule?.FilePath ?? _selectedPresentationPath;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            SetPresentationFeedback("文件不存在，无法在资源管理器中显示。", FeedbackKind.Warning);
            return;
        }
        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true });
        }
        catch
        {
            SetPresentationFeedback("无法打开资源管理器，请检查文件路径。", FeedbackKind.Warning);
        }
    }

    private void SendPresentationCommand(string command)
    {
        if (_powerPoint is null)
        {
            SetPresentationFeedback("PowerPoint 控制服务不可用。", FeedbackKind.Warning);
            return;
        }
        var needsSelection = command is "ppt.openPresentation" or "ppt.startFromBeginning" or "ppt.startFromCurrent";
        if (needsSelection && string.IsNullOrWhiteSpace(_selectedPresentationId))
        {
            SetPresentationFeedback("请先从列表中选择要操作的演示文稿。", FeedbackKind.Warning);
            return;
        }
        var result = _powerPoint.Execute(new RemoteCommand
        {
            Command = command,
            PresentationId = needsSelection ? _selectedPresentationId : null
        });
        SetPresentationFeedback(result.Message, result.Success ? FeedbackKind.Success : FeedbackKind.Warning);
        RefreshPresentationPanel();
    }

    private void ConfirmForceQuit()
    {
        if (!RemoteConfirmDialog.Confirm(this, Font, "强制退出会终止电脑端全部 PowerPoint/WPS/演示软件，未保存内容将丢失。")) return;
        if (_powerPoint is null) return;
        var result = _powerPoint.Queue(new RemoteCommand { Command = "ppt.forceQuitAll", Confirmed = true });
        SetPresentationFeedback(result.Message, result.Success ? FeedbackKind.Info : FeedbackKind.Warning);
        RefreshPresentationPanel();
    }

    private void SetPresentationFeedback(string? message, FeedbackKind kind)
    {
        _presentationStatus.Text = string.IsNullOrWhiteSpace(message)
            ? "选择左侧演示文稿以编辑规则或执行放映操作。"
            : message;
        _presentationStatus.ForeColor = kind switch
        {
            FeedbackKind.Success => RemoteDashboardTheme.Success,
            FeedbackKind.Warning => RemoteDashboardTheme.Warning,
            _ => RemoteDashboardTheme.Info
        };
    }

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
        _port.Text = Math.Clamp(
            _remoteControl.CurrentPort > 0
                ? _remoteControl.CurrentPort
                : _config.RemoteControl.Port <= 0 ? 1 : _config.RemoteControl.Port,
            1,
            65535).ToString(CultureInfo.InvariantCulture);

        var previousAddress = _address.SelectedItem?.ToString();
        _address.Items.Clear();
        foreach (var item in _networkAddressService.GetIPv4Addresses()) _address.Items.Add(item.Address);
        if (_address.Items.Count == 0) _address.Items.Add("127.0.0.1");
        var previousIndex = previousAddress is null ? -1 : _address.Items.FindIndex(value => string.Equals(value, previousAddress, StringComparison.OrdinalIgnoreCase));
        _address.SelectedIndex = previousIndex >= 0 ? previousIndex : 0;

        var running = _remoteControl.IsRunning;
        _toggle.Text = running ? "关闭服务" : "启动服务";
        _toggle.BackColor = running ? Color.White : RemoteDashboardTheme.Accent;
        _toggle.ForeColor = running ? RemoteDashboardTheme.Danger : Color.White;
        _toggle.FlatAppearance.BorderSize = running ? 1 : 0;
        _toggle.FlatAppearance.BorderColor = running ? Color.FromArgb(248, 113, 113) : RemoteDashboardTheme.Accent;
        _toggle.FlatAppearance.MouseOverBackColor = running ? RemoteDashboardTheme.DangerSoft : RemoteDashboardTheme.AccentHover;
        _toggle.FlatAppearance.MouseDownBackColor = running ? Color.FromArgb(254, 226, 226) : RemoteDashboardTheme.AccentPressed;
        _state.Text = running ? "服务已启动，可扫码连接" : "服务未启动";
        _stateDescription.Text = running
            ? "手机与电脑处于同一 Wi-Fi 时即可连接"
            : "启动服务后，手机才能访问远程控制页面";
        _state.ForeColor = running ? RemoteDashboardTheme.Success : RemoteDashboardTheme.Danger;
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

    private void CopyText(string text, string successMessage, bool presentationFeedback = false)
    {
        try
        {
            Clipboard.SetText(text);
            if (presentationFeedback) SetPresentationFeedback(successMessage, FeedbackKind.Success);
            else
            {
                _connectionFeedback.Text = successMessage;
                _connectionFeedback.ForeColor = RemoteDashboardTheme.Success;
            }
        }
        catch
        {
            if (presentationFeedback) SetPresentationFeedback("复制失败，请稍后重试。", FeedbackKind.Warning);
            else
            {
                _connectionFeedback.Text = "复制失败，请稍后重试。";
                _connectionFeedback.ForeColor = RemoteDashboardTheme.Warning;
            }
        }
    }

    private void OpenCurrentUrl()
    {
        try
        {
            Process.Start(new ProcessStartInfo(CurrentUrl()) { UseShellExecute = true });
            _connectionFeedback.Text = "已在本机默认浏览器中打开远程控制页面。";
            _connectionFeedback.ForeColor = RemoteDashboardTheme.Success;
        }
        catch
        {
            _connectionFeedback.Text = "无法打开浏览器，请复制链接后手动访问。";
            _connectionFeedback.ForeColor = RemoteDashboardTheme.Warning;
        }
    }

    private static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return string.Empty;
        try { return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar); }
        catch { return path.Trim(); }
    }

    private static bool SamePath(string? left, string? right) =>
        string.Equals(NormalizePath(left), NormalizePath(right), StringComparison.OrdinalIgnoreCase);
}

internal enum FeedbackKind
{
    Info,
    Success,
    Warning
}

internal sealed class RemoteConfirmDialog : Form
{
    private RemoteConfirmDialog(Font font, string message)
    {
        Text = "确认危险操作";
        Font = font;
        AutoScaleMode = AutoScaleMode.Dpi;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        ShowInTaskbar = false;
        MinimizeBox = false;
        MaximizeBox = false;
        ClientSize = new Size(560, 220);
        BackColor = RemoteDashboardTheme.Window;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24),
            ColumnCount = 1,
            RowCount = 3,
            BackColor = RemoteDashboardTheme.Window
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        root.Controls.Add(new Label
        {
            Text = "强制退出 PowerPoint / WPS",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = RemoteDashboardTheme.CreateFont(13F, FontStyle.Bold),
            ForeColor = RemoteDashboardTheme.Danger
        }, 0, 0);
        root.Controls.Add(new Label
        {
            Text = message + "\r\n此操作无法撤销，请确认所有重要文件已经保存。",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = RemoteDashboardTheme.CreateFont(9.5F),
            ForeColor = RemoteDashboardTheme.Text
        }, 0, 1);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Margin = Padding.Empty
        };
        var confirm = new Button { Text = "仍然强制退出", DialogResult = DialogResult.OK, Width = 150, Margin = new Padding(8, 4, 0, 4) };
        var cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Width = 104, Margin = new Padding(8, 4, 0, 4) };
        RemoteDashboardTheme.StyleButton(confirm, ButtonKind.Danger);
        RemoteDashboardTheme.StyleButton(cancel, ButtonKind.Secondary);
        actions.Controls.Add(confirm);
        actions.Controls.Add(cancel);
        root.Controls.Add(actions, 0, 2);
        Controls.Add(root);
        CancelButton = cancel;
        ActiveControl = cancel;
    }

    public static bool Confirm(IWin32Window owner, Font font, string message)
    {
        using var dialog = new RemoteConfirmDialog(font, message);
        return dialog.ShowDialog(owner) == DialogResult.OK;
    }
}

internal sealed class PresentationRuleCard : RemoteCard
{
    private readonly TableLayoutPanel _layout;
    private readonly Label _fileBadge;
    private readonly Label _title;
    private readonly Label _duration;
    private readonly Label _path;
    private readonly Label _status;
    private readonly Button _enabled;
    private bool _updating;
    private bool _selected;

    public FileRule? CurrentRule { get; private set; }
    public PresentationOption? CurrentPresentation { get; private set; }
    public string CurrentPath { get; private set; } = string.Empty;

    public event EventHandler? Selected;
    public event EventHandler<bool>? EnabledChangedByUser;

    public PresentationRuleCard()
    {
        Height = RemoteDashboardTheme.PresentationRowHeight;
        MinimumSize = new Size(320, RemoteDashboardTheme.PresentationRowHeight);
        Margin = new Padding(0, 0, 0, 8);
        Cursor = Cursors.Hand;
        FillColor = RemoteDashboardTheme.Card;
        BorderColor = RemoteDashboardTheme.Border;
        CornerRadius = RemoteDashboardTheme.ControlRadius;
        Padding = new Padding(12, 10, 10, 10);

        _layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 3,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _fileBadge = new Label
        {
            Text = "PPT",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = RemoteDashboardTheme.CreateFont(8.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(234, 88, 12),
            BackColor = Color.FromArgb(255, 247, 237),
            Margin = new Padding(0, 3, 8, 3)
        };
        RemoteDashboardTheme.ApplyRoundedRegion(_fileBadge, 6);
        _fileBadge.SizeChanged += (_, _) => RemoteDashboardTheme.ApplyRoundedRegion(_fileBadge, 6);

        _title = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = RemoteDashboardTheme.CreateFont(9.5F, FontStyle.Bold),
            ForeColor = RemoteDashboardTheme.Text,
            AutoEllipsis = true
        };
        _duration = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = RemoteDashboardTheme.CreateFont(8.5F),
            ForeColor = RemoteDashboardTheme.MutedText,
            AutoEllipsis = true
        };
        _path = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.TopLeft,
            Font = RemoteDashboardTheme.CreateFont(8F),
            ForeColor = RemoteDashboardTheme.SubtleText,
            AutoEllipsis = true
        };
        _status = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = RemoteDashboardTheme.CreateFont(8.5F, FontStyle.Bold),
            Margin = new Padding(4, 3, 0, 3)
        };
        _enabled = new Button
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(4, 3, 0, 3),
            UseCompatibleTextRendering = false,
            AutoSize = false
        };
        RemoteDashboardTheme.StyleButton(_enabled, ButtonKind.Secondary);
        _enabled.Font = RemoteDashboardTheme.CreateFont(8.5F);
        _enabled.Padding = Padding.Empty;
        _enabled.Click += (_, _) =>
        {
            if (_updating || CurrentRule is null) return;
            EnabledChangedByUser?.Invoke(this, !CurrentRule.Enabled);
        };

        _layout.Controls.Add(_fileBadge, 0, 0);
        _layout.SetRowSpan(_fileBadge, 2);
        _layout.Controls.Add(_title, 1, 0);
        _layout.Controls.Add(_status, 2, 0);
        _layout.Controls.Add(_duration, 1, 1);
        _layout.Controls.Add(_enabled, 2, 1);
        _layout.Controls.Add(_path, 0, 2);
        _layout.SetColumnSpan(_path, 3);
        Controls.Add(_layout);

        WireSelection(this);
        MouseEnter += (_, _) => ApplyVisualState(true);
        MouseLeave += (_, _) => ApplyVisualState(false);
    }

    public void Update(FileRule? rule, PresentationOption? option, bool selected, bool exists)
    {
        _updating = true;
        CurrentRule = rule;
        CurrentPresentation = option;
        CurrentPath = rule?.FilePath ?? Path.Combine(option?.Directory ?? string.Empty, option?.Name ?? string.Empty);
        _selected = selected;

        var isShowing = option?.IsSlideShowRunning == true;
        var isOpen = option?.IsOpen == true;
        var statusText = !exists
            ? "文件不存在"
            : rule is null
                ? "已打开 · 无规则"
                : !rule.Enabled
                    ? "规则已禁用"
                    : isShowing
                        ? "正在放映"
                        : option?.IsActive == true
                            ? "当前活动"
                            : isOpen ? "已打开" : "规则已启用";

        _title.Text = rule?.FileName ?? option?.Name ?? "演示文稿";
        _duration.Text = rule is null ? "未添加计时规则" : $"自动时长  {rule.Duration}";
        _path.Text = CurrentPath;
        _status.Text = statusText;
        _status.BackColor = !exists
            ? RemoteDashboardTheme.DangerSoft
            : isShowing || option?.IsActive == true
                ? RemoteDashboardTheme.AccentSoft
                : RemoteDashboardTheme.CardMuted;
        _status.ForeColor = !exists
            ? RemoteDashboardTheme.Danger
            : isShowing || option?.IsActive == true
                ? RemoteDashboardTheme.Accent
                : RemoteDashboardTheme.MutedText;
        RemoteDashboardTheme.ApplyRoundedRegion(_status, 6);

        _enabled.Visible = rule is not null;
        _enabled.Text = rule?.Enabled == true ? "已启用" : "已禁用";
        _enabled.BackColor = rule?.Enabled == true ? RemoteDashboardTheme.SuccessSoft : RemoteDashboardTheme.CardMuted;
        _enabled.ForeColor = rule?.Enabled == true ? RemoteDashboardTheme.Success : RemoteDashboardTheme.MutedText;
        _enabled.FlatAppearance.BorderColor = rule?.Enabled == true ? Color.FromArgb(167, 231, 196) : RemoteDashboardTheme.BorderStrong;
        ApplyVisualState(false);
        _updating = false;
    }

    private void WireSelection(Control control)
    {
        if (!ReferenceEquals(control, _enabled)) control.Click += (_, _) => Selected?.Invoke(this, EventArgs.Empty);
        foreach (Control child in control.Controls) WireSelection(child);
    }

    private void ApplyVisualState(bool hovered)
    {
        FillColor = _selected
            ? RemoteDashboardTheme.AccentSoft
            : hovered ? RemoteDashboardTheme.CardMuted : RemoteDashboardTheme.Card;
        BorderColor = _selected ? RemoteDashboardTheme.Accent : RemoteDashboardTheme.Border;
        Invalidate();
    }
}

internal sealed class RemoteFlatSelectBox : Control
{
    private int _selectedIndex = -1;
    private ContextMenuStrip? _menu;

    public RemoteFlatSelectBox()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.Selectable, true);
        Height = RemoteDashboardTheme.InputHeight;
        BackColor = RemoteDashboardTheme.CardMuted;
        ForeColor = RemoteDashboardTheme.Text;
        Font = RemoteDashboardTheme.CreateFont(9.5F);
        TabStop = true;
        Cursor = Cursors.Hand;
        AccessibleRole = AccessibleRole.ComboBox;
        AccessibleName = "选择本机地址";
    }

    public List<string> Items { get; } = new();
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
        Focus();
        ShowMenu();
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        Invalidate();
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        Invalidate();
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
            Renderer = new RemoteMenuRenderer(),
            BackColor = Color.White,
            ForeColor = RemoteDashboardTheme.Text,
            Font = Font,
            Padding = new Padding(6),
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
            item.Size = new Size(Math.Max(Width, 240), 40);
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
        e.Graphics.Clear(BackColor);
        var textRect = new Rectangle(2, 0, Math.Max(0, Width - 38), Height);
        TextRenderer.DrawText(
            e.Graphics,
            SelectedItem?.ToString() ?? string.Empty,
            Font,
            textRect,
            ForeColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

        var centerX = Width - 18;
        var centerY = Height / 2 + 1;
        using var arrow = new SolidBrush(RemoteDashboardTheme.MutedText);
        e.Graphics.FillPolygon(arrow, new[]
        {
            new Point(centerX - 5, centerY - 3),
            new Point(centerX + 5, centerY - 3),
            new Point(centerX, centerY + 3)
        });
        if (Focused)
        {
            using var focusPen = new Pen(RemoteDashboardTheme.Accent, 1.5F);
            using var path = RemoteDashboardTheme.RoundedRectangle(new Rectangle(1, 1, Width - 3, Height - 3), 6);
            e.Graphics.DrawPath(focusPen, path);
        }
    }
}

internal sealed class RemoteMenuRenderer : ToolStripProfessionalRenderer
{
    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var path = RemoteDashboardTheme.RoundedRectangle(new Rectangle(Point.Empty, e.ToolStrip.Size - new Size(1, 1)), 8);
        using var brush = new SolidBrush(Color.White);
        e.Graphics.FillPath(brush, path);
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        if (!e.Item.Selected) return;
        var rectangle = new Rectangle(4, 2, Math.Max(1, e.Item.Width - 8), Math.Max(1, e.Item.Height - 4));
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var path = RemoteDashboardTheme.RoundedRectangle(rectangle, 6);
        using var brush = new SolidBrush(RemoteDashboardTheme.AccentSoft);
        e.Graphics.FillPath(brush, path);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        using var pen = new Pen(RemoteDashboardTheme.Border);
        var y = e.Item.ContentRectangle.Top + e.Item.ContentRectangle.Height / 2;
        e.Graphics.DrawLine(pen, 12, y, e.Item.Width - 12, y);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e) { }
}
