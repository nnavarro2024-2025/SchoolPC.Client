namespace SchoolPC.Client
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            // Check auto-startup configuration (optional prompt on first run)
            if (!AutoStartupHelper.IsAutoStartupEnabled())
            {
                AutoStartupHelper.ShowAutoStartupStatus();
            }

            Application.Run(new LoginForm());
        }
    }
}