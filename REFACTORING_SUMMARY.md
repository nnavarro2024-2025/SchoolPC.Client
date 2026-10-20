# SchoolPC.Client Refactoring Summary
## Direct MySQL + Crash Recovery + Heartbeat Implementation

---

## CHANGES COMPLETED (Steps 1-16)

### 1. **REMOVED: SignalR Integration**
   - ❌ Deleted: `SchoolPC.Client/SignalRClient.cs` (was empty anyway)
   - ❌ Removed NuGet: `Microsoft.AspNetCore.SignalR.Client` v10.0.0
   - ❌ Removed from MainForm.cs:
	 - `private SignalRClient? _signalRClient` field
	 - `ConnectToAdminServer()` method
	 - `OnAdminMessageReceived()` handler
	 - `OnForceLogout()` handler
	 - SignalR calls in `MainForm_Load()`
	 - SignalR calls in `PerformLogout()`
	 - `public SignalRClient? SignalRClient` property
   - ❌ Removed from ReportDialog.cs:
	 - SignalR check: `if (mainForm?.SignalRClient != null && mainForm.SignalRClient.IsConnected)`
	 - `await mainForm.SignalRClient.ReportProblemAsync()` call

---

### 2. **ADDED: Configuration Support (appsettings.json)**
   - ✅ Created: `appsettings.json` with Database section:
	 ```json
	 {
	   "Database": {
		 "Server": "localhost",
		 "Port": 3306,
		 "Database": "schoolpc_db",
		 "User": "root",
		 "Password": ""
	   }
	 }
	 ```
   - ✅ Updated `SchoolPC.Client.csproj`:
	 - Removed: `Microsoft.AspNetCore.SignalR.Client` NuGet package
	 - Added: `Microsoft.Extensions.Configuration` v10.0.0
	 - Added: `Microsoft.Extensions.Configuration.Json` v10.0.0
	 - Added: `<None Update="appsettings.json"><CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory></None>`
   - ✅ `appsettings.json` is copied to output directory on every build

---

### 3. **UPDATED: DatabaseService.cs** (Major Refactoring)

#### A. Configuration Loading (New Constructor)
   - Default constructor (no params) now reads from `appsettings.json`
   - Falls back to hardcoded defaults if file missing or invalid
   - Logs config errors to console; displays on login screen via `GetConfigError()`
   - New static method: `public static string? GetConfigError()` - returns config error if any
   - Old constructor (string params) preserved for backward compatibility

#### B. New Methods Added

   **1. `CrashRecoveryAsync(string clientGuid)` - Recovery on startup**
   ```csharp
   UPDATE sessions s
   JOIN pc_instances p ON p.client_guid = s.client_guid
   SET s.logout_time = GREATEST(s.login_time, p.last_seen),
	   s.duration_minutes = TIMESTAMPDIFF(MINUTE, s.login_time, 
							GREATEST(s.login_time, p.last_seen)),
	   s.end_reason = 'PC turned off unexpectedly',
	   s.recovered_at = NOW()
   WHERE s.client_guid = @guid AND s.logout_time IS NULL
   ```
   - Closes any orphaned sessions from previous crashes
   - Uses `p.last_seen` as estimated crash time (not boot time)
   - Fails silently if database unreachable (doesn't block startup)
   - Returns `true` on success or no-op, `false` on error (still doesn't block)

   **2. `RegisterOrUpdatePcAsync(string clientGuid, string machineName, string? ipAddress)` - PC registration**
   ```csharp
   INSERT INTO pc_instances 
	 (client_guid, first_pc_name, current_pc_name, display_name, ip_address, is_approved, is_active, last_seen) 
   VALUES 
	 (@clientGuid, @machineName, @machineName, @machineName, @ipAddress, 1, 1, NOW())
   ON DUPLICATE KEY UPDATE 
	 current_pc_name = @machineName,
	 ip_address = @ipAddress,
	 last_seen = NOW()
   ```
   - First run: Creates new pc_instances row with all fields
   - Subsequent runs: Updates only `current_pc_name`, `ip_address`, `last_seen` (preserves `display_name`)
   - Handles machine renames gracefully (uses UNIQUE key on `client_guid`)

   **3. `UpdatePcHeartbeatAsync(string clientGuid, string? ipAddress)` - Periodic update**
   ```csharp
   UPDATE pc_instances 
   SET last_seen = NOW(),
	   ip_address = @ipAddress
   WHERE client_guid = @clientGuid
   ```
   - Call every 60 seconds from MainForm heartbeat timer
   - Failures are silent (doesn't affect user)
   - Keeps PC marked as "alive" in admin dashboard

#### C. Updated Methods

   **1. `StartSessionAsync(...)` - Skip BYPASS_ADMIN**
   - Added check: if `studentId == "BYPASS_ADMIN"`, return `-1` without creating session
   - Avoids foreign key violation (BYPASS_ADMIN not in students table)
   - Logs message: `[DatabaseService] Skipping session insert for BYPASS_ADMIN`

   **2. `EndSessionAsync(int sessionId, string endReason = "logout")` - Set end reason**
   - Now accepts `endReason` parameter (default: "logout")
   - Skip if `sessionId <= 0` (no-op, returns true)
   - SQL now sets: `end_reason = @endReason`
   - Caller can pass: "logout", "windows shutdown", "PC turned off unexpectedly", etc.

---

### 4. **CREATED: NetworkUtility.cs** (New Utility Class)

```csharp
public static class NetworkUtility
{
	public static string? GetLocalLanIpAddress()
}
```

**Algorithm:**
1. Gets all network adapters that are `OperationalStatus.Up`
2. Excludes adapters by type: Loopback, Tunnel
3. Excludes by name/description: ZeroTier, VirtualBox, VMware, Hyper-V, vEthernet, Bluetooth, VPN
4. **Preferred**: Adapter with IPv4 + default gateway
5. **Fallback**: First remaining operational IPv4 adapter (if no preferred found)
6. **Returns**: IPv4 address string, or `null` if no suitable adapter found
7. **Never returns**: Made-up or invalid addresses; always real or null

**Example:**
- Your scenario: Ethernet 172.16.1.236 (gateway 172.16.1.1) → returns "172.16.1.236"
- ZeroTier adapter → automatically excluded
- Multiple adapters → picks one with gateway; if tie, picks first

---

### 5. **MAJOR REFACTOR: MainForm.cs**

#### A. Removed
- `private SignalRClient? _signalRClient` field
- `ConnectToAdminServer()` method (entire method, ~20 lines)
- `OnAdminMessageReceived(string message)` handler
- `OnForceLogout()` handler
- `public SignalRClient? SignalRClient` property
- All SignalR calls from `MainForm_Load()`
- All SignalR calls from `PerformLogout()`

#### B. Added

   **1. Heartbeat Timer**
   ```csharp
   private System.Windows.Forms.Timer? _heartbeatTimer;

   private void StartHeartbeat()
   {
	   _heartbeatTimer = new System.Windows.Forms.Timer();
	   _heartbeatTimer.Interval = 60000; // 60 seconds
	   _heartbeatTimer.Tick += async (s, e) => await OnHeartbeatTick();
	   _heartbeatTimer.Start();
   }
   ```
   - Starts in `MainForm_Load()` after session starts
   - Runs every 60 seconds
   - Calls `DatabaseService.UpdatePcHeartbeatAsync(Program.ClientGuid, ipAddress)`
   - Gets IP from `NetworkUtility.GetLocalLanIpAddress()`

   **2. Windows Shutdown Handler**
   ```csharp
   private async void OnWindowsSessionEnding(object sender, 
	   Microsoft.Win32.SessionEndingEventArgs e)
   {
	   e.Cancel = false; // Don't block shutdown
	   try {
		   StopHeartbeat();
		   await _sessionManager.EndSessionAsync(endReason: "windows shutdown");
	   }
	   catch (Exception ex) { /* log, don't crash */ }
   }
   ```
   - Hooks: `Microsoft.Win32.SystemEvents.SessionEnding`
   - Catches Windows shutdown, restart, user logoff
   - Sets `end_reason = 'windows shutdown'` (not marked as crash)
   - Executes quickly (doesn't block Windows)
   - Unhooks in `Dispose()`

   **3. Improved Disposal**
   ```csharp
   protected override void Dispose(bool disposing)
   {
	   if (disposing) {
		   Microsoft.Win32.SystemEvents.SessionEnding -= OnWindowsSessionEnding;
		   StopHeartbeat();
	   }
	   base.Dispose(disposing);
   }
   ```

#### C. Updated Methods

   **1. `MainForm_Load()`**
   - Removed: `ConnectToAdminServer()` call
   - Removed: SignalR notification
   - Added: `StartHeartbeat()` call
   - Added: `Microsoft.Win32.SystemEvents.SessionEnding += OnWindowsSessionEnding`

   **2. `PerformLogout()`**
   - Added: `StopHeartbeat()` call
   - Changed: `await _sessionManager.EndSessionAsync()` → `await _sessionManager.EndSessionAsync(endReason: "logout")`

   **3. `MainForm_FormClosing()`**
   - Updated to call improved `PerformLogout()`

---

### 6. **REFACTORED: ReportDialog.cs**

#### Removed
- `private bool _submitted = false` (unused)
- SignalR check logic
- `var mainForm = Owner as MainForm;` reference
- `if (mainForm?.SignalRClient != null && mainForm.SignalRClient.IsConnected)` check

#### Added
- Direct `DatabaseService.SubmitProblemReportAsync()` call
- Uses `Program.ClientGuid` for client tracking
- Uses `Environment.MachineName` for PC name
- Better error handling and user feedback

#### New Logic
```csharp
bool success = await _databaseService.SubmitProblemReportAsync(
	Program.ClientGuid, 
	pcName, 
	_studentId, 
	_studentName, 
	message
);

if (success) {
	MessageBox.Show("Problem reported successfully...", "Success", ...);
} else {
	MessageBox.Show("Failed to submit report...", "Error", ...);
	return; // Don't close dialog on failure
}
```

---

## STARTUP SEQUENCE (Planned for Program.cs - Step 17-18)

1. **Initialize**: `ApplicationConfiguration.Initialize()`
2. **Load ClientGuid**: `ClientGuid = ClientIdentityService.GetOrCreateClientGuid()`
3. **Crash Recovery**: `await db.CrashRecoveryAsync(ClientGuid)` [NEW]
4. **PC Registration**: `await db.RegisterOrUpdatePcAsync(ClientGuid, Environment.MachineName, ipAddress)` [NEW]
5. **Check AutoStartup**: `AutoStartupHelper.IsAutoStartupEnabled()` [EXISTING]
6. **Show Login**: `Application.Run(new LoginForm())`

---

## DATABASE SCHEMA REQUIREMENTS

The following columns must exist (you mentioned they do):

### `sessions` table
- ✅ `client_guid VARCHAR(36)` - NEW for recovery
- ✅ `end_reason VARCHAR(60) NULL` - NEW (you added)
- ✅ `recovered_at TIMESTAMP NULL` - NEW (you added)

### `pc_instances` table
- ✅ `client_guid VARCHAR(36) UNIQUE` - PRIMARY for registration
- ✅ `first_pc_name VARCHAR(100)` - Set once, never changed
- ✅ `current_pc_name VARCHAR(100)` - Updated on machine rename
- ✅ `display_name VARCHAR(100)` - Set by admin, never overwritten by client
- ✅ `ip_address VARCHAR(45) NULL` - Updated by heartbeat
- ✅ `is_approved BOOLEAN` - Set to 1 on first registration
- ✅ `is_active BOOLEAN` - Set to 1 on first registration
- ✅ `last_seen TIMESTAMP ON UPDATE CURRENT_TIMESTAMP` - Set by heartbeat & recovery

---

## CONFIG FILE BEHAVIOR

**If `appsettings.json` is missing or invalid:**
- App still starts (doesn't crash)
- Uses hardcoded defaults: `localhost`, `root`, empty password
- Error message stored in `DatabaseService.GetConfigError()`
- Error displayed on LoginForm when `TestConnectionAsync()` fails

**Example flow:**
```
1. Main() runs
2. DatabaseService constructor reads appsettings.json
3. If error → DatabaseService.GetConfigError() = "appsettings.json not found..."
4. LoginForm_Load() calls TestDatabaseConnection()
5. If connection fails → shows error on login screen
6. User sees: "Database connection failed" or config error message
```

---

## NEXT STEPS (Steps 17-18 REMAINING)

### Step 17: Update Program.cs
- Add: `using System.Net.NetworkInformation;` import
- Modify: `Main()` method to async
- Add: Crash recovery call after ClientGuid load
- Add: PC registration call after crash recovery
- Get IP address from `NetworkUtility.GetLocalLanIpAddress()`

### Step 18: Build & Verify
- Run full build
- Check for compilation errors
- Verify no broken references

---

## FILES MODIFIED

| File | Action | Key Changes |
|------|--------|-------------|
| `SchoolPC.Client.csproj` | Modified | Removed SignalR NuGet, added Config NuGet, added appsettings.json copy |
| `appsettings.json` | Modified | Added Database config section |
| `DatabaseService.cs` | Modified | Config loading, crash recovery, PC registration, heartbeat, skip BYPASS_ADMIN |
| `NetworkUtility.cs` | Created | New: `GetLocalLanIpAddress()` |
| `MainForm.cs` | Major Refactor | Removed SignalR, added heartbeat timer, added Windows shutdown handler |
| `ReportDialog.cs` | Refactored | Removed SignalR, direct DB submission |
| `SignalRClient.cs` | Deleted | Entire file removed |
| `Program.cs` | Pending | Will add crash recovery & registration calls |

---

## EXPECTED DATABASE BEHAVIOR

### On First Launch
1. **Crash Recovery**: No-op (pc_instances doesn't exist yet)
2. **Registration**: Creates pc_instances row
3. **Login**: Student logs in, session starts
4. **Heartbeat**: Updates last_seen every 60 seconds
5. **Logout**: Sets end_reason='logout', logout_time=NOW()

### On Power Loss / Crash (while student logged in)
1. **PC Restarts**
2. **Startup**: CrashRecoveryAsync finds orphaned sessions (logout_time IS NULL)
3. **Uses**: last_seen from pc_instances as estimated crash time
4. **Sets**: end_reason='PC turned off unexpectedly', recovered_at=NOW()
5. **Registration**: Updates pc_instances.last_seen=NOW()
6. **Heartbeat**: Continues updating

### On Windows Shutdown (normal)
1. **User initiates shutdown**
2. **SystemEvents.SessionEnding fires**
3. **OnWindowsSessionEnding() runs (fast)**
4. **Sets**: end_reason='windows shutdown', logout_time=NOW()
5. **Result**: Not marked as crash (admin knows it was intentional shutdown)

---

## DOES NOT CHANGE

✓ **NOT TOUCHED:**
- LockdownManager.cs (keyboard hook, Alt+Tab block)
- ClientIdentityService.cs (persistent GUID generation)
- LoginForm.cs (BCrypt authentication, bypass_admin security)
- SessionManager.cs (will only update step 17-18 to pass endReason)
- AdminForm.cs (if it exists; client doesn't have one)
- Everything else

---

**Ready to proceed with Steps 17-18 (Program.cs update + build verification)?**
