using System.Drawing.Drawing2D;
using FlyPPTTimer.Models;
using FlyPPTTimer.Native;
using FlyPPTTimer.Services;

namespace FlyPPTTimer.Forms;

public sealed class TimerOverlayForm : Form
{
    private readonly Label _timeLabel = new();
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

    public TimerOverlayForm(ContextMenuStrip contextMenu, Action<ContextMenuStrip>? showContextMenu = null)
    {
        _contextMenu = contextMenu;
        _showContextMenu = showContextMenu;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        FormBorderStyle = FormBorderStyle.None;
        DoubleBuffered = true;
        Controls.Add(_timeLabel);
        _timeLabel.TextAlign = ContentAlignment.MiddleCenter;
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

    public Screen TargetScreen => _targetScreen;

    public void ApplyConfig(AppConfig config, Screen screen)
    {
        _config = config;
        _targetScreen = screen;
        Size = new Size(Math.Max(120, config.Appearance.Width), Math.Max(48, config.Appearance.Height));
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
        Location = CalculateLocation(screen, Size, config.Placement.Anchor, config.Placement.OffsetXPercent, config.Placement.OffsetYPercent);
        Visible = config.Placement.Visible;
    }

    public void UpdateTime(TimerSnapshot snapshot)
    {
        var showHours = AlertService.ShouldShowHours(snapshot);
        var text = AlertService.Format(snapshot.Display, showHours);
        if (snapshot.Mode == TimerMode.Countdown && snapshot.State == TimerState.Finished)
        {
            text = snapshot.Elapsed > snapshot.Duration && _config.Timer.ContinueOvertime
                ? _config.Appearance.OvertimePrefix + AlertService.Format(snapshot.Elapsed - snapshot.Duration, showHours)
                : AlertService.Format(TimeSpan.Zero, showHours);
        }
        _timeLabel.Visible = true;
        _timeLabel.Text = text;
        if (snapshot.State == TimerState.Finished)
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

    public void Flash(PromptSettings prompt, int seconds)
    {
        _pauseFlashActive = false;
        _flashUntil = DateTime.Now.AddSeconds(Math.Max(1, seconds));
        _flashTimer.Interval = Math.Max(50, _config.Appearance.FlashOnMs);
        _flashTimer.Start();
    }

    private void StartPauseFlash()
    {
        if (_pauseFlashActive) return;
        _pauseFlashActive = true;
        _flashUntil = DateTime.MaxValue;
        _flashTimer.Interval = Math.Max(50, _config.Appearance.FlashOnMs);
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
        var style = _config.Appearance.FlashStyle;
        if (style.Contains("文字"))
        {
            _timeLabel.Visible = _flashVisible;
        }
        if (style.Contains("背景") || style.Contains("边框"))
        {
            BackColor = _flashVisible ? flashBack : _normalBack;
        }
        Invalidate();
        _flashTimer.Interval = _flashVisible ? Math.Max(50, _config.Appearance.FlashOnMs) : Math.Max(50, _config.Appearance.FlashOffMs);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var style = _config.Appearance.FlashStyle;
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
        var area = screen.WorkingArea;
        var x = anchor switch
        {
            OverlayAnchor.TopCenter or OverlayAnchor.Center or OverlayAnchor.BottomCenter => area.Left + (area.Width - size.Width) / 2,
            OverlayAnchor.TopRight or OverlayAnchor.MiddleRight or OverlayAnchor.BottomRight => area.Right - size.Width,
            _ => area.Left
        };
        var y = anchor switch
        {
            OverlayAnchor.MiddleLeft or OverlayAnchor.Center or OverlayAnchor.MiddleRight => area.Top + (area.Height - size.Height) / 2,
            OverlayAnchor.BottomLeft or OverlayAnchor.BottomCenter or OverlayAnchor.BottomRight => area.Bottom - size.Height,
            _ => area.Top
        };

        x += (int)Math.Round(area.Width * (double)offsetXPercent / 100d);
        y += (int)Math.Round(area.Height * (double)offsetYPercent / 100d);
        x = Math.Clamp(x, area.Left, Math.Max(area.Left, area.Right - size.Width));
        y = Math.Clamp(y, area.Top, Math.Max(area.Top, area.Bottom - size.Height));
        return new Point(x, y);
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

public sealed record OverlayMovedEventArgs(Point Location, Screen Screen);
