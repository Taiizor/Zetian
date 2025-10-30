using System;
using System.Collections.Generic;
using Zetian.Abstractions;
using Zetian.Monitoring.Models;

namespace Zetian.Monitoring.Abstractions
{
    /// <summary>
    /// Interface for collecting and exposing SMTP server metrics
    /// </summary>
    public interface IMetricsCollector : IStatisticsCollector
    {
        /// <summary>
        /// Records an SMTP command execution
        /// </summary>
        void RecordCommand(string command, bool success, double durationMs);

        /// <summary>
        /// Records authentication attempt
        /// </summary>
        void RecordAuthentication(bool success, string mechanism);

        /// <summary>
        /// Records connection establishment
        /// </summary>
        void RecordConnection(string ipAddress, bool accepted);

        /// <summary>
        /// Records TLS upgrade
        /// </summary>
        void RecordTlsUpgrade(bool success);

        /// <summary>
        /// Records message rejection
        /// </summary>
        void RecordRejection(string reason);

        /// <summary>
        /// Gets the current number of active sessions
        /// </summary>
        int ActiveSessions { get; }

        /// <summary>
        /// Gets server uptime
        /// </summary>
        TimeSpan Uptime { get; }

        /// <summary>
        /// Gets command statistics
        /// </summary>
        IReadOnlyDictionary<string, CommandMetrics> CommandMetrics { get; }

        /// <summary>
        /// Gets authentication statistics
        /// </summary>
        AuthenticationMetrics AuthenticationMetrics { get; }

        /// <summary>
        /// Gets connection statistics
        /// </summary>
        ConnectionMetrics ConnectionMetrics { get; }

        /// <summary>
        /// Gets rejection statistics
        /// </summary>
        IReadOnlyDictionary<string, long> RejectionReasons { get; }

        /// <summary>
        /// Gets throughput statistics
        /// </summary>
        ThroughputMetrics GetThroughput(TimeSpan window);

        /// <summary>
        /// Gets comprehensive server statistics
        /// </summary>
        ServerStatistics GetStatistics();

        /// <summary>
        /// Resets all metrics
        /// </summary>
        void Reset();
    }
}