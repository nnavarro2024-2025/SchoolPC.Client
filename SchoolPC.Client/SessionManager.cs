namespace SchoolPC.Client
{
    public class SessionManager
    {
        private readonly DatabaseService _databaseService;
        private int _currentSessionId = -1;
        private string? _studentId;
        private string? _studentName;
        private DateTime _loginTime;

        public bool IsSessionActive => _currentSessionId != -1;
        public string? StudentId => _studentId;
        public string? StudentName => _studentName;
        public DateTime LoginTime => _loginTime;
        public TimeSpan SessionDuration => DateTime.Now - _loginTime;

        public SessionManager(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task<bool> StartSessionAsync(string studentId, string studentName)
        {
            if (IsSessionActive)
            {
                await EndSessionAsync();
            }

            string pcName = Environment.MachineName;
            _currentSessionId = await _databaseService.StartSessionAsync(Program.ClientGuid, pcName, studentId, studentName);

            if (_currentSessionId > 0)
            {
                _studentId = studentId;
                _studentName = studentName;
                _loginTime = DateTime.Now;
                return true;
            }

            return false;
        }

        public async Task<bool> EndSessionAsync()
        {
            if (!IsSessionActive)
                return false;

            bool success = await _databaseService.EndSessionAsync(_currentSessionId);

            if (success)
            {
                _currentSessionId = -1;
                _studentId = null;
                _studentName = null;
            }

            return success;
        }

        public string GetSessionInfo()
        {
            if (!IsSessionActive)
                return "No active session";

            TimeSpan duration = SessionDuration;
            return $"Student: {_studentName} ({_studentId})\n" +
                   $"PC: {Environment.MachineName}\n" +
                   $"Login Time: {_loginTime:HH:mm:ss}\n" +
                   $"Duration: {duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
        }
    }
}
