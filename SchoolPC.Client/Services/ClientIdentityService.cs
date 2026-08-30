namespace SchoolPC.Client.Services
{
    /// <summary>
    /// Singleton service for managing the persistent client identity (GUID).
    /// On first run, generates a new GUID and stores it in C:\ProgramData\SchoolPC\client_id.txt.
    /// On subsequent runs, reads the existing GUID from the file.
    /// This GUID survives Windows machine name changes and is used by SignalR 
    /// to uniquely identify this physical PC installation.
    /// </summary>
    public static class ClientIdentityService
    {
        private static string? _clientGuid;
        private static readonly string ClientIdFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "SchoolPC",
            "client_id.txt"
        );

        private static readonly object _lockObject = new object();

        /// <summary>
        /// Gets or creates the persistent client GUID for this installation.
        /// Thread-safe. On first call, checks if the GUID file exists; if not, 
        /// generates a new one and attempts to write it. Includes graceful error 
        /// handling: if write fails (permissions issue, etc.), logs the error 
        /// and returns the in-memory GUID anyway (it won't persist across restart, 
        /// but won't crash the app).
        /// </summary>
        /// <returns>A GUID string (36 characters, UUID format)</returns>
        public static string GetOrCreateClientGuid()
        {
            lock (_lockObject)
            {
                // Return cached value if already loaded
                if (!string.IsNullOrEmpty(_clientGuid))
                {
                    return _clientGuid;
                }

                // Try to read existing GUID from file
                if (File.Exists(ClientIdFilePath))
                {
                    try
                    {
                        string content = File.ReadAllText(ClientIdFilePath).Trim();

                        // Validate it's a valid GUID
                        if (Guid.TryParse(content, out Guid parsedGuid))
                        {
                            _clientGuid = parsedGuid.ToString();
                            Console.WriteLine($"[ClientIdentityService] Loaded existing client GUID: {_clientGuid}");
                            return _clientGuid;
                        }
                        else
                        {
                            // File exists but doesn't contain valid GUID; regenerate
                            Console.WriteLine("[ClientIdentityService] WARNING: Existing client_id.txt contains invalid GUID. Regenerating...");
                            _clientGuid = Guid.NewGuid().ToString();
                            TryWriteClientGuidToFile(_clientGuid);
                            return _clientGuid;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ClientIdentityService] ERROR reading existing GUID file: {ex.Message}");
                        // Fall through to generate new one
                    }
                }

                // File doesn't exist or couldn't be read; generate new GUID
                _clientGuid = Guid.NewGuid().ToString();
                Console.WriteLine($"[ClientIdentityService] Generated new client GUID: {_clientGuid}");
                TryWriteClientGuidToFile(_clientGuid);

                return _clientGuid;
            }
        }

        /// <summary>
        /// Attempts to write the client GUID to the persistent file location.
        /// If the folder or file can't be created (permission issues, missing folder, etc.),
        /// logs the error but doesn't crash. The GUID will still be in memory for this session,
        /// just won't persist to the next restart.
        /// </summary>
        private static void TryWriteClientGuidToFile(string guid)
        {
            try
            {
                // Ensure the directory exists
                string directory = Path.GetDirectoryName(ClientIdFilePath)!;
                if (!Directory.Exists(directory))
                {
                    Console.WriteLine($"[ClientIdentityService] Creating directory: {directory}");
                    Directory.CreateDirectory(directory);
                }

                // Write GUID to file
                File.WriteAllText(ClientIdFilePath, guid);
                Console.WriteLine($"[ClientIdentityService] Successfully persisted client GUID to: {ClientIdFilePath}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"[ClientIdentityService] WARNING: Permission denied writing to {ClientIdFilePath}. " +
                    $"Client GUID will not persist across restarts. Error: {ex.Message}");
                Console.WriteLine($"[ClientIdentityService] Ensure C:\\ProgramData\\SchoolPC folder has write permissions. " +
                    $"Run the deployment script (deploy-schoolpc.ps1) as Administrator.");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"[ClientIdentityService] WARNING: I/O error writing client ID file: {ex.Message}. " +
                    $"Client GUID will not persist across restarts.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ClientIdentityService] WARNING: Unexpected error writing client ID file: {ex.GetType().Name}: {ex.Message}. " +
                    $"Client GUID will not persist across restarts.");
            }
        }

        /// <summary>
        /// Gets the folder path for the persistent client identity file.
        /// Used primarily for deployment/testing purposes.
        /// </summary>
        public static string GetClientIdFolderPath => Path.GetDirectoryName(ClientIdFilePath)!;

        /// <summary>
        /// Gets the full file path for the persistent client identity file.
        /// Used primarily for deployment/testing purposes.
        /// </summary>
        public static string GetClientIdFilePath => ClientIdFilePath;
    }
}
