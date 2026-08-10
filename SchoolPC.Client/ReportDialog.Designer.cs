namespace SchoolPC.Client
{
    partial class ReportDialog
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
            lblTitle = new Label();
            lblInstruction = new Label();
            txtMessage = new TextBox();
            btnSubmit = new Button();
            btnCancel = new Button();
            SuspendLayout();
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblTitle.Location = new Point(12, 12);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(194, 32);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Report Problem";
            // 
            // lblInstruction
            // 
            lblInstruction.AutoSize = true;
            lblInstruction.Font = new Font("Segoe UI", 9F);
            lblInstruction.Location = new Point(12, 50);
            lblInstruction.Name = "lblInstruction";
            lblInstruction.Size = new Size(346, 20);
            lblInstruction.TabIndex = 1;
            lblInstruction.Text = "Describe the problem with this PC (e.g., hardware issue):";
            // 
            // txtMessage
            // 
            txtMessage.Font = new Font("Segoe UI", 10F);
            txtMessage.Location = new Point(12, 80);
            txtMessage.Multiline = true;
            txtMessage.Name = "txtMessage";
            txtMessage.ScrollBars = ScrollBars.Vertical;
            txtMessage.Size = new Size(460, 150);
            txtMessage.TabIndex = 0;
            // 
            // btnSubmit
            // 
            btnSubmit.BackColor = Color.FromArgb(0, 120, 215);
            btnSubmit.FlatStyle = FlatStyle.Flat;
            btnSubmit.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnSubmit.ForeColor = Color.White;
            btnSubmit.Location = new Point(272, 245);
            btnSubmit.Name = "btnSubmit";
            btnSubmit.Size = new Size(100, 40);
            btnSubmit.TabIndex = 1;
            btnSubmit.Text = "Submit";
            btnSubmit.UseVisualStyleBackColor = false;
            btnSubmit.Click += BtnSubmit_Click;
            // 
            // btnCancel
            // 
            btnCancel.BackColor = Color.Gray;
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.Font = new Font("Segoe UI", 10F);
            btnCancel.ForeColor = Color.White;
            btnCancel.Location = new Point(378, 245);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(94, 40);
            btnCancel.TabIndex = 2;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = false;
            btnCancel.Click += BtnCancel_Click;
            // 
            // ReportDialog
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(484, 300);
            Controls.Add(btnCancel);
            Controls.Add(btnSubmit);
            Controls.Add(txtMessage);
            Controls.Add(lblInstruction);
            Controls.Add(lblTitle);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ReportDialog";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Report Problem";
            ResumeLayout(false);
            PerformLayout();
        }

        private Label lblTitle;
        private Label lblInstruction;
        private TextBox txtMessage;
        private Button btnSubmit;
        private Button btnCancel;
    }
}
