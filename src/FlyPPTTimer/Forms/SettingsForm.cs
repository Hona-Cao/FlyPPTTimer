using FlyPPTTimer.Models;
using FlyPPTTimer.Services;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;

namespace FlyPPTTimer.Forms;

public sealed class SettingsForm : Form
{
    private const int WmNcHitTest = 0x0084;
    private const int HtClient = 1;
    private const int HtCaption = 2;
    private const int HtLeft = 10;
    private const int HtRight = 11;
    private const int HtTop = 12;
    private const int HtTopLeft = 13;
    private const int HtTopRight = 14;
    private const int HtBottom = 15;
    private const int HtBottomLeft = 16;
    private const int HtBottomRight = 17;
    private const int ResizeBorder = 8;

    private AppConfig _config;
    private readonly RemoteControlService _remoteControl;
    private readonly NetworkAddressService _networkAddressService;
    private readonly TableLayoutPanel _shell = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = ModernTheme.Surface };
    private readonly Panel _titleBar = new() { Dock = DockStyle.Fill, BackColor = ModernTheme.Card, Padding = new Padding(18, 0, 8, 0) };
    private readonly TableLayoutPanel _settingsArea = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = ModernTheme.Surface };
    private readonly FlowLayoutPanel _navBar = new()
    {
        Dock = DockStyle.Fill,
        FlowDirection = FlowDirection.LeftToRight,
        WrapContents = true,
        Padding = new Padding(8, 8, 8, 8),
        Margin = new Padding(0, 0, 0, 12),
        BackColor = ModernTheme.Surface
    };
    private readonly Panel _contentHost = new() { Dock = DockStyle.Fill, BackColor = ModernTheme.Surface };
    private readonly TableLayoutPanel _root = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(16), BackColor = ModernTheme.Surface };
    private readonly List<(SettingsNavButton Button, ScrollableControl Page)> _pages = [];
    private readonly Dictionary<string, Control> _fields = [];
    private readonly BindingSource _rulesSource = new();
    private readonly HashSet<string> _checkedRulePaths = new(StringComparer.OrdinalIgnoreCase);
    private FlowLayoutPanel? _rulesList;
    private TextBox? _rulePathBox;
    private TextBox? _ruleDurationBox;
    private Button? _ruleEnabledButton;
    private TextBox? _ruleNameBox;
    private bool _updatingRuleEditor;
    private bool _renderingRules;
    private bool _syncingRules;
    private bool _syncingTimer;
    private bool _isDirty;
    private bool _resetOverlayPositionPending;
    private readonly Label _dirtyLabel = new() { AutoSize = true, ForeColor = ModernTheme.AccentStrong, Text = "有未应用的更改", Visible = false, Margin = new Padding(10, 16, 12, 0) };

    public SettingsForm(AppConfig config, RemoteControlService remoteControl, NetworkAddressService networkAddressService)
    {
        _config = ConfigService.Clone(config);
        _remoteControl = remoteControl;
        _networkAddressService = networkAddressService;
        Text = "演讲计时器设置";
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        FormBorderStyle = FormBorderStyle.None;
        MaximizeBox = true;
        Padding = new Padding(ResizeBorder);
        BackColor = ModernTheme.Surface;
        DoubleBuffered = true;
        ClientSize = new Size(980, 760);
        MinimumSize = new Size(780, 560);
        ResizeRedraw = true;
        Resize += (_, _) => LayoutNavigation();
        HandleCreated += (_, _) => ApplyWindowChromeRegion();
        SizeChanged += (_, _) => ApplyWindowChromeRegion();
        Shown += (_, _) => EnsureVisibleOnPrimaryScreen();
        BuildWindowChrome();
        BuildTabs();
        BuildBottomButtons();
        TrackDraftChanges();
        LayoutNavigation();
    }

    public void ReloadConfig(AppConfig config)
    {
        _config = ConfigService.Clone(config);
        if (!IsDisposed)
        {
            Hide();
            Dispose();
        }
    }

    public void SyncRules(IReadOnlyList<FileRule> rules)
    {
        var incoming = rules.Select(CloneRule).ToList();
        if (IsDisposed) return;
        if (InvokeRequired)
        {
            BeginInvoke((MethodInvoker)(() => SyncRules(incoming)));
            return;
        }

        var current = CurrentRules().ToList();
        if (current.Count == incoming.Count && current.Zip(incoming).All(pair => SameRule(pair.First, pair.Second)))
            return;

        var selectedPath = (_rulesSource.Current as FileRule)?.FilePath;
        _checkedRulePaths.IntersectWith(incoming.Select(rule => PresentationRuleValidator.NormalizePath(rule.FilePath)));
        _syncingRules = true;
        try
        {
            _config.Rules = incoming.Select(CloneRule).ToList();
            _rulesSource.DataSource = new BindingList<FileRule>(incoming);
            if (!string.IsNullOrWhiteSpace(selectedPath))
            {
                var index = CurrentRules().ToList().FindIndex(rule => SameRulePath(rule.FilePath, selectedPath));
                if (index >= 0) _rulesSource.Position = index;
            }
        }
        finally
        {
            _syncingRules = false;
        }
        RenderSettingsRules();
        RefreshRuleEditor();
    }

    public void SyncTimerSettings(TimerSettings timer)
    {
        if (IsDisposed) return;
        if (InvokeRequired)
        {
            BeginInvoke((MethodInvoker)(() => SyncTimerSettings(timer)));
            return;
        }

        _config.Timer.DefaultDuration = timer.DefaultDuration;
        _config.Timer.Mode = timer.Mode;
        _config.Timer.ContinueOvertime = timer.ContinueOvertime;
        if (!_fields.TryGetValue("duration", out var durationControl)
            || !_fields.TryGetValue("mode", out var modeControl)
            || !_fields.TryGetValue("continueOvertime", out var overtimeControl)) return;

        _syncingTimer = true;
        try
        {
            durationControl.Text = timer.DefaultDuration;
            ((ComboBox)modeControl).SelectedItem = timer.Mode == TimerMode.Countdown ? "倒计时" : "正计时";
            ((ComboBox)overtimeControl).SelectedItem = timer.ContinueOvertime ? "继续显示超时" : "到零后停止";
        }
        finally
        {
            _syncingTimer = false;
        }
    }

    public event EventHandler<AppConfig>? ConfigApplied;
    public event EventHandler? ResetRequested;
    public event EventHandler? ExportRequested;
    public event EventHandler? ImportRequested;
    public event EventHandler? OpenConfigRequested;
    public event EventHandler? OpenLogRequested;
    public event EventHandler? ResetOverlayPositionRequested;
    public event EventHandler? CheckUpdateRequested;

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            TryHide();
        }
        base.OnFormClosing(e);
    }

    private void TrackDraftChanges()
    {
        foreach (var field in _fields.Values) TrackControl(field);
        _rulesSource.ListChanged += (_, _) =>
        {
            if (!_syncingRules) MarkDirty();
            RenderSettingsRules();
        };
    }

    private void TrackControl(Control control)
    {
        control.TextChanged += (_, _) => MarkDirty();
        if (control is CheckBox checkBox) checkBox.CheckedChanged += (_, _) => MarkDirty();
        if (control is ComboBox comboBox) comboBox.SelectedIndexChanged += (_, _) => MarkDirty();
        foreach (Control child in control.Controls) TrackControl(child);
    }

    private void MarkDirty()
    {
        if (_syncingTimer) return;
        _isDirty = true;
        _dirtyLabel.Visible = true;
    }

    private bool TryHide()
    {
        if (!_isDirty) { Hide(); return true; }
        var choice = MessageBox.Show("设置中有未应用的更改。是：应用并关闭；否：放弃更改；取消：继续编辑。", "演讲计时器", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
        if (choice == DialogResult.Cancel) return false;
        if (choice == DialogResult.Yes && !Apply()) return false;
        _isDirty = false;
        _dirtyLabel.Visible = false;
        Hide();
        return true;
    }

    private void BuildTabs()
    {
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        _settingsArea.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        _settingsArea.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _settingsArea.Controls.Add(_navBar, 0, 0);
        _settingsArea.Controls.Add(_contentHost, 0, 1);
        _root.Controls.Add(_settingsArea, 0, 0);
        _shell.Controls.Add(_root, 0, 1);
        AddDurationTab();
        AddBehaviorTab();
        AddAppearanceTab();
        AddRemoteControlTab();
        AddControlTab();
        AddOtherTab();
        SelectSettingsPage(0);
    }

    private void LayoutNavigation()
    {
        if (_settingsArea.RowStyles.Count == 0) return;
        var available = Math.Max(1, _navBar.ClientSize.Width - _navBar.Padding.Horizontal);
        var total = _navBar.Controls.Cast<Control>().Sum(control => control.Width + control.Margin.Horizontal);
        var rows = total > available ? 2 : 1;
        _settingsArea.RowStyles[0].Height = rows == 1 ? 58 : 108;
    }

    private void BuildWindowChrome()
    {
        _shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        _shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(_shell);

        var brand = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = ModernTheme.Card,
            Margin = Padding.Empty
        };
        brand.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44));
        brand.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var logo = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.CenterImage,
            Image = LoadBrandImage(),
            Margin = Padding.Empty,
            BackColor = ModernTheme.Card
        };
        var title = new Label
        {
            Text = "演讲计时器设置",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font(Font.FontFamily, 11.5F, FontStyle.Bold),
            ForeColor = ModernTheme.Text,
            BackColor = ModernTheme.Card,
            Padding = new Padding(4, 0, 0, 0)
        };
        title.MouseDown += DragWindowFromTitleBar;
        logo.MouseDown += DragWindowFromTitleBar;
        brand.MouseDown += DragWindowFromTitleBar;
        _titleBar.MouseDown += DragWindowFromTitleBar;
        brand.Controls.Add(logo, 0, 0);
        brand.Controls.Add(title, 1, 0);
        _titleBar.Controls.Add(brand);
        Disposed += (_, _) => logo.Image?.Dispose();

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Width = 232,
            Padding = new Padding(0, 8, 0, 8),
            BackColor = ModernTheme.Card
        };
        buttons.Controls.Add(TitleButton("－", () => WindowState = FormWindowState.Minimized));
        buttons.Controls.Add(TitleButton("□", ToggleMaximize));
        buttons.Controls.Add(TitleButton("×", () => TryHide()));
        _titleBar.Controls.Add(buttons);
        _shell.Controls.Add(_titleBar, 0, 0);
    }

    private void ToggleMaximize()
    {
        WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
        ApplyWindowChromeRegion();
    }

    private void DragWindowFromTitleBar(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        ReleaseCapture();
        SendMessage(Handle, 0xA1, HtCaption, 0);
    }

    private void ApplyWindowChromeRegion()
    {
        if (WindowState == FormWindowState.Maximized)
        {
            Region = null;
            return;
        }

        ModernTheme.ApplyRoundedRegion(this, ModernTheme.WindowRadius);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (WindowState == FormWindowState.Maximized) return;
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var path = ModernTheme.RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), ModernTheme.WindowRadius);
        using var pen = new Pen(Color.FromArgb(225, 234, 237), 1);
        e.Graphics.DrawPath(pen, path);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmNcHitTest && WindowState == FormWindowState.Normal)
        {
            base.WndProc(ref m);
            if ((int)m.Result != HtClient) return;

            var point = PointToClient(new Point((short)((long)m.LParam & 0xFFFF), (short)(((long)m.LParam >> 16) & 0xFFFF)));
            var left = point.X <= ResizeBorder;
            var right = point.X >= ClientSize.Width - ResizeBorder;
            var top = point.Y <= ResizeBorder;
            var bottom = point.Y >= ClientSize.Height - ResizeBorder;

            m.Result = (IntPtr)(left && top ? HtTopLeft
                : right && top ? HtTopRight
                : left && bottom ? HtBottomLeft
                : right && bottom ? HtBottomRight
                : left ? HtLeft
                : right ? HtRight
                : top ? HtTop
                : bottom ? HtBottom
                : HtClient);
            return;
        }

        base.WndProc(ref m);
    }

    private Button TitleButton(string text, Action action)
    {
        var button = new Button
        {
            Text = text,
            Width = 68,
            Height = 58,
            Font = new Font(Font.FontFamily, 14F, FontStyle.Regular),
            Margin = new Padding(6, 0, 0, 0),
            BackColor = ModernTheme.Card,
            ForeColor = ModernTheme.Text,
            UseCompatibleTextRendering = true
        };
        button.Click += (_, _) => action();
        ModernTheme.StyleRounded(button, ModernTheme.ControlRadius);
        return button;
    }

    private TableLayoutPanel NewGrid()
    {
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            GrowStyle = TableLayoutPanelGrowStyle.AddRows,
            ColumnCount = 2,
            Padding = new Padding(24, 20, 24, 30),
            Margin = new Padding(0, 0, 0, 12),
            BackColor = ModernTheme.Card
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 270));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        ModernTheme.StyleRounded(grid, ModernTheme.CardRadius);
        return grid;
    }

    private void AddTab(string title, Control content)
    {
        var page = new Panel
        {
            AutoScroll = true,
            BackColor = ModernTheme.Surface,
            Padding = new Padding(16),
            Dock = DockStyle.Fill,
            Visible = false
        };
        content.Dock = DockStyle.Top;
        page.Controls.Add(content);
        WireMouseWheelToPage(content, page);
        _contentHost.Controls.Add(page);

        var index = _pages.Count;
        var button = new SettingsNavButton
        {
            Text = title,
            Width = 132,
            Height = 38,
            Margin = new Padding(0, 0, 12, 0),
            TabStop = false
        };
        button.Click += (_, _) => SelectSettingsPage(index);
        _navBar.Controls.Add(button);
        _pages.Add((button, page));
        LayoutNavigation();
    }

    private void SelectSettingsPage(int index)
    {
        if (index < 0 || index >= _pages.Count) return;
        for (var i = 0; i < _pages.Count; i++)
        {
            var selected = i == index;
            _pages[i].Button.Selected = selected;
            _pages[i].Page.Visible = selected;
            if (selected) _pages[i].Page.BringToFront();
        }
    }

    private void WireMouseWheelToPage(Control root, ScrollableControl page)
    {
        root.MouseWheel += (_, e) => ScrollPageByWheel(page, e);
        foreach (Control child in root.Controls)
        {
            WireMouseWheelToPage(child, page);
        }
    }

    private static void ScrollPageByWheel(ScrollableControl page, MouseEventArgs e)
    {
        if (e is HandledMouseEventArgs handled) handled.Handled = true;
        var current = -page.AutoScrollPosition.Y;
        var max = Math.Max(0, page.DisplayRectangle.Height - page.ClientSize.Height);
        var lineHeight = Math.Max(18, page.Font.Height + 8);
        var lines = Math.Max(1, SystemInformation.MouseWheelScrollLines);
        var next = current + (e.Delta < 0 ? lineHeight * lines : -lineHeight * lines);
        next = Math.Clamp(next, 0, max);
        page.AutoScrollPosition = new Point(0, next);
    }

    private void Section(TableLayoutPanel grid, string title)
    {
        var row = grid.RowCount++;
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        var label = new Label
        {
            Text = title,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font(Font, FontStyle.Bold),
            ForeColor = Color.FromArgb(28, 79, 112),
            BackColor = ModernTheme.SectionFill,
            Padding = new Padding(14, 0, 0, 0),
            Margin = new Padding(0, 12, 0, 8)
        };
        ModernTheme.StyleRounded(label, ModernTheme.ControlRadius);
        grid.Controls.Add(label, 0, row);
        grid.SetColumnSpan(label, 2);
    }

    private void Row(TableLayoutPanel grid, string label, Control control, string key)
    {
        var row = grid.RowCount++;
        NormalizeControl(control);
        var height = control is DataGridView ? 246
            : control is TextBox { Multiline: true } ? Math.Max(86, control.Height + 18)
            : control is Label labelControl && labelControl.Text.Length > 42 ? 86
            : 64;
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        grid.Controls.Add(new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(6, 0, 18, 0), AutoEllipsis = true, ForeColor = ModernTheme.Text }, 0, row);
        var displayControl = DecorateControl(control);
        displayControl.Dock = DockStyle.Fill;
        displayControl.Margin = new Padding(3, 9, 3, 9);
        grid.Controls.Add(displayControl, 1, row);
        _fields[key] = control;
    }

    private Control DecorateControl(Control control)
    {
        if (control is System.Windows.Forms.Button)
        {
            control.Dock = DockStyle.Fill;
            return control;
        }

        if (control is TextBox textBox)
        {
            textBox.BorderStyle = BorderStyle.None;
            textBox.BackColor = ModernTheme.ControlFill;
            if (textBox.ReadOnly && textBox.Multiline && textBox.Text.TrimStart().StartsWith("netsh", StringComparison.OrdinalIgnoreCase))
            {
                textBox.Font = new Font("Consolas", Font.Size, FontStyle.Regular, GraphicsUnit.Point);
            }
        }
        else if (control is ComboBox combo)
        {
            combo.FlatStyle = FlatStyle.Flat;
            combo.BackColor = ModernTheme.ControlFill;
        }
        else if (control is CheckBox checkBox)
        {
            checkBox.BackColor = ModernTheme.ControlFill;
            checkBox.Padding = new Padding(8, 0, 0, 0);
        }
        else if (control is Label label)
        {
            label.BackColor = ModernTheme.ControlFill;
            label.Padding = new Padding(10, 0, 10, 0);
        }

        var host = new RoundedHostPanel
        {
            FillColor = ModernTheme.ControlFill,
            Padding = control switch
            {
                DataGridView => new Padding(1),
                ComboBox => new Padding(12, 6, 10, 6),
                TextBox => new Padding(10, 8, 10, 6),
                CheckBox => new Padding(4, 7, 8, 7),
                Label => new Padding(0),
                _ => new Padding(10, 8, 10, 6)
            }
        };
        control.Dock = DockStyle.Fill;
        control.Margin = Padding.Empty;
        host.Controls.Add(control);
        return host;
    }

    private void NormalizeControl(Control control)
    {
        control.Font = Font;
        control.MinimumSize = control is Button ? new Size(0, 46) : new Size(0, 38);
        switch (control)
        {
            case ComboBox combo:
                combo.IntegralHeight = false;
                combo.DropDownHeight = 360;
                combo.ItemHeight = Math.Max(combo.ItemHeight, 32);
                combo.FlatStyle = FlatStyle.Flat;
                combo.BackColor = ModernTheme.ControlFill;
                combo.MouseWheel += BlockMouseWheel;
                break;
            case CheckBox checkBox:
                checkBox.AutoSize = false;
                checkBox.TextAlign = ContentAlignment.MiddleLeft;
                break;
        }
        if (control is not Label and not CheckBox)
        {
            ModernTheme.StyleRounded(control, ModernTheme.ControlRadius);
        }
    }

    private void AddDurationTab()
    {
        var grid = NewGrid();
        Section(grid, "基础计时");
        Row(grid, "默认时长 HH:mm:ss", new TextBox { Text = _config.Timer.DefaultDuration }, "duration");
        Row(grid, "计时模式", Combo(["倒计时", "正计时"], _config.Timer.Mode == TimerMode.Countdown ? "倒计时" : "正计时"), "mode");
        Row(grid, "到达预设时间后", Combo(["停止计时", "继续显示超时"], _config.Timer.ContinueOvertime ? "继续显示超时" : "停止计时"), "continueOvertime");
        Row(grid, "时间到后的操作", Combo(["仅提示", "黑屏并显示“时间到”", "退出放映"], EndActionToText(_config.Timer.EndAction)), "endAction");
        Section(grid, "文件规则");
        AddFileRulesPanel(grid);
        AddTab("时长设置", grid);
    }

    private void AddFileRulesPanel(TableLayoutPanel grid)
    {
        var row = grid.RowCount++;
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 480));

        var card = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            Padding = new Padding(12),
            Margin = new Padding(0, 6, 0, 10),
            BackColor = Color.White
        };
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        card.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 108));
        ModernTheme.StyleRounded(card, ModernTheme.CardRadius);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(4, 4, 4, 4),
            Margin = Padding.Empty,
            BackColor = Color.White
        };
        var add = SmallButton("添加文件", AddRuleFiles);
        add.BackColor = ModernTheme.AccentStrong;
        add.ForeColor = Color.White;
        var remove = SmallButton("删除", DeleteSelectedRule);
        remove.BackColor = ModernTheme.DangerSoft;
        remove.ForeColor = ModernTheme.Danger;
        var clear = SmallButton("清空", ClearRules);
        clear.BackColor = ModernTheme.DangerSoft;
        clear.ForeColor = ModernTheme.Danger;
        buttons.Controls.Add(add);
        buttons.Controls.Add(remove);
        buttons.Controls.Add(clear);
        var batch = SmallButton("批量设置", EditCheckedRules);
        batch.BackColor = ModernTheme.AccentSoft;
        batch.ForeColor = ModernTheme.AccentStrong;
        buttons.Controls.Add(batch);
        card.Controls.Add(buttons, 0, 0);

        _rulesSource.DataSource = new BindingList<FileRule>(_config.Rules.Select(CloneRule).ToList());
        _rulesList = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(6),
            Margin = new Padding(0, 0, 0, 8),
            BackColor = ModernTheme.ControlFill
        };
        ModernTheme.StyleRounded(_rulesList, ModernTheme.ButtonRadius);
        _rulesList.SizeChanged += (_, _) => UpdateSettingsRuleWidths();
        card.Controls.Add(_rulesList, 0, 1);

        card.Controls.Add(BuildRuleEditor(), 0, 2);
        grid.Controls.Add(card, 0, row);
        grid.SetColumnSpan(card, 2);
        _fields["rules"] = _rulesList;
        RenderSettingsRules();
        RefreshRuleEditor();
    }

    private Control BuildRuleEditor()
    {
        var editor = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 2,
            Padding = new Padding(2, 6, 2, 0),
            Margin = Padding.Empty,
            BackColor = Color.White
        };
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        _ruleNameBox = new TextBox { Text = "未选择文件", ReadOnly = true, BorderStyle = BorderStyle.None, TabStop = true };
        _rulePathBox = new TextBox { ReadOnly = true, BorderStyle = BorderStyle.None, TabStop = true };
        _ruleDurationBox = new TextBox { BorderStyle = BorderStyle.None };
        _ruleEnabledButton = new Button { Text = "已启用", FlatStyle = FlatStyle.Flat, UseCompatibleTextRendering = true };
        ModernTheme.StyleRounded(_ruleEnabledButton, ModernTheme.ControlRadius);
        _ruleDurationBox.TextChanged += (_, _) => UpdateCurrentRuleFromEditor();
        _ruleEnabledButton.Click += (_, _) => ToggleCurrentRuleEnabled();

        AddEditorCell(editor, "文件", _ruleNameBox, 0);
        AddEditorCell(editor, "路径", _rulePathBox, 1);
        editor.Controls.Add(new Label { Text = "时长", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 0, 0) }, 2, 0);
        var duration = DecorateControl(_ruleDurationBox);
        duration.Dock = DockStyle.Fill;
        duration.Margin = new Padding(0, 3, 0, 3);
        editor.Controls.Add(duration, 3, 0);
        editor.Controls.Add(new Label { Text = "状态", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 0, 0) }, 2, 1);
        var enabled = DecorateControl(_ruleEnabledButton);
        enabled.Dock = DockStyle.Fill;
        enabled.Margin = new Padding(0, 3, 0, 3);
        editor.Controls.Add(enabled, 3, 1);
        return editor;
    }

    private void AddEditorCell(TableLayoutPanel editor, string label, Control control, int row)
    {
        editor.Controls.Add(new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(2, 0, 0, 0) }, 0, row);
        var decorated = DecorateControl(control);
        decorated.Dock = DockStyle.Fill;
        decorated.Margin = new Padding(0, 3, 8, 3);
        editor.Controls.Add(decorated, 1, row);
    }

    private Button SmallButton(string text, Action action)
    {
        var button = new Button
        {
            Text = text,
            Width = 112,
            Height = 42,
            Margin = new Padding(0, 0, 10, 0),
            BackColor = ModernTheme.ControlFill,
            UseCompatibleTextRendering = true
        };
        button.Click += (_, _) => action();
        ModernTheme.StyleRounded(button, ModernTheme.ButtonRadius);
        return button;
    }

    private void AddRuleFiles()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "选择 PPT 文件",
            Filter = "演示文稿 (*.ppt;*.pptx;*.pptm)|*.ppt;*.pptx;*.pptm|所有文件 (*.*)|*.*",
            Multiselect = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        foreach (var file in dialog.FileNames)
        {
            if (CurrentRules().Any(rule => string.Equals(rule.FilePath, file, StringComparison.OrdinalIgnoreCase))) continue;
            _rulesSource.Add(new FileRule
            {
                FileName = Path.GetFileName(file),
                FilePath = file,
                Duration = Get<TextBox>("duration").Text,
                Mode = (string)Get<ComboBox>("mode").SelectedItem! == "倒计时" ? TimerMode.Countdown : TimerMode.CountUp,
                Enabled = true
            });
        }
        MarkDirty();
    }

    private void DeleteSelectedRule()
    {
        if (_rulesSource.Current is null) return;
        if (_rulesSource.Current is FileRule current)
            _checkedRulePaths.Remove(PresentationRuleValidator.NormalizePath(current.FilePath));
        _rulesSource.RemoveCurrent();
        RefreshRuleEditor();
        MarkDirty();
    }

    private void ClearRules()
    {
        if (_rulesSource.Count == 0) return;
        if (MessageBox.Show("确定清空所有文件计时规则？", "演讲计时器", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK) return;
        _rulesSource.Clear();
        _checkedRulePaths.Clear();
        RefreshRuleEditor();
        MarkDirty();
    }

    private void RefreshRuleEditor()
    {
        if (_ruleNameBox is null || _rulePathBox is null || _ruleDurationBox is null || _ruleEnabledButton is null) return;
        _updatingRuleEditor = true;
        if (_rulesSource.Current is FileRule rule)
        {
            _ruleNameBox.Text = rule.FileName;
            _rulePathBox.Text = rule.FilePath;
            _rulePathBox.Tag = rule.FilePath;
            _ruleDurationBox.Text = rule.Duration;
            SetRuleEnabledButton(rule.Enabled);
        }
        else
        {
            _ruleNameBox.Text = "未选择文件";
            _rulePathBox.Text = "";
            _rulePathBox.Tag = null;
            _ruleDurationBox.Text = "";
            SetRuleEnabledButton(false);
        }
        _updatingRuleEditor = false;
    }

    private void UpdateCurrentRuleFromEditor()
    {
        if (_updatingRuleEditor || _rulesSource.Current is not FileRule rule || _ruleDurationBox is null) return;
        rule.Duration = _ruleDurationBox.Text.Trim();
        RefreshExistingRuleRows();
        MarkDirty();
    }

    private void ToggleCurrentRuleEnabled()
    {
        if (_updatingRuleEditor || _rulesSource.Current is not FileRule rule) return;
        rule.Enabled = !rule.Enabled;
        SetRuleEnabledButton(rule.Enabled);
        RefreshExistingRuleRows();
        MarkDirty();
    }

    private void SetRuleEnabledButton(bool enabled)
    {
        if (_ruleEnabledButton is null) return;
        _ruleEnabledButton.Tag = enabled;
        _ruleEnabledButton.Text = enabled ? "已启用" : "已禁用";
        _ruleEnabledButton.BackColor = enabled ? ModernTheme.SuccessSoft : ModernTheme.ControlFill;
        _ruleEnabledButton.ForeColor = enabled ? ModernTheme.Success : ModernTheme.MutedText;
    }

    private IEnumerable<FileRule> CurrentRules()
    {
        return _rulesSource.List.Cast<FileRule>();
    }

    private void EditCheckedRules()
    {
        var selected = CurrentRules()
            .Where(rule => _checkedRulePaths.Contains(PresentationRuleValidator.NormalizePath(rule.FilePath)))
            .ToList();
        if (selected.Count == 0)
        {
            MessageBox.Show("请先勾选要批量修改的文件规则。", "演讲计时器", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new BatchRuleSettingsDialog(selected.Count, selected[0].Duration, selected[0].Mode);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        foreach (var rule in selected)
        {
            rule.Duration = dialog.Duration;
            rule.Mode = dialog.Mode;
        }
        RefreshRuleEditor();
        RefreshExistingRuleRows();
        MarkDirty();
    }

    private void RefreshExistingRuleRows()
    {
        if (_rulesList is null) return;
        var selectedPath = (_rulesSource.Current as FileRule)?.FilePath;
        var state = _remoteControl.PresentationController?.GetState();
        foreach (var row in _rulesList.Controls.OfType<PresentationRuleRow>())
        {
            if (row.Tag is not FileRule rule) continue;
            var normalizedPath = PresentationRuleValidator.NormalizePath(rule.FilePath);
            var option = state?.Presentations.FirstOrDefault(item => SameRulePath(Path.Combine(item.Directory, item.Name), rule.FilePath));
            row.Update(rule, option, SameRulePath(selectedPath, rule.FilePath), File.Exists(rule.FilePath), _checkedRulePaths.Contains(normalizedPath));
        }
    }

    private void RenderSettingsRules()
    {
        if (_renderingRules || _rulesList is null || IsDisposed) return;
        _renderingRules = true;
        try
        {
            var selectedPath = (_rulesSource.Current as FileRule)?.FilePath ?? "";
            var state = _remoteControl.PresentationController?.GetState();
            var scroll = _rulesList.AutoScrollPosition;
            _rulesList.SuspendLayout();
            foreach (Control control in _rulesList.Controls) control.Dispose();
            _rulesList.Controls.Clear();
            var index = 0;
            foreach (var rule in CurrentRules().OrderBy(rule => rule.FileName, StringComparer.CurrentCultureIgnoreCase))
            {
                var option = state?.Presentations.FirstOrDefault(item => SameRulePath(Path.Combine(item.Directory, item.Name), rule.FilePath));
                var row = new PresentationRuleRow();
                row.Width = SettingsRuleWidth();
                var normalizedPath = PresentationRuleValidator.NormalizePath(rule.FilePath);
                row.Tag = rule;
                row.Update(rule, option, SameRulePath(selectedPath, rule.FilePath), File.Exists(rule.FilePath), _checkedRulePaths.Contains(normalizedPath));
                var rowIndex = CurrentRules().ToList().FindIndex(item => ReferenceEquals(item, rule));
                row.Selected += (_, _) =>
                {
                    _rulesSource.Position = rowIndex;
                    RefreshRuleEditor();
                    RefreshExistingRuleRows();
                };
                row.CheckedChangedByUser += (_, isChecked) =>
                {
                    if (isChecked) _checkedRulePaths.Add(normalizedPath);
                    else _checkedRulePaths.Remove(normalizedPath);
                };
                row.EnabledChangedByUser += (_, enabled) =>
                {
                    rule.Enabled = enabled;
                    row.Update(rule, option, SameRulePath((_rulesSource.Current as FileRule)?.FilePath, rule.FilePath), File.Exists(rule.FilePath), _checkedRulePaths.Contains(normalizedPath));
                    if (ReferenceEquals(_rulesSource.Current, rule)) SetRuleEnabledButton(enabled);
                    MarkDirty();
                };
                _rulesList.Controls.Add(row);
                index++;
            }
            _rulesList.ResumeLayout();
            UpdateSettingsRuleWidths();
            _rulesList.AutoScrollPosition = new Point(-scroll.X, -scroll.Y);
        }
        finally { _renderingRules = false; }
    }

    private static bool SameRulePath(string? left, string? right) =>
        string.Equals(PresentationRuleValidator.NormalizePath(left), PresentationRuleValidator.NormalizePath(right), StringComparison.OrdinalIgnoreCase);

    private int SettingsRuleWidth()
    {
        if (_rulesList is null) return 420;
        var scrollbar = _rulesList.VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0;
        return Math.Max(420, _rulesList.ClientSize.Width - _rulesList.Padding.Horizontal - scrollbar - 2);
    }

    private void UpdateSettingsRuleWidths()
    {
        if (_rulesList is null) return;
        var width = SettingsRuleWidth();
        foreach (Control row in _rulesList.Controls)
            row.Width = width;
    }

    private static bool SameRule(FileRule left, FileRule right) =>
        SameRulePath(left.FilePath, right.FilePath) &&
        string.Equals(left.FileName, right.FileName, StringComparison.Ordinal) &&
        string.Equals(left.Duration, right.Duration, StringComparison.Ordinal) &&
        left.Mode == right.Mode &&
        left.Enabled == right.Enabled &&
        string.Equals(left.TitlePattern, right.TitlePattern, StringComparison.Ordinal) &&
        string.Equals(left.Feature, right.Feature, StringComparison.Ordinal);

    private static FileRule CloneRule(FileRule rule) => new()
    {
        FileName = rule.FileName,
        FilePath = rule.FilePath,
        Duration = rule.Duration,
        Mode = rule.Mode,
        Enabled = rule.Enabled,
        TitlePattern = rule.TitlePattern,
        Feature = rule.Feature
    };

    private void AddBehaviorTab()
    {
        var grid = NewGrid();
        Section(grid, "全局与启动");
        Row(grid, "退出全屏时停止计时", new CheckBox { Checked = _config.Behavior.StopWhenLeavingFullscreen, Text = "启用" }, "stopFullscreen");
        Row(grid, "全屏白名单自动开始", new CheckBox { Checked = _config.Behavior.AutoStartOnFullscreen, Text = "启用" }, "autoFullscreen");
        Row(grid, "退出全屏时重置", new CheckBox { Checked = _config.Behavior.ResetWhenLeavingFullscreen, Text = "启用" }, "resetFullscreen");
        Row(grid, "暂停时闪烁当前时间", new CheckBox { Checked = _config.Behavior.FlashPausedTime, Text = "启用" }, "pauseFlash");
        Section(grid, "提示 1");
        Row(grid, "提示1", new CheckBox { Checked = _config.Behavior.Prompt1.Enabled, Text = "启用" }, "p1Enabled");
        Row(grid, "距离预设时间还剩（秒）", NumberText(_config.Behavior.Prompt1.TriggerBeforeEndSeconds), "p1Before");
        Row(grid, "提示1语音播报", new CheckBox { Checked = _config.Behavior.Prompt1.Speak, Text = "启用" }, "p1Speak");
        Row(grid, "提示1提示音", new TextBox { ReadOnly = true, Text = _config.Behavior.Prompt1.SoundFile }, "p1Sound");
        Row(grid, "选择提示1提示音", Button("选择文件", (_, _) => ChooseAlertSound("p1Sound", "prompt1")), "p1SoundChoose");
        Row(grid, "清除提示1提示音", Button("恢复默认", (_, _) => ClearAlertSound("p1Sound")), "p1SoundClear");
        AddPromptFlashRows(grid, "提示1", "p1", _config.Behavior.Prompt1);
        Section(grid, "提示 2");
        Row(grid, "提示2", new CheckBox { Checked = _config.Behavior.Prompt2.Enabled, Text = "启用" }, "p2Enabled");
        Row(grid, "距离预设时间还剩（秒）", NumberText(_config.Behavior.Prompt2.TriggerBeforeEndSeconds), "p2Before");
        Row(grid, "提示2语音播报", new CheckBox { Checked = _config.Behavior.Prompt2.Speak, Text = "启用" }, "p2Speak");
        Row(grid, "提示2提示音", new TextBox { ReadOnly = true, Text = _config.Behavior.Prompt2.SoundFile }, "p2Sound");
        Row(grid, "选择提示2提示音", Button("选择文件", (_, _) => ChooseAlertSound("p2Sound", "prompt2")), "p2SoundChoose");
        Row(grid, "清除提示2提示音", Button("恢复默认", (_, _) => ClearAlertSound("p2Sound")), "p2SoundClear");
        AddPromptFlashRows(grid, "提示2", "p2", _config.Behavior.Prompt2);
        Section(grid, "计时结束");
        Row(grid, "计时结束", new CheckBox { Checked = _config.Behavior.EndPrompt.Enabled, Text = "启用" }, "endEnabled");
        Row(grid, "到时语音播报", new CheckBox { Checked = _config.Behavior.EndPrompt.Speak, Text = "启用" }, "endSpeak");
        Row(grid, "到时提示音", new TextBox { ReadOnly = true, Text = _config.Behavior.EndPrompt.SoundFile }, "endSound");
        Row(grid, "选择到时提示音", Button("选择文件", (_, _) => ChooseAlertSound("endSound", "end")), "endSoundChoose");
        Row(grid, "清除到时提示音", Button("恢复默认", (_, _) => ClearAlertSound("endSound")), "endSoundClear");
        AddPromptFlashRows(grid, "计时结束", "end", _config.Behavior.EndPrompt);
        Row(grid, "超时文字颜色", new TextBox { Text = _config.Appearance.TimeoutTextColor }, "timeoutColor");
        Row(grid, "超时背景颜色", new TextBox { Text = _config.Appearance.TimeoutBackgroundColor }, "timeoutBack");
        Row(grid, "超时前缀", new TextBox { Text = _config.Appearance.OvertimePrefix }, "overtimePrefix");
        AddTab("行为设置", grid);
    }

    private void AddAppearanceTab()
    {
        var grid = NewGrid();
        Section(grid, "配色");
        var scheme = Combo(AppearancePresetService.Names, _config.Appearance.ColorScheme);
        Row(grid, "配色方案", scheme, "scheme");
        Row(grid, "字体颜色", new TextBox { Text = _config.Appearance.TextColor }, "textColor");
        Row(grid, "背景颜色", new TextBox { Text = _config.Appearance.BackgroundColor }, "backColor");
        Row(grid, "闪烁背景颜色", new TextBox { Text = _config.Appearance.FlashBackgroundColor }, "flashBack");
        Section(grid, "窗口尺寸与字号");
        Row(grid, "宽", NumberText(_config.Appearance.Width), "width");
        Row(grid, "高", NumberText(_config.Appearance.Height), "height");
        Row(grid, "字号", NumberText((decimal)_config.Appearance.FontSize), "fontSize");
        Row(grid, "外观形状", Combo(["直角矩形", "圆角矩形（小）", "圆角矩形（大）"], _config.Appearance.Shape), "shape");
        Row(grid, "背景不透明度", NumberText(_config.Appearance.BackgroundOpacity), "bgOpacity");
        Section(grid, "多屏显示");
        Row(grid, "所有屏幕同时显示", new CheckBox { Checked = _config.Placement.ShowOnAllScreens, Text = "启用" }, "showAllScreens");
        Row(grid, "指定屏幕", Combo(GetScreenItems(), string.IsNullOrWhiteSpace(_config.Placement.TargetScreenDeviceName) ? "主屏幕" : _config.Placement.TargetScreenDeviceName), "targetScreen");
        Section(grid, "默认位置");
        Row(grid, "默认点位", Combo(GetAnchorItems(), AnchorToText(_config.Placement.Anchor)), "anchor");
        Row(grid, "水平微调百分比", NumberText(ClampDecimal(_config.Placement.OffsetXPercent, -50, 50)), "offsetX");
        Row(grid, "垂直微调百分比", NumberText(ClampDecimal(_config.Placement.OffsetYPercent, -50, 50)), "offsetY");
        Row(grid, "窗口位置", Button("重置计时窗口位置", (_, _) => ResetOverlayPositionDraft()), "resetOverlayPosition");
        scheme.SelectedIndexChanged += (_, _) => ApplyAppearancePresetToDraft();
        AddTab("外观与显示", grid);
    }

    private void AddControlTab()
    {
        var grid = NewGrid();
        Section(grid, "快捷键");
        Row(grid, "开始/暂停快捷键", Combo(["F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12"], _config.Controls.StartPauseHotkey), "hkStart");
        Row(grid, "停止/重置快捷键", Combo(["F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12"], _config.Controls.StopResetHotkey), "hkStop");
        Row(grid, "显示/隐藏快捷键", Combo(["F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12"], _config.Controls.ToggleWindowHotkey), "hkToggle");
        Section(grid, "窗口行为");
        Row(grid, "鼠标穿透", new CheckBox { Checked = _config.Controls.ClickThrough, Text = "启用" }, "clickThrough");
        Row(grid, "锁定窗口", new CheckBox { Checked = _config.Controls.LockPosition, Text = "启用" }, "lock");
        Row(grid, "托盘最小化", new CheckBox { Checked = _config.Controls.MinimizeToTray, Text = "启用" }, "minTray");
        Row(grid, "关闭按钮行为", Combo(["退出程序", "最小化到托盘"], _config.Controls.CloseButtonBehavior == CloseButtonBehavior.Exit ? "退出程序" : "最小化到托盘"), "closeBehavior");
        AddTab("控制设置", grid);
    }

    private void AddRemoteControlTab()
    {
        var grid = NewGrid();
        Section(grid, "本地网页遥控");
        Row(grid, "启用远程控制", new CheckBox { Checked = _config.RemoteControl.Enabled, Text = "启用" }, "remoteEnabled");
        Row(grid, "当前服务状态", new Label { Text = _remoteControl.StatusText, TextAlign = ContentAlignment.MiddleLeft }, "remoteStatus");
        Row(grid, "本次启动端口", new Label { Text = (_remoteControl.CurrentPort > 0 ? _remoteControl.CurrentPort : _config.RemoteControl.Port).ToString(), TextAlign = ContentAlignment.MiddleLeft }, "remoteCurrentPort");
        Row(grid, "下次服务端口", NumberText(_config.RemoteControl.Port <= 0 ? Math.Max(1, _remoteControl.CurrentPort) : _config.RemoteControl.Port), "remotePort");
        Row(grid, "使用随机端口", new CheckBox { Checked = _config.RemoteControl.UseRandomPort, Text = "启用" }, "remoteRandomPort");
        Row(grid, "端口生效说明", new Label { Text = "服务运行中端口会保持固定。修改端口或随机端口设置后，请点击“重启远程服务并应用端口”，或下次启动后生效。", TextAlign = ContentAlignment.MiddleLeft }, "remotePortHelp");
        Row(grid, "连接设备数量", new Label { Text = _remoteControl.ConnectedClients.ToString(), TextAlign = ContentAlignment.MiddleLeft }, "remoteClients");
        Section(grid, "访问地址");
        Row(grid, "推荐访问地址", new TextBox { ReadOnly = true, Text = BuildRecommendedUrl() }, "remoteRecommendedUrl");
        Row(grid, "手机可用局域网地址", new TextBox { ReadOnly = true, Multiline = true, ScrollBars = ScrollBars.None, Text = BuildAllUrls(), Height = 170 }, "remoteAllUrls");
        Section(grid, "操作");
        Row(grid, "重启远程服务", Button("重启远程服务并应用端口", (_, _) => { Apply(); _remoteControl.Restart(); RefreshRemoteTexts(); }), "remoteRestartButton");
        Row(grid, "重新生成令牌", Button("重新生成令牌", (_, _) => { _remoteControl.RegenerateToken(); RefreshRemoteTexts(); }), "remoteTokenButton");
        Row(grid, "断开所有设备", Button("断开所有远程设备", (_, _) => { _remoteControl.DisconnectAll(); RefreshRemoteTexts(); }), "remoteDisconnectButton");
        Row(grid, "复制访问地址", Button("复制推荐 URL", (_, _) => CopyRecommendedUrl()), "remoteCopyUrlButton");
        Row(grid, "打开本机控制页", Button("打开本机控制页", (_, _) => OpenUrl($"http://127.0.0.1:{(_remoteControl.CurrentPort > 0 ? _remoteControl.CurrentPort : _config.RemoteControl.Port)}/?token={_config.RemoteControl.Token}")), "remoteOpenLocalButton");
        Section(grid, "防火墙排障");
        Row(grid, "防火墙说明", new TextBox { ReadOnly = true, Multiline = true, Text = "手机无法访问时，常见原因是 Windows 防火墙、IP 选错、端口被占用、手机和电脑不在同一网络。不要关闭防火墙；只为当前程序和当前端口添加入站规则。", Height = 100 }, "remoteFirewallHelp");
        Row(grid, "修复命令", new TextBox { ReadOnly = true, Multiline = true, Text = BuildFirewallCommand(), Height = 90 }, "remoteFirewallCommand");
        Row(grid, "复制修复命令", Button("复制防火墙修复命令", (_, _) => Clipboard.SetText(BuildFirewallCommand())), "remoteCopyFirewallButton");
        Row(grid, "二维码显示", new Label { Text = "可从计时器或托盘右键菜单打开远程控制二维码。", TextAlign = ContentAlignment.MiddleLeft }, "remoteQr");
        AddTab("远程控制", grid);
    }

    private void AddOtherTab()
    {
        var grid = NewGrid();
        Section(grid, "软件更新");
        Row(grid, "启动时检测新版本", new CheckBox
        {
            Checked = _config.Update.CheckOnStartup,
            Text = "启用"
        }, "checkUpdateOnStartup");
        Row(grid, "手动检测", Button("立即检测新版本", (_, _) => CheckUpdateRequested?.Invoke(this, EventArgs.Empty)), "otherCheckUpdate");
        Section(grid, "配置管理");
        Row(grid, "配置导入", Button("配置导入", (_, _) => ImportRequested?.Invoke(this, EventArgs.Empty)), "otherImport");
        Row(grid, "配置导出", Button("配置导出", (_, _) => ExportRequested?.Invoke(this, EventArgs.Empty)), "otherExport");
        Row(grid, "恢复默认", Button("恢复默认", (_, _) => ResetRequested?.Invoke(this, EventArgs.Empty)), "otherReset");
        Section(grid, "文件位置");
        Row(grid, "配置文件", Button("打开配置文件位置", (_, _) => OpenConfigRequested?.Invoke(this, EventArgs.Empty)), "otherConfigPath");
        Row(grid, "日志文件", Button("打开日志文件位置", (_, _) => OpenLogRequested?.Invoke(this, EventArgs.Empty)), "otherLogPath");
        Section(grid, "关于 FlyPPTTimer");
        Row(grid, "当前版本", new Label { Text = $"FlyPPTTimer {AppVersion.Current} · Windows x64", TextAlign = ContentAlignment.MiddleLeft }, "otherVersion");
        Row(grid, "项目介绍", new TextBox
        {
            ReadOnly = true,
            Multiline = true,
            Height = 190,
            ScrollBars = ScrollBars.Vertical,
            Text = "FlyPPTTimer 是一款面向演讲、教学和会议场景的 Windows 演示计时工具，提供倒计时、正计时、多显示器悬浮显示、演示文稿规则，以及手机或浏览器局域网远程控制功能。软件配置、规则和日志默认保存在本机；远程控制仅在本地网络中运行，不依赖云端账户，也不会主动上传演示文稿内容。"
        }, "otherProjectDescription");
        Section(grid, "作者与协作");
        Row(grid, "作者的话", new TextBox
        {
            ReadOnly = true,
            Multiline = true,
            Height = 210,
            ScrollBars = ScrollBars.Vertical,
            Text = "FlyPPTTimer 由曹虎男发起并从零开发。作者毕业于南京大学医学院护理专业，目前就职于江苏省人民医院宿迁医院。在工作实践中发现了演讲计时、演示控制和台下远程调整的实际需求，因此将这个想法逐步实现为本项目。希望它能让大家的演讲、教学和会议更加从容，也欢迎有兴趣的朋友参与测试、提出建议或共同开发。祝大家使用愉快！"
        }, "otherAuthorStory");
        Row(grid, "联系邮箱", new Label { Text = "caohunan@smail.nju.edu.cn", TextAlign = ContentAlignment.MiddleLeft }, "otherEmail");
        Row(grid, "GitHub 项目主页", Button("打开 GitHub（可能需要网络工具）", (_, _) => OpenUrl("https://github.com/Hona-Cao/FlyPPTTimer")), "otherGitHub");
        Row(grid, "Gitee 项目主页", Button("打开 Gitee（中国大陆可直接访问）", (_, _) => OpenUrl("https://gitee.com/hona-cao/fly-ppttimer")), "otherGitee");
        Row(grid, "联系作者", Button("发送邮件", (_, _) => OpenUrl("mailto:caohunan@smail.nju.edu.cn")), "otherContact");
        AddTab("其他设置", grid);
    }

    private Button Button(string text, EventHandler handler)
    {
        var b = new Button { Text = text, Height = 50, UseCompatibleTextRendering = true, BackColor = ModernTheme.AccentSoft };
        b.Click += handler;
        NormalizeControl(b);
        return b;
    }

    private void ChooseAlertSound(string fieldKey, string slot)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "选择提示音文件",
            Filter = "音频文件 (*.mp3;*.wav;*.wma;*.m4a)|*.mp3;*.wav;*.wma;*.m4a",
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            Get<TextBox>(fieldKey).Text = AlertSoundStorage.ImportSound(dialog.FileName, slot);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "演讲计时器", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ClearAlertSound(string fieldKey) => Get<TextBox>(fieldKey).Text = "";

    private void AddPromptFlashRows(TableLayoutPanel grid, string label, string prefix, PromptSettings prompt)
    {
        var style = prompt.FlashStyle == "边框+背景" ? "边框加背景" : prompt.FlashStyle;
        Row(grid, $"{label}闪烁样式", Combo(["无", "闪烁文字", "闪烁背景", "实线边框", "边框加背景"], style), prefix + "FlashStyle");
        Row(grid, $"{label}闪现时长（毫秒）", NumberText(prompt.FlashOnMs), prefix + "FlashOn");
        Row(grid, $"{label}隐藏时长（毫秒）", NumberText(prompt.FlashOffMs), prefix + "FlashOff");
        Row(grid, $"{label}闪烁持续（秒）", NumberText(prompt.FlashSeconds), prefix + "FlashSeconds");
    }

    private void ApplyPromptFlash(string prefix, PromptSettings prompt)
    {
        prompt.FlashStyle = ((string)Get<ComboBox>(prefix + "FlashStyle").SelectedItem!).Replace("边框加背景", "边框+背景");
        prompt.FlashOnMs = ReadInt(prefix + "FlashOn", 50, 5000, prompt.FlashOnMs);
        prompt.FlashOffMs = ReadInt(prefix + "FlashOff", 50, 5000, prompt.FlashOffMs);
        prompt.FlashSeconds = ReadInt(prefix + "FlashSeconds", 0, 120, prompt.FlashSeconds);
        prompt.FlashText = prompt.FlashStyle.Contains("文字");
        prompt.FlashBackground = prompt.FlashStyle is not "无";
    }

    private void ApplyAppearancePresetToDraft()
    {
        if (!_fields.TryGetValue("scheme", out var control) || control is not ComboBox combo || combo.SelectedItem is not string name) return;
        var appearance = new AppearanceSettings();
        if (!AppearancePresetService.Apply(name, appearance)) return;
        Get<TextBox>("textColor").Text = appearance.TextColor;
        Get<TextBox>("backColor").Text = appearance.BackgroundColor;
        Get<TextBox>("timeoutColor").Text = appearance.TimeoutTextColor;
        Get<TextBox>("timeoutBack").Text = appearance.TimeoutBackgroundColor;
        Get<TextBox>("flashBack").Text = appearance.FlashBackgroundColor;
    }

    private void ResetOverlayPositionDraft()
    {
        _resetOverlayPositionPending = true;
        MarkDirty();
        MessageBox.Show("应用设置后，计时窗口中心将还原到当前选择的默认点位。", "演讲计时器", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void BuildBottomButtons()
    {
        var bottom = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8, 8, 8, 4), WrapContents = false, BackColor = ModernTheme.Surface };
        var ok = new Button { Text = "确定", Width = 124, Height = 42, MinimumSize = new Size(124, 42), AutoSize = false, UseCompatibleTextRendering = true, BackColor = ModernTheme.AccentStrong, ForeColor = Color.White };
        var cancel = new Button { Text = "取消", Width = 124, Height = 42, MinimumSize = new Size(124, 42), AutoSize = false, UseCompatibleTextRendering = true, BackColor = ModernTheme.Card };
        var apply = new Button { Text = "应用", Width = 124, Height = 42, MinimumSize = new Size(124, 42), AutoSize = false, UseCompatibleTextRendering = true, BackColor = ModernTheme.AccentSoft };
        ok.Click += (_, _) => { if (Apply()) { DialogResult = DialogResult.OK; Hide(); } };
        apply.Click += (_, _) => Apply();
        cancel.Click += (_, _) => TryHide();
        bottom.Controls.Add(_dirtyLabel);
        bottom.Controls.Add(ok);
        bottom.Controls.Add(cancel);
        bottom.Controls.Add(apply);
        ModernTheme.StyleRounded(ok, ModernTheme.ButtonRadius);
        ModernTheme.StyleRounded(cancel, ModernTheme.ButtonRadius);
        ModernTheme.StyleRounded(apply, ModernTheme.ButtonRadius);
        ok.BackColor = ModernTheme.AccentStrong;
        ok.ForeColor = Color.White;
        ok.FlatAppearance.MouseOverBackColor = ModernTheme.Accent;
        ok.FlatAppearance.MouseDownBackColor = Color.FromArgb(13, 72, 66);
        _root.Controls.Add(bottom, 0, 1);
    }

    private ComboBox Combo(string[] items, string selected)
    {
        var combo = new ModernComboBox();
        combo.Items.AddRange(items);
        combo.SelectedItem = items.Contains(selected) ? selected : items.First();
        return combo;
    }

    private static TextBox NumberText(decimal value) => new()
    {
        Text = value.ToString("0.##", CultureInfo.InvariantCulture),
        BorderStyle = BorderStyle.None
    };

    private static Image? LoadBrandImage()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "app.ico");
        try
        {
            using var icon = File.Exists(iconPath) ? new Icon(iconPath, 30, 30) : Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            if (icon is not null) return new Bitmap(icon.ToBitmap(), new Size(30, 30));
        }
        catch { }

        var path = Path.Combine(AppContext.BaseDirectory, "app.png");
        if (!File.Exists(path)) return null;
        try
        {
            using var source = Image.FromFile(path);
            return new Bitmap(source, new Size(30, 30));
        }
        catch { return null; }
    }

    private static void BlockMouseWheel(object? sender, MouseEventArgs e)
    {
        if (e is HandledMouseEventArgs handled) handled.Handled = true;
    }

    private T Get<T>(string key) where T : Control => (T)_fields[key];

    private bool ValidateDraft(out string error)
    {
        if (!PresentationRuleValidator.TryNormalizeDuration(Get<TextBox>("duration").Text, out _, out _))
        {
            error = "默认时长必须是 HH:mm:ss 格式。";
            return false;
        }
        var duplicatePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rule in CurrentRules())
        {
            if (!PresentationRuleValidator.TryNormalizeDuration(rule.Duration, out _, out _))
            {
                error = $"文件规则“{rule.FileName}”的计时时长无效。";
                return false;
            }
            var key = NormalizeRulePath(rule.FilePath);
            if (!string.IsNullOrWhiteSpace(key) && !duplicatePaths.Add(key))
            {
                error = "文件规则中不能重复添加同一份演示文稿。";
                return false;
            }
        }
        error = "";
        return true;
    }

    private static string NormalizeRulePath(string path)
    {
        try { return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar); }
        catch { return path.Trim(); }
    }

    private bool Apply()
    {
        if (!ValidateDraft(out var validationError))
        {
            MessageBox.Show(validationError, "演讲计时器", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
        var newDefaultDuration = Get<TextBox>("duration").Text;
        var rules = CurrentRules().Select(CloneRule).ToList();
        var syncRuleDurations = false;
        if (!string.Equals(_config.Timer.DefaultDuration, newDefaultDuration, StringComparison.Ordinal)
            && rules.Count > 0)
        {
            syncRuleDurations = MessageBox.Show(
                $"全局默认时长将改为 {newDefaultDuration}。\n\n是否同步应用到全部 {rules.Count} 个待控演示文稿？\n\n选择“否”将保留各文件规则原来的时长。",
                "同步文件规则时长",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes;
        }
        if (syncRuleDurations)
        {
            foreach (var rule in rules) rule.Duration = newDefaultDuration;
        }
        _config.Timer.DefaultDuration = newDefaultDuration;
        _config.Timer.Mode = (string)Get<ComboBox>("mode").SelectedItem! == "倒计时" ? TimerMode.Countdown : TimerMode.CountUp;
        _config.Timer.ContinueOvertime = (string)Get<ComboBox>("continueOvertime").SelectedItem! == "继续显示超时";
        _config.Timer.EndAction = TextToEndAction((string)Get<ComboBox>("endAction").SelectedItem!);
        _config.Timer.EnablePerSlideTimer = false;
        _config.Rules = rules;
        _config.Behavior.AutoStartOnFullscreen = Get<CheckBox>("autoFullscreen").Checked;
        _config.Behavior.StopWhenLeavingFullscreen = Get<CheckBox>("stopFullscreen").Checked;
        _config.Behavior.ResetWhenLeavingFullscreen = Get<CheckBox>("resetFullscreen").Checked;
        _config.Behavior.FlashPausedTime = Get<CheckBox>("pauseFlash").Checked;
        _config.Behavior.Prompt1.Enabled = Get<CheckBox>("p1Enabled").Checked;
        _config.Behavior.Prompt1.TriggerBeforeEndSeconds = ReadInt("p1Before", 0, 99999, _config.Behavior.Prompt1.TriggerBeforeEndSeconds);
        _config.Behavior.Prompt1.Text = "时间即将结束";
        _config.Behavior.Prompt1.Speak = Get<CheckBox>("p1Speak").Checked;
        _config.Behavior.Prompt1.SoundFile = Get<TextBox>("p1Sound").Text;
        _config.Behavior.Prompt1.PlaySound = !string.IsNullOrWhiteSpace(_config.Behavior.Prompt1.SoundFile);
        _config.Behavior.Prompt1.Beep = false;
        ApplyPromptFlash("p1", _config.Behavior.Prompt1);
        _config.Behavior.Prompt2.Enabled = Get<CheckBox>("p2Enabled").Checked;
        _config.Behavior.Prompt2.TriggerBeforeEndSeconds = ReadInt("p2Before", 0, 99999, _config.Behavior.Prompt2.TriggerBeforeEndSeconds);
        _config.Behavior.Prompt2.Text = "时间即将结束";
        _config.Behavior.Prompt2.Speak = Get<CheckBox>("p2Speak").Checked;
        _config.Behavior.Prompt2.SoundFile = Get<TextBox>("p2Sound").Text;
        _config.Behavior.Prompt2.PlaySound = !string.IsNullOrWhiteSpace(_config.Behavior.Prompt2.SoundFile);
        _config.Behavior.Prompt2.Beep = false;
        ApplyPromptFlash("p2", _config.Behavior.Prompt2);
        _config.Behavior.EndPrompt.Enabled = Get<CheckBox>("endEnabled").Checked;
        _config.Behavior.EndPrompt.Speak = Get<CheckBox>("endSpeak").Checked;
        _config.Behavior.EndPrompt.Text = "预设时间到";
        _config.Behavior.EndPrompt.SoundFile = Get<TextBox>("endSound").Text;
        _config.Behavior.EndPrompt.PlaySound = !string.IsNullOrWhiteSpace(_config.Behavior.EndPrompt.SoundFile);
        _config.Behavior.EndPrompt.Beep = false;
        ApplyPromptFlash("end", _config.Behavior.EndPrompt);
        _config.Appearance.ColorScheme = (string)Get<ComboBox>("scheme").SelectedItem!;
        _config.Appearance.TextColor = Get<TextBox>("textColor").Text;
        _config.Appearance.BackgroundColor = Get<TextBox>("backColor").Text;
        _config.Appearance.TimeoutTextColor = Get<TextBox>("timeoutColor").Text;
        _config.Appearance.TimeoutBackgroundColor = Get<TextBox>("timeoutBack").Text;
        _config.Appearance.FlashBackgroundColor = Get<TextBox>("flashBack").Text;
        _config.Appearance.Width = ReadInt("width", 1, 2000, _config.Appearance.Width);
        _config.Appearance.Height = ReadInt("height", 1, 1000, _config.Appearance.Height);
        _config.Appearance.FontFamily = "Microsoft YaHei UI";
        _config.Appearance.FontSize = (float)ReadDecimal("fontSize", 8, 180, (decimal)_config.Appearance.FontSize);
        _config.Appearance.Shape = (string)Get<ComboBox>("shape").SelectedItem!;
        _config.Appearance.BackgroundOpacity = ReadInt("bgOpacity", 0, 100, _config.Appearance.BackgroundOpacity);
        _config.Appearance.OvertimePrefix = Get<TextBox>("overtimePrefix").Text;
        _config.Placement.ShowOnAllScreens = Get<CheckBox>("showAllScreens").Checked;
        var screenText = (string)Get<ComboBox>("targetScreen").SelectedItem!;
        _config.Placement.TargetScreenDeviceName = screenText == "主屏幕" ? "" : screenText;
        _config.Placement.Anchor = TextToAnchor((string)Get<ComboBox>("anchor").SelectedItem!);
        _config.Placement.OffsetXPercent = ReadDecimal("offsetX", -50, 50, _config.Placement.OffsetXPercent);
        _config.Placement.OffsetYPercent = ReadDecimal("offsetY", -50, 50, _config.Placement.OffsetYPercent);
        _config.Placement.HasCustomPlacement = true;
        _config.Controls.StartPauseHotkey = (string)Get<ComboBox>("hkStart").SelectedItem!;
        _config.Controls.StopResetHotkey = (string)Get<ComboBox>("hkStop").SelectedItem!;
        _config.Controls.ToggleWindowHotkey = (string)Get<ComboBox>("hkToggle").SelectedItem!;
        _config.RemoteControl.Enabled = Get<CheckBox>("remoteEnabled").Checked;
        _config.RemoteControl.UseRandomPort = Get<CheckBox>("remoteRandomPort").Checked;
        _config.RemoteControl.Port = ReadInt("remotePort", 1, 65535, _config.RemoteControl.Port <= 0 ? 1 : _config.RemoteControl.Port);
        foreach (var item in HotkeyItems())
        {
            if (_fields.TryGetValue("hk_" + item.key, out var control))
            {
                _config.Controls.Hotkeys[item.key] = control.Text.Trim();
            }
        }
        _config.Controls.Hotkeys["startPause"] = _config.Controls.StartPauseHotkey;
        _config.Controls.Hotkeys["stopReset"] = _config.Controls.StopResetHotkey;
        _config.Controls.Hotkeys["toggleWindow"] = _config.Controls.ToggleWindowHotkey;
        _config.Controls.Hotkeys.Remove("openSettings");
        if (HasDuplicateHotkeys(_config.Controls.Hotkeys, out var duplicate))
        {
            MessageBox.Show($"快捷键重复：{duplicate}", "演讲计时器", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
        _config.Controls.ClickThrough = Get<CheckBox>("clickThrough").Checked;
        _config.Controls.LockPosition = Get<CheckBox>("lock").Checked;
        _config.Controls.MinimizeToTray = Get<CheckBox>("minTray").Checked;
        _config.Controls.CloseButtonBehavior = (string)Get<ComboBox>("closeBehavior").SelectedItem! == "退出程序" ? CloseButtonBehavior.Exit : CloseButtonBehavior.MinimizeToTray;
        _config.Update.CheckOnStartup = Get<CheckBox>("checkUpdateOnStartup").Checked;
        ConfigApplied?.Invoke(this, _config);
        if (_resetOverlayPositionPending)
        {
            ResetOverlayPositionRequested?.Invoke(this, EventArgs.Empty);
            _resetOverlayPositionPending = false;
        }
        _isDirty = false;
        _dirtyLabel.Visible = false;
        return true;
    }

    private static string[] GetScreenItems()
    {
        return new[] { "主屏幕" }.Concat(Screen.AllScreens.Select(x => x.DeviceName)).Distinct().ToArray();
    }

    private static string[] GetAnchorItems() =>
    [
        "左上", "上中", "右上",
        "左中", "正中", "右中",
        "左下", "下中", "右下"
    ];

    private static string AnchorToText(OverlayAnchor anchor) => anchor switch
    {
        OverlayAnchor.TopLeft => "左上",
        OverlayAnchor.TopCenter => "上中",
        OverlayAnchor.TopRight => "右上",
        OverlayAnchor.MiddleLeft => "左中",
        OverlayAnchor.Center => "正中",
        OverlayAnchor.MiddleRight => "右中",
        OverlayAnchor.BottomLeft => "左下",
        OverlayAnchor.BottomCenter => "下中",
        OverlayAnchor.BottomRight => "右下",
        _ => "右上"
    };

    private static OverlayAnchor TextToAnchor(string text) => text switch
    {
        "左上" => OverlayAnchor.TopLeft,
        "上中" => OverlayAnchor.TopCenter,
        "右上" => OverlayAnchor.TopRight,
        "左中" => OverlayAnchor.MiddleLeft,
        "正中" => OverlayAnchor.Center,
        "右中" => OverlayAnchor.MiddleRight,
        "左下" => OverlayAnchor.BottomLeft,
        "下中" => OverlayAnchor.BottomCenter,
        "右下" => OverlayAnchor.BottomRight,
        _ => OverlayAnchor.TopRight
    };

    private static string EndActionToText(TimerEndAction action) => action switch
    {
        TimerEndAction.BlackScreen => "黑屏并显示“时间到”",
        TimerEndAction.ExitSlideShow => "退出放映",
        _ => "仅提示"
    };

    private static TimerEndAction TextToEndAction(string text) => text switch
    {
        "黑屏并显示“时间到”" => TimerEndAction.BlackScreen,
        "退出放映" => TimerEndAction.ExitSlideShow,
        _ => TimerEndAction.None
    };

    private static decimal ClampDecimal(decimal value, decimal min, decimal max)
    {
        return Math.Min(max, Math.Max(min, value));
    }

    private int ReadInt(string key, int min, int max, int fallback)
    {
        var value = ReadDecimal(key, min, max, fallback);
        return (int)Math.Round(value);
    }

    private decimal ReadDecimal(string key, decimal min, decimal max, decimal fallback)
    {
        var text = Get<TextBox>(key).Text.Trim();
        if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            && !decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value))
        {
            value = fallback;
        }

        value = ClampDecimal(value, min, max);
        Get<TextBox>(key).Text = value.ToString("0.##", CultureInfo.InvariantCulture);
        return value;
    }

    private string BuildRecommendedUrl()
    {
        var port = _remoteControl.CurrentPort > 0 ? _remoteControl.CurrentPort : _config.RemoteControl.Port;
        var address = _networkAddressService.GetRemoteAccessAddresses().FirstOrDefault()?.Address;
        return address is null ? "未检测到可供手机访问的局域网地址" : $"http://{address}:{port}/?token={_config.RemoteControl.Token}";
    }

    private string BuildAllUrls()
    {
        var port = _remoteControl.CurrentPort > 0 ? _remoteControl.CurrentPort : _config.RemoteControl.Port;
        var addresses = _networkAddressService.GetRemoteAccessAddresses();
        return addresses.Count == 0
            ? "请让手机与电脑连接同一 Wi-Fi 或局域网后重试。"
            : string.Join(Environment.NewLine, addresses.Select(x => $"{x.Type} - {x.Name}: http://{x.Address}:{port}/?token={_config.RemoteControl.Token}"));
    }

    private void CopyRecommendedUrl()
    {
        var url = BuildRecommendedUrl();
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("未检测到手机可访问的局域网地址。请先让手机和电脑连接同一 Wi-Fi 或局域网。", "演讲计时器", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        Clipboard.SetText(url);
    }

    private string BuildFirewallCommand()
    {
        var port = _remoteControl.CurrentPort > 0 ? _remoteControl.CurrentPort : _config.RemoteControl.Port;
        var exe = Path.Combine(AppContext.BaseDirectory, "FlyPPTTimer.exe");
        return $"netsh advfirewall firewall add rule name=\"FlyPPTTimer Remote {port}\" dir=in action=allow program=\"{exe}\" protocol=TCP localport={port}";
    }

    private void RefreshRemoteTexts()
    {
        if (_fields.TryGetValue("remoteStatus", out var status)) status.Text = _remoteControl.StatusText;
        if (_fields.TryGetValue("remoteCurrentPort", out var currentPort)) currentPort.Text = (_remoteControl.CurrentPort > 0 ? _remoteControl.CurrentPort : _config.RemoteControl.Port).ToString();
        if (_fields.TryGetValue("remoteClients", out var clients)) clients.Text = _remoteControl.ConnectedClients.ToString();
        if (_fields.TryGetValue("remoteRecommendedUrl", out var url)) url.Text = BuildRecommendedUrl();
        if (_fields.TryGetValue("remoteAllUrls", out var urls)) urls.Text = BuildAllUrls();
        if (_fields.TryGetValue("remoteFirewallCommand", out var cmd)) cmd.Text = BuildFirewallCommand();
    }

    private static void OpenUrl(string url)
    {
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { }
    }

    private static (string key, string label)[] HotkeyItems() =>
    [
        ("startPause", "开始/暂停"),
        ("start", "开始"),
        ("pause", "暂停"),
        ("resume", "继续"),
        ("stopReset", "停止/重置"),
        ("stop", "停止"),
        ("reset", "重置"),
        ("toggleWindow", "显示/隐藏窗口"),
        ("showWindow", "显示窗口"),
        ("hideWindow", "隐藏窗口"),
        ("flash", "触发闪烁"),
        ("toggleMute", "静音/取消静音"),
        ("toggleMode", "切换倒计时/正计时"),
        ("addMinute", "增加 1 分钟"),
        ("subtractMinute", "减少 1 分钟"),
        ("preset3", "设置为 3 分钟"),
        ("preset5", "设置为 5 分钟"),
        ("preset8", "设置为 8 分钟"),
        ("preset10", "设置为 10 分钟"),
        ("preset15", "设置为 15 分钟")
    ];

    private static bool HasDuplicateHotkeys(Dictionary<string, string> hotkeys, out string duplicate)
    {
        duplicate = "";
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in hotkeys.Values.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            if (!seen.Add(value))
            {
                duplicate = value;
                return true;
            }
        }
        return false;
    }

    private void EnsureVisibleOnPrimaryScreen()
    {
        var area = Screen.PrimaryScreen!.WorkingArea;
        if (area.Contains(Bounds)) return;
        Location = new Point(
            area.Left + Math.Max(0, (area.Width - Width) / 2),
            area.Top + Math.Max(0, (area.Height - Height) / 2));
    }

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
}

internal sealed class SettingsNavButton : Button
{
    private bool _selected;

    public bool Selected
    {
        get => _selected;
        set
        {
            if (_selected == value) return;
            _selected = value;
            Invalidate();
        }
    }

    public SettingsNavButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        FlatAppearance.MouseDownBackColor = ModernTheme.AccentSoft;
        FlatAppearance.MouseOverBackColor = ModernTheme.AccentSoft;
        BackColor = Color.Transparent;
        ForeColor = ModernTheme.Text;
        UseCompatibleTextRendering = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        e.Graphics.Clear(Parent?.BackColor ?? ModernTheme.Surface);
        var rect = new Rectangle(1, 1, Width - 2, Height - 2);
        var fillColor = Selected ? ModernTheme.AccentStrong : ModernTheme.Card;
        using (var path = ModernTheme.RoundedRect(rect, ModernTheme.ButtonRadius))
        using (var fill = new SolidBrush(fillColor))
        {
            e.Graphics.FillPath(fill, path);
        }

        TextRenderer.DrawText(
            e.Graphics,
            Text,
            Font,
            rect,
            Selected ? Color.White : ModernTheme.Text,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }
}
