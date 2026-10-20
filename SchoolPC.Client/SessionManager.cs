namespace SchoolPC.Client
{
    public class SessionManager
    {
        private readonly DatabaseService _databaseService;
        private int _currentSessionId = -1;
        private string? _studentId;
        private string? _studentName;
        private DateTime _loginTime;
        private int _isEndingSession = 0; // 0 = not ending, 1 = ending. Use Interlocked for thread-safety.

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

        public async Task<bool> EndSessionAsync(string endReason = "logout")
        {
            if (!IsSessionActive)
                return false;

            // Prevent double-ending: atomically exchange 0 -> 1.
            // If we get 0 back, we set it; if we get 1 back, someone else already set it.
            int previousState = System.Threading.Interlocked.CompareExchange(ref _isEndingSession, 1, 0);
            if (previousState != 0)
                return false; // Another thread is already ending this session

            try
            {
                bool success = await _databaseService.EndSessionAsync(_currentSessionId, endReason);

                if (success)
                {
                    _currentSessionId = -1;
                    _studentId = null;
                    _studentName = null;
                }

                return success;
            }
            finally
            {
                System.Threading.Interlocked.Exchange(ref _isEndingSession, 0);
            }
        }

        /// <summary>
        /// Synchronously ends the session with very short timeouts.
        /// Used by Windows shutdown handler to ensure completion before Windows kills the app.
        /// Calls the blocking (non-async) EndSessionBlocking method to avoid deadlocks.
        /// Returns immediately if no session is active or if already ending (via atomic check).
        /// </summary>
        public bool EndSessionSync(string endReason = "logout")
        {
            if (!IsSessionActive)
                return false;

            // Prevent double-ending: atomically exchange 0 -> 1.
            int previousState = System.Threading.Interlocked.CompareExchange(ref _isEndingSession, 1, 0);
            if (previousState != 0)
                return false; // Another thread is already ending this session

            try
            {
                // Call the synchronous (non-async) blocking method
                // This runs truly synchronously with no Task.Wait or deadlock risk
                bool success = _databaseService.EndSessionBlocking(_currentSessionId, endReason);

                if (success)
                {
                    _currentSessionId = -1;
                    _studentId = null;
                    _studentName = null;
                }

                return success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SessionManager.EndSessionSync] Error: {ex.Message}");
                return false;
            }
            finally
            {
                System.Threading.Interlocked.Exchange(ref _isEndingSession, 0);
            }
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
