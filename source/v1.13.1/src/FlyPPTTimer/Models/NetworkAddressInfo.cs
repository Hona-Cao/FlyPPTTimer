namespace FlyPPTTimer.Models;

public sealed class NetworkAddressInfo
{
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public string Type { get; set; } = "";
    public bool Recommended { get; set; }
    public int Priority { get; set; }
}
