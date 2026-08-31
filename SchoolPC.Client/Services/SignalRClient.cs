using Microsoft.AspNetCore.SignalR.Client;
using System.Configuration;

namespace SchoolPC.Client.Services
{
    /// <summary>
    /// Manages SignalR connection to the admin server.
    /// Handles session notifications, problem reports, and admin messages.
    /// </summary>
    public class SignalRClient
    {
        private HubConnection? _connection;
        private readonly LockdownManager _lockdownManager;
        private readonly Action<string> _onAdminMessageReceived;
        private readonly Action _onForceLogout;

        public bool IsConnected => _connection?.State == HubConnectionState.Connected;

        public SignalRClient(
            LockdownManager lockdownManager,
            Action<string> onAdminMessageReceived,
            Action onForceLogout)
        {
            _lockdownManager = lockdownManager;
            _onAdminMessageReceived = onAdminMessageReceived;
            _onForceLogout = onForceLogout;
        }

        public async Task<bool> ConnectAsync()
        {
            try
            {
                string adminUrl = GetAdminServerUrl();
                _connection = new HubConnectionBuilder()
                    .WithUrl($"{adminUrl}/hubs/monitoring")
                    .WithAutomaticReconnect()
                    .Build();

                // Register handlers
                _connection.On<string>("ReceiveAdminMessage", message => _onAdminMessageReceived(message));
                _connection.On("ForceLogout", () => _onForceLogout());

                await _connection.StartAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SignalR connection error: {ex.Message}");
                return false;
            }
        }

        public async Task DisconnectAsync()
        {
            try
            {
                if (_connection != null)
                {
                    await _connection.StopAsync();
                    await _connection.DisposeAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SignalR disconnect error: {ex.Message}");
            }
        }

        public async Task NotifySessionStartedAsync(string studentId, string studentName)
        {
            try
            {
                if (IsConnected && _connection != null)
                {
                    await _connection.InvokeAsync("NotifySessionStarted", studentId, studentName);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error notifying session started: {ex.Message}");
            }
        }

        public async Task NotifySessionEndedAsync(string studentId)
        {
            try
            {
                if (IsConnected && _connection != null)
                {
                    await _connection.InvokeAsync("NotifySessionEnded", studentId);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error notifying session ended: {ex.Message}");
            }
        }

        public async Task ReportProblemAsync(string studentId, string studentName, string message)
        {
            try
            {
                if (IsConnected && _connection != null)
                {
                    string pcName = Environment.MachineName;
                    await _connection.InvokeAsync("ReportProblem", studentId, studentName, pcName, message);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reporting problem: {ex.Message}");
                throw;
            }
        }

        private string GetAdminServerUrl()
        {
            try
            {
                // Try to read from appsettings.json
                string appsettingsPath = Path.Combine(
                    AppContext.BaseDirectory,
                    "appsettings.json"
                );

                if (File.Exists(appsettingsPath))
                {
                    string jsonContent = File.ReadAllText(appsettingsPath);
                    // Simple JSON parsing for the URL
                    var lines = jsonContent.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        if (line.Contains("\"Url\""))
                        {
                            // Extract URL from line like: "Url": "http://localhost:5000",
                            int startIndex = line.IndexOf("\"http");
                            if (startIndex != -1)
                            {
                                int endIndex = line.IndexOf("\"", startIndex + 1);
                                if (endIndex != -1)
                                {
                                    return line.Substring(startIndex + 1, endIndex - startIndex - 1);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reading admin URL from settings: {ex.Message}");
            }

            // Default fallback
            return "http://localhost:5000";
        }
    }
}
