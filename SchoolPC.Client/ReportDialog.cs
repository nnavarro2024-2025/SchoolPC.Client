using SchoolPC.Client.Services;

namespace SchoolPC.Client
{
    public partial class ReportDialog : Form
    {
        private readonly DatabaseService _databaseService;
        private readonly string _studentId;
        private readonly string _studentName;
        private bool _submitted = false;

        public ReportDialog(DatabaseService databaseService, string studentId, string studentName)
        {
            InitializeComponent();
            _databaseService = databaseService;
            _studentId = studentId;
            _studentName = studentName;

            txtMessage.Focus();
        }

        private async void BtnSubmit_Click(object? sender, EventArgs e)
        {
            string message = txtMessage.Text.Trim();

            if (string.IsNullOrWhiteSpace(message))
            {
                MessageBox.Show("Please describe the problem.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Send via SignalR to admin server
                var mainForm = Owner as MainForm;
                if (mainForm?.SignalRClient != null && mainForm.SignalRClient.IsConnected)
                {
                    await mainForm.SignalRClient.ReportProblemAsync(_studentId, _studentName, message);
                    MessageBox.Show("Problem reported successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Not connected to admin server.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnCancel_Click(object? sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
