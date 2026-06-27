[Setup]
AppName=Office Print Server
AppVersion=1.0.0
AppPublisher=Bagus Pradika
DefaultDirName={pf}\OfficePrintServer
DefaultGroupName=Office Print Server
OutputDir=..\Installers
OutputBaseFilename=OfficePrintServer_Setup
Compression=lzma2/max
SolidCompression=yes
DisableProgramGroupPage=yes
PrivilegesRequired=admin
SetupIconFile=
UninstallDisplayIcon={app}\OfficePrintServer.Service.exe

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "..\Publish\Server\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs

[Run]
; Register Windows Service
Filename: "{sys}\sc.exe"; Parameters: "create OfficePrintServer binPath= ""{app}\OfficePrintServer.Service.exe"" start= auto"; Flags: runhidden; StatusMsg: "Registering Windows Service..."; Check: not IsServiceInstalled
; Open firewall ports 18080 and 9100
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall add rule name=""Office Print Server IPP"" dir=in action=allow protocol=TCP localport=18080"; Flags: runhidden; StatusMsg: "Configuring Windows Firewall (IPP)..."; Check: not IsPortOpen
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall add rule name=""Office Print Server RAW"" dir=in action=allow protocol=TCP localport=9100"; Flags: runhidden; StatusMsg: "Configuring Windows Firewall (RAW TCP)..."; Check: not IsPortOpen
; Start the service
Filename: "{sys}\sc.exe"; Parameters: "start OfficePrintServer"; Flags: runhidden; StatusMsg: "Starting Office Print Server..."; Check: IsServiceInstalled

[UninstallRun]
; Stop the service
Filename: "{sys}\sc.exe"; Parameters: "stop OfficePrintServer"; Flags: runhidden
; Delete the service after a small delay
Filename: "{sys}\sc.exe"; Parameters: "delete OfficePrintServer"; Flags: runhidden
; Remove firewall rules
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall delete rule name=""Office Print Server IPP"""; Flags: runhidden
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall delete rule name=""Office Print Server RAW"""; Flags: runhidden

[Code]
function IsServiceInstalled: Boolean;
begin
  Result := DirExists(ExpandConstant('{app}'));
end;

function IsPortOpen: Boolean;
begin
  Result := False;
end;
