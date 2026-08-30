-- SchoolPC Database Setup Script (WITH PERSISTENT CLIENT GUID SUPPORT)
-- Run this in MySQL terminal or phpMyAdmin SQL tab

CREATE DATABASE IF NOT EXISTS schoolpc_db;
USE schoolpc_db;

-- Students table
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

-- Session logs (WITH persistent client_guid for PC identification across machine renames)
CREATE TABLE IF NOT EXISTS sessions (
	session_id INT AUTO_INCREMENT PRIMARY KEY,
	client_guid VARCHAR(36) COMMENT 'Persistent GUID of client PC (from C:\ProgramData\SchoolPC\client_id.txt)',
	pc_name VARCHAR(100) NOT NULL,
	student_id VARCHAR(50) NOT NULL,
	student_name VARCHAR(200) NOT NULL,
	login_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
	logout_time TIMESTAMP NULL,
	duration_minutes INT,
	FOREIGN KEY (student_id) REFERENCES students(student_id),
	INDEX idx_client_guid (client_guid),
	INDEX idx_login_time (login_time)
);

-- Problem reports (WITH persistent client_guid for PC identification across machine renames)
CREATE TABLE IF NOT EXISTS problem_reports (
	report_id INT AUTO_INCREMENT PRIMARY KEY,
	client_guid VARCHAR(36) COMMENT 'Persistent GUID of client PC (from C:\ProgramData\SchoolPC\client_id.txt)',
	pc_name VARCHAR(100) NOT NULL,
	student_id VARCHAR(50) NOT NULL,
	student_name VARCHAR(200) NOT NULL,
	message TEXT NOT NULL,
	status VARCHAR(50) DEFAULT 'open',
	created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
	resolved_at TIMESTAMP NULL,
	resolved_by VARCHAR(100),
	INDEX idx_client_guid (client_guid),
	INDEX idx_status (status),
	INDEX idx_created_at (created_at)
);

-- PC Instances: Track which client_guid belongs to which PC (allows audit trail if PC is renamed)
CREATE TABLE IF NOT EXISTS pc_instances (
	instance_id INT AUTO_INCREMENT PRIMARY KEY,
	client_guid VARCHAR(36) NOT NULL UNIQUE COMMENT 'Persistent GUID of client PC',
	first_pc_name VARCHAR(100) NOT NULL COMMENT 'Initial Windows machine name at first registration',
	current_pc_name VARCHAR(100) NOT NULL COMMENT 'Current Windows machine name (may differ if PC was renamed)',
	first_seen TIMESTAMP DEFAULT CURRENT_TIMESTAMP COMMENT 'When this client was first registered',
	last_seen TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT 'When we last saw activity from this client',
	notes TEXT COMMENT 'Admin notes (e.g., Lab location, physical description, etc.)',
	INDEX idx_client_guid (client_guid),
	INDEX idx_current_pc_name (current_pc_name)
);

-- ============================================================================
-- Insert real UIC students
-- ============================================================================
INSERT INTO students (student_id, username, password_hash, full_name, email) 
VALUES 
	('230000000813', '230000000813', SHA2('password', 256), 'JO-AN PAULA HONRADA', 'jhonrada_230000000813@uic.edu.ph'),
	('230000001794', '230000001794', SHA2('password', 256), 'KYZYLL GWEN O. MENDOZA', 'kmendoza_230000001794@uic.edu.ph'),
	('230000002875', '230000002875', SHA2('password', 256), 'NIKKO ANGELO P. NAVARRO', 'nnavarro_230000002875@uic.edu.ph'),
	('220000002154', '220000002154', SHA2('password', 256), 'KYLE CLANCY F. TAN', 'ktan_220000002154@uic.edu.ph'),
	('230000001482', '230000001482', SHA2('password', 256), 'PRINCESS ELLEIN S. UGAY', 'pugay_230000001482@uic.edu.ph'),
	('240000000531', '240000000531', SHA2('password', 256), 'KURT VINCENT S. LOPENA', 'klopena_240000000531@uic.edu.ph'),
	('230000000268', '230000000268', SHA2('password', 256), 'KEN JONAS T. BALTAZAR', 'kbaltazar_230000000268@uic.edu.ph'),
	('230000001888', '230000001888', SHA2('password', 256), 'PAUL ADRIAN CLAVE L. PONCE', 'pponce_230000001888@uic.edu.ph');

-- Insert admin accounts
INSERT INTO admin_accounts (username, password_hash, full_name, role) 
VALUES 
	('admin', SHA2('admin123', 256), 'System Administrator', 'super_admin'),
	('emergency_admin', SHA2('emergency2024', 256), 'Emergency Admin', 'super_admin');

-- ============================================================================
-- Verify tables created
-- ============================================================================
SHOW TABLES; 

-- Verify students
SELECT student_id, username, full_name, email FROM students;

-- Verify sessions table structure (check client_guid column exists)
DESCRIBE sessions;

-- Verify problem_reports table structure (check client_guid column exists)
DESCRIBE problem_reports;
