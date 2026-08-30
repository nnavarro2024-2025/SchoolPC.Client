using SchoolPC.Client.Services;

namespace SchoolPC.Client
{
    internal static class Program
    {
        /// <summary>
        /// The persistent client GUID for this installation.
        /// Retrieved on startup from C:\ProgramData\SchoolPC\client_id.txt
        /// and passed to SignalR hub for unique PC identification.
        /// </summary>
        public static string ClientGuid { get; private set; } = string.Empty;

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            // IMPORTANT: Initialize persistent client GUID FIRST, before any other services.
            // This GUID is unique per physical PC installation and persists even if the
            // Windows machine name is changed. It's used by SignalR to uniquely identify
            // this client to the admin server.
            ClientGuid = ClientIdentityService.GetOrCreateClientGuid();
            Console.WriteLine($"Application started. Client GUID: {ClientGuid}");

            // Check auto-startup configuration (optional prompt on first run)
            if (!AutoStartupHelper.IsAutoStartupEnabled())
            {
                AutoStartupHelper.ShowAutoStartupStatus();
            }

            Application.Run(new LoginForm());
        }
    }
}