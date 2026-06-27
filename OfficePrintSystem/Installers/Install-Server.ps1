<#
.SYNOPSIS
    Install Office Print Server as a Windows Service
.DESCRIPTION
    Installs the Office Print Server files, registers as Windows Service,
    opens firewall ports 18080 and 9100, and starts the service.
.NOTES
    Created by Bagus Pradika | 2026
    Must be run as Administrator
#>

#Requires -RunAsAdministrator

$ErrorActionPreference = "Stop"
$SourceDir = Join-Path $PSScriptRoot "Server"
$InstallDir = "$env:ProgramFiles\OfficePrintServer"

Write-Host "Office Print Server Installer" -ForegroundColor Cyan
Write-Host "==============================" -ForegroundColor Cyan

# 1. Copy files
Write-Host "Copying files to $InstallDir..." -ForegroundColor Yellow
if (Test-Path $InstallDir) {
    Remove-Item "$InstallDir\*" -Recurse -Force
}
New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
Copy-Item "$SourceDir\*" $InstallDir -Recurse -Force

# 2. Stop existing service if running
$service = Get-Service -Name "OfficePrintServer" -ErrorAction SilentlyContinue
if ($service) {
    Write-Host "Stopping existing service..." -ForegroundColor Yellow
    Stop-Service -Name "OfficePrintServer" -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    & sc.exe delete OfficePrintServer 2>&1 | Out-Null
}

# 3. Register Windows Service
Write-Host "Registering Windows Service..." -ForegroundColor Yellow
$serviceExe = "$InstallDir\OfficePrintServer.Service.exe"
& sc.exe create OfficePrintServer binPath= "`"$serviceExe`"" start= auto 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to create service"
    exit 1
}
Write-Host "Service registered successfully." -ForegroundColor Green

# 4. Configure service description
& sc.exe description OfficePrintServer "Office Print Server - IPP + Raw TCP Print Service (Port 18080 + 9100)" 2>&1 | Out-Null

# 5. Open firewall ports
Write-Host "Configuring Windows Firewall..." -ForegroundColor Yellow
$rules = @(
    @{Name="Office Print Server IPP"; Port=18080},
    @{Name="Office Print Server RAW"; Port=9100}
)
foreach ($rule in $rules) {
    $existing = Get-NetFirewallRule -DisplayName $rule.Name -ErrorAction SilentlyContinue
    if (-not $existing) {
        New-NetFirewallRule -DisplayName $rule.Name -Direction Inbound -Protocol TCP -LocalPort $rule.Port -Action Allow | Out-Null
        Write-Host "Firewall rule $($rule.Name) added." -ForegroundColor Green
    } else {
        Write-Host "Firewall rule $($rule.Name) already exists." -ForegroundColor Gray
    }
}

# 6. Set service recovery options (restart on failure)
& sc.exe failure OfficePrintServer reset= 86400 actions= restart/30000/restart/60000/restart/120000 2>&1 | Out-Null

# 7. Start the service
Write-Host "Starting service..." -ForegroundColor Yellow
Start-Service -Name "OfficePrintServer"
Write-Host "Service started successfully." -ForegroundColor Green

Write-Host ""
Write-Host "Installation complete!" -ForegroundColor Green
Write-Host "Server is running on ports 18080 (IPP) + 9100 (Raw TCP)" -ForegroundColor Green
Write-Host "Add printer on client using Standard TCP/IP Port: IP=<SERVER_IP>, Port=9100" -ForegroundColor White
Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
