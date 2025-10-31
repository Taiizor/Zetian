using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Zetian.WebUI.Services
{
    /// <summary>
    /// Service for statistics and metrics
    /// </summary>
    public interface IStatisticsService
    {
        /// <summary>
        /// Gets overall statistics
        /// </summary>
        Task<OverallStatistics> GetOverallStatisticsAsync();

        /// <summary>
        /// Gets message statistics
        /// </summary>
        Task<MessageStatistics> GetMessageStatisticsAsync();

        /// <summary>
        /// Gets error statistics
        /// </summary>
        Task<ErrorStatistics> GetErrorStatisticsAsync();

        /// <summary>
        /// Gets performance statistics
        /// </summary>
        Task<PerformanceStatistics> GetPerformanceStatisticsAsync();

        /// <summary>
        /// Gets historical data
        /// </summary>
        Task<HistoricalData> GetHistoricalDataAsync(DateTime from, DateTime to);

        /// <summary>
        /// Gets Prometheus metrics
        /// </summary>
        Task<string> GetPrometheusMetricsAsync();

        /// <summary>
        /// Exports statistics
        /// </summary>
        Task<byte[]> ExportStatisticsAsync(string format = "json");
    }

    /// <summary>
    /// Overall statistics
    /// </summary>
    public class OverallStatistics
    {
        public DateTime ServerStartTime { get; set; }
        public TimeSpan Uptime { get; set; }
        public long TotalConnections { get; set; }
        public long TotalMessages { get; set; }
        public long TotalBytes { get; set; }
        public long TotalErrors { get; set; }
        public double MessagesPerSecond { get; set; }
        public double BytesPerSecond { get; set; }
        public double ErrorRate { get; set; }
        public int ActiveConnections { get; set; }
        public int QueuedMessages { get; set; }
    }

    /// <summary>
    /// Message statistics
    /// </summary>
    public class MessageStatistics
    {
        public long TotalReceived { get; set; }
        public long TotalSent { get; set; }
        public long TotalBounced { get; set; }
        public long TotalRejected { get; set; }
        public long TotalDeferred { get; set; }
        public double AverageSize { get; set; }
        public long LargestMessage { get; set; }
        public double AverageRecipients { get; set; }
        public Dictionary<string, long> MessagesByDomain { get; set; } = [];
        public Dictionary<int, long> MessagesByHour { get; set; } = [];
        public List<TimeSeriesData> MessageTimeSeries { get; set; } = [];
    }

    /// <summary>
    /// Error statistics
    /// </summary>
    public class ErrorStatistics
    {
        public long TotalErrors { get; set; }
        public Dictionary<string, long> ErrorsByType { get; set; } = [];
        public Dictionary<int, long> ErrorsByCode { get; set; } = [];
        public List<CommonError> MostCommonErrors { get; set; } = [];
        public List<TimeSeriesData> ErrorTimeSeries { get; set; } = [];
    }

    /// <summary>
    /// Performance statistics
    /// </summary>
    public class PerformanceStatistics
    {
        public double AverageResponseTime { get; set; }
        public double MedianResponseTime { get; set; }
        public double P95ResponseTime { get; set; }
        public double P99ResponseTime { get; set; }
        public double AverageSessionDuration { get; set; }
        public double AverageCommandExecutionTime { get; set; }
        public double AverageDataTransferRate { get; set; }
        public Dictionary<string, CommandPerformance> CommandPerformance { get; set; } = [];
        public List<TimeSeriesData> PerformanceTimeSeries { get; set; } = [];
    }

    /// <summary>
    /// Historical data
    /// </summary>
    public class HistoricalData
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public List<DailyStatistics> DailyStats { get; set; } = [];
        public List<HourlyStatistics> HourlyStats { get; set; } = [];
        public Dictionary<string, long> TopSenders { get; set; } = [];
        public Dictionary<string, long> TopRecipients { get; set; } = [];
        public Dictionary<string, long> TopDomains { get; set; } = [];
    }

    /// <summary>
    /// Time series data point
    /// </summary>
    public class TimeSeriesData
    {
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
        public string? Label { get; set; }
    }

    /// <summary>
    /// Common error
    /// </summary>
    public class CommonError
    {
        public string Type { get; set; } = "";
        public string Message { get; set; } = "";
        public long Count { get; set; }
        public DateTime LastOccurrence { get; set; }
    }

    /// <summary>
    /// Command performance
    /// </summary>
    public class CommandPerformance
    {
        public string Command { get; set; } = "";
        public long Count { get; set; }
        public double AverageTime { get; set; }
        public double MinTime { get; set; }
        public double MaxTime { get; set; }
        public long SuccessCount { get; set; }
        public long FailureCount { get; set; }
    }

    /// <summary>
    /// Daily statistics
    /// </summary>
    public class DailyStatistics
    {
        public DateTime Date { get; set; }
        public long Messages { get; set; }
        public long Connections { get; set; }
        public long Errors { get; set; }
        public long Bytes { get; set; }
        public double AverageResponseTime { get; set; }
    }

    /// <summary>
    /// Hourly statistics
    /// </summary>
    public class HourlyStatistics
    {
        public DateTime Hour { get; set; }
        public long Messages { get; set; }
        public long Connections { get; set; }
        public long Errors { get; set; }
        public long Bytes { get; set; }
        public double AverageResponseTime { get; set; }
    }
}