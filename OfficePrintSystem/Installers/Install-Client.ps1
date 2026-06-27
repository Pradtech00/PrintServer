<#
.SYNOPSIS
    Install Office Print Client (Dashboard)
.DESCRIPTION
    Installs the Office Print Client dashboard application
    and creates desktop shortcuts.
#>

$ErrorActionPreference = "Stop"
$SourceDir = Join-Path $PSScriptRoot "Client"
$InstallDir = "$env:LOCALAPPDATA\OfficePrintClient"
$DesktopPath = [Environment]::GetFolderPath("Desktop")
$StartMenuPath = "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\Office Print Client"

Write-Host "Office Print Client Installer" -ForegroundColor Cyan
Write-Host "=============================" -ForegroundColor Cyan

# 1. Copy files
Write-Host "Copying files to $InstallDir..." -ForegroundColor Yellow
if (Test-Path $InstallDir) {
    Remove-Item "$InstallDir\*" -Recurse -Force
}
New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
Copy-Item "$SourceDir\*" $InstallDir -Recurse -Force

# 2. Create shortcuts
Write-Host "Creating shortcuts..." -ForegroundColor Yellow
$exePath = "$InstallDir\OfficePrintClient.WPF.exe"

$shell = New-Object -ComObject WScript.Shell

# Desktop shortcut
$desktopShortcut = $shell.CreateShortcut("$DesktopPath\Office Print Client.lnk")
$desktopShortcut.TargetPath = $exePath
$desktopShortcut.WorkingDirectory = $InstallDir
$desktopShortcut.Description = "Office Print Client Dashboard"
$desktopShortcut.Save()

# Start Menu shortcut
New-Item -ItemType Directory -Path $StartMenuPath -Force | Out-Null
$startMenuShortcut = $shell.CreateShortcut("$StartMenuPath\Office Print Client.lnk")
$startMenuShortcut.TargetPath = $exePath
$startMenuShortcut.WorkingDirectory = $InstallDir
$startMenuShortcut.Description = "Office Print Client Dashboard"
$startMenuShortcut.Save()

Write-Host ""
Write-Host "Installation complete!" -ForegroundColor Green
Write-Host "Shortcuts created on Desktop and Start Menu." -ForegroundColor Green
Write-Host ""
Write-Host "Press any key to launch Office Print Client..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

Start-Process $exePath
