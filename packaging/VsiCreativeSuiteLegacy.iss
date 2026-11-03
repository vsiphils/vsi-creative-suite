; EXPERIMENTAL: launcher UI only. Does not bundle or claim Windows 7/8 app support.
#ifndef StageRoot
  #error StageRoot must be passed to the Inno Setup compiler.
#endif

[Setup]
AppId={{A521ADE7-3C13-4C09-99A9-A053E62261D4}
AppName=VSI Creative Suite (Legacy Launcher)
AppVersion=0.2.1
AppPublisher=Vertical Space Interiors, Inc.
DefaultDirName={localappdata}\Programs\VSI Creative Suite Legacy
DefaultGroupName=VSI Creative Suite
OutputBaseFilename=VSI-Creative-Suite-Legacy-Launcher-Win7-8
WizardStyle=modern
MinVersion=6.1sp1
PrivilegesRequired=lowest
Compression=lzma2
SolidCompression=yes
SetupIconFile={#ProjectRoot}\assets\vsi-logo.ico
DisableProgramGroupPage=yes

[Files]
Source: "{#StageRoot}\launcher\VsiCreativeSuite.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#StageRoot}\launcher\vsi-logo.png"; DestDir: "{app}\assets"; Flags: ignoreversion
Source: "{#StageRoot}\THIRD_PARTY_NOTICES.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\VSI Creative Suite (Legacy)"; Filename: "{app}\VsiCreativeSuite.exe"

[Run]
Filename: "{app}\VsiCreativeSuite.exe"; Description: "Open legacy launcher"; Flags: nowait postinstall skipifsilent

; Windows 7 SP1 requires a working .NET Framework 4.x runtime.
; The six requested upstream apps have NOT been validated/supported on Windows 7/8.
