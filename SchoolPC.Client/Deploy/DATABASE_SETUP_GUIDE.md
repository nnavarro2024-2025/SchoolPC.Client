## SchoolPC Database Setup Guide

### Quick Start (Fresh Database)

**Step 1: Open phpMyAdmin**
- Start XAMPP Control Panel
- Click "Admin" button next to Apache MySQL
- This opens phpMyAdmin in your browser

**Step 2: Open SQL Editor**
- Click on the "SQL" tab at the top
- You should see a large text area for entering SQL

**Step 3: Copy Database Script**
- Open `SchoolPC.Client\Deploy\database_setup.sql`
- Copy the ENTIRE contents
- Paste it into the phpMyAdmin SQL editor

**Step 4: Execute**
- Click the "Go" button to execute
- Wait a few seconds for it to complete
- You should see "Queries executed successfully" message

**Step 5: Verify (Optional)**
- Run the verification queries from the script to confirm tables exist
- Look for these tables in phpMyAdmin left sidebar:
  - students
  - admin_accounts
  - sessions
  - problem_reports
  - pc_instances
  - banned_students

### Database Structure

The database includes these key tables:

```
students              - Student user accounts
├─ student_id (PRIMARY KEY)
├─ username
├─ password_hash (BCrypt)
└─ full_name, email, is_active

sessions             - Login/logout audit trail
├─ session_id (PRIMARY KEY)
├─ client_guid (references PC identity)
├─ student_id (FOREIGN KEY -> students.student_id)
└─ contains: pc_name, login_time, logout_time, duration

problem_reports      - Issue tracking
├─ report_id (PRIMARY KEY)
├─ client_guid (references PC identity)
├─ student_id (FOREIGN KEY -> students.student_id)
└─ contains: message, status, created_at, resolved_at

pc_instances         - Persistent PC tracking
├─ client_guid (PRIMARY KEY, immutable)
├─ first_pc_name, current_pc_name
└─ contains: display_name, location, ip_address, is_approved, is_active
```

### Important: Password Setup

⚠️ **The database is created with PLACEHOLDER_BCRYPT_HASH passwords**

**To make login work, you MUST:**

1. **Option A: Use Online BCrypt Generator**
   - Go to https://bcrypt-generator.com/
   - Enter password "password"
   - Generate BCrypt hash
   - Copy the hash
   - Run this SQL in phpMyAdmin:
	 ```sql
	 UPDATE students SET password_hash = '$2a$11$YOUR_HASH_HERE' WHERE student_id = '230000002875';
	 ```

2. **Option B: Use C# Utility**
   - Use the PasswordHashGenerator tool in the Admin project
   - Generate BCrypt hashes
   - Run UPDATE statements in phpMyAdmin

3. **Test Login**
   - Student ID: `230000002875`
   - Password: `password` (or whatever you set)

### Troubleshooting

**Q: "Already exists" error?**
- A: This script drops the old database first. Make sure you don't need any data from it.

**Q: "Cannot add or update a child row" error?**
- A: The student you're trying to login as doesn't exist in the students table. Run the seed data INSERT statements again.

**Q: "Unknown column 'client_guid'" error?**
- A: Shouldn't happen with this script - it creates client_guid columns in sessions and problem_reports.

**Q: Query runs but no confirmation message?**
- A: Try pasting and running one SELECT statement at a time to debug

**Q: Still can't login?**
- A: Check that password_hash is NOT still "PLACEHOLDER_BCRYPT_HASH"
- A: Run: `SELECT * FROM students;` and verify the password_hash value

### Additional Notes

- Delete/recreate database: Use the script's `DROP DATABASE IF EXISTS` line
- Add new student: Use INSERT INTO students statement
- Ban a student: Insert into banned_students table
- Get login logs: Query sessions table with student_id or date range
- Get issue reports: Query problem_reports table ordered by status
