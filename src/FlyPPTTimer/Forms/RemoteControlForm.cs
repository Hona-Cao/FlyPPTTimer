using FlyPPTTimer.Models;
using FlyPPTTimer.Services;
using QRCoder;
using System.Diagnostics;

namespace FlyPPTTimer.Forms;

/// <summary>
/// v0.16 text-first remote-control workspace.
/// This file replaces only the PC window composition. Existing services and commands remain unchanged.
/// </summary>
public sealed class RemoteControlForm : Form
{
    private AppConfig _config;
    private readonly RemoteControlService _remoteControl;
    private readonly PresentationCommandService? _presentationCommands;
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

    private readonly VerticalFlowLayoutPanel _ruleList = new()
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
    private readonly System.Windows.Forms.Timer _responsiveLayoutTimer =
        new() { Interval = 75 };
    private readonly System.Windows.Forms.Timer _placementSaveTimer =
        new() { Interval = 400 };
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
    private RemoteTextButton? _clearRulesButton;
    private RemoteTextButton? _ruleEnabledButton;
    private RemoteTextButton? _saveDurationButton;
    private RemoteTextButton? _moreActionsButton;
    private RemoteTextButton? _browsePathButton;
    private RemoteTextButton? _openPresentationButton;
    private RemoteTextButton? _startFromBeginningButton;
    private RemoteTextButton? _startFromCurrentButton;
    private RemoteTextButton? _closeActivePresentationButton;

    private Panel? _contentHost;
    private Control? _connectionPage;
    private Control? _presentationPage;
    private Panel? _ruleListHost;
    private TableLayoutPanel? _shell;
    private TableLayoutPanel? _workspace;
    private TableLayoutPanel? _pageHeaderLayout;
    private Panel? _connectionScroll;
    private TableLayoutPanel? _connectionBody;
    private TableLayoutPanel? _connectionColumns;
    private TableLayoutPanel? _browserLayout;
    private TableLayoutPanel? _qrLayout;
    private TableLayoutPanel? _browserActions;
    private TableLayoutPanel? _presentationRoot;
    private TableLayoutPanel? _presentationSplit;
    private Panel? _presentationDetailsViewport;
    private FlowLayoutPanel? _presentationDetailsFlow;
    private TableLayoutPanel? _presentationActionsLayout;
    private TableLayoutPanel? _presentationCardLayout;
    private TableLayoutPanel? _ruleEditorLayout;
    private TableLayoutPanel? _presentationListLayout;
    private RemoteSurface? _ruleEditorCard;
    private RemoteSurface? _presentationActionsCard;
    private RemoteSurface? _dangerActionsCard;
    private RemoteSurface? _qrFrame;
    private TableLayoutPanel? _qrCenter;
    private RemoteTextButton? _copyLinkButton;
    private RemoteTextButton? _openBrowserButton;
    private RemoteTextButton? _firewallButton;
    private RemoteTextButton? _endSlideShowButton;
    private Action? _reflowConnection;

    private ContextMenuStrip? _moreActionsMenu;
    private ToolStripMenuItem? _copyPathMenuItem;
    private ToolStripMenuItem? _showPathMenuItem;

    private FileRule? _selectedRule;
    private string? _selectedPresentationId;
    private string? _selectedPresentationPath;
    private bool _updatingRuleEditor;
    private bool _durationDirty;
    private bool _slideShowRunning;
    private bool _presentationTabActive;
    private bool _restoringPlacement;
    private bool _savingPlacement;
    private bool _placementLoaded;
    private bool _responsiveLayoutPending;
    private bool _interactiveResize;
    private bool _initialLayoutApplied;
    private RemoteLayoutMode? _layoutMode;
    private FormWindowState _lastWindowState = FormWindowState.Normal;

    public RemoteControlForm(
        AppConfig config,
        RemoteControlService remoteControl,
        NetworkAddressService networkAddressService,
        Action<AppConfig> saveConfig)
    {
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96F, 96F);
        _config = config;
        _remoteControl = remoteControl;
        _presentationCommands = remoteControl.PresentationCommands;
        _networkAddressService = networkAddressService;
        _saveConfig = saveConfig;

        Text = "FlyPPTTimer";
        StartPosition = FormStartPosition.Manual;
        Font = RemoteDashboardTheme.CreateFont(9.5F);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = true;
        BackColor = RemoteDashboardTheme.Window;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.UserPaint,
            true);

        ConfigureText();
        Build();
        RefreshState();
        Localization.Attach(this);
        _responsiveLayoutTimer.Tick += (_, _) => ApplyScheduledResponsiveLayout();
        _placementSaveTimer.Tick += (_, _) => SavePlacementNow();
    }

    public void ReloadConfig(AppConfig config)
    {
        _config = config;
        if (_savingPlacement) return;
        if (IsDisposed) return;
        RefreshState();
        if (_presentationTabActive) RefreshPresentationPanel();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        if (!_placementLoaded) RestoreWindowPlacement();
        PrepareInitialLayout();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (!_initialLayoutApplied) PrepareInitialLayout();
    }

    private void PrepareInitialLayout()
    {
        if (_initialLayoutApplied || ClientSize.Width <= 0 || ClientSize.Height <= 0) return;

        _restoringPlacement = true;
        try
        {
            if (_config.RemoteControl.Window.Maximized)
                WindowState = FormWindowState.Maximized;

            _responsiveLayoutTimer.Stop();
            _responsiveLayoutPending = false;
            ApplyResponsiveLayout(RemoteWindowLayoutService.GetLayoutMode(ClientSize, DeviceDpi));
            _responsiveLayoutTimer.Stop();
            _responsiveLayoutPending = false;
            _initialLayoutApplied = true;
        }
        finally
        {
            _restoringPlacement = false;
        }
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        SuspendLayout();
        _restoringPlacement = true;
        try
        {
            Bounds = e.SuggestedRectangle;
            base.OnDpiChanged(e);
            UpdateMinimumSizeForCurrentScreen();
            ScheduleResponsiveLayout();
            PerformLayout();
        }
        finally
        {
            _restoringPlacement = false;
            ResumeLayout(true);
            Invalidate(true);
        }
        SchedulePlacementSave();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (!_interactiveResize)
            ScheduleResponsiveLayout();
        if (_lastWindowState == FormWindowState.Maximized && WindowState == FormWindowState.Normal)
            SchedulePlacementSave();
        _lastWindowState = WindowState;
    }

    protected override void OnMove(EventArgs e)
    {
        base.OnMove(e);
        SchedulePlacementSave();
    }

    protected override void OnResizeEnd(EventArgs e)
    {
        base.OnResizeEnd(e);
        _interactiveResize = false;
        ScheduleResponsiveLayout();
        UpdatePresentationRefreshState();
        SchedulePlacementSave();
    }

    protected override void OnResizeBegin(EventArgs e)
    {
        _interactiveResize = true;
        _responsiveLayoutTimer.Stop();
        _responsiveLayoutPending = false;
        _presentationRefreshTimer.Enabled = false;
        base.OnResizeBegin(e);
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        UpdatePresentationRefreshState();
        if (!Visible) SavePlacementNow();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        SavePlacementNow();
        base.OnFormClosing(e);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _responsiveLayoutTimer.Dispose();
        _placementSaveTimer.Dispose();
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
        _state.Font = RemoteDashboardTheme.CreateFont(10F, FontStyle.Bold);
        _state.ForeColor = RemoteDashboardTheme.Success;

        _connectionFeedback.Font = RemoteDashboardTheme.CreateFont(8.5F);
        _connectionFeedback.ForeColor = RemoteDashboardTheme.MutedText;

        _pageTitle.Font = RemoteDashboardTheme.CreateFont(16F, FontStyle.Bold);
        _pageTitle.ForeColor = RemoteDashboardTheme.Text;

        _pageSubtitle.Font = RemoteDashboardTheme.CreateFont(8.5F);
        _pageSubtitle.ForeColor = RemoteDashboardTheme.MutedText;

        _presentationStatus.Font = RemoteDashboardTheme.CreateFont(8.5F);
        _presentationStatus.ForeColor = RemoteDashboardTheme.Info;

        _ruleCount.Font = RemoteDashboardTheme.CreateFont(8.75F);
        _ruleCount.ForeColor = RemoteDashboardTheme.MutedText;

        _detailTitle.Font = RemoteDashboardTheme.CreateFont(9.5F, FontStyle.Bold);
        _detailTitle.ForeColor = RemoteDashboardTheme.Text;

        _emptyList.Text = "暂无演示文稿";
        _emptyList.Font = RemoteDashboardTheme.CreateFont(10F);
        _emptyList.ForeColor = RemoteDashboardTheme.SubtleText;
    }

    private void Build()
    {
        var shell = _shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 1,
            BackColor = RemoteDashboardTheme.Window,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(shell);

        shell.Controls.Add(BuildWorkspace(), 0, 0);
    }

    private static RemoteTextButton CreateNavigationButton(string text, EventHandler click)
    {
        text = Localization.T(text);
        using var measureFont = RemoteDashboardTheme.CreateFont(9F);
        var measuredWidth = TextRenderer.MeasureText(
            text,
            measureFont,
            Size.Empty,
            TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix).Width + 24;
        var button = new RemoteTextButton
        {
            Text = text,
            Kind = RemoteButtonKind.Secondary,
            TextAlign = ContentAlignment.MiddleCenter,
            Width = Math.Max(104, measuredWidth),
            Height = RemoteDashboardTheme.NavigationButtonHeight,
            MinimumSize = new Size(104, RemoteDashboardTheme.NavigationButtonHeight),
            Margin = new Padding(0, 0, RemoteDashboardTheme.NavigationButtonGap, 0),
            Padding = new Padding(12, 0, 12, 0),
            Font = RemoteDashboardTheme.CreateFont(9F),
            CornerRadius = RemoteDashboardTheme.NavigationRadius
        };
        button.Click += click;
        return button;
    }

    private Control BuildWorkspace()
    {
        var workspace = _workspace = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = RemoteDashboardTheme.Window,
            Padding = new Padding(RemoteDashboardTheme.PagePadding),
            Margin = Padding.Empty
        };
        workspace.RowStyles.Add(new RowStyle(SizeType.Absolute, RemoteDashboardTheme.NavigationHeight));
        workspace.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        workspace.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        workspace.Controls.Add(BuildTopNavigation(), 0, 0);
        workspace.Controls.Add(BuildPageHeader(), 0, 1);

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
        workspace.Controls.Add(_contentHost, 0, 2);

        _presentationRefreshTimer.Tick += (_, _) => RefreshPresentationPanel();
        ShowContentPage(false);
        return workspace;
    }

    private Control BuildTopNavigation()
    {
        var navigation = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = false,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        _connectionNav = CreateNavigationButton("远程连接", (_, _) => ShowContentPage(false));
        _presentationNav = CreateNavigationButton("演示文稿", (_, _) => ShowContentPage(true));
        navigation.Controls.Add(_connectionNav);
        navigation.Controls.Add(_presentationNav);

        var navigationArea = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = RemoteDashboardTheme.Window,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        navigation.Dock = DockStyle.Fill;
        navigationArea.Controls.Add(navigation);
        navigationArea.Paint += (_, e) =>
        {
            using var divider = new Pen(RemoteDashboardTheme.Border, 1F);
            e.Graphics.DrawLine(divider, 0, navigationArea.ClientSize.Height - 1, navigationArea.ClientSize.Width, navigationArea.ClientSize.Height - 1);
        };
        return navigationArea;
    }

    private Control BuildPageHeader()
    {
        var header = _pageHeaderLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = RemoteDashboardTheme.Window,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
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
        _pageSubtitle.Text = presentation ? "规则与放映" : "通过手机或浏览器控制演示";

        _presentationTabActive = presentation;
        UpdatePresentationRefreshState();
    }

    private void UpdatePresentationRefreshState()
    {
        _presentationRefreshTimer.Enabled = Visible && _presentationTabActive;
        if (_presentationRefreshTimer.Enabled) RefreshPresentationPanel();
    }

    private void ScheduleResponsiveLayout()
    {
        if (_interactiveResize || IsDisposed || !IsHandleCreated || WindowState == FormWindowState.Minimized) return;
        _responsiveLayoutPending = true;
        _responsiveLayoutTimer.Stop();
        _responsiveLayoutTimer.Start();
    }

    private void ApplyScheduledResponsiveLayout()
    {
        _responsiveLayoutTimer.Stop();
        if (!_responsiveLayoutPending || IsDisposed || ClientSize.Width <= 0 || ClientSize.Height <= 0) return;
        _responsiveLayoutPending = false;
        ApplyResponsiveLayout(RemoteWindowLayoutService.GetLayoutMode(ClientSize, DeviceDpi));
    }

    private void ApplyResponsiveLayout(RemoteLayoutMode mode)
    {
        var changed = _layoutMode != mode;
        _layoutMode = mode;
        var compact = mode == RemoteLayoutMode.Compact;
        var pagePaddingDip = compact ? RemoteDashboardTheme.CompactPagePadding : RemoteDashboardTheme.PagePadding;
        var cardGapDip = compact ? RemoteDashboardTheme.CompactCardGap : RemoteDashboardTheme.CardGap;
        var sectionGapDip = compact ? RemoteDashboardTheme.CompactSectionGap : RemoteDashboardTheme.SectionGap;
        var controlGapDip = compact ? RemoteDashboardTheme.CompactControlGap : RemoteDashboardTheme.ControlGap;

        SuspendLayout();
        try
        {
            int D(int dip) => LogicalToDeviceUnits(dip);
            if (_workspace is not null)
            {
                _workspace.Padding = new Padding(
                    D(14),
                    D(3),
                    D(14),
                    D(compact ? 4 : 6));
                _workspace.RowStyles[0].Height = D(RemoteDashboardTheme.NavigationHeight);
                _workspace.RowStyles[1].Height = D(54);
            }
            if (_pageHeaderLayout is not null)
            {
                _pageHeaderLayout.RowStyles[0].Height = D(32);
                _pageHeaderLayout.RowStyles[1].Height = D(22);
            }
            if (_connectionNav is not null && _presentationNav is not null)
            {
                var connectionWidth = Localization.IsEnglish ? 142 : 108;
                var presentationWidth = Localization.IsEnglish ? 118 : 108;
                _connectionNav.Width = D(connectionWidth);
                _connectionNav.MinimumSize = new Size(D(connectionWidth), D(32));
                _presentationNav.Width = D(presentationWidth);
                _presentationNav.MinimumSize = new Size(D(presentationWidth), D(32));
                _connectionNav.Height = _presentationNav.Height = D(32);
            }
            if (_connectionBody is not null)
            {
                _connectionBody.RowStyles[0].Height = D(45);
                _connectionBody.RowStyles[1].Height = D(10);
                _connectionBody.MinimumSize = new Size(
                    D(compact ? 620 : 656),
                    D(compact ? 320 : 332));
            }
            if (_connectionColumns is not null)
                _connectionColumns.ColumnStyles[1].Width = LogicalToDeviceUnits(cardGapDip);
            if (_browserLayout is not null)
            {
                var rows = new[] { 28, 4, 20, 30, 6, 20, 30, 8, 36, 6, 36, 8 };
                for (var i = 0; i < rows.Length; i++) _browserLayout.RowStyles[i].Height = D(rows[i]);
                _browserLayout.RowStyles[13].Height = D(Localization.IsEnglish ? 116 : 86);
            }
            if (_qrLayout is not null)
            {
                _qrLayout.RowStyles[0].Height = D(28);
                _qrLayout.RowStyles[1].Height = D(6);
                _qrLayout.RowStyles[3].Height = D(6);
                _qrLayout.RowStyles[4].Height = 0;
            }
            if (_presentationRoot is not null)
            {
                _presentationRoot.Padding = new Padding(0, D(8), 0, 0);
                _presentationRoot.RowStyles[0].Height = D(36);
                _presentationRoot.RowStyles[1].Height = D(12);
            }
            if (_presentationSplit is not null)
                _presentationSplit.ColumnStyles[1].Width = D(8);
            if (_presentationListLayout is not null)
            {
                _presentationListLayout.RowStyles[0].Height = D(28);
                _presentationListLayout.RowStyles[2].Height = D(24);
            }
            if (_ruleEditorLayout is not null)
            {
                var rows = new[] { 24, 6, 20, 6, 36, 8, 20, 6, 36 };
                for (var i = 0; i < rows.Length; i++) _ruleEditorLayout.RowStyles[i].Height = D(rows[i]);
            }
            if (_ruleEditorCard is not null && _presentationActionsCard is not null)
            {
                _ruleEditorCard.Height = D(178);
                _presentationActionsCard.Height = D(152);
                _ruleEditorCard.Margin = new Padding(0, 0, 0, D(8));
                _presentationActionsCard.Margin = Padding.Empty;
            }
            if (_presentationCardLayout is not null)
            {
                _presentationCardLayout.RowStyles[0].Height = D(24);
                _presentationCardLayout.RowStyles[1].Height = D(4);
            }

            ConfigureBrowserActions(compact, controlGapDip);
            ConfigurePresentationActions(compact, sectionGapDip);
            _connectionScroll!.AutoScroll = true;
            if (_presentationDetailsViewport is not null)
                _presentationDetailsViewport.AutoScroll = compact;

            if (changed)
            {
                var oldTitleFont = _pageTitle.Font;
                _pageTitle.Font = RemoteDashboardTheme.CreateFont(
                    RemoteWindowLayoutService.GetPageTitleFontSize(mode),
                    FontStyle.Bold);
                oldTitleFont.Dispose();
            }

            _reflowConnection?.Invoke();
            UpdateQrFrameSize();
            UpdatePresentationDetailsBounds();
            PerformLayout();
        }
        finally
        {
            ResumeLayout(true);
            Invalidate(true);
        }
    }

    private void ConfigureBrowserActions(bool compact, int controlGapDip)
    {
        if (_browserActions is null || _copyLinkButton is null || _openBrowserButton is null)
            return;

        var actions = _browserActions;
        actions.SuspendLayout();
        actions.ColumnStyles.Clear();
        actions.RowStyles.Clear();
        actions.ColumnCount = 3;
        actions.RowCount = 1;
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, LogicalToDeviceUnits(controlGapDip)));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        actions.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        actions.SetCellPosition(_copyLinkButton, new TableLayoutPanelCellPosition(0, 0));
        actions.SetCellPosition(_openBrowserButton, new TableLayoutPanelCellPosition(2, 0));
        actions.ResumeLayout(true);
    }

    private void ConfigurePresentationActions(bool compact, int sectionGapDip)
    {
        if (_presentationActionsLayout is null ||
            _openPresentationButton is null ||
            _startFromBeginningButton is null ||
            _startFromCurrentButton is null ||
            _endSlideShowButton is null ||
            _closeActivePresentationButton is null ||
            _presentationActionsCard is null ||
            _presentationCardLayout is null)
            return;

        var actions = _presentationActionsLayout;
        actions.SuspendLayout();
        actions.ColumnStyles.Clear();
        actions.RowStyles.Clear();
        actions.ColumnCount = 2;
        actions.RowCount = 5;
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        actions.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        actions.RowStyles.Add(new RowStyle(SizeType.Absolute, LogicalToDeviceUnits(RemoteDashboardTheme.ControlGap)));
        actions.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        actions.RowStyles.Add(new RowStyle(SizeType.Absolute, LogicalToDeviceUnits(RemoteDashboardTheme.ControlGap)));
        actions.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        SetPlaybackPosition(_openPresentationButton, 0, 0, true, false);
        SetPlaybackPosition(_startFromBeginningButton, 1, 0, false, true);
        SetPlaybackPosition(_startFromCurrentButton, 0, 2, true, false);
        SetPlaybackPosition(_endSlideShowButton, 1, 2, false, true);
        actions.SetCellPosition(_closeActivePresentationButton, new TableLayoutPanelCellPosition(0, 4));
        actions.SetColumnSpan(_closeActivePresentationButton, 2);
        _closeActivePresentationButton.Dock = DockStyle.Fill;
        _closeActivePresentationButton.Margin = Padding.Empty;
        _presentationCardLayout.RowStyles[2].SizeType = SizeType.Percent;
        _presentationCardLayout.RowStyles[2].Height = 100;
        _presentationActionsCard.Height = LogicalToDeviceUnits(152);
        actions.ResumeLayout(true);
    }

    private static void SetPlaybackPosition(
        RemoteTextButton button,
        int column,
        int row,
        bool first,
        bool last)
    {
        if (button.Parent is not TableLayoutPanel layout) return;
        layout.SetCellPosition(button, new TableLayoutPanelCellPosition(column, row));
        button.Dock = DockStyle.Fill;
        button.Margin = new Padding(first ? 0 : 3, 0, last ? 0 : 3, 0);
    }

    private void UpdatePresentationDetailsBounds()
    {
        if (_presentationDetailsViewport is null ||
            _presentationDetailsFlow is null ||
            _ruleEditorCard is null ||
            _presentationActionsCard is null)
            return;

        var scrollbar = _presentationDetailsViewport.AutoScroll
            ? SystemInformation.VerticalScrollBarWidth
            : 0;
        var width = Math.Max(1, _presentationDetailsViewport.ClientSize.Width - scrollbar);
        foreach (var card in new[] { _ruleEditorCard, _presentationActionsCard })
            card.Width = width;
        _presentationDetailsFlow.Width = width;
        _presentationDetailsFlow.Height = _ruleEditorCard.Height + _ruleEditorCard.Margin.Vertical +
                                          _presentationActionsCard.Height + _presentationActionsCard.Margin.Vertical;
    }

    private void RestoreWindowPlacement()
    {
        _restoringPlacement = true;
        try
        {
            var screens = Screen.AllScreens.Select(RemoteScreenDpiProvider.FromScreen).ToArray();
            var preferred = Screen.FromPoint(Cursor.Position).DeviceName;
            var selected = RemoteWindowLayoutService.SelectScreen(
                screens,
                _config.RemoteControl.Window.HasValue ? _config.RemoteControl.Window.ScreenDeviceName : null,
                preferred);
            Location = selected.WorkingArea.Location;
            var nonClient = SizeFromClientSize(Size.Empty);
            var plan = RemoteWindowLayoutService.CreateRestorePlan(
                _config.RemoteControl.Window,
                screens,
                preferred,
                nonClient);
            MinimumSize = SizeFromClientSize(RemoteWindowLayoutService.GetMinimumClientSizePhysical(plan.Screen));
            Bounds = plan.WindowBoundsPhysical;
            ClientSize = plan.ClientSizePhysical;
            Bounds = RemoteWindowLayoutService.ClampToWorkingArea(Bounds, plan.Screen.WorkingArea);
            _placementLoaded = true;
            _lastWindowState = FormWindowState.Normal;
        }
        finally
        {
            _restoringPlacement = false;
        }
    }

    private void UpdateMinimumSizeForCurrentScreen()
    {
        if (!IsHandleCreated) return;
        var screen = RemoteScreenDpiProvider.FromScreen(Screen.FromControl(this));
        MinimumSize = SizeFromClientSize(RemoteWindowLayoutService.GetMinimumClientSizePhysical(screen));
    }

    private void SchedulePlacementSave()
    {
        if (!_placementLoaded || _restoringPlacement || _savingPlacement || !Visible ||
            !RemoteWindowLayoutService.CanSave(WindowState))
            return;
        _placementSaveTimer.Stop();
        _placementSaveTimer.Start();
    }

    private void SavePlacementNow()
    {
        _placementSaveTimer.Stop();
        if (!_placementLoaded || _restoringPlacement || _savingPlacement || !IsHandleCreated ||
            !RemoteWindowLayoutService.CanSave(WindowState))
            return;

        _savingPlacement = true;
        try
        {
            var maximized = WindowState == FormWindowState.Maximized;
            var normalBounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
            var screen = RemoteScreenDpiProvider.FromScreen(Screen.FromRectangle(normalBounds));
            var nonClient = SizeFromClientSize(Size.Empty);
            var normalClient = WindowState == FormWindowState.Normal
                ? ClientSize
                : new Size(
                    Math.Max(1, normalBounds.Width - nonClient.Width),
                    Math.Max(1, normalBounds.Height - nonClient.Height));
            _config.RemoteControl.Window = RemoteWindowLayoutService.CapturePlacement(
                normalBounds,
                normalClient,
                screen,
                maximized);
            _saveConfig(_config);
        }
        finally
        {
            _savingPlacement = false;
        }
    }

    private Control BuildConnectionPage()
    {
        var scroll = _connectionScroll = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = RemoteDashboardTheme.Window,
            Margin = Padding.Empty
        };

        var body = _connectionBody = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = false,
            Height = 360,
            MinimumSize = new Size(660, 360),
            ColumnCount = 1,
            RowCount = 3,
            BackColor = RemoteDashboardTheme.Window,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, RemoteDashboardTheme.CardGap));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        body.Controls.Add(BuildServiceCard(), 0, 0);
        body.Controls.Add(BuildConnectionColumns(), 0, 2);
        scroll.Controls.Add(body);

        void Reflow()
        {
            var scrollbar = scroll.VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0;
            body.Width = Math.Max(body.MinimumSize.Width, scroll.ClientSize.Width - scrollbar);
            body.Height = Math.Max(body.MinimumSize.Height, scroll.ClientSize.Height);
        }

        _reflowConnection = Reflow;
        scroll.Resize += (_, _) => Reflow();
        scroll.HandleCreated += (_, _) => BeginInvoke((MethodInvoker)Reflow);
        return scroll;
    }

    private Control BuildServiceCard()
    {
        var card = NewSurface(new Padding(10));
        card.AccessibleName = "服务状态卡";
        card.Dock = DockStyle.Fill;
        card.Margin = Padding.Empty;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        _state.Dock = DockStyle.Fill;
        _state.Margin = Padding.Empty;
        _state.Padding = new Padding(4, 0, 0, 0);
        layout.Controls.Add(_state, 0, 0);

        var restart = CreateActionButton("重启", (_, _) => RestartService(), RemoteButtonKind.Secondary, 58);
        restart.Dock = DockStyle.Fill;
        restart.Margin = new Padding(4, 0, 4, 0);
        layout.Controls.Add(restart, 1, 0);

        _serviceToggle = CreateActionButton("停止服务", (_, _) => ToggleService(), RemoteButtonKind.DangerOutline, 82);
        _serviceToggle.Dock = DockStyle.Fill;
        _serviceToggle.Margin = new Padding(4, 0, 4, 0);

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(
            SizeType.Absolute,
            restart.MinimumSize.Width + restart.Margin.Horizontal));
        layout.ColumnStyles.Add(new ColumnStyle(
            SizeType.Absolute,
            _serviceToggle.MinimumSize.Width + _serviceToggle.Margin.Horizontal));
        layout.Controls.Add(_serviceToggle, 2, 0);

        card.Controls.Add(layout);
        return card;
    }

    private Control BuildConnectionColumns()
    {
        var columns = _connectionColumns = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = RemoteDashboardTheme.Window,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 41.5F));
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, RemoteDashboardTheme.CardGap));
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58.5F));
        columns.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        columns.Controls.Add(BuildQrCard(), 0, 0);
        columns.Controls.Add(BuildBrowserCard(), 2, 0);
        return columns;
    }

    private Control BuildQrCard()
    {
        var card = NewSurface(new Padding(8));
        card.AccessibleName = "二维码卡";
        card.Dock = DockStyle.Fill;
        card.Margin = Padding.Empty;

        var layout = _qrLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 6));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 6));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 0));
        layout.Controls.Add(CreateSectionTitle("手机扫码连接"), 0, 0);

        var qrCenter = _qrCenter = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        var qrFrame = _qrFrame = NewSurface(new Padding(8));
        qrFrame.Size = new Size(212, 212);
        qrFrame.Anchor = AnchorStyles.None;
        qrFrame.BorderColor = RemoteDashboardTheme.BorderStrong;
        qrFrame.Controls.Add(_qr);
        qrCenter.Controls.Add(qrFrame, 0, 0);
        qrCenter.Resize += (_, _) => UpdateQrFrameSize();
        layout.Controls.Add(qrCenter, 0, 2);

        card.Controls.Add(layout);
        return card;
    }

    private void UpdateQrFrameSize()
    {
        if (_qrCenter is null || _qrFrame is null || _qrCenter.ClientSize.Width <= 0 || _qrCenter.ClientSize.Height <= 0)
            return;

        var available = Math.Max(
            1,
            Math.Min(
                _qrCenter.ClientSize.Width - _qrCenter.Padding.Horizontal - LogicalToDeviceUnits(8),
                _qrCenter.ClientSize.Height - _qrCenter.Padding.Vertical - LogicalToDeviceUnits(4)));
        var maximum = LogicalToDeviceUnits(214);
        var side = Math.Min(maximum, available);
        _qrFrame.Size = new Size(side, side);
    }

    private Control BuildBrowserCard()
    {
        var card = NewSurface(new Padding(12));
        card.AccessibleName = "浏览器访问卡";
        card.Dock = DockStyle.Fill;
        card.Margin = Padding.Empty;

        var layout = _browserLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 15,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 4));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 6));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 8));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 6));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 8));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 0));

        layout.Controls.Add(CreateSectionTitle("浏览器访问"), 0, 0);
        layout.Controls.Add(CreateFieldLabel("本机 IP"), 0, 2);

        var copyAddress = CreateActionButton(
            "复制",
            (_, _) => CopyText(_address.SelectedAddress, "已复制。"),
            RemoteButtonKind.Secondary,
            56);
        layout.Controls.Add(CreateFieldRow(_address, copyAddress), 0, 3);

        layout.Controls.Add(CreateFieldLabel("访问链接"), 0, 5);
        var urlHost = CreateInputHost(_url, new Padding(8, 5, 8, 5), 30);
        var copyUrlInline = CreateActionButton(
            "复制",
            (_, _) => CopyText(CurrentUrl(), "已复制。"),
            RemoteButtonKind.Secondary,
            56);
        layout.Controls.Add(CreateFieldRow(urlHost, copyUrlInline), 0, 6);

        var actions = _browserActions = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, RemoteDashboardTheme.ControlGap));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        var copy = _copyLinkButton = CreateActionButton(
            "复制链接",
            (_, _) => CopyText(CurrentUrl(), "已复制。"),
            RemoteButtonKind.Primary);
        copy.Dock = DockStyle.Fill;
        copy.Margin = Padding.Empty;
        actions.Controls.Add(copy, 0, 0);

        var open = _openBrowserButton = CreateActionButton(
            "在浏览器中打开",
            (_, _) => OpenCurrentUrl(),
            RemoteButtonKind.Secondary);
        open.Dock = DockStyle.Fill;
        open.Margin = Padding.Empty;
        actions.Controls.Add(open, 2, 0);
        layout.Controls.Add(actions, 0, 8);

        var firewall = _firewallButton = CreateActionButton(
            "允许远程控制",
            (_, _) => CopyText(BuildFirewallCommand(), "命令已复制。"),
            RemoteButtonKind.Secondary);
        firewall.Dock = DockStyle.Fill;
        firewall.Margin = Padding.Empty;
        layout.Controls.Add(firewall, 0, 10);

        var feedback = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0, 8, 0, 0) };
        feedback.Dock = DockStyle.Fill;
        _connectionFeedback.Text = "手机与电脑需连接同一局域网；也可通过手机热点或电脑热点创建局域网进行控制。";
        feedback.Controls.Add(_connectionFeedback);
        layout.Controls.Add(feedback, 0, 13);

        card.Controls.Add(layout);
        return card;
    }

    private Control BuildPresentationPage()
    {
        var root = _presentationRoot = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = RemoteDashboardTheme.Window,
            Margin = Padding.Empty,
            Padding = new Padding(0, 8, 0, 0)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 12));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(BuildPresentationToolbar(), 0, 0);
        root.Controls.Add(BuildPresentationWorkspace(), 0, 2);
        return root;
    }

    private Control BuildPresentationToolbar()
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
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

        var add = CreateActionButton("添加", (_, _) => AddPresentationRules(), RemoteButtonKind.Primary, 72);
        _deleteRuleButton = CreateActionButton("删除", (_, _) => DeleteSelectedRule(), RemoteButtonKind.Secondary, 78);
        var refresh = CreateActionButton("刷新", (_, _) => RefreshPresentationPanel(), RemoteButtonKind.Secondary, 88);
        _clearRulesButton = CreateActionButton("清空列表", (_, _) => ClearPresentationRules(), RemoteButtonKind.Secondary, 104);
        foreach (var button in new[] { add, _deleteRuleButton, refresh, _clearRulesButton })
        {
            button.Height = 36;
            button.MinimumSize = new Size(button.MinimumSize.Width, 36);
            button.Padding = new Padding(12, 0, 12, 0);
            button.Margin = new Padding(0, 0, RemoteDashboardTheme.ControlGap, 0);
            button.Font = RemoteDashboardTheme.CreateFont(10F);
        }

        actions.Controls.Add(add);
        actions.Controls.Add(_deleteRuleButton);
        actions.Controls.Add(refresh);
        actions.Controls.Add(_clearRulesButton);
        card.Controls.Add(actions);
        return card;
    }

    private Control BuildPresentationStatus()
    {
        var card = NewSurface(new Padding(10, 4, 10, 4));
        card.AccessibleName = "演示状态提示";
        card.Dock = DockStyle.Fill;
        card.Margin = Padding.Empty;
        card.AutoSize = false;
        card.MinimumSize = new Size(0, 30);
        card.FillColor = RemoteDashboardTheme.InfoSoft;
        card.BorderColor = Color.FromArgb(191, 214, 248);
        _presentationStatus.Text = "请选择演示文稿。";
        _presentationStatus.AutoSize = false;
        _presentationStatus.Dock = DockStyle.Fill;
        _presentationStatus.AutoEllipsis = false;
        card.Resize += (_, _) =>
        {
            _presentationStatus.MaximumSize = new Size(Math.Max(120, card.ClientSize.Width - card.Padding.Horizontal), 0);
            card.PerformLayout();
        };
        card.Controls.Add(_presentationStatus);
        return card;
    }

    private Control BuildPresentationWorkspace()
    {
        var split = _presentationSplit = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = RemoteDashboardTheme.Window,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 8));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56));
        split.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        split.Controls.Add(BuildPresentationList(), 0, 0);
        split.Controls.Add(BuildPresentationDetails(), 2, 0);
        return split;
    }

    private Control BuildPresentationList()
    {
        var card = NewSurface(new Padding(10));
        card.AccessibleName = "演示文稿列表卡";
        card.Dock = DockStyle.Fill;
        card.Margin = Padding.Empty;

        var layout = _presentationListLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        layout.Controls.Add(CreateSectionTitle("演示文稿列表"), 0, 0);

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

        _ruleCount.Text = "0 个项目";
        layout.Controls.Add(_ruleCount, 0, 2);
        card.Controls.Add(layout);
        return card;
    }

    private Control BuildPresentationDetails()
    {
        var viewport = _presentationDetailsViewport = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = false,
            BackColor = RemoteDashboardTheme.Window,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        var flow = _presentationDetailsFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = false,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = false,
            BackColor = RemoteDashboardTheme.Window,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        flow.Controls.Add(BuildRuleEditor());
        flow.Controls.Add(BuildPresentationActions());
        viewport.Controls.Add(flow);
        viewport.Resize += (_, _) => UpdatePresentationDetailsBounds();
        return viewport;
    }

    private Control BuildRuleEditor()
    {
        var card = _ruleEditorCard = NewSurface(new Padding(8));
        card.AccessibleName = "规则编辑卡";
        card.Dock = DockStyle.None;
        card.Height = 178;
        card.Margin = new Padding(0, 0, 0, RemoteDashboardTheme.SectionGap);

        var layout = _ruleEditorLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 9,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 6));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 6));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 8));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 6));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

        _detailTitle.Text = "未选择演示文稿";
        layout.Controls.Add(_detailTitle, 0, 0);
        layout.Controls.Add(CreateFieldLabel("路径"), 0, 2);

        var pathHost = CreateInputHost(_rulePath, new Padding(8, 7, 8, 7), 36);
        pathHost.Margin = Padding.Empty;
        _browsePathButton = CreateActionButton("浏览", (_, _) => ShowSelectedPath(), RemoteButtonKind.Secondary, 58);
        layout.Controls.Add(CreateFieldRow(pathHost, _browsePathButton), 0, 4);
        layout.Controls.Add(CreateFieldLabel("时长与规则"), 0, 6);

        var controls = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 7,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, RemoteDashboardTheme.ControlGap));

        var durationHost = CreateInputHost(_ruleDuration, new Padding(6, 7, 6, 7), 36);
        durationHost.Dock = DockStyle.Fill;
        durationHost.MinimumSize = new Size(56, 36);
        durationHost.Margin = Padding.Empty;
        controls.Controls.Add(durationHost, 0, 0);

        _ruleEnabledButton = CreateActionButton("启用规则", (_, _) => ToggleSelectedRuleEnabled(), RemoteButtonKind.Secondary, 84);
        _ruleEnabledButton.Padding = new Padding(6, 0, 6, 0);
        _ruleEnabledButton.Dock = DockStyle.Fill;
        _ruleEnabledButton.Margin = Padding.Empty;
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, _ruleEnabledButton.MinimumSize.Width));
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, RemoteDashboardTheme.ControlGap));
        controls.Controls.Add(_ruleEnabledButton, 2, 0);

        _moreActionsButton = CreateActionButton("规则设置", (_, _) => ShowMoreActions(_moreActionsButton), RemoteButtonKind.Secondary, 90);
        _moreActionsButton.Dock = DockStyle.Fill;
        _moreActionsButton.Margin = Padding.Empty;
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, _moreActionsButton.MinimumSize.Width));
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, RemoteDashboardTheme.ControlGap));
        controls.Controls.Add(_moreActionsButton, 4, 0);

        _saveDurationButton = CreateActionButton("保存", (_, _) => SaveSelectedDuration(), RemoteButtonKind.Primary, 58);
        _saveDurationButton.Dock = DockStyle.Fill;
        _saveDurationButton.Margin = Padding.Empty;
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, _saveDurationButton.MinimumSize.Width));
        controls.Controls.Add(_saveDurationButton, 6, 0);
        layout.Controls.Add(controls, 0, 8);

        _ruleDuration.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            SaveSelectedDuration();
        };
        _ruleDuration.TextChanged += (_, _) =>
        {
            if (_updatingRuleEditor) return;
            _durationDirty = true;
            if (_saveDurationButton is not null) _saveDurationButton.Enabled = _selectedRule is not null;
        };

        card.Controls.Add(layout);
        return card;
    }

    private Control BuildPresentationActions()
    {
        var card = _presentationActionsCard = NewSurface(new Padding(6));
        card.AccessibleName = "放映卡";
        card.Dock = DockStyle.None;
        card.Height = 152;
        card.Margin = new Padding(0, 0, 0, RemoteDashboardTheme.SectionGap);

        var layout = _presentationCardLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 4));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(CreateSectionTitle("放映控制"), 0, 0);

        var actions = _presentationActionsLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        actions.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        actions.RowStyles.Add(new RowStyle(SizeType.Absolute, RemoteDashboardTheme.ControlGap));
        actions.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        actions.RowStyles.Add(new RowStyle(SizeType.Absolute, RemoteDashboardTheme.ControlGap));
        actions.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        _openPresentationButton = CreateActionButton(
            "打开演示文稿",
            (_, _) => SendPresentationCommand(PresentationCommandKind.OpenPresentation),
            RemoteButtonKind.Secondary);
        _startFromBeginningButton = CreateActionButton(
            "从头放映",
            (_, _) => SendPresentationCommand(PresentationCommandKind.StartFromBeginning),
            RemoteButtonKind.Secondary);
        _startFromCurrentButton = CreateActionButton(
            "当前页放映",
            (_, _) => SendPresentationCommand(PresentationCommandKind.StartFromCurrent),
            RemoteButtonKind.Secondary);
        var end = _endSlideShowButton = CreateActionButton(
            "结束放映",
            (_, _) => SendPresentationCommand(PresentationCommandKind.EndShow),
            RemoteButtonKind.DangerOutline);
        _closeActivePresentationButton = CreateActionButton(
            "关闭当前文档",
            (_, _) => SendPresentationCommand(PresentationCommandKind.CloseActivePresentation),
            RemoteButtonKind.DangerOutline);

        actions.Controls.Add(_openPresentationButton, 0, 0);
        actions.Controls.Add(_startFromBeginningButton, 1, 0);
        actions.Controls.Add(_startFromCurrentButton, 0, 2);
        actions.Controls.Add(end, 1, 2);
        actions.Controls.Add(_closeActivePresentationButton, 0, 4);
        actions.SetColumnSpan(_closeActivePresentationButton, 2);
        SetPlaybackPosition(_openPresentationButton, 0, 0, true, false);
        SetPlaybackPosition(_startFromBeginningButton, 1, 0, false, true);
        SetPlaybackPosition(_startFromCurrentButton, 0, 2, true, false);
        SetPlaybackPosition(end, 1, 2, false, true);
        _closeActivePresentationButton.Dock = DockStyle.Fill;
        _closeActivePresentationButton.Margin = Padding.Empty;
        layout.Controls.Add(actions, 0, 2);

        card.Controls.Add(layout);
        return card;
    }

    private Control BuildDangerActions()
    {
        var card = _dangerActionsCard = NewSurface(new Padding(8));
        card.AccessibleName = "退出软件卡";
        card.Dock = DockStyle.None;
        card.Height = 50;
        card.Margin = Padding.Empty;
        card.BorderColor = RemoteDashboardTheme.Border;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var copy = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        copy.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
        copy.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
        var title = CreateSectionTitle("退出软件");
        title.ForeColor = RemoteDashboardTheme.Text;
        copy.Controls.Add(title, 0, 0);
        copy.Controls.Add(new Label
        {
            Text = Localization.T("点击退出并关闭程序"),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = RemoteDashboardTheme.MutedText,
            Font = RemoteDashboardTheme.CreateFont(8F),
            AutoEllipsis = false,
            Margin = Padding.Empty,
            UseCompatibleTextRendering = false
        }, 0, 1);
        layout.Controls.Add(copy, 0, 0);

        var force = CreateActionButton(
            Localization.IsEnglish ? "Quit" : "退出软件",
            (_, _) => ConfirmForceQuit(),
            RemoteButtonKind.DangerOutline,
            112);
        force.Dock = DockStyle.Fill;
        force.Margin = Padding.Empty;
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, RemoteDashboardTheme.ControlGap));
        layout.ColumnStyles.Add(new ColumnStyle(
            SizeType.Absolute,
            Math.Max(86, force.MinimumSize.Width + 8)));
        layout.Controls.Add(force, 2, 0);
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
        button.Margin = new Padding(first ? 0 : 3, 0, last ? 0 : 3, 0);
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
        text = Localization.T(text);
        var label = new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = RemoteDashboardTheme.CreateFont(9.5F, FontStyle.Bold),
            ForeColor = RemoteDashboardTheme.Text,
            AutoEllipsis = false,
            Margin = Padding.Empty,
            UseCompatibleTextRendering = false
        };
        label.MinimumSize = new Size(0, 22);
        return label;
    }

    private static Label CreateFieldLabel(
        string text,
        ContentAlignment alignment = ContentAlignment.MiddleLeft)
    {
        text = Localization.T(text);
        var font = RemoteDashboardTheme.CreateFont(8F, FontStyle.Bold);
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
        label.MinimumSize = new Size(0, 16);
        return label;
    }

    private static RemoteTextButton CreateActionButton(
        string text,
        EventHandler click,
        RemoteButtonKind kind,
        int minimumWidth = 0)
    {
        text = Localization.T(text);
        using var measureFont = RemoteDashboardTheme.CreateFont(8.5F);
        var measured = TextRenderer.MeasureText(
            text,
            measureFont,
            Size.Empty,
            TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix).Width + 20;

        var button = new RemoteTextButton
        {
            Text = text,
            Kind = kind,
            Width = Math.Max(minimumWidth, measured),
            Height = RemoteDashboardTheme.ButtonHeight,
            MinimumSize = new Size(Math.Max(minimumWidth, measured), RemoteDashboardTheme.ButtonHeight),
            Margin = new Padding(0, 0, 8, 0),
            Font = RemoteDashboardTheme.CreateFont(8.5F)
        };
        button.Click += click;
        return button;
    }

    private static RemoteSurface CreateInputHost(
        Control control,
        Padding padding,
        int height = RemoteDashboardTheme.InputHeight)
    {
        var readOnly = control is TextBox { ReadOnly: true };
        var host = NewSurface(padding);
        host.Dock = DockStyle.Fill;
        host.Height = height;
        host.MinimumSize = new Size(0, height);
        host.CornerRadius = RemoteDashboardTheme.ControlRadius;

        control.Dock = DockStyle.Fill;
        control.Margin = Padding.Empty;
        control.Font = RemoteDashboardTheme.CreateFont(9.5F);
        host.Controls.Add(control);

        void ApplyState()
        {
            var fill = !control.Enabled
                ? RemoteDashboardTheme.DisabledField
                : readOnly
                    ? RemoteDashboardTheme.ReadOnlyField
                    : RemoteDashboardTheme.Field;
            host.FillColor = fill;
            host.BorderColor = control.Enabled
                ? RemoteDashboardTheme.Border
                : RemoteDashboardTheme.DisabledBorder;
            control.BackColor = fill;
            control.ForeColor = !control.Enabled || readOnly
                ? RemoteDashboardTheme.MutedText
                : RemoteDashboardTheme.Text;
            host.Invalidate();
        }

        control.EnabledChanged += (_, _) => ApplyState();
        ApplyState();
        return host;
    }

    private static TableLayoutPanel CreateFieldRow(Control field, RemoteTextButton button)
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, RemoteDashboardTheme.ControlGap));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Math.Max(56, button.MinimumSize.Width)));
        row.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        field.Dock = DockStyle.Fill;
        field.Margin = Padding.Empty;
        button.Dock = DockStyle.Fill;
        button.Margin = Padding.Empty;
        row.Controls.Add(field, 0, 0);
        row.Controls.Add(button, 2, 0);
        return row;
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
                Mode = _config.Timer.Mode,
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

    private void ClearPresentationRules()
    {
        if (_config.Rules.Count == 0) return;

        _config.Rules.Clear();
        _selectedRule = null;
        _selectedPresentationId = null;
        _selectedPresentationPath = null;
        SaveRulesImmediately();
        RefreshPresentationPanel();
    }

    private void RefreshPresentationPanel()
    {
        if (IsDisposed) return;

        var state = _presentationCommands?.GetState() ??
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

        _slideShowRunning = state.IsSlideShowRunning;
        if (_closeActivePresentationButton is not null)
            _closeActivePresentationButton.Enabled =
                state.OpenPresentationCount > 0 || state.Presentations.Any(item => item.IsOpen);
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
        _ruleCount.Text = $"{items.Count} 个项目";
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
            1,
            _ruleList.ClientSize.Width -
            _ruleList.Padding.Horizontal -
            scrollbar -
            4);

        foreach (Control control in _ruleList.Controls)
            control.Width = width;
        _ruleList.HideHorizontalScrollBar();
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
        var selectedName = !string.IsNullOrWhiteSpace(rule?.FileName)
            ? rule.FileName
            :
                           (!string.IsNullOrWhiteSpace(_selectedPresentationPath)
                               ? Path.GetFileName(_selectedPresentationPath)
                               : null);

        SetTextIfChanged(_detailTitle, selectedName ?? "未选择演示文稿");
        SetTextIfChanged(
            _rulePath,
            rule?.FilePath ?? _selectedPresentationPath ?? "请选择演示文稿");
        _toolTip.SetToolTip(
            _rulePath,
            rule?.FilePath ?? _selectedPresentationPath ?? string.Empty);
        SetTextIfChanged(_ruleDuration, rule?.Duration ?? string.Empty);
        _durationDirty = false;
        SetRuleButton(rule?.Enabled == true);
        _ruleDuration.Enabled = hasRule;
        if (_browsePathButton is not null) _browsePathButton.Enabled = hasSelection;
        if (_ruleEnabledButton is not null) _ruleEnabledButton.Enabled = hasRule;
        _updatingRuleEditor = false;

        if (_deleteRuleButton is not null) _deleteRuleButton.Enabled = hasRule;
        if (_clearRulesButton is not null) _clearRulesButton.Enabled = _config.Rules.Count > 0;
        if (_saveDurationButton is not null) _saveDurationButton.Enabled = false;
        if (_moreActionsButton is not null) _moreActionsButton.Enabled = hasSelection;
        if (_openPresentationButton is not null) _openPresentationButton.Enabled = hasSelection;
        if (_startFromBeginningButton is not null) _startFromBeginningButton.Enabled = hasSelection;
        if (_startFromCurrentButton is not null) _startFromCurrentButton.Enabled = hasSelection;
        if (_endSlideShowButton is not null)
        {
            _endSlideShowButton.Enabled = true;
            _endSlideShowButton.Kind = RemoteButtonKind.DangerOutline;
        }
    }

    private void SetRuleButton(bool enabled)
    {
        if (_ruleEnabledButton is null) return;
        var text = Localization.T(enabled ? "禁用规则" : "启用规则");
        var kind = enabled
            ? RemoteButtonKind.Secondary
            : RemoteButtonKind.Primary;
        if (_ruleEnabledButton.Text != text) _ruleEnabledButton.Text = text;
        if (_ruleEnabledButton.Kind != kind) _ruleEnabledButton.Kind = kind;
    }

    private static void SetTextIfChanged(Control control, string text)
    {
        var localized = Localization.T(text);
        if (!string.Equals(control.Text, localized, StringComparison.Ordinal))
            control.Text = localized;
    }

    private void ToggleSelectedRuleEnabled()
    {
        if (_updatingRuleEditor || _selectedRule is null) return;
        _selectedRule.Enabled = !_selectedRule.Enabled;
        SetRuleButton(_selectedRule.Enabled);
        SaveRulesImmediately();
        SetPresentationFeedback(
            _selectedRule.Enabled ? "规则已启用。" : "规则已禁用。",
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
            "关闭最后打开的文稿",
            null,
            (_, _) => SendPresentationCommand(PresentationCommandKind.CloseCurrentPresentation));

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
            if (_saveDurationButton is not null) _saveDurationButton.Enabled = false;
            return;
        }

        _selectedRule.Duration = duration;
        _ruleDuration.Text = duration;
        _durationDirty = false;
        if (_saveDurationButton is not null) _saveDurationButton.Enabled = false;
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

    private void SendPresentationCommand(PresentationCommandKind command)
    {
        if (_presentationCommands is null)
        {
            SetPresentationFeedback("PowerPoint 不可用。", FeedbackKind.Warning);
            return;
        }

        var needsSelection = command is
            PresentationCommandKind.OpenPresentation or
            PresentationCommandKind.StartFromBeginning or
            PresentationCommandKind.StartFromCurrent;

        if (needsSelection &&
            string.IsNullOrWhiteSpace(_selectedPresentationId))
        {
            SetPresentationFeedback("请先选择。", FeedbackKind.Warning);
            return;
        }

        var result = _presentationCommands.Execute(new PresentationCommand(
            command,
            needsSelection ? _selectedPresentationId : null));
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
        if (_presentationCommands is null)
        {
            SetPresentationFeedback("PowerPoint 不可用。", FeedbackKind.Warning);
            return;
        }

        var result = _presentationCommands.Queue(new PresentationCommand(
            PresentationCommandKind.ForceQuitAll,
            Confirmed: true));
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
        _saveConfig(_config);
        _remoteControl.Restart();
        RefreshState();
    }

    private void RefreshState()
    {
        if (_config.RemoteControl.Enabled && !_remoteControl.IsRunning)
            _remoteControl.Start();

        var previousAddress = _address.SelectedAddress;
        _address.SetAddresses(
            _networkAddressService.GetRemoteAccessAddresses().Select(item => item.Address),
            previousAddress);

        var running = _remoteControl.IsRunning;
        _state.Text = running ? "运行中" : "已停止";
        _state.ForeColor = running
            ? RemoteDashboardTheme.Success
            : RemoteDashboardTheme.Danger;

        if (_serviceToggle is not null)
        {
            _serviceToggle.Text = running ? "停止服务" : "启动服务";
            _serviceToggle.Kind = running
                ? RemoteButtonKind.DangerOutline
                : RemoteButtonKind.Primary;
        }

        UpdateUrlAndQr();
    }

    private void UpdateUrlAndQr()
    {
        var url = CurrentUrl();
        if (string.IsNullOrWhiteSpace(url))
        {
            _url.Text = "未检测到可供手机访问的局域网地址";
            _toolTip.SetToolTip(_url, "请让手机与电脑连接同一 Wi-Fi 或局域网后刷新。");
            _qr.Image?.Dispose();
            _qr.Image = null;
            if (_copyLinkButton is not null) _copyLinkButton.Enabled = false;
            if (_openBrowserButton is not null) _openBrowserButton.Enabled = false;
            return;
        }
        if (_copyLinkButton is not null) _copyLinkButton.Enabled = true;
        if (_openBrowserButton is not null) _openBrowserButton.Enabled = true;
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
        if (string.IsNullOrWhiteSpace(address)) return "";

        var port = _remoteControl.CurrentPort > 0
            ? _remoteControl.CurrentPort
            : Math.Clamp(_config.RemoteControl.Port, 1, 65535);

        return $"http://{address}:{port}/?token={_config.RemoteControl.Token}";
    }

    private string BuildFirewallCommand()
    {
        var port = _remoteControl.CurrentPort > 0
            ? _remoteControl.CurrentPort
            : Math.Clamp(_config.RemoteControl.Port, 1, 65535);
        var exe = Path.Combine(
            AppContext.BaseDirectory,
            "FlyPPTTimer.exe");

        return $"netsh advfirewall firewall add rule name=\"FlyPPTTimer Remote {port}\" dir=in action=allow program=\"{exe}\" protocol=TCP localport={port}";
    }

    private void CopyText(
        string text,
        string successMessage,
        bool presentationFeedback = false)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            _connectionFeedback.Text = "未检测到可供手机访问的局域网地址。";
            _connectionFeedback.ForeColor = RemoteDashboardTheme.Warning;
            return;
        }
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
        var url = CurrentUrl();
        if (string.IsNullOrWhiteSpace(url))
        {
            _connectionFeedback.Text = "请先让手机与电脑连接同一局域网。";
            _connectionFeedback.ForeColor = RemoteDashboardTheme.Warning;
            return;
        }
        try
        {
            Process.Start(
                new ProcessStartInfo(url)
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
            Text = "确认退出软件",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = RemoteDashboardTheme.CreateFont(12.5F, FontStyle.Bold),
            ForeColor = RemoteDashboardTheme.Danger,
            UseCompatibleTextRendering = false
        }, 0, 0);

        root.Controls.Add(new Label
        {
            Text = "将强制关闭全部 PowerPoint/WPS 演示进程，未保存内容会丢失。",
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
            Text = "退出软件",
            DialogResult = DialogResult.OK,
            Width = 132,
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
