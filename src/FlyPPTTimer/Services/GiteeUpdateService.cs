using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;

namespace FlyPPTTimer.Services;

public sealed record GiteeReleaseAsset(string Name, string DownloadUrl);

public sealed record GiteeReleaseInfo(
    string Version,
    string TagName,
    string Name,
    string Body,
    string ReleaseUrl,
    IReadOnlyList<GiteeReleaseAsset> Assets);

public enum UpdateCheckStatus
{
    NoRelease,
    UpToDate,
    UpdateAvailable
}

public sealed record UpdateCheckResult(UpdateCheckStatus Status, GiteeReleaseInfo? Release = null);

public sealed class GiteeUpdateService : IDisposable
{
    public const string ReleasesUrl = "https://gitee.com/hona-cao/fly-ppttimer/releases";
    public const string LatestReleaseApiUrl = "https://gitee.com/api/v5/repos/hona-cao/fly-ppttimer/releases/latest";
    private const string ReleaseApiBase = "https://gitee.com/api/v5/repos/hona-cao/fly-ppttimer/releases";

    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private readonly LogService _log;

    public GiteeUpdateService(LogService log, HttpClient? httpClient = null)
    {
        _log = log;
        _ownsHttpClient = httpClient is null;
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        if (!_httpClient.DefaultRequestHeaders.UserAgent.Any())
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd($"FlyPPTTimer/{AppVersion.Current}");
    }

    public static bool IsInstalledEdition
    {
        get
        {
            var installedDirectory = Path.GetFullPath(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FlyPPTTimer"));
            var executableDirectory = Path.GetFullPath(AppContext.BaseDirectory)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return string.Equals(
                executableDirectory,
                installedDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase);
        }
    }

    public async Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(LatestReleaseApiUrl, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _log.Info("Gitee update check completed: no release exists.");
            return new UpdateCheckResult(UpdateCheckStatus.NoRelease);
        }
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = document.RootElement;
        var tagName = GetString(root, "tag_name");
        if (!TryParseVersion(tagName, out var remoteVersion))
            throw new InvalidDataException("Gitee Release 的版本标签无法识别。");

        var releaseId = GetInt64(root, "id");
        var assets = releaseId > 0
            ? await GetAssetsAsync(releaseId, cancellationToken)
            : ParseAssets(root);
        var releaseUrl = GetString(root, "html_url");
        if (string.IsNullOrWhiteSpace(releaseUrl))
            releaseUrl = $"{ReleasesUrl}/tag/{Uri.EscapeDataString(tagName)}";
        var release = new GiteeReleaseInfo(
            remoteVersion.ToString(3),
            tagName,
            GetString(root, "name"),
            GetString(root, "body"),
            releaseUrl,
            assets);

        if (!TryParseVersion(AppVersion.Current, out var currentVersion))
            throw new InvalidDataException("当前程序版本无法识别。");
        var status = remoteVersion > currentVersion
            ? UpdateCheckStatus.UpdateAvailable
            : UpdateCheckStatus.UpToDate;
        _log.Info($"Gitee update check completed: current={currentVersion}, remote={remoteVersion}, status={status}.");
        return new UpdateCheckResult(status, release);
    }

    public static GiteeReleaseAsset? FindInstaller(GiteeReleaseInfo release) => release.Assets.FirstOrDefault(asset =>
        asset.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
        && asset.Name.Contains("setup", StringComparison.OrdinalIgnoreCase)
        && asset.Name.Contains("win-x64", StringComparison.OrdinalIgnoreCase));

    public async Task<string> DownloadInstallerAsync(
        GiteeReleaseInfo release,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var installer = FindInstaller(release)
            ?? throw new FileNotFoundException("此 Release 中未找到 Windows x64 安装版。");
        var directory = Path.Combine(Path.GetTempPath(), "FlyPPTTimer", "updates", $"v{release.Version}");
        Directory.CreateDirectory(directory);
        var destination = Path.Combine(directory, Path.GetFileName(installer.Name));
        var temporary = destination + ".download";
        if (File.Exists(temporary)) File.Delete(temporary);

        try
        {
            using (var response = await _httpClient.GetAsync(installer.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                response.EnsureSuccessStatusCode();
                var total = response.Content.Headers.ContentLength;
                await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
                await using var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
                var buffer = new byte[81920];
                long received = 0;
                int read;
                while ((read = await input.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                    received += read;
                    if (total is > 0) progress?.Report((int)Math.Min(100, received * 100 / total.Value));
                }
            }

            await VerifyHashWhenPublishedAsync(release, installer, temporary, cancellationToken);
            File.Move(temporary, destination, true);
            _log.Info($"Update installer downloaded: {destination}");
            return destination;
        }
        catch
        {
            if (File.Exists(temporary)) File.Delete(temporary);
            throw;
        }
    }

    public static void LaunchInstallerAfterExit(string installerPath, int processId)
    {
        if (!File.Exists(installerPath)) throw new FileNotFoundException("下载的安装程序不存在。", installerPath);
        var scriptPath = Path.Combine(Path.GetDirectoryName(installerPath)!, "install-update.ps1");
        var escapedInstaller = installerPath.Replace("'", "''");
        File.WriteAllText(scriptPath, $$"""
            $ErrorActionPreference = 'Stop'
            try { Wait-Process -Id {{processId}} -Timeout 30 -ErrorAction SilentlyContinue } catch { }
            Start-Process -FilePath '{{escapedInstaller}}' -WorkingDirectory (Split-Path -Parent '{{escapedInstaller}}')
            Remove-Item -LiteralPath $PSCommandPath -Force -ErrorAction SilentlyContinue
            """);
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-WindowStyle");
        startInfo.ArgumentList.Add("Hidden");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(scriptPath);
        _ = Process.Start(startInfo) ?? throw new InvalidOperationException("无法启动更新安装程序。");
    }

    public static bool TryParseVersion(string? text, out Version version)
    {
        var normalized = (text ?? "").Trim().TrimStart('v', 'V');
        var suffix = normalized.IndexOfAny(['-', '+']);
        if (suffix >= 0) normalized = normalized[..suffix];
        if (!Version.TryParse(normalized, out var parsed))
        {
            version = new Version();
            return false;
        }
        version = new Version(parsed.Major, Math.Max(0, parsed.Minor), Math.Max(0, parsed.Build));
        return true;
    }

    private async Task<IReadOnlyList<GiteeReleaseAsset>> GetAssetsAsync(long releaseId, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync($"{ReleaseApiBase}/{releaseId}/attach_files", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _log.Warn($"Unable to read Gitee release assets: HTTP {(int)response.StatusCode}.");
                return [];
            }
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            return ParseAssets(document.RootElement);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _log.Warn($"Unable to read Gitee release assets: {ex.Message}");
            return [];
        }
    }

    private async Task VerifyHashWhenPublishedAsync(
        GiteeReleaseInfo release,
        GiteeReleaseAsset installer,
        string downloadedFile,
        CancellationToken cancellationToken)
    {
        var hashAsset = release.Assets.FirstOrDefault(asset =>
            asset.Name.Equals(installer.Name + ".sha256", StringComparison.OrdinalIgnoreCase));
        if (hashAsset is null) return;
        var published = (await _httpClient.GetStringAsync(hashAsset.DownloadUrl, cancellationToken))
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();
        if (published is null || published.Length != 64)
            throw new InvalidDataException("Release 中的 SHA-256 校验文件格式不正确。");
        await using var downloaded = File.OpenRead(downloadedFile);
        var actual = Convert.ToHexString(await SHA256.HashDataAsync(downloaded, cancellationToken));
        if (!actual.Equals(published, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("下载的安装程序 SHA-256 校验失败，已停止安装。");
    }

    private static IReadOnlyList<GiteeReleaseAsset> ParseAssets(JsonElement element)
    {
        var source = element;
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("assets", out var assets)) source = assets;
            else if (element.TryGetProperty("attach_files", out var attachFiles)) source = attachFiles;
        }
        if (source.ValueKind != JsonValueKind.Array) return [];
        var result = new List<GiteeReleaseAsset>();
        foreach (var item in source.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object) continue;
            var name = FirstString(item, "name", "filename", "file_name");
            var url = FirstString(item, "browser_download_url", "download_url");
            if (string.IsNullOrWhiteSpace(url) && GetInt64(item, "id") is var id && id > 0)
                url = $"https://gitee.com/hona-cao/fly-ppttimer/attach_files/{id}/download/{Uri.EscapeDataString(name)}";
            if (!string.IsNullOrWhiteSpace(name) && Uri.TryCreate(url, UriKind.Absolute, out _))
                result.Add(new GiteeReleaseAsset(name, url));
        }
        return result;
    }

    private static string FirstString(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            var value = GetString(element, name);
            if (!string.IsNullOrWhiteSpace(value)) return value;
        }
        return "";
    }

    private static string GetString(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";

    private static long GetInt64(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(name, out var value)
        && value.TryGetInt64(out var number)
            ? number
            : 0;

    public void Dispose()
    {
        if (_ownsHttpClient) _httpClient.Dispose();
    }
}
