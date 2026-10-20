using System.Net.NetworkInformation;

namespace SchoolPC.Client
{
    public static class NetworkUtility
    {
        /// <summary>
        /// Gets the IPv4 address of the primary LAN adapter.
        /// Selects adapters that:
        /// - Are Up and operational
        /// - Are NOT loopback or tunnel adapters
        /// - HAVE a default gateway
        /// - Are NOT named/described with: ZeroTier, VirtualBox, VMware, Hyper-V, vEthernet, Bluetooth, VPN
        /// Falls back to the first remaining Up IPv4 adapter if no "good" adapter is found.
        /// Returns NULL if no suitable adapter is found.
        /// </summary>
        public static string? GetLocalLanIpAddress()
        {
            try
            {
                var interfaces = NetworkInterface.GetAllNetworkInterfaces();

                // First pass: find adapters with default gateway, excluding virtual/tunnel/special ones
                var excludePatterns = new[] { "ZeroTier", "VirtualBox", "VMware", "Hyper-V", "vEthernet", "Bluetooth", "VPN" };

                string? preferredIp = null;
                string? fallbackIp = null;

                foreach (var iface in interfaces)
                {
                    // Skip if not operational
                    if (iface.OperationalStatus != OperationalStatus.Up)
                        continue;

                    // Skip loopback and tunnel adapters
                    if (iface.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                        iface.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
                        continue;

                    // Get IPv4 addresses
                    var ipProps = iface.GetIPProperties();
                    var ipv4Addresses = ipProps.UnicastAddresses
                        .Where(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        .ToList();

                    if (!ipv4Addresses.Any())
                        continue;

                    string ipaddr = ipv4Addresses.First().Address.ToString();

                    // Check if name/description contains excluded patterns
                    bool isExcluded = excludePatterns.Any(pattern =>
                        iface.Name.Contains(pattern, StringComparison.OrdinalIgnoreCase) ||
                        iface.Description.Contains(pattern, StringComparison.OrdinalIgnoreCase));

                    if (isExcluded)
                        continue;

                    // Check if this adapter has a default gateway
                    bool hasGateway = ipProps.GatewayAddresses.Any();

                    if (hasGateway && preferredIp == null)
                    {
                        preferredIp = ipaddr;
                        Console.WriteLine($"[NetworkUtility] Selected adapter: {iface.Name} ({iface.Description}) -> {ipaddr}");
                        break;
                    }

                    // Save fallback if no preferred found yet
                    if (fallbackIp == null)
                    {
                        fallbackIp = ipaddr;
                    }
                }

                if (preferredIp != null)
                    return preferredIp;

                if (fallbackIp != null)
                {
                    Console.WriteLine($"[NetworkUtility] No adapter with gateway found; using fallback IP: {fallbackIp}");
                    return fallbackIp;
                }

                Console.WriteLine("[NetworkUtility] No suitable network adapter found");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NetworkUtility] Error getting IP address: {ex.Message}");
                return null;
            }
        }
    }
}
