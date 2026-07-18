using FlyPPTTimer.Models;
using FlyPPTTimer.Services;

namespace FlyPPTTimer.Forms;

internal sealed class BatchRuleSettingsDialog : Form
{
    private readonly TextBox _duration = new();
    private readonly ComboBox _mode = new() { DropDownStyle = ComboBoxStyle.DropDownList };

    public string Duration { get; private set; } = "00:08:00";
    public TimerMode Mode { get; private set; } = TimerMode.Countdown;

    public BatchRuleSettingsDialog(int count, string duration, TimerMode mode)
    {
        Text = "批量设置文件规则";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Microsoft YaHei UI", 10F);
        ClientSize = new Size(430, 220);
        BackColor = ModernTheme.Surface;

        _duration.Text = duration;
        _mode.Items.AddRange(["倒计时", "正计时"]);
        _mode.SelectedIndex = mode == TimerMode.CountUp ? 1 : 0;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 18, 24, 18),
            ColumnCount = 2,
            RowCount = 4,
            BackColor = ModernTheme.Card
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var summary = new Label { Text = $"已选择 {count} 条规则", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = ModernTheme.AccentStrong };
        root.Controls.Add(summary, 0, 0);
        root.SetColumnSpan(summary, 2);
        root.Controls.Add(new Label { Text = "统一时长", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 1);
        root.Controls.Add(_duration, 1, 1);
        root.Controls.Add(new Label { Text = "统一计时方式", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 2);
        root.Controls.Add(_mode, 1, 2);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
        var ok = new Button { Text = "确定", Width = 94, Height = 38, BackColor = ModernTheme.AccentStrong, ForeColor = Color.White };
        var cancel = new Button { Text = "取消", Width = 94, Height = 38, DialogResult = DialogResult.Cancel };
        ok.Click += (_, _) => AcceptChanges();
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);
        root.Controls.Add(buttons, 0, 3);
        root.SetColumnSpan(buttons, 2);
        Controls.Add(root);
        AcceptButton = ok;
        CancelButton = cancel;
    }

    private void AcceptChanges()
    {
        if (!PresentationRuleValidator.TryNormalizeDuration(_duration.Text, out var normalized, out var error))
        {
            MessageBox.Show(error, "演讲计时器", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _duration.Focus();
            return;
        }
        Duration = normalized;
        Mode = _mode.SelectedIndex == 1 ? TimerMode.CountUp : TimerMode.Countdown;
        DialogResult = DialogResult.OK;
        Close();
    }
}
