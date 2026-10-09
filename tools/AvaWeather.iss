#ifndef AppVersion
  #error AppVersion must be supplied to ISCC
#endif
#ifndef TargetArch
  #error TargetArch must be supplied to ISCC
#endif

#if TargetArch == "arm64"
  #define AllowedArchitectures "arm64"
#elif TargetArch == "x64"
  #define AllowedArchitectures "x64compatible and not arm64"
#else
  #error Unsupported Windows architecture
#endif

[Setup]
AppId=AvaWeather.Desktop
AppName=AvaWeather
AppVersion={#AppVersion}
AppPublisher=gagarinbefree
AppPublisherURL=https://github.com/gagarinbefree/AvaWeather
DefaultDirName={userpf}\AvaWeather
DefaultGroupName=AvaWeather
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed={#AllowedArchitectures}
ArchitecturesInstallIn64BitMode={#AllowedArchitectures}
SetupIconFile=..\AvaWeather\Assets\app-icon.ico
UninstallDisplayIcon={app}\AvaWeather.exe
OutputBaseFilename=AvaWeather-setup-win-{#TargetArch}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Files]
Source: "..\publish\AvaWeather.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\AvaWeather"; Filename: "{app}\AvaWeather.exe"
Name: "{autodesktop}\AvaWeather"; Filename: "{app}\AvaWeather.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
