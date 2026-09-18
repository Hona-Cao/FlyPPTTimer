using FlyPPTTimer.Models;
using FlyPPTTimer.Services;

namespace FlyPPTTimer.Forms;

/// <summary>A resizable timer window intended only for an extended display.</summary>
public sealed class BigScreenTimerForm : Form
{
    private readonly TimeDisplayControl _display = new()
    {
        Dock = DockStyle.Fill
    };
    private AppConfig _config = new();
    private Font? _timerFont;

    public BigScreenTimerForm()
    {
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "FlyPPTTimer Big Screen";
        FormBorderStyle = FormBorderStyle.Sizable;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = true;
        MinimizeBox = true;
        MaximizeBox = true;
        ControlBox = true;
        MinimumSize = new Size(640, 360);
        KeyPreview = true;
        DoubleBuffered = true;
        Controls.Add(_display);
        Resize += (_, _) => UpdateDisplayFont();
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape) Hide();
        };
    }

    public void ApplyConfig(AppConfig config, Screen screen)
    {
        if (screen.Primary)
            throw new ArgumentException("The big-screen timer cannot use the primary display.", nameof(screen));

        _config = config;
        WindowState = FormWindowState.Normal;
        Bounds = screen.WorkingArea;
        TopMost = config.Appearance.AlwaysOnTop;
        BackColor = ParseColor(config.Appearance.BackgroundColor, Color.Black);
        _display.BackColor = BackColor;
        UpdateDisplayFont();
        WindowState = FormWindowState.Maximized;
        _display.ForeColor = ParseColor(config.Appearance.TextColor, Color.White);
    }

    private void UpdateDisplayFont()
    {
        if (WindowState == FormWindowState.Minimized || ClientSize.Height <= 0) return;
        var fontSize = Math.Clamp(ClientSize.Height * 0.30F, 48F, 360F);
        var nextFont = new Font(
            string.IsNullOrWhiteSpace(_config.Appearance.FontFamily)
                ? "Microsoft YaHei UI"
                : _config.Appearance.FontFamily,
            fontSize,
            FontStyle.Bold,
            GraphicsUnit.Pixel);
        var previousFont = _timerFont;
        _timerFont = nextFont;
        _display.Font = nextFont;
        previousFont?.Dispose();
    }

    public void UpdateTime(TimerSnapshot snapshot)
    {
        var overtime = snapshot.IsOvertime;
        var showHours = AlertService.ShouldShowHours(snapshot);
        _display.Text = snapshot.Mode == TimerMode.Countdown &&
                        (snapshot.State == TimerState.Finished || overtime)
            ? overtime
                ? _config.Appearance.OvertimePrefix +
                  AlertService.Format(snapshot.Elapsed - snapshot.Duration, showHours)
                : AlertService.Format(TimeSpan.Zero, showHours)
            : AlertService.Format(snapshot.Display, showHours);
        _display.ForeColor = ParseColor(
            overtime ? _config.Appearance.TimeoutTextColor : _config.Appearance.TextColor,
            overtime ? Color.White : Color.Black);
        BackColor = ParseColor(
            overtime ? _config.Appearance.TimeoutBackgroundColor : _config.Appearance.BackgroundColor,
            overtime ? Color.DarkRed : Color.White);
        _display.BackColor = BackColor;
    }

    private static Color ParseColor(string? html, Color fallback)
    {
        try
        {
            return string.IsNullOrWhiteSpace(html)
                ? fallback
                : ColorTranslator.FromHtml(html);
        }
        catch
        {
            return fallback;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timerFont?.Dispose();
            _timerFont = null;
        }
        base.Dispose(disposing);
    }
}
