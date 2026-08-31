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

        #region Database Schema SQL (Use Deploy/database_setup.sql instead)
        /*
        IMPORTANT: This code class uses client_guid to track persistent PC identity.

        The database schema is defined in: SchoolPC.Client/Deploy/database_setup.sql
        Run that file in phpMyAdmin to create the database with the correct schema:

        Key tables:
        - students: Student accounts (username = student ID, password_hash = BCrypt)
        - sessions: Login/logout audit trail (uses client_guid as primary key)
        - problem_reports: Issue tracking (uses client_guid as primary key)
        - pc_instances: Persistent PC tracking (client_guid survives machine renames)

        CRITICAL: sessions and problem_reports tables MUST have 'client_guid VARCHAR(36)' column.
        If your database doesn't have this column, run:

        ALTER TABLE sessions ADD COLUMN client_guid VARCHAR(36) FIRST;
        ALTER TABLE problem_reports ADD COLUMN client_guid VARCHAR(36) FIRST;

        After adding client_guid, verify with:
        DESCRIBE sessions;
        DESCRIBE problem_reports;
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
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                // Check if student is banned first
                const string banCheckSql = @"
                    SELECT COUNT(*) 
                    FROM banned_students bs 
                    INNER JOIN students s ON bs.student_id = s.student_id 
                    WHERE s.username = @username AND bs.is_active = TRUE";

                using (var banCmd = new MySqlCommand(banCheckSql, connection))
                {
                    banCmd.Parameters.AddWithValue("@username", username);
                    var banCount = Convert.ToInt32(await banCmd.ExecuteScalarAsync());
                    if (banCount > 0)
                    {
                        return (false, null, null);
                    }
                }

                // Retrieve student record to verify password with BCrypt
                const string authSql = @"
                    SELECT student_id, full_name, password_hash 
                    FROM students 
                    WHERE username = @username 
                    AND is_active = TRUE";

                using (var authCmd = new MySqlCommand(authSql, connection))
                {
                    authCmd.Parameters.AddWithValue("@username", username);

                    using var reader = await authCmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        string studentId = reader.GetString("student_id");
                        string fullName = reader.GetString("full_name");
                        string passwordHash = reader.GetString("password_hash");

                        // Verify password using BCrypt
                        if (BCrypt.Net.BCrypt.Verify(password, passwordHash))
                        {
                            return (true, studentId, fullName);
                        }
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

        public async Task<int> StartSessionAsync(string clientGuid, string pcName, string studentId, string studentName)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sql = @"
                    INSERT INTO sessions (client_guid, pc_name, student_id, student_name, login_time) 
                    VALUES (@clientGuid, @pcName, @studentId, @studentName, NOW())";

                using var cmd = new MySqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@clientGuid", clientGuid);
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

        public async Task<bool> SubmitProblemReportAsync(string clientGuid, string pcName, string studentId, string studentName, string message)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sql = @"
                    INSERT INTO problem_reports (client_guid, pc_name, student_id, student_name, message, status, created_at) 
                    VALUES (@clientGuid, @pcName, @studentId, @studentName, @message, 'open', NOW())";

                using var cmd = new MySqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@clientGuid", clientGuid);
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
    }
}
