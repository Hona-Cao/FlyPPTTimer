using FlyPPTTimer.Models;
using FlyPPTTimer.Services;
using QRCoder;
using System.Diagnostics;
using System.Globalization;

namespace FlyPPTTimer.Forms;

public sealed class RemoteControlForm : Form
{
    private AppConfig _config;
    private readonly RemoteControlService _remoteControl;
    private readonly NetworkAddressService _networkAddressService;
    private readonly Action<AppConfig> _saveConfig;

    private readonly PictureBox _qr = new() { SizeMode = PictureBoxSizeMode.Zoom, Dock = DockStyle.Fill };
    private readonly FlatSelectBox _address = new();
    private readonly TextBox _url = new() { ReadOnly = true, BorderStyle = BorderStyle.None };
    private readonly TextBox _port = new() { BorderStyle = BorderStyle.None, TextAlign = HorizontalAlignment.Center };
    private readonly Label _state = new()
    {
        AutoSize = false,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold)
    };
    private readonly Label _hint = new()
    {
        AutoSize = false,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.TopLeft,
        ForeColor = Color.FromArgb(80, 96, 104)
    };
    private readonly Button _toggle = new();
    private readonly ToolTip _toolTip = new()
    {
        AutoPopDelay = 30000,
        InitialDelay = 300,
        ReshowDelay = 100,
        ShowAlways = true
    };

    public RemoteControlForm(AppConfig config, RemoteControlService remoteControl, NetworkAddressService networkAddressService, Action<AppConfig> saveConfig)
    {
        _config = config;
        _remoteControl = remoteControl;
        _networkAddressService = networkAddressService;
        _saveConfig = saveConfig;

        Text = "远程控制";
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = false;
        ClientSize = new Size(900, 660);
        MinimumSize = new Size(720, 560);
        BackColor = ModernTheme.Surface;

        Build();
        RefreshState();
    }

    public void ReloadConfig(AppConfig config)
    {
        _config = config;
        if (!IsDisposed) RefreshState();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _qr.Image?.Dispose();
        base.OnFormClosed(e);
    }

    private void Build()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(18),
            BackColor = ModernTheme.Surface
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 126));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        Controls.Add(root);

        root.Controls.Add(Header(), 0, 0);
        root.Controls.Add(StatusStrip(), 0, 1);
        root.Controls.Add(ConnectionPanel(), 0, 2);
        root.Controls.Add(BottomPanel(), 0, 3);
    }

    private Control Header()
    {
        var panel = Card(new Padding(24, 16, 24, 16));
        panel.BackColor = ModernTheme.HeaderFill;
        panel.ColumnCount = 2;
        panel.RowCount = 1;
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 142));

        var titleStack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            BackColor = ModernTheme.HeaderFill
        };
        titleStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        titleStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        titleStack.Controls.Add(new Label
        {
            Text = "手机遥控",
            Dock = DockStyle.Fill,
            Font = new Font(Font.FontFamily, 21F, FontStyle.Bold),
            ForeColor = ModernTheme.Text,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = Padding.Empty
        }, 0, 0);
        _hint.Text = "同一网络下扫码即可控制开始、暂停、重置和显示状态。";
        titleStack.Controls.Add(_hint, 0, 1);
        panel.Controls.Add(titleStack, 0, 0);

        _toggle.Text = "关闭服务";
        _toggle.Width = 124;
        _toggle.Height = 48;
        _toggle.Click += (_, _) => ToggleService();
        ModernTheme.StyleRounded(_toggle, ModernTheme.ButtonRadius);
        panel.Controls.Add(Center(_toggle, ModernTheme.HeaderFill), 1, 0);
        return panel;
    }

    private Control StatusStrip()
    {
        var panel = Card(new Padding(24, 12, 24, 12));
        panel.ColumnCount = 3;
        panel.RowCount = 1;
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));

        panel.Controls.Add(_state, 0, 0);
        panel.Controls.Add(PortPanel(), 1, 0);
        panel.Controls.Add(FillButton("重启服务", (_, _) => RestartService()), 2, 0);
        return panel;
    }

    private Control PortPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.White,
            Padding = new Padding(8, 0, 8, 0)
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 62));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.Controls.Add(new Label
        {
            Text = "端口",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = Color.FromArgb(80, 96, 104)
        }, 0, 0);
        panel.Controls.Add(Host(_port, new Padding(10, 8, 10, 6), Color.FromArgb(242, 246, 248)), 1, 0);
        return panel;
    }

    private Control ConnectionPanel()
    {
        var panel = Card(new Padding(24, 20, 24, 20));
        panel.ColumnCount = 2;
        panel.RowCount = 1;
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 330));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var qrCard = new Panel { Dock = DockStyle.Fill, BackColor = ModernTheme.ControlFill, Padding = new Padding(14) };
        ModernTheme.StyleRounded(qrCard, ModernTheme.CardRadius);
        qrCard.Controls.Add(_qr);
        panel.Controls.Add(qrCard, 0, 0);

        var side = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 8,
            ColumnCount = 1,
            BackColor = Color.White,
            Padding = new Padding(24, 0, 0, 0)
        };
        side.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        side.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        side.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        side.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        side.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        side.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        side.RowStyles.Add(new RowStyle(SizeType.Absolute, 16));
        side.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        side.Controls.Add(Label("选择本机地址"), 0, 0);
        _address.SelectedIndexChanged += (_, _) => UpdateUrlAndQr();
        side.Controls.Add(Host(_address, new Padding(10, 7, 10, 6), Color.FromArgb(242, 246, 248)), 0, 1);
        side.Controls.Add(Label("访问链接"), 0, 2);
        side.Controls.Add(Host(_url, new Padding(10, 8, 10, 6), Color.FromArgb(242, 246, 248)), 0, 3);
        side.Controls.Add(FillButton("复制链接", (_, _) => Clipboard.SetText(CurrentUrl()), primary: true), 0, 4);
        side.Controls.Add(FillButton("在本机浏览器打开", (_, _) => OpenUrl(CurrentUrl())), 0, 5);
        side.Controls.Add(new Label
        {
            Text = "手机无法访问时，请先确认手机和电脑在同一 Wi-Fi，再复制放行命令添加防火墙规则。",
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(80, 96, 104),
            TextAlign = ContentAlignment.TopLeft
        }, 0, 7);

        panel.Controls.Add(side, 1, 0);
        return panel;
    }

    private Control BottomPanel()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = ModernTheme.Surface,
            Padding = new Padding(0, 12, 0, 0)
        };
        panel.Controls.Add(Button("关闭", (_, _) => Hide(), 150));
        panel.Controls.Add(Button("复制放行命令", (_, _) => Clipboard.SetText(BuildFirewallCommand()), 220));
        return panel;
    }

    private TableLayoutPanel Card(Padding padding)
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = padding,
            Margin = new Padding(0, 0, 0, 12),
            BackColor = Color.White
        };
        ModernTheme.StyleRounded(panel, ModernTheme.CardRadius);
        return panel;
    }

    private static Control Center(Control control, Color? fill = null)
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = fill ?? ModernTheme.Card };
        panel.Controls.Add(control, 0, 0);
        control.Anchor = AnchorStyles.None;
        return panel;
    }

    private static Label Label(string text) => new()
    {
        Text = text,
        AutoSize = false,
        Dock = DockStyle.Fill,
        ForeColor = Color.FromArgb(80, 96, 104),
        TextAlign = ContentAlignment.BottomLeft,
        Margin = Padding.Empty
    };

    private static Button FillButton(string text, EventHandler handler, bool primary = false)
    {
        var button = new Button
        {
            Text = text,
            Dock = DockStyle.Fill,
            Height = 46,
            Margin = new Padding(0, 4, 0, 6),
            AutoSize = false,
            UseCompatibleTextRendering = true,
            BackColor = primary ? ModernTheme.AccentStrong : ModernTheme.AccentSoft,
            ForeColor = primary ? Color.White : ModernTheme.Text
        };
        button.Click += handler;
        ModernTheme.StyleRounded(button, ModernTheme.ButtonRadius);
        if (primary)
        {
            button.BackColor = ModernTheme.AccentStrong;
            button.ForeColor = Color.White;
            button.FlatAppearance.MouseOverBackColor = ModernTheme.Accent;
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(9, 69, 62);
        }
        return button;
    }

    private static Button Button(string text, EventHandler handler, int width)
    {
        var button = new Button
        {
            Text = text,
            Width = width,
            Height = 48,
            Margin = new Padding(6, 0, 0, 0),
            AutoSize = false,
            UseCompatibleTextRendering = true,
            BackColor = ModernTheme.Card
        };
        button.Click += handler;
        ModernTheme.StyleRounded(button, ModernTheme.ButtonRadius);
        return button;
    }

    private static Control Host(Control control, Padding padding, Color fill)
    {
        var host = new RoundedHostPanel
        {
            Padding = padding,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 2, 0, 8),
            FillColor = fill
        };
        control.Dock = DockStyle.Fill;
        control.Margin = Padding.Empty;
        control.BackColor = fill;
        host.Controls.Add(control);
        return host;
    }

    private void ToggleService()
    {
        _config.RemoteControl.Enabled = !_remoteControl.IsRunning;
        _config.RemoteControl.UseRandomPort = false;
        _config.RemoteControl.Port = ReadPort();
        _saveConfig(_config);
        if (_config.RemoteControl.Enabled) _remoteControl.Restart();
        else _remoteControl.Stop();
        RefreshState();
    }

    private void RestartService()
    {
        _config.RemoteControl.Enabled = true;
        _config.RemoteControl.UseRandomPort = false;
        _config.RemoteControl.Port = ReadPort();
        _saveConfig(_config);
        _remoteControl.Restart();
        RefreshState();
    }

    private void RefreshState()
    {
        if (_config.RemoteControl.Enabled && !_remoteControl.IsRunning) _remoteControl.Start();
        _port.Text = Math.Clamp(_remoteControl.CurrentPort > 0 ? _remoteControl.CurrentPort : _config.RemoteControl.Port <= 0 ? 1 : _config.RemoteControl.Port, 1, 65535).ToString(CultureInfo.InvariantCulture);
        _address.Items.Clear();
        foreach (var item in _networkAddressService.GetIPv4Addresses())
        {
            _address.Items.Add(item.Address);
        }

        if (_address.Items.Count == 0) _address.Items.Add("127.0.0.1");
        _address.SelectedIndex = 0;
        _toggle.Text = _remoteControl.IsRunning ? "关闭服务" : "启动服务";
        _toggle.BackColor = _remoteControl.IsRunning ? ModernTheme.SuccessSoft : ModernTheme.AccentSoft;
        _state.Text = _remoteControl.IsRunning ? "服务已启动，可以扫码连接" : "服务未启动";
        _state.ForeColor = _remoteControl.IsRunning ? ModernTheme.Success : Color.FromArgb(160, 64, 64);
        UpdateUrlAndQr();
    }

    private void UpdateUrlAndQr()
    {
        var url = CurrentUrl();
        _url.Text = url;
        _toolTip.SetToolTip(_url, url);
        _qr.Image?.Dispose();
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        using var code = new QRCode(data);
        _qr.Image = code.GetGraphic(8, Color.Black, Color.White, true);
    }

    private string CurrentUrl()
    {
        var address = _address.SelectedItem?.ToString();
        if (string.IsNullOrWhiteSpace(address)) address = "127.0.0.1";
        var port = _remoteControl.CurrentPort > 0 ? _remoteControl.CurrentPort : ReadPort();
        return $"http://{address}:{port}/?token={_config.RemoteControl.Token}";
    }

    private string BuildFirewallCommand()
    {
        var port = _remoteControl.CurrentPort > 0 ? _remoteControl.CurrentPort : ReadPort();
        var exe = Path.Combine(AppContext.BaseDirectory, "FlyPPTTimer.exe");
        return $"netsh advfirewall firewall add rule name=\"FlyPPTTimer Remote {port}\" dir=in action=allow program=\"{exe}\" protocol=TCP localport={port}";
    }

    private int ReadPort()
    {
        if (!int.TryParse(_port.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var port)
            && !int.TryParse(_port.Text.Trim(), out port))
        {
            port = _config.RemoteControl.Port <= 0 ? 1 : _config.RemoteControl.Port;
        }

        port = Math.Clamp(port, 1, 65535);
        _port.Text = port.ToString(CultureInfo.InvariantCulture);
        return port;
    }

    private static void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { }
    }

    private static void BlockMouseWheel(object? sender, MouseEventArgs e)
    {
        if (e is HandledMouseEventArgs handled) handled.Handled = true;
    }
}

internal sealed class FlatSelectBox : Control
{
    private int _selectedIndex = -1;
    private ContextMenuStrip? _menu;

    public FlatSelectBox()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        Height = 36;
        BackColor = ModernTheme.ControlFill;
        ForeColor = ModernTheme.Text;
    }

    public List<string> Items { get; } = [];
    public event EventHandler? SelectedIndexChanged;

    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            var next = Items.Count == 0 ? -1 : Math.Clamp(value, 0, Items.Count - 1);
            if (_selectedIndex == next) return;
            _selectedIndex = next;
            Invalidate();
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public object? SelectedItem => _selectedIndex >= 0 && _selectedIndex < Items.Count ? Items[_selectedIndex] : null;

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        if (Items.Count == 0) return;
        _menu?.Close();
        _menu?.Dispose();
        var menu = _menu = new ContextMenuStrip
        {
            Renderer = new ModernContextMenuRenderer(),
            BackColor = Color.White,
            ForeColor = ModernTheme.Text,
            Font = Font,
            Padding = new Padding(7),
            ShowImageMargin = false,
            ShowCheckMargin = false
        };

        for (var i = 0; i < Items.Count; i++)
        {
            var index = i;
            var item = menu.Items.Add(Items[i], null, (_, _) => SelectedIndex = index);
            item.AutoSize = false;
            item.Size = new Size(Math.Max(Width, 220), 36);
            item.Padding = new Padding(10, 0, 10, 0);
        }

        menu.Closed += (_, _) =>
        {
            if (!ReferenceEquals(_menu, menu)) return;
            _menu = null;
            menu.Dispose();
        };
        var preferred = menu.GetPreferredSize(Size.Empty);
        var screen = PointToScreen(new Point(0, Height + 4));
        var area = Screen.FromPoint(screen).WorkingArea;
        screen.X = Math.Clamp(screen.X, area.Left, Math.Max(area.Left, area.Right - preferred.Width));
        screen.Y = Math.Clamp(screen.Y, area.Top, Math.Max(area.Top, area.Bottom - preferred.Height));
        menu.Show(screen);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (Items.Count > 0 && (keyData == Keys.Space || keyData == Keys.Enter || keyData == Keys.Down))
        {
            OnClick(EventArgs.Empty);
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _menu?.Dispose();
        base.Dispose(disposing);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var fill = new SolidBrush(BackColor);
        e.Graphics.FillRectangle(fill, ClientRectangle);

        var textRect = new Rectangle(2, 0, Math.Max(0, Width - 34), Height);
        TextRenderer.DrawText(
            e.Graphics,
            SelectedItem?.ToString() ?? "",
            Font,
            textRect,
            ForeColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        var cx = Width - 18;
        var cy = Height / 2 + 1;
        using var brush = new SolidBrush(Color.FromArgb(80, 96, 104));
        e.Graphics.FillPolygon(brush, new Point[] { new(cx - 5, cy - 3), new(cx + 5, cy - 3), new(cx, cy + 3) });
    }
}
