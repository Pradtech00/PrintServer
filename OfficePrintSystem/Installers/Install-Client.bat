@echo off
title Office Print Client Installer
echo =====================================
echo  Office Print Client - One Click Setup
echo =====================================
echo.

cd /d "%~dp0"

echo [1/2] Copying files...
xcopy /E /I /Y "Client\*" "%LOCALAPPDATA%\OfficePrintClient\" >nul

echo [2/2] Creating shortcuts...
powershell -Command "$ws = New-Object -ComObject WScript.Shell; $s = $ws.CreateShortcut([Environment]::GetFolderPath('Desktop') + '\Office Print Client.lnk'); $s.TargetPath = '%LOCALAPPDATA%\OfficePrintClient\OfficePrintClient.WPF.exe'; $s.Save()" >nul

powershell -Command "$sm = [Environment]::GetFolderPath('Programs') + '\Office Print Client'; if(!(Test-Path $sm)){mkdir $sm}; $ws = New-Object -ComObject WScript.Shell; $s = $ws.CreateShortcut($sm + '\Office Print Client.lnk'); $s.TargetPath = '%LOCALAPPDATA%\OfficePrintClient\OfficePrintClient.WPF.exe'; $s.Save()" >nul

echo.
echo =====================================
echo  INSTALLATION COMPLETE!
echo =====================================
echo.
echo Shortcuts created on Desktop and Start Menu.
echo.
start "" "%LOCALAPPDATA%\OfficePrintClient\OfficePrintClient.WPF.exe"
echo The client dashboard is now launching...
echo.
pause
