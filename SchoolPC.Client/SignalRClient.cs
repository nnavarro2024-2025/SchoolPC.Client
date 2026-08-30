using Microsoft.AspNetCore.SignalR.Client;

namespace SchoolPC.Client
{
    public class SignalRClient
    {
        private HubConnection? _connection;
        private readonly string _hubUrl;
        private readonly LockdownManager _lockdownManager;
        private readonly Action<string> _onMessageReceived;
        private readonly Action _onForceLogout;

        public bool IsConnected => _connection?.State == HubConnectionState.Connected;

        public SignalRClient(
            LockdownManager lockdownManager,
            Action<string> onMessageReceived,
            Action onForceLogout,
            string hubUrl = "http://localhost:5000/adminHub")
        {
            _hubUrl = hubUrl;
            _lockdownManager = lockdownManager;
            _onMessageReceived = onMessageReceived;
            _onForceLogout = onForceLogout;
        }

        public async Task<bool> ConnectAsync()
        {
            try
            {
                _connection = new HubConnectionBuilder()
                    .WithUrl(_hubUrl)
                    .WithAutomaticReconnect()
                    .Build();

                // Register handlers for admin commands
                _connection.On<string>("ReceiveMessage", (message) =>
                {
                    _onMessageReceived?.Invoke(message);
                });

                _connection.On("Shutdown", () =>
                {
                    MessageBox.Show("Admin has initiated shutdown.", "System Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LockdownManager.Shutdown(5);
                });

                _connection.On("Restart", () =>
                {
                    MessageBox.Show("Admin has initiated restart.", "System Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LockdownManager.Restart(5);
                });

                _connection.On("Logout", () =>
                {
                    MessageBox.Show("Admin has initiated logout.", "System Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LockdownManager.Logout();
                });

                _connection.On("Lock", () =>
                {
                    MessageBox.Show("Admin has locked this PC.", "System Message", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _onForceLogout?.Invoke();
                });

                _connection.On("ForceLogout", () =>
                {
                    MessageBox.Show("Admin has forced logout.", "System Message", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _onForceLogout?.Invoke();
                });

                _connection.Closed += async (error) =>
                {
                    await Task.Delay(new Random().Next(0, 5) * 1000);
                    await ConnectAsync();
                };

                await _connection.StartAsync();

                // Register this client with the server
                await RegisterClientAsync();

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to connect to admin server: {ex.Message}", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
        }

        public async Task DisconnectAsync()
        {
            if (_connection != null)
            {
                await _connection.StopAsync();
                await _connection.DisposeAsync();
                _connection = null;
            }
        }

        public async Task RegisterClientAsync()
        {
            if (_connection == null || !IsConnected)
                return;

            try
            {
                // RegisterClient now includes both machine name and persistent client GUID
                // This allows the server to uniquely identify this PC even if the machine name changes
                await _connection.InvokeAsync("RegisterClient", Program.ClientGuid, Environment.MachineName);
                Console.WriteLine($"Client registered with ID: {Program.ClientGuid} (Machine: {Environment.MachineName})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to register client: {ex.Message}");
            }
        }

        public async Task SendStatusUpdateAsync(string studentId, string studentName, string status)
        {
            if (_connection == null || !IsConnected)
                return;

            try
            {
                // UpdateClientStatus now includes persistent client GUID for server-side PC identification
                await _connection.InvokeAsync("UpdateClientStatus",
                    Program.ClientGuid,
                    Environment.MachineName,
                    studentId,
                    studentName,
                    status);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send status update: {ex.Message}");
            }
        }

        public async Task SendHeartbeatAsync()
        {
            if (_connection == null || !IsConnected)
                return;

            try
            {
                // Heartbeat now includes persistent client GUID for continuous server-side tracking
                await _connection.InvokeAsync("Heartbeat", Program.ClientGuid, Environment.MachineName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send heartbeat: {ex.Message}");
            }
        }
    }
}
