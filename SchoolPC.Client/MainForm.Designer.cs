namespace SchoolPC.Client
{
    partial class MainForm
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
            components = new System.ComponentModel.Container();
            lblWelcome = new Label();
            lblSessionInfo = new Label();
            btnReportProblem = new Button();
            btnLogout = new Button();
            timerSession = new System.Windows.Forms.Timer(components);
            lblConnectionStatus = new Label();
            notifyIcon = new NotifyIcon(components);
            contextMenuTray = new ContextMenuStrip(components);
            menuItemShow = new ToolStripMenuItem();
            menuItemReportProblem = new ToolStripMenuItem();
            menuItemLogout = new ToolStripMenuItem();
            contextMenuTray.SuspendLayout();
            SuspendLayout();
            // 
            // lblWelcome
            // 
            lblWelcome.AutoSize = true;
            lblWelcome.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblWelcome.Location = new Point(10, 9);
            lblWelcome.Name = "lblWelcome";
            lblWelcome.Size = new Size(108, 30);
            lblWelcome.TabIndex = 0;
            lblWelcome.Text = "Welcome";
            // 
            // lblSessionInfo
            // 
            lblSessionInfo.Font = new Font("Segoe UI", 10F);
            lblSessionInfo.Location = new Point(10, 45);
            lblSessionInfo.Name = "lblSessionInfo";
            lblSessionInfo.Size = new Size(350, 90);
            lblSessionInfo.TabIndex = 1;
            lblSessionInfo.Text = "Session Info";
            // 
            // btnReportProblem
            // 
            btnReportProblem.BackColor = Color.FromArgb(255, 165, 0);
            btnReportProblem.FlatStyle = FlatStyle.Flat;
            btnReportProblem.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            btnReportProblem.ForeColor = Color.White;
            btnReportProblem.Location = new Point(10, 146);
            btnReportProblem.Margin = new Padding(3, 2, 3, 2);
            btnReportProblem.Name = "btnReportProblem";
            btnReportProblem.Size = new Size(175, 38);
            btnReportProblem.TabIndex = 0;
            btnReportProblem.Text = "Report Problem";
            btnReportProblem.UseVisualStyleBackColor = false;
            btnReportProblem.Click += BtnReportProblem_Click;
            // 
            // btnLogout
            // 
            btnLogout.BackColor = Color.FromArgb(192, 0, 0);
            btnLogout.FlatStyle = FlatStyle.Flat;
            btnLogout.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            btnLogout.ForeColor = Color.White;
            btnLogout.Location = new Point(191, 146);
            btnLogout.Margin = new Padding(3, 2, 3, 2);
            btnLogout.Name = "btnLogout";
            btnLogout.Size = new Size(105, 38);
            btnLogout.TabIndex = 1;
            btnLogout.Text = "Logout";
            btnLogout.UseVisualStyleBackColor = false;
            btnLogout.Click += BtnLogout_Click;
            // 
            // timerSession
            // 
            timerSession.Interval = 1000;
            timerSession.Tick += TimerSession_Tick;
            // 
            // lblConnectionStatus
            // 
            lblConnectionStatus.AutoSize = true;
            lblConnectionStatus.Font = new Font("Segoe UI", 9F);
            lblConnectionStatus.ForeColor = Color.Gray;
            lblConnectionStatus.Location = new Point(10, 191);
            lblConnectionStatus.Name = "lblConnectionStatus";
            lblConnectionStatus.Size = new Size(124, 15);
            lblConnectionStatus.TabIndex = 4;
            lblConnectionStatus.Text = "Server: Not connected";
            // 
            // notifyIcon
            // 
            notifyIcon.ContextMenuStrip = contextMenuTray;
            notifyIcon.Text = "School PC Student Session";
            notifyIcon.Visible = true;
            notifyIcon.DoubleClick += NotifyIcon_DoubleClick;
            // 
            // contextMenuTray
            // 
            contextMenuTray.Items.AddRange(new ToolStripItem[] { menuItemShow, menuItemReportProblem, menuItemLogout });
            contextMenuTray.Name = "contextMenuTray";
            contextMenuTray.Size = new Size(181, 70);
            // 
            // menuItemShow
            // 
            menuItemShow.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            menuItemShow.Name = "menuItemShow";
            menuItemShow.Size = new Size(180, 22);
            menuItemShow.Text = "Show Window";
            menuItemShow.Click += MenuItemShow_Click;
            // 
            // menuItemReportProblem
            // 
            menuItemReportProblem.Name = "menuItemReportProblem";
            menuItemReportProblem.Size = new Size(180, 22);
            menuItemReportProblem.Text = "Report Problem";
            menuItemReportProblem.Click += MenuItemReportProblem_Click;
            // 
            // menuItemLogout
            // 
            menuItemLogout.Name = "menuItemLogout";
            menuItemLogout.Size = new Size(180, 22);
            menuItemLogout.Text = "Logout";
            menuItemLogout.Click += MenuItemLogout_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(484, 253);
            Controls.Add(lblConnectionStatus);
            Controls.Add(btnLogout);
            Controls.Add(btnReportProblem);
            Controls.Add(lblSessionInfo);
            Controls.Add(lblWelcome);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Margin = new Padding(3, 2, 3, 2);
            MaximizeBox = false;
            MinimizeBox = true;
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "School PC - Student Session";
            FormClosing += MainForm_FormClosing;
            Load += MainForm_Load;
            Resize += MainForm_Resize;
            contextMenuTray.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        private Label lblWelcome;
        private Label lblSessionInfo;
        private Button btnReportProblem;
        private Button btnLogout;
        private System.Windows.Forms.Timer timerSession;
        private Label lblConnectionStatus;
        private NotifyIcon notifyIcon;
        private ContextMenuStrip contextMenuTray;
        private ToolStripMenuItem menuItemShow;
        private ToolStripMenuItem menuItemReportProblem;
        private ToolStripMenuItem menuItemLogout;
    }
}
