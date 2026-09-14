namespace SchoolPC.Client
{
    partial class LoginForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LoginForm));
            lblTitle = new Label();
            lblPCName = new Label();
            lblUsername = new Label();
            txtUsername = new TextBox();
            lblPassword = new Label();
            txtPassword = new TextBox();
            btnLogin = new Button();
            panelLogin = new Panel();
            lblStatus = new Label();
            lblSchoolName = new Label();
            panelLogin.SuspendLayout();
            SuspendLayout();
            // 
            // lblTitle
            // 
            lblTitle.BackColor = Color.Transparent;
            lblTitle.Font = new Font("Segoe UI Black", 32F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTitle.ForeColor = Color.FromArgb(248, 130, 138);
            lblTitle.Location = new Point(0, 50);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(1418, 80);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Lab PC Login";
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            lblTitle.Click += lblTitle_Click;
            // 
            // lblPCName
            // 
            lblPCName.AutoSize = true;
            lblPCName.BackColor = Color.Transparent;
            lblPCName.Font = new Font("Segoe UI", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblPCName.ForeColor = Color.FromArgb(248, 130, 138);
            lblPCName.Location = new Point(389, 203);
            lblPCName.Name = "lblPCName";
            lblPCName.Size = new Size(46, 25);
            lblPCName.TabIndex = 2;
            lblPCName.Text = "PC: ";
            // 
            // lblUsername
            // 
            lblUsername.AutoSize = true;
            lblUsername.BackColor = Color.Transparent;
            lblUsername.Font = new Font("Segoe UI", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblUsername.ForeColor = Color.White;
            lblUsername.Location = new Point(389, 248);
            lblUsername.Name = "lblUsername";
            lblUsername.Size = new Size(106, 25);
            lblUsername.TabIndex = 3;
            lblUsername.Text = "Username:";
            lblUsername.Click += lblUsername_Click;
            // 
            // txtUsername
            // 
            txtUsername.Font = new Font("Segoe UI", 14F);
            txtUsername.Location = new Point(389, 278);
            txtUsername.Margin = new Padding(3, 2, 3, 2);
            txtUsername.Name = "txtUsername";
            txtUsername.PlaceholderText = "Username";
            txtUsername.Size = new Size(575, 32);
            txtUsername.TabIndex = 0;
            txtUsername.TextChanged += txtUsername_TextChanged;
            // 
            // lblPassword
            // 
            lblPassword.AutoSize = true;
            lblPassword.BackColor = Color.Transparent;
            lblPassword.Font = new Font("Segoe UI", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblPassword.ForeColor = Color.White;
            lblPassword.Location = new Point(389, 318);
            lblPassword.Name = "lblPassword";
            lblPassword.Size = new Size(102, 25);
            lblPassword.TabIndex = 5;
            lblPassword.Text = "Password:";
            lblPassword.Click += lblPassword_Click;
            // 
            // txtPassword
            // 
            txtPassword.Font = new Font("Segoe UI", 14F);
            txtPassword.Location = new Point(389, 345);
            txtPassword.Margin = new Padding(3, 2, 3, 2);
            txtPassword.Name = "txtPassword";
            txtPassword.PasswordChar = '*';
            txtPassword.PlaceholderText = "Password";
            txtPassword.Size = new Size(575, 32);
            txtPassword.TabIndex = 1;
            txtPassword.UseSystemPasswordChar = true;
            txtPassword.TextChanged += txtPassword_TextChanged;
            // 
            // btnLogin
            // 
            btnLogin.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnLogin.BackColor = Color.FromArgb(0, 120, 215);
            btnLogin.Cursor = Cursors.Hand;
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.FlatStyle = FlatStyle.Flat;
            btnLogin.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            btnLogin.ForeColor = Color.White;
            btnLogin.Location = new Point(389, 410);
            btnLogin.Margin = new Padding(3, 2, 3, 2);
            btnLogin.Name = "btnLogin";
            btnLogin.Size = new Size(575, 32);
            btnLogin.TabIndex = 2;
            btnLogin.Text = "Login";
            btnLogin.UseVisualStyleBackColor = false;
            btnLogin.Click += BtnLogin_Click;
            // 
            // panelLogin
            // 
            panelLogin.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            panelLogin.BackColor = Color.Transparent;
            panelLogin.BackgroundImage = (Image)resources.GetObject("panelLogin.BackgroundImage");
            panelLogin.BackgroundImageLayout = ImageLayout.Stretch;
            panelLogin.Controls.Add(lblStatus);
            panelLogin.Controls.Add(lblSchoolName);
            panelLogin.Controls.Add(lblTitle);
            panelLogin.Controls.Add(lblPCName);
            panelLogin.Controls.Add(lblUsername);
            panelLogin.Controls.Add(txtUsername);
            panelLogin.Controls.Add(lblPassword);
            panelLogin.Controls.Add(txtPassword);
            panelLogin.Controls.Add(btnLogin);
            panelLogin.Dock = DockStyle.Fill;
            panelLogin.ForeColor = Color.Transparent;
            panelLogin.Location = new Point(0, 0);
            panelLogin.Margin = new Padding(0);
            panelLogin.Name = "panelLogin";
            panelLogin.Size = new Size(1418, 690);
            panelLogin.TabIndex = 9;
            panelLogin.Paint += panelLogin_Paint_1;
            // 
            // lblStatus
            // 
            lblStatus.BackColor = Color.Transparent;
            lblStatus.Font = new Font("Segoe UI", 10F);
            lblStatus.ForeColor = Color.Yellow;
            lblStatus.ImageAlign = ContentAlignment.TopCenter;
            lblStatus.Location = new Point(389, 379);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(575, 29);
            lblStatus.TabIndex = 8;
            lblStatus.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblSchoolName
            // 
            lblSchoolName.AutoSize = true;
            lblSchoolName.BackColor = Color.Transparent;
            lblSchoolName.Font = new Font("Segoe UI", 15.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblSchoolName.ForeColor = Color.White;
            lblSchoolName.Location = new Point(497, 140);
            lblSchoolName.Margin = new Padding(0);
            lblSchoolName.Name = "lblSchoolName";
            lblSchoolName.Size = new Size(416, 30);
            lblSchoolName.TabIndex = 1;
            lblSchoolName.Text = "University of the Immaculate Conception";
            lblSchoolName.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // LoginForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(20, 20, 22);
            BackgroundImageLayout = ImageLayout.Zoom;
            ClientSize = new Size(1418, 690);
            ControlBox = false;
            Controls.Add(panelLogin);
            FormBorderStyle = FormBorderStyle.None;
            Margin = new Padding(3, 2, 3, 2);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "LoginForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Text = "School PC Login";
            TopMost = true;
            WindowState = FormWindowState.Maximized;
            Load += LoginForm_Load;
            panelLogin.ResumeLayout(false);
            panelLogin.PerformLayout();
            ResumeLayout(false);
        }

        private Label lblTitle;
        private Label lblSchoolName;
        private Label lblPCName;
        private Label lblUsername;
        private TextBox txtUsername;
        private Label lblPassword;
        private TextBox txtPassword;
        private Button btnLogin;
        private Panel panelLogin;
        private Label lblStatus;
    }
}
