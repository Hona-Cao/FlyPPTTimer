using FlyPPTTimer.Models;

namespace FlyPPTTimer.Forms;

/// <summary>Compact file-name and duration row for the presentation list.</summary>
internal sealed class RemotePresentationRow : RemoteSurface
{
    private readonly TableLayoutPanel _layout;
    private readonly Label _title;
    private readonly Label _duration;
    private bool _selected;
    private bool _hovered;

    public FileRule? CurrentRule { get; private set; }
    public PresentationOption? CurrentPresentation { get; private set; }
    public string CurrentPath { get; private set; } = string.Empty;

    public event EventHandler? Selected;

    public RemotePresentationRow()
    {
        Height = RemoteDashboardTheme.PresentationRowHeight;
        MinimumSize = new Size(0, RemoteDashboardTheme.PresentationRowHeight);
        Margin = new Padding(0, 0, 0, 8);
        Padding = new Padding(12, 8, 10, 8);
        Cursor = Cursors.Hand;
        FillColor = RemoteDashboardTheme.Card;
        BorderColor = RemoteDashboardTheme.Border;
        CornerRadius = RemoteDashboardTheme.ControlRadius;

        _layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _title = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = RemoteDashboardTheme.CreateFont(9.5F, FontStyle.Bold),
            ForeColor = RemoteDashboardTheme.Text,
            AutoEllipsis = true
        };

        _duration = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = RemoteDashboardTheme.CreateFont(8.75F),
            ForeColor = RemoteDashboardTheme.MutedText,
            AutoEllipsis = true
        };

        _layout.Controls.Add(_title, 0, 0);
        _layout.Controls.Add(_duration, 0, 1);
        Controls.Add(_layout);

        WireSelection(this);
        Services.Localization.Attach(this);
        WireHover(this);
    }

    public void Update(FileRule? rule, PresentationOption? option, bool selected, bool exists)
    {
        CurrentRule = rule;
        CurrentPresentation = option;
        CurrentPath = rule?.FilePath ??
                      Path.Combine(option?.Directory ?? string.Empty, option?.Name ?? string.Empty);
        _selected = selected;

        var title = Services.Localization.T(!string.IsNullOrWhiteSpace(rule?.FileName)
            ? rule.FileName
            : option?.Name ?? Path.GetFileName(CurrentPath) ?? "演示文稿");
        var duration = Services.Localization.T(rule?.Duration ?? "无规则");
        if (_title.Text != title) _title.Text = title;
        if (_duration.Text != duration) _duration.Text = duration;
        ApplyVisualState();
    }

    private void WireSelection(Control root)
    {
        root.Click += (_, _) => Selected?.Invoke(this, EventArgs.Empty);
        foreach (Control child in root.Controls)
            WireSelection(child);
    }

    private void WireHover(Control root)
    {
        root.MouseEnter += (_, _) =>
        {
            _hovered = true;
            ApplyVisualState();
        };
        root.MouseLeave += (_, _) =>
        {
            if (ClientRectangle.Contains(PointToClient(Cursor.Position))) return;
            _hovered = false;
            ApplyVisualState();
        };

        foreach (Control child in root.Controls)
            WireHover(child);
    }

    private void ApplyVisualState()
    {
        FillColor = _selected
            ? RemoteDashboardTheme.AccentSoft
            : _hovered ? Color.FromArgb(249, 251, 254) : RemoteDashboardTheme.Card;
        BorderColor = _selected ? RemoteDashboardTheme.Accent : RemoteDashboardTheme.Border;
        _layout.BackColor = Color.Transparent;
    }
}
