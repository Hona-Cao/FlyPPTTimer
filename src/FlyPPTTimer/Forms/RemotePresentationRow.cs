using FlyPPTTimer.Models;

namespace FlyPPTTimer.Forms;

/// <summary>Compact text-only row for the v0.16 presentation list.</summary>
internal sealed class RemotePresentationRow : RemoteSurface
{
    private readonly TableLayoutPanel _layout;
    private readonly Label _title;
    private readonly Label _meta;
    private readonly Label _path;
    private readonly Label _status;
    private readonly RemoteTextButton _toggle;
    private bool _updating;
    private bool _selected;
    private bool _hovered;

    public FileRule? CurrentRule { get; private set; }
    public PresentationOption? CurrentPresentation { get; private set; }
    public string CurrentPath { get; private set; } = string.Empty;

    public event EventHandler? Selected;
    public event EventHandler<bool>? EnabledChangedByUser;

    public RemotePresentationRow()
    {
        Height = RemoteDashboardTheme.PresentationRowHeight;
        MinimumSize = new Size(300, RemoteDashboardTheme.PresentationRowHeight);
        Margin = new Padding(0, 0, 0, 8);
        Padding = new Padding(12, 8, 10, 8);
        Cursor = Cursors.Hand;
        FillColor = RemoteDashboardTheme.Card;
        BorderColor = RemoteDashboardTheme.Border;
        CornerRadius = RemoteDashboardTheme.ControlRadius;

        _layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 3,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 76));
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _title = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = RemoteDashboardTheme.CreateFont(9.5F, FontStyle.Bold),
            ForeColor = RemoteDashboardTheme.Text,
            AutoEllipsis = true
        };

        _meta = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = RemoteDashboardTheme.CreateFont(8.5F),
            ForeColor = RemoteDashboardTheme.MutedText,
            AutoEllipsis = true
        };

        _path = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = RemoteDashboardTheme.CreateFont(8.25F),
            ForeColor = RemoteDashboardTheme.SubtleText,
            AutoEllipsis = true
        };

        _status = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = RemoteDashboardTheme.CreateFont(8.5F, FontStyle.Bold),
            AutoEllipsis = true
        };

        _toggle = new RemoteTextButton
        {
            Dock = DockStyle.Fill,
            Kind = RemoteButtonKind.Secondary,
            Font = RemoteDashboardTheme.CreateFont(8.5F),
            Margin = new Padding(4, 1, 0, 1),
            Padding = new Padding(4, 0, 4, 0)
        };
        _toggle.Click += (_, _) =>
        {
            if (_updating || CurrentRule is null) return;
            EnabledChangedByUser?.Invoke(this, !CurrentRule.Enabled);
        };

        _layout.Controls.Add(_title, 0, 0);
        _layout.Controls.Add(_status, 1, 0);
        _layout.SetColumnSpan(_status, 2);
        _layout.Controls.Add(_meta, 0, 1);
        _layout.Controls.Add(_toggle, 2, 1);
        _layout.Controls.Add(_path, 0, 2);
        _layout.SetColumnSpan(_path, 3);
        Controls.Add(_layout);

        WireSelection(this);
        WireHover(this);
    }

    public void Update(FileRule? rule, PresentationOption? option, bool selected, bool exists)
    {
        _updating = true;
        CurrentRule = rule;
        CurrentPresentation = option;
        CurrentPath = rule?.FilePath ?? Path.Combine(option?.Directory ?? string.Empty, option?.Name ?? string.Empty);
        _selected = selected;

        var showing = option?.IsSlideShowRunning == true;
        var open = option?.IsOpen == true;
        var active = option?.IsActive == true;

        _title.Text = rule?.FileName ?? option?.Name ?? "演示文稿";
        _meta.Text = rule?.Duration ?? "无规则";
        _path.Text = CurrentPath;
        _status.Text = !exists ? "缺失"
            : rule is null ? "无规则"
            : !rule.Enabled ? "已禁用"
            : showing ? "放映中"
            : active ? "当前"
            : open ? "已打开"
            : "待打开";

        _status.ForeColor = !exists
            ? RemoteDashboardTheme.Danger
            : showing || active
                ? RemoteDashboardTheme.Accent
                : rule?.Enabled == false
                    ? RemoteDashboardTheme.SubtleText
                    : RemoteDashboardTheme.MutedText;

        _toggle.Visible = rule is not null;
        _toggle.Text = rule?.Enabled == true ? "禁用" : "启用";
        _toggle.Kind = rule?.Enabled == true ? RemoteButtonKind.Secondary : RemoteButtonKind.Primary;
        _updating = false;
        ApplyVisualState();
    }

    private void WireSelection(Control root)
    {
        if (!ReferenceEquals(root, _toggle))
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
