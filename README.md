# School PC Kiosk System - Client Application

## Overview
A comprehensive school lab kiosk PC lock authentication & attendance system for the University of the Immaculate Conception (UIC). This client application runs on each student PC and provides:

- **Fullscreen lockdown mode** at Windows startup
- **Student authentication** via MySQL database (will connect to UIC portal API later)
- **Session tracking** with login/logout timestamps and duration
- **Problem reporting** for hardware/software issues
- **Remote control** via SignalR for admin commands (shutdown, restart, logout, lock)
- **Deep OS-level control** blocking Alt+Tab, Task Manager, Win key, etc.

## System Requirements

- **OS**: Windows 10/11 (with administrative privileges)
- **.NET**: .NET 10 (or .NET 8/9)
- **Database**: XAMPP MySQL (localhost:3306)
- **Admin Server**: SignalR Hub (localhost:5000/adminHub) - to be built separately

## Installation & Setup

### 1. Install XAMPP
1. Download and install XAMPP from https://www.apachefriends.org/
2. Start Apache and MySQL from XAMPP Control Panel

### 2. Create MySQL Database
Open phpMyAdmin (http://localhost/phpmyadmin) and run the following SQL:

```sql
CREATE DATABASE IF NOT EXISTS schoolpc_db;
USE schoolpc_db;

-- Students table (will be replaced by UIC portal API later)
CREATE TABLE IF NOT EXISTS students (
	student_id VARCHAR(50) PRIMARY KEY,
	username VARCHAR(100) NOT NULL UNIQUE,
	password_hash VARCHAR(255) NOT NULL,
	full_name VARCHAR(200) NOT NULL,
	email VARCHAR(150),
	is_active BOOLEAN DEFAULT TRUE,
	created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Admin accounts
CREATE TABLE IF NOT EXISTS admin_accounts (
	admin_id INT AUTO_INCREMENT PRIMARY KEY,
	username VARCHAR(100) NOT NULL UNIQUE,
	password_hash VARCHAR(255) NOT NULL,
	full_name VARCHAR(200) NOT NULL,
	role VARCHAR(50) DEFAULT 'admin',
	is_active BOOLEAN DEFAULT TRUE,
	created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Test/Guest accounts
CREATE TABLE IF NOT EXISTS test_accounts (
	test_id INT AUTO_INCREMENT PRIMARY KEY,
	username VARCHAR(100) NOT NULL UNIQUE,
	password_hash VARCHAR(255) NOT NULL,
	full_name VARCHAR(200) NOT NULL,
	notes TEXT,
	is_active BOOLEAN DEFAULT TRUE,
	created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Banned students
CREATE TABLE IF NOT EXISTS banned_students (
	ban_id INT AUTO_INCREMENT PRIMARY KEY,
	student_id VARCHAR(50) NOT NULL,
	reason TEXT,
	banned_by VARCHAR(100),
	banned_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
	FOREIGN KEY (student_id) REFERENCES students(student_id)
);

-- Session logs
CREATE TABLE IF NOT EXISTS sessions (
	session_id INT AUTO_INCREMENT PRIMARY KEY,
	pc_name VARCHAR(100) NOT NULL,
	student_id VARCHAR(50) NOT NULL,
	student_name VARCHAR(200) NOT NULL,
	login_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
	logout_time TIMESTAMP NULL,
	duration_minutes INT,
	FOREIGN KEY (student_id) REFERENCES students(student_id)
);

-- Problem reports
CREATE TABLE IF NOT EXISTS problem_reports (
	report_id INT AUTO_INCREMENT PRIMARY KEY,
	pc_name VARCHAR(100) NOT NULL,
	student_id VARCHAR(50) NOT NULL,
	student_name VARCHAR(200) NOT NULL,
	message TEXT NOT NULL,
	status VARCHAR(50) DEFAULT 'open',
	created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
	resolved_at TIMESTAMP NULL,
	resolved_by VARCHAR(100)
);

-- Insert sample test data
INSERT INTO students (student_id, username, password_hash, full_name, email) 
VALUES 
	('2024-001', 'jdoe', SHA2('password123', 256), 'John Doe', 'jdoe@uic.edu.ph'),
	('2024-002', 'msmith', SHA2('password123', 256), 'Mary Smith', 'msmith@uic.edu.ph');

INSERT INTO admin_accounts (username, password_hash, full_name, role) 
VALUES 
	('admin', SHA2('admin123', 256), 'System Administrator', 'super_admin');
```

**Test Credentials:**
- Student: `jdoe` / `password123`
- Student: `msmith` / `password123`
- Admin: `admin` / `admin123`

### 3. Configure Database Connection
The default connection settings are:
- **Server**: localhost
- **Port**: 3306
- **Database**: schoolpc_db
- **Username**: root
- **Password**: (empty)

To change these, modify the `DatabaseService` constructor in `DatabaseService.cs`.

### 4. Build and Run
1. Open the solution in Visual Studio 2026
2. Build the solution (Ctrl+Shift+B)
3. Run the application (F5)
4. The app will prompt to enable auto-startup (optional)

### 5. Configure Auto-Startup (Production)
For production deployment on lab PCs:
1. Run the app **as Administrator** once
2. Click "Yes" when prompted to enable auto-startup
3. The app will now launch at Windows startup and lock the desktop

## Features

### 🔒 Lockdown Mode
When no student is logged in, the app:
- Shows a fullscreen login screen
- Blocks Alt+Tab, Win key, Ctrl+Alt+Del, Task Manager
- Prevents shutdown/restart without login
- Monitors and kills blocked processes (cmd, powershell, regedit, etc.)

### 🎓 Student Authentication
- Validates username/password against MySQL database
- Checks if student is banned (blocks login even with valid credentials)
- Uses SHA-256 password hashing
- Will integrate with UIC portal API in the future

### ⏱️ Session Tracking
- Logs login time, logout time, and duration
- Displays live session info (student name, PC name, elapsed time)
- Stores session history in database for admin reports

### 🚨 Problem Reporting
- Students can report PC issues (hardware, software)
- Reports are sent to database with PC name, student ID, and timestamp
- Admin can view and resolve reports via admin dashboard

### 🌐 SignalR Communication
The client connects to an admin SignalR hub (to be built separately) for:
- Real-time status updates (online/offline, logged-in student)
- Remote commands: Shutdown, Restart, Logout, Lock
- Admin messages displayed to students
- Heartbeat for connection monitoring

## Architecture

### Core Components

1. **LoginForm** - Fullscreen kiosk-mode login UI
2. **MainForm** - Post-login desktop with session info and controls
3. **LockdownManager** - Win32 API wrapper for OS-level lockdown
4. **DatabaseService** - MySQL connectivity for authentication and logging
5. **SessionManager** - Tracks student sessions
6. **SignalRClient** - Real-time communication with admin server
7. **ReportDialog** - Problem reporting UI
8. **AutoStartupHelper** - Registry configuration for Windows auto-startup

### Data Flow

```
Windows Startup → LoginForm (Locked) → Authenticate → MainForm (Unlocked)
					   ↓                                      ↓
				  LockdownManager                      SessionManager
					   ↓                                      ↓
				  Block Input/Keys                    Log to Database
															  ↓
														 SignalR Server
															  ↓
													  Admin Dashboard
```

## Next Steps

### Phase 1: Current (Client-side foundation) ✅
- [x] Login form with lockdown
- [x] MySQL authentication
- [x] Session tracking
- [x] Problem reporting
- [x] SignalR client
- [x] Auto-startup

### Phase 2: Admin Server (To be built)
- [ ] ASP.NET Core SignalR Hub
- [ ] Admin dashboard (WinForms/WPF or Web)
- [ ] Real-time PC monitoring
- [ ] Remote control commands
- [ ] Session reports (Excel export)
- [ ] Account management (test accounts, ban students)

### Phase 3: UIC Integration
- [ ] Replace MySQL auth with UIC portal API
- [ ] Active Directory integration (if available)
- [ ] Sync student data from school database

## Troubleshooting

### Database Connection Failed
- Ensure XAMPP MySQL is running
- Check phpMyAdmin is accessible at http://localhost/phpmyadmin
- Verify database `schoolpc_db` exists
- Check MySQL port is 3306

### Lockdown Not Working
- Run the app **as Administrator** for full lockdown capabilities
- Some features (blocking Ctrl+Alt+Del) require system-level privileges
- Test on a non-admin account to verify lockdown effectiveness

### SignalR Connection Failed
- The admin server hasn't been built yet
- The app will work without SignalR (local-only mode)
- To disable SignalR warnings, comment out `ConnectToAdminServer()` in MainForm

### Auto-Startup Not Working
- Requires user-level registry permissions (HKEY_CURRENT_USER)
- Check Registry Editor: `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run`
- For system-wide startup, use Task Scheduler or Windows Service (requires admin)

## Security Notes

⚠️ **Important Security Considerations:**

1. **Password Storage**: Currently using SHA-256 hashing. For production, consider using bcrypt or Argon2 for stronger protection.

2. **Database Security**: Default XAMPP MySQL has no root password. Set a strong password before production deployment.

3. **Admin Privileges**: The app requires admin privileges for full lockdown. Deploy via Group Policy or SCCM in production.

4. **Network Security**: SignalR uses HTTP by default. Use HTTPS in production to encrypt admin commands.

5. **Bypass Prevention**: Determined users with physical access can still boot to Safe Mode or BIOS. Consider BIOS password protection and secure boot policies.

## License

This is an academic project for the University of the Immaculate Conception. All rights reserved.

## Support

For issues or questions, contact the development team or refer to the internal IT department documentation.
