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
        TextAlign = ContentAlignment.MiddleLeft,
        Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold)
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
    private readonly FlowLayoutPanel _ruleList = new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = true,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        BackColor = ModernTheme.ControlFill,
        Padding = new Padding(8)
    };
    private readonly Label _presentationStatus = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = ModernTheme.MutedText };
    private readonly TextBox _ruleDuration = new() { BorderStyle = BorderStyle.None };
    private readonly CheckBox _ruleEnabled = new() { Text = "启用" };
    private readonly Label _rulePath = new() { Dock = DockStyle.Fill, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft };
    private FileRule? _selectedRule;
    private string? _selectedPresentationId;
    private string? _selectedPresentationPath;
    private bool _updatingRuleEditor;
    private bool _durationDirty;
    private readonly Dictionary<string, PresentationRuleRow> _ruleRows = new(StringComparer.OrdinalIgnoreCase);
    private TableLayoutPanel? _presentationLayout;
    private Control? _ruleEditorCard;
    private Control? _presentationActions;
    private Button? _toggleEditorButton;
    private Button? _toggleActionsButton;
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
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = false;
        ClientSize = new Size(900, 660);
        MinimumSize = new Size(720, 560);
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
            RowCount = 4,
            Padding = new Padding(18),
            BackColor = ModernTheme.Surface
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        Controls.Add(root);

        root.Controls.Add(Header(), 0, 0);
        root.Controls.Add(StatusStrip(), 0, 1);
        root.Controls.Add(BuildContentTabs(), 0, 2);
        root.Controls.Add(BottomPanel(), 0, 3);
    }

    private Control BuildContentTabs()
    {
        var tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            DrawMode = TabDrawMode.OwnerDrawFixed,
            ItemSize = new Size(154, 42),
            SizeMode = TabSizeMode.Fixed,
            Padding = new Point(10, 4)
        };
        ModernTheme.StyleTabs(tabs);
        var connection = new TabPage("远程连接") { BackColor = ModernTheme.Surface, Padding = new Padding(0, 10, 0, 0) };
        connection.Controls.Add(ConnectionPanel());
        var presentation = new TabPage("演示文稿") { BackColor = ModernTheme.Surface, Padding = new Padding(0, 10, 0, 0) };
        presentation.Controls.Add(PresentationPanel());
        tabs.TabPages.Add(connection);
        tabs.TabPages.Add(presentation);
        tabs.SelectedIndexChanged += (_, _) =>
        {
            _presentationTabActive = tabs.SelectedTab == presentation;
            UpdatePresentationRefreshState();
        };
        _presentationRefreshTimer.Tick += (_, _) => RefreshPresentationPanel();
        return tabs;
    }

    private Control Header()
    {
        var panel = Card(new Padding(24, 16, 24, 16));
        panel.BackColor = ModernTheme.HeaderFill;
        panel.ColumnCount = 2;
        panel.RowCount = 1;
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 142));

        var titleStack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            BackColor = ModernTheme.HeaderFill
        };
        titleStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        titleStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        titleStack.Controls.Add(new Label
        {
            Text = "手机遥控",
            Dock = DockStyle.Fill,
            Font = new Font(Font.FontFamily, 21F, FontStyle.Bold),
            ForeColor = ModernTheme.Text,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = Padding.Empty
        }, 0, 0);
        _hint.Text = "同一网络下扫码即可控制开始、暂停、重置和显示状态。";
        titleStack.Controls.Add(_hint, 0, 1);
        panel.Controls.Add(titleStack, 0, 0);

        _toggle.Text = "关闭服务";
        _toggle.Width = 124;
        _toggle.Height = 48;
        _toggle.Click += (_, _) => ToggleService();
        ModernTheme.StyleRounded(_toggle, ModernTheme.ButtonRadius);
        panel.Controls.Add(Center(_toggle, ModernTheme.HeaderFill), 1, 0);
        return panel;
    }

    private Control StatusStrip()
    {
        var panel = Card(new Padding(24, 12, 24, 12));
        panel.ColumnCount = 3;
        panel.RowCount = 1;
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));

        panel.Controls.Add(_state, 0, 0);
        panel.Controls.Add(PortPanel(), 1, 0);
        panel.Controls.Add(FillButton("重启服务", (_, _) => RestartService()), 2, 0);
        return panel;
    }

    private Control PortPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
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
        var panel = Card(new Padding(24, 20, 24, 20));
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
            RowCount = 8,
            ColumnCount = 1,
            BackColor = Color.White,
            Padding = new Padding(24, 0, 0, 0)
        };
        side.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        side.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        side.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        side.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        side.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        side.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        side.RowStyles.Add(new RowStyle(SizeType.Absolute, 16));
        side.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        side.Controls.Add(Label("选择本机地址"), 0, 0);
        _address.SelectedIndexChanged += (_, _) => UpdateUrlAndQr();
        side.Controls.Add(Host(_address, new Padding(10, 7, 10, 6), Color.FromArgb(242, 246, 248)), 0, 1);
        side.Controls.Add(Label("访问链接"), 0, 2);
        side.Controls.Add(Host(_url, new Padding(10, 8, 10, 6), Color.FromArgb(242, 246, 248)), 0, 3);
        side.Controls.Add(FillButton("复制链接", (_, _) => Clipboard.SetText(CurrentUrl()), primary: true), 0, 4);
        side.Controls.Add(FillButton("在本机浏览器打开", (_, _) => OpenUrl(CurrentUrl())), 0, 5);
        side.Controls.Add(new Label
        {
            Text = "手机无法访问时，请先确认手机和电脑在同一 Wi-Fi，再复制放行命令添加防火墙规则。",
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(80, 96, 104),
            TextAlign = ContentAlignment.TopLeft
        }, 0, 7);

        panel.Controls.Add(side, 1, 0);
        return panel;
    }

    private Control PresentationPanel()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = ModernTheme.Surface,
            Padding = new Padding(0)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 104));

        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill, BackColor = ModernTheme.Surface, WrapContents = false, Padding = new Padding(0, 2, 0, 2) };
        var add = SmallAction("添加 PPT", (_, _) => AddPresentationRules(), true);
        var remove = SmallAction("删除规则", (_, _) => DeleteSelectedRule(), false, true);
        var refresh = SmallAction("刷新状态", (_, _) => RefreshPresentationPanel());
        _toggleEditorButton = SmallAction("收起编辑", (_, _) => TogglePresentationSection(2, _ruleEditorCard, _toggleEditorButton, 76));
        _toggleActionsButton = SmallAction("收起操作", (_, _) => TogglePresentationSection(3, _presentationActions, _toggleActionsButton, 104));
        foreach (var button in new[] { add, remove, refresh, _toggleEditorButton, _toggleActionsButton }) button.Width = 96;
        toolbar.Controls.Add(add);
        toolbar.Controls.Add(remove);
        toolbar.Controls.Add(refresh);
        toolbar.Controls.Add(_toggleEditorButton);
        toolbar.Controls.Add(_toggleActionsButton);
        toolbar.Controls.Add(_presentationStatus);
        root.Controls.Add(toolbar, 0, 0);

        ModernTheme.StyleRounded(_ruleList, ModernTheme.CardRadius);
        root.Controls.Add(_ruleList, 0, 1);
        _ruleEditorCard = BuildRuleEditor();
        _presentationActions = BuildPresentationActions();
        root.Controls.Add(_ruleEditorCard, 0, 2);
        root.Controls.Add(_presentationActions, 0, 3);
        _presentationLayout = root;
        return root;
    }

    private void TogglePresentationSection(int row, Control? section, Button? button, int expandedHeight)
    {
        if (_presentationLayout is null || section is null || button is null) return;
        section.Visible = !section.Visible;
        _presentationLayout.RowStyles[row].Height = section.Visible ? expandedHeight : 0;
        button.Text = section.Visible ? button == _toggleEditorButton ? "收起编辑" : "收起操作" : button == _toggleEditorButton ? "展开编辑" : "展开操作";
    }

    private Control BuildRuleEditor()
    {
        var card = Card(new Padding(14, 8, 14, 8));
        card.Margin = new Padding(0, 8, 0, 8);
        card.ColumnCount = 6;
        card.RowCount = 2;
        card.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 66));
        card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        card.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 74));
        card.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 124));
        card.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 98));
        card.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        card.Controls.Add(new Label { Text = "路径", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        _toolTip.SetToolTip(_rulePath, "");
        card.Controls.Add(_rulePath, 1, 0);
        card.SetColumnSpan(_rulePath, 5);
        card.Controls.Add(new Label { Text = "时长", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 1);
        card.Controls.Add(Host(_ruleDuration, new Padding(10, 7, 10, 6), ModernTheme.ControlFill), 1, 1);
        card.Controls.Add(Host(_ruleEnabled, new Padding(10, 7, 10, 6), ModernTheme.ControlFill), 2, 1);
        card.Controls.Add(SmallAction("保存时长", (_, _) => SaveSelectedDuration(), true), 3, 1);
        card.Controls.Add(SmallAction("复制路径", (_, _) => CopySelectedPath()), 4, 1);
        card.Controls.Add(SmallAction("资源管理器", (_, _) => ShowSelectedPath()), 5, 1);
        _ruleDuration.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            SaveSelectedDuration();
        };
        _ruleDuration.TextChanged += (_, _) => { if (!_updatingRuleEditor) _durationDirty = true; };
        _ruleDuration.Leave += (_, _) => SaveSelectedDuration();
        _ruleEnabled.CheckedChanged += (_, _) => UpdateSelectedRule();
        return card;
    }

    private Control BuildPresentationActions()
    {
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, BackColor = ModernTheme.Surface, WrapContents = true, Padding = new Padding(0, 2, 0, 0) };
        panel.Controls.Add(SmallAction("只读打开/切换", (_, _) => SendPresentationCommand("ppt.openPresentation"), true));
        panel.Controls.Add(SmallAction("从头放映", (_, _) => SendPresentationCommand("ppt.startFromBeginning")));
        panel.Controls.Add(SmallAction("从当前页放映", (_, _) => SendPresentationCommand("ppt.startFromCurrent")));
        panel.Controls.Add(SmallAction("结束放映", (_, _) => SendPresentationCommand("ppt.endShow")));
        panel.Controls.Add(SmallAction("关闭当前受控文稿", (_, _) => SendPresentationCommand("ppt.closeCurrentPresentation"), false, true));
        panel.Controls.Add(SmallAction("退出 PowerPoint", (_, _) => SendPresentationCommand("ppt.exitApplication"), false, true));
        panel.Controls.Add(SmallAction("强制退出 PowerPoint/WPS", (_, _) => ConfirmForceQuit(), false, true));
        return panel;
    }

    private Button SmallAction(string text, EventHandler handler, bool primary = false, bool danger = false)
    {
        var button = new Button { Text = text, Height = 42, Width = 142, Margin = new Padding(0, 0, 8, 8), UseCompatibleTextRendering = true };
        button.Click += handler;
        button.BackColor = primary ? ModernTheme.AccentStrong : danger ? ModernTheme.DangerSoft : ModernTheme.AccentSoft;
        button.ForeColor = primary ? Color.White : danger ? ModernTheme.Danger : ModernTheme.Text;
        ModernTheme.StyleRounded(button, ModernTheme.ButtonRadius);
        return button;
    }

    private Control BottomPanel()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = ModernTheme.Surface,
            Padding = new Padding(0, 12, 0, 0)
        };
        panel.Controls.Add(Button("关闭", (_, _) => Hide(), 150));
        panel.Controls.Add(Button("复制放行命令", (_, _) => Clipboard.SetText(BuildFirewallCommand()), 220));
        return panel;
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
        AutoSize = false,
        Dock = DockStyle.Fill,
        ForeColor = Color.FromArgb(80, 96, 104),
        TextAlign = ContentAlignment.BottomLeft,
        Margin = Padding.Empty
    };

    private static Button FillButton(string text, EventHandler handler, bool primary = false)
    {
        var button = new Button
        {
            Text = text,
            Dock = DockStyle.Fill,
            Height = 46,
            Margin = new Padding(0, 4, 0, 6),
            AutoSize = false,
            UseCompatibleTextRendering = true,
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
            Height = 48,
            Margin = new Padding(6, 0, 0, 0),
            AutoSize = false,
            UseCompatibleTextRendering = true,
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
            FillColor = fill
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
    }

    private void RenderRuleRows(PresentationState state)
    {
        var selectedPath = _selectedPresentationPath ?? _selectedRule?.FilePath ?? "";
        var items = PresentationRuleValidator.MergeRulesAndOpenPresentations(_config.Rules, state.Presentations);

        var scroll = _ruleList.AutoScrollPosition;
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
            row.Width = Math.Max(420, _ruleList.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 16);
            row.Update(item.Rule, item.Presentation, SamePath(item.Path, selectedPath), File.Exists(item.Path));
            _ruleList.Controls.SetChildIndex(row, index++);
        }
        _ruleList.ResumeLayout();
        _ruleList.AutoScrollPosition = new Point(-scroll.X, -scroll.Y);
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
        _ruleEnabled.Checked = rule?.Enabled ?? false;
        _ruleDuration.Enabled = _ruleEnabled.Enabled = rule is not null;
        _updatingRuleEditor = false;
    }

    private void UpdateSelectedRule()
    {
        if (_updatingRuleEditor || _selectedRule is null || !_ruleEnabled.Focused) return;
        _selectedRule.Enabled = _ruleEnabled.Checked;
        SaveRulesImmediately();
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
        if (MessageBox.Show("强制退出会终止电脑端全部 PowerPoint/WPS/演示软件，未保存内容将丢失。确定继续吗？", "演讲计时器", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK) return;
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
        _url.Text = url;
        _toolTip.SetToolTip(_url, url);
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

    public PresentationRuleRow()
    {
        Height = 52;
        Margin = new Padding(0, 0, 0, 8);
        Cursor = Cursors.Hand;
        ModernTheme.StyleRounded(this, ModernTheme.CardRadius);

        _layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 2, Padding = new Padding(12, 3, 10, 3) };
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 94));
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78));
        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
        _title = new Label { Dock = DockStyle.Fill, Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
        _path = new Label { Dock = DockStyle.Fill, ForeColor = ModernTheme.MutedText, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
        _status = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
        _enabled = new Button { Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, UseCompatibleTextRendering = true, Margin = Padding.Empty };
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
        foreach (Control child in _layout.Controls) child.Click += (_, _) => Selected?.Invoke(this, EventArgs.Empty);
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
        Height = 36;
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
            item.Size = new Size(Math.Max(Width, 220), 36);
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
