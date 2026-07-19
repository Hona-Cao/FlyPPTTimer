$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)

$root = $PSScriptRoot
$projectPath = Join-Path $root "src\FlyPPTTimer\FlyPPTTimer.csproj"
[xml]$project = Get-Content -LiteralPath $projectPath -Raw
$fullVersion = [string]$project.Project.PropertyGroup.Version
if ([string]::IsNullOrWhiteSpace($fullVersion)) { throw "Project version is missing: $projectPath" }
$version = $fullVersion.Trim()
$dist = Join-Path $root "dist\v$version"
$releaseRoot = Join-Path $root "releases\v$version"
if (Test-Path -LiteralPath $releaseRoot) { throw "Release directory already exists and will not be overwritten: $releaseRoot" }

$assets = Join-Path $releaseRoot "assets"
$portable = Join-Path $assets "portable"
$installerSource = Join-Path $assets "installer-source"
$innoOutput = Join-Path $assets "inno-output"
$issPath = Join-Path $assets "FlyPPTTimer-v$version.iss"
$portableZip = Join-Path $assets "FlyPPTTimer_Portable_v$version.zip"
$distPortableZip = Join-Path $root "dist\FlyPPTTimer-v$version-portable-win-x64.zip"
$distPortableHash = "$distPortableZip.sha256"
$distSetupExe = Join-Path $root "dist\FlyPPTTimer-v$version-setup-win-x64.exe"
$distSetupHash = "$distSetupExe.sha256"

if (-not (Test-Path -LiteralPath $dist)) {
    & (Join-Path $root "build.ps1")
}

New-Item -ItemType Directory -Path $portable, $installerSource, $innoOutput | Out-Null

$runtimeFiles = [ordered]@{
    "FlyPPTTimer.exe" = Join-Path $dist "FlyPPTTimer.exe"
    "FlyPPTTimer.config.json" = Join-Path $dist "FlyPPTTimer.config.json"
    "app.ico" = Join-Path $root "src\FlyPPTTimer\Assets\app.ico"
    "README.md" = Join-Path $root "README.md"
}

foreach ($file in $runtimeFiles.GetEnumerator()) {
    if (-not (Test-Path -LiteralPath $file.Value)) {
        throw "Release runtime file is missing: $($file.Value)"
    }
    Copy-Item -LiteralPath $file.Value -Destination (Join-Path $portable $file.Key) -Force
    Copy-Item -LiteralPath $file.Value -Destination (Join-Path $installerSource $file.Key) -Force
}

Compress-Archive -Path (Join-Path $portable "*") -DestinationPath $portableZip -Force
Copy-Item -LiteralPath $portableZip -Destination $distPortableZip -Force
$portableHash = (Get-FileHash -LiteralPath $distPortableZip -Algorithm SHA256).Hash
"$portableHash  $([IO.Path]::GetFileName($distPortableZip))" |
    Set-Content -LiteralPath $distPortableHash -Encoding ASCII

function ConvertTo-InnoPath([string]$path) {
    return $path.Replace('"', '""')
}

$source = ConvertTo-InnoPath $installerSource
$output = ConvertTo-InnoPath $innoOutput
$icon = ConvertTo-InnoPath (Join-Path $root "src\FlyPPTTimer\Assets\app.ico")
$iss = @"
[Setup]
AppId={{8B4B0C52-DA7E-4B71-976E-F4A24177EA6C}
AppName=FlyPPTTimer
AppVersion=$version
AppVerName=FlyPPTTimer $version
AppPublisher=Cao Hunan
AppPublisherURL=https://github.com/Hona-Cao/FlyPPTTimer
AppSupportURL=https://github.com/Hona-Cao/FlyPPTTimer/issues
AppUpdatesURL=https://github.com/Hona-Cao/FlyPPTTimer/releases
VersionInfoVersion=$version.0
VersionInfoCompany=FlyPPTTimer
VersionInfoDescription=FlyPPTTimer presentation timer installer
VersionInfoProductName=FlyPPTTimer
VersionInfoProductVersion=$version
DefaultDirName={localappdata}\FlyPPTTimer
DefaultGroupName=FlyPPTTimer
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern
SetupIconFile=$icon
UninstallDisplayIcon={app}\app.ico
OutputDir=$output
OutputBaseFilename=FlyPPTTimer-v$version-setup-win-x64
Compression=lzma2/ultra64
SolidCompression=yes
LZMAUseSeparateProcess=yes
CloseApplications=yes
RestartApplications=no
UsePreviousAppDir=yes
UsePreviousGroup=yes
AllowNoIcons=yes
MinVersion=10.0

[Files]
Source: "$source\FlyPPTTimer.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "$source\FlyPPTTimer.config.json"; DestDir: "{app}"; Flags: onlyifdoesntexist uninsneveruninstall
Source: "$source\app.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "$source\README.md"; DestDir: "{app}"; Flags: ignoreversion

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: checkedonce

[Icons]
Name: "{group}\FlyPPTTimer"; Filename: "{app}\FlyPPTTimer.exe"; WorkingDir: "{app}"; IconFilename: "{app}\app.ico"
Name: "{autodesktop}\FlyPPTTimer"; Filename: "{app}\FlyPPTTimer.exe"; WorkingDir: "{app}"; IconFilename: "{app}\app.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\FlyPPTTimer.exe"; Description: "Launch FlyPPTTimer"; Flags: nowait postinstall skipifsilent

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
var
  ConfigPath: String;
  BackupPath: String;
begin
  if CurStep <> ssInstall then
    Exit;
  ConfigPath := ExpandConstant('{app}\FlyPPTTimer.config.json');
  if not FileExists(ConfigPath) then
    Exit;
  BackupPath := ConfigPath + '.upgrade.' + GetDateTimeString('yyyymmddhhnnss', #0, #0) + '.backup.json';
  if not FileCopy(ConfigPath, BackupPath, False) then
    Log('Warning: unable to create config upgrade backup: ' + BackupPath);
end;
"@
[IO.File]::WriteAllText($issPath, $iss, [Text.UTF8Encoding]::new($true))

$isccCandidates = @(
    (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe"),
    (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe"),
    (Join-Path $env:ProgramFiles "Inno Setup 6\ISCC.exe")
)
$iscc = $isccCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $iscc) {
    $command = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($command) { $iscc = $command.Source }
}
if (-not $iscc) {
    throw "Inno Setup 6 compiler was not found. Install it with: winget install --id JRSoftware.InnoSetup --exact"
}

& $iscc /Qp $issPath
if ($LASTEXITCODE -ne 0) { throw "Inno Setup compilation failed with exit code $LASTEXITCODE" }
$innoSetup = Join-Path $innoOutput "FlyPPTTimer-v$version-setup-win-x64.exe"
if (-not (Test-Path -LiteralPath $innoSetup)) { throw "Inno Setup did not create the expected installer: $innoSetup" }
Copy-Item -LiteralPath $innoSetup -Destination $distSetupExe -Force

$setupHash = (Get-FileHash -LiteralPath $distSetupExe -Algorithm SHA256).Hash
"$setupHash  $([IO.Path]::GetFileName($distSetupExe))" |
    Set-Content -LiteralPath $distSetupHash -Encoding ASCII

Get-Item $distSetupExe, $distSetupHash, $portableZip, $distPortableZip, $distPortableHash |
    Select-Object FullName, Length
