using Microsoft.Win32;
using System.Diagnostics;
using System.Security.Principal;

namespace SchoolPC.Client
{
    public static class AutoStartupHelper
    {
        private const string APP_NAME = "SchoolPC.Client";
        private const string RUN_KEY_PATH = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        public static bool IsAutoStartupEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RUN_KEY_PATH, false);
                return key?.GetValue(APP_NAME) != null;
            }
            catch
            {
                return false;
            }
        }

        public static bool EnableAutoStartup()
        {
            try
            {
                string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
                if (string.IsNullOrEmpty(exePath))
                    return false;

                using var key = Registry.CurrentUser.OpenSubKey(RUN_KEY_PATH, true);
                if (key == null)
                    return false;

                key.SetValue(APP_NAME, $"\"{exePath}\"");
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to enable auto-startup: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public static bool DisableAutoStartup()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RUN_KEY_PATH, true);
                if (key == null)
                    return false;

                if (key.GetValue(APP_NAME) != null)
                {
                    key.DeleteValue(APP_NAME);
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to disable auto-startup: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public static bool IsRunningAsAdministrator()
        {
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        public static void ShowAutoStartupStatus()
        {
            bool isEnabled = IsAutoStartupEnabled();
            bool isAdmin = IsRunningAsAdministrator();

            string message = $"Auto-Startup: {(isEnabled ? "Enabled" : "Disabled")}\n" +
                           $"Running as Admin: {(isAdmin ? "Yes" : "No")}\n\n" +
                           "Note: For full system lockdown, the app should:\n" +
                           "1. Run at Windows startup\n" +
                           "2. Run with administrative privileges\n\n" +
                           "Would you like to enable auto-startup now?";

            if (!isEnabled)
            {
                var result = MessageBox.Show(message, "Auto-Startup Configuration", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    if (EnableAutoStartup())
                    {
                        MessageBox.Show("Auto-startup enabled successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }
    }
}
