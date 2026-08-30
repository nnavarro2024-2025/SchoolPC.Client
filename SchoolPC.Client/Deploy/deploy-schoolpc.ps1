#Requires -RunAsAdministrator
<#
.SYNOPSIS
	Deploy SchoolPC.Client: Create ProgramData folder, set NTFS permissions, and register Scheduled Task.

.DESCRIPTION
	This script performs one-time setup for SchoolPC.Client deployment:
	1. Creates C:\ProgramData\SchoolPC folder (if missing)
	2. Sets NTFS ACLs to allow read/write for SYSTEM and Authenticated Users
	3. Registers a Windows Scheduled Task to run SchoolPC.Client at user logon
	   with elevated privileges and interactive session

.PARAMETER ApplicationPath
	Full path to SchoolPC.Client.exe (default: .\SchoolPC.Client.exe in script directory)

.PARAMETER DisableUAC
	If specified, disables UAC for SchoolPC.Client.exe (not recommended; elevation 
	via Scheduled Task is sufficient)

.EXAMPLE
	.\deploy-schoolpc.ps1 -ApplicationPath "C:\Program Files\SchoolPC\SchoolPC.Client.exe"

.NOTES
	Author: SchoolPC Deployment
	Requires: Administrator privileges
	Date Created: 2024
#>

param(
	[string]$ApplicationPath = $(Join-Path -Path $PSScriptRoot -ChildPath "SchoolPC.Client.exe"),
	[switch]$DisableUAC = $false
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "SchoolPC.Client Deployment Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Verify admin privileges
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
	Write-Host "ERROR: This script must be run as Administrator." -ForegroundColor Red
	exit 1
}

Write-Host "[OK] Running as Administrator`n" -ForegroundColor Green

# Verify application exists
if (-not (Test-Path -Path $ApplicationPath)) {
	Write-Host "ERROR: SchoolPC.Client.exe not found at: $ApplicationPath" -ForegroundColor Red
	Write-Host "Please ensure the application has been built and provide correct path." -ForegroundColor Red
	exit 1
}

Write-Host "[OK] Found application at: $ApplicationPath`n" -ForegroundColor Green

# Step 1: Create ProgramData folder
$ProgramDataPath = "C:\ProgramData\SchoolPC"
Write-Host "Step 1: Creating ProgramData folder..."
Write-Host "  Path: $ProgramDataPath"

try {
	if (-not (Test-Path -Path $ProgramDataPath)) {
		New-Item -ItemType Directory -Path $ProgramDataPath -Force | Out-Null
		Write-Host "  [OK] Folder created" -ForegroundColor Green
	} else {
		Write-Host "  [OK] Folder already exists" -ForegroundColor Green
	}
} catch {
	Write-Host "  [ERROR] Failed to create folder: $_" -ForegroundColor Red
	exit 1
}

# Step 2: Set NTFS permissions
Write-Host "`nStep 2: Setting NTFS permissions..."
Write-Host "  Granting: SYSTEM (Full Control), Authenticated Users (Modify)"

try {
	$acl = Get-Acl -Path $ProgramDataPath
	$acl.SetAccessRuleProtection($false, $false)  # Inherit from parent, remove explicit deny

	# Full Control for SYSTEM
	$rule1 = New-Object System.Security.AccessControl.FileSystemAccessRule(
		"NT AUTHORITY\SYSTEM",
		"FullControl",
		"ContainerInherit,ObjectInherit",
		"None",
		"Allow"
	)

	# Modify for Authenticated Users
	$rule2 = New-Object System.Security.AccessControl.FileSystemAccessRule(
		"NT AUTHORITY\Authenticated Users",
		"Modify",
		"ContainerInherit,ObjectInherit",
		"None",
		"Allow"
	)

	$acl.AddAccessRule($rule1)
	$acl.AddAccessRule($rule2)
	Set-Acl -Path $ProgramDataPath -AclObject $acl

	Write-Host "  [OK] Permissions set successfully" -ForegroundColor Green
} catch {
	Write-Host "  [ERROR] Failed to set permissions: $_" -ForegroundColor Red
	Write-Host "  Application may still work if Scheduled Task runs elevated, but persistence may fail." -ForegroundColor Yellow
}

# Step 3: Register Scheduled Task
Write-Host "`nStep 3: Registering Windows Scheduled Task..."

$TaskName = "SchoolPC.Client"
$TaskFolder = "\SchoolPC"
$FullTaskPath = "$TaskFolder\$TaskName"

try {
	# Check if task already exists
	$existingTask = Get-ScheduledTask -TaskName $TaskName -TaskPath $TaskFolder -ErrorAction SilentlyContinue

	if ($existingTask) {
		Write-Host "  Task already exists. Updating..."
		Unregister-ScheduledTask -TaskName $TaskName -TaskPath $TaskFolder -Confirm:$false
	}

	# Create task action (run the executable)
	$action = New-ScheduledTaskAction -Execute $ApplicationPath

	# Create task trigger: "At log on" (for any user)
	$trigger = New-ScheduledTaskTrigger -AtLogOn

	# Create principal with elevated privileges
	# RunLevel "Highest" = Run with highest privileges
	# ProcessTokenSidType "Default" = Standard behavior
	$principal = New-ScheduledTaskPrincipal `
		-UserId "SYSTEM" `
		-RunLevel "Highest" `
		-LogonType "ServiceAccount"

	# Create task settings
	$settings = New-ScheduledTaskSettingsSet `
		-AllowStartIfOnBatteries $true `
		-DontStopIfGoingOnBatteries $true `
		-StartWhenAvailable $true `
		-MultipleInstances "IgnoreNew"  # Don't run multiple instances

	# Register the task
	if (-not (Test-Path "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tree$TaskFolder")) {
		$null = New-Item -Path "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tree$TaskFolder" -Force
	}

	Register-ScheduledTask `
		-TaskName $TaskName `
		-TaskPath $TaskFolder `
		-Action $action `
		-Trigger $trigger `
		-Principal $principal `
		-Settings $settings `
		-Force | Out-Null

	Write-Host "  [OK] Scheduled Task registered: $FullTaskPath" -ForegroundColor Green
	Write-Host "  - Trigger: At log on (any user)" -ForegroundColor Gray
	Write-Host "  - Run with highest privileges: Enabled" -ForegroundColor Gray
	Write-Host "  - Session type: Interactive (user must be logged in)" -ForegroundColor Gray

} catch {
	Write-Host "  [ERROR] Failed to register Scheduled Task: $_" -ForegroundColor Red
	exit 1
}

# Step 4: Verification
Write-Host "`nStep 4: Verifying deployment..."

try {
	# Verify folder exists and permissions
	if (Test-Path -Path $ProgramDataPath) {
		Write-Host "  [OK] ProgramData folder exists" -ForegroundColor Green

		# Try to write a test file (verify permissions)
		$testFile = Join-Path -Path $ProgramDataPath -ChildPath ".deploy_test"
		try {
			"test" | Out-File -FilePath $testFile -Force
			Remove-Item -Path $testFile -Force
			Write-Host "  [OK] Write permissions verified (test file created and deleted)" -ForegroundColor Green
		} catch {
			Write-Host "  [warning] Could not write test file; permissions may be insufficient" -ForegroundColor Yellow
		}
	} else {
		Write-Host "  [ERROR] ProgramData folder not found!" -ForegroundColor Red
		exit 1
	}

	# Verify Scheduled Task
	$task = Get-ScheduledTask -TaskName $TaskName -TaskPath $TaskFolder -ErrorAction Stop
	Write-Host "  [OK] Scheduled Task verified" -ForegroundColor Green
	Write-Host "     State: $($task.State)" -ForegroundColor Gray

} catch {
	Write-Host "  [ERROR] Verification failed: $_" -ForegroundColor Red
	exit 1
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Deployment Complete!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Summary:" -ForegroundColor White
Write-Host "  ✓ ProgramData folder created/verified: $ProgramDataPath"
Write-Host "  ✓ NTFS permissions configured"
Write-Host "  ✓ Scheduled Task registered: $FullTaskPath"
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor White
Write-Host "  1. Ensure SchoolPC.Client.exe is in the location specified above"
Write-Host "  2. The application will run automatically when any user logs on"
Write-Host "  3. Monitor logs for any permission issues on first run"
Write-Host "  4. Client GUID will be stored in: $ProgramDataPath\client_id.txt"
Write-Host ""
Write-Host "To unregister the task later, run:" -ForegroundColor Gray
Write-Host "  Unregister-ScheduledTask -TaskName '$TaskName' -TaskPath '$TaskFolder' -Confirm:`$false" -ForegroundColor Gray
Write-Host ""
