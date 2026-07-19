using System.Net;
using System.Security.Cryptography;
using System.Text;
using FlyPPTTimer.Models;
using FlyPPTTimer.Services;

namespace FlyPPTTimer.Tests;

public sealed class V0193FeatureTests
{
    [Fact]
    public void UpdateCheckIsOptInAndVersionComparisonHandlesReleaseTags()
    {
        Assert.False(new AppConfig().Update.CheckOnStartup);
        Assert.True(GiteeUpdateService.TryParseVersion("v0.19.4", out var newer));
        Assert.Equal(new Version(0, 19, 4), newer);
        Assert.True(GiteeUpdateService.TryParseVersion("0.20.0-beta.1", out var prerelease));
        Assert.Equal(new Version(0, 20, 0), prerelease);
    }

    [Fact]
    public async Task GiteeLatestReleaseAndAttachmentsProduceAnAvailableInstallerUpdate()
    {
        const string releaseJson = """
            {"id":123,"tag_name":"v0.21.0","name":"FlyPPTTimer v0.21.0","body":"更新说明","html_url":"https://gitee.com/hona-cao/fly-ppttimer/releases/tag/v0.21.0"}
            """;
        const string assetsJson = """
            [
              {"id":456,"name":"FlyPPTTimer-v0.21.0-setup-win-x64.exe","browser_download_url":"https://gitee.com/hona-cao/fly-ppttimer/attach_files/456/download/setup.exe"},
              {"id":457,"name":"FlyPPTTimer-v0.21.0-portable-win-x64.zip","browser_download_url":"https://gitee.com/hona-cao/fly-ppttimer/attach_files/457/download/portable.zip"}
            ]
            """;
        using var client = new HttpClient(new StubHttpHandler(request =>
            request.RequestUri!.AbsolutePath.EndsWith("/attach_files", StringComparison.Ordinal)
                ? Json(assetsJson)
                : Json(releaseJson)));
        using var service = new GiteeUpdateService(new LogService(), client);

        var result = await service.CheckAsync();

        Assert.Equal(UpdateCheckStatus.UpdateAvailable, result.Status);
        Assert.Equal("0.21.0", result.Release!.Version);
        Assert.Equal("FlyPPTTimer-v0.21.0-setup-win-x64.exe", GiteeUpdateService.FindInstaller(result.Release)!.Name);
    }

    [Fact]
    public async Task MissingGiteeReleaseIsAValidNoReleaseResult()
    {
        using var client = new HttpClient(new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)));
        using var service = new GiteeUpdateService(new LogService(), client);

        var result = await service.CheckAsync();

        Assert.Equal(UpdateCheckStatus.NoRelease, result.Status);
        Assert.Null(result.Release);
    }

    [Fact]
    public async Task InstallerDownloadVerifiesPublishedSha256BeforeItCanBeInstalled()
    {
        var payload = Encoding.UTF8.GetBytes("signed installer payload");
        var hash = Convert.ToHexString(SHA256.HashData(payload));
        var release = new GiteeReleaseInfo(
            "99.98.97",
            "v99.98.97",
            "test",
            "",
            GiteeUpdateService.ReleasesUrl,
            [
                new GiteeReleaseAsset("FlyPPTTimer-v99.98.97-setup-win-x64.exe", "https://gitee.com/test/setup.exe"),
                new GiteeReleaseAsset("FlyPPTTimer-v99.98.97-setup-win-x64.exe.sha256", "https://gitee.com/test/setup.exe.sha256")
            ]);
        using var client = new HttpClient(new StubHttpHandler(request =>
            request.RequestUri!.AbsolutePath.EndsWith(".sha256", StringComparison.OrdinalIgnoreCase)
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(hash + "  setup.exe") }
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(payload) }));
        using var service = new GiteeUpdateService(new LogService(), client);

        var downloaded = await service.DownloadInstallerAsync(release);
        try
        {
            Assert.Equal(payload, await File.ReadAllBytesAsync(downloaded));
        }
        finally
        {
            if (File.Exists(downloaded)) File.Delete(downloaded);
        }
    }

    [Fact]
    public void SettingsAndTrayExposeTheNewUpdateControls()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var settings = File.ReadAllText(Path.Combine(root, "src", "FlyPPTTimer", "Forms", "SettingsForm.cs"));
        var context = File.ReadAllText(Path.Combine(root, "src", "FlyPPTTimer", "FlyPPTTimerContext.cs"));
        var packaging = File.ReadAllText(Path.Combine(root, "package_release.ps1"));

        Assert.Contains("启动时检测新版本", settings);
        Assert.Contains("_config.Update.CheckOnStartup", settings);
        Assert.DoesNotContain("启用（默认关闭）", settings);
        Assert.DoesNotContain("更新来源", settings);
        Assert.Contains("检测新版本", context);
        Assert.Contains("includeUpdateCheck: true", context);
        Assert.Contains("GiteeUpdateService.IsInstalledEdition", context);
        Assert.Contains("if (-not (Test-Path -LiteralPath $configPath))", packaging);
    }

    private static HttpResponseMessage Json(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    private sealed class StubHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }
}
