#ifndef SourceDir
  #error SourceDir must be supplied with /DSourceDir=...
#endif
#ifndef OutputDir
  #error OutputDir must be supplied with /DOutputDir=...
#endif
#ifndef MyVersion
  #define MyVersion "1.6.0"
#endif

[Setup]
AppId={{8B4B0C52-DA7E-4B71-976E-F4A24177EA6C}
AppName=FlyPPTTimer
AppVersion={#MyVersion}
AppVerName=FlyPPTTimer {#MyVersion}
AppPublisher=Cao Hunan
AppPublisherURL=https://github.com/Hona-Cao/FlyPPTTimer
AppSupportURL=https://github.com/Hona-Cao/FlyPPTTimer/issues
AppUpdatesURL=https://gitee.com/hona-cao/fly-ppttimer/releases
VersionInfoVersion={#MyVersion}.0
VersionInfoCompany=FlyPPTTimer
VersionInfoDescription=FlyPPTTimer presentation timer installer
VersionInfoProductName=FlyPPTTimer
VersionInfoProductVersion={#MyVersion}
DefaultDirName={localappdata}\FlyPPTTimer
DefaultGroupName=FlyPPTTimer
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern
SetupIconFile={#SourceDir}\app.ico
UninstallDisplayIcon={app}\app.ico
OutputDir={#OutputDir}
OutputBaseFilename=FlyPPTTimer-v{#MyVersion}-setup-win-x64
Compression=lzma2/ultra64
SolidCompression=yes
LZMAUseSeparateProcess=yes
CloseApplications=force
RestartApplications=no
UsePreviousAppDir=yes
UsePreviousGroup=yes
AllowNoIcons=yes
MinVersion=10.0
ShowLanguageDialog=auto
AppMutex=Local\FlyPPTTimer.SingleInstance

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "chinesesimp"; MessagesFile: "ChineseSimplified.isl"

[Files]
Source: "{#SourceDir}\FlyPPTTimer.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\FlyPPTTimer.config.json"; DestDir: "{app}"; Flags: onlyifdoesntexist uninsneveruninstall
Source: "{#SourceDir}\app.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SourceDir}\README.zh-CN.md"; DestDir: "{app}"; Flags: ignoreversion

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checkedonce

[Icons]
Name: "{group}\FlyPPTTimer"; Filename: "{app}\FlyPPTTimer.exe"; WorkingDir: "{app}"; IconFilename: "{app}\app.ico"
Name: "{autodesktop}\FlyPPTTimer"; Filename: "{app}\FlyPPTTimer.exe"; WorkingDir: "{app}"; IconFilename: "{app}\app.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\FlyPPTTimer.exe"; Description: "{cm:LaunchProgram,FlyPPTTimer}"; Flags: nowait postinstall skipifsilent

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
var
  LanguagePath: String;
begin
  if CurStep = ssPostInstall then
  begin
    LanguagePath := ExpandConstant('{app}\install-language.txt');
    if ActiveLanguage = 'chinesesimp' then
      SaveStringToFile(LanguagePath, 'zh-CN', False)
    else
      SaveStringToFile(LanguagePath, 'en', False);
  end;
end;
