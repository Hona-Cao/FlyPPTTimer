namespace FlyPPTTimer.Forms;

internal sealed class RemoteAddressSelector : RemoteSurface
{
    private readonly Label _value;

    public string SelectedAddress { get; private set; } = "";

    public RemoteAddressSelector()
    {
        Height = RemoteDashboardTheme.InputHeight;
        MinimumSize = new Size(0, RemoteDashboardTheme.InputHeight);
        FillColor = RemoteDashboardTheme.ReadOnlyField;
        BorderColor = RemoteDashboardTheme.Border;
        CornerRadius = RemoteDashboardTheme.ControlRadius;
        Padding = new Padding(1);
        TabStop = false;

        _value = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = RemoteDashboardTheme.CreateFont(9.5F),
            ForeColor = RemoteDashboardTheme.MutedText,
            BackColor = RemoteDashboardTheme.ReadOnlyField,
            AutoEllipsis = true,
            UseCompatibleTextRendering = false,
            Padding = new Padding(10, 0, 6, 0),
            Margin = Padding.Empty
        };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 1,
            BackColor = RemoteDashboardTheme.ReadOnlyField,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(_value, 0, 0);
        Controls.Add(layout);
        UpdateValue();
        Services.Localization.Attach(this);
    }

    public void SetAddresses(IEnumerable<string> addresses, string? preferred)
    {
        var values = addresses.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        SelectedAddress = !string.IsNullOrWhiteSpace(preferred) && values.Contains(preferred, StringComparer.OrdinalIgnoreCase)
            ? preferred
            : values.FirstOrDefault() ?? "";
        UpdateValue();
    }

    private void UpdateValue()
    {
        _value.Text = string.IsNullOrWhiteSpace(SelectedAddress) ? "未检测到局域网地址" : SelectedAddress;
        _value.AccessibleName = string.IsNullOrWhiteSpace(SelectedAddress) ? "未检测到局域网地址" : $"当前地址 {SelectedAddress}";
    }
}
