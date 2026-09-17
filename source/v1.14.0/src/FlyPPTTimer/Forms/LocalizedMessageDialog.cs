using FlyPPTTimer.Services;

namespace FlyPPTTimer.Forms;

internal sealed class LocalizedMessageDialog : Form
{
    private LocalizedMessageDialog(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
    {
        var localizedText = Localization.T(text);
        Text = Localization.T(caption);
        Font = SystemFonts.MessageBoxFont;
        BackColor = ModernTheme.Surface;
        ForeColor = ModernTheme.Text;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        MaximizeBox = false;
        MinimizeBox = false;
        ControlBox = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        var measuredMessage = TextRenderer.MeasureText(
            localizedText,
            Font,
            new Size(390, int.MaxValue),
            TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
        ClientSize = new Size(500, Math.Clamp(measuredMessage.Height + 150, 220, 380));

        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 22, 24, 18),
            ColumnCount = 2,
            RowCount = 2,
            BackColor = ModernTheme.Surface
        };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 48));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

        var picture = new PictureBox
        {
            Size = new Size(32, 32),
            Margin = new Padding(0, 3, 12, 0),
            SizeMode = PictureBoxSizeMode.StretchImage,
            Image = ResolveIcon(icon)?.ToBitmap()
        };
        body.Controls.Add(picture, 0, 0);

        var message = new Label
        {
            Text = localizedText,
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = ModernTheme.Text,
            Margin = new Padding(0, 0, 0, 8)
        };
        body.Controls.Add(message, 1, 0);

        var buttonBar = new FlowLayoutPanel
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 8, 0, 0),
            Margin = Padding.Empty
        };
        body.SetColumnSpan(buttonBar, 2);
        body.Controls.Add(buttonBar, 0, 1);

        foreach (var spec in GetButtons(buttons).Reverse())
        {
            var label = Localization.T(spec.Label);
            var button = new Button
            {
                Text = label,
                DialogResult = spec.Result,
                AutoSize = false,
                Height = 36,
                Width = Math.Clamp(TextRenderer.MeasureText(label, Font).Width + 38, 88, 150),
                Margin = new Padding(10, 0, 0, 0),
                BackColor = spec.IsPrimary ? ModernTheme.Accent : Color.White,
                ForeColor = spec.IsPrimary ? Color.White : ModernTheme.Text
            };
            ModernTheme.StyleRounded(button);
            buttonBar.Controls.Add(button);
            if (spec.IsPrimary) AcceptButton = button;
            if (spec.Result == DialogResult.Cancel) CancelButton = button;
        }

        Controls.Add(body);
    }

    public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
    {
        using var dialog = new LocalizedMessageDialog(text, caption, buttons, icon);
        return dialog.ShowDialog();
    }

    public static DialogResult Show(IWin32Window owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
    {
        using var dialog = new LocalizedMessageDialog(text, caption, buttons, icon);
        return dialog.ShowDialog(owner);
    }

    private static Icon? ResolveIcon(MessageBoxIcon icon) => icon switch
    {
        MessageBoxIcon.Error => SystemIcons.Error,
        MessageBoxIcon.Question => SystemIcons.Question,
        MessageBoxIcon.Warning => SystemIcons.Warning,
        MessageBoxIcon.Information => SystemIcons.Information,
        _ => null
    };

    private static IReadOnlyList<ButtonSpec> GetButtons(MessageBoxButtons buttons) => buttons switch
    {
        MessageBoxButtons.OKCancel =>
        [
            new("确定", DialogResult.OK, true),
            new("取消", DialogResult.Cancel, false)
        ],
        MessageBoxButtons.YesNo =>
        [
            new("是", DialogResult.Yes, true),
            new("否", DialogResult.No, false)
        ],
        MessageBoxButtons.YesNoCancel =>
        [
            new("是", DialogResult.Yes, true),
            new("否", DialogResult.No, false),
            new("取消", DialogResult.Cancel, false)
        ],
        MessageBoxButtons.RetryCancel =>
        [
            new("重试", DialogResult.Retry, true),
            new("取消", DialogResult.Cancel, false)
        ],
        _ =>
        [
            new("确定", DialogResult.OK, true)
        ]
    };

    private sealed record ButtonSpec(string Label, DialogResult Result, bool IsPrimary);
}
