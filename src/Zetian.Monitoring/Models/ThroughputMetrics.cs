using System;

namespace Zetian.Monitoring.Models
{
    /// <summary>
    /// Represents throughput metrics over a time window
    /// </summary>
    public class ThroughputMetrics
    {
        /// <summary>
        /// Gets or sets the time window
        /// </summary>
        public TimeSpan Window { get; set; }

        /// <summary>
        /// Gets or sets messages per second
        /// </summary>
        public double MessagesPerSecond { get; set; }

        /// <summary>
        /// Gets or sets bytes per second
        /// </summary>
        public double BytesPerSecond { get; set; }

        /// <summary>
        /// Gets or sets connections per second
        /// </summary>
        public double ConnectionsPerSecond { get; set; }

        /// <summary>
        /// Gets or sets commands per second
        /// </summary>
        public double CommandsPerSecond { get; set; }

        /// <summary>
        /// Gets or sets total messages in window
        /// </summary>
        public long TotalMessages { get; set; }

        /// <summary>
        /// Gets or sets total bytes in window
        /// </summary>
        public long TotalBytes { get; set; }

        /// <summary>
        /// Gets or sets total connections in window
        /// </summary>
        public long TotalConnections { get; set; }

        /// <summary>
        /// Gets or sets total commands in window
        /// </summary>
        public long TotalCommands { get; set; }

        /// <summary>
        /// Gets or sets average message size in bytes
        /// </summary>
        public double AverageMessageSize => TotalMessages > 0 ? (double)TotalBytes / TotalMessages : 0;

        /// <summary>
        /// Gets or sets peak messages per second
        /// </summary>
        public double PeakMessagesPerSecond { get; set; }

        /// <summary>
        /// Gets or sets peak bytes per second
        /// </summary>
        public double PeakBytesPerSecond { get; set; }

        /// <summary>
        /// Gets or sets the calculation timestamp
        /// </summary>
        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    }
}