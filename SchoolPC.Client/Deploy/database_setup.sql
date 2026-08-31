-- ============================================================================
-- SchoolPC Database Setup Script (COMPLETE WITH BCrypt SUPPORT)
-- ============================================================================
-- 
-- HOW TO USE THIS SCRIPT:
-- ========================
-- 1. Open phpMyAdmin (XAMPP Control Panel -> Admin for Apache MySQL)
-- 2. Click on "SQL" tab at the top
-- 3. Copy and paste THIS ENTIRE SCRIPT into the SQL editor
-- 4. Click "Go" button to execute
--
-- IMPORTANT NOTES:
-- ================
-- - This script will DROP and recreate schoolpc_db (removes all old data)
-- - Password hashes are BCrypt (generated in C#.NET via BCrypt.Net-Next)
-- - Do NOT modify password hashes here; they're placeholders
-- - client_guid columns are included for PC identification
-- ============================================================================

-- DROP existing database (careful!)
DROP DATABASE IF EXISTS schoolpc_db;

-- Create fresh database
CREATE DATABASE IF NOT EXISTS schoolpc_db;
USE schoolpc_db;

-- ============================================================================
-- MAIN TABLES
-- ============================================================================

-- Students table
CREATE TABLE IF NOT EXISTS students (
	student_id VARCHAR(50) PRIMARY KEY,
	username VARCHAR(100) NOT NULL UNIQUE,
	password_hash VARCHAR(255) NOT NULL COMMENT 'BCrypt hash (generated in C#, not SHA2)',
	full_name VARCHAR(200) NOT NULL,
	email VARCHAR(150),
	is_active BOOLEAN DEFAULT TRUE COMMENT 'FALSE = banned, TRUE = active',
	created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
	INDEX idx_username (username)
);

-- Admin accounts
CREATE TABLE IF NOT EXISTS admin_accounts (
	admin_id INT AUTO_INCREMENT PRIMARY KEY,
	username VARCHAR(100) NOT NULL UNIQUE,
	password_hash VARCHAR(255) NOT NULL COMMENT 'BCrypt hash (generated in C#, not SHA2)',
	full_name VARCHAR(200) NOT NULL,
	role VARCHAR(50) DEFAULT 'admin' COMMENT 'e.g., super_admin, admin, monitor',
	is_active BOOLEAN DEFAULT TRUE COMMENT 'FALSE = deactivated, TRUE = active',
	created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
	INDEX idx_username (username)
);

-- Test/Guest accounts (for testing purposes)
CREATE TABLE IF NOT EXISTS test_accounts (
	test_id INT AUTO_INCREMENT PRIMARY KEY,
	username VARCHAR(100) NOT NULL UNIQUE,
	password_hash VARCHAR(255) NOT NULL COMMENT 'BCrypt hash',
	full_name VARCHAR(200) NOT NULL,
	notes TEXT COMMENT 'Admin notes',
	is_active BOOLEAN DEFAULT TRUE,
	created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Banned students (audit trail with unban support)
CREATE TABLE IF NOT EXISTS banned_students (
	ban_id INT AUTO_INCREMENT PRIMARY KEY,
	student_id VARCHAR(50) NOT NULL,
	reason TEXT COMMENT 'Reason for ban',
	banned_by VARCHAR(100) COMMENT 'Admin username who banned',
	banned_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
	is_active BOOLEAN DEFAULT TRUE COMMENT 'FALSE = unbanned (record kept for history), TRUE = currently banned',
	FOREIGN KEY (student_id) REFERENCES students(student_id),
	INDEX idx_student_id (student_id),
	INDEX idx_is_active (is_active)
);

-- ============================================================================
-- SESSION & REPORT LOGGING (WITH PERSISTENT client_guid)
-- ============================================================================

-- Session logs: Student login/logout tracking
-- Uses client_guid (NOT pc_name) as primary join key for audit trail across PC renames
CREATE TABLE IF NOT EXISTS sessions (
	session_id INT AUTO_INCREMENT PRIMARY KEY,
	client_guid VARCHAR(36) COMMENT 'Persistent GUID of client PC (from C:\ProgramData\SchoolPC\client_id.txt)',
	pc_name VARCHAR(100) NOT NULL COMMENT 'Windows machine name at time of login (snapshot)',
	student_id VARCHAR(50) NOT NULL,
	student_name VARCHAR(200) NOT NULL COMMENT 'Student name at time of login (snapshot)',
	login_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
	logout_time TIMESTAMP NULL COMMENT 'NULL if still logged in',
	duration_minutes INT COMMENT 'Calculated from login_time to logout_time',
	FOREIGN KEY (student_id) REFERENCES students(student_id),
	INDEX idx_client_guid (client_guid),
	INDEX idx_student_id (student_id),
	INDEX idx_login_time (login_time)
);

-- Problem reports: Issue tracking
-- Uses client_guid (NOT pc_name) as primary join key
CREATE TABLE IF NOT EXISTS problem_reports (
	report_id INT AUTO_INCREMENT PRIMARY KEY,
	client_guid VARCHAR(36) COMMENT 'Persistent GUID of client PC (from C:\ProgramData\SchoolPC\client_id.txt)',
	pc_name VARCHAR(100) NOT NULL COMMENT 'Windows machine name at time of report (snapshot)',
	student_id VARCHAR(50) NOT NULL,
	student_name VARCHAR(200) NOT NULL COMMENT 'Student name at time of report (snapshot)',
	message TEXT NOT NULL COMMENT 'Problem description',
	status VARCHAR(50) DEFAULT 'open' COMMENT 'open, in_progress, resolved',
	created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
	resolved_at TIMESTAMP NULL COMMENT 'NULL if not resolved',
	resolved_by VARCHAR(100) COMMENT 'Admin username who resolved',
	FOREIGN KEY (student_id) REFERENCES students(student_id),
	INDEX idx_client_guid (client_guid),
	INDEX idx_status (status),
	INDEX idx_created_at (created_at)
);

-- ============================================================================
-- PC INSTANCES: TRUE PC IDENTIFIER (persistent across machine renames)
-- ============================================================================
-- This table is the source of truth for lab PCs.
-- client_guid is immutable; machine_name can change (IT renames the machine).
-- display_name and location are admin-editable for organizational purposes.

CREATE TABLE IF NOT EXISTS pc_instances (
	instance_id INT AUTO_INCREMENT PRIMARY KEY,
	client_guid VARCHAR(36) NOT NULL UNIQUE COMMENT 'Persistent GUID of client PC (from C:\ProgramData\SchoolPC\client_id.txt) - THE TRUE IDENTIFIER',
	first_pc_name VARCHAR(100) NOT NULL COMMENT 'Initial Windows machine name at first registration (read-only, system-managed)',
	current_pc_name VARCHAR(100) NOT NULL COMMENT 'Current Windows machine name (may differ from first_pc_name if PC was renamed; read-only, system-managed)',
	display_name VARCHAR(150) COMMENT 'Admin-editable friendly name shown in dashboard (e.g., "Lab 101 - PC 5")',
	location VARCHAR(150) COMMENT 'Admin-editable location label (e.g., "Building A, Room 101")',
	ip_address VARCHAR(45) COMMENT 'Current IP address of the PC (read-only, updated by hub)',
	is_approved BOOLEAN DEFAULT FALSE COMMENT 'FALSE = pending admin approval (not shown in dashboard), TRUE = approved',
	is_active BOOLEAN DEFAULT TRUE COMMENT 'FALSE = decommissioned (not shown in dashboard), TRUE = active',
	first_seen TIMESTAMP DEFAULT CURRENT_TIMESTAMP COMMENT 'When this PC was first registered (read-only, system-managed)',
	last_seen TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT 'When we last saw activity from this PC (auto-updated)',
	notes TEXT COMMENT 'Admin notes (hardware info, maintenance history, physical location, etc.)',
	INDEX idx_client_guid (client_guid),
	INDEX idx_current_pc_name (current_pc_name),
	INDEX idx_is_approved (is_approved),
	INDEX idx_is_active (is_active)
);

-- ============================================================================
-- SEED DATA (WITH TEMPORARY PASSWORD)
-- ============================================================================
-- TEMPORARY PASSWORD HASH FOR ALL ACCOUNTS:
-- Password: "password" (BCrypt rounds 12)
-- Hash: $2a$12$DQ4skCsan4rSeCh9IY.iE..nJ61CeC1vJuL6I1xl2gH71DfmdoL/a
-- 
-- ⚠️ IMPORTANT: This is a TEMPORARY password for initial setup only.
-- Change passwords for all accounts before production use.
-- 
-- To change password later:
-- 1. Use the C# PasswordHashGenerator utility (Admin project)
-- 2. Or use: https://bcrypt-generator.com/
-- 3. Run UPDATE statement in phpMyAdmin with new hash
--

-- Insert real UIC students with TEMPORARY password
-- All students can login with password: "password"
-- ⚠️ Require password change on first login in production
INSERT INTO students (student_id, username, password_hash, full_name, email, is_active) 
VALUES 
	('230000000813', '230000000813', '$2a$12$DQ4skCsan4rSeCh9IY.iE..nJ61CeC1vJuL6I1xl2gH71DfmdoL/a', 'JO-AN PAULA HONRADA', 'jhonrada_230000000813@uic.edu.ph', TRUE),
	('230000001794', '230000001794', '$2a$12$DQ4skCsan4rSeCh9IY.iE..nJ61CeC1vJuL6I1xl2gH71DfmdoL/a', 'KYZYLL GWEN O. MENDOZA', 'kmendoza_230000001794@uic.edu.ph', TRUE),
	('230000002875', '230000002875', '$2a$12$DQ4skCsan4rSeCh9IY.iE..nJ61CeC1vJuL6I1xl2gH71DfmdoL/a', 'NIKKO ANGELO P. NAVARRO', 'nnavarro_230000002875@uic.edu.ph', TRUE),
	('220000002154', '220000002154', '$2a$12$DQ4skCsan4rSeCh9IY.iE..nJ61CeC1vJuL6I1xl2gH71DfmdoL/a', 'KYLE CLANCY F. TAN', 'ktan_220000002154@uic.edu.ph', TRUE),
	('230000001482', '230000001482', '$2a$12$DQ4skCsan4rSeCh9IY.iE..nJ61CeC1vJuL6I1xl2gH71DfmdoL/a', 'PRINCESS ELLEIN S. UGAY', 'pugay_230000001482@uic.edu.ph', TRUE),
	('240000000531', '240000000531', '$2a$12$DQ4skCsan4rSeCh9IY.iE..nJ61CeC1vJuL6I1xl2gH71DfmdoL/a', 'KURT VINCENT S. LOPENA', 'klopena_240000000531@uic.edu.ph', TRUE),
	('230000000268', '230000000268', '$2a$12$DQ4skCsan4rSeCh9IY.iE..nJ61CeC1vJuL6I1xl2gH71DfmdoL/a', 'KEN JONAS T. BALTAZAR', 'kbaltazar_230000000268@uic.edu.ph', TRUE),
	('230000001888', '230000001888', '$2a$12$DQ4skCsan4rSeCh9IY.iE..nJ61CeC1vJuL6I1xl2gH71DfmdoL/a', 'PAUL ADRIAN CLAVE L. PONCE', 'pponce_230000001888@uic.edu.ph', TRUE);

-- Insert admin accounts with TEMPORARY password
-- All admins can login with password: "password"
-- ⚠️ Change password immediately after first login
INSERT INTO admin_accounts (username, password_hash, full_name, role, is_active) 
VALUES 
	('admin', '$2a$12$DQ4skCsan4rSeCh9IY.iE..nJ61CeC1vJuL6I1xl2gH71DfmdoL/a', 'System Administrator', 'super_admin', TRUE),
	('emergency_admin', '$2a$12$DQ4skCsan4rSeCh9IY.iE..nJ61CeC1vJuL6I1xl2gH71DfmdoL/a', 'Emergency Admin', 'super_admin', TRUE);

-- ============================================================================
-- VERIFICATION QUERIES
-- ============================================================================
-- Run these to verify all tables were created correctly:
-- Copy and paste each query below one at a time in phpMyAdmin

-- Show all tables created:
SHOW TABLES;

-- Show students:
SELECT student_id, username, full_name, email, is_active FROM students;

-- Show admin accounts:
SELECT admin_id, username, full_name, role, is_active FROM admin_accounts;

-- Verify sessions table structure (should show client_guid column):
DESCRIBE sessions;

-- Verify problem_reports table structure (should show client_guid column):
DESCRIBE problem_reports;

-- Verify pc_instances table structure:
DESCRIBE pc_instances;

-- Verify banned_students table structure:
DESCRIBE banned_students;

-- ============================================================================
-- TROUBLESHOOTING
-- ============================================================================
-- 
-- ERROR: "Cannot add or update a child row"
-- SOLUTION: Make sure student_id exists in students table before inserting sessions/reports
--
-- ERROR: "Unknown column 'client_guid'"
-- SOLUTION: The tables must have client_guid column (this script creates them correctly)
--
-- LOGIN NOT WORKING:
-- SOLUTION: Password hashes are still PLACEHOLDER_BCRYPT_HASH - you need to:
--    1. Generate real BCrypt hashes
--    2. Use UPDATE statements to replace them in the database
--    3. Or use the C# PasswordHashGenerator utility
--
-- RESET PASSWORD FOR student 230000002875:
-- =========================================
-- If you have a BCrypt hash for password "password", use:
--   UPDATE students SET password_hash = '$2a$11$YOUR_BCRYPT_HASH_HERE' WHERE student_id = '230000002875';
-- Then flush/refresh the phpMyAdmin page
--

