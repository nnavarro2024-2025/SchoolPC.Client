using SchoolPC.Client.Services;
using System.Net.NetworkInformation;
using System.Diagnostics;

namespace SchoolPC.Client
{
    internal static class Program
    {
        /// <summary>
        /// A stopwatch started at the very first line of Main.
        /// Used to measure elapsed time from app launch to the first successful database clock read.
        /// This allows us to compute the app's true launch time on the database's clock,
        /// without relying on the potentially-different PC clock.
        /// </summary>
        private static readonly Stopwatch _appLaunchStopwatch = new Stopwatch();

        /// <summary>
        /// The persistent client GUID for this installation.
        /// Retrieved on startup from C:\ProgramData\SchoolPC\client_id.txt
        /// and used to uniquely identify this client for crash recovery, PC registration, and heartbeat.
        /// </summary>
        public static string ClientGuid { get; private set; } = string.Empty;

        /// <summary>
        /// The database server's clock time at application launch, expressed in database time.
        /// Computed when the database is first reachable: appStartDbTime = dbNow - stopwatch.Elapsed.
        /// Truncated to whole seconds (round DOWN) to match session.login_time precision.
        /// Used by CrashRecoveryAsync to exclude sessions created after launch from recovery.
        /// Sessions with login_time < AppStartDbTime are treated as orphaned from previous crashes.
        /// </summary>
        public static DateTime AppStartDbTime { get; private set; } = DateTime.MinValue;

        /// <summary>
        /// The global heartbeat timer that runs every 60 seconds to update pc_instances.last_seen.
        /// Runs for the entire lifetime of the application, even while at the login screen.
        /// Uses System.Threading.Timer, not UI timer, since it runs on a background thread.
        /// </summary>
        private static System.Threading.Timer? _heartbeatTimer;
        private static DatabaseService? _globalDatabaseService;
        private static CancellationTokenSource? _startupCts;

        /// <summary>
        /// The main entry point for the application.
        /// MUST be synchronous with [STAThread] so Application.Run runs on the STA thread.
        /// Startup work runs in the background so the login screen appears immediately.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // START STOPWATCH FIRST - This is the baseline for crash recovery cutoff.
            // Even if MySQL is down, this captures the true app launch moment.
            _appLaunchStopwatch.Start();

            ApplicationConfiguration.Initialize();

            // IMPORTANT: Initialize persistent client GUID FIRST, before any other services.
            // This GUID is unique per physical PC installation and persists even if the
            // Windows machine name is changed. It's used by the database to uniquely identify
            // this client.
            ClientGuid = ClientIdentityService.GetOrCreateClientGuid();
            System.Diagnostics.Debug.WriteLine($"[Program] Application started. Client GUID: {ClientGuid}");

            // Initialize database service
            _globalDatabaseService = new DatabaseService();

            // Check for config errors and warn user
            string? configError = DatabaseService.GetConfigError();
            if (configError != null)
            {
                System.Diagnostics.Debug.WriteLine($"[Program] CONFIG WARNING: {configError}");
            }

            // Create cancellation token for startup task, allowing graceful shutdown
            _startupCts = new CancellationTokenSource();

            // Fire off startup work in background. This does NOT block the UI thread.
            // Order: crash recovery -> registration -> start heartbeat (if recovery succeeded)
            _ = Task.Run(() => RunStartupWorkAsync(_startupCts.Token), _startupCts.Token);

            // Check auto-startup configuration (optional prompt on first run)
            if (!AutoStartupHelper.IsAutoStartupEnabled())
            {
                AutoStartupHelper.ShowAutoStartupStatus();
            }

            // DEBUG: Verify we're on STA thread before running WinForms
            System.Diagnostics.Debug.WriteLine(Thread.CurrentThread.GetApartmentState());

            // Show the login form - THIS RUNS ON THE STA THREAD
            Application.Run(new LoginForm());

            // Cleanup shutdown
            _startupCts?.Cancel();
            _heartbeatTimer?.Dispose();
        }

        /// <summary>
        /// Runs startup work asynchronously in the background.
        /// Order of operations (CRITICAL):
        ///   1. Read database server time (establishes cutoff for crash recovery)
        ///   2. Crash recovery (closes orphaned sessions with login_time < appStartDbTime)
        ///   3. PC registration (sets initial last_seen = NOW())
        ///   4. Start heartbeat (60-second interval, updates last_seen)
        /// 
        /// If the database is unreachable:
        ///   - Retry database time read -> recovery -> registration every 60 seconds until both succeed
        ///   - Once both succeed, start the heartbeat
        ///   - The heartbeat will use INSERT ... ON DUPLICATE KEY UPDATE to update even if
        ///     the PC was registered before the database came online
        /// </summary>
        private static async Task RunStartupWorkAsync(CancellationToken cancellationToken)
        {
            try
            {
                bool appStartTimeAcquired = false;
                bool recoverySucceeded = false;
                bool registrationSucceeded = false;

                while (!cancellationToken.IsCancellationRequested && !registrationSucceeded)
                {
                    // Step 0: Acquire database server time (MUST happen first, before 1st recovery attempt)
                    if (!appStartTimeAcquired)
                    {
                        System.Diagnostics.Debug.WriteLine("[Program.StartupWork] Reading database server time...");
                        DateTime dbNow = await _globalDatabaseService!.GetDatabaseTimeAsync();
                        if (dbNow != DateTime.MinValue)
                        {
                            // Compute the app's launch time on the database clock:
                            // appStartDbTime = dbNow - stopwatch.Elapsed
                            // Then truncate to whole seconds (round DOWN) to match login_time precision
                            TimeSpan elapsed = _appLaunchStopwatch.Elapsed;
                            DateTime appLaunchDbTime = dbNow.Subtract(elapsed);
                            AppStartDbTime = appLaunchDbTime.AddTicks(-(appLaunchDbTime.Ticks % TimeSpan.TicksPerSecond));

                            appStartTimeAcquired = true;
                            System.Diagnostics.Debug.WriteLine($"[Program.StartupWork] Database time acquired. Stopwatch elapsed: {elapsed.TotalMilliseconds:F0}ms. App start cutoff: {AppStartDbTime}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("[Program.StartupWork] Failed to read database time; will retry in 60 seconds...");
                        }
                    }

                    // Step 1: Crash Recovery (only after we have the db time cutoff)
                    if (appStartTimeAcquired && !recoverySucceeded)
                    {
                        System.Diagnostics.Debug.WriteLine("[Program.StartupWork] Running crash recovery...");
                        recoverySucceeded = await _globalDatabaseService.CrashRecoveryAsync(ClientGuid, AppStartDbTime);
                        if (!recoverySucceeded)
                        {
                            System.Diagnostics.Debug.WriteLine("[Program.StartupWork] Crash recovery failed; will retry in 60 seconds...");
                        }
                    }

                    // Step 2: PC Registration (only after recovery succeeds)
                    if (recoverySucceeded && !registrationSucceeded)
                    {
                        System.Diagnostics.Debug.WriteLine("[Program.StartupWork] Registering/updating PC...");
                        string machineName = Environment.MachineName;
                        string? ipAddress = NetworkUtility.GetLocalLanIpAddress();
                        registrationSucceeded = await _globalDatabaseService.RegisterOrUpdatePcAsync(ClientGuid, machineName, ipAddress);
                        if (!registrationSucceeded)
                        {
                            System.Diagnostics.Debug.WriteLine("[Program.StartupWork] PC registration failed; will retry in 60 seconds...");
                        }
                    }

                    // Step 3: If both recovery and registration succeeded, start the heartbeat
                    if (recoverySucceeded && registrationSucceeded)
                    {
                        System.Diagnostics.Debug.WriteLine("[Program.StartupWork] Crash recovery and registration succeeded. Starting heartbeat...");
                        StartHeartbeatTimer();
                        break;
                    }

                    // Wait 60 seconds before retrying
                    try
                    {
                        await Task.Delay(60000, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Program.StartupWork] Unhandled exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Starts the global heartbeat timer using System.Threading.Timer.
        /// This runs every 60 seconds on a background thread (not the UI thread).
        /// Updates pc_instances.last_seen and ip_address.
        /// Uses INSERT ... ON DUPLICATE KEY UPDATE so it works even if PC was registered before DB was online.
        /// Failures are silent.
        /// </summary>
        private static void StartHeartbeatTimer()
        {
            // Use System.Threading.Timer (background thread), not UI Timer
            _heartbeatTimer = new System.Threading.Timer(
                async _ => await OnHeartbeatTick(),
                null,
                TimeSpan.Zero,           // Start immediately
                TimeSpan.FromSeconds(60) // Every 60 seconds
            );
            System.Diagnostics.Debug.WriteLine("[Program] Heartbeat timer started (60 second interval)");
        }

        /// <summary>
        /// Called every 60 seconds by the heartbeat timer (on a background thread).
        /// Updates pc_instances.last_seen and ip_address in the database.
        /// Failures are silent and don't affect the application.
        /// </summary>
        private static async Task OnHeartbeatTick()
        {
            try
            {
                if (_globalDatabaseService == null)
                    return;

                string? ipAddress = NetworkUtility.GetLocalLanIpAddress();
                await _globalDatabaseService.UpdatePcHeartbeatAsync(ClientGuid, ipAddress);
            }
            catch (Exception ex)
            {
                // Silent failure - heartbeat is not critical
                System.Diagnostics.Debug.WriteLine($"[Program] Heartbeat update failed (silent): {ex.Message}");
            }
        }
    }
}
