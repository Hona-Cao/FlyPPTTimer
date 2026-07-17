using FlyPPTTimer.Models;
using FlyPPTTimer.Services;
using QRCoder;
using System.Diagnostics;
using System.Globalization;

namespace FlyPPTTimer.Forms;

/// <summary>
/// v0.16 text-first remote-control workspace.
/// This file replaces only the PC window composition. Existing services and commands remain unchanged.
/// </summary>
public sealed class RemoteControlForm : Form
{
    private AppConfig _config;
    private readonly RemoteControlService _remoteControl;
    private readonly PowerPointControlService? _powerPoint;
    private readonly NetworkAddressService _networkAddressService;
    private readonly Action<AppConfig> _saveConfig;

    private readonly PictureBox _qr = new()
    {
        SizeMode = PictureBoxSizeMode.Zoom,
        Dock = DockStyle.Fill,
        BackColor = Color.White
    };
    private readonly RemoteAddressSelector _address = new() { Dock = DockStyle.Fill };
    private readonly TextBox _url = new()
    {
        ReadOnly = true,
        BorderStyle = BorderStyle.None,
        TabStop = false
    };
    private readonly TextBox _port = new()
    {
        BorderStyle = BorderStyle.None,
        TextAlign = HorizontalAlignment.Center
    };

    private readonly Label _state = NewLabel(ContentAlignment.MiddleLeft);
    private readonly Label _connectionFeedback = NewLabel(ContentAlignment.MiddleLeft);
    private readonly Label _pageTitle = NewLabel(ContentAlignment.MiddleLeft);
    private readonly Label _pageSubtitle = NewLabel(ContentAlignment.MiddleLeft);
    private readonly Label _presentationStatus = NewLabel(ContentAlignment.MiddleLeft);
    private readonly Label _ruleCount = NewLabel(ContentAlignment.MiddleLeft);
    private readonly Label _detailTitle = NewLabel(ContentAlignment.MiddleLeft);
    private readonly Label _emptyList = NewLabel(ContentAlignment.MiddleCenter);
    private readonly TextBox _ruleDuration = new()
    {
        BorderStyle = BorderStyle.None,
        TextAlign = HorizontalAlignment.Center
    };
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

    private readonly FlowLayoutPanel _ruleList = new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = true,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        BackColor = RemoteDashboardTheme.Card,
        Padding = new Padding(0, 4, 4, 4),
        Margin = Padding.Empty
    };
    private readonly Dictionary<string, RemotePresentationRow> _ruleRows =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly System.Windows.Forms.Timer _presentationRefreshTimer =
        new() { Interval = 1000 };
    private readonly ToolTip _toolTip = new()
    {
        AutoPopDelay = 30000,
        InitialDelay = 300,
        ReshowDelay = 100,
        ShowAlways = true
    };

    private RemoteTextButton? _connectionNav;
    private RemoteTextButton? _presentationNav;
    private RemoteTextButton? _serviceToggle;
    private RemoteTextButton? _deleteRuleButton;
    private RemoteTextButton? _ruleEnabledButton;
    private RemoteTextButton? _saveDurationButton;
    private RemoteTextButton? _moreActionsButton;
    private RemoteTextButton? _openPresentationButton;
    private RemoteTextButton? _startFromBeginningButton;
    private RemoteTextButton? _startFromCurrentButton;

    private Panel? _contentHost;
    private Control? _connectionPage;
    private Control? _presentationPage;
    private Panel? _ruleListHost;

    private ContextMenuStrip? _moreActionsMenu;
    private ToolStripMenuItem? _copyPathMenuItem;
    private ToolStripMenuItem? _showPathMenuItem;

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
        ClientSize = new Size(1180, 760);
        MinimumSize = new Size(1040, 700);
        BackColor = RemoteDashboardTheme.Window;

        ConfigureText();
        Build();
        RefreshState();
        VisibleChanged += (_, _) => UpdatePresentationRefreshState();
    }

    public void ReloadConfig(AppConfig config)
    {
        _config = config;
        if (IsDisposed) return;
        RefreshState();
        if (_presentationTabActive) RefreshPresentationPanel();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _presentationRefreshTimer.Dispose();
        _moreActionsMenu?.Dispose();
        _qr.Image?.Dispose();
        base.OnFormClosed(e);
    }

    private static Label NewLabel(ContentAlignment alignment) => new()
    {
        Dock = DockStyle.Fill,
        TextAlign = alignment,
        AutoEllipsis = false,
        Margin = Padding.Empty,
        UseCompatibleTextRendering = false
    };

    private void ConfigureText()
    {
        _state.Font = RemoteDashboardTheme.CreateFont(11.5F, FontStyle.Bold);
        _state.ForeColor = RemoteDashboardTheme.Success;

        _connectionFeedback.Font = RemoteDashboardTheme.CreateFont(9F);
        _connectionFeedback.ForeColor = RemoteDashboardTheme.Info;

        _pageTitle.Font = RemoteDashboardTheme.CreateFont(19F, FontStyle.Bold);
        _pageTitle.ForeColor = RemoteDashboardTheme.Text;

        _pageSubtitle.Font = RemoteDashboardTheme.CreateFont(9.5F);
        _pageSubtitle.ForeColor = RemoteDashboardTheme.MutedText;

        _presentationStatus.Font = RemoteDashboardTheme.CreateFont(9F);
        _presentationStatus.ForeColor = RemoteDashboardTheme.Info;

        _ruleCount.Font = RemoteDashboardTheme.CreateFont(8.75F);
        _ruleCount.ForeColor = RemoteDashboardTheme.MutedText;

        _detailTitle.Font = RemoteDashboardTheme.CreateFont(11F, FontStyle.Bold);
        _detailTitle.ForeColor = RemoteDashboardTheme.Text;

        _emptyList.Text = "暂无演示文稿";
        _emptyList.Font = RemoteDashboardTheme.CreateFont(10F);
        _emptyList.ForeColor = RemoteDashboardTheme.SubtleText;
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
            Padding = new Padding(0, 18, 1, 18),
            Margin = Padding.Empty
        };
        sidebar.Controls.Add(new Panel
        {
            Dock = DockStyle.Right,
            Width = 1,
            BackColor = RemoteDashboardTheme.Border
        });

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, RemoteDashboardTheme.NavigationHeight + 8));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, RemoteDashboardTheme.NavigationHeight + 8));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        layout.Controls.Add(new Label
        {
            Text = "FlyPPTTimer",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = RemoteDashboardTheme.CreateFont(10.5F, FontStyle.Bold),
            ForeColor = RemoteDashboardTheme.Text,
            Padding = new Padding(14, 3, 8, 3),
            Margin = Padding.Empty,
            AutoSize = false,
            MinimumSize = new Size(0, 40),
            AutoEllipsis = false,
            UseCompatibleTextRendering = false
        }, 0, 0);

        _connectionNav = CreateNavigationButton("远程连接", (_, _) => ShowContentPage(false));
        _presentationNav = CreateNavigationButton("演示文稿", (_, _) => ShowContentPage(true));
        layout.Controls.Add(_connectionNav, 0, 1);
        layout.Controls.Add(_presentationNav, 0, 2);

        sidebar.Controls.Add(layout);
        layout.BringToFront();
        return sidebar;
    }

    private static RemoteTextButton CreateNavigationButton(string text, EventHandler click)
    {
        var button = new RemoteTextButton
        {
            Text = text,
            Dock = DockStyle.Fill,
            Kind = RemoteButtonKind.Quiet,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 4, 0, 4),
            Padding = new Padding(14, 0, 10, 0),
            Font = RemoteDashboardTheme.CreateFont(9.5F)
        };
        button.Click += click;
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
            Padding = new Padding(
                RemoteDashboardTheme.PagePadding,
                16,
                RemoteDashboardTheme.PagePadding,
                RemoteDashboardTheme.PagePadding),
            Margin = Padding.Empty
        };
        workspace.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
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
        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        header.Controls.Add(_pageTitle, 0, 0);
        header.Controls.Add(_pageSubtitle, 0, 1);
        return header;
    }

    private void ShowContentPage(bool presentation)
    {
        if (_connectionPage is null ||
            _presentationPage is null ||
            _connectionNav is null ||
            _presentationNav is null)
            return;

        _connectionPage.Visible = !presentation;
        _presentationPage.Visible = presentation;
        if (presentation) _presentationPage.BringToFront();
        else _connectionPage.BringToFront();

        _connectionNav.Selected = !presentation;
        _presentationNav.Selected = presentation;
        _pageTitle.Text = presentation ? "演示文稿" : "远程连接";
        _pageSubtitle.Text = presentation ? "规则与放映" : "手机或浏览器访问";

        _presentationTabActive = presentation;
        UpdatePresentationRefreshState();
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
            Height = 590,
            MinimumSize = new Size(800, 590),
            ColumnCount = 1,
            RowCount = 4,
            BackColor = RemoteDashboardTheme.Window,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 12));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, RemoteDashboardTheme.CardGap));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        body.Controls.Add(BuildServiceCard(), 0, 1);
        body.Controls.Add(BuildConnectionColumns(), 0, 3);
        scroll.Controls.Add(body);

        void Reflow()
        {
            var scrollbar = scroll.VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0;
            body.Width = Math.Max(body.MinimumSize.Width, scroll.ClientSize.Width - scrollbar);
            body.Height = Math.Max(body.MinimumSize.Height, scroll.ClientSize.Height);
        }

        scroll.Resize += (_, _) => Reflow();
        scroll.HandleCreated += (_, _) => BeginInvoke((MethodInvoker)Reflow);
        return scroll;
    }

    private Control BuildServiceCard()
    {
        var card = NewSurface(new Padding(16, 12, 16, 12));
        card.AccessibleName = "服务状态卡";
        card.Dock = DockStyle.Fill;
        card.Margin = Padding.Empty;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 52));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 84));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 84));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 84));

        _state.Dock = DockStyle.Fill;
        _state.Margin = Padding.Empty;
        _state.Padding = new Padding(4, 0, 0, 0);
        layout.Controls.Add(_state, 0, 0);

        var portLabel = CreateFieldLabel("端口", ContentAlignment.MiddleRight);
        portLabel.Anchor = AnchorStyles.None;
        portLabel.Size = new Size(44, 40);
        portLabel.Margin = new Padding(0, 0, 8, 0);
        layout.Controls.Add(portLabel, 1, 0);

        var portHost = CreateInputHost(_port, new Padding(8, 7, 8, 7), 40);
        portHost.Anchor = AnchorStyles.None;
        portHost.Size = new Size(84, 40);
        portHost.Margin = Padding.Empty;
        layout.Controls.Add(portHost, 2, 0);

        var restart = CreateActionButton("重启", (_, _) => RestartService(), RemoteButtonKind.Secondary, 76);
        restart.Anchor = AnchorStyles.None;
        restart.Size = new Size(76, 40);
        restart.Margin = new Padding(4, 0, 4, 0);
        layout.Controls.Add(restart, 3, 0);

        _serviceToggle = CreateActionButton("关闭", (_, _) => ToggleService(), RemoteButtonKind.DangerOutline, 76);
        _serviceToggle.Anchor = AnchorStyles.None;
        _serviceToggle.Size = new Size(76, 40);
        _serviceToggle.Margin = new Padding(4, 0, 4, 0);
        layout.Controls.Add(_serviceToggle, 4, 0);

        card.Controls.Add(layout);
        return card;
    }

    private Control BuildConnectionColumns()
    {
        var columns = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = RemoteDashboardTheme.Window,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, RemoteDashboardTheme.CardGap));
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
        columns.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        columns.Controls.Add(BuildQrCard(), 0, 0);
        columns.Controls.Add(BuildBrowserCard(), 2, 0);
        return columns;
    }

    private Control BuildQrCard()
    {
        var card = NewSurface(new Padding(20, 16, 20, 16));
        card.AccessibleName = "二维码卡";
        card.Dock = DockStyle.Fill;
        card.Margin = Padding.Empty;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, RemoteDashboardTheme.SectionGap));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 16));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        layout.Controls.Add(CreateSectionTitle("手机扫码"), 0, 0);

        var qrCenter = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = new Padding(12)
        };
        var qrFrame = NewSurface(new Padding(14));
        qrFrame.Size = new Size(252, 252);
        qrFrame.Anchor = AnchorStyles.None;
        qrFrame.BorderColor = RemoteDashboardTheme.BorderStrong;
        qrFrame.Controls.Add(_qr);
        qrCenter.Controls.Add(qrFrame, 0, 0);
        layout.Controls.Add(qrCenter, 0, 2);

        var tip = NewSurface(new Padding(12, 8, 12, 8));
        tip.Dock = DockStyle.Fill;
        tip.FillColor = RemoteDashboardTheme.InfoSoft;
        tip.BorderColor = Color.FromArgb(191, 214, 248);
        tip.Controls.Add(new Label
        {
            Text = "手机与电脑需连接同一网络。",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = RemoteDashboardTheme.CreateFont(9F),
            ForeColor = RemoteDashboardTheme.Info,
            UseCompatibleTextRendering = false
        });
        layout.Controls.Add(tip, 0, 4);

        card.Controls.Add(layout);
        return card;
    }

    private Control BuildBrowserCard()
    {
        var card = NewSurface(new Padding(22, 16, 22, 16));
        card.AccessibleName = "浏览器访问卡";
        card.Dock = DockStyle.Fill;
        card.Margin = Padding.Empty;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 12,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 8));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, RemoteDashboardTheme.ControlGap));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, RemoteDashboardTheme.ControlGap));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, RemoteDashboardTheme.SectionGap));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        layout.Controls.Add(CreateSectionTitle("浏览器访问"), 0, 0);
        layout.Controls.Add(CreateFieldLabel("地址"), 0, 2);

        _address.SelectedAddressChanged += (_, _) => UpdateUrlAndQr();
        layout.Controls.Add(_address, 0, 3);

        layout.Controls.Add(CreateFieldLabel("链接"), 0, 5);
        layout.Controls.Add(CreateInputHost(_url, new Padding(10, 9, 10, 7)), 0, 6);

        var actions = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, RemoteDashboardTheme.ControlGap));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, RemoteDashboardTheme.ControlGap));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));

        var copy = CreateActionButton(
            "复制链接",
            (_, _) => CopyText(CurrentUrl(), "已复制。"),
            RemoteButtonKind.Primary);
        copy.Dock = DockStyle.Fill;
        copy.Margin = new Padding(0, 4, 0, 4);
        actions.Controls.Add(copy, 0, 0);

        var open = CreateActionButton(
            "浏览器打开",
            (_, _) => OpenCurrentUrl(),
            RemoteButtonKind.Secondary);
        open.Dock = DockStyle.Fill;
        open.Margin = new Padding(0, 4, 0, 4);
        actions.Controls.Add(open, 2, 0);

        var firewall = CreateActionButton(
            "放行命令",
            (_, _) => CopyText(BuildFirewallCommand(), "命令已复制。"),
            RemoteButtonKind.Secondary);
        firewall.Dock = DockStyle.Fill;
        firewall.Margin = new Padding(0, 4, 0, 4);
        actions.Controls.Add(firewall, 4, 0);
        layout.Controls.Add(actions, 0, 8);

        var feedback = NewSurface(new Padding(12, 7, 12, 7));
        feedback.Dock = DockStyle.Fill;
        feedback.FillColor = RemoteDashboardTheme.InfoSoft;
        feedback.BorderColor = Color.FromArgb(191, 214, 248);
        _connectionFeedback.Text = "同一网络可访问。";
        feedback.Controls.Add(_connectionFeedback);
        layout.Controls.Add(feedback, 0, 10);

        card.Controls.Add(layout);
        return card;
    }

    private Control BuildPresentationPage()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            BackColor = RemoteDashboardTheme.Window,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 10));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 10));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, RemoteDashboardTheme.SectionGap));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(BuildPresentationToolbar(), 0, 1);
        root.Controls.Add(BuildPresentationStatus(), 0, 3);
        root.Controls.Add(BuildPresentationWorkspace(), 0, 5);
        return root;
    }

    private Control BuildPresentationToolbar()
    {
        var card = NewSurface(new Padding(8, 2, 8, 2));
        card.AccessibleName = "演示工具栏";
        card.Dock = DockStyle.Fill;
        card.Margin = Padding.Empty;

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

        var add = CreateActionButton("添加", (_, _) => AddPresentationRules(), RemoteButtonKind.Primary, 74);
        _deleteRuleButton = CreateActionButton("删除", (_, _) => DeleteSelectedRule(), RemoteButtonKind.DangerOutline, 74);
        var refresh = CreateActionButton("刷新", (_, _) => RefreshPresentationPanel(), RemoteButtonKind.Secondary, 74);
        foreach (var button in new[] { add, _deleteRuleButton, refresh })
        {
            button.Height = 40;
            button.Margin = new Padding(0, 0, RemoteDashboardTheme.ControlGap, 0);
        }

        actions.Controls.Add(add);
        actions.Controls.Add(_deleteRuleButton);
        actions.Controls.Add(refresh);
        card.Controls.Add(actions);
        return card;
    }

    private Control BuildPresentationStatus()
    {
        var card = NewSurface(new Padding(12, 4, 12, 4));
        card.AccessibleName = "演示状态提示";
        card.Dock = DockStyle.Fill;
        card.Margin = Padding.Empty;
        card.FillColor = RemoteDashboardTheme.InfoSoft;
        card.BorderColor = Color.FromArgb(191, 214, 248);
        _presentationStatus.Text = "请选择演示文稿。";
        card.Controls.Add(_presentationStatus);
        return card;
    }

    private Control BuildPresentationWorkspace()
    {
        var split = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = RemoteDashboardTheme.Window,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, RemoteDashboardTheme.CardGap));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        split.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        split.Controls.Add(BuildPresentationList(), 0, 0);
        split.Controls.Add(BuildPresentationDetails(), 2, 0);
        return split;
    }

    private Control BuildPresentationList()
    {
        var card = NewSurface(new Padding(12, 10, 8, 8));
        card.AccessibleName = "演示文稿列表卡";
        card.Dock = DockStyle.Fill;
        card.Margin = Padding.Empty;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.Controls.Add(CreateSectionTitle("演示文稿"), 0, 0);

        _ruleListHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = RemoteDashboardTheme.Card,
            Margin = Padding.Empty
        };
        _ruleListHost.Controls.Add(_ruleList);
        _ruleListHost.Controls.Add(_emptyList);
        _emptyList.BringToFront();
        _ruleList.SizeChanged += (_, _) => UpdateRuleRowWidths();
        layout.Controls.Add(_ruleListHost, 0, 1);

        _ruleCount.Text = "0 项";
        layout.Controls.Add(_ruleCount, 0, 2);
        card.Controls.Add(layout);
        return card;
    }

    private Control BuildPresentationDetails()
    {
        var details = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 7,
            BackColor = RemoteDashboardTheme.Window,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        details.RowStyles.Add(new RowStyle(SizeType.Absolute, 222));
        details.RowStyles.Add(new RowStyle(SizeType.Absolute, RemoteDashboardTheme.SectionGap));
        details.RowStyles.Add(new RowStyle(SizeType.Absolute, 106));
        details.RowStyles.Add(new RowStyle(SizeType.Absolute, RemoteDashboardTheme.SectionGap));
        details.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        details.RowStyles.Add(new RowStyle(SizeType.Absolute, 0));
        details.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        details.Controls.Add(BuildRuleEditor(), 0, 0);
        details.Controls.Add(BuildPresentationActions(), 0, 2);
        details.Controls.Add(BuildDangerActions(), 0, 4);
        return details;
    }

    private Control BuildRuleEditor()
    {
        var card = NewSurface(new Padding(16, 12, 16, 12));
        card.AccessibleName = "规则编辑卡";
        card.Dock = DockStyle.Fill;
        card.Margin = Padding.Empty;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        _detailTitle.Text = "未选择";
        layout.Controls.Add(_detailTitle, 0, 0);
        layout.Controls.Add(CreateFieldLabel("路径"), 0, 1);

        var pathHost = CreateInputHost(_rulePath, new Padding(10, 7, 10, 7), 60);
        pathHost.Margin = Padding.Empty;
        layout.Controls.Add(pathHost, 0, 2);
        layout.Controls.Add(CreateFieldLabel("时长与规则"), 0, 3);

        var controls = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = false,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        var durationHost = CreateInputHost(_ruleDuration, new Padding(8, 7, 8, 7), 40);
        durationHost.Width = 112;
        durationHost.Height = 40;
        durationHost.Margin = new Padding(0, 4, RemoteDashboardTheme.ControlGap, 4);
        controls.Controls.Add(durationHost);

        _ruleEnabledButton = CreateActionButton("启用", (_, _) => ToggleSelectedRuleEnabled(), RemoteButtonKind.Secondary, 72);
        _ruleEnabledButton.Width = 76;
        _ruleEnabledButton.Height = 40;
        _ruleEnabledButton.Margin = new Padding(0, 4, RemoteDashboardTheme.ControlGap, 4);
        controls.Controls.Add(_ruleEnabledButton);

        _moreActionsButton = CreateActionButton("更多", (_, _) => ShowMoreActions(_moreActionsButton), RemoteButtonKind.Secondary, 70);
        _moreActionsButton.Width = 76;
        _moreActionsButton.Height = 40;
        _moreActionsButton.Margin = new Padding(0, 4, RemoteDashboardTheme.ControlGap, 4);
        controls.Controls.Add(_moreActionsButton);

        _saveDurationButton = CreateActionButton("保存", (_, _) => SaveSelectedDuration(), RemoteButtonKind.Primary, 72);
        _saveDurationButton.Width = 76;
        _saveDurationButton.Height = 40;
        _saveDurationButton.Margin = new Padding(0, 4, 0, 4);
        controls.Controls.Add(_saveDurationButton);
        layout.Controls.Add(controls, 0, 4);

        _ruleDuration.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            SaveSelectedDuration();
        };
        _ruleDuration.TextChanged += (_, _) =>
        {
            if (!_updatingRuleEditor) _durationDirty = true;
        };
        _ruleDuration.Leave += (_, _) => SaveSelectedDuration();

        card.Controls.Add(layout);
        return card;
    }

    private Control BuildPresentationActions()
    {
        var card = NewSurface(new Padding(8, 7, 8, 7));
        card.AccessibleName = "放映卡";
        card.Dock = DockStyle.Fill;
        card.Margin = Padding.Empty;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 6));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.Controls.Add(CreateSectionTitle("放映"), 0, 0);

        var actions = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        for (var i = 0; i < 4; i++)
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

        _openPresentationButton = CreateActionButton(
            "打开",
            (_, _) => SendPresentationCommand("ppt.openPresentation"),
            RemoteButtonKind.Secondary);
        _startFromBeginningButton = CreateActionButton(
            "从头放映",
            (_, _) => SendPresentationCommand("ppt.startFromBeginning"),
            RemoteButtonKind.Secondary);
        _startFromCurrentButton = CreateActionButton(
            "当前页放映",
            (_, _) => SendPresentationCommand("ppt.startFromCurrent"),
            RemoteButtonKind.Secondary);
        var end = CreateActionButton(
            "结束放映",
            (_, _) => SendPresentationCommand("ppt.endShow"),
            RemoteButtonKind.DangerOutline);

        AddEqualButton(actions, _openPresentationButton, 0, true, false);
        AddEqualButton(actions, _startFromBeginningButton, 1, false, false);
        AddEqualButton(actions, _startFromCurrentButton, 2, false, false);
        AddEqualButton(actions, end, 3, false, true);
        layout.Controls.Add(actions, 0, 2);

        card.Controls.Add(layout);
        return card;
    }

    private Control BuildDangerActions()
    {
        var card = NewSurface(new Padding(16, 5, 16, 5));
        card.AccessibleName = "危险操作卡";
        card.Dock = DockStyle.Fill;
        card.BorderColor = Color.FromArgb(247, 190, 190);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        var title = CreateSectionTitle("危险操作");
        title.ForeColor = RemoteDashboardTheme.Danger;
        layout.Controls.Add(title, 0, 0);

        var actions = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        var exit = CreateActionButton(
            "退出 PowerPoint",
            (_, _) => SendPresentationCommand("ppt.exitApplication"),
            RemoteButtonKind.DangerOutline);
        exit.Dock = DockStyle.Fill;
        exit.Margin = new Padding(0, 4, 5, 4);

        var force = CreateActionButton(
            "强制退出",
            (_, _) => ConfirmForceQuit(),
            RemoteButtonKind.DangerOutline);
        force.Dock = DockStyle.Fill;
        force.Margin = new Padding(5, 4, 0, 4);

        actions.Controls.Add(exit, 0, 0);
        actions.Controls.Add(force, 1, 0);
        layout.Controls.Add(actions, 0, 1);
        card.Controls.Add(layout);
        return card;
    }

    private static void AddEqualButton(
        TableLayoutPanel layout,
        RemoteTextButton button,
        int column,
        bool first,
        bool last)
    {
        button.Dock = DockStyle.Fill;
        button.Padding = new Padding(6, 0, 6, 0);
        button.Margin = new Padding(first ? 0 : 3, 4, last ? 0 : 3, 4);
        layout.Controls.Add(button, column, 0);
    }

    private static RemoteSurface NewSurface(Padding padding) => new()
    {
        Padding = new Padding(
            padding.Left + 2,
            padding.Top + 2,
            padding.Right + 2,
            padding.Bottom + 2),
        FillColor = RemoteDashboardTheme.Card,
        BorderColor = RemoteDashboardTheme.Border,
        CornerRadius = RemoteDashboardTheme.CardRadius
    };

    private static Label CreateSectionTitle(string text)
    {
        var label = new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = RemoteDashboardTheme.CreateFont(11F, FontStyle.Bold),
            ForeColor = RemoteDashboardTheme.Text,
            AutoEllipsis = false,
            Margin = Padding.Empty,
            UseCompatibleTextRendering = false
        };
        label.MinimumSize = new Size(0, RemoteDashboardTheme.GetSafeTextHeight(label, label.Font, 8));
        return label;
    }

    private static Label CreateFieldLabel(
        string text,
        ContentAlignment alignment = ContentAlignment.MiddleLeft)
    {
        var font = RemoteDashboardTheme.CreateFont(8.75F, FontStyle.Bold);
        var label = new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = alignment,
            Font = font,
            ForeColor = RemoteDashboardTheme.Text,
            AutoEllipsis = false,
            UseCompatibleTextRendering = false
        };
        label.MinimumSize = new Size(0, RemoteDashboardTheme.GetSafeTextHeight(label, font, 6));
        return label;
    }

    private static RemoteTextButton CreateActionButton(
        string text,
        EventHandler click,
        RemoteButtonKind kind,
        int minimumWidth = 0)
    {
        using var measureFont = RemoteDashboardTheme.CreateFont(9.5F);
        var measured = TextRenderer.MeasureText(
            text,
            measureFont,
            Size.Empty,
            TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix).Width + 28;

        var button = new RemoteTextButton
        {
            Text = text,
            Kind = kind,
            Width = Math.Max(minimumWidth, measured),
            Height = RemoteDashboardTheme.ButtonHeight,
            MinimumSize = new Size(0, RemoteDashboardTheme.ButtonHeight),
            Margin = new Padding(0, 0, 8, 0),
            Font = RemoteDashboardTheme.CreateFont(9.5F)
        };
        button.Click += click;
        return button;
    }

    private static RemoteSurface CreateInputHost(
        Control control,
        Padding padding,
        int height = RemoteDashboardTheme.InputHeight)
    {
        var host = NewSurface(padding);
        host.Dock = DockStyle.Fill;
        host.Height = height;
        host.MinimumSize = new Size(0, height);
        host.FillColor = RemoteDashboardTheme.Field;
        host.BorderColor = RemoteDashboardTheme.Border;
        host.CornerRadius = RemoteDashboardTheme.ControlRadius;

        control.Dock = DockStyle.Fill;
        control.Margin = Padding.Empty;
        control.BackColor = RemoteDashboardTheme.Field;
        control.ForeColor = RemoteDashboardTheme.Text;
        control.Font = RemoteDashboardTheme.CreateFont(9.5F);
        host.Controls.Add(control);
        return host;
    }

    private void AddPresentationRules()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "添加演示文稿",
            Filter = "PowerPoint (*.ppt;*.pptx;*.pptm;*.pps;*.ppsx)|*.ppt;*.pptx;*.pptm;*.pps;*.ppsx|所有文件 (*.*)|*.*",
            Multiselect = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        foreach (var path in dialog.FileNames)
        {
            var normalized = NormalizePath(path);
            if (_config.Rules.Any(rule =>
                    string.Equals(
                        NormalizePath(rule.FilePath),
                        normalized,
                        StringComparison.OrdinalIgnoreCase)))
                continue;

            _config.Rules.Add(new FileRule
            {
                FileName = Path.GetFileName(path),
                FilePath = normalized,
                Duration = _config.Timer.DefaultDuration,
                Enabled = true
            });
        }

        SaveRulesImmediately();
        SetPresentationFeedback("已添加。", FeedbackKind.Success);
        RefreshPresentationPanel();
    }

    private void DeleteSelectedRule()
    {
        if (_selectedRule is null)
        {
            SetPresentationFeedback("请先选择。", FeedbackKind.Warning);
            return;
        }

        _config.Rules.Remove(_selectedRule);
        _selectedRule = null;
        _selectedPresentationId = null;
        _selectedPresentationPath = null;
        SaveRulesImmediately();
        SetPresentationFeedback("已删除。", FeedbackKind.Info);
        RefreshPresentationPanel();
    }

    private void RefreshPresentationPanel()
    {
        if (IsDisposed) return;

        var state = _powerPoint?.GetState() ??
                    new PresentationState { Error = "PowerPoint 不可用。" };
        var message = !string.IsNullOrWhiteSpace(state.OperationMessage)
            ? state.OperationMessage
            : state.Error;

        if (!string.IsNullOrWhiteSpace(message))
            SetPresentationFeedback(
                message,
                string.IsNullOrWhiteSpace(state.Error)
                    ? FeedbackKind.Info
                    : FeedbackKind.Warning);

        RenderRuleRows(state);
        RefreshRuleEditor();
    }

    private void RenderRuleRows(PresentationState state)
    {
        var selectedPath = _selectedPresentationPath ??
                           _selectedRule?.FilePath ??
                           string.Empty;
        var items = PresentationRuleValidator.MergeRulesAndOpenPresentations(
            _config.Rules,
            state.Presentations);
        var scrollY = -_ruleList.AutoScrollPosition.Y;

        _ruleList.SuspendLayout();
        foreach (var obsolete in _ruleRows.Keys
                     .Except(items.Select(item => item.Path), StringComparer.OrdinalIgnoreCase)
                     .ToArray())
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
                row = new RemotePresentationRow();
                row.Selected += (_, _) =>
                    SelectPresentation(
                        row.CurrentRule,
                        row.CurrentPresentation,
                        row.CurrentPath);
                row.EnabledChangedByUser += (_, enabled) =>
                {
                    var currentRule = row.CurrentRule;
                    if (currentRule is null) return;
                    currentRule.Enabled = enabled;
                    SaveRulesImmediately();
                    SetPresentationFeedback(
                        enabled ? "已启用。" : "已禁用。",
                        FeedbackKind.Info);
                    RefreshPresentationPanel();
                };
                _ruleRows.Add(key, row);
                _ruleList.Controls.Add(row);
            }

            row.Update(
                item.Rule,
                item.Presentation,
                SamePath(item.Path, selectedPath),
                File.Exists(item.Path));
            _ruleList.Controls.SetChildIndex(row, index++);
        }

        _ruleList.ResumeLayout();
        _ruleCount.Text = $"{items.Count} 项";
        _emptyList.Visible = items.Count == 0;
        _ruleList.Visible = items.Count > 0;
        UpdateRuleRowWidths();
        if (scrollY > 0)
            _ruleList.AutoScrollPosition = new Point(0, scrollY);
    }

    private void UpdateRuleRowWidths()
    {
        var scrollbar = _ruleList.VerticalScroll.Visible
            ? SystemInformation.VerticalScrollBarWidth
            : 0;
        var width = Math.Max(
            280,
            _ruleList.ClientSize.Width -
            _ruleList.Padding.Horizontal -
            scrollbar -
            2);

        foreach (Control control in _ruleList.Controls)
            control.Width = width;
    }

    private void SelectPresentation(
        FileRule? rule,
        PresentationOption? option,
        string path)
    {
        _selectedRule = rule;
        _selectedPresentationPath = path;
        _selectedPresentationId = option?.Id ??
                                  PresentationRuleValidator.IdForPath(path);
        RefreshPresentationPanel();
    }

    private void RefreshRuleEditor()
    {
        var rule = _selectedRule;
        if (rule is not null && !_config.Rules.Contains(rule))
            rule = _selectedRule = null;

        var hasSelection = !string.IsNullOrWhiteSpace(_selectedPresentationId);
        var hasRule = rule is not null;

        _updatingRuleEditor = true;
        var selectedName = rule?.FileName ??
                           (!string.IsNullOrWhiteSpace(_selectedPresentationPath)
                               ? Path.GetFileName(_selectedPresentationPath)
                               : null);

        _detailTitle.Text = selectedName ?? "未选择";
        _rulePath.Text = rule?.FilePath ??
                         _selectedPresentationPath ??
                         "请选择演示文稿";
        _toolTip.SetToolTip(
            _rulePath,
            rule?.FilePath ?? _selectedPresentationPath ?? string.Empty);
        _ruleDuration.Text = rule?.Duration ?? string.Empty;
        _durationDirty = false;
        SetRuleButton(rule?.Enabled == true);
        _ruleDuration.Enabled = hasRule;
        if (_ruleEnabledButton is not null) _ruleEnabledButton.Enabled = hasRule;
        _updatingRuleEditor = false;

        if (_deleteRuleButton is not null) _deleteRuleButton.Enabled = hasRule;
        if (_saveDurationButton is not null) _saveDurationButton.Enabled = hasRule;
        if (_moreActionsButton is not null) _moreActionsButton.Enabled = hasSelection;
        if (_openPresentationButton is not null) _openPresentationButton.Enabled = hasSelection;
        if (_startFromBeginningButton is not null) _startFromBeginningButton.Enabled = hasSelection;
        if (_startFromCurrentButton is not null) _startFromCurrentButton.Enabled = hasSelection;
    }

    private void SetRuleButton(bool enabled)
    {
        if (_ruleEnabledButton is null) return;
        _ruleEnabledButton.Text = enabled ? "禁用" : "启用";
        _ruleEnabledButton.Kind = enabled
            ? RemoteButtonKind.Secondary
            : RemoteButtonKind.Primary;
    }

    private void ToggleSelectedRuleEnabled()
    {
        if (_updatingRuleEditor || _selectedRule is null) return;
        _selectedRule.Enabled = !_selectedRule.Enabled;
        SetRuleButton(_selectedRule.Enabled);
        SaveRulesImmediately();
        SetPresentationFeedback(
            _selectedRule.Enabled ? "已启用。" : "已禁用。",
            FeedbackKind.Info);
        RefreshPresentationPanel();
    }

    private void ShowMoreActions(RemoteTextButton? button)
    {
        if (button is null || IsDisposed) return;
        _moreActionsMenu ??= CreateMoreActionsMenu();
        if (_moreActionsMenu.Visible) return;

        var path = _selectedRule?.FilePath ?? _selectedPresentationPath;
        if (_copyPathMenuItem is not null)
            _copyPathMenuItem.Enabled = !string.IsNullOrWhiteSpace(path);
        if (_showPathMenuItem is not null)
            _showPathMenuItem.Enabled =
                !string.IsNullOrWhiteSpace(path) && File.Exists(path);

        _moreActionsMenu.Show(button, new Point(0, button.Height + 3));
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
            Padding = new Padding(5)
        };

        _copyPathMenuItem = new ToolStripMenuItem(
            "复制路径",
            null,
            (_, _) => CopySelectedPath());
        _showPathMenuItem = new ToolStripMenuItem(
            "显示文件",
            null,
            (_, _) => ShowSelectedPath());

        menu.Items.Add(_copyPathMenuItem);
        menu.Items.Add(_showPathMenuItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(
            "关闭受控文稿",
            null,
            (_, _) => SendPresentationCommand("ppt.closeCurrentPresentation"));

        foreach (ToolStripItem item in menu.Items.OfType<ToolStripMenuItem>())
        {
            item.AutoSize = false;
            item.Height = 36;
            item.Padding = new Padding(10, 0, 10, 0);
        }

        return menu;
    }

    private void SaveSelectedDuration()
    {
        if (_updatingRuleEditor || _selectedRule is null || !_durationDirty)
            return;

        if (!PresentationRuleValidator.TryNormalizeDuration(
                _ruleDuration.Text,
                out var duration,
                out var error))
        {
            SetPresentationFeedback(error, FeedbackKind.Warning);
            return;
        }

        if (string.Equals(
                _selectedRule.Duration,
                duration,
                StringComparison.Ordinal))
        {
            _durationDirty = false;
            return;
        }

        _selectedRule.Duration = duration;
        _ruleDuration.Text = duration;
        _durationDirty = false;
        SaveRulesImmediately();
        SetPresentationFeedback("已保存。", FeedbackKind.Success);
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
            SetPresentationFeedback("请先选择。", FeedbackKind.Warning);
            return;
        }

        CopyText(path, "已复制。", presentationFeedback: true);
    }

    private void ShowSelectedPath()
    {
        var path = _selectedRule?.FilePath ?? _selectedPresentationPath;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            SetPresentationFeedback("文件不存在。", FeedbackKind.Warning);
            return;
        }

        try
        {
            Process.Start(
                new ProcessStartInfo(
                    "explorer.exe",
                    $"/select,\"{path}\"")
                {
                    UseShellExecute = true
                });
        }
        catch
        {
            SetPresentationFeedback("打开失败。", FeedbackKind.Warning);
        }
    }

    private void SendPresentationCommand(string command)
    {
        if (_powerPoint is null)
        {
            SetPresentationFeedback("PowerPoint 不可用。", FeedbackKind.Warning);
            return;
        }

        var needsSelection = command is
            "ppt.openPresentation" or
            "ppt.startFromBeginning" or
            "ppt.startFromCurrent";

        if (needsSelection &&
            string.IsNullOrWhiteSpace(_selectedPresentationId))
        {
            SetPresentationFeedback("请先选择。", FeedbackKind.Warning);
            return;
        }

        var result = _powerPoint.Execute(new RemoteCommand
        {
            Command = command,
            PresentationId = needsSelection
                ? _selectedPresentationId
                : null
        });
        SetPresentationFeedback(
            result.Message,
            result.Success
                ? FeedbackKind.Success
                : FeedbackKind.Warning);
        RefreshPresentationPanel();
    }

    private void ConfirmForceQuit()
    {
        if (!RemoteConfirmDialog.Confirm(this)) return;
        if (_powerPoint is null)
        {
            SetPresentationFeedback("PowerPoint 不可用。", FeedbackKind.Warning);
            return;
        }

        var result = _powerPoint.Queue(new RemoteCommand
        {
            Command = "ppt.forceQuitAll",
            Confirmed = true
        });
        SetPresentationFeedback(
            result.Message,
            result.Success
                ? FeedbackKind.Info
                : FeedbackKind.Warning);
        RefreshPresentationPanel();
    }

    private void SetPresentationFeedback(string? message, FeedbackKind kind)
    {
        _presentationStatus.Text = string.IsNullOrWhiteSpace(message)
            ? "请选择演示文稿。"
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

        if (_config.RemoteControl.Enabled)
            _remoteControl.Restart();
        else
            _remoteControl.Stop();

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
        if (_config.RemoteControl.Enabled && !_remoteControl.IsRunning)
            _remoteControl.Start();

        _port.Text = Math.Clamp(
                _remoteControl.CurrentPort > 0
                    ? _remoteControl.CurrentPort
                    : _config.RemoteControl.Port <= 0
                        ? 1
                        : _config.RemoteControl.Port,
                1,
                65535)
            .ToString(CultureInfo.InvariantCulture);

        var previousAddress = _address.SelectedAddress;
        _address.SetAddresses(
            _networkAddressService.GetIPv4Addresses().Select(item => item.Address),
            previousAddress);

        var running = _remoteControl.IsRunning;
        _state.Text = running ? "运行中" : "已停止";
        _state.ForeColor = running
            ? RemoteDashboardTheme.Success
            : RemoteDashboardTheme.Danger;

        if (_serviceToggle is not null)
        {
            _serviceToggle.Text = running ? "关闭" : "启动";
            _serviceToggle.Kind = running
                ? RemoteButtonKind.DangerOutline
                : RemoteButtonKind.Primary;
        }

        UpdateUrlAndQr();
    }

    private void UpdateUrlAndQr()
    {
        var url = CurrentUrl();
        _url.Text = RemoteUrlPrivacy.MaskToken(url);
        _toolTip.SetToolTip(
            _url,
            "显示已隐藏 token；复制仍为完整链接。");

        _qr.Image?.Dispose();
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(
            url,
            QRCodeGenerator.ECCLevel.Q);
        using var code = new QRCode(data);
        _qr.Image = code.GetGraphic(
            8,
            Color.Black,
            Color.White,
            true);
    }

    private string CurrentUrl()
    {
        var address = _address.SelectedAddress;
        if (string.IsNullOrWhiteSpace(address))
            address = "127.0.0.1";

        var port = _remoteControl.CurrentPort > 0
            ? _remoteControl.CurrentPort
            : ReadPort();

        return $"http://{address}:{port}/?token={_config.RemoteControl.Token}";
    }

    private string BuildFirewallCommand()
    {
        var port = _remoteControl.CurrentPort > 0
            ? _remoteControl.CurrentPort
            : ReadPort();
        var exe = Path.Combine(
            AppContext.BaseDirectory,
            "FlyPPTTimer.exe");

        return $"netsh advfirewall firewall add rule name=\"FlyPPTTimer Remote {port}\" dir=in action=allow program=\"{exe}\" protocol=TCP localport={port}";
    }

    private int ReadPort()
    {
        if (!int.TryParse(
                _port.Text.Trim(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var port) &&
            !int.TryParse(_port.Text.Trim(), out port))
        {
            port = _config.RemoteControl.Port <= 0
                ? 1
                : _config.RemoteControl.Port;
        }

        port = Math.Clamp(port, 1, 65535);
        _port.Text = port.ToString(CultureInfo.InvariantCulture);
        return port;
    }

    private void CopyText(
        string text,
        string successMessage,
        bool presentationFeedback = false)
    {
        try
        {
            Clipboard.SetText(text);
            if (presentationFeedback)
            {
                SetPresentationFeedback(
                    successMessage,
                    FeedbackKind.Success);
            }
            else
            {
                _connectionFeedback.Text = successMessage;
                _connectionFeedback.ForeColor =
                    RemoteDashboardTheme.Success;
            }
        }
        catch
        {
            if (presentationFeedback)
            {
                SetPresentationFeedback(
                    "复制失败。",
                    FeedbackKind.Warning);
            }
            else
            {
                _connectionFeedback.Text = "复制失败。";
                _connectionFeedback.ForeColor =
                    RemoteDashboardTheme.Warning;
            }
        }
    }

    private void OpenCurrentUrl()
    {
        try
        {
            Process.Start(
                new ProcessStartInfo(CurrentUrl())
                {
                    UseShellExecute = true
                });
            _connectionFeedback.Text = "已打开。";
            _connectionFeedback.ForeColor =
                RemoteDashboardTheme.Success;
        }
        catch
        {
            _connectionFeedback.Text = "打开失败，请复制链接。";
            _connectionFeedback.ForeColor =
                RemoteDashboardTheme.Warning;
        }
    }

    private static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        try
        {
            return Path.GetFullPath(path)
                .TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar);
        }
        catch
        {
            return path.Trim();
        }
    }

    private static bool SamePath(string? left, string? right) =>
        string.Equals(
            NormalizePath(left),
            NormalizePath(right),
            StringComparison.OrdinalIgnoreCase);
}

internal enum FeedbackKind
{
    Info,
    Success,
    Warning
}

internal sealed class RemoteConfirmDialog : Form
{
    private RemoteConfirmDialog()
    {
        Text = "确认";
        Font = RemoteDashboardTheme.CreateFont(9.5F);
        AutoScaleMode = AutoScaleMode.Dpi;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        ShowInTaskbar = false;
        MinimizeBox = false;
        MaximizeBox = false;
        ClientSize = new Size(480, 176);
        BackColor = RemoteDashboardTheme.Window;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(22),
            ColumnCount = 1,
            RowCount = 3,
            BackColor = RemoteDashboardTheme.Window,
            Margin = Padding.Empty
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

        root.Controls.Add(new Label
        {
            Text = "确认强制退出",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = RemoteDashboardTheme.CreateFont(12.5F, FontStyle.Bold),
            ForeColor = RemoteDashboardTheme.Danger,
            UseCompatibleTextRendering = false
        }, 0, 0);

        root.Controls.Add(new Label
        {
            Text = "将关闭全部 PowerPoint/WPS，未保存内容会丢失。",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = RemoteDashboardTheme.CreateFont(9.5F),
            ForeColor = RemoteDashboardTheme.Text,
            UseCompatibleTextRendering = false
        }, 0, 1);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        var confirm = new RemoteTextButton
        {
            Text = "强制退出",
            DialogResult = DialogResult.OK,
            Width = 104,
            Kind = RemoteButtonKind.Danger,
            Margin = new Padding(8, 4, 0, 4)
        };
        var cancel = new RemoteTextButton
        {
            Text = "取消",
            DialogResult = DialogResult.Cancel,
            Width = 88,
            Kind = RemoteButtonKind.Secondary,
            Margin = new Padding(8, 4, 0, 4)
        };

        actions.Controls.Add(confirm);
        actions.Controls.Add(cancel);
        root.Controls.Add(actions, 0, 2);
        Controls.Add(root);

        CancelButton = cancel;
        ActiveControl = cancel;
    }

    public static bool Confirm(IWin32Window owner)
    {
        using var dialog = new RemoteConfirmDialog();
        return dialog.ShowDialog(owner) == DialogResult.OK;
    }
}
