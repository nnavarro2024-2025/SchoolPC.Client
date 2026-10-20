using MySqlConnector;
using System.Data;
using Microsoft.Extensions.Configuration;

namespace SchoolPC.Client
{
    public class DatabaseService
    {
        private readonly string _connectionString;
        private static string? _configError = null;

        public static string? GetConfigError() => _configError;

        public DatabaseService()
        {
            // Try to load from appsettings.json first
            string appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

            string server = "localhost";
            int port = 3306;
            string database = "schoolpc_db";
            string user = "root";
            string password = "";

            if (File.Exists(appSettingsPath))
            {
                try
                {
                    var config = new ConfigurationBuilder()
                        .AddJsonFile(appSettingsPath, optional: false, reloadOnChange: false)
                        .Build();

                    var dbSection = config.GetSection("Database");
                    server = dbSection["Server"] ?? "localhost";
                    if (int.TryParse(dbSection["Port"], out int parsedPort))
                    {
                        port = parsedPort;
                    }
                    database = dbSection["Database"] ?? "schoolpc_db";
                    user = dbSection["User"] ?? "root";
                    password = dbSection["Password"] ?? "";

                    Console.WriteLine($"[DatabaseService] Loaded config from {appSettingsPath}");
                }
                catch (Exception ex)
                {
                    _configError = $"Failed to read appsettings.json: {ex.Message}";
                    Console.WriteLine($"[DatabaseService] WARNING: {_configError}. Using defaults.");
                }
            }
            else
            {
                _configError = $"appsettings.json not found at {appSettingsPath}. Using default localhost/root configuration.";
                Console.WriteLine($"[DatabaseService] WARNING: {_configError}");
            }

            _connectionString = $"Server={server};Database={database};User ID={user};Password={password};Port={port};Connection Timeout=5;";
        }

        public DatabaseService(string server = "localhost", string database = "schoolpc_db", string user = "root", string password = "", int port = 3306)
        {
            _connectionString = $"Server={server};Database={database};User ID={user};Password={password};Port={port};Connection Timeout=5;";
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
            // Skip session insertion for BYPASS_ADMIN (doesn't exist in students table)
            if (studentId == "BYPASS_ADMIN")
            {
                Console.WriteLine("[DatabaseService] Skipping session insert for BYPASS_ADMIN");
                return -1; // -1 indicates no session was created
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sql = @"
                    INSERT INTO sessions (client_guid, pc_name, student_id, student_name, login_time, end_reason) 
                    VALUES (@clientGuid, @pcName, @studentId, @studentName, NOW(), NULL)";

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

        public async Task<bool> EndSessionAsync(int sessionId, string endReason = "logout", int commandTimeoutSeconds = 30)
        {
            // Skip if no session was created (e.g., BYPASS_ADMIN)
            if (sessionId <= 0)
            {
                return true; // Not an error; just no session to end
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sql = @"
                    UPDATE sessions 
                    SET logout_time = NOW(), 
                        duration_minutes = TIMESTAMPDIFF(MINUTE, login_time, NOW()),
                        end_reason = @endReason
                    WHERE session_id = @sessionId";

                using var cmd = new MySqlCommand(sql, connection);
                cmd.CommandTimeout = commandTimeoutSeconds;
                cmd.Parameters.AddWithValue("@sessionId", sessionId);
                cmd.Parameters.AddWithValue("@endReason", endReason);

                await cmd.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to end session: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Synchronously ends a session with very short timeouts.
        /// Used by Windows shutdown handler to ensure the operation completes before the app is forcibly killed.
        /// NO async/await, NO Task.Wait to avoid deadlocks with SynchronizationContext.
        /// All I/O is truly synchronous: Open(), ExecuteNonQuery().
        /// Returns immediately if sessionId is invalid (no session to end).
        /// </summary>
        public bool EndSessionBlocking(int sessionId, string endReason = "logout")
        {
            // Skip if no session was created (e.g., BYPASS_ADMIN)
            if (sessionId <= 0)
            {
                return true; // Not an error; just no session to end
            }

            try
            {
                // Create a connection string with 2-second timeout for shutdown handler
                // MySqlConnection.ConnectionTimeout is read-only, so we must pass it in the connection string
                var shutdownConnectionString = _connectionString.Contains("Connection Timeout")
                    ? System.Text.RegularExpressions.Regex.Replace(_connectionString, @"Connection Timeout=\d+", "Connection Timeout=2")
                    : _connectionString + ";Connection Timeout=2";

                using var connection = new MySqlConnection(shutdownConnectionString);
                connection.Open();

                const string sql = @"
                    UPDATE sessions 
                    SET logout_time = NOW(), 
                        duration_minutes = TIMESTAMPDIFF(MINUTE, login_time, NOW()),
                        end_reason = @endReason
                    WHERE session_id = @sessionId";

                using var cmd = new MySqlCommand(sql, connection);
                // Set 2-second command timeout for shutdown handler
                cmd.CommandTimeout = 2;
                cmd.Parameters.AddWithValue("@sessionId", sessionId);
                cmd.Parameters.AddWithValue("@endReason", endReason);

                cmd.ExecuteNonQuery();
                System.Diagnostics.Debug.WriteLine($"[DatabaseService.EndSessionBlocking] Session {sessionId} closed with reason '{endReason}'");
                return true;
            }
            catch (Exception ex)
            {
                // Silent failure in shutdown handler (don't show MessageBox, might hang)
                System.Diagnostics.Debug.WriteLine($"[DatabaseService.EndSessionBlocking] Error: {ex.Message}");
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

        /// <summary>
        /// Reads the database server's current time (NOW()).
        /// Used to establish a cutoff point for crash recovery.
        /// Returns DateTime.MinValue if the read fails.
        /// </summary>
        public async Task<DateTime> GetDatabaseTimeAsync()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sql = "SELECT NOW()";
                using var cmd = new MySqlCommand(sql, connection);
                var result = await cmd.ExecuteScalarAsync();
                if (result is DateTime dbTime)
                {
                    return dbTime;
                }
                return DateTime.MinValue;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DatabaseService] Failed to read database time: {ex.Message}");
                return DateTime.MinValue;
            }
        }

        /// <summary>
        /// Recovers from crashes/power loss by closing any open sessions for this client
        /// that were created BEFORE the application started.
        /// Orphaned sessions are detected by comparing login_time < appStartDbTime.
        /// This prevents closing sessions if a student logs in after the app starts but
        /// before recovery completes.
        /// Run this on app startup BEFORE PC registration and heartbeat.
        /// </summary>
        public async Task<bool> CrashRecoveryAsync(string clientGuid, DateTime appStartDbTime)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sql = @"
                    UPDATE sessions s
                    JOIN pc_instances p ON p.client_guid = s.client_guid
                    SET s.logout_time = GREATEST(s.login_time, p.last_seen),
                        s.duration_minutes = TIMESTAMPDIFF(MINUTE, s.login_time, 
                                            GREATEST(s.login_time, p.last_seen)),
                        s.end_reason = 'PC turned off unexpectedly',
                        s.recovered_at = NOW()
                    WHERE s.client_guid = @guid 
                      AND s.logout_time IS NULL
                      AND s.login_time < @appStart";

                using var cmd = new MySqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@guid", clientGuid);
                cmd.Parameters.AddWithValue("@appStart", appStartDbTime);

                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                if (rowsAffected > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[DatabaseService] Crash recovery: closed {rowsAffected} orphaned session(s)");
                }
                return true;
            }
            catch (Exception ex)
            {
                // Silently skip if database is unreachable or pc_instances doesn't exist yet
                System.Diagnostics.Debug.WriteLine($"[DatabaseService] Crash recovery skipped: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Registers or updates this PC in the pc_instances table.
        /// On first run: creates a new row with first_pc_name, current_pc_name, display_name, last_seen.
        /// On subsequent runs: updates current_pc_name, ip_address, last_seen=NOW(), but preserves display_name.
        /// </summary>
        public async Task<bool> RegisterOrUpdatePcAsync(string clientGuid, string machineName, string? ipAddress)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                // Use INSERT ... ON DUPLICATE KEY UPDATE to handle both cases
                const string sql = @"
                    INSERT INTO pc_instances 
                      (client_guid, first_pc_name, current_pc_name, display_name, ip_address, last_seen) 
                    VALUES 
                      (@clientGuid, @machineName, @machineName, @machineName, @ipAddress, NOW())
                    ON DUPLICATE KEY UPDATE 
                      current_pc_name = @machineName,
                      ip_address = @ipAddress,
                      last_seen = NOW()";

                using var cmd = new MySqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@clientGuid", clientGuid);
                cmd.Parameters.AddWithValue("@machineName", machineName);
                cmd.Parameters.AddWithValue("@ipAddress", ipAddress == null ? DBNull.Value : (object)ipAddress);

                await cmd.ExecuteNonQueryAsync();
                Console.WriteLine($"[DatabaseService] PC registered/updated: {clientGuid} ({machineName})");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DatabaseService] PC registration failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Updates the heartbeat (last_seen and ip_address) for this PC.
        /// Call this periodically (e.g., every 60 seconds) to keep the PC alive.
        /// Failures are silent.
        /// </summary>
        public async Task<bool> UpdatePcHeartbeatAsync(string clientGuid, string? ipAddress)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                const string sql = @"
                    UPDATE pc_instances 
                    SET last_seen = NOW(),
                        ip_address = @ipAddress
                    WHERE client_guid = @clientGuid";

                using var cmd = new MySqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@clientGuid", clientGuid);
                cmd.Parameters.AddWithValue("@ipAddress", ipAddress == null ? DBNull.Value : (object)ipAddress);

                await cmd.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                // Silent failure - heartbeat missed is not critical
                return false;
            }
        }
    }
}
