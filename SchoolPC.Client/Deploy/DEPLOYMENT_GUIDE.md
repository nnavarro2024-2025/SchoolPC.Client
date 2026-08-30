# SchoolPC.Client Persistent GUID Implementation - Deployment Guide

## Overview

This document explains how to deploy the persistent client GUID feature for SchoolPC.Client. This feature ensures that each physical PC is uniquely identified with a GUID that persists even if the Windows machine name is changed.

**Key Components:**
- **ClientIdentityService.cs**: Manages GUID creation/retrieval from persistent storage
- **deploy-schoolpc.ps1**: PowerShell setup script (run once during deployment)
- **schema-migration.sql**: Database migration to track client GUIDs

---

## ⚡ Quick Start

### Prerequisites
- Windows Server 2016+ or Windows 10+
- .NET 10 runtime installed
- MySQL 8.0+ database running and accessible
- PowerShell 5.1+ (run as Administrator)
- SchoolPC.Client.exe built and ready (Release build recommended)

### Step 1: Run the Deployment Script (One-time Setup)

Run this **once per PC** as Administrator:

```powershell
# Open PowerShell as Administrator
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force
cd "C:\path\to\SchoolPC.Client\Deploy"
.\deploy-schoolpc.ps1 -ApplicationPath "C:\path\to\SchoolPC.Client.exe"
```

**What this script does:**
1. ✅ Creates `C:\ProgramData\SchoolPC` folder
2. ✅ Sets NTFS permissions for SYSTEM and Authenticated Users
3. ✅ Registers Windows Scheduled Task to run at user logon with elevated privileges
4. ✅ Verifies folder access with a test write operation

**Expected Output:**
```
========================================
SchoolPC.Client Deployment Script
========================================

[OK] Running as Administrator

[OK] Found application at: C:\path\to\SchoolPC.Client.exe

Step 1: Creating ProgramData folder...
  Path: C:\ProgramData\SchoolPC
  [OK] Folder created

Step 2: Setting NTFS permissions...
  Granting: SYSTEM (Full Control), Authenticated Users (Modify)
  [OK] Permissions set successfully

Step 3: Registering Windows Scheduled Task...
  Task already exists. Updating...
  [OK] Scheduled Task registered: \SchoolPC\SchoolPC.Client

Step 4: Verifying deployment...
  [OK] ProgramData folder exists
  [OK] Write permissions verified
  [OK] Scheduled Task verified

========================================
Deployment Complete!
========================================
```

### Step 2: Migrate Database Schema

Run this **once** against your MySQL database. Use the consolidated `database_setup.sql` that includes the client_guid columns:

```bash
# Option A: Via phpMyAdmin
# 1. Open phpMyAdmin (http://localhost/phpmyadmin)
# 2. Click "SQL" tab (top menu)
# 3. Paste contents of Deploy/database_setup.sql
# 4. Click "Go" to execute

# Option B: Via MySQL CLI (Command Line)
mysql -u root -p schoolpc_db < Deploy/database_setup.sql

# Option C: Copy-paste into MySQL Console
# 1. Open MySQL console or phpMyAdmin SQL editor
# 2. Copy entire contents of Deploy/database_setup.sql
# 3. Paste and execute
```

**What this script does:**
- Creates all tables with proper schema
- Adds `client_guid VARCHAR(36)` column to `sessions` table (NEW!)
- Adds `client_guid VARCHAR(36)` column to `problem_reports` table (NEW!)
- Creates optional `pc_instances` table for tracking machine rename history (NEW!)
- Inserts student accounts and admin accounts
- Creates indexes on client_guid for better query performance

**Note:** This replaces the old separate `schema-migration.sql` with one consolidated script!

**Verification:**
```sql
-- Verify in MySQL:
DESCRIBE sessions;  -- Check for client_guid column
DESCRIBE problem_reports;  -- Check for client_guid column
DESCRIBE pc_instances;  -- Optional, for rename tracking
```

### Step 3: Verify Deployment

After running steps 1 & 2, verify everything is working:

**Check 1: GUID File Created**
```powershell
# On the client PC, check if the GUID was created:
Get-Content "C:\ProgramData\SchoolPC\client_id.txt"
# Should output a GUID like: 550e8400-e29b-41d4-a716-446655440000
```

**Check 2: Scheduled Task Running**
```powershell
# Verify the Scheduled Task exists and is configured correctly:
Get-ScheduledTask -TaskName "SchoolPC.Client" -TaskPath "\SchoolPC"

# Check task status:
Get-ScheduledTask -TaskName "SchoolPC.Client" | Select-Object -Property State, LastTaskResult
```

**Check 3: Database Recording Client GUID**
```sql
-- Query the most recent session to verify client_guid is stored:
SELECT session_id, client_guid, pc_name, student_id, login_time 
FROM sessions 
ORDER BY login_time DESC 
LIMIT 1;

-- Similarly for problem reports:
SELECT report_id, client_guid, pc_name, student_id, created_at 
FROM problem_reports 
ORDER BY created_at DESC 
LIMIT 1;
```

---

## 🔄 How It Works

### On First Run:
1. Application launches (via Scheduled Task at user logon with elevated privileges)
2. `Program.cs` calls `ClientIdentityService.GetOrCreateClientGuid()`
3. Service checks for `C:\ProgramData\SchoolPC\client_id.txt`
4. File doesn't exist, so service generates a new GUID via `Guid.NewGuid()`
5. Service attempts to write GUID to file
6. If successful, GUID is persisted; if not, a warning is logged but app continues
7. `Program.ClientGuid` property is set and accessible globally

### On Subsequent Runs:
1. Application launches
2. `Program.cs` calls `ClientIdentityService.GetOrCreateClientGuid()`
3. Service finds existing `C:\ProgramData\SchoolPC\client_id.txt`
4. Service reads and validates GUID (must be valid UUID format)
5. If valid, uses existing GUID; if corrupted, regenerates new one
6. `Program.ClientGuid` property is set to persistent GUID

### SignalR Hub Communication:
- Every hub call now includes `Program.ClientGuid` as first parameter:
  - `RegisterClient(clientGuid, machineName)`
  - `UpdateClientStatus(clientGuid, machineName, studentId, ...)`
  - `Heartbeat(clientGuid, machineName)`
- Server-side SignalR hub can now uniquely identify PCs regardless of machine name changes

### Database Recording:
- `SessionManager.StartSessionAsync()` now passes `Program.ClientGuid` to database
- `ReportDialog` now passes `Program.ClientGuid` when submitting problem reports
- Both methods store the GUID alongside machine name in database

---

## ✅ Troubleshooting

### Problem: "Permission denied" when writing to C:\ProgramData\SchoolPC

**Cause:** NTFS permissions not set correctly during deployment

**Solution:**
```powershell
# 1. Re-run deployment script with elevated privileges:
.\deploy-schoolpc.ps1 -ApplicationPath "C:\path\to\SchoolPC.Client.exe"

# 2. Or manually set permissions (if script fails):
$folder = "C:\ProgramData\SchoolPC"
$acl = Get-Acl $folder
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule("NT AUTHORITY\Authenticated Users", "Modify", "ContainerInherit,ObjectInherit", "None", "Allow")
$acl.AddAccessRule($rule)
Set-Acl $folder $acl
```

### Problem: Client GUID not persisting across restarts

**Cause:** File permissions issue or folder not created

**Solution:**
1. Check `C:\ProgramData\SchoolPC` exists and has write permissions:
   ```powershell
   Test-Path "C:\ProgramData\SchoolPC"
   ls -la "C:\ProgramData\SchoolPC"
   ```
2. Check console output when app starts (look for `[ClientIdentityService]` messages):
   - Search for `WARNING: Permission denied` messages
   - Indicates which operation failed

3. Manually create the file:
   ```powershell
   mkdir "C:\ProgramData\SchoolPC" -Force
   [Guid]::NewGuid() | Out-File "C:\ProgramData\SchoolPC\client_id.txt" -Encoding UTF8
   ```

### Problem: Scheduled Task not running

**Cause:** Task is disabled or not configured correctly

**Solution:**
```powershell
# 1. Check task exists:
Get-ScheduledTask -TaskName "SchoolPC.Client" -TaskPath "\SchoolPC" | Format-List

# 2. Enable task if disabled:
Enable-ScheduledTask -TaskName "SchoolPC.Client" -TaskPath "\SchoolPC"

# 3. Run task manually:
Start-ScheduledTask -TaskName "SchoolPC.Client" -TaskPath "\SchoolPC"

# 4. Check last run result:
$task = Get-ScheduledTask -TaskName "SchoolPC.Client" -TaskPath "\SchoolPC"
$task | Select-Object -Property LastTaskResult, LastRunTime
```

### Problem: Database column not added

**Cause:** Schema migration not executed or failed

**Solution:**
```sql
-- Check if columns exist:
DESCRIBE sessions;  -- Look for client_guid column
DESCRIBE problem_reports;  -- Look for client_guid column

-- If missing, manually add:
ALTER TABLE sessions 
ADD COLUMN client_guid VARCHAR(36) DEFAULT NULL 
AFTER pc_name;

ALTER TABLE problem_reports 
ADD COLUMN client_guid VARCHAR(36) DEFAULT NULL 
AFTER pc_name;
```

---

## 🔐 Security Considerations

### File Permissions
- `C:\ProgramData\SchoolPC` is world-writable (Modify for Authenticated Users)
- This is acceptable because the file contains an anonymized GUID (not a secret)
- For higher security environments, restrict to SYSTEM only

### GUID Uniqueness
- GUIDs are generated using `Guid.NewGuid()` (.NET cryptographic random)
- Probability of collision is astronomically low (128-bit random)
- Safe to use as unique identifier for up to millions of PCs

### Admin Elevation
- App runs with elevated privileges via Scheduled Task (not app.manifest)
- Eliminates UAC prompts that would break kiosk lockdown
- Scheduled Task elevation is transparent to user

---

## 📋 Deployment Checklist

- [ ] SchoolPC.Client.exe built successfully (Release configuration recommended)
- [ ] Run `deploy-schoolpc.ps1` as Administrator
- [ ] Verify script output shows all steps successful
- [ ] Verify `C:\ProgramData\SchoolPC\client_id.txt` exists and contains a GUID
- [ ] Run database migration (schema-migration.sql)
- [ ] Verify database columns added: `sessions.client_guid`, `problem_reports.client_guid`
- [ ] Test application launch (manually or wait for scheduled task at next logon)
- [ ] Check database for new session with populated `client_guid`
- [ ] Verify SignalR hub receives client_guid in RegisterClient message
- [ ] (Optional) Document the client GUID and PC identity in your inventory system

---

## 🔄 Post-Deployment

### For System Admins
1. Keep a record of which GUID is assigned to which physical PC
2. When a PC is renamed, the GUID will remain the same—this is expected!
3. Use the GUID in your monitoring/admin dashboard instead of machine name
4. If a PC needs a fresh GUID, delete `C:\ProgramData\SchoolPC\client_id.txt` and restart the app

### For Help Desk / Support
- Ask students: "What's your PC's GUID?" (They won't know it's client ID)
- Check `C:\ProgramData\SchoolPC\client_id.txt` to identify the PC
- Use GUID to look up sessions and problem reports in the database
- GUID survives Windows reinstalls and machine renames—it's the true PC identity

### For Developers
- `Program.ClientGuid` is accessible from anywhere in the code
- SignalR hub calls automatically include it (no manual passing required)
- Database methods include it in all relevant inserts
- Graceful error handling: if file can't be written, app continues with in-memory GUID

---

## 📞 Support

If you encounter issues:

1. **Check console output** for `[ClientIdentityService]` messages
2. **Verify file permissions** on `C:\ProgramData\SchoolPC`
3. **Run deployment script again** with all parameters:
   ```powershell
   .\deploy-schoolpc.ps1 -ApplicationPath "C:\path\to\SchoolPC.Client.exe"
   ```
4. **Review logs** in Windows Event Viewer → Windows Logs → System (for Scheduled Task issues)

---

## Version History

- **v1.0** (2024): Initial persistent GUID implementation
  - Singleton ClientIdentityService
  - PowerShell deployment automation
  - Database schema updates with client_guid tracking
  - SignalR hub integration
