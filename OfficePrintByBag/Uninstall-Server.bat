@echo off
title Office Print Server Uninstaller
echo =====================================
echo  Office Print Server - Uninstall
echo  Created by Bagus Pradika ^| 2026
echo =====================================
echo.

REM Self-elevate to admin
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo Requesting administrator privileges...
    powershell -Command "Start-Process '%~f0' -Verb RunAs"
    exit /b
)

echo [1/3] Stopping and removing service...
sc stop OfficePrintServer >nul 2>&1
timeout /t 3 /nobreak >nul
sc delete OfficePrintServer >nul 2>&1

echo [2/3] Removing firewall rules...
netsh advfirewall firewall delete rule name="Office Print Server IPP" >nul
netsh advfirewall firewall delete rule name="Office Print Server RAW" >nul

echo [3/3] Removing files...
rmdir /S /Q "%ProgramFiles%\OfficePrintServer" >nul 2>&1

echo.
echo =====================================
echo  UNINSTALL COMPLETE!
echo =====================================
echo.
pause
