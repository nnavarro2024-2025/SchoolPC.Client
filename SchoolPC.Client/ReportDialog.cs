using SchoolPC.Client.Services;

namespace SchoolPC.Client
{
    public partial class ReportDialog : Form
    {
        private readonly DatabaseService _databaseService;
        private readonly string _studentId;
        private readonly string _studentName;

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
                // Submit problem report directly to database
                string pcName = Environment.MachineName;
                bool success = await _databaseService.SubmitProblemReportAsync(
                    Program.ClientGuid, 
                    pcName, 
                    _studentId, 
                    _studentName, 
                    message
                );

                if (success)
                {
                    MessageBox.Show("Problem reported successfully. Thank you for reporting this issue.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK;
                }
                else
                {
                    MessageBox.Show("Failed to submit problem report. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error submitting report: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            this.Close();
        }

        private void BtnCancel_Click(object? sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
