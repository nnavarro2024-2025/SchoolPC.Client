using MySqlConnector;
using System.Data;

namespace SchoolPC.Client
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService(string server = "localhost", string database = "schoolpc_db", string user = "root", string password = "")
        {
            _connectionString = $"Server={server};Database={database};User ID={user};Password={password};Port=3306;";
        }

        #region Database Schema SQL (For XAMPP MySQL Setup)
        /*
        -- Run this SQL in phpMyAdmin (XAMPP) to create the database:

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

        -- Insert sample test data (optional)
        INSERT INTO students (student_id, username, password_hash, full_name, email) 
        VALUES 
            ('2024-001', 'jdoe', SHA2('password123', 256), 'John Doe', 'jdoe@uic.edu.ph'),
            ('2024-002', 'msmith', SHA2('password123', 256), 'Mary Smith', 'msmith@uic.edu.ph');

        INSERT INTO admin_accounts (username, password_hash, full_name, role) 
        VALUES 
            ('admin', SHA2('admin123', 256), 'System Administrator', 'super_admin');
        */
        #endregion

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                return connection.State == ConnectionState.Open;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Database connection failed: {ex.Message}", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public async Task<(bool success, string? studentId, string? fullName)> AuthenticateStudentAsync(string username, string password)
        {
            // EMERGENCY ADMIN BYPASS - Works even when database is down
            // This allows admins to unlock the PC if database fails
            if (username == "bypass_admin" && password == "bypass#superadmin")
            {
                return (true, "BYPASS_ADMIN", "System Administrator (Bypass)");
            }

            try
            {
                string passwordHash = HashPassword(password);

                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                // Check if student is banned first
                const string banCheckSql = @"
                    SELECT COUNT(*) 
                    FROM banned_students bs 
                    INNER JOIN students s ON bs.student_id = s.student_id 
                    WHERE s.username = @username";

                using (var banCmd = new MySqlCommand(banCheckSql, connection))
                {
                    banCmd.Parameters.AddWithValue("@username", username);
                    var banCount = Convert.ToInt32(await banCmd.ExecuteScalarAsync());
                    if (banCount > 0)
                    {
                        return (false, null, null);
                    }
                }

                // Authenticate student
                const string authSql = @"
                    SELECT student_id, full_name 
                    FROM students 
                    WHERE username = @username 
                    AND password_hash = @passwordHash 
                    AND is_active = TRUE";

                using (var authCmd = new MySqlCommand(authSql, connection))
                {
                    authCmd.Parameters.AddWithValue("@username", username);
                    authCmd.Parameters.AddWithValue("@passwordHash", passwordHash);

                    using var reader = await authCmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        string studentId = reader.GetString("student_id");
                        string fullName = reader.GetString("full_name");
                        return (true, studentId, fullName);
                    }
                }

                return (false, null, null);
            }
            catch (Exception ex)
            {
                // If database is completely down, still allow emergency admin
                if (username == "bypass_admin" && password == "bypass#superadmin")
                {
                    return (true, "BYPASS_ADMIN", "System Administrator (Bypass)");
                }

                // Don't reveal bypass credentials in error message (security)
                MessageBox.Show($"Authentication error: {ex.Message}\n\nPlease contact IT support if the problem persists.", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return (false, null, null);
            }
        }

        public async Task<int> StartSessionAsync(string pcName, string studentId, string studentName)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sql = @"
                    INSERT INTO sessions (pc_name, student_id, student_name, login_time) 
                    VALUES (@pcName, @studentId, @studentName, NOW())";

                using var cmd = new MySqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@pcName", pcName);
                cmd.Parameters.AddWithValue("@studentId", studentId);
                cmd.Parameters.AddWithValue("@studentName", studentName);

                await cmd.ExecuteNonQueryAsync();
                return (int)cmd.LastInsertedId;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to start session: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return -1;
            }
        }

        public async Task<bool> EndSessionAsync(int sessionId)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sql = @"
                    UPDATE sessions 
                    SET logout_time = NOW(), 
                        duration_minutes = TIMESTAMPDIFF(MINUTE, login_time, NOW()) 
                    WHERE session_id = @sessionId";

                using var cmd = new MySqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@sessionId", sessionId);

                await cmd.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to end session: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public async Task<bool> SubmitProblemReportAsync(string pcName, string studentId, string studentName, string message)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sql = @"
                    INSERT INTO problem_reports (pc_name, student_id, student_name, message, status, created_at) 
                    VALUES (@pcName, @studentId, @studentName, @message, 'open', NOW())";

                using var cmd = new MySqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@pcName", pcName);
                cmd.Parameters.AddWithValue("@studentId", studentId);
                cmd.Parameters.AddWithValue("@studentName", studentName);
                cmd.Parameters.AddWithValue("@message", message);

                await cmd.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to submit report: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private string HashPassword(string password)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(password);
            byte[] hash = sha256.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}
