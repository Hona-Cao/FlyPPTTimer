using System.Drawing.Drawing2D;

namespace FlyPPTTimer.Forms;

internal static class ModernTheme
{
    public const int WindowRadius = 8;
    public const int CardRadius = 7;
    public const int ControlRadius = 5;
    public const int ButtonRadius = 6;
    public static readonly Color Surface = Color.FromArgb(242, 246, 248);
    public static readonly Color Card = Color.White;
    public static readonly Color AccentSoft = Color.FromArgb(232, 241, 246);
    public static readonly Color Accent = Color.FromArgb(22, 101, 92);
    public static readonly Color AccentStrong = Color.FromArgb(20, 92, 84);
    public static readonly Color ControlFill = Color.FromArgb(246, 249, 250);
    public static readonly Color MutedText = Color.FromArgb(80, 96, 104);
    public static readonly Color Border = Color.FromArgb(205, 218, 223);
    public static readonly Color Text = Color.FromArgb(32, 46, 52);

    public static void StyleTabs(TabControl tabs)
    {
        tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
        tabs.Appearance = TabAppearance.FlatButtons;
        tabs.ItemSize = new Size(142, 48);
        tabs.SizeMode = TabSizeMode.Fixed;
        tabs.BackColor = Surface;
        tabs.Margin = new Padding(0, 0, 0, 12);
        tabs.DrawItem += (_, e) =>
        {
            var selected = e.Index == tabs.SelectedIndex;
            var rect = Rectangle.Inflate(tabs.GetTabRect(e.Index), -8, -7);
            rect.Offset(0, 2);
            using var path = RoundedRect(rect, 8);
            using var fill = new SolidBrush(selected ? Color.White : Color.FromArgb(232, 239, 243));
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.FillPath(fill, path);
            if (selected)
            {
                using var accent = new SolidBrush(Accent);
                e.Graphics.FillRectangle(accent, rect.Left + 18, rect.Bottom - 4, rect.Width - 36, 3);
            }
            TextRenderer.DrawText(e.Graphics, tabs.TabPages[e.Index].Text, tabs.Font, rect, selected ? AccentStrong : Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        };
    }

    public static void StyleRounded(Control control, int radius = ButtonRadius)
    {
        if (control is Button && control.BackColor == SystemColors.Control)
        {
            control.BackColor = Color.FromArgb(249, 251, 252);
        }
        var ownsRoundedRegion = control is Button or Label or Panel or TableLayoutPanel;
        if (ownsRoundedRegion)
        {
            control.SizeChanged += (_, _) => ApplyRoundedRegion(control, radius);
            control.HandleCreated += (_, _) => ApplyRoundedRegion(control, radius);
            ApplyRoundedRegion(control, radius);
        }

        if (control is Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = button.BackColor;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = AccentSoft;
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(218, 233, 238);
            button.ForeColor = Text;
        }
        else if (control is TextBox textBox)
        {
            textBox.BorderStyle = BorderStyle.None;
            textBox.BackColor = ControlFill;
        }
        else if (control is ComboBox comboBox)
        {
            comboBox.FlatStyle = FlatStyle.Flat;
            comboBox.BackColor = ControlFill;
        }
        else if (control is DataGridView grid)
        {
            grid.BorderStyle = BorderStyle.None;
            grid.BackgroundColor = Color.FromArgb(248, 250, 251);
            grid.GridColor = Color.FromArgb(248, 250, 251);
            grid.EnableHeadersVisualStyles = false;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.None;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.ColumnHeadersDefaultCellStyle.BackColor = AccentSoft;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Text;
        }
    }

    public static void ApplyRoundedRegion(Control control, int radius)
    {
        if (control.Width <= 0 || control.Height <= 0) return;
        control.Region?.Dispose();
        using var path = RoundedRect(new Rectangle(0, 0, control.Width, control.Height), Math.Min(radius, Math.Min(control.Width, control.Height) / 2));
        control.Region = new Region(path);
    }

    public static GraphicsPath RoundedRect(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        var diameter = Math.Max(2, radius * 2);
        var arc = new Rectangle(rect.Location, new Size(diameter, diameter));
        path.AddArc(arc, 180, 90);
        arc.X = rect.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = rect.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = rect.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal sealed class RoundedHostPanel : Panel
{
    public int CornerRadius { get; set; } = ModernTheme.ControlRadius;
    public Color FillColor { get; set; } = Color.White;

    public RoundedHostPanel()
    {
        DoubleBuffered = true;
        BackColor = Color.Transparent;
        Padding = new Padding(10, 7, 10, 7);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = ModernTheme.RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), CornerRadius);
        using var fill = new SolidBrush(FillColor);
        e.Graphics.FillPath(fill, path);
    }
}

internal sealed class ModernContextMenuRenderer : ToolStripProfessionalRenderer
{
    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = ModernTheme.RoundedRect(new Rectangle(Point.Empty, e.ToolStrip.Size - new Size(1, 1)), ModernTheme.ButtonRadius);
        using var brush = new SolidBrush(Color.White);
        e.Graphics.FillPath(brush, path);
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        if (!e.Item.Selected)
        {
            base.OnRenderMenuItemBackground(e);
            return;
        }

        var rect = new Rectangle(4, 2, e.Item.Width - 8, e.Item.Height - 4);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = ModernTheme.RoundedRect(rect, ModernTheme.ControlRadius);
        using var brush = new SolidBrush(ModernTheme.AccentSoft);
        e.Graphics.FillPath(brush, path);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e) { }
}
