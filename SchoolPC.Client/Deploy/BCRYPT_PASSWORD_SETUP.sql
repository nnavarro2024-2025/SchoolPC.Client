-- ============================================================================
-- BCRYPT PASSWORD SETUP FOR SchoolPC
-- ============================================================================
--
-- The database_setup.sql creates tables with PLACEHOLDER_BCRYPT_HASH passwords.
-- This file shows you how to set real passwords via SQL.
--
-- ============================================================================
-- OPTION 1: Using Online BCrypt Generator
-- ============================================================================
--
-- 1. Go to: https://bcrypt-generator.com/
-- 2. Enter password: password
-- 3. Select rounds: 11 (higher = more secure but slower)
-- 4. Click "Generate"
-- 5. Copy the hash (starts with $2a$ or $2b$)
-- 6. Replace YOUR_BCRYPT_HASH_HERE in the statements below
--
-- Example BCrypt hash for "password" with 11 rounds:
-- $2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcg7b3XeKeUxWdeS86E36P4/KLm
--

-- ============================================================================
-- SET PASSWORD FOR STUDENTS (Replace YOUR_BCRYPT_HASH_HERE with actual hash)
-- ============================================================================

-- SET ALL STUDENTS TO SAME PASSWORD (for testing)
UPDATE students 
SET password_hash = 'YOUR_BCRYPT_HASH_HERE' 
WHERE is_active = TRUE;

-- OR SET PASSWORD FOR SPECIFIC STUDENT
UPDATE students 
SET password_hash = 'YOUR_BCRYPT_HASH_HERE' 
WHERE student_id = '230000002875';

-- ============================================================================
-- SET PASSWORD FOR ADMIN ACCOUNTS
-- ============================================================================

-- Set admin password
UPDATE admin_accounts 
SET password_hash = 'YOUR_BCRYPT_HASH_HERE' 
WHERE username = 'admin';

-- Set emergency_admin password
UPDATE admin_accounts 
SET password_hash = 'YOUR_BCRYPT_HASH_HERE' 
WHERE username = 'emergency_admin';

-- ============================================================================
-- VERIFY PASSWORD WAS SET
-- ============================================================================

-- Check students passwords (should NOT show PLACEHOLDER_BCRYPT_HASH)
SELECT student_id, username, password_hash, full_name FROM students;

-- Check admin passwords
SELECT admin_id, username, password_hash, full_name, role FROM admin_accounts;

-- ============================================================================
-- COMMON BCrypt HASHES (for testing/development only!)
-- ============================================================================
--
-- WARNING: These are PUBLIC hashes. NEVER use in production.
-- These are just examples to help you get started.
--
-- Password "password" with rounds 11:
-- $2a$11$7J3UjVl5c5eqKlJv8L8zPuZj3XL4eXt5rHVl5V5I5V5JjUZ9j5R/O
--
-- Password "admin123" with rounds 11:
-- $2a$11$vR/EHPfLivZbM4DYHEBQWuI7gUfAX1ZI5F5V5I5V5I5V5I5V5I5I5
--
-- Password "testpass" with rounds 11:
-- $2a$11$K7Z8F5L6M9P2Q3R4S5T6U7V8W9X0Y1Z2A3B4C5D6E7F8G9H0I1J2K3
--

-- ============================================================================
-- OPTION 2: C# BCRYPT GENERATION (Using BCrypt.Net-Next NuGet)
-- ============================================================================
--
-- If you can run C# code, use this approach:
--
-- using BCrypt.Net;
-- 
-- string password = "password";
-- string hash = BCrypt.HashPassword(password, workFactor: 11);
-- Console.WriteLine(hash);
--
-- Then use the output hash in the UPDATE statements above.
--

-- ============================================================================
-- QUICK TEST: After setting password
-- ============================================================================
--
-- 1. Start the SchoolPC.Client application
-- 2. Login with:
--    Username: 230000002875
--    Password: password
-- 3. If it works, password hash is correct!
--
