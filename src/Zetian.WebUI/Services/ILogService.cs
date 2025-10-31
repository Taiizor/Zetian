using System.Runtime.CompilerServices;

namespace Zetian.WebUI.Services
{
    /// <summary>
    /// Service for log management
    /// </summary>
    public interface ILogService
    {
        /// <summary>
        /// Gets recent log entries
        /// </summary>
        Task<IEnumerable<LogEntry>> GetRecentLogsAsync(int count = 100, string? filter = null, string? level = null);

        /// <summary>
        /// Gets paged log entries
        /// </summary>
        Task<PagedResult<LogEntry>> GetPagedLogsAsync(int page, int pageSize, LogFilter? filter = null);

        /// <summary>
        /// Searches logs
        /// </summary>
        Task<IEnumerable<LogEntry>> SearchLogsAsync(string query, DateTime? from = null, DateTime? to = null);

        /// <summary>
        /// Exports logs
        /// </summary>
        Task<byte[]> ExportLogsAsync(DateTime from, DateTime to, string format = "txt");

        /// <summary>
        /// Clears old logs
        /// </summary>
        Task<int> ClearOldLogsAsync(DateTime before);

        /// <summary>
        /// Gets log statistics
        /// </summary>
        Task<LogStatistics> GetStatisticsAsync();

        /// <summary>
        /// Subscribes to real-time log updates
        /// </summary>
        IAsyncEnumerable<LogEntry> SubscribeToLogsAsync(LogFilter? filter = null, [EnumeratorCancellation] CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Log entry
    /// </summary>
    public class LogEntry
    {
        public long Id { get; set; }
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Category { get; set; } = "";
        public string Message { get; set; } = "";
        public string? SessionId { get; set; }
        public string? RemoteIp { get; set; }
        public string? User { get; set; }
        public string? Command { get; set; }
        public string? Exception { get; set; }
        public Dictionary<string, object> Properties { get; set; } = [];
    }

    /// <summary>
    /// Log level
    /// </summary>
    public enum LogLevel
    {
        Trace = 0,
        Debug = 1,
        Information = 2,
        Warning = 3,
        Error = 4,
        Critical = 5
    }

    /// <summary>
    /// Log filter
    /// </summary>
    public class LogFilter
    {
        public LogLevel? MinLevel { get; set; }
        public LogLevel? MaxLevel { get; set; }
        public string? Category { get; set; }
        public string? SessionId { get; set; }
        public string? RemoteIp { get; set; }
        public string? User { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public string? SearchText { get; set; }
        public bool IncludeExceptions { get; set; } = true;
    }

    /// <summary>
    /// Log statistics
    /// </summary>
    public class LogStatistics
    {
        public long TotalEntries { get; set; }
        public Dictionary<LogLevel, long> EntriesByLevel { get; set; } = [];
        public Dictionary<string, long> EntriesByCategory { get; set; } = [];
        public long ErrorCount { get; set; }
        public long WarningCount { get; set; }
        public DateTime OldestEntry { get; set; }
        public DateTime NewestEntry { get; set; }
        public double EntriesPerSecond { get; set; }
        public List<LogTrend> Trends { get; set; } = [];
    }

    /// <summary>
    /// Log trend data
    /// </summary>
    public class LogTrend
    {
        public DateTime Timestamp { get; set; }
        public Dictionary<LogLevel, int> CountByLevel { get; set; } = [];
    }
}