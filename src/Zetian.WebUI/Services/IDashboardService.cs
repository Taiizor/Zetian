namespace Zetian.WebUI.Services
{
    /// <summary>
    /// Service for dashboard functionality
    /// </summary>
    public interface IDashboardService
    {
        /// <summary>
        /// Gets the dashboard overview
        /// </summary>
        Task<DashboardOverview> GetOverviewAsync();

        /// <summary>
        /// Gets server metrics
        /// </summary>
        Task<ServerMetrics> GetMetricsAsync();

        /// <summary>
        /// Gets recent activity
        /// </summary>
        Task<IEnumerable<ActivityItem>> GetRecentActivityAsync(int count = 50);

        /// <summary>
        /// Gets server health status
        /// </summary>
        Task<HealthStatus> GetHealthStatusAsync();

        /// <summary>
        /// Gets performance metrics for a time range
        /// </summary>
        Task<PerformanceData> GetPerformanceDataAsync(DateTime from, DateTime to);

        /// <summary>
        /// Gets top senders
        /// </summary>
        Task<IEnumerable<TopSender>> GetTopSendersAsync(int count = 10);

        /// <summary>
        /// Gets top recipients
        /// </summary>
        Task<IEnumerable<TopRecipient>> GetTopRecipientsAsync(int count = 10);

        /// <summary>
        /// Gets error summary
        /// </summary>
        Task<ErrorSummary> GetErrorSummaryAsync();
    }

    /// <summary>
    /// Dashboard overview model
    /// </summary>
    public class DashboardOverview
    {
        public ServerStatus ServerStatus { get; set; }
        public long TotalMessages { get; set; }
        public long ActiveSessions { get; set; }
        public long TotalErrors { get; set; }
        public double MessagesPerSecond { get; set; }
        public TimeSpan Uptime { get; set; }
        public double CpuUsage { get; set; }
        public long MemoryUsage { get; set; }
        public double NetworkThroughput { get; set; }
        public DateTime LastUpdate { get; set; }
    }

    /// <summary>
    /// Server status enum
    /// </summary>
    public enum ServerStatus
    {
        Stopped,
        Starting,
        Running,
        Stopping,
        Error
    }

    /// <summary>
    /// Server metrics
    /// </summary>
    public class ServerMetrics
    {
        public long MessagesReceived { get; set; }
        public long MessagesSent { get; set; }
        public long MessagesQueued { get; set; }
        public long MessagesFailed { get; set; }
        public long SessionsCreated { get; set; }
        public long SessionsCompleted { get; set; }
        public long AuthenticationAttempts { get; set; }
        public long AuthenticationSuccesses { get; set; }
        public long AuthenticationFailures { get; set; }
        public long TlsNegotiations { get; set; }
        public double AverageMessageSize { get; set; }
        public double AverageSessionDuration { get; set; }
    }

    /// <summary>
    /// Activity item
    /// </summary>
    public class ActivityItem
    {
        public DateTime Timestamp { get; set; }
        public string Type { get; set; } = "";
        public string Description { get; set; } = "";
        public string Details { get; set; } = "";
        public ActivityLevel Level { get; set; }
    }

    /// <summary>
    /// Activity level
    /// </summary>
    public enum ActivityLevel
    {
        Info,
        Warning,
        Error,
        Success
    }

    /// <summary>
    /// Health status
    /// </summary>
    public class HealthStatus
    {
        public bool IsHealthy { get; set; }
        public Dictionary<string, ComponentHealth> Components { get; set; } = [];
        public DateTime CheckTime { get; set; }
    }

    /// <summary>
    /// Component health
    /// </summary>
    public class ComponentHealth
    {
        public string Name { get; set; } = "";
        public bool IsHealthy { get; set; }
        public string Status { get; set; } = "";
        public string? Message { get; set; }
    }

    /// <summary>
    /// Performance data
    /// </summary>
    public class PerformanceData
    {
        public List<DataPoint> MessagesPerMinute { get; set; } = [];
        public List<DataPoint> SessionsPerMinute { get; set; } = [];
        public List<DataPoint> ErrorsPerMinute { get; set; } = [];
        public List<DataPoint> CpuUsage { get; set; } = [];
        public List<DataPoint> MemoryUsage { get; set; } = [];
        public List<DataPoint> NetworkThroughput { get; set; } = [];
    }

    /// <summary>
    /// Data point for charts
    /// </summary>
    public class DataPoint
    {
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
    }

    /// <summary>
    /// Top sender
    /// </summary>
    public class TopSender
    {
        public string Address { get; set; } = "";
        public long MessageCount { get; set; }
        public long TotalSize { get; set; }
        public DateTime LastSeen { get; set; }
    }

    /// <summary>
    /// Top recipient
    /// </summary>
    public class TopRecipient
    {
        public string Address { get; set; } = "";
        public long MessageCount { get; set; }
        public long TotalSize { get; set; }
        public DateTime LastSeen { get; set; }
    }

    /// <summary>
    /// Error summary
    /// </summary>
    public class ErrorSummary
    {
        public long TotalErrors { get; set; }
        public Dictionary<string, long> ErrorsByType { get; set; } = [];
        public List<RecentError> RecentErrors { get; set; } = [];
    }

    /// <summary>
    /// Recent error
    /// </summary>
    public class RecentError
    {
        public DateTime Timestamp { get; set; }
        public string Type { get; set; } = "";
        public string Message { get; set; } = "";
        public string? StackTrace { get; set; }
        public string? SessionId { get; set; }
    }
}