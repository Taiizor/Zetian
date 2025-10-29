using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Zetian.Abstractions;
using Zetian.Monitoring.Abstractions;

namespace Zetian.Monitoring.Services
{
    /// <summary>
    /// Default implementation of metrics collector
    /// </summary>
    public class MetricsCollector : IMetricsCollector, IDisposable
    {
        private readonly ILogger<MetricsCollector> _logger;
        private readonly ConcurrentDictionary<string, CommandMetrics> _commandMetrics;
        private readonly ConcurrentDictionary<string, long> _rejectionReasons;
        private readonly ConcurrentDictionary<string, long> _connectionsByIp;
        private readonly ConcurrentDictionary<string, MechanismMetrics> _authMechanisms;
        private readonly ConcurrentDictionary<string, HashSet<string>> _uniqueUsers;

        private readonly DateTime _startTime;
        private readonly Timer _cleanupTimer;
        private readonly Process _currentProcess;

        private long _totalSessions;
        private long _totalMessages;
        private long _totalErrors;
        private long _totalBytes;
        private long _totalBytesTransmitted;
        private int _activeSessions;
        private int _peakConcurrentConnections;

        // Connection metrics
        private long _connectionAttempts;
        private long _connectionsAccepted;
        private long _connectionsRejected;
        private long _tlsUpgrades;
        private long _tlsUpgradeFailures;
        private long _rateLimitedConnections;

        // Authentication metrics
        private long _authAttempts;
        private long _authSuccesses;
        private long _authFailures;
        private long _bruteForceAttempts;

        // Message metrics
        private long _messagesDelivered;
        private long _messagesRejected;

        // Throughput tracking
        private readonly Queue<TimestampedMetric> _recentMessages = new();
        private readonly Queue<TimestampedMetric> _recentBytes = new();
        private readonly Queue<TimestampedMetric> _recentConnections = new();
        private readonly Queue<TimestampedMetric> _recentCommands = new();
        private readonly object _throughputLock = new();

        private bool _disposed;

        public MetricsCollector(ILogger<MetricsCollector>? logger = null)
        {
            _logger = logger ?? new NullLogger<MetricsCollector>();
            _commandMetrics = new ConcurrentDictionary<string, CommandMetrics>(StringComparer.OrdinalIgnoreCase);
            _rejectionReasons = new ConcurrentDictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            _connectionsByIp = new ConcurrentDictionary<string, long>();
            _authMechanisms = new ConcurrentDictionary<string, MechanismMetrics>(StringComparer.OrdinalIgnoreCase);
            _uniqueUsers = new ConcurrentDictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            _startTime = DateTime.UtcNow;
            _currentProcess = Process.GetCurrentProcess();

            // Cleanup old throughput data every minute
            _cleanupTimer = new Timer(CleanupOldData, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        #region IStatisticsCollector Implementation

        public long TotalSessions => _totalSessions;
        public long TotalMessages => _totalMessages;
        public long TotalErrors => _totalErrors;
        public long TotalBytes => _totalBytes;

        public void RecordSession()
        {
            Interlocked.Increment(ref _totalSessions);
            int current = Interlocked.Increment(ref _activeSessions);

            // Update peak concurrent connections
            int currentPeak = _peakConcurrentConnections;
            while (current > currentPeak)
            {
                Interlocked.CompareExchange(ref _peakConcurrentConnections, current, currentPeak);
                currentPeak = _peakConcurrentConnections;
            }
        }

        public void RecordMessage(ISmtpMessage message)
        {
            Interlocked.Increment(ref _totalMessages);
            Interlocked.Add(ref _totalBytes, message.Size);
            Interlocked.Increment(ref _messagesDelivered);

            // Track throughput
            lock (_throughputLock)
            {
                DateTime now = DateTime.UtcNow;
                _recentMessages.Enqueue(new TimestampedMetric { Timestamp = now, Value = 1 });
                _recentBytes.Enqueue(new TimestampedMetric { Timestamp = now, Value = message.Size });
            }
        }

        public void RecordError(Exception exception)
        {
            Interlocked.Increment(ref _totalErrors);
            _logger.LogError(exception, "Error recorded in metrics");
        }

        #endregion

        #region IMetricsCollector Implementation

        public int ActiveSessions => _activeSessions;
        public TimeSpan Uptime => DateTime.UtcNow - _startTime;

        public IReadOnlyDictionary<string, CommandMetrics> CommandMetrics => _commandMetrics;
        public IReadOnlyDictionary<string, long> RejectionReasons => _rejectionReasons;

        public AuthenticationMetrics AuthenticationMetrics
        {
            get
            {
                AuthenticationMetrics metrics = new()
                {
                    TotalAttempts = _authAttempts,
                    SuccessCount = _authSuccesses,
                    FailureCount = _authFailures,
                    BruteForceAttempts = _bruteForceAttempts,
                    UniqueUsers = _uniqueUsers.Count
                };

                foreach (KeyValuePair<string, MechanismMetrics> kvp in _authMechanisms)
                {
                    metrics.PerMechanism[kvp.Key] = kvp.Value;
                }

                return metrics;
            }
        }

        public ConnectionMetrics ConnectionMetrics
        {
            get
            {
                ConnectionMetrics metrics = new()
                {
                    TotalAttempts = _connectionAttempts,
                    AcceptedCount = _connectionsAccepted,
                    RejectedCount = _connectionsRejected,
                    ActiveConnections = _activeSessions,
                    PeakConcurrentConnections = _peakConcurrentConnections,
                    TlsUpgrades = _tlsUpgrades,
                    TlsUpgradeFailures = _tlsUpgradeFailures,
                    RateLimitedCount = _rateLimitedConnections,
                    LastConnectionTime = _connectionAttempts > 0 ? DateTime.UtcNow : null
                };

                foreach (KeyValuePair<string, long> kvp in _connectionsByIp)
                {
                    metrics.ConnectionsByIp[kvp.Key] = kvp.Value;
                }

                return metrics;
            }
        }

        public void RecordCommand(string command, bool success, double durationMs)
        {
            CommandMetrics metrics = _commandMetrics.GetOrAdd(command.ToUpperInvariant(),
                _ => new CommandMetrics { Command = command.ToUpperInvariant() });

            // Use lock for thread-safe updates
            lock (metrics)
            {
                metrics.TotalCount++;

                if (success)
                {
                    metrics.SuccessCount++;
                }
                else
                {
                    metrics.FailureCount++;
                }

                metrics.TotalDurationMs += durationMs;
                metrics.MinDurationMs = Math.Min(metrics.MinDurationMs, durationMs);
                metrics.MaxDurationMs = Math.Max(metrics.MaxDurationMs, durationMs);
                metrics.LastExecutionTime = DateTime.UtcNow;
            }

            // Track throughput
            lock (_throughputLock)
            {
                _recentCommands.Enqueue(new TimestampedMetric { Timestamp = DateTime.UtcNow, Value = 1 });
            }
        }

        public void RecordAuthentication(bool success, string mechanism)
        {
            Interlocked.Increment(ref _authAttempts);

            if (success)
            {
                Interlocked.Increment(ref _authSuccesses);
            }
            else
            {
                Interlocked.Increment(ref _authFailures);
            }

            // Track per-mechanism
            MechanismMetrics mechMetrics = _authMechanisms.GetOrAdd(mechanism,
                _ => new MechanismMetrics { Mechanism = mechanism });

            lock (mechMetrics)
            {
                mechMetrics.Attempts++;
                if (success)
                {
                    mechMetrics.Successes++;
                }
                else
                {
                    mechMetrics.Failures++;
                }
            }
        }

        public void RecordConnection(string ipAddress, bool accepted)
        {
            Interlocked.Increment(ref _connectionAttempts);

            if (accepted)
            {
                Interlocked.Increment(ref _connectionsAccepted);
                _connectionsByIp.AddOrUpdate(ipAddress, 1, (_, count) => count + 1);
            }
            else
            {
                Interlocked.Increment(ref _connectionsRejected);
            }

            // Track throughput
            lock (_throughputLock)
            {
                _recentConnections.Enqueue(new TimestampedMetric { Timestamp = DateTime.UtcNow, Value = 1 });
            }
        }

        public void RecordTlsUpgrade(bool success)
        {
            if (success)
            {
                Interlocked.Increment(ref _tlsUpgrades);
            }
            else
            {
                Interlocked.Increment(ref _tlsUpgradeFailures);
            }
        }

        public void RecordRejection(string reason)
        {
            Interlocked.Increment(ref _messagesRejected);
            _rejectionReasons.AddOrUpdate(reason, 1, (_, count) => count + 1);
        }

        public ThroughputMetrics GetThroughput(TimeSpan window)
        {
            lock (_throughputLock)
            {
                DateTime cutoff = DateTime.UtcNow - window;
                double windowSeconds = window.TotalSeconds;

                // Calculate metrics for the window
                long messages = CountRecentMetrics(_recentMessages, cutoff);
                long bytes = CountRecentMetrics(_recentBytes, cutoff);
                long connections = CountRecentMetrics(_recentConnections, cutoff);
                long commands = CountRecentMetrics(_recentCommands, cutoff);

                return new ThroughputMetrics
                {
                    Window = window,
                    TotalMessages = messages,
                    TotalBytes = bytes,
                    TotalConnections = connections,
                    TotalCommands = commands,
                    MessagesPerSecond = messages / windowSeconds,
                    BytesPerSecond = bytes / windowSeconds,
                    ConnectionsPerSecond = connections / windowSeconds,
                    CommandsPerSecond = commands / windowSeconds
                };
            }
        }

        public void Reset()
        {
            _commandMetrics.Clear();
            _rejectionReasons.Clear();
            _connectionsByIp.Clear();
            _authMechanisms.Clear();
            _uniqueUsers.Clear();

            Interlocked.Exchange(ref _totalSessions, 0);
            Interlocked.Exchange(ref _totalMessages, 0);
            Interlocked.Exchange(ref _totalErrors, 0);
            Interlocked.Exchange(ref _totalBytes, 0);
            Interlocked.Exchange(ref _activeSessions, 0);
            Interlocked.Exchange(ref _peakConcurrentConnections, 0);

            lock (_throughputLock)
            {
                _recentMessages.Clear();
                _recentBytes.Clear();
                _recentConnections.Clear();
                _recentCommands.Clear();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Decrements active session count
        /// </summary>
        public void SessionCompleted()
        {
            Interlocked.Decrement(ref _activeSessions);
        }

        /// <summary>
        /// Records a unique authenticated user
        /// </summary>
        public void RecordAuthenticatedUser(string username, string mechanism)
        {
            _uniqueUsers.GetOrAdd(mechanism, _ => []).Add(username);
        }

        /// <summary>
        /// Records a rate-limited connection
        /// </summary>
        public void RecordRateLimitedConnection()
        {
            Interlocked.Increment(ref _rateLimitedConnections);
        }

        /// <summary>
        /// Gets comprehensive server statistics
        /// </summary>
        public ServerStatistics GetStatistics()
        {
            _currentProcess.Refresh();

            ServerStatistics stats = new()
            {
                StartTime = _startTime,
                TotalSessions = _totalSessions,
                ActiveSessions = _activeSessions,
                TotalMessagesReceived = _totalMessages,
                TotalMessagesDelivered = _messagesDelivered,
                TotalMessagesRejected = _messagesRejected,
                TotalBytesReceived = _totalBytes,
                TotalBytesTransmitted = _totalBytesTransmitted,
                TotalErrors = _totalErrors,
                ConnectionMetrics = ConnectionMetrics,
                AuthenticationMetrics = AuthenticationMetrics,
                CurrentThroughput = GetThroughput(TimeSpan.FromMinutes(1)),
                MemoryUsageBytes = _currentProcess.WorkingSet64,
                ThreadCount = _currentProcess.Threads.Count,
                LastUpdated = DateTime.UtcNow
            };

            // Copy command metrics
            foreach (KeyValuePair<string, CommandMetrics> kvp in _commandMetrics)
            {
                stats.CommandMetrics[kvp.Key] = kvp.Value;
            }

            // Copy rejection reasons
            foreach (KeyValuePair<string, long> kvp in _rejectionReasons)
            {
                stats.RejectionReasons[kvp.Key] = kvp.Value;
            }

            return stats;
        }

        #endregion

        #region Private Methods

        private long CountRecentMetrics(Queue<TimestampedMetric> queue, DateTime cutoff)
        {
            // Remove old entries and sum remaining
            while (queue.Count > 0 && queue.Peek().Timestamp < cutoff)
            {
                queue.Dequeue();
            }

            return queue.Sum(m => m.Value);
        }

        private void CleanupOldData(object? state)
        {
            try
            {
                lock (_throughputLock)
                {
                    // Keep only last hour of data
                    DateTime cutoff = DateTime.UtcNow - TimeSpan.FromHours(1);

                    CleanupQueue(_recentMessages, cutoff);
                    CleanupQueue(_recentBytes, cutoff);
                    CleanupQueue(_recentConnections, cutoff);
                    CleanupQueue(_recentCommands, cutoff);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during metrics cleanup");
            }
        }

        private void CleanupQueue(Queue<TimestampedMetric> queue, DateTime cutoff)
        {
            while (queue.Count > 0 && queue.Peek().Timestamp < cutoff)
            {
                queue.Dequeue();
            }
        }

        #endregion

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _cleanupTimer?.Dispose();
            _currentProcess?.Dispose();
        }

        private class TimestampedMetric
        {
            public DateTime Timestamp { get; set; }
            public long Value { get; set; }
        }
    }
}