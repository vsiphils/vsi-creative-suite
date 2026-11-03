; Build from Windows using build.ps1. Paths are supplied by /DStageRoot /DProjectRoot.
#ifndef StageRoot
  #error StageRoot must be passed to the Inno Setup compiler.
#endif

[Setup]
AppId={{C5D51761-22C8-4B5F-A53A-9FC08C27F481}
AppName=VSI Creative Suite
AppVersion=0.3.0
AppVerName=VSI Creative Suite 0.3.0
AppPublisher=Vertical Space Interiors, Inc.
AppPublisherURL=https://vsiphils.com/
DefaultDirName={localappdata}\Programs\VSI Creative Suite
DefaultGroupName=VSI Creative Suite
UninstallDisplayIcon={app}\VsiCreativeSuite.exe
OutputBaseFilename=VSI-Creative-Suite-Setup-Win10-11-x64
WizardStyle=modern
MinVersion=10.0
ArchitecturesAllowed=x64compatible
PrivilegesRequired=lowest
Compression=lzma2
SolidCompression=yes
SetupIconFile={#ProjectRoot}\assets\vsi-logo.ico
DisableProgramGroupPage=yes
DisableWelcomePage=no
AllowNoIcons=yes

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Shortcuts:"; Flags: unchecked

[Dirs]
Name: "{app}\apps"
Name: "{app}\assets"

[Files]
Source: "{#StageRoot}\launcher\VsiCreativeSuite.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#StageRoot}\launcher\vsi-logo.png"; DestDir: "{app}\assets"; Flags: ignoreversion
Source: "{#StageRoot}\THIRD_PARTY_NOTICES.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#StageRoot}\release-lock.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#StageRoot}\licenses\*"; DestDir: "{app}\licenses"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#StageRoot}\apps\*"; DestDir: "{app}\apps"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#StageRoot}\runtime\MicrosoftEdgeWebView2RuntimeInstallerX64.exe"; DestDir: "{app}\runtime"; Flags: deleteafterinstall
Source: "{#StageRoot}\runtime\Hew-x64-setup.exe"; DestDir: "{app}\runtime"; Flags: deleteafterinstall

[Icons]
Name: "{autoprograms}\VSI Creative Suite"; Filename: "{app}\VsiCreativeSuite.exe"
Name: "{autodesktop}\VSI Creative Suite"; Filename: "{app}\VsiCreativeSuite.exe"; Tasks: desktopicon

[Run]
; Both installers are embedded in this Setup EXE; no install-time download.
; Per-user install is preferred. Use only the upstream official NSIS package for Hew.
Filename: "{app}\runtime\MicrosoftEdgeWebView2RuntimeInstallerX64.exe"; Parameters: "/silent /install"; StatusMsg: "Installing offline WebView2 runtime..."; Flags: runhidden waituntilterminated; Check: NeedsWebView2
Filename: "{app}\runtime\Hew-x64-setup.exe"; Parameters: "/S"; StatusMsg: "Installing Hew 3D offline..."; Flags: runhidden waituntilterminated; BeforeInstall: RememberHewDesktopShortcuts; AfterInstall: RemoveNewHewDesktopShortcuts
Filename: "{app}\VsiCreativeSuite.exe"; Description: "Launch VSI Creative Suite"; Flags: nowait postinstall skipifsilent

[Code]
var
  HadUserHewShortcut: Boolean;
  HadCommonHewShortcut: Boolean;

// Hew's upstream NSIS installer creates its own desktop shortcut. Keep Hew
// installed and discoverable inside VSI, but remove only shortcuts that it
// CREATED during this run. Never delete a pre-existing user-owned shortcut.
procedure RememberHewDesktopShortcuts;
begin
  HadUserHewShortcut := FileExists(ExpandConstant('{userdesktop}\Hew.lnk'));
  HadCommonHewShortcut := FileExists(ExpandConstant('{commondesktop}\Hew.lnk'));
end;

procedure RemoveNewHewDesktopShortcuts;
var
  ShortcutPath: String;
begin
  ShortcutPath := ExpandConstant('{userdesktop}\Hew.lnk');
  if (not HadUserHewShortcut) and FileExists(ShortcutPath) then
  begin
    if DeleteFile(ShortcutPath) then
      Log('Removed desktop shortcut created by bundled Hew installer: ' + ShortcutPath)
    else
      Log('Could not remove new Hew desktop shortcut: ' + ShortcutPath);
  end;

  ShortcutPath := ExpandConstant('{commondesktop}\Hew.lnk');
  if (not HadCommonHewShortcut) and FileExists(ShortcutPath) then
  begin
    if DeleteFile(ShortcutPath) then
      Log('Removed common desktop shortcut created by bundled Hew installer: ' + ShortcutPath)
    else
      Log('Could not remove new common Hew shortcut: ' + ShortcutPath);
  end;
end;

function InstalledWebView2(): Boolean;
var
  Ver: String;
  Key: String;
begin
  Key := 'Software\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}';
  Result :=
    (RegQueryStringValue(HKCU, Key, 'pv', Ver) and (Ver <> '') and (Ver <> '0.0.0.0')) or
    (RegQueryStringValue(HKLM, 'SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv', Ver) and (Ver <> '') and (Ver <> '0.0.0.0'));
end;

function NeedsWebView2(): Boolean;
begin
  Result := not InstalledWebView2();
end;

// Important: Hew and WebView2 are separately installed upstream products.
// Uninstalling this suite removes the bundled portable Craft apps and launcher,
// but intentionally does not uninstall Hew or the shared WebView2 runtime.
