#define MyAppName "Divvun Installer"
#define MyAppPublisher "Universitetet i Tromsø - Norges arktiske universitet"
#define MyAppURL "http://divvun.no"
#define MyAppExeName "Divvun.Installer.exe"
; #define MyAppVersion "1.2.3"
#define PahkatSvcExe "pahkat-service.exe"

[Setup]
AppId={{4CF2F367-82A8-5E60-8334-34619CBA8347}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
;AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={pf}\Divvun Installer
DisableProgramGroupPage=yes
OutputBaseFilename=install
Compression=lzma
SolidCompression=yes
AppMutex=DivvunInstaller
SignedUninstaller=yes
SignTool=signtool
MinVersion=6.3.9200                 

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "armenian"; MessagesFile: "compiler:Languages\Armenian.isl"
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"
Name: "catalan"; MessagesFile: "compiler:Languages\Catalan.isl"
Name: "corsican"; MessagesFile: "compiler:Languages\Corsican.isl"
Name: "czech"; MessagesFile: "compiler:Languages\Czech.isl"
Name: "danish"; MessagesFile: "compiler:Languages\Danish.isl"
Name: "dutch"; MessagesFile: "compiler:Languages\Dutch.isl"
Name: "finnish"; MessagesFile: "compiler:Languages\Finnish.isl"
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
Name: "hebrew"; MessagesFile: "compiler:Languages\Hebrew.isl"
Name: "icelandic"; MessagesFile: "compiler:Languages\Icelandic.isl"
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"
Name: "norwegian"; MessagesFile: "compiler:Languages\Norwegian.isl"
Name: "polish"; MessagesFile: "compiler:Languages\Polish.isl"
Name: "portuguese"; MessagesFile: "compiler:Languages\Portuguese.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "slovenian"; MessagesFile: "compiler:Languages\Slovenian.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"
Name: "ukrainian"; MessagesFile: "compiler:Languages\Ukrainian.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "noui"; Description: "Install without UI";  Flags: unchecked

[Files]
Source: ".\{#PahkatSvcExe}"; DestDir: "{app}"; Flags:
Source: ".\Divvun.Installer\bin\x86\Release\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs uninsrestartdelete; Tasks: not noui

[Run]
Filename: "{app}\{#PahkatSvcExe}"; Parameters: "service install"; StatusMsg: "Installing service.."
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Parameters: ""; Flags: nowait postinstall; Tasks: not noui

[UninstallRun]
Filename: "{app}\{#PahkatSvcExe}"; Parameters: "service stop"; StatusMsg: "Stopping service.."
Filename: "{app}\{#PahkatSvcExe}"; Parameters: "service uninstall"; StatusMsg: "Uninstalling service.."

; [Icons]
; Name: "{commonprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
; Name: "{commonstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Parameters: "-s"
; Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Code]
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
begin
    // Stop the service
    ExtractTemporaryFile('{#PahkatSvcExe}');
    Exec(ExpandConstant('{tmp}\{#PahkatSvcExe}'), 'service stop', '', SW_SHOW, ewWaitUntilTerminated, ResultCode)
end;