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
    private FlowLayoutPanel? _rulesList;
    private TextBox? _rulePathBox;
    private TextBox? _ruleDurationBox;
    private Button? _ruleEnabledButton;
    private Label? _ruleNameLabel;
    private bool _updatingRuleEditor;
    private bool _renderingRules;
    private bool _isDirty;
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

    public event EventHandler<AppConfig>? ConfigApplied;
    public event EventHandler? ResetRequested;
    public event EventHandler? ExportRequested;
    public event EventHandler? ImportRequested;
    public event EventHandler? OpenConfigRequested;
    public event EventHandler? OpenLogRequested;

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
            MarkDirty();
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
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
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
        Row(grid, "单页计时（后续版本）", new CheckBox { Checked = _config.Timer.EnablePerSlideTimer, Text = "启用" }, "perSlide");
        Section(grid, "文件规则");
        AddFileRulesPanel(grid);
        AddTab("时长设置", grid);
    }

    private void AddFileRulesPanel(TableLayoutPanel grid)
    {
        var row = grid.RowCount++;
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 470));

        var card = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            Padding = new Padding(14),
            Margin = new Padding(0, 6, 0, 10),
            BackColor = Color.White
        };
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        card.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 132));
        ModernTheme.StyleRounded(card, ModernTheme.CardRadius);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 4, 0, 6),
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
        card.Controls.Add(buttons, 0, 0);

        _rulesSource.DataSource = new BindingList<FileRule>(_config.Rules.Select(CloneRule).ToList());
        _rulesList = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(6),
            Margin = new Padding(0, 0, 0, 12),
            BackColor = ModernTheme.ControlFill
        };
        ModernTheme.StyleRounded(_rulesList, ModernTheme.ButtonRadius);
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
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 84));
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));

        _ruleNameLabel = new Label { Text = "未选择文件", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
        _rulePathBox = new TextBox { ReadOnly = true, BorderStyle = BorderStyle.None };
        _ruleDurationBox = new TextBox { BorderStyle = BorderStyle.None };
        _ruleEnabledButton = new Button { Text = "已启用", FlatStyle = FlatStyle.Flat, UseCompatibleTextRendering = true };
        ModernTheme.StyleRounded(_ruleEnabledButton, ModernTheme.ControlRadius);
        _ruleDurationBox.TextChanged += (_, _) => UpdateCurrentRuleFromEditor();
        _ruleEnabledButton.Click += (_, _) => ToggleCurrentRuleEnabled();

        AddEditorCell(editor, "文件", _ruleNameLabel, 0);
        AddEditorCell(editor, "路径", _rulePathBox, 1);
        editor.Controls.Add(new Label { Text = "时长", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 0, 0) }, 2, 0);
        editor.Controls.Add(DecorateControl(_ruleDurationBox), 3, 0);
        editor.Controls.Add(new Label { Text = "状态", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 0, 0) }, 2, 1);
        editor.Controls.Add(DecorateControl(_ruleEnabledButton), 3, 1);
        return editor;
    }

    private void AddEditorCell(TableLayoutPanel editor, string label, Control control, int row)
    {
        editor.Controls.Add(new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(2, 0, 0, 0) }, 0, row);
        var decorated = DecorateControl(control);
        decorated.Margin = new Padding(0, 5, 8, 5);
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
                Enabled = true
            });
        }
        MarkDirty();
    }

    private void DeleteSelectedRule()
    {
        if (_rulesSource.Current is null) return;
        _rulesSource.RemoveCurrent();
        RefreshRuleEditor();
        MarkDirty();
    }

    private void ClearRules()
    {
        if (_rulesSource.Count == 0) return;
        if (MessageBox.Show("确定清空所有文件计时规则？", "演讲计时器", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK) return;
        _rulesSource.Clear();
        RefreshRuleEditor();
        MarkDirty();
    }

    private void RefreshRuleEditor()
    {
        if (_ruleNameLabel is null || _rulePathBox is null || _ruleDurationBox is null || _ruleEnabledButton is null) return;
        _updatingRuleEditor = true;
        if (_rulesSource.Current is FileRule rule)
        {
            _ruleNameLabel.Text = rule.FileName;
            _rulePathBox.Text = rule.FilePath;
            _rulePathBox.Tag = rule.FilePath;
            _ruleDurationBox.Text = rule.Duration;
            SetRuleEnabledButton(rule.Enabled);
        }
        else
        {
            _ruleNameLabel.Text = "未选择文件";
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
        _rulesSource.ResetCurrentItem();
        MarkDirty();
    }

    private void ToggleCurrentRuleEnabled()
    {
        if (_updatingRuleEditor || _rulesSource.Current is not FileRule rule) return;
        rule.Enabled = !rule.Enabled;
        SetRuleEnabledButton(rule.Enabled);
        _rulesSource.ResetCurrentItem();
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
                row.Width = Math.Max(420, _rulesList.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 16);
                row.Update(rule, option, SameRulePath(selectedPath, rule.FilePath), File.Exists(rule.FilePath));
                var rowIndex = CurrentRules().ToList().FindIndex(item => ReferenceEquals(item, rule));
                row.Selected += (_, _) =>
                {
                    _rulesSource.Position = rowIndex;
                    RefreshRuleEditor();
                    RenderSettingsRules();
                };
                row.EnabledChangedByUser += (_, enabled) =>
                {
                    rule.Enabled = enabled;
                    _rulesSource.ResetItem(rowIndex);
                    MarkDirty();
                };
                _rulesList.Controls.Add(row);
                index++;
            }
            _rulesList.ResumeLayout();
            _rulesList.AutoScrollPosition = new Point(-scroll.X, -scroll.Y);
        }
        finally { _renderingRules = false; }
    }

    private static bool SameRulePath(string? left, string? right) =>
        string.Equals(PresentationRuleValidator.NormalizePath(left), PresentationRuleValidator.NormalizePath(right), StringComparison.OrdinalIgnoreCase);

    private static FileRule CloneRule(FileRule rule) => new()
    {
        FileName = rule.FileName,
        FilePath = rule.FilePath,
        Duration = rule.Duration,
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
        Row(grid, "提示1启用", new CheckBox { Checked = _config.Behavior.Prompt1.Enabled, Text = "启用" }, "p1Enabled");
        Row(grid, "提示1提前秒数", NumberText(_config.Behavior.Prompt1.TriggerBeforeEndSeconds), "p1Before");
        Row(grid, "提示1文本", new TextBox { Text = _config.Behavior.Prompt1.Text }, "p1Text");
        Section(grid, "提示 2");
        Row(grid, "提示2启用", new CheckBox { Checked = _config.Behavior.Prompt2.Enabled, Text = "启用" }, "p2Enabled");
        Row(grid, "提示2提前秒数", NumberText(_config.Behavior.Prompt2.TriggerBeforeEndSeconds), "p2Before");
        Row(grid, "提示2文本", new TextBox { Text = _config.Behavior.Prompt2.Text }, "p2Text");
        Section(grid, "计时结束");
        Row(grid, "结束语音提示", new CheckBox { Checked = _config.Behavior.EndPrompt.Speak, Text = "启用" }, "endSpeak");
        Row(grid, "结束提示文本", new TextBox { Text = _config.Behavior.EndPrompt.Text }, "endText");
        Row(grid, "结束闪烁秒数", NumberText(_config.Behavior.EndPrompt.FlashSeconds), "endFlash");
        AddTab("行为设置", grid);
    }

    private void AddAppearanceTab()
    {
        var grid = NewGrid();
        Section(grid, "配色");
        Row(grid, "配色方案", Combo(["默认", "医疗与卫生-手术室蓝", "高对比红色警示", "透明黑底白字", "自定义"], _config.Appearance.ColorScheme), "scheme");
        Row(grid, "字体颜色", new TextBox { Text = _config.Appearance.TextColor }, "textColor");
        Row(grid, "背景颜色", new TextBox { Text = _config.Appearance.BackgroundColor }, "backColor");
        Row(grid, "超时颜色", new TextBox { Text = _config.Appearance.TimeoutTextColor }, "timeoutColor");
        Row(grid, "超时背景", new TextBox { Text = _config.Appearance.TimeoutBackgroundColor }, "timeoutBack");
        Row(grid, "闪烁背景颜色", new TextBox { Text = _config.Appearance.FlashBackgroundColor }, "flashBack");
        Section(grid, "窗口尺寸与字体");
        Row(grid, "宽", NumberText(_config.Appearance.Width), "width");
        Row(grid, "高", NumberText(_config.Appearance.Height), "height");
        Row(grid, "字体", new TextBox { Text = _config.Appearance.FontFamily }, "font");
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
        Section(grid, "闪烁与超时");
        Row(grid, "闪烁样式", Combo(["无", "闪烁文字", "闪烁背景", "实线边框", "边框加背景"], _config.Appearance.FlashStyle == "边框+背景" ? "边框加背景" : _config.Appearance.FlashStyle), "flashStyle");
        Row(grid, "闪现时长（毫秒）", NumberText(_config.Appearance.FlashOnMs), "flashOn");
        Row(grid, "隐藏时长（毫秒）", NumberText(_config.Appearance.FlashOffMs), "flashOff");
        Row(grid, "超时前缀", new TextBox { Text = _config.Appearance.OvertimePrefix }, "overtimePrefix");
        AddTab("外观与显示", grid);
    }

    private void AddControlTab()
    {
        var grid = NewGrid();
        Section(grid, "快捷键");
        Row(grid, "开始/暂停快捷键", Combo(["F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12"], _config.Controls.StartPauseHotkey), "hkStart");
        Row(grid, "停止/重置快捷键", Combo(["F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12"], _config.Controls.StopResetHotkey), "hkStop");
        Row(grid, "显示/隐藏快捷键", Combo(["F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12"], _config.Controls.ToggleWindowHotkey), "hkToggle");
        Row(grid, "打开设置快捷键", Combo(["F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12"], _config.Controls.OpenSettingsHotkey), "hkSettings");
        Section(grid, "窗口行为");
        Row(grid, "鼠标穿透", new CheckBox { Checked = _config.Controls.ClickThrough, Text = "启用" }, "clickThrough");
        Row(grid, "锁定窗口", new CheckBox { Checked = _config.Controls.LockPosition, Text = "启用" }, "lock");
        Row(grid, "托盘最小化", new CheckBox { Checked = _config.Controls.MinimizeToTray, Text = "启用" }, "minTray");
        Row(grid, "关闭按钮行为", Combo(["退出程序", "最小化到托盘"], _config.Controls.CloseButtonBehavior == CloseButtonBehavior.Exit ? "退出程序" : "最小化到托盘"), "closeBehavior");
        Row(grid, "开机自启（后续版本）", new CheckBox { Checked = false, Text = "预留" }, "startup");
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
        Row(grid, "所有 IPv4 地址", new TextBox { ReadOnly = true, Multiline = true, ScrollBars = ScrollBars.None, Text = BuildAllUrls(), Height = 170 }, "remoteAllUrls");
        Section(grid, "操作");
        Row(grid, "重启远程服务", Button("重启远程服务并应用端口", (_, _) => { Apply(); _remoteControl.Restart(); RefreshRemoteTexts(); }), "remoteRestartButton");
        Row(grid, "重新生成令牌", Button("重新生成令牌", (_, _) => { _remoteControl.RegenerateToken(); RefreshRemoteTexts(); }), "remoteTokenButton");
        Row(grid, "断开所有设备", Button("断开所有远程设备", (_, _) => { _remoteControl.DisconnectAll(); RefreshRemoteTexts(); }), "remoteDisconnectButton");
        Row(grid, "复制访问地址", Button("复制推荐 URL", (_, _) => Clipboard.SetText(BuildRecommendedUrl())), "remoteCopyUrlButton");
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
        Section(grid, "配置管理");
        Row(grid, "配置导入", Button("配置导入", (_, _) => ImportRequested?.Invoke(this, EventArgs.Empty)), "otherImport");
        Row(grid, "配置导出", Button("配置导出", (_, _) => ExportRequested?.Invoke(this, EventArgs.Empty)), "otherExport");
        Row(grid, "恢复默认", Button("恢复默认", (_, _) => ResetRequested?.Invoke(this, EventArgs.Empty)), "otherReset");
        Section(grid, "文件位置");
        Row(grid, "配置文件", Button("打开配置文件位置", (_, _) => OpenConfigRequested?.Invoke(this, EventArgs.Empty)), "otherConfigPath");
        Row(grid, "日志文件", Button("打开日志文件位置", (_, _) => OpenLogRequested?.Invoke(this, EventArgs.Empty)), "otherLogPath");
        Section(grid, "版本");
        Row(grid, "当前版本", new Label { Text = $"演讲计时器 {AppVersion.Current} 便携版", TextAlign = ContentAlignment.MiddleLeft }, "otherVersion");
        AddTab("其他设置", grid);
    }

    private Button Button(string text, EventHandler handler)
    {
        var b = new Button { Text = text, Height = 50, UseCompatibleTextRendering = true, BackColor = ModernTheme.AccentSoft };
        b.Click += handler;
        NormalizeControl(b);
        return b;
    }

    private void BuildBottomButtons()
    {
        var bottom = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8, 12, 8, 12), WrapContents = false, BackColor = ModernTheme.Surface };
        var ok = new Button { Text = "确定", Width = 132, Height = 54, MinimumSize = new Size(132, 54), AutoSize = false, UseCompatibleTextRendering = true, BackColor = ModernTheme.AccentStrong, ForeColor = Color.White };
        var cancel = new Button { Text = "取消", Width = 132, Height = 54, MinimumSize = new Size(132, 54), AutoSize = false, UseCompatibleTextRendering = true, BackColor = ModernTheme.Card };
        var apply = new Button { Text = "应用", Width = 132, Height = 54, MinimumSize = new Size(132, 54), AutoSize = false, UseCompatibleTextRendering = true, BackColor = ModernTheme.AccentSoft };
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
        _config.Timer.DefaultDuration = Get<TextBox>("duration").Text;
        _config.Timer.Mode = (string)Get<ComboBox>("mode").SelectedItem! == "倒计时" ? TimerMode.Countdown : TimerMode.CountUp;
        _config.Timer.EnablePerSlideTimer = Get<CheckBox>("perSlide").Checked;
        _config.Rules = CurrentRules().Select(CloneRule).ToList();
        _config.Behavior.AutoStartOnFullscreen = Get<CheckBox>("autoFullscreen").Checked;
        _config.Behavior.StopWhenLeavingFullscreen = Get<CheckBox>("stopFullscreen").Checked;
        _config.Behavior.ResetWhenLeavingFullscreen = Get<CheckBox>("resetFullscreen").Checked;
        _config.Behavior.FlashPausedTime = Get<CheckBox>("pauseFlash").Checked;
        _config.Behavior.Prompt1.Enabled = Get<CheckBox>("p1Enabled").Checked;
        _config.Behavior.Prompt1.TriggerBeforeEndSeconds = ReadInt("p1Before", 0, 99999, _config.Behavior.Prompt1.TriggerBeforeEndSeconds);
        _config.Behavior.Prompt1.Text = Get<TextBox>("p1Text").Text;
        _config.Behavior.Prompt2.Enabled = Get<CheckBox>("p2Enabled").Checked;
        _config.Behavior.Prompt2.TriggerBeforeEndSeconds = ReadInt("p2Before", 0, 99999, _config.Behavior.Prompt2.TriggerBeforeEndSeconds);
        _config.Behavior.Prompt2.Text = Get<TextBox>("p2Text").Text;
        _config.Behavior.EndPrompt.Speak = Get<CheckBox>("endSpeak").Checked;
        _config.Behavior.EndPrompt.Text = Get<TextBox>("endText").Text;
        _config.Behavior.EndPrompt.FlashSeconds = ReadInt("endFlash", 0, 120, _config.Behavior.EndPrompt.FlashSeconds);
        _config.Appearance.ColorScheme = (string)Get<ComboBox>("scheme").SelectedItem!;
        _config.Appearance.TextColor = Get<TextBox>("textColor").Text;
        _config.Appearance.BackgroundColor = Get<TextBox>("backColor").Text;
        _config.Appearance.TimeoutTextColor = Get<TextBox>("timeoutColor").Text;
        _config.Appearance.TimeoutBackgroundColor = Get<TextBox>("timeoutBack").Text;
        _config.Appearance.FlashBackgroundColor = Get<TextBox>("flashBack").Text;
        _config.Appearance.Width = ReadInt("width", 80, 2000, _config.Appearance.Width);
        _config.Appearance.Height = ReadInt("height", 40, 1000, _config.Appearance.Height);
        _config.Appearance.FontFamily = Get<TextBox>("font").Text;
        _config.Appearance.FontSize = (float)ReadDecimal("fontSize", 8, 180, (decimal)_config.Appearance.FontSize);
        _config.Appearance.Shape = (string)Get<ComboBox>("shape").SelectedItem!;
        _config.Appearance.BackgroundOpacity = ReadInt("bgOpacity", 0, 100, _config.Appearance.BackgroundOpacity);
        _config.Appearance.FlashStyle = ((string)Get<ComboBox>("flashStyle").SelectedItem!).Replace("边框加背景", "边框+背景");
        _config.Appearance.FlashOnMs = ReadInt("flashOn", 50, 5000, _config.Appearance.FlashOnMs);
        _config.Appearance.FlashOffMs = ReadInt("flashOff", 50, 5000, _config.Appearance.FlashOffMs);
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
        _config.Controls.OpenSettingsHotkey = (string)Get<ComboBox>("hkSettings").SelectedItem!;
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
        _config.Controls.Hotkeys["openSettings"] = _config.Controls.OpenSettingsHotkey;
        if (HasDuplicateHotkeys(_config.Controls.Hotkeys, out var duplicate))
        {
            MessageBox.Show($"快捷键重复：{duplicate}", "演讲计时器", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
        _config.Controls.ClickThrough = Get<CheckBox>("clickThrough").Checked;
        _config.Controls.LockPosition = Get<CheckBox>("lock").Checked;
        _config.Controls.MinimizeToTray = Get<CheckBox>("minTray").Checked;
        _config.Controls.CloseButtonBehavior = (string)Get<ComboBox>("closeBehavior").SelectedItem! == "退出程序" ? CloseButtonBehavior.Exit : CloseButtonBehavior.MinimizeToTray;
        ConfigApplied?.Invoke(this, _config);
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
        var address = _networkAddressService.GetIPv4Addresses().FirstOrDefault(x => x.Recommended)?.Address ?? "127.0.0.1";
        return $"http://{address}:{port}/?token={_config.RemoteControl.Token}";
    }

    private string BuildAllUrls()
    {
        var port = _remoteControl.CurrentPort > 0 ? _remoteControl.CurrentPort : _config.RemoteControl.Port;
        return string.Join(Environment.NewLine, _networkAddressService.GetIPv4Addresses().Select(x => $"{x.Type} - {x.Name}: http://{x.Address}:{port}/?token={_config.RemoteControl.Token}"));
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
        ("preset15", "设置为 15 分钟"),
        ("openSettings", "打开设置")
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
