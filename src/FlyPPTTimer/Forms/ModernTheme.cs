using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace FlyPPTTimer.Forms;

internal static class ModernTheme
{
    public const int StandardControlHeight = 42;
    public const int WindowRadius = 8;
    public const int CardRadius = 7;
    public const int ControlRadius = 5;
    public const int ButtonRadius = 6;
    public static readonly Color Surface = Color.FromArgb(244, 247, 249);
    public static readonly Color Card = Color.White;
    public static readonly Color HeaderFill = Color.FromArgb(231, 243, 240);
    public static readonly Color SectionFill = Color.FromArgb(232, 241, 246);
    public static readonly Color AccentSoft = Color.FromArgb(224, 241, 237);
    public static readonly Color Accent = Color.FromArgb(16, 112, 99);
    public static readonly Color AccentStrong = Color.FromArgb(12, 87, 78);
    public static readonly Color ControlFill = Color.FromArgb(241, 248, 252);
    public static readonly Color ReadOnlyFill = Color.FromArgb(229, 236, 240);
    public static readonly Color ReadOnlyText = Color.FromArgb(82, 98, 106);
    public static readonly Color ControlHover = Color.FromArgb(228, 237, 240);
    public static readonly Color MutedText = Color.FromArgb(82, 98, 106);
    public static readonly Color Border = Color.FromArgb(215, 225, 229);
    public static readonly Color Text = Color.FromArgb(27, 42, 48);
    public static readonly Color SuccessSoft = Color.FromArgb(220, 244, 229);
    public static readonly Color Success = Color.FromArgb(23, 120, 69);
    public static readonly Color DangerSoft = Color.FromArgb(249, 232, 233);
    public static readonly Color Danger = Color.FromArgb(164, 55, 64);

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
            button.FlatAppearance.MouseOverBackColor = ControlHover;
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(214, 227, 231);
            button.ForeColor = Text;
            button.Cursor = Cursors.Hand;
            button.UseVisualStyleBackColor = false;
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
    private Color _fillColor = Color.White;
    public Color FillColor
    {
        get => _fillColor;
        set
        {
            if (_fillColor == value) return;
            _fillColor = value;
            Invalidate();
        }
    }

    public RoundedHostPanel()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer,
            true);
        BackColor = ModernTheme.Card;
        Padding = new Padding(10, 7, 10, 7);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent?.BackColor ?? ModernTheme.Card);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (Width <= 0 || Height <= 0) return;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = ModernTheme.RoundedRect(new Rectangle(0, 0, Width, Height), CornerRadius);
        using var fill = new SolidBrush(FillColor);
        e.Graphics.FillPath(fill, path);
        base.OnPaint(e);
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

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        using var pen = new Pen(ModernTheme.Border);
        var y = e.Item.ContentRectangle.Top + e.Item.ContentRectangle.Height / 2;
        e.Graphics.DrawLine(pen, 12, y, e.Item.Width - 12, y);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e) { }
}

internal sealed class ModernComboBox : ComboBox
{
    private const int WmPaint = 0x000F;
    private const int WmNcPaint = 0x0085;
    private const int GwlStyle = -16;
    private const int GwlExStyle = -20;
    private const int WsBorder = 0x00800000;
    private const int WsExClientEdge = 0x00000200;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpFrameChanged = 0x0020;
    private const int ComboRadius = 4;

    public ModernComboBox()
    {
        DropDownStyle = ComboBoxStyle.DropDownList;
        DrawMode = DrawMode.OwnerDrawFixed;
        FlatStyle = FlatStyle.Flat;
        BackColor = ModernTheme.ControlFill;
        ForeColor = ModernTheme.Text;
        ItemHeight = 30;
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            parameters.Style &= ~WsBorder;
            parameters.ExStyle &= ~WsExClientEdge;
            return parameters;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyComboRegion();
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        ApplyComboRegion();
    }

    private void ApplyComboRegion() =>
        ModernTheme.ApplyRoundedRegion(this, Math.Max(3, ComboRadius * Math.Max(96, DeviceDpi) / 96));

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        BackColor = Enabled ? ModernTheme.ControlFill : ModernTheme.ReadOnlyFill;
        Invalidate();
    }

    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        if (e.Index < 0) return;
        var selected = (e.State & DrawItemState.Selected) != 0;
        using var fill = new SolidBrush(selected ? ModernTheme.AccentSoft : ModernTheme.ControlFill);
        e.Graphics.FillRectangle(fill, e.Bounds);
        TextRenderer.DrawText(
            e.Graphics,
            Services.Localization.T(GetItemText(Items[e.Index])),
            Font,
            Rectangle.Inflate(e.Bounds, -12, 0),
            ModernTheme.Text,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
    }

    protected override void OnDropDown(EventArgs e)
    {
        base.OnDropDown(e);
        ApplyDropDownWindowStyle();
    }

    private void ApplyDropDownWindowStyle()
    {
        var info = new ComboBoxInfo { Size = Marshal.SizeOf<ComboBoxInfo>() };
        if (!GetComboBoxInfo(Handle, ref info) || info.ListHandle == IntPtr.Zero) return;

        var style = GetWindowLong(info.ListHandle, GwlStyle) & ~WsBorder;
        var exStyle = GetWindowLong(info.ListHandle, GwlExStyle) & ~WsExClientEdge;
        SetWindowLong(info.ListHandle, GwlStyle, style);
        SetWindowLong(info.ListHandle, GwlExStyle, exStyle);
        SetWindowPos(
            info.ListHandle,
            IntPtr.Zero,
            0,
            0,
            0,
            0,
            SwpNoSize | SwpNoMove | SwpNoZOrder | SwpNoActivate | SwpFrameChanged);

        if (!GetWindowRect(info.ListHandle, out var bounds)) return;
        var radius = Math.Max(4, ComboRadius * Math.Max(96, DeviceDpi) / 96);
        var region = CreateRoundRectRgn(
            0,
            0,
            Math.Max(1, bounds.Right - bounds.Left) + 1,
            Math.Max(1, bounds.Bottom - bounds.Top) + 1,
            radius * 2,
            radius * 2);
        if (region == IntPtr.Zero) return;
        if (SetWindowRgn(info.ListHandle, region, true) == 0)
            DeleteObject(region);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmNcPaint) return;
        if (m.Msg != WmPaint || Width <= 0 || Height <= 0)
        {
            base.WndProc(ref m);
            return;
        }

        var paint = new PaintStruct { Reserved = new byte[32] };
        var deviceContext = BeginPaint(Handle, ref paint);
        if (deviceContext == IntPtr.Zero)
        {
            base.WndProc(ref m);
            return;
        }
        try
        {
            using var graphics = Graphics.FromHdc(deviceContext);
            PaintCollapsedControl(graphics);
        }
        finally
        {
            EndPaint(Handle, ref paint);
        }
        m.Result = IntPtr.Zero;
    }

    private void PaintCollapsedControl(Graphics graphics)
    {
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var fill = new SolidBrush(Enabled ? ModernTheme.ControlFill : ModernTheme.ReadOnlyFill);
        graphics.FillRectangle(fill, ClientRectangle);
        var textRect = new Rectangle(12, 0, Math.Max(0, Width - 48), Height);
        TextRenderer.DrawText(
            graphics,
            Services.Localization.T(Text),
            Font,
            textRect,
            Enabled ? ModernTheme.Text : ModernTheme.ReadOnlyText,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        var centerX = Width - 17;
        var centerY = Height / 2 + 1;
        using var arrow = new SolidBrush(ModernTheme.MutedText);
        graphics.FillPolygon(arrow, new Point[] {
            new Point(centerX - 4, centerY - 2),
            new Point(centerX + 4, centerY - 2),
            new Point(centerX, centerY + 3) });
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PaintStruct
    {
        public IntPtr DeviceContext;
        [MarshalAs(UnmanagedType.Bool)] public bool Erase;
        public NativeRect Paint;
        [MarshalAs(UnmanagedType.Bool)] public bool Restore;
        [MarshalAs(UnmanagedType.Bool)] public bool IncUpdate;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public byte[] Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ComboBoxInfo
    {
        public int Size;
        public NativeRect ItemRect;
        public NativeRect ButtonRect;
        public int ButtonState;
        public IntPtr ComboHandle;
        public IntPtr EditHandle;
        public IntPtr ListHandle;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetComboBoxInfo(IntPtr comboHandle, ref ComboBoxInfo info);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr windowHandle, int index);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr windowHandle, int index, int value);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr windowHandle, out NativeRect bounds);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(
        IntPtr windowHandle,
        IntPtr insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateRoundRectRgn(
        int left,
        int top,
        int right,
        int bottom,
        int ellipseWidth,
        int ellipseHeight);

    [DllImport("user32.dll")]
    private static extern int SetWindowRgn(IntPtr windowHandle, IntPtr region, bool redraw);

    [DllImport("user32.dll")]
    private static extern IntPtr BeginPaint(IntPtr windowHandle, ref PaintStruct paint);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EndPaint(IntPtr windowHandle, ref PaintStruct paint);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(IntPtr handle);
}
