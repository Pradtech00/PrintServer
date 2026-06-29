@echo off
title Office Print Server Installer
echo =====================================
echo   Office Print Server - One Click Setup
echo   Created by Bagus Pradika ^| 2026
echo =====================================
echo.
echo This will install Office Print Server as a Windows Service
echo on ports 18080 and 9100. Administrator privileges required.
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
sc description OfficePrintServer "Office Print Server - IPP + Raw TCP Print Service (Port 18080 + 9100)" >nul

echo [3/4] Configuring Windows Firewall...
netsh advfirewall firewall add rule name="Office Print Server IPP" dir=in action=allow protocol=TCP localport=18080 >nul
netsh advfirewall firewall add rule name="Office Print Server RAW" dir=in action=allow protocol=TCP localport=9100 >nul

echo [4/4] Starting service...
sc start OfficePrintServer >nul

echo.
echo =====================================
echo  INSTALLATION COMPLETE!
echo  Created by Bagus Pradika
echo =====================================
echo.
echo Service: OfficePrintServer
echo Port: 18080 (IPP) + 9100 (Raw TCP)
echo Status: Running
echo.
echo METHOD A - Standard TCP/IP Port (RECOMMENDED):
echo On client: Control Panel ^> Devices and Printers ^> Add Printer
echo Select "Add a printer using an IP address or hostname"
echo Device type: TCP/IP Device
echo IP: 193.13.7.17
echo Port: 9100
echo Then select printer driver: EPSON L1110 Series
echo.
echo METHOD B - IPP (alternative):
echo http://^<SERVER_IP^>:18080/ipp/^<Printer^>
echo.
echo METHOD C - WPF Dashboard:
echo http://^<SERVER_IP^>:18080
echo.
echo To uninstall, run: Uninstall-Server.bat
echo.
pause
