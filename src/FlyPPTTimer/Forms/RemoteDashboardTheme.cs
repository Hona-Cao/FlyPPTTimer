using System.Drawing.Drawing2D;

namespace FlyPPTTimer.Forms;

/// <summary>
/// Text-first visual system used only by the v0.16 remote-control window.
/// No icon font, decorative glyph, gradient, animation, or shared-theme mutation.
/// </summary>
internal static class RemoteDashboardTheme
{
    public const int SidebarWidth = 174;
    public const int PagePadding = 22;
    public const int CardRadius = 8;
    public const int ControlRadius = 6;
    public const int InputHeight = 42;
    public const int ButtonHeight = 40;
    public const int NavigationHeight = 44;
    public const int PresentationRowHeight = 92;
    public const int PageGap = 14;
    public const int CardGap = 14;
    public const int SectionGap = 12;
    public const int ControlGap = 10;

    public static readonly Color Window = Color.FromArgb(246, 248, 251);
    public static readonly Color Sidebar = Color.FromArgb(252, 253, 255);
    public static readonly Color Card = Color.White;
    public static readonly Color Field = Color.FromArgb(248, 250, 252);
    public static readonly Color Border = Color.FromArgb(218, 224, 232);
    public static readonly Color BorderStrong = Color.FromArgb(194, 204, 218);
    public static readonly Color Text = Color.FromArgb(23, 31, 45);
    public static readonly Color MutedText = Color.FromArgb(91, 103, 123);
    public static readonly Color SubtleText = Color.FromArgb(122, 133, 151);

    public static readonly Color Accent = Color.FromArgb(37, 99, 235);
    public static readonly Color AccentHover = Color.FromArgb(29, 78, 216);
    public static readonly Color AccentPressed = Color.FromArgb(30, 64, 175);
    public static readonly Color AccentSoft = Color.FromArgb(235, 242, 255);

    public static readonly Color Success = Color.FromArgb(15, 128, 75);
    public static readonly Color SuccessSoft = Color.FromArgb(232, 247, 238);
    public static readonly Color Warning = Color.FromArgb(166, 91, 0);
    public static readonly Color WarningSoft = Color.FromArgb(255, 248, 228);
    public static readonly Color Danger = Color.FromArgb(208, 40, 40);
    public static readonly Color DangerHover = Color.FromArgb(178, 28, 28);
    public static readonly Color DangerSoft = Color.FromArgb(255, 240, 240);
    public static readonly Color Info = Color.FromArgb(38, 82, 170);
    public static readonly Color InfoSoft = Color.FromArgb(239, 245, 255);

    public static Font CreateFont(float size, FontStyle style = FontStyle.Regular) =>
        new("Microsoft YaHei UI", size, style, GraphicsUnit.Point);

    public static GraphicsPath RoundedPath(Rectangle bounds, int radius)
        => RoundedPath(new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height), radius);

    public static GraphicsPath RoundedPath(RectangleF bounds, float radius)
    {
        var path = new GraphicsPath();
        if (bounds.Width <= 1 || bounds.Height <= 1)
        {
            path.AddRectangle(bounds);
            return path;
        }

        var safe = Math.Max(1F, Math.Min(radius, Math.Min(bounds.Width, bounds.Height) / 2F));
        var diameter = safe * 2;
        var arc = new RectangleF(bounds.Left, bounds.Top, diameter, diameter);

        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }

    public static int Scale(Control control, int logicalPixels) =>
        Math.Max(1, logicalPixels * Math.Max(96, control.DeviceDpi) / 96);

    public static int GetSafeTextHeight(Control control, Font font, int extraLogicalPixels = 6)
    {
        var measuredHeight = TextRenderer.MeasureText(
            "国Ag端口",
            font,
            Size.Empty,
            TextFormatFlags.SingleLine |
            TextFormatFlags.NoPrefix |
            TextFormatFlags.GlyphOverhangPadding).Height;
        return measuredHeight + Scale(control, extraLogicalPixels);
    }

    public static Color OpaqueParentBackColor(Control control, Color fallback)
    {
        for (Control? current = control.Parent; current is not null; current = current.Parent)
        {
            if (current.BackColor.A == 255)
                return current.BackColor;
        }
        return fallback;
    }
}

internal enum RemoteButtonKind
{
    Primary,
    Secondary,
    Quiet,
    Danger,
    DangerOutline
}

/// <summary>Small rounded surface with one-pixel border and no visual effects.</summary>
internal class RemoteSurface : Panel
{
    private Color _fillColor = RemoteDashboardTheme.Card;
    private Color _borderColor = RemoteDashboardTheme.Border;
    private int _cornerRadius = RemoteDashboardTheme.CardRadius;

    public Color FillColor
    {
        get => _fillColor;
        set
        {
            _fillColor = value;
            BackColor = value;
            Invalidate();
        }
    }

    public Color BorderColor
    {
        get => _borderColor;
        set
        {
            _borderColor = value;
            Invalidate();
        }
    }

    public int CornerRadius
    {
        get => _cornerRadius;
        set
        {
            _cornerRadius = value;
            Invalidate();
        }
    }

    public RemoteSurface()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);
        BackColor = _fillColor;
        Margin = Padding.Empty;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(RemoteDashboardTheme.OpaqueParentBackColor(this, RemoteDashboardTheme.Window));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (Width <= 1 || Height <= 1) return;

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        var bounds = new RectangleF(
            0.5F,
            0.5F,
            Math.Max(1F, Width - 1.0F),
            Math.Max(1F, Height - 1.0F));
        using var path = RemoteDashboardTheme.RoundedPath(bounds, RemoteDashboardTheme.Scale(this, CornerRadius));
        using var fill = new SolidBrush(FillColor);
        using var border = new Pen(BorderColor, 1F) { Alignment = PenAlignment.Inset };
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(border, path);
        base.OnPaint(e);
    }
}

internal sealed class RemoteMenuColorTable : ProfessionalColorTable
{
    public override Color MenuItemSelected => RemoteDashboardTheme.AccentSoft;
    public override Color MenuItemBorder => RemoteDashboardTheme.AccentSoft;
    public override Color MenuItemPressedGradientBegin => RemoteDashboardTheme.AccentSoft;
    public override Color MenuItemPressedGradientEnd => RemoteDashboardTheme.AccentSoft;
    public override Color ToolStripDropDownBackground => Color.White;
    public override Color ImageMarginGradientBegin => Color.White;
    public override Color ImageMarginGradientMiddle => Color.White;
    public override Color ImageMarginGradientEnd => Color.White;
    public override Color SeparatorDark => RemoteDashboardTheme.Border;
    public override Color SeparatorLight => RemoteDashboardTheme.Border;
}

internal sealed class RemoteMenuRenderer : ToolStripProfessionalRenderer
{
    public RemoteMenuRenderer() : base(new RemoteMenuColorTable())
    {
        RoundedEdges = false;
    }
}
