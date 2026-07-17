namespace FlyPPTTimer.Forms;

internal sealed class RemoteAddressSelector : RemoteSurface
{
    private readonly Label _value;
    private readonly RemoteTextButton _select;
    private readonly ContextMenuStrip _menu;
    private IReadOnlyList<string> _addresses = Array.Empty<string>();

    public event EventHandler? SelectedAddressChanged;
    public string SelectedAddress { get; private set; } = "127.0.0.1";

    public RemoteAddressSelector()
    {
        Height = RemoteDashboardTheme.InputHeight;
        MinimumSize = new Size(0, RemoteDashboardTheme.InputHeight);
        FillColor = RemoteDashboardTheme.Field;
        BorderColor = RemoteDashboardTheme.Border;
        CornerRadius = RemoteDashboardTheme.ControlRadius;
        Padding = new Padding(12, 5, 5, 5);
        TabStop = false;

        _value = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = RemoteDashboardTheme.CreateFont(9.5F),
            ForeColor = RemoteDashboardTheme.Text,
            BackColor = RemoteDashboardTheme.Field,
            AutoEllipsis = true,
            UseCompatibleTextRendering = false
        };
        _select = new RemoteTextButton
        {
            Dock = DockStyle.Right,
            Width = 72,
            Text = "选择",
            Kind = RemoteButtonKind.Quiet,
            Margin = Padding.Empty
        };
        _select.Click += (_, _) => OpenMenu();

        _menu = new ContextMenuStrip
        {
            Renderer = new RemoteMenuRenderer(),
            ShowImageMargin = false,
            ShowCheckMargin = false,
            Font = RemoteDashboardTheme.CreateFont(9.5F)
        };
        Controls.Add(_value);
        Controls.Add(_select);
        _select.BringToFront();
        UpdateValue();
    }

    public void SetAddresses(IEnumerable<string> addresses, string? preferred)
    {
        var values = addresses.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        _addresses = values.Length == 0 ? new[] { "127.0.0.1" } : values;
        SelectedAddress = !string.IsNullOrWhiteSpace(preferred) && _addresses.Contains(preferred, StringComparer.OrdinalIgnoreCase)
            ? preferred
            : _addresses[0];
        UpdateValue();
    }

    private void OpenMenu()
    {
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
        _value.Text = SelectedAddress;
        _value.AccessibleName = $"当前地址 {SelectedAddress}";
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _menu.Dispose();
        base.Dispose(disposing);
    }
}
