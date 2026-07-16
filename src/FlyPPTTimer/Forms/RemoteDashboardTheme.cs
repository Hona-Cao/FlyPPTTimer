using System.Drawing.Drawing2D;

namespace FlyPPTTimer.Forms;

/// <summary>
/// Visual tokens used only by the redesigned remote-control window.
/// Keeping these tokens separate prevents the redesign from changing unrelated forms.
/// </summary>
internal static class RemoteDashboardTheme
{
    public const int SidebarWidth = 188;
    public const int PagePadding = 24;
    public const int CardRadius = 12;
    public const int ControlRadius = 8;
    public const int InputHeight = 44;
    public const int ButtonHeight = 42;
    public const int CompactHeight = 34;
    public const int NavigationHeight = 52;
    public const int PresentationRowHeight = 112;

    public static readonly Color Window = Color.FromArgb(247, 249, 252);
    public static readonly Color Sidebar = Color.FromArgb(252, 253, 255);
    public static readonly Color Card = Color.White;
    public static readonly Color CardMuted = Color.FromArgb(249, 250, 252);
    public static readonly Color Border = Color.FromArgb(222, 228, 236);
    public static readonly Color BorderStrong = Color.FromArgb(196, 208, 224);
    public static readonly Color Text = Color.FromArgb(20, 31, 50);
    public static readonly Color MutedText = Color.FromArgb(96, 110, 132);
    public static readonly Color SubtleText = Color.FromArgb(126, 139, 158);

    public static readonly Color Accent = Color.FromArgb(37, 99, 235);
    public static readonly Color AccentHover = Color.FromArgb(29, 78, 216);
    public static readonly Color AccentPressed = Color.FromArgb(30, 64, 175);
    public static readonly Color AccentSoft = Color.FromArgb(237, 244, 255);
    public static readonly Color AccentBorder = Color.FromArgb(159, 190, 246);

    public static readonly Color Success = Color.FromArgb(16, 145, 82);
    public static readonly Color SuccessSoft = Color.FromArgb(231, 248, 239);
    public static readonly Color Warning = Color.FromArgb(180, 104, 8);
    public static readonly Color WarningSoft = Color.FromArgb(255, 247, 224);
    public static readonly Color Danger = Color.FromArgb(220, 38, 38);
    public static readonly Color DangerHover = Color.FromArgb(185, 28, 28);
    public static readonly Color DangerSoft = Color.FromArgb(254, 242, 242);
    public static readonly Color Info = Color.FromArgb(29, 78, 216);
    public static readonly Color InfoSoft = Color.FromArgb(239, 246, 255);

    public static Font CreateFont(float size, FontStyle style = FontStyle.Regular) =>
        new("Microsoft YaHei UI", size, style, GraphicsUnit.Point);

    public static void StyleButton(Button button, ButtonKind kind = ButtonKind.Secondary)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = kind is ButtonKind.Secondary or ButtonKind.DangerOutline ? 1 : 0;
        button.Height = ButtonHeight;
        button.MinimumSize = new Size(0, ButtonHeight);
        button.Cursor = Cursors.Hand;
        button.UseVisualStyleBackColor = false;
        button.UseCompatibleTextRendering = false;
        button.Padding = new Padding(14, 0, 14, 0);
        button.Font = CreateFont(9.5F);

        switch (kind)
        {
            case ButtonKind.Primary:
                button.BackColor = Accent;
                button.ForeColor = Color.White;
                button.FlatAppearance.BorderColor = Accent;
                button.FlatAppearance.MouseOverBackColor = AccentHover;
                button.FlatAppearance.MouseDownBackColor = AccentPressed;
                break;
            case ButtonKind.Danger:
                button.BackColor = Danger;
                button.ForeColor = Color.White;
                button.FlatAppearance.BorderColor = Danger;
                button.FlatAppearance.MouseOverBackColor = DangerHover;
                button.FlatAppearance.MouseDownBackColor = Color.FromArgb(153, 27, 27);
                break;
            case ButtonKind.DangerOutline:
                button.BackColor = Color.White;
                button.ForeColor = Danger;
                button.FlatAppearance.BorderColor = Color.FromArgb(248, 113, 113);
                button.FlatAppearance.MouseOverBackColor = DangerSoft;
                button.FlatAppearance.MouseDownBackColor = Color.FromArgb(254, 226, 226);
                break;
            case ButtonKind.Ghost:
                button.BackColor = Color.Transparent;
                button.ForeColor = MutedText;
                button.FlatAppearance.BorderColor = Color.Transparent;
                button.FlatAppearance.MouseOverBackColor = CardMuted;
                button.FlatAppearance.MouseDownBackColor = Color.FromArgb(240, 243, 248);
                break;
            default:
                button.BackColor = Color.White;
                button.ForeColor = Text;
                button.FlatAppearance.BorderColor = BorderStrong;
                button.FlatAppearance.MouseOverBackColor = CardMuted;
                button.FlatAppearance.MouseDownBackColor = Color.FromArgb(240, 243, 248);
                break;
        }

        ApplyRoundedRegion(button, ControlRadius);
        button.SizeChanged += (_, _) => ApplyRoundedRegion(button, ControlRadius);
        button.HandleCreated += (_, _) => ApplyRoundedRegion(button, ControlRadius);
    }

    public static void ApplyRoundedRegion(Control control, int radius)
    {
        if (control.Width <= 0 || control.Height <= 0 || control.IsDisposed) return;
        control.Region?.Dispose();
        using var path = RoundedRectangle(new Rectangle(0, 0, control.Width, control.Height), radius);
        control.Region = new Region(path);
    }

    public static GraphicsPath RoundedRectangle(Rectangle rectangle, int radius)
    {
        var path = new GraphicsPath();
        if (rectangle.Width <= 1 || rectangle.Height <= 1)
        {
            path.AddRectangle(rectangle);
            return path;
        }

        var safeRadius = Math.Max(1, Math.Min(radius, Math.Min(rectangle.Width, rectangle.Height) / 2));
        var diameter = safeRadius * 2;
        var arc = new Rectangle(rectangle.Left, rectangle.Top, diameter, diameter);
        path.AddArc(arc, 180, 90);
        arc.X = rectangle.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = rectangle.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = rectangle.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal enum ButtonKind
{
    Primary,
    Secondary,
    Ghost,
    Danger,
    DangerOutline
}

/// <summary>A lightweight rounded surface with a one-pixel border.</summary>
internal class RemoteCard : Panel
{
    private Color _fillColor = RemoteDashboardTheme.Card;
    private Color _borderColor = RemoteDashboardTheme.Border;
    private int _cornerRadius = RemoteDashboardTheme.CardRadius;

    public Color FillColor
    {
        get => _fillColor;
        set { _fillColor = value; Invalidate(); }
    }

    public Color BorderColor
    {
        get => _borderColor;
        set { _borderColor = value; Invalidate(); }
    }

    public int CornerRadius
    {
        get => _cornerRadius;
        set { _cornerRadius = value; Invalidate(); }
    }

    public RemoteCard()
    {
        DoubleBuffered = true;
        BackColor = Color.Transparent;
        Padding = new Padding(1);
        Resize += (_, _) => Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (Width <= 1 || Height <= 1) return;

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = RemoteDashboardTheme.RoundedRectangle(bounds, CornerRadius);
        using var fill = new SolidBrush(FillColor);
        using var border = new Pen(BorderColor);
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(border, path);
    }
}
