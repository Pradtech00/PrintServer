@echo off
title Office Print Client Uninstaller
echo =====================================
echo  Office Print Client - Uninstall
echo  Created by Bagus Pradika ^| 2026
echo =====================================
echo.
echo This will remove the client application and shortcuts.
echo Note: You still need to remove the printer manually
echo from Control Panel ^> Devices and Printers.
echo.

REM Self-elevate to admin
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo Requesting administrator privileges...
    powershell -Command "Start-Process '%~f0' -Verb RunAs"
    exit /b
)

echo [1/3] Removing files...
if exist "%LOCALAPPDATA%\OfficePrintClient" (
    rmdir /S /Q "%LOCALAPPDATA%\OfficePrintClient" >nul 2>&1
    echo Files removed.
)

echo [2/3] Removing shortcuts...
if exist "%USERPROFILE%\Desktop\Office Print Client.lnk" (
    del "%USERPROFILE%\Desktop\Office Print Client.lnk" >nul 2>&1
)
if exist "%APPDATA%\Microsoft\Windows\Start Menu\Programs\Office Print Client" (
    rmdir /S /Q "%APPDATA%\Microsoft\Windows\Start Menu\Programs\Office Print Client" >nul 2>&1
)
echo Shortcuts removed.

echo [3/3] Cleanup...
for /f "tokens=2*" %%a in ('reg query "HKCU\Software\Microsoft\Windows\CurrentVersion\UFH\SHC" /s 2^>nul ^| findstr /i "OfficePrintClient"') do (
    reg delete "%%a" /f >nul 2>&1
)

echo.
echo =====================================
echo  UNINSTALL COMPLETE!
echo =====================================
echo.
echo Remaining step (manual):
echo Go to Control Panel ^> Devices and Printers
echo Right-click the printer ^> Remove Device
echo.
pause
