using System.Drawing.Drawing2D;
using System.Drawing.Text;
using FlyPPTTimer.Models;
using FlyPPTTimer.Native;
using FlyPPTTimer.Services;

namespace FlyPPTTimer.Forms;

public sealed class TimerOverlayForm : Form
{
    private readonly TimeDisplayControl _timeLabel = new();
    private readonly System.Windows.Forms.Timer _flashTimer = new();
    private AppConfig _config = new();
    private bool _dragging;
    private Point _dragStart;
    private bool _flashVisible = true;
    private Color _normalBack;
    private Color _normalText;
    private DateTime _flashUntil = DateTime.MinValue;
    private Screen _targetScreen = Screen.PrimaryScreen!;
    private readonly ContextMenuStrip _contextMenu;
    private readonly Action<ContextMenuStrip>? _showContextMenu;
    private bool _pauseFlashActive;
    private string _activeFlashStyle = "闪烁背景";
    private int _activeFlashOnMs = 350;
    private int _activeFlashOffMs = 350;

    public TimerOverlayForm(ContextMenuStrip contextMenu, Action<ContextMenuStrip>? showContextMenu = null)
    {
        _contextMenu = contextMenu;
        _showContextMenu = showContextMenu;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        // Overlay dimensions are 96-DPI logical units and are converted explicitly
        // for the target monitor. Disabling WinForms autoscaling prevents Show()
        // from resizing only the right/bottom edges after placement is calculated.
        AutoScaleMode = AutoScaleMode.None;
        FormBorderStyle = FormBorderStyle.None;
        DoubleBuffered = true;
        Controls.Add(_timeLabel);
        _timeLabel.Dock = DockStyle.Fill;
        _flashTimer.Tick += (_, _) => UpdateFlash();
        MouseDown += StartDrag;
        MouseMove += DragMove;
        MouseUp += EndDragOrShowMenu;
        _timeLabel.MouseDown += StartDrag;
        _timeLabel.MouseMove += DragMove;
        _timeLabel.MouseUp += EndDragOrShowMenu;
    }

    public event EventHandler<OverlayMovedEventArgs>? PositionChangedByUser;
    public event EventHandler<OverlaySizeExpansionEventArgs>? SizeExpansionRequested;

    public Screen TargetScreen => _targetScreen;

    public void ApplyConfig(AppConfig config, Screen screen, PointF? preservedCenter = null)
    {
        _config = config;
        _targetScreen = screen;
        var dpi = RemoteScreenDpiProvider.FromScreen(screen).Dpi;
        Size = RemoteWindowLayoutService.DipToPhysical(
            new Size(Math.Max(1, config.Appearance.Width), Math.Max(1, config.Appearance.Height)),
            dpi);
        TopMost = config.Appearance.AlwaysOnTop;
        Opacity = Math.Clamp(config.Appearance.BackgroundOpacity, 10, 100) / 100d;
        FormBorderStyle = FormBorderStyle.None;
        _normalBack = ParseColor(config.Appearance.BackgroundColor, Color.FromArgb(32, 39, 51));
        _normalText = ParseColor(config.Appearance.TextColor, Color.White);
        BackColor = _normalBack;
        _timeLabel.ForeColor = _normalText;
        _timeLabel.Font = new Font(config.Appearance.FontFamily, config.Appearance.FontSize, ParseFontStyle(config.Appearance.FontStyle), GraphicsUnit.Point);
        ApplyClickThrough(config.Controls.ClickThrough);
        ApplyRegion();
        Location = preservedCenter.HasValue
            ? LocationFromCenter(preservedCenter.Value, Size, screen.WorkingArea)
            : CalculateLocation(screen, Size, config.Placement.Anchor, config.Placement.OffsetXPercent, config.Placement.OffsetYPercent);
        Visible = config.Placement.Visible;
    }

    public void UpdateTime(TimerSnapshot snapshot)
    {
        var showHours = AlertService.ShouldShowHours(snapshot);
        var text = AlertService.Format(snapshot.Display, showHours);
        if (snapshot.Mode == TimerMode.Countdown && (snapshot.State == TimerState.Finished || snapshot.IsOvertime))
        {
            text = snapshot.IsOvertime
                ? _config.Appearance.OvertimePrefix + AlertService.Format(snapshot.Elapsed - snapshot.Duration, showHours)
                : AlertService.Format(TimeSpan.Zero, showHours);
        }
        _timeLabel.Visible = true;
        _timeLabel.Text = text;
        using var graphics = _timeLabel.CreateGraphics();
        var requiredWidth = CalculateRequiredWidth(text, _timeLabel.Font, graphics);
        var requiredHeight = CalculateRequiredHeight(text, _timeLabel.Font, graphics);
        if (requiredWidth > Width || requiredHeight > Height)
            SizeExpansionRequested?.Invoke(this, new OverlaySizeExpansionEventArgs(
                Math.Max(Width, requiredWidth),
                Math.Max(Height, requiredHeight)));
        if (snapshot.State == TimerState.Finished || snapshot.IsOvertime)
        {
            BackColor = ParseColor(_config.Appearance.TimeoutBackgroundColor, Color.Firebrick);
            _timeLabel.ForeColor = ParseColor(_config.Appearance.TimeoutTextColor, Color.White);
        }
        else if (_flashUntil < DateTime.Now)
        {
            BackColor = _normalBack;
            _timeLabel.ForeColor = _normalText;
        }

        if (snapshot.State == TimerState.Paused && _config.Behavior.FlashPausedTime)
        {
            StartPauseFlash();
        }
        else if (_pauseFlashActive)
        {
            StopFlash();
        }
    }

    public void ReassertTopMost()
    {
        if (!Visible || !_config.Appearance.AlwaysOnTop) return;
        TopMost = false;
        TopMost = true;
    }

    public void Flash(PromptSettings prompt, int seconds)
    {
        _activeFlashStyle = string.IsNullOrWhiteSpace(prompt.FlashStyle) ? "闪烁背景" : prompt.FlashStyle;
        if (_activeFlashStyle == "无" || seconds <= 0) return;
        _pauseFlashActive = false;
        _flashUntil = DateTime.Now.AddSeconds(Math.Max(1, seconds));
        _activeFlashOnMs = Math.Max(50, prompt.FlashOnMs);
        _activeFlashOffMs = Math.Max(50, prompt.FlashOffMs);
        _flashTimer.Interval = _activeFlashOnMs;
        _flashTimer.Start();
    }

    private void StartPauseFlash()
    {
        if (_pauseFlashActive) return;
        _pauseFlashActive = true;
        _flashUntil = DateTime.MaxValue;
        _activeFlashStyle = _config.Appearance.FlashStyle;
        _activeFlashOnMs = Math.Max(50, _config.Appearance.FlashOnMs);
        _activeFlashOffMs = Math.Max(50, _config.Appearance.FlashOffMs);
        _flashTimer.Interval = _activeFlashOnMs;
        _flashTimer.Start();
    }

    private void StopFlash()
    {
        _pauseFlashActive = false;
        _flashTimer.Stop();
        _flashUntil = DateTime.MinValue;
        _flashVisible = true;
        _timeLabel.Visible = true;
        BackColor = _normalBack;
        _timeLabel.ForeColor = _normalText;
        Invalidate();
    }

    private void UpdateFlash()
    {
        if (DateTime.Now >= _flashUntil)
        {
            _flashTimer.Stop();
            _pauseFlashActive = false;
            _flashVisible = true;
            _timeLabel.Visible = true;
            BackColor = _normalBack;
            _timeLabel.ForeColor = _normalText;
            Invalidate();
            return;
        }

        _flashVisible = !_flashVisible;
        var flashBack = ParseColor(_config.Appearance.FlashBackgroundColor, Color.Gold);
        var style = _activeFlashStyle;
        if (style.Contains("文字"))
        {
            _timeLabel.Visible = _flashVisible;
        }
        if (style.Contains("背景") || style.Contains("边框"))
        {
            BackColor = _flashVisible ? flashBack : _normalBack;
        }
        Invalidate();
        _flashTimer.Interval = _flashVisible ? _activeFlashOnMs : _activeFlashOffMs;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var style = _activeFlashStyle;
        if (!style.Contains("边框")) return;
        using var pen = new Pen(ParseColor(_config.Appearance.FlashBackgroundColor, Color.Gold), 3);
        e.Graphics.DrawRectangle(pen, new Rectangle(1, 1, Width - 3, Height - 3));
    }

    private void StartDrag(object? sender, MouseEventArgs e)
    {
        if (_config.Controls.LockPosition || e.Button != MouseButtons.Left) return;
        _dragging = true;
        _dragStart = e.Location;
    }

    private void DragMove(object? sender, MouseEventArgs e)
    {
        if (!_dragging) return;
        Location = new Point(Left + e.X - _dragStart.X, Top + e.Y - _dragStart.Y);
        PositionChangedByUser?.Invoke(this, new OverlayMovedEventArgs(Location, _targetScreen));
    }

    private void EndDragOrShowMenu(object? sender, MouseEventArgs e)
    {
        _dragging = false;
        if (e.Button != MouseButtons.Right) return;
        _contextMenu.Close();
        if (_showContextMenu is not null) _showContextMenu(_contextMenu);
        else _contextMenu.Show(Cursor.Position, ToolStripDropDownDirection.AboveLeft);
    }

    private void ApplyClickThrough(bool enabled)
    {
        if (!IsHandleCreated) return;
        var style = NativeMethods.GetWindowLong(Handle, NativeMethods.GwlExStyle);
        style |= NativeMethods.WsExLayered;
        style = enabled ? style | NativeMethods.WsExTransparent : style & ~NativeMethods.WsExTransparent;
        NativeMethods.SetWindowLong(Handle, NativeMethods.GwlExStyle, style);
    }

    private void ApplyRegion()
    {
        if (!_config.Appearance.Shape.Contains("圆角"))
        {
            Region = null;
            return;
        }
        var radius = _config.Appearance.Shape.Contains("大") ? 28 : 14;
        using var path = new GraphicsPath();
        path.AddArc(0, 0, radius, radius, 180, 90);
        path.AddArc(Width - radius, 0, radius, radius, 270, 90);
        path.AddArc(Width - radius, Height - radius, radius, radius, 0, 90);
        path.AddArc(0, Height - radius, radius, radius, 90, 90);
        path.CloseAllFigures();
        Region = new Region(path);
    }

    public static Point CalculateLocation(Screen screen, Size size, OverlayAnchor anchor, decimal offsetXPercent, decimal offsetYPercent)
    {
        return LocationFromCenter(CalculateOrigin(screen, anchor, offsetXPercent, offsetYPercent), size, screen.WorkingArea);
    }

    public static PointF CalculateOrigin(Screen screen, OverlayAnchor anchor, decimal offsetXPercent, decimal offsetYPercent)
    {
        var area = screen.WorkingArea;
        var dpi = RemoteScreenDpiProvider.FromScreen(screen).Dpi;
        return CalculateOrigin(area, dpi, anchor, offsetXPercent, offsetYPercent);
    }

    internal static PointF CalculateOrigin(Rectangle area, int dpi, OverlayAnchor anchor, decimal offsetXPercent, decimal offsetYPercent)
    {
        var baseline = RemoteWindowLayoutService.DipToPhysical(new Size(140, 50), dpi);
        var x = anchor switch
        {
            OverlayAnchor.TopCenter or OverlayAnchor.Center or OverlayAnchor.BottomCenter => area.Left + area.Width / 2f,
            OverlayAnchor.TopRight or OverlayAnchor.MiddleRight or OverlayAnchor.BottomRight => area.Right - baseline.Width / 2f,
            _ => area.Left + baseline.Width / 2f
        };
        var y = anchor switch
        {
            OverlayAnchor.MiddleLeft or OverlayAnchor.Center or OverlayAnchor.MiddleRight => area.Top + area.Height / 2f,
            OverlayAnchor.BottomLeft or OverlayAnchor.BottomCenter or OverlayAnchor.BottomRight => area.Bottom - baseline.Height / 2f,
            _ => area.Top + baseline.Height / 2f
        };

        x += (float)(area.Width * (double)offsetXPercent / 100d);
        y += (float)(area.Height * (double)offsetYPercent / 100d);
        return new PointF(x, y);
    }

    public static Point LocationFromCenter(PointF center, Size size, Rectangle workingArea)
    {
        var x = (int)Math.Round(center.X - size.Width / 2f, MidpointRounding.AwayFromZero);
        var y = (int)Math.Round(center.Y - size.Height / 2f, MidpointRounding.AwayFromZero);
        return new Point(x, y);
    }

    public PointF CenterPoint => new(Left + Width / 2f, Top + Height / 2f);

    internal static int CalculateRequiredWidth(string text, Font font)
    {
        using var bitmap = new Bitmap(1, 1);
        using var graphics = Graphics.FromImage(bitmap);
        return CalculateRequiredWidth(text, font, graphics);
    }

    private static int CalculateRequiredWidth(string text, Font font, Graphics graphics)
    {
        var layout = TimeDisplayControl.MeasureLogicalLayout(text, font, graphics);
        var balancedWidth = 2f * Math.Max(
            layout.AnchorOffset - layout.InkBounds.Left,
            layout.InkBounds.Right - layout.AnchorOffset);
        var gdiWidth = TextRenderer.MeasureText(graphics, text, font, Size.Empty,
            TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width;
        return Math.Max(1, Math.Max((int)Math.Ceiling(balancedWidth), gdiWidth) + 4);
    }

    internal static int CalculateRequiredHeight(string text, Font font)
    {
        using var bitmap = new Bitmap(1, 1);
        using var graphics = Graphics.FromImage(bitmap);
        return CalculateRequiredHeight(text, font, graphics);
    }

    private static int CalculateRequiredHeight(string text, Font font, Graphics graphics)
    {
        var layout = TimeDisplayControl.MeasureLogicalLayout(text, font, graphics);
        return Math.Max(1, (int)Math.Ceiling(layout.InkBounds.Height) + 4);
    }

    private static Color ParseColor(string value, Color fallback)
    {
        try { return ColorTranslator.FromHtml(value); }
        catch { return fallback; }
    }

    private static FontStyle ParseFontStyle(string value)
    {
        return Enum.TryParse<FontStyle>(value, true, out var style) ? style : FontStyle.Bold;
    }
}

internal sealed class TimeDisplayControl : Control
{
    public TimeDisplayControl()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
    }

    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        Invalidate();
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        Invalidate();
    }

    protected override void OnForeColorChanged(EventArgs e)
    {
        base.OnForeColorChanged(e);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (string.IsNullOrEmpty(Text)) return;
        TextRenderer.DrawText(
            e.Graphics,
            Text,
            Font,
            ClientRectangle,
            ForeColor,
            CenteredSingleLineFlags);
    }

    internal static LogicalTimeLayout MeasureLogicalLayout(string text, Font font)
    {
        using var bitmap = new Bitmap(1, 1);
        using var graphics = Graphics.FromImage(bitmap);
        return MeasureLogicalLayout(text, font, graphics);
    }

    internal static PointF CalculateDrawOrigin(Size clientSize, LogicalTimeLayout layout) => new(
        clientSize.Width / 2f - layout.AnchorOffset,
        clientSize.Height / 2f - (layout.InkBounds.Top + layout.InkBounds.Bottom) / 2f);

    internal static LogicalTimeLayout MeasureLogicalLayout(string text, Font font, Graphics graphics)
    {
        var measured = TextRenderer.MeasureText(graphics, text, font, Size.Empty, MeasureSingleLineFlags);
        var bounds = new RectangleF(0, 0, measured.Width, measured.Height);
        return new LogicalTimeLayout(bounds, measured.Width / 2f);
    }

    private const TextFormatFlags MeasureSingleLineFlags =
        TextFormatFlags.SingleLine | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix;

    private const TextFormatFlags CenteredSingleLineFlags =
        MeasureSingleLineFlags | TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
        TextFormatFlags.PreserveGraphicsClipping;
}

internal readonly record struct LogicalTimeLayout(RectangleF InkBounds, float AnchorOffset)
{
    public float Width => InkBounds.Width;
    public float Height => InkBounds.Height;
    public float InkCenterY => (InkBounds.Top + InkBounds.Bottom) / 2f;
}

public sealed record OverlayMovedEventArgs(Point Location, Screen Screen);
public sealed record OverlaySizeExpansionEventArgs(int RequiredWidth, int RequiredHeight);
