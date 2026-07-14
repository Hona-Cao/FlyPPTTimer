using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using FlyPPTTimer.Core.Models;

namespace FlyPPTTimer.Infrastructure.Windows;

public sealed class NetworkAddressService
{
    public IReadOnlyList<NetworkAddressInfo> GetIPv4Addresses()
    {
        var result = new List<NetworkAddressInfo>();
        foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces().Where(x => x.OperationalStatus == OperationalStatus.Up))
        foreach (var ip in adapter.GetIPProperties().UnicastAddresses.Where(x => x.Address.AddressFamily == AddressFamily.InterNetwork))
        {
            var address = ip.Address.ToString(); var name = $"{adapter.Name} {adapter.Description}"; var (type, priority) = Classify(name, adapter.NetworkInterfaceType, address);
            result.Add(new(adapter.Name, address, type, priority, priority <= 3 && IsLanAddress(address)));
        }
        return result.OrderBy(x => x.Priority).ThenBy(x => x.Name).ToList();
    }
    internal static (string, int) Classify(string name, NetworkInterfaceType type, string address)
    {
        var value = name.ToLowerInvariant();
        if (address.StartsWith("198.18.") || address.StartsWith("198.19.") || new[] { "clash", "tun", "wintun", "proxy" }.Any(value.Contains)) return ("代理/TUN 虚拟网卡（手机不可用）", 85);
        if (value.Contains("virtual") || value.Contains("vmware") || value.Contains("hyper-v")) return ("虚拟网卡", 70);
        if (value.Contains("wi-fi") || value.Contains("wireless") || value.Contains("wlan")) return ("无线网络", 1);
        if (value.Contains("usb") || value.Contains("rndis") || value.Contains("tether")) return ("手机热点或 USB 网络", 2);
        if (type == NetworkInterfaceType.Ethernet) return ("以太网", 3);
        return ("其他网络", 50);
    }
    internal static bool IsLanAddress(string address) { if (!IPAddress.TryParse(address, out var ip)) return false; var b = ip.GetAddressBytes(); return b[0] == 10 || b[0] == 172 && b[1] is >= 16 and <= 31 || b[0] == 192 && b[1] == 168; }
}
