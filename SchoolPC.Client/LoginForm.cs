namespace SchoolPC.Client
{
    public partial class LoginForm : Form
    {
        private readonly LockdownManager _lockdownManager;
        private readonly DatabaseService _databaseService;
        private string? _authenticatedStudentId;
        private string? _authenticatedStudentName;

        public LoginForm()
        {
            InitializeComponent();
            _lockdownManager = new LockdownManager();
            _databaseService = new DatabaseService();
        }

        private void LoginForm_Load(object sender, EventArgs e)
        {
            // Set fullscreen and center the login panel
            this.WindowState = FormWindowState.Maximized;
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;

            // Load background image if exists
            LoadBackgroundImage();

            // Center all controls within the panel
            CenterAllControls();

            // Display PC name
            lblPCName.Text = $"PC: {Environment.MachineName}";

            // Enable lockdown mode
            _lockdownManager.EnableLockdown();

            // Focus username textbox
            txtUsername.Focus();

            // Handle Enter key press for login
            txtPassword.KeyPress += (s, evt) =>
            {
                if (evt.KeyChar == (char)Keys.Enter)
                {
                    BtnLogin_Click(btnLogin, EventArgs.Empty);
                }
            };

            // Handle window resize to keep controls centered
            this.Resize += (s, evt) => CenterAllControls();

            // Test database connection
            TestDatabaseConnection();
        }

        private void CenterAllControls()
        {
            // Calculate center position for the group of controls
            int formCenterX = this.ClientSize.Width / 2;

            // Define control widths (most inputs are 575 pixels wide)
            int controlWidth = 575;
            int leftMargin = formCenterX - (controlWidth / 2);

            // Center X position for all controls
            // Title spans full width and is centered with TextAlign
            lblTitle.Left = 0;
            lblTitle.Width = this.ClientSize.Width;
            lblTitle.Height = 80;

            // School name - center it properly
            lblSchoolName.AutoSize = false;
            lblSchoolName.Left = leftMargin;
            lblSchoolName.Width = controlWidth;
            lblSchoolName.Height = 40;

            // All input controls aligned to center
            lblPCName.Left = leftMargin;
            lblUsername.Left = leftMargin;
            txtUsername.Left = leftMargin;
            txtUsername.Width = controlWidth;

            lblPassword.Left = leftMargin;
            txtPassword.Left = leftMargin;
            txtPassword.Width = controlWidth;

            lblStatus.Left = leftMargin;
            lblStatus.Width = controlWidth;

            btnLogin.Left = leftMargin;
            btnLogin.Width = controlWidth;

            // Center Y position - start from top with padding
            int topPadding = Math.Max(40, (this.ClientSize.Height - 520) / 2);

            lblTitle.Top = topPadding + 50;
            lblSchoolName.Top = topPadding + 135;  // After lblTitle (50 + 80 = 130, added 5px gap)
            lblPCName.Top = topPadding + 185;
            lblUsername.Top = topPadding + 230;
            txtUsername.Top = topPadding + 260;
            lblPassword.Top = topPadding + 300;
            txtPassword.Top = topPadding + 327;
            lblStatus.Top = topPadding + 361;
            btnLogin.Top = topPadding + 392;
        }

        private void LoadBackgroundImage()
        {
            // Try to load background image from Images folder
            string[] possiblePaths = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "uic_background.jpg"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "uic_background.png"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "login_background.jpg"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "login_background.png"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "background.jpg"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "background.png")
            };

            foreach (string path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    try
                    {
                        this.BackgroundImage = Image.FromFile(path);
                        this.BackgroundImageLayout = ImageLayout.Stretch;
                        break;
                    }
                    catch
                    {
                        // If image fails to load, continue with default dark background
                    }
                }
            }
        }

        private async void TestDatabaseConnection()
        {
            lblStatus.Text = "Testing database connection...";
            lblStatus.ForeColor = Color.Yellow;

            bool connected = await _databaseService.TestConnectionAsync();
            if (connected)
            {
                lblStatus.Text = "Database connected. Ready to login.";
                lblStatus.ForeColor = Color.LightGreen;
            }
            else
            {
                lblStatus.Text = "Database connection failed. Check XAMPP MySQL.";
                lblStatus.ForeColor = Color.Red;
            }

            await Task.Delay(3000);
            lblStatus.Text = "";
        }

        private async void BtnLogin_Click(object? sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                lblStatus.Text = "Please enter username and password.";
                lblStatus.ForeColor = Color.Red;
                return;
            }

            btnLogin.Enabled = false;
            lblStatus.Text = "Authenticating...";
            lblStatus.ForeColor = Color.Yellow;

            var (success, studentId, fullName) = await _databaseService.AuthenticateStudentAsync(username, password);

            if (success && studentId != null && fullName != null)
            {
                _authenticatedStudentId = studentId;
                _authenticatedStudentName = fullName;

                lblStatus.Text = $"Welcome, {fullName}!";
                lblStatus.ForeColor = Color.LightGreen;

                await Task.Delay(1000);

                // Disable lockdown and open main form
                _lockdownManager.DisableLockdown();
                OpenMainForm();
            }
            else
            {
                lblStatus.Text = "Invalid credentials or account is banned.";
                lblStatus.ForeColor = Color.Red;
                txtPassword.Clear();
                btnLogin.Enabled = true;
                txtUsername.Focus();
            }
        }

        private void OpenMainForm()
        {
            // Create and show the main form with student info
            var mainForm = new MainForm(
                _authenticatedStudentId!,
                _authenticatedStudentName!,
                _databaseService,
                _lockdownManager
            );

            // Hide this login form and show main form
            this.Hide();
            mainForm.FormClosed += (s, e) =>
            {
                // When main form closes, show login form again
                _lockdownManager.EnableLockdown();
                this.Show();
                txtUsername.Clear();
                txtPassword.Clear();
                txtUsername.Focus();
            };

            mainForm.Show();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Prevent closing the form when locked
            if (_authenticatedStudentId == null)
            {
                e.Cancel = true;
            }
            base.OnFormClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _lockdownManager.Dispose();
            base.OnFormClosed(e);
        }

        private void lblTitle_Click(object sender, EventArgs e)
        {

        }

        private void lblUsername_Click(object sender, EventArgs e)
        {

        }

        private void panelLogin_Paint(object sender, PaintEventArgs e)
        {

        }

        private void txtUsername_TextChanged(object sender, EventArgs e)
        {

        }

        private void panelLogin_Paint_1(object sender, PaintEventArgs e)
        {

        }

        private void txtPassword_TextChanged(object sender, EventArgs e)
        {

        }

        private void lblPassword_Click(object sender, EventArgs e)
        {

        }
    }
}
