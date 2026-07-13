using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using FlyPPTTimer.Models;

namespace FlyPPTTimer.Services;

public sealed class NetworkAddressService
{
    public List<NetworkAddressInfo> GetIPv4Addresses()
    {
        var list = new List<NetworkAddressInfo>();
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up) continue;
            var props = ni.GetIPProperties();
            foreach (var ip in props.UnicastAddresses.Where(x => x.Address.AddressFamily == AddressFamily.InterNetwork))
            {
                var addr = ip.Address.ToString();
                var type = Classify(ni, addr);
                list.Add(new NetworkAddressInfo
                {
                    Name = ni.Name,
                    Address = addr,
                    Type = type.label,
                    Priority = type.priority,
                    Recommended = type.priority <= 3 && IsLanAddress(addr)
                });
            }
        }

        if (!list.Any(x => x.Address == "127.0.0.1"))
        {
            list.Add(new NetworkAddressInfo { Name = "本机测试", Address = "127.0.0.1", Type = "本机测试", Priority = 90, Recommended = false });
        }

        return list
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.Name)
            .ThenBy(x => x.Address)
            .ToList();
    }

    private static (string label, int priority) Classify(NetworkInterface ni, string address)
    {
        var name = (ni.Name + " " + ni.Description).ToLowerInvariant();
        if (address.StartsWith("127.")) return ("本机测试", 90);
        if (address.StartsWith("169.254.")) return ("自动私有地址", 80);
        if (address.StartsWith("198.18.") || address.StartsWith("198.19.") || name.Contains("clash") || name.Contains("tun") || name.Contains("wintun") || name.Contains("proxy")) return ("代理/TUN 虚拟网卡（手机不可用）", 85);
        if (name.Contains("virtual") || name.Contains("vmware") || name.Contains("hyper-v") || name.Contains("virtualbox")) return ("虚拟网卡", 70);
        if (name.Contains("wi-fi") || name.Contains("wireless") || name.Contains("wlan")) return ("无线网络", 1);
        if (name.Contains("remote ndis") || name.Contains("usb") || name.Contains("rndis") || name.Contains("mobile") || name.Contains("tether")) return ("手机热点或 USB 共享网络", 2);
        if (ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet) return ("以太网", 3);
        return ("其他网络", 50);
    }

    private static bool IsLanAddress(string address)
    {
        if (!IPAddress.TryParse(address, out var ip)) return false;
        var b = ip.GetAddressBytes();
        return b[0] == 10 || (b[0] == 172 && b[1] is >= 16 and <= 31) || (b[0] == 192 && b[1] == 168);
    }
}
