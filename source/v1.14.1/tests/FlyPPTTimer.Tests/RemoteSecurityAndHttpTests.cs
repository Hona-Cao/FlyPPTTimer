using System.Text;
using FlyPPTTimer.Models;
using FlyPPTTimer.Services;

namespace FlyPPTTimer.Tests;

public sealed class RemoteSecurityAndHttpTests
{
    [Fact]
    public async Task ReadBody_UsesUtf8ByteLength()
    {
        var bytes = Encoding.UTF8.GetBytes("{\"mode\":\"正计时\"}");
        await using var stream = new MemoryStream(bytes[3..]);
        var result = await RemoteControlService.ReadBody(stream, bytes[..3], bytes.Length, CancellationToken.None);
        Assert.Equal("{\"mode\":\"正计时\"}", Encoding.UTF8.GetString(result));
    }

    [Fact]
    public async Task ReadBody_RejectsOversizeAndIncompleteRequests()
    {
        await Assert.ThrowsAsync<InvalidDataException>(() => RemoteControlService.ReadBody(Stream.Null, [], RemoteControlService.MaxBodyBytes + 1, CancellationToken.None));
        await Assert.ThrowsAsync<EndOfStreamException>(() => RemoteControlService.ReadBody(new MemoryStream([1, 2]), [], 3, CancellationToken.None));
    }

    [Fact]
    public void TokenComparisonAndLogRedaction_AreSafe()
    {
        Assert.True(RemoteControlService.FixedTimeTokenEquals("abc", "abc"));
        Assert.False(RemoteControlService.FixedTimeTokenEquals("abc", "abd"));
        Assert.Equal("/state", RemoteControlService.RedactUrl("/state?token=secret&x=1"));
    }

    [Fact]
    public void DisconnectAll_RegeneratesTokenAndInvalidatesOldLink()
    {
        var config = new AppConfig();
        config.RemoteControl.Token = "old-token";
        using var service = new RemoteControlService(() => config, c => config = c, null!, null!, TestLog.Create());
        service.DisconnectAll();
        Assert.NotEqual("old-token", config.RemoteControl.Token);
        Assert.False(RemoteControlService.FixedTimeTokenEquals("old-token", config.RemoteControl.Token));
    }
}
