namespace Zetian.WebUI.Services
{
    /// <summary>
    /// Service for managing SMTP sessions
    /// </summary>
    public interface ISessionManager
    {
        /// <summary>
        /// Gets all active sessions
        /// </summary>
        Task<IEnumerable<SessionInfo>> GetActiveSessionsAsync();

        /// <summary>
        /// Gets a specific session by ID
        /// </summary>
        Task<SessionInfo?> GetSessionAsync(string sessionId);

        /// <summary>
        /// Gets session history
        /// </summary>
        Task<IEnumerable<SessionInfo>> GetSessionHistoryAsync(int count = 100);

        /// <summary>
        /// Disconnects a session
        /// </summary>
        Task<bool> DisconnectSessionAsync(string sessionId);

        /// <summary>
        /// Gets session statistics
        /// </summary>
        Task<SessionStatistics> GetStatisticsAsync();
    }

    /// <summary>
    /// Session information
    /// </summary>
    public class SessionInfo
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string RemoteAddress { get; set; } = "";
        public int RemotePort { get; set; }
        public string LocalAddress { get; set; } = "";
        public int LocalPort { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan Duration => (EndTime ?? DateTime.UtcNow) - StartTime;
        public SessionState State { get; set; }
        public bool IsAuthenticated { get; set; }
        public string? AuthenticatedUser { get; set; }
        public bool IsSecure { get; set; }
        public string? TlsVersion { get; set; }
        public string? CipherSuite { get; set; }
        public int MessagesReceived { get; set; }
        public long BytesReceived { get; set; }
        public long BytesSent { get; set; }
        public int CommandsExecuted { get; set; }
        public string? LastCommand { get; set; }
        public DateTime? LastActivity { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = [];
    }

    /// <summary>
    /// Session state
    /// </summary>
    public enum SessionState
    {
        Connected,
        Authenticating,
        Authenticated,
        ReceivingData,
        Processing,
        Closing,
        Closed,
        Error
    }

    /// <summary>
    /// Session statistics
    /// </summary>
    public class SessionStatistics
    {
        public int ActiveSessions { get; set; }
        public int TotalSessions { get; set; }
        public int AuthenticatedSessions { get; set; }
        public int SecureSessions { get; set; }
        public double AverageDuration { get; set; }
        public long TotalBytesReceived { get; set; }
        public long TotalBytesSent { get; set; }
        public Dictionary<string, int> SessionsByCountry { get; set; } = [];
        public Dictionary<string, int> SessionsByClient { get; set; } = [];
    }
}