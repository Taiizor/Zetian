using System;
using System.Collections.Generic;

namespace Zetian.Monitoring
{
    /// <summary>
    /// Comprehensive server statistics
    /// </summary>
    public class ServerStatistics
    {
        /// <summary>
        /// Gets or sets server start time
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets server uptime
        /// </summary>
        public TimeSpan Uptime => DateTime.UtcNow - StartTime;

        /// <summary>
        /// Gets or sets total sessions
        /// </summary>
        public long TotalSessions { get; set; }

        /// <summary>
        /// Gets or sets active sessions
        /// </summary>
        public int ActiveSessions { get; set; }

        /// <summary>
        /// Gets or sets total messages received
        /// </summary>
        public long TotalMessagesReceived { get; set; }

        /// <summary>
        /// Gets or sets total messages delivered
        /// </summary>
        public long TotalMessagesDelivered { get; set; }

        /// <summary>
        /// Gets or sets total messages rejected
        /// </summary>
        public long TotalMessagesRejected { get; set; }

        /// <summary>
        /// Gets or sets total bytes received
        /// </summary>
        public long TotalBytesReceived { get; set; }

        /// <summary>
        /// Gets or sets total bytes transmitted
        /// </summary>
        public long TotalBytesTransmitted { get; set; }

        /// <summary>
        /// Gets or sets total errors
        /// </summary>
        public long TotalErrors { get; set; }

        /// <summary>
        /// Gets or sets connection metrics
        /// </summary>
        public ConnectionMetrics ConnectionMetrics { get; set; } = new();

        /// <summary>
        /// Gets or sets authentication metrics
        /// </summary>
        public AuthenticationMetrics AuthenticationMetrics { get; set; } = new();

        /// <summary>
        /// Gets command metrics
        /// </summary>
        public Dictionary<string, CommandMetrics> CommandMetrics { get; } = [];

        /// <summary>
        /// Gets rejection reasons
        /// </summary>
        public Dictionary<string, long> RejectionReasons { get; } = [];

        /// <summary>
        /// Gets or sets current throughput
        /// </summary>
        public ThroughputMetrics? CurrentThroughput { get; set; }

        /// <summary>
        /// Gets or sets memory usage in bytes
        /// </summary>
        public long MemoryUsageBytes { get; set; }

        /// <summary>
        /// Gets or sets CPU usage percentage
        /// </summary>
        public double CpuUsagePercent { get; set; }

        /// <summary>
        /// Gets or sets thread count
        /// </summary>
        public int ThreadCount { get; set; }

        /// <summary>
        /// Gets error types and counts
        /// </summary>
        public Dictionary<string, long> ErrorTypes { get; } = [];

        /// <summary>
        /// Gets or sets the timestamp of the last update
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets the message delivery rate
        /// </summary>
        public double DeliveryRate => TotalMessagesReceived > 0
            ? (double)TotalMessagesDelivered / TotalMessagesReceived * 100
            : 0;

        /// <summary>
        /// Gets the rejection rate
        /// </summary>
        public double RejectionRate => TotalMessagesReceived > 0
            ? (double)TotalMessagesRejected / TotalMessagesReceived * 100
            : 0;

        /// <summary>
        /// Gets the error rate per session
        /// </summary>
        public double ErrorRate => TotalSessions > 0
            ? (double)TotalErrors / TotalSessions
            : 0;
    }
}