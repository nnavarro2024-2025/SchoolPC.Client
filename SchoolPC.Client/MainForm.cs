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
        private SignalRClient? _signalRClient;

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

            ConnectToAdminServer();

            // Notify server that session started
            if (_signalRClient != null && _signalRClient.IsConnected)
            {
                await _signalRClient.NotifySessionStartedAsync(_studentId, _studentName);
            }
        }

        private void SetupTrayIcon()
        {
            // Use a default system icon or create custom icon
            // For now, use Application icon (you can customize this later)
            notifyIcon.Icon = SystemIcons.Application;
            notifyIcon.Text = $"School PC - {_studentName}";
            notifyIcon.Visible = true;
        }

        private async void ConnectToAdminServer()
        {
            _signalRClient = new SignalRClient(
                _lockdownManager,
                OnAdminMessageReceived,
                OnForceLogout
            );

            bool connected = await _signalRClient.ConnectAsync();
            if (connected)
            {
                lblConnectionStatus.Text = "Server: Connected";
                lblConnectionStatus.ForeColor = Color.Green;
            }
            else
            {
                lblConnectionStatus.Text = "Server: Connection failed";
                lblConnectionStatus.ForeColor = Color.Red;
            }
        }

        private void OnAdminMessageReceived(string message)
        {
            if (InvokeRequired)
            {
                Invoke(() => OnAdminMessageReceived(message));
                return;
            }

            MessageBox.Show(message, "Message from Admin", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OnForceLogout()
        {
            if (InvokeRequired)
            {
                Invoke(OnForceLogout);
                return;
            }

            PerformLogout();
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

            if (_signalRClient != null)
            {
                await _signalRClient.NotifySessionEndedAsync(_studentId);
                await _signalRClient.DisconnectAsync();
            }

            await _sessionManager.EndSessionAsync();
            this.Close();
        }

        private async void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (_sessionManager.IsSessionActive)
            {
                e.Cancel = true;
                await PerformLogout();
            }
        }

        // Tray icon event handlers
        private void MainForm_Resize(object? sender, EventArgs e)
        {
            // Minimize to tray when minimized
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
                notifyIcon.ShowBalloonTip(2000, "School PC", "Session minimized to system tray", ToolTipIcon.Info);
            }
        }

        private void NotifyIcon_DoubleClick(object? sender, EventArgs e)
        {
            // Restore window on double-click
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        private void MenuItemShow_Click(object? sender, EventArgs e)
        {
            // Show window from tray menu
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        private void MenuItemReportProblem_Click(object? sender, EventArgs e)
        {
            // Report problem from tray menu
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
            BtnReportProblem_Click(sender, e);
        }

        private void MenuItemLogout_Click(object? sender, EventArgs e)
        {
            // Logout from tray menu
            Show();
            WindowState = FormWindowState.Normal;
            BtnLogout_Click(sender, e);
        }

        public SignalRClient? SignalRClient => _signalRClient;
    }
}
