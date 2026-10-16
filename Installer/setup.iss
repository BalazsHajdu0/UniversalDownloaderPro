; Universal Downloader Pro - Inno Setup Installer Script
; Compile this with Inno Setup Compiler (https://jrsoftware.org/isinfo.php)

#define MyAppName "Universal Downloader Pro"
#define MyAppVersion "4.0"
#define MyAppPublisher "Universal Downloader"
#define MyAppURL "https://github.com/your-repo"
#define MyAppExeName "UniversalDownloader.exe"

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=LICENSE.txt
OutputDir=Output
OutputBaseFilename=UniversalDownloaderSetup
;SetupIconFile=app.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

; Visual appearance

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "hungarian"; MessagesFile: "compiler:Languages\Hungarian.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1; Check: not IsAdminInstallMode

[Files]
; Main application
Source: "..\UniversalDownloaderWPF\bin\Release\UniversalDownloader.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\UniversalDownloaderWPF\bin\Release\UniversalDownloader.exe.config"; DestDir: "{app}"; Flags: ignoreversion

; Szükséges külső eszközök (yt-dlp és ffmpeg) hozzáadása
; A fájlok a fejlesztői környezeted Tools mappájából kerülnek a telepített program Tools mappájába
Source: "Tools\yt-dlp.exe"; DestDir: "{app}\Tools"; Flags: ignoreversion
Source: "Tools\ffmpeg.exe"; DestDir: "{app}\Tools"; Flags: ignoreversion

; Opcionális: Ha a programod NuGet csomagokat vagy külső DLL-eket használ, ezt a sort érdemes kivenni a megjegyzésből:
; Source: "..\UniversalDownloaderWPF\bin\Release\*.dll"; DestDir: "{app}"; Flags: ignoreversion

; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
// Check for .NET Framework 4.8
function IsDotNetDetected(): Boolean;
var
    release: Cardinal;
begin
    Result := False;
    if RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', release) then
    begin
        // 4.8 or later
        Result := release >= 528040;
    end;
end;

function InitializeSetup(): Boolean;
begin
    Result := True;
    if not IsDotNetDetected() then
    begin
        MsgBox('This application requires .NET Framework 4.8 or later.'#13#13
            'Please install .NET Framework 4.8 from Microsoft and run this installer again.', mbError, MB_OK);
        Result := False;
    end;
end;

// Create Tools folder if it doesn't exist
procedure CurStepChanged(CurStep: TSetupStep);
begin
    if CurStep = ssPostInstall then
    begin
        ForceDirectories(ExpandConstant('{app}\Tools'));
    end;
end;