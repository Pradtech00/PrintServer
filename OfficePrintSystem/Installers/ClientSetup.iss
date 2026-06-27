[Setup]
AppName=Office Print Client
AppVersion=1.0.0
AppPublisher=Bagus Pradika
DefaultDirName={localappdata}\OfficePrintClient
DefaultGroupName=Office Print Client
OutputDir=..\Installers
OutputBaseFilename=OfficePrintClient_Setup
Compression=lzma2/max
SolidCompression=yes
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
SetupIconFile=
UninstallDisplayIcon={app}\OfficePrintClient.WPF.exe

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "..\Publish\Client\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs

[Icons]
Name: "{userdesktop}\Office Print Client"; Filename: "{app}\OfficePrintClient.WPF.exe"
Name: "{userprograms}\Office Print Client\Office Print Client"; Filename: "{app}\OfficePrintClient.WPF.exe"
Name: "{userprograms}\Office Print Client\Uninstall Office Print Client"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\OfficePrintClient.WPF.exe"; Description: "Launch Office Print Client"; Flags: postinstall nowait skipifsilent
