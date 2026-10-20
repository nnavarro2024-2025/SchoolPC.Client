-- ============================================================================
-- SchoolPC Database (v2): Client writes directly to MySQL, Admin reads/manages
-- ============================================================================
-- WARNING: this DROPS and recreates schoolpc_db (all data is lost).
-- Run it once to migrate. After you have real data, comment out the DROP line
-- and use ALTER statements for future changes instead.
-- ============================================================================

DROP DATABASE IF EXISTS schoolpc_db;
CREATE DATABASE schoolpc_db;
USE schoolpc_db;

-- Students (students log in on the kiosk against this table)
CREATE TABLE students (
	student_id VARCHAR(50) PRIMARY KEY,
	username VARCHAR(100) NOT NULL UNIQUE,
	password_hash VARCHAR(255) NOT NULL COMMENT 'BCrypt hash',
	full_name VARCHAR(200) NOT NULL,
	email VARCHAR(150),
	is_active BOOLEAN DEFAULT TRUE COMMENT 'FALSE = account disabled (bans are in banned_students)',
	created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Admin accounts (log in to SchoolPC.Admin)
CREATE TABLE admin_accounts (
	admin_id INT AUTO_INCREMENT PRIMARY KEY,
	username VARCHAR(100) NOT NULL UNIQUE,
	password_hash VARCHAR(255) NOT NULL COMMENT 'BCrypt hash',
	full_name VARCHAR(200) NOT NULL,
	role VARCHAR(50) DEFAULT 'admin' COMMENT 'super_admin | admin',
	is_active BOOLEAN DEFAULT TRUE,
	created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Bans (unban sets is_active = FALSE; the record stays for history)
CREATE TABLE banned_students (
	ban_id INT AUTO_INCREMENT PRIMARY KEY,
	student_id VARCHAR(50) NOT NULL,
	reason TEXT,
	banned_by VARCHAR(100) COMMENT 'Admin username',
	banned_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
	is_active BOOLEAN DEFAULT TRUE COMMENT 'TRUE = currently banned',
	FOREIGN KEY (student_id) REFERENCES students(student_id),
	INDEX idx_student_active (student_id, is_active)
);

-- Lab PCs. client_guid is the permanent identity (survives Windows renames).
-- Written by SchoolPC.Client only (registration + 60-second heartbeat).
CREATE TABLE pc_instances (
	instance_id INT AUTO_INCREMENT PRIMARY KEY,
	client_guid VARCHAR(36) NOT NULL UNIQUE COMMENT 'From C:\ProgramData\SchoolPC\client_id.txt',
	first_pc_name VARCHAR(100) NOT NULL COMMENT 'Windows name at first registration',
	current_pc_name VARCHAR(100) NOT NULL COMMENT 'Windows name now (updates if renamed)',
	display_name VARCHAR(150) COMMENT 'Friendly name; defaults to the machine name',
	ip_address VARCHAR(45),
	is_active BOOLEAN DEFAULT TRUE COMMENT 'FALSE = decommissioned PC',
	first_seen TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
	last_seen TIMESTAMP DEFAULT CURRENT_TIMESTAMP COMMENT 'Set explicitly by the Client heartbeat only'
);

-- Sessions = the logbook
CREATE TABLE sessions (
	session_id INT AUTO_INCREMENT PRIMARY KEY,
	client_guid VARCHAR(36) NOT NULL COMMENT 'Which PC',
	pc_name VARCHAR(100) NOT NULL COMMENT 'Windows name at login (snapshot)',
	student_id VARCHAR(50) NOT NULL,
	student_name VARCHAR(200) NOT NULL COMMENT 'Name at login (snapshot)',
	login_time TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
	logout_time TIMESTAMP NULL COMMENT 'NULL = still logged in',
	duration_minutes INT,
	end_reason VARCHAR(60) NULL COMMENT 'logout | windows shutdown | PC turned off unexpectedly',
	recovered_at TIMESTAMP NULL COMMENT 'When the PC booted again and the cut-off session was closed',
	FOREIGN KEY (student_id) REFERENCES students(student_id),
	INDEX idx_pc_time (client_guid, login_time),
	INDEX idx_student (student_id)
);

-- Problem reports sent from the kiosk
CREATE TABLE problem_reports (
	report_id INT AUTO_INCREMENT PRIMARY KEY,
	client_guid VARCHAR(36) NOT NULL,
	pc_name VARCHAR(100) NOT NULL,
	student_id VARCHAR(50) NOT NULL,
	student_name VARCHAR(200) NOT NULL,
	message TEXT NOT NULL,
	status VARCHAR(50) DEFAULT 'open' COMMENT 'open | resolved',
	created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
	resolved_at TIMESTAMP NULL,
	resolved_by VARCHAR(100),
	FOREIGN KEY (student_id) REFERENCES students(student_id),
	INDEX idx_status_created (status, created_at)
);

-- ============================================================================
-- SEED DATA. Every seeded account's password is: password
-- ============================================================================
INSERT INTO students (student_id, username, password_hash, full_name, email) VALUES
	('230000000813', '230000000813', '$2a$12$DQ4skCsan4rSeCh9IY.iE..nJ61CeC1vJuL6I1xl2gH71DfmdoL/a', 'JO-AN PAULA HONRADA', 'jhonrada_230000000813@uic.edu.ph'),
	('230000001794', '230000001794', '$2a$12$DQ4skCsan4rSeCh9IY.iE..nJ61CeC1vJuL6I1xl2gH71DfmdoL/a', 'KYZYLL GWEN O. MENDOZA', 'kmendoza_230000001794@uic.edu.ph'),
	('230000002875', '230000002875', '$2a$12$DQ4skCsan4rSeCh9IY.iE..nJ61CeC1vJuL6I1xl2gH71DfmdoL/a', 'NIKKO ANGELO P. NAVARRO', 'nnavarro_230000002875@uic.edu.ph'),
	('220000002154', '220000002154', '$2a$12$DQ4skCsan4rSeCh9IY.iE..nJ61CeC1vJuL6I1xl2gH71DfmdoL/a', 'KYLE CLANCY F. TAN', 'ktan_220000002154@uic.edu.ph'),
	('230000001482', '230000001482', '$2a$12$DQ4skCsan4rSeCh9IY.iE..nJ61CeC1vJuL6I1xl2gH71DfmdoL/a', 'PRINCESS ELLEIN S. UGAY', 'pugay_230000001482@uic.edu.ph'),
	('240000000531', '240000000531', '$2a$12$DQ4skCsan4rSeCh9IY.iE..nJ61CeC1vJuL6I1xl2gH71DfmdoL/a', 'KURT VINCENT S. LOPENA', 'klopena_240000000531@uic.edu.ph'),
	('230000000268', '230000000268', '$2a$12$DQ4skCsan4rSeCh9IY.iE..nJ61CeC1vJuL6I1xl2gH71DfmdoL/a', 'KEN JONAS T. BALTAZAR', 'kbaltazar_230000000268@uic.edu.ph'),
	('230000001888', '230000001888', '$2a$12$DQ4skCsan4rSeCh9IY.iE..nJ61CeC1vJuL6I1xl2gH71DfmdoL/a', 'PAUL ADRIAN CLAVE L. PONCE', 'pponce_230000001888@uic.edu.ph');

INSERT INTO admin_accounts (username, password_hash, full_name, role) VALUES
	('admin', '$2a$12$DQ4skCsan4rSeCh9IY.iE..nJ61CeC1vJuL6I1xl2gH71DfmdoL/a', 'System Administrator', 'super_admin'),
	('emergency_admin', '$2a$12$DQ4skCsan4rSeCh9IY.iE..nJ61CeC1vJuL6I1xl2gH71DfmdoL/a', 'Emergency Admin', 'super_admin');

SHOW TABLES;


USE schoolpc_db;

CREATE TABLE pc_groups (
    group_id   INT AUTO_INCREMENT PRIMARY KEY,
    group_name VARCHAR(100) NOT NULL UNIQUE,
    sort_order INT NOT NULL DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

ALTER TABLE pc_instances
    ADD COLUMN group_id   INT NULL,
    ADD COLUMN sort_order INT NOT NULL DEFAULT 0,
    ADD CONSTRAINT fk_pc_group FOREIGN KEY (group_id)
        REFERENCES pc_groups(group_id) ON DELETE SET NULL;