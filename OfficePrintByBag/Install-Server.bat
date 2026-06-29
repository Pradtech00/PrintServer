@echo off
setlocal enabledelayedexpansion
title Office Print Server Slim Installer

REM ---------- Self-elevate to admin ----------
net session >nul 2>&1
if %errorLevel% neq 0 (
    powershell -Command "Start-Process '%~f0' -Verb RunAs"
    exit /b
)

cd /d "%~dp0"

echo =====================================
echo  Office Print Server - Slim Version
echo  Created by Bagus Pradika ^| 2026
echo  Requires: .NET 8 Runtime (ASP.NET Core)
echo =====================================
echo.

REM ---------- Check .NET 8 Runtime ----------
echo Checking for .NET 8 ASP.NET Core Runtime...
set DOTNET_FOUND=
reg query "HKLM\SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.AspNetCore.App" /v 8.0.0 >nul 2>&1
if %errorLevel% equ 0 set DOTNET_FOUND=1
reg query "HKLM\SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.AspNetCore.App" /v 8.0.0 >nul 2>&1
if %errorLevel% equ 0 set DOTNET_FOUND=1
if not defined DOTNET_FOUND (
    echo.
    echo [ERROR] .NET 8 ASP.NET Core Runtime not found.
    echo.
    echo This application requires the ASP.NET Core Runtime 8.0.x.
    echo Download size: ~30 MB
    echo.
    set /p INSTALL_DOTNET=Download and install automatically? (Y/N):
    if /I "!INSTALL_DOTNET!" NEQ "Y" (
        echo.
        echo Please download manually from:
        echo https://dotnet.microsoft.com/en-us/download/dotnet/8.0
        echo Choose: ASP.NET Core Runtime 8.0.x (Hosting Bundle)
        echo.
        pause
        exit /b 1
    )
    echo.
    echo [1/5] Downloading .NET 8 Runtime...
    powershell -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; try{ Invoke-WebRequest -Uri 'https://aka.ms/dotnet/8.0/dotnet-hosting-win.exe' -OutFile '%TEMP%\dotnet-hosting.exe' -UseBasicParsing; exit 0 } catch{ exit 1 }"
    if !errorLevel! neq 0 (
        echo.
        echo [ERROR] Download failed. Check internet connection.
        echo Download manually: https://aka.ms/dotnet/8.0/dotnet-hosting-win.exe
        pause
        exit /b 1
    )
    echo Download complete.

    echo [2/5] Installing .NET 8 Runtime (this may take a few minutes)...
    start /wait "" "%TEMP%\dotnet-hosting.exe" /install /quiet /norestart
    if !errorLevel! neq 0 (
        echo [WARNING] Installer exit code: !errorLevel! (may need reboot)
    ) else (
        echo .NET 8 Runtime installed successfully.
    )
    echo.
)

echo [1/5] Copying files...
if not exist "%ProgramFiles%\OfficePrintServer" mkdir "%ProgramFiles%\OfficePrintServer"
xcopy /E /I /Y "Server\*" "%ProgramFiles%\OfficePrintServer\" >nul

echo [2/5] Registering Windows Service...
sc create OfficePrintServer binPath="%ProgramFiles%\OfficePrintServer\OfficePrintServer.Service.exe" start=auto >nul 2>&1
sc description OfficePrintServer "Office Print Server - IPP + Raw TCP Print Service (Port 18080 + 9100)" >nul

echo [3/5] Configuring Windows Firewall...
netsh advfirewall firewall add rule name="Office Print Server IPP" dir=in action=allow protocol=TCP localport=18080 >nul
netsh advfirewall firewall add rule name="Office Print Server RAW" dir=in action=allow protocol=TCP localport=9100 >nul

echo [4/5] Configuring service recovery...
sc failure OfficePrintServer reset= 86400 actions= restart/30000/restart/60000/restart/120000 >nul

echo [5/5] Starting service...
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
echo Mode: Framework-dependent (slim, ~38 MB)
echo.
echo METHOD A - Standard TCP/IP Port (RECOMMENDED):
echo On client: Control Panel ^> Devices and Printers ^> Add Printer
echo Select "Add a printer using an IP address or hostname"
echo Device type: TCP/IP Device
echo IP: 192.168.1.100
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
