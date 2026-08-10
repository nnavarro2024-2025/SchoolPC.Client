using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SchoolPC.Client
{
    public class LockdownManager
    {
        #region Win32 API Declarations

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern bool BlockInput(bool fBlockIt);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        #endregion

        private IntPtr _hookID = IntPtr.Zero;
        private LowLevelKeyboardProc? _proc;
        private System.Threading.Timer? _processMonitor;
        private bool _isLocked = false;

        private readonly HashSet<string> _blockedProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "taskmgr",
            "cmd",
            "powershell",
            "regedit",
            "msconfig",
            "control"
            // REMOVED "explorer" - it kills the Windows shell (desktop, taskbar, everything!)
        };

        public void EnableLockdown()
        {
            if (_isLocked) return;

            _isLocked = true;
            _proc = HookCallback;
            _hookID = SetHook(_proc);

            StartProcessMonitoring();
        }

        public void DisableLockdown()
        {
            if (!_isLocked) return;

            _isLocked = false;

            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }

            StopProcessMonitoring();

            // Restart explorer if it's not running (in case it was killed)
            RestartExplorerIfNeeded();
        }

        private void RestartExplorerIfNeeded()
        {
            try
            {
                // Check if explorer is running
                var explorerProcesses = Process.GetProcessesByName("explorer");
                if (explorerProcesses.Length == 0)
                {
                    // Explorer is not running, restart it
                    Process.Start("explorer.exe");
                }
            }
            catch
            {
                // If we can't check/restart explorer, just continue
            }
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule? curModule = curProcess.MainModule)
            {
                if (curModule != null)
                {
                    return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
                }
            }
            return IntPtr.Zero;
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                int vkCode = Marshal.ReadInt32(lParam);

                // Block specific key combinations
                bool altPressed = (Control.ModifierKeys & Keys.Alt) != 0;
                bool ctrlPressed = (Control.ModifierKeys & Keys.Control) != 0;
                bool shiftPressed = (Control.ModifierKeys & Keys.Shift) != 0;

                // Block Alt+Tab
                if (altPressed && vkCode == (int)Keys.Tab)
                {
                    return (IntPtr)1;
                }

                // Block Alt+F4
                if (altPressed && vkCode == (int)Keys.F4)
                {
                    return (IntPtr)1;
                }

                // Block Ctrl+Alt+Delete (partial - requires system-level privileges)
                if (ctrlPressed && altPressed && vkCode == (int)Keys.Delete)
                {
                    return (IntPtr)1;
                }

                // Block Ctrl+Shift+Esc (Task Manager)
                if (ctrlPressed && shiftPressed && vkCode == (int)Keys.Escape)
                {
                    return (IntPtr)1;
                }

                // Block Windows key
                if (vkCode == 0x5B || vkCode == 0x5C) // VK_LWIN or VK_RWIN
                {
                    return (IntPtr)1;
                }

                // Block F1 (Help)
                if (vkCode == (int)Keys.F1)
                {
                    return (IntPtr)1;
                }

                // Block Alt+Esc
                if (altPressed && vkCode == (int)Keys.Escape)
                {
                    return (IntPtr)1;
                }

                // Block Ctrl+Esc
                if (ctrlPressed && vkCode == (int)Keys.Escape)
                {
                    return (IntPtr)1;
                }
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private void StartProcessMonitoring()
        {
            _processMonitor = new System.Threading.Timer(MonitorProcesses, null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
        }

        private void StopProcessMonitoring()
        {
            _processMonitor?.Dispose();
            _processMonitor = null;
        }

        private void MonitorProcesses(object? state)
        {
            if (!_isLocked) return;

            try
            {
                foreach (var process in Process.GetProcesses())
                {
                    try
                    {
                        if (_blockedProcesses.Contains(process.ProcessName))
                        {
                            process.Kill();
                            process.WaitForExit(1000);
                        }
                    }
                    catch
                    {
                        // Process may have already exited or we don't have permission
                    }
                }
            }
            catch
            {
                // Ignore errors during process enumeration
            }
        }

        public void Dispose()
        {
            DisableLockdown();
        }

        #region Remote Control Commands

        public static void Shutdown(int delaySeconds = 0)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "shutdown.exe",
                    Arguments = $"/s /t {delaySeconds}",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Shutdown failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static void Restart(int delaySeconds = 0)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "shutdown.exe",
                    Arguments = $"/r /t {delaySeconds}",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Restart failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static void Logout()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "shutdown.exe",
                    Arguments = "/l",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Logout failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion
    }
}
