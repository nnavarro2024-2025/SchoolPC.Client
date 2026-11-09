using SchoolPC.Client.Services;

namespace SchoolPC.Client
{
    public partial class MainForm : Form
    {
        private readonly string _studentId;
        private readonly string _studentName;
        private readonly DatabaseService _databaseService;
        private readonly LockdownManager _lockdownManager;
        private readonly SessionManager _sessionManager;
        private bool _isLoggingOut = false;

        public MainForm(
            string studentId,
            string studentName,
            DatabaseService databaseService,
            LockdownManager lockdownManager)
        {
            InitializeComponent();
            _studentId = studentId;
            _studentName = studentName;
            _databaseService = databaseService;
            _lockdownManager = lockdownManager;
            _sessionManager = new SessionManager(_databaseService);
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            lblWelcome.Text = $"Welcome, {_studentName}!";

            // Set up the tray icon
            SetupTrayIcon();

            await _sessionManager.StartSessionAsync(_studentId, _studentName);

            UpdateSessionInfo();
            timerSession.Start();

            // Hook Windows shutdown events
            Microsoft.Win32.SystemEvents.SessionEnding += OnWindowsSessionEnding;
        }

        private void SetupTrayIcon()
        {
            notifyIcon.Icon = SystemIcons.Application;
            notifyIcon.Text = $"School PC - {_studentName}";
            notifyIcon.Visible = true;
        }

        private void TimerSession_Tick(object? sender, EventArgs e)
        {
            UpdateSessionInfo();
        }

        private void UpdateSessionInfo()
        {
            lblSessionInfo.Text = _sessionManager.GetSessionInfo();
        }

        private void BtnReportProblem_Click(object? sender, EventArgs e)
        {
            using var reportDialog = new ReportDialog(_databaseService, _studentId, _studentName);
            reportDialog.ShowDialog();
        }

        private async void BtnLogout_Click(object? sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to logout?",
                "Confirm Logout",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                await PerformLogout();
            }
        }

        private async Task PerformLogout()
        {
            timerSession.Stop();

            // End session with 'logout' reason
            await _sessionManager.EndSessionAsync(endReason: "logout");
            _isLoggingOut = true;
            this.Close();
        }

        private async void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // Unhook event on close
            Microsoft.Win32.SystemEvents.SessionEnding -= OnWindowsSessionEnding;

            // Allow closing if we're intentionally logging out
            if (_isLoggingOut)
            {
                return;
            }

            // Prevent closing when X button is clicked - minimize to system tray instead
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.WindowState = FormWindowState.Minimized;
                this.Hide();
                return;
            }

            // Allow closing only if explicitly logging out or system shutdown
            if (_sessionManager.IsSessionActive)
            {
                e.Cancel = true;
                await PerformLogout();
            }
        }

        private void OnWindowsSessionEnding(object sender, Microsoft.Win32.SessionEndingEventArgs e)
        {
            Console.WriteLine("[MainForm] Windows session ending (shutdown/restart/sign-out)");
            e.Cancel = false; // Don't block Windows shutdown

            // End session synchronously with a 2-second timeout.
            // async void handlers return at the first await, so Windows won't wait.
            // We must run this synchronously to ensure the session is closed before Windows kills the process.
            try
            {
                _sessionManager.EndSessionSync(endReason: "windows shutdown");
                Console.WriteLine("[MainForm] Windows shutdown session closed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MainForm] Error during Windows shutdown: {ex.Message}");
            }
        }

        // Tray icon event handlers
        private void MainForm_Resize(object? sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
                notifyIcon.ShowBalloonTip(2000, "School PC", "Session minimized to system tray", ToolTipIcon.Info);
            }
        }

        private void NotifyIcon_DoubleClick(object? sender, EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        private void MenuItemShow_Click(object? sender, EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        private void MenuItemReportProblem_Click(object? sender, EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
            BtnReportProblem_Click(sender, e);
        }

        private void MenuItemLogout_Click(object? sender, EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
            BtnLogout_Click(sender, e);
        }

            }
        }
