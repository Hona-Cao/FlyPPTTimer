namespace FlyPPTTimer.Forms;

internal sealed class TimeUpBlackoutForm : Form
{
    protected override bool ShowWithoutActivation => true;

    public TimeUpBlackoutForm(Screen screen)
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Bounds = screen.Bounds;
        BackColor = Color.Black;
        TopMost = true;
        ShowInTaskbar = false;
        DoubleBuffered = true;
        var label = new Label
        {
            Dock = DockStyle.Fill,
            Text = "时间到",
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.White,
            BackColor = Color.Black,
            Font = new Font("Microsoft YaHei UI", 72F, FontStyle.Bold, GraphicsUnit.Point)
        };
        label.Click += (_, _) => Close();
        Click += (_, _) => Close();
        Controls.Add(label);
    }

    protected override CreateParams CreateParams
    {
        get
        {
            const int wsExNoActivate = 0x08000000;
            var parameters = base.CreateParams;
            parameters.ExStyle |= wsExNoActivate;
            return parameters;
        }
    }
}
