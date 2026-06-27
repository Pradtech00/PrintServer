@echo off
title Office Print Server Installer
echo =====================================
echo  Office Print Server - One Click Setup
echo =====================================
echo.
echo This will install Office Print Server as a Windows Service
echo on port 18080. Administrator privileges required.
echo.

REM Self-elevate to admin
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo Requesting administrator privileges...
    powershell -Command "Start-Process '%~f0' -Verb RunAs"
    exit /b
)

cd /d "%~dp0"

echo [1/4] Copying files...
xcopy /E /I /Y "Server\*" "%ProgramFiles%\OfficePrintServer\" >nul

echo [2/4] Registering Windows Service...
sc create OfficePrintServer binPath="%ProgramFiles%\OfficePrintServer\OfficePrintServer.Service.exe" start=auto >nul 2>&1
sc description OfficePrintServer "Office Print Server - IPP Print Service (Port 18080)" >nul

echo [3/4] Configuring Windows Firewall...
netsh advfirewall firewall add rule name="Office Print Server IPP" dir=in action=allow protocol=TCP localport=18080 >nul

echo [4/4] Starting service...
sc start OfficePrintServer >nul

echo.
echo =====================================
echo  INSTALLATION COMPLETE!
echo =====================================
echo.
echo Service: OfficePrintServer
echo Port: 18080
echo Status: Running
echo.
echo Now add printer on client machines using URL:
echo http://<THIS_SERVER_IP>:18080/ipp/<PrinterName>
echo.
echo To uninstall, run: Uninstall-Server.bat
echo.
pause
