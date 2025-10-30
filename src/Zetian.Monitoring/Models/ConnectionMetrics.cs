using System;
using System.Collections.Generic;

namespace Zetian.Monitoring.Models
{
    /// <summary>
    /// Represents connection metrics
    /// </summary>
    public class ConnectionMetrics
    {
        /// <summary>
        /// Gets or sets total connection attempts
        /// </summary>
        public long TotalAttempts { get; set; }

        /// <summary>
        /// Gets or sets accepted connections
        /// </summary>
        public long AcceptedCount { get; set; }

        /// <summary>
        /// Gets or sets rejected connections
        /// </summary>
        public long RejectedCount { get; set; }

        /// <summary>
        /// Gets the acceptance rate
        /// </summary>
        public double AcceptanceRate => TotalAttempts > 0 ? (double)AcceptedCount / TotalAttempts * 100 : 0;

        /// <summary>
        /// Gets or sets current active connections
        /// </summary>
        public int ActiveConnections { get; set; }

        /// <summary>
        /// Gets or sets peak concurrent connections
        /// </summary>
        public int PeakConcurrentConnections { get; set; }

        /// <summary>
        /// Gets or sets TLS upgrade count
        /// </summary>
        public long TlsUpgrades { get; set; }

        /// <summary>
        /// Gets or sets TLS upgrade failures
        /// </summary>
        public long TlsUpgradeFailures { get; set; }

        /// <summary>
        /// Gets the TLS usage rate
        /// </summary>
        public double TlsUsageRate => AcceptedCount > 0 ? (double)TlsUpgrades / AcceptedCount * 100 : 0;

        /// <summary>
        /// Gets or sets average session duration in seconds
        /// </summary>
        public double AverageSessionDurationSeconds { get; set; }

        /// <summary>
        /// Gets or sets maximum session duration in seconds
        /// </summary>
        public double MaxSessionDurationSeconds { get; set; }

        /// <summary>
        /// Gets connection counts by IP address
        /// </summary>
        public Dictionary<string, long> ConnectionsByIp { get; } = [];

        /// <summary>
        /// Gets or sets the timestamp of the last connection
        /// </summary>
        public DateTime? LastConnectionTime { get; set; }

        /// <summary>
        /// Gets or sets rate-limited connections count
        /// </summary>
        public long RateLimitedCount { get; set; }
    }
}