@echo off
title Office Print Client Installer
echo =====================================
echo  Office Print Client - One Click Setup
echo  Created by Bagus Pradika ^| 2026
echo =====================================
echo.

cd /d "%~dp0"
set "APP_DIR=%LOCALAPPDATA%\OfficePrintClient"

echo [1/2] Copying files...
if not exist "%APP_DIR%" mkdir "%APP_DIR%"
copy /Y "OfficePrintClient.WPF.exe" "%APP_DIR%\OfficePrintClient.WPF.exe" >nul

echo [2/2] Creating shortcuts...

:: Desktop shortcut
set "PS1=$s=(New-Object -ComObject WScript.Shell).CreateShortcut('%USERPROFILE%\Desktop\Office Print Client.lnk');$s.TargetPath='%APP_DIR%\OfficePrintClient.WPF.exe';$s.Save()"
powershell -Command "%PS1%" >nul

:: Start Menu shortcut
set "PS2=$sm='%APPDATA%\Microsoft\Windows\Start Menu\Programs\Office Print Client';if(!(Test-Path $sm)){mkdir $sm|Out-Null};$s=(New-Object -ComObject WScript.Shell).CreateShortcut($sm+'\Office Print Client.lnk');$s.TargetPath='%APP_DIR%\OfficePrintClient.WPF.exe';$s.Save()"
powershell -Command "%PS2%" >nul

echo.
echo =====================================
echo  INSTALLATION COMPLETE!
echo =====================================
echo.
echo Shortcuts created on Desktop and Start Menu.
echo.
start "" "%APP_DIR%\OfficePrintClient.WPF.exe"
echo The client dashboard is now launching...
echo.
pause
