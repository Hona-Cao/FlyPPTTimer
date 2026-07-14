$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)

$root = $PSScriptRoot
$dotnet = Join-Path $root ".dotnet\dotnet.exe"
if (-not (Test-Path -LiteralPath $dotnet)) { $dotnet = "dotnet" }
$project = Join-Path $root "src\FlyPPTTimer.WinUI\FlyPPTTimer.WinUI.csproj"
[xml]$projectXml = Get-Content -LiteralPath $project -Raw
$fullVersion = [string]$projectXml.SelectSingleNode('//Version').InnerText
$fullVersion = $fullVersion.Trim()
if ([string]::IsNullOrWhiteSpace($fullVersion)) { throw "The WinUI project version is missing." }
$version = $fullVersion -replace '\.0$', ''
$release = Join-Path $root "releases\v$version"
$dist = Join-Path $root "dist\v$version"
if (Test-Path -LiteralPath $release) { throw "Release directory already exists: $release" }
if (Test-Path -LiteralPath $dist) { throw "Dist directory already exists: $dist" }

$sdkRoot = "${env:ProgramFiles(x86)}\Windows Kits\10\bin"
$kit = Get-ChildItem -LiteralPath $sdkRoot -Directory | Where-Object Name -Match '^\d+\.' | Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
$tools = Join-Path $kit.FullName "x64"; $makePri = Join-Path $tools "makepri.exe"; $env:Path = "$tools;$env:Path"
$portable = Join-Path $release "FlyPPTTimer_Portable_v$version"
$portableZip = Join-Path $release "FlyPPTTimer_Portable_v$version.zip"
$setup = Join-Path $release "FlyPPTTimer_Setup_v$version.exe"
$work = Join-Path $env:TEMP "FlyPPTTimer-v$version-package"
if (Test-Path -LiteralPath $work) { Remove-Item -LiteralPath $work -Recurse -Force }
New-Item -ItemType Directory -Path $portable, $dist, $work | Out-Null
foreach ($document in @("README.md", "CHANGELOG.md", "TEST_REPORT.md", "RELEASE_NOTES_v0.13.md")) {
    Copy-Item -LiteralPath (Join-Path $root $document) -Destination $release
}
Copy-Item -LiteralPath (Join-Path $root "docs\ARCHITECTURE_v0.13.md") -Destination $release
Copy-Item -LiteralPath (Join-Path $root "docs\DEVELOPMENT_ENVIRONMENT_v0.13.md") -Destination $release

& $dotnet publish $project -c Release -r win-x64 --self-contained true --no-restore -o $portable -p:MakePriExeFullPath=$makePri -p:PublishTrimmed=false -p:PublishReadyToRun=false
if ($LASTEXITCODE -ne 0) { throw "WinUI publish failed." }
$buildOutput = Join-Path $root "src\FlyPPTTimer.WinUI\bin\Release\net8.0-windows10.0.26100.0\win-x64"
Copy-Item -Path (Join-Path $buildOutput "*.xbf") -Destination $portable
Copy-Item -LiteralPath (Join-Path $buildOutput "FlyPPTTimer.pri") -Destination $portable
Copy-Item -LiteralPath (Join-Path $buildOutput "Assets") -Destination $portable -Recurse
Copy-Item -LiteralPath (Join-Path $buildOutput "Pages") -Destination $portable -Recurse
Copy-Item -LiteralPath (Join-Path $root "README.md") -Destination $portable
Copy-Item -Path (Join-Path $portable "*") -Destination $dist -Recurse
Compress-Archive -Path (Join-Path $portable "*") -DestinationPath $portableZip

$installerProject = Join-Path $work "installer"
New-Item -ItemType Directory -Path $installerProject | Out-Null
Copy-Item -LiteralPath $portableZip -Destination (Join-Path $installerProject "payload.zip")
@'
<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>WinExe</OutputType><TargetFramework>net8.0-windows</TargetFramework><UseWindowsForms>true</UseWindowsForms><PublishSingleFile>true</PublishSingleFile><SelfContained>true</SelfContained><RuntimeIdentifier>win-x64</RuntimeIdentifier><AssemblyName>FlyPPTTimer_Setup</AssemblyName></PropertyGroup><ItemGroup><EmbeddedResource Include="payload.zip" /></ItemGroup></Project>
'@ | Set-Content -LiteralPath (Join-Path $installerProject "Installer.csproj") -Encoding UTF8
@'
using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Windows.Forms;
namespace FlyPPTTimerSetup;
internal static class Program
{
    [STAThread] private static void Main()
    {
        ApplicationConfiguration.Initialize();
        try
        {
            var install = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FlyPPTTimer"); Directory.CreateDirectory(install);
            var config = Path.Combine(install, "FlyPPTTimer.config.json"); if (File.Exists(config)) File.Copy(config, config + ".upgrade." + DateTime.Now.ToString("yyyyMMddHHmmss") + ".backup.json", true);
            using var source = Assembly.GetExecutingAssembly().GetManifestResourceStream("FlyPPTTimer_Setup.payload.zip")!;
            using var archive = new ZipArchive(source); foreach (var entry in archive.Entries) { if (string.IsNullOrEmpty(entry.Name)) continue; var target = Path.Combine(install, entry.FullName); Directory.CreateDirectory(Path.GetDirectoryName(target)!); if (entry.Name.Equals("FlyPPTTimer.config.json", StringComparison.OrdinalIgnoreCase) && File.Exists(target)) continue; entry.ExtractToFile(target, true); }
            var shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")!); dynamic shortcut = ((dynamic)shell!).CreateShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "FlyPPTTimer.lnk")); shortcut.TargetPath = Path.Combine(install, "FlyPPTTimer.exe"); shortcut.WorkingDirectory = install; shortcut.Save();
            MessageBox.Show("FlyPPTTimer v0.13 has been installed.", "FlyPPTTimer", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { MessageBox.Show("Installation failed:\r\n" + ex.Message, "FlyPPTTimer", MessageBoxButtons.OK, MessageBoxIcon.Error); Environment.ExitCode = 1; }
    }
}
'@ | Set-Content -LiteralPath (Join-Path $installerProject "Program.cs") -Encoding UTF8
& $dotnet publish (Join-Path $installerProject "Installer.csproj") -c Release -o (Join-Path $work "setup")
if ($LASTEXITCODE -ne 0) { throw "Installer build failed." }
Copy-Item -LiteralPath (Join-Path $work "setup\FlyPPTTimer_Setup.exe") -Destination $setup
Write-Host "v$version release candidate created: $release"
