using Microsoft.AspNetCore.SignalR.Client;
using System.Configuration;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

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

                // Handle reconnection - re-register the PC when connection is restored
                _connection.Reconnected += async (connectionId) =>
                {
                    System.Diagnostics.Debug.WriteLine("SignalR reconnected. Re-registering PC...");
                    await RegisterOrUpdatePcAsync();
                };

                await _connection.StartAsync();

                // Register this PC instance on initial connection
                await RegisterOrUpdatePcAsync();

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

        /// <summary>
        /// Registers or updates this PC instance with the admin hub.
        /// Called on initial connection and on each reconnection to ensure
        /// last_seen timestamp and IP address are current on the server.
        /// </summary>
        public async Task RegisterOrUpdatePcAsync()
        {
            try
            {
                if (IsConnected && _connection != null)
                {
                    string clientGuid = Program.ClientGuid;
                    string machineName = Environment.MachineName;
                    string ipAddress = GetLocalLanIpAddress();

                    await _connection.InvokeAsync("RegisterOrUpdatePc", clientGuid, machineName, ipAddress);
                    System.Diagnostics.Debug.WriteLine($"PC registered/updated: ClientGuid={clientGuid}, Machine={machineName}, IP={ipAddress}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error registering PC: {ex.Message}");
                // Don't throw - this is a best-effort operation that shouldn't crash the client
            }
        }

        /// <summary>
        /// Gets the local LAN IPv4 address by iterating through network interfaces
        /// and selecting the first active, non-loopback, LAN-connected interface.
        /// 
        /// This approach correctly handles multi-NIC machines by:
        /// 1. Filtering to interfaces that are Up and Running
        /// 2. Excluding loopback and virtual adapters
        /// 3. Excluding point-to-point connections (dialup, VPN, etc.)
        /// 4. Returning the first valid IPv4 address found
        /// 
        /// Returns "127.0.0.1" as fallback if no LAN adapter is found, which 
        /// the client recognizes as "no network available" and logs appropriately.
        /// </summary>
        private string GetLocalLanIpAddress()
        {
            try
            {
                NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

                foreach (NetworkInterface ni in interfaces)
                {
                    // Skip down interfaces or loopback
                    if (ni.OperationalStatus != OperationalStatus.Up)
                        continue;

                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                        continue;

                    // Skip virtual adapters (common on VMs and container scenarios)
                    if (ni.Description.Contains("Virtual", StringComparison.OrdinalIgnoreCase) ||
                        ni.Description.Contains("VirtualBox", StringComparison.OrdinalIgnoreCase) ||
                        ni.Name.Contains("vEthernet", StringComparison.OrdinalIgnoreCase))
                        continue;

                    // Skip point-to-point interfaces (dialup, some VPNs)
                    if (ni.IsReceiveOnly)
                        continue;

                    IPInterfaceProperties props = ni.GetIPProperties();

                    foreach (UnicastIPAddressInformation addr in props.UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            string ip = addr.Address.ToString();

                            // Skip loopback range (127.x.x.x)
                            if (ip.StartsWith("127."))
                                continue;

                            // Found a valid LAN IPv4 address
                            System.Diagnostics.Debug.WriteLine($"Selected LAN IP: {ip} from interface {ni.Name} ({ni.Description})");
                            return ip;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting local IP address: {ex.Message}");
            }

            // Fallback: return loopback to indicate "no network available"
            System.Diagnostics.Debug.WriteLine("No LAN IPv4 address found, using fallback 127.0.0.1");
            return "127.0.0.1";
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
