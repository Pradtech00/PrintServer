<#
.SYNOPSIS
    Uninstall Office Print Server
.DESCRIPTION
    Stops the service, removes it, and deletes firewall rules.
.NOTES
    Must be run as Administrator
#>

#Requires -RunAsAdministrator

$ErrorActionPreference = "Stop"
$InstallDir = "$env:ProgramFiles\OfficePrintServer"

Write-Host "Office Print Server Uninstaller" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan

# 1. Stop & delete service
$service = Get-Service -Name "OfficePrintServer" -ErrorAction SilentlyContinue
if ($service) {
    Write-Host "Stopping and removing service..." -ForegroundColor Yellow
    Stop-Service -Name "OfficePrintServer" -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    & sc.exe delete OfficePrintServer 2>&1 | Out-Null
    Write-Host "Service removed." -ForegroundColor Green
}

# 2. Remove firewall rules
Write-Host "Removing firewall rules..." -ForegroundColor Yellow
$ruleNames = @("Office Print Server IPP", "Office Print Server RAW")
foreach ($ruleName in $ruleNames) {
    $existing = Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue
    if ($existing) {
        Remove-NetFirewallRule -DisplayName $ruleName | Out-Null
        Write-Host "Firewall rule '$ruleName' removed." -ForegroundColor Green
    }
}

# 3. Remove files
if (Test-Path $InstallDir) {
    Write-Host "Removing files..." -ForegroundColor Yellow
    Remove-Item $InstallDir -Recurse -Force
    Write-Host "Files removed." -ForegroundColor Green
}

Write-Host ""
Write-Host "Uninstall complete!" -ForegroundColor Green
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
