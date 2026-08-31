# SchoolPC Database Complete Setup Instructions

## ✅ Your Updated Files

I've updated the database setup files to make it easy to drop your current database and recreate it fresh:

### Files in `SchoolPC.Client\Deploy\`:
1. **database_setup.sql** - Main script (includes DROP command to clean slate)
2. **DATABASE_SETUP_GUIDE.md** - Step-by-step usage guide
3. **BCRYPT_PASSWORD_SETUP.sql** - Password configuration helper
4. **database_migration_client_guid.sql** - For if you want to migrate instead of drop

---

## 🚀 Quick Start (5 Minutes)

### Step 1: Open phpMyAdmin
1. Start XAMPP Control Panel
2. Make sure MySQL is running (green checkmark)
3. Click **"Admin"** button next to MySQL
4. Browser opens to phpMyAdmin

### Step 2: Backup Your Data (Optional)
If you have data in schoolpc_db you want to keep, export it first, but this fresh setup will drop everything.

### Step 3: Run the Setup Script
1. In phpMyAdmin, click the **"SQL"** tab at the top
2. Open `SchoolPC.Client\Deploy\database_setup.sql` in your text editor
3. **Copy the entire contents**
4. **Paste into phpMyAdmin SQL editor**
5. Click **"Go"** button
6. Wait for completion (should say "Queries executed successfully")

### Step 4: Verify Database Created
1. Look at left sidebar - you should see **schoolpc_db** folder
2. Click it to expand and see tables:
   - students ✓
   - admin_accounts ✓
   - sessions ✓ (has client_guid column!)
   - problem_reports ✓ (has client_guid column!)
   - pc_instances ✓
   - banned_students ✓

### Step 5: Set Passwords
⚠️ **CRITICAL:** Database created with PLACEHOLDER passwords - need to set real ones!

#### Option A: Quick Test Setup (Use Online Tool)
1. Go to: https://bcrypt-generator.com/
2. Enter password: `password`
3. Rounds: `11`
4. Click "Generate"
5. Copy the BCrypt hash
6. Go to phpMyAdmin SQL tab
7. Run this (replace YOUR_BCRYPT_HASH_HERE):
   ```sql
   USE schoolpc_db;
   UPDATE students SET password_hash = 'YOUR_BCRYPT_HASH_HERE' WHERE student_id = '230000002875';
   UPDATE admin_accounts SET password_hash = 'YOUR_BCRYPT_HASH_HERE' WHERE username = 'admin';
   ```

#### Option B: Generate in C# (More Secure)
1. Open `SchoolPC.Admin` project (if you have it)
2. Use PasswordHashGenerator utility
3. Generate actual BCrypt hashes
4. Run UPDATE statements in phpMyAdmin

#### For Now - Just to Test
Use the script `BCRYPT_PASSWORD_SETUP.sql` as a template.

### Step 6: Test Login
1. Run SchoolPC.Client.exe
2. Try logging in:
   - **Username:** 230000002875 (your student ID)
   - **Password:** password (or whatever you set it to)
3. Should see "Welcome, NIKKO ANGELO P. NAVARRO!"

---

## 🎯 What Changed

### database_setup.sql
✅ Now includes `DROP DATABASE IF EXISTS schoolpc_db;` at start
✅ Clearer instructions on how to run it
✅ Includes all 8 UIC student accounts with correct structure
✅ Tables already have `client_guid` columns (no migration needed!)
✅ Includes verification queries at end
✅ Better comments about BCrypt passwords

### Why the Login Was Broken Before
- Your database was created from old schema without `client_guid` column
- Code tried to insert `client_guid` but column didn't exist
- Database foreign key constraint failed

### This Script Fixes It
- Creates fresh database with ALL correct columns
- Includes `client_guid` in sessions and problem_reports tables
- No migration needed - clean slate

---

## 📋 Your Student Account Details

After running setup and setting password, you can login with:

```
Username: 230000002875
Name: NIKKO ANGELO P. NAVARRO
Email: nnavarro_230000002875@uic.edu.ph
Status: Active (is_active = TRUE)
```

Other test accounts pre-loaded:
- 230000000813 - JO-AN PAULA HONRADA
- 230000001794 - KYZYLL GWEN O. MENDOZA
- 220000002154 - KYLE CLANCY F. TAN
- 230000001482 - PRINCESS ELLEIN S. UGAY
- 240000000531 - KURT VINCENT S. LOPENA
- 230000000268 - KEN JONAS T. BALTAZAR
- 230000001888 - PAUL ADRIAN CLAVE L. PONCE

Admin accounts:
- Username: `admin` (password = needs to be set)
- Username: `emergency_admin` (password = needs to be set)

---

## ⚠️ Important Notes

1. **Password Hashes Are Placeholders**
   - Must be replaced with real BCrypt hashes before login works
   - Use online generator or C# utility

2. **Data Loss**
   - running setup.sql will **DROP** your current schoolpc_db
   - All data will be deleted
   - That's why it starts with `DROP DATABASE IF EXISTS`

3. **client_guid**
   - New tables have this column automatically
   - Generated in C# by `ClientIdentityService.cs`
   - Creates file at `C:\ProgramData\SchoolPC\client_id.txt`

4. **BCrypt Recommended**
   - Uses BCrypt.Net-Next NuGet package
   - Better security than SHA2
   - Takes ~0.1-1 second per login to verify (intentional for security)

---

## 🔧 Troubleshooting

| Problem | Solution |
|---------|----------|
| "Database 'schoolpc_db' already exists" | Ignore - script drops it first |
| "Cannot add or update a child row" | Seed data (INSERT) must run correctly; check student_id exists |
| "Unknown column 'client_guid'" | Should not happen - script creates it |
| "Login fails with auth error" | Password hash is still PLACEHOLDER - set a real one |
| "Cannot start session" | Verify client_guid columns exist: `DESCRIBE sessions;` |
| "SQL syntax error at line X" | Check that you copied the ENTIRE script |

---

## ✨ Next Steps

1. ✅ Run database_setup.sql
2. ✅ Set password for student 230000002875
3. ✅ Test login in SchoolPC.Client
4. ✅ If admin features needed, set admin password too
5. ✅ You're done! 🎉

---

**Questions?** Check the other .sql and .md files in this Deploy folder for more details.
