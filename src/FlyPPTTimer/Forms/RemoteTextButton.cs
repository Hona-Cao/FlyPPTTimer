using System.Drawing.Drawing2D;

namespace FlyPPTTimer.Forms;

/// <summary>Fully owner-drawn text button; native WinForms button chrome never participates.</summary>
internal sealed class RemoteTextButton : Control, IButtonControl
{
    public const int DefaultButtonHeight = 40;
    private bool _hovered;
    private bool _pressed;
    private bool _isDefault;
    private RemoteButtonKind _kind = RemoteButtonKind.Secondary;
    private bool _selected;

    public RemoteButtonKind Kind
    {
        get => _kind;
        set { _kind = value; Invalidate(); }
    }

    public bool Selected
    {
        get => _selected;
        set { _selected = value; Invalidate(); }
    }

    public ContentAlignment TextAlign { get; set; } = ContentAlignment.MiddleCenter;
    public DialogResult DialogResult { get; set; } = DialogResult.None;

    public RemoteTextButton()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.Selectable,
            true);
        // Mouse activation is raised manually in OnMouseUp. Disabling the
        // standard Control click prevents one physical click toggling twice.
        SetStyle(ControlStyles.StandardClick | ControlStyles.StandardDoubleClick, false);
        AutoSize = false;
        Height = DefaultButtonHeight;
        MinimumSize = new Size(0, DefaultButtonHeight);
        Font = RemoteDashboardTheme.CreateFont(9.5F);
        Cursor = Cursors.Hand;
        TabStop = true;
        AccessibleRole = AccessibleRole.PushButton;
        Padding = new Padding(12, 0, 12, 0);
    }

    protected override AccessibleObject CreateAccessibilityInstance()
    {
        AccessibleName = Text;
        return base.CreateAccessibilityInstance();
    }

    public void NotifyDefault(bool value)
    {
        _isDefault = value;
        Invalidate();
    }

    public void PerformClick()
    {
        if (!Enabled) return;
        OnClick(EventArgs.Empty);
        if (DialogResult != DialogResult.None && FindForm() is { } form)
            form.DialogResult = DialogResult;
    }

    protected override bool IsInputKey(Keys keyData) =>
        keyData is Keys.Enter or Keys.Space || base.IsInputKey(keyData);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (Enabled && e.KeyCode is Keys.Enter or Keys.Space)
        {
            _pressed = true;
            e.Handled = true;
            Invalidate();
        }
        base.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        if (_pressed && e.KeyCode is Keys.Enter or Keys.Space)
        {
            _pressed = false;
            e.Handled = true;
            Invalidate();
            PerformClick();
        }
        base.OnKeyUp(e);
    }

    protected override void OnMouseEnter(EventArgs e) { _hovered = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hovered = false; _pressed = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (Enabled && e.Button == MouseButtons.Left)
        {
            Focus();
            _pressed = true;
            Invalidate();
        }
        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        var click = Enabled && _pressed && e.Button == MouseButtons.Left && ClientRectangle.Contains(e.Location);
        _pressed = false;
        Invalidate();
        base.OnMouseUp(e);
        if (click) PerformClick();
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        Cursor = Enabled ? Cursors.Hand : Cursors.Default;
        _pressed = false;
        Invalidate();
        base.OnEnabledChanged(e);
    }

    protected override void OnTextChanged(EventArgs e)
    {
        AccessibleName = Text;
        Invalidate();
        base.OnTextChanged(e);
    }

    protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
    protected override void OnLostFocus(EventArgs e) { _pressed = false; Invalidate(); base.OnLostFocus(e); }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        MinimumSize = new Size(MinimumSize.Width, DefaultButtonHeight);
        Invalidate();
    }

    protected override void OnPaintBackground(PaintEventArgs e) =>
        e.Graphics.Clear(RemoteDashboardTheme.OpaqueParentBackColor(this, RemoteDashboardTheme.Window));

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
        using var path = RemoteDashboardTheme.RoundedPath(bounds, RemoteDashboardTheme.Scale(this, RemoteDashboardTheme.ControlRadius));
        var palette = ResolvePalette();
        using var fill = new SolidBrush(palette.Fill);
        using var border = new Pen(palette.Border, 1F) { Alignment = PenAlignment.Inset };
        e.Graphics.FillPath(fill, path);
        if (palette.Border.A > 0) e.Graphics.DrawPath(border, path);

        if ((Focused && ShowFocusCues) || _isDefault)
        {
            var focusBounds = RectangleF.Inflate(bounds, -3F, -3F);
            using var focusPath = RemoteDashboardTheme.RoundedPath(focusBounds, Math.Max(2, RemoteDashboardTheme.Scale(this, RemoteDashboardTheme.ControlRadius) - 2));
            using var focusPen = new Pen(Color.FromArgb(147, 197, 253), 1F) { Alignment = PenAlignment.Inset };
            e.Graphics.DrawPath(focusPen, focusPath);
        }

        var safeInset = RemoteDashboardTheme.Scale(this, 2);
        var horizontalInset = Math.Max(safeInset, Padding.Left);
        var textBounds = new Rectangle(
            horizontalInset,
            safeInset,
            Math.Max(1, ClientSize.Width - horizontalInset - Math.Max(safeInset, Padding.Right)),
            Math.Max(1, ClientSize.Height - safeInset * 2));
        var flags = TextFormatFlags.SingleLine |
                    TextFormatFlags.VerticalCenter |
                    TextFormatFlags.HorizontalCenter |
                    TextFormatFlags.EndEllipsis |
                    TextFormatFlags.NoPrefix |
                    TextFormatFlags.GlyphOverhangPadding;
        if (TextAlign == ContentAlignment.MiddleLeft)
            flags = (flags & ~TextFormatFlags.HorizontalCenter) | TextFormatFlags.Left;
        else if (TextAlign == ContentAlignment.MiddleRight)
            flags = (flags & ~TextFormatFlags.HorizontalCenter) | TextFormatFlags.Right;
        TextRenderer.DrawText(e.Graphics, Text, Font, textBounds, palette.Text, flags);
    }

    private (Color Fill, Color Border, Color Text) ResolvePalette()
    {
        if (!Enabled)
            return (Color.FromArgb(244, 246, 249), RemoteDashboardTheme.Border, RemoteDashboardTheme.SubtleText);
        if (Selected)
            return (_pressed ? Color.FromArgb(219, 232, 255) : _hovered ? Color.FromArgb(226, 237, 255) : RemoteDashboardTheme.AccentSoft,
                Color.Transparent, RemoteDashboardTheme.Accent);
        return Kind switch
        {
            RemoteButtonKind.Primary => (_pressed ? RemoteDashboardTheme.AccentPressed : _hovered ? RemoteDashboardTheme.AccentHover : RemoteDashboardTheme.Accent, Color.Transparent, Color.White),
            RemoteButtonKind.Danger => (_pressed ? Color.FromArgb(153, 27, 27) : _hovered ? RemoteDashboardTheme.DangerHover : RemoteDashboardTheme.Danger, Color.Transparent, Color.White),
            RemoteButtonKind.DangerOutline => (_pressed ? Color.FromArgb(254, 226, 226) : _hovered ? RemoteDashboardTheme.DangerSoft : Color.White, Color.FromArgb(244, 139, 139), RemoteDashboardTheme.Danger),
            RemoteButtonKind.Quiet => (_pressed ? Color.FromArgb(234, 238, 244) : _hovered ? Color.FromArgb(242, 245, 249) : RemoteDashboardTheme.Sidebar, Color.Transparent, RemoteDashboardTheme.MutedText),
            _ => (_pressed ? Color.FromArgb(238, 242, 247) : _hovered ? Color.FromArgb(246, 248, 251) : Color.White, RemoteDashboardTheme.BorderStrong, RemoteDashboardTheme.Text)
        };
    }
}
