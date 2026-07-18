namespace FlyPPTTimer.Forms;

internal sealed class RemoteAddressSelector : RemoteSurface
{
    private readonly Label _value;
    private readonly RemoteTextButton _select;
    private readonly ContextMenuStrip _menu;
    private IReadOnlyList<string> _addresses = Array.Empty<string>();

    public event EventHandler? SelectedAddressChanged;
    public string SelectedAddress { get; private set; } = "";

    public RemoteAddressSelector()
    {
        Height = RemoteDashboardTheme.InputHeight;
        MinimumSize = new Size(0, RemoteDashboardTheme.InputHeight);
        FillColor = RemoteDashboardTheme.Field;
        BorderColor = RemoteDashboardTheme.Border;
        CornerRadius = RemoteDashboardTheme.ControlRadius;
        Padding = new Padding(1);
        TabStop = false;

        _value = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = RemoteDashboardTheme.CreateFont(9.5F),
            ForeColor = RemoteDashboardTheme.Text,
            BackColor = RemoteDashboardTheme.Field,
            AutoEllipsis = true,
            UseCompatibleTextRendering = false,
            Padding = new Padding(10, 0, 6, 0),
            Margin = Padding.Empty
        };
        _select = new RemoteTextButton
        {
            Dock = DockStyle.Fill,
            Text = "选择",
            Kind = RemoteButtonKind.Quiet,
            Margin = Padding.Empty,
            Padding = new Padding(8, 0, 8, 0)
        };
        _select.Click += (_, _) => OpenMenu();

        _menu = new ContextMenuStrip
        {
            Renderer = new RemoteMenuRenderer(),
            ShowImageMargin = false,
            ShowCheckMargin = false,
            Font = RemoteDashboardTheme.CreateFont(9.5F)
        };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = RemoteDashboardTheme.Field,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 76));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(_value, 0, 0);
        layout.Controls.Add(_select, 1, 0);
        Controls.Add(layout);
        UpdateValue();
    }

    public void SetAddresses(IEnumerable<string> addresses, string? preferred)
    {
        var values = addresses.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        _addresses = values;
        SelectedAddress = !string.IsNullOrWhiteSpace(preferred) && _addresses.Contains(preferred, StringComparer.OrdinalIgnoreCase)
            ? preferred
            : _addresses.FirstOrDefault() ?? "";
        _select.Enabled = _addresses.Count > 1;
        UpdateValue();
    }

    private void OpenMenu()
    {
        if (_addresses.Count == 0) return;
        _menu.Items.Clear();
        foreach (var address in _addresses)
        {
            var item = new ToolStripMenuItem(address) { Image = null };
            item.Click += (_, _) => SelectAddress(address);
            _menu.Items.Add(item);
        }
        _menu.Show(this, new Point(0, Height));
    }

    private void SelectAddress(string address)
    {
        if (string.Equals(SelectedAddress, address, StringComparison.OrdinalIgnoreCase)) return;
        SelectedAddress = address;
        UpdateValue();
        SelectedAddressChanged?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateValue()
    {
        _value.Text = string.IsNullOrWhiteSpace(SelectedAddress) ? "未检测到局域网地址" : SelectedAddress;
        _value.AccessibleName = string.IsNullOrWhiteSpace(SelectedAddress) ? "未检测到局域网地址" : $"当前地址 {SelectedAddress}";
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _menu.Dispose();
        base.Dispose(disposing);
    }
}
