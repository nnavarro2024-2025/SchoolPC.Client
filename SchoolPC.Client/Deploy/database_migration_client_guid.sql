-- ============================================================================
-- SchoolPC Database Migration Script
-- Add client_guid columns to existing sessions and problem_reports tables
-- ============================================================================
-- 
-- This script adds the missing client_guid column to sessions and problem_reports
-- tables if they were created with an older version of the schema.
--
-- Run this script in phpMyAdmin (XAMPP) if you encounter foreign key errors when logging in.
--

USE schoolpc_db;

-- Check if client_guid column exists in sessions table
-- If it doesn't exist, add it
ALTER TABLE sessions ADD COLUMN client_guid VARCHAR(36) FIRST;

-- Add index for faster queries
ALTER TABLE sessions ADD INDEX idx_client_guid (client_guid);

-- Check if client_guid column exists in problem_reports table
-- If it doesn't exist, add it
ALTER TABLE problem_reports ADD COLUMN client_guid VARCHAR(36) FIRST;

-- Add index for faster queries
ALTER TABLE problem_reports ADD INDEX idx_client_guid (client_guid);

-- ============================================================================
-- Verify the migration
-- ============================================================================

-- Verify sessions table structure
DESCRIBE sessions;

-- Verify problem_reports table structure
DESCRIBE problem_reports;

-- Check row counts
SELECT 'sessions' as table_name, COUNT(*) as row_count FROM sessions
UNION ALL
SELECT 'problem_reports', COUNT(*) FROM problem_reports;
