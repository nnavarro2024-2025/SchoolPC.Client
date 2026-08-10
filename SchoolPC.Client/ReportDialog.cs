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

            if (string.IsNullOrEmpty(message))
            {
                MessageBox.Show("Please enter a problem description.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnSubmit.Enabled = false;
            btnCancel.Enabled = false;

            string pcName = Environment.MachineName;
            bool success = await _databaseService.SubmitProblemReportAsync(pcName, _studentId, _studentName, message);

            if (success)
            {
                MessageBox.Show("Problem report submitted successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _submitted = true;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Failed to submit report. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnSubmit.Enabled = true;
                btnCancel.Enabled = true;
            }
        }

        private void BtnCancel_Click(object? sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
