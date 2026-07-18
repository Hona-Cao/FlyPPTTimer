using FlyPPTTimer.Models;

namespace FlyPPTTimer.Forms;

// Compatibility control retained for the settings page. The redesigned remote
// dashboard uses PresentationRuleCard and its isolated blue theme instead.
internal sealed class PresentationRuleRow : UserControl
{
    private readonly TableLayoutPanel _layout;
    private readonly CheckBox _checked;
    private readonly Label _title;
    private readonly Label _path;
    private readonly Button _enabled;
    private FileRule? _rule;
    private bool _updating;

    public event EventHandler? Selected;
    public event EventHandler<bool>? CheckedChangedByUser;
    public event EventHandler<bool>? EnabledChangedByUser;

    public PresentationRuleRow()
    {
        Height = MinimumHeightFor(Font);
        Margin = new Padding(0, 0, 0, 5);
        Cursor = Cursors.Hand;
        ModernTheme.StyleRounded(this, ModernTheme.CardRadius);

        _layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 2,
            Padding = new Padding(12, 4, 10, 4)
        };
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 34));
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78));
        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, ModernTheme.StandardControlHeight));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _checked = new CheckBox
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            Text = string.Empty,
            AccessibleName = "选择此文件规则",
            Margin = Padding.Empty
        };
        _title = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
        _path = new Label { Dock = DockStyle.Fill, ForeColor = ModernTheme.MutedText, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
        _enabled = new Button { Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, UseCompatibleTextRendering = false, Margin = Padding.Empty };

        ModernTheme.StyleRounded(_enabled, ModernTheme.ControlRadius);
        _checked.CheckedChanged += (_, _) =>
        {
            if (_updating || _rule is null) return;
            CheckedChangedByUser?.Invoke(this, _checked.Checked);
        };
        _enabled.Click += (_, _) =>
        {
            if (_updating || _rule is null) return;
            EnabledChangedByUser?.Invoke(this, !_rule.Enabled);
        };

        _layout.Controls.Add(_checked, 0, 0);
        _layout.SetRowSpan(_checked, 2);
        _layout.Controls.Add(_title, 1, 0);
        _layout.Controls.Add(_enabled, 2, 0);
        _layout.Controls.Add(_path, 1, 1);
        _layout.SetColumnSpan(_path, 2);
        Controls.Add(_layout);

        Click += (_, _) => Selected?.Invoke(this, EventArgs.Empty);
        foreach (Control child in _layout.Controls)
            child.Click += (_, _) => Selected?.Invoke(this, EventArgs.Empty);
        ApplyTextMetrics();
    }

    public static int MinimumHeightFor(Font font)
    {
        var pathHeight = TextRenderer.MeasureText("C:\\演示文稿\\文件.pptx", font, Size.Empty, TextFormatFlags.SingleLine).Height;
        return ModernTheme.StandardControlHeight + pathHeight + 12;
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        if (_layout is not null) ApplyTextMetrics();
    }

    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        if (_layout is not null) ApplyTextMetrics();
    }

    private void ApplyTextMetrics()
    {
        var pathHeight = Math.Max(TextRenderer.MeasureText("C:\\演示文稿\\文件.pptx", _path.Font, Size.Empty, TextFormatFlags.SingleLine).Height + 4, 24);
        _layout.RowStyles[1].SizeType = SizeType.Absolute;
        _layout.RowStyles[1].Height = pathHeight;
        Height = _layout.Padding.Vertical + ModernTheme.StandardControlHeight + pathHeight;
    }

    public void Update(FileRule? rule, PresentationOption? option, bool selected, bool exists, bool checkedForBatch = false)
    {
        _updating = true;
        _rule = rule;

        BackColor = selected ? ModernTheme.AccentSoft : ModernTheme.Card;
        _layout.BackColor = BackColor;
        _checked.Visible = rule is not null;
        _checked.Checked = rule is not null && checkedForBatch;
        var modeText = rule?.Mode == TimerMode.CountUp ? "正计时" : "倒计时";
        _title.Text = rule is null ? option?.Name ?? "演示文稿" : $"{rule.FileName}   {rule.Duration}   {modeText}";
        _path.Text = rule?.FilePath ?? Path.Combine(option?.Directory ?? string.Empty, option?.Name ?? string.Empty);
        _enabled.Visible = rule is not null;
        _enabled.Text = rule?.Enabled == true ? "已启用" : "已禁用";
        _enabled.BackColor = rule?.Enabled == true ? ModernTheme.SuccessSoft : ModernTheme.ControlFill;
        _enabled.ForeColor = rule?.Enabled == true ? ModernTheme.Success : ModernTheme.MutedText;
        _updating = false;
    }
}
