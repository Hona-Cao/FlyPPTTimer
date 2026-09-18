using FlyPPTTimer.Models;
using FlyPPTTimer.Services;

namespace FlyPPTTimer.Tests;

public sealed class ConfigAndNetworkTests
{
    [Fact]
    public void AtomicSave_PreservesUserConfigAndRecoversLatestValidBackup()
    {
        var dir = Path.Combine(Path.GetTempPath(), "FlyPPTTimerTests", Guid.NewGuid().ToString("N"));
        var path = Path.Combine(dir, "FlyPPTTimer.config.json");
        var service = new ConfigService(new LogService(Path.Combine(dir, "logs")), path);
        var config = new AppConfig();
        config.Timer.DefaultDuration = "00:05:00";
        service.Save(config);
        config.Timer.DefaultDuration = "00:06:00";
        service.Save(config);
        File.WriteAllText(path, "{broken");
        var recovered = service.Load();
        Assert.Equal("00:05:00", recovered.Timer.DefaultDuration);
        Assert.Contains(Directory.GetFiles(dir), file => file.Contains(".bad.json", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("192.168.1.5", true)]
    [InlineData("172.16.1.5", true)]
    [InlineData("198.18.0.1", false)]
    [InlineData("8.8.8.8", false)]
    public void LanAddressFilter_OnlyRecommendsPrivateLanRanges(string address, bool expected) => Assert.Equal(expected, NetworkAddressService.IsLanAddress(address));

    [Fact]
    public void ProxyAddressFilter_RecognizesClashTunRanges()
    {
        Assert.True(NetworkAddressService.IsProxyAddress("198.18.0.1", "Ethernet"));
        Assert.True(NetworkAddressService.IsProxyAddress("192.168.1.2", "Clash TUN"));
        Assert.False(NetworkAddressService.IsProxyAddress("192.168.1.2", "Wi-Fi"));
    }
}
