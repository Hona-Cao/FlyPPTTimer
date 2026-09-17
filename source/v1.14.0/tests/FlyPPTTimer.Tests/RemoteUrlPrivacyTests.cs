using FlyPPTTimer.Services;

namespace FlyPPTTimer.Tests;

public sealed class RemoteUrlPrivacyTests
{
    [Fact]
    public void MaskToken_HidesSecretButKeepsEndpointVisible()
    {
        var value = RemoteUrlPrivacy.MaskToken("http://192.168.1.8:11835/?token=secret-value");

        Assert.Equal("http://192.168.1.8:11835/?token=••••••", value);
        Assert.DoesNotContain("secret-value", value);
    }

    [Fact]
    public void MaskToken_LeavesUrlsWithoutTokenUnchanged()
    {
        const string value = "http://127.0.0.1:11835/";
        Assert.Equal(value, RemoteUrlPrivacy.MaskToken(value));
    }
}
