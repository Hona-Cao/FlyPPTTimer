namespace FlyPPTTimer.Tests;

public sealed class PackageScriptTests
{
    [Fact]
    public void UpgradeScript_BacksUpAndDoesNotOverwriteExistingConfig()
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "package_release.ps1"));
        var script = File.ReadAllText(path);
        Assert.Contains("onlyifdoesntexist uninsneveruninstall", script);
        Assert.Contains(".upgrade.", script);
        Assert.Contains("FileCopy(ConfigPath, BackupPath, False)", script);
        Assert.Contains("Release directory already exists and will not be overwritten", script);
    }

    [Fact]
    public void PackageScript_UsesCompressedInnoInstallerWithoutDuplicatedDotNetRuntime()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var package = File.ReadAllText(Path.Combine(root, "package_release.ps1"));
        var build = File.ReadAllText(Path.Combine(root, "build.ps1"));

        Assert.Contains("Compression=lzma2/ultra64", package);
        Assert.Contains("SolidCompression=yes", package);
        Assert.Contains("ISCC.exe", package);
        Assert.DoesNotContain("dotnet-installer", package);
        Assert.DoesNotContain("IEXPRESS", package);
        Assert.Contains("EnableCompressionInSingleFile=true", build);
    }

    [Fact]
    public void PackageScript_PreservesThreePartVersionInArtifactNames()
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "package_release.ps1"));
        var script = File.ReadAllText(path);

        Assert.Contains("$version = $fullVersion.Trim()", script);
        Assert.DoesNotContain("$fullVersion -replace '\\.0$'", script);
    }
}
