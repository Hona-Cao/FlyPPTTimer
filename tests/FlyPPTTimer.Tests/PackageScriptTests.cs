namespace FlyPPTTimer.Tests;

public sealed class PackageScriptTests
{
    [Fact]
    public void UpgradeScript_BacksUpAndDoesNotOverwriteExistingConfig()
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "package_release.ps1"));
        var script = File.ReadAllText(path);
        Assert.Contains("if (-not (Test-Path -LiteralPath $configPath))", script);
        Assert.Contains(".upgrade.", script);
        Assert.Contains("continue;", script);
        Assert.Contains("Release directory already exists and will not be overwritten", script);
    }
}
