$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)

$root = $PSScriptRoot
$projectPath = Join-Path $root "src\FlyPPTTimer\FlyPPTTimer.csproj"
[xml]$project = Get-Content -LiteralPath $projectPath -Raw
$fullVersion = [string]$project.Project.PropertyGroup.Version
if ([string]::IsNullOrWhiteSpace($fullVersion)) { throw "Project version is missing: $projectPath" }
$version = $fullVersion -replace '\.0$', ''
$dist = Join-Path $root "dist\v$fullVersion"
$releaseRoot = Join-Path $root "releases\v$version"
if (Test-Path -LiteralPath $releaseRoot) { throw "Release directory already exists and will not be overwritten: $releaseRoot" }
$assets = Join-Path $releaseRoot "assets"
$portable = Join-Path $assets "portable"
$installerSource = Join-Path $assets "installer-source"
$setupExe = Join-Path $assets "FlyPPTTimer_Setup_v$version.exe"
$portableZip = Join-Path $assets "FlyPPTTimer_Portable_v$version.zip"
$iexpressWork = "C:\Temp\FlyPPTTimerPackage_v$version"
$iexpressSource = Join-Path $iexpressWork "source"
$iexpressSetup = Join-Path $iexpressWork "FlyPPTTimer_Setup_v$version.exe"

if (-not (Test-Path -LiteralPath $dist)) {
    & (Join-Path $root "build.ps1")
}

Remove-Item -LiteralPath $iexpressWork -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $portable, $installerSource, $iexpressSource | Out-Null

$runtimeFiles = @(
    "FlyPPTTimer.exe",
    "FlyPPTTimer.config.json",
    "app.ico",
    "README.md"
)

foreach ($file in $runtimeFiles) {
    Copy-Item -LiteralPath (Join-Path $dist $file) -Destination $portable -Force
    Copy-Item -LiteralPath (Join-Path $dist $file) -Destination $installerSource -Force
    Copy-Item -LiteralPath (Join-Path $dist $file) -Destination $iexpressSource -Force
}

Compress-Archive -Path (Join-Path $portable "*") -DestinationPath $portableZip -Force

$installScript = @'
$ErrorActionPreference = "Stop"
$installDir = Join-Path $env:LOCALAPPDATA "FlyPPTTimer"
New-Item -ItemType Directory -Path $installDir -Force | Out-Null
$configPath = Join-Path $installDir "FlyPPTTimer.config.json"
if (Test-Path -LiteralPath $configPath) {
    Copy-Item -LiteralPath $configPath -Destination ($configPath + ".upgrade." + (Get-Date -Format "yyyyMMddHHmmss") + ".backup.json") -Force
}
Copy-Item -LiteralPath ".\FlyPPTTimer.exe", ".\app.ico", ".\README.md" -Destination $installDir -Force
if (-not (Test-Path -LiteralPath $configPath)) {
    Copy-Item -LiteralPath ".\FlyPPTTimer.config.json" -Destination $configPath
}

$shell = New-Object -ComObject WScript.Shell
$startMenuDir = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\FlyPPTTimer"
New-Item -ItemType Directory -Path $startMenuDir -Force | Out-Null

$shortcut = $shell.CreateShortcut((Join-Path $startMenuDir "FlyPPTTimer.lnk"))
$shortcut.TargetPath = Join-Path $installDir "FlyPPTTimer.exe"
$shortcut.WorkingDirectory = $installDir
$shortcut.IconLocation = Join-Path $installDir "app.ico"
$shortcut.Save()

$desktopShortcut = $shell.CreateShortcut((Join-Path ([Environment]::GetFolderPath("Desktop")) "FlyPPTTimer.lnk"))
$desktopShortcut.TargetPath = Join-Path $installDir "FlyPPTTimer.exe"
$desktopShortcut.WorkingDirectory = $installDir
$desktopShortcut.IconLocation = Join-Path $installDir "app.ico"
$desktopShortcut.Save()

Add-Type -AssemblyName System.Windows.Forms
[System.Windows.Forms.MessageBox]::Show("FlyPPTTimer 已安装到：" + [Environment]::NewLine + $installDir, "FlyPPTTimer", "OK", "Information") | Out-Null
'@
$installScriptPath = Join-Path $installerSource "install.ps1"
Set-Content -LiteralPath $installScriptPath -Value $installScript -Encoding UTF8
Set-Content -LiteralPath (Join-Path $iexpressSource "install.ps1") -Value $installScript -Encoding UTF8

$sedPath = Join-Path $iexpressWork "FlyPPTTimer_Setup.sed"
$sourceEscaped = $iexpressSource
$targetEscaped = $iexpressSetup
$sed = @"
[Version]
Class=IEXPRESS
SEDVersion=3
[Options]
PackagePurpose=InstallApp
ShowInstallProgramWindow=0
HideExtractAnimation=1
UseLongFileName=1
InsideCompressed=0
CAB_FixedSize=0
CAB_ResvCodeSigning=0
RebootMode=N
InstallPrompt=%InstallPrompt%
DisplayLicense=%DisplayLicense%
FinishMessage=%FinishMessage%
TargetName=%TargetName%
FriendlyName=%FriendlyName%
AppLaunched=%AppLaunched%
PostInstallCmd=%PostInstallCmd%
AdminQuietInstCmd=%AdminQuietInstCmd%
UserQuietInstCmd=%UserQuietInstCmd%
SourceFiles=SourceFiles
[Strings]
InstallPrompt=Install FlyPPTTimer.
DisplayLicense=
FinishMessage=FlyPPTTimer installation completed.
TargetName=$targetEscaped
FriendlyName=FlyPPTTimer
AppLaunched=powershell.exe -NoProfile -ExecutionPolicy Bypass -File install.ps1
PostInstallCmd=<None>
AdminQuietInstCmd=
UserQuietInstCmd=
FILE0=FlyPPTTimer.exe
FILE1=FlyPPTTimer.config.json
FILE2=app.ico
FILE3=README.md
FILE4=install.ps1
[SourceFiles]
SourceFiles0=$sourceEscaped
[SourceFiles0]
%FILE0%=
%FILE1%=
%FILE2%=
%FILE3%=
%FILE4%=
"@
Set-Content -LiteralPath $sedPath -Value $sed -Encoding ASCII

$iexpress = Join-Path $env:WINDIR "System32\iexpress.exe"
if (-not (Test-Path $iexpress)) {
    throw "iexpress.exe not found."
}
& $iexpress /N /Q $sedPath
if (-not (Test-Path $iexpressSetup)) {
    Write-Host "IExpress did not create setup; building .NET fallback installer..."
    $installerProject = Join-Path $iexpressWork "dotnet-installer"
    $payloadZip = Join-Path $installerProject "payload.zip"
    New-Item -ItemType Directory -Path $installerProject | Out-Null
    Compress-Archive -Path (Join-Path $iexpressSource "*") -DestinationPath $payloadZip -Force

    $csproj = @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <Nullable>enable</Nullable>
    <AssemblyName>FlyPPTTimer_Setup</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <EmbeddedResource Include="payload.zip" />
  </ItemGroup>
</Project>
'@
    Set-Content -LiteralPath (Join-Path $installerProject "FlyPPTTimer_Setup.csproj") -Value $csproj -Encoding UTF8

    $program = @'
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Windows.Forms;

namespace FlyPPTTimerSetup;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        try
        {
            var installDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FlyPPTTimer");
            Directory.CreateDirectory(installDir);
            using var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("FlyPPTTimer_Setup.payload.zip")
                ?? throw new InvalidOperationException("Installer payload is missing.");
            var tempZip = Path.Combine(Path.GetTempPath(), "FlyPPTTimer_payload.zip");
            using (var file = File.Create(tempZip))
            {
                resource.CopyTo(file);
            }
            var extractDir = Path.Combine(Path.GetTempPath(), "FlyPPTTimer_install_" + Guid.NewGuid().ToString("N"));
            ZipFile.ExtractToDirectory(tempZip, extractDir, true);
            var configPath = Path.Combine(installDir, "FlyPPTTimer.config.json");
            if (File.Exists(configPath))
            {
                File.Copy(configPath, configPath + ".upgrade." + DateTime.Now.ToString("yyyyMMddHHmmss") + ".backup.json", true);
            }
            foreach (var file in Directory.GetFiles(extractDir))
            {
                var target = Path.Combine(installDir, Path.GetFileName(file));
                if (Path.GetFileName(file).Equals("FlyPPTTimer.config.json", StringComparison.OrdinalIgnoreCase) && File.Exists(target)) continue;
                File.Copy(file, target, true);
            }
            Directory.Delete(extractDir, true);
            TryCreateShortcut(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "FlyPPTTimer.lnk"),
                installDir);
            var startMenu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "FlyPPTTimer");
            Directory.CreateDirectory(startMenu);
            TryCreateShortcut(Path.Combine(startMenu, "FlyPPTTimer.lnk"), installDir);
            MessageBox.Show("FlyPPTTimer has been installed to:\r\n" + installDir, "FlyPPTTimer", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Installation failed:\r\n" + ex.Message, "FlyPPTTimer", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Environment.ExitCode = 1;
        }
    }

    private static void TryCreateShortcut(string shortcutPath, string installDir)
    {
        try
        {
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType is null) return;
            dynamic shell = Activator.CreateInstance(shellType)!;
            dynamic shortcut = shell.CreateShortcut(shortcutPath);
            shortcut.TargetPath = Path.Combine(installDir, "FlyPPTTimer.exe");
            shortcut.WorkingDirectory = installDir;
            shortcut.IconLocation = Path.Combine(installDir, "app.ico");
            shortcut.Save();
        }
        catch { }
    }
}
'@
    Set-Content -LiteralPath (Join-Path $installerProject "Program.cs") -Value $program -Encoding UTF8
    $dotnet = Join-Path $root ".dotnet\dotnet.exe"
    if (-not (Test-Path $dotnet)) { $dotnet = "dotnet" }
    & $dotnet publish (Join-Path $installerProject "FlyPPTTimer_Setup.csproj") -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o (Join-Path $installerProject "publish")
    if ($LASTEXITCODE -ne 0) { throw "Fallback installer publish failed." }
    $fallbackSetup = Join-Path $installerProject "publish\FlyPPTTimer_Setup.exe"
    if (-not (Test-Path $fallbackSetup)) { throw "Fallback installer was not created." }
    Copy-Item -LiteralPath $fallbackSetup -Destination $iexpressSetup -Force
}
Copy-Item -LiteralPath $iexpressSetup -Destination $setupExe -Force

Get-Item $setupExe, $portableZip | Select-Object FullName, Length
