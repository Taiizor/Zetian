using Prometheus;
using System;
using System.Collections.Generic;
using Zetian.Monitoring.Abstractions;

namespace Zetian.Monitoring.Exporters
{
    /// <summary>
    /// Prometheus metrics exporter for SMTP server
    /// </summary>
    public class PrometheusExporter : IDisposable
    {
        private readonly IMetricsCollector _collector;
        private readonly MetricServer? _metricServer;
        
        // Counters
        private readonly Counter _sessionsTotal;
        private readonly Counter _messagesTotal;
        private readonly Counter _bytesTotal;
        private readonly Counter _errorsTotal;
        private readonly Counter _commandsTotal;
        private readonly Counter _authenticationsTotal;
        private readonly Counter _connectionsTotal;
        private readonly Counter _tlsUpgradesTotal;
        private readonly Counter _rejectionsTotal;
        
        // Gauges
        private readonly Gauge _activeSessions;
        private readonly Gauge _uptime;
        private readonly Gauge _memoryUsage;
        private readonly Gauge _throughputMessages;
        private readonly Gauge _throughputBytes;
        
        // Histograms
        private readonly Histogram _commandDuration;
        private readonly Histogram _messageSize;
        
        // Summary
        private readonly Summary _sessionDuration;

        public PrometheusExporter(IMetricsCollector collector, int? port = null, string? url = null)
        {
            _collector = collector ?? throw new ArgumentNullException(nameof(collector));
            
            // Initialize Prometheus metrics
            _sessionsTotal = Metrics.CreateCounter(
                "zetian_sessions_total",
                "Total number of SMTP sessions");
            
            _messagesTotal = Metrics.CreateCounter(
                "zetian_messages_total",
                "Total number of messages processed",
                new CounterConfiguration
                {
                    LabelNames = ["status"] // delivered, rejected
                });
            
            _bytesTotal = Metrics.CreateCounter(
                "zetian_bytes_total",
                "Total bytes processed",
                new CounterConfiguration
                {
                    LabelNames = ["direction"] // in, out
                });
            
            _errorsTotal = Metrics.CreateCounter(
                "zetian_errors_total",
                "Total number of errors",
                new CounterConfiguration
                {
                    LabelNames = ["type"]
                });
            
            _commandsTotal = Metrics.CreateCounter(
                "zetian_commands_total",
                "Total SMTP commands executed",
                new CounterConfiguration
                {
                    LabelNames = ["command", "status"] // command name, success/failure
                });
            
            _authenticationsTotal = Metrics.CreateCounter(
                "zetian_authentications_total",
                "Total authentication attempts",
                new CounterConfiguration
                {
                    LabelNames = ["mechanism", "status"]
                });
            
            _connectionsTotal = Metrics.CreateCounter(
                "zetian_connections_total",
                "Total connection attempts",
                new CounterConfiguration
                {
                    LabelNames = ["status"] // accepted, rejected
                });
            
            _tlsUpgradesTotal = Metrics.CreateCounter(
                "zetian_tls_upgrades_total",
                "Total TLS upgrade attempts",
                new CounterConfiguration
                {
                    LabelNames = ["status"] // success, failure
                });
            
            _rejectionsTotal = Metrics.CreateCounter(
                "zetian_rejections_total",
                "Total message rejections",
                new CounterConfiguration
                {
                    LabelNames = ["reason"]
                });
            
            _activeSessions = Metrics.CreateGauge(
                "zetian_active_sessions",
                "Current active sessions");
            
            _uptime = Metrics.CreateGauge(
                "zetian_uptime_seconds",
                "Server uptime in seconds");
            
            _memoryUsage = Metrics.CreateGauge(
                "zetian_memory_bytes",
                "Memory usage in bytes");
            
            _throughputMessages = Metrics.CreateGauge(
                "zetian_throughput_messages_per_second",
                "Current message throughput");
            
            _throughputBytes = Metrics.CreateGauge(
                "zetian_throughput_bytes_per_second",
                "Current bytes throughput");
            
            _commandDuration = Metrics.CreateHistogram(
                "zetian_command_duration_milliseconds",
                "SMTP command execution duration",
                new HistogramConfiguration
                {
                    LabelNames = ["command"],
                    Buckets = Histogram.ExponentialBuckets(1, 2, 10) // 1ms to ~1s
                });
            
            _messageSize = Metrics.CreateHistogram(
                "zetian_message_size_bytes",
                "Message size distribution",
                new HistogramConfiguration
                {
                    Buckets = Histogram.ExponentialBuckets(1024, 2, 15) // 1KB to ~16MB
                });
            
            _sessionDuration = Metrics.CreateSummary(
                "zetian_session_duration_seconds",
                "Session duration statistics",
                new SummaryConfiguration
                {
                    MaxAge = TimeSpan.FromMinutes(5),
                    Objectives = new[]
                    {
                        new QuantileEpsilonPair(0.5, 0.05),
                        new QuantileEpsilonPair(0.9, 0.01),
                        new QuantileEpsilonPair(0.95, 0.01),
                        new QuantileEpsilonPair(0.99, 0.001)
                    }
                });
            
            // Start metric server if port specified
            if (port.HasValue)
            {
                _metricServer = new MetricServer(port.Value);
                _metricServer.Start();
            }
            else if (!string.IsNullOrEmpty(url))
            {
                _metricServer = new MetricServer(url);
                _metricServer.Start();
            }
        }

        /// <summary>
        /// Updates all Prometheus metrics from the collector
        /// </summary>
        public void UpdateMetrics()
        {
            // Update counters
            _sessionsTotal.IncTo(_collector.TotalSessions);
            _bytesTotal.WithLabels("in").IncTo(_collector.TotalBytes);
            _errorsTotal.WithLabels("general").IncTo(_collector.TotalErrors);
            
            // Update gauges
            _activeSessions.Set(_collector.ActiveSessions);
            _uptime.Set(_collector.Uptime.TotalSeconds);
            
            // Update connection metrics
            ConnectionMetrics connMetrics = _collector.ConnectionMetrics;
            _connectionsTotal.WithLabels("accepted").IncTo(connMetrics.AcceptedCount);
            _connectionsTotal.WithLabels("rejected").IncTo(connMetrics.RejectedCount);
            _tlsUpgradesTotal.WithLabels("success").IncTo(connMetrics.TlsUpgrades);
            _tlsUpgradesTotal.WithLabels("failure").IncTo(connMetrics.TlsUpgradeFailures);
            
            // Update authentication metrics
            AuthenticationMetrics authMetrics = _collector.AuthenticationMetrics;
            foreach (KeyValuePair<string, MechanismMetrics> mechanism in authMetrics.PerMechanism)
            {
                _authenticationsTotal
                    .WithLabels(mechanism.Key, "success")
                    .IncTo(mechanism.Value.Successes);
                _authenticationsTotal
                    .WithLabels(mechanism.Key, "failure")
                    .IncTo(mechanism.Value.Failures);
            }
            
            // Update command metrics
            foreach (KeyValuePair<string, CommandMetrics> cmd in _collector.CommandMetrics)
            {
                _commandsTotal
                    .WithLabels(cmd.Key, "success")
                    .IncTo(cmd.Value.SuccessCount);
                _commandsTotal
                    .WithLabels(cmd.Key, "failure")
                    .IncTo(cmd.Value.FailureCount);
                
                // Record command duration if available
                if (cmd.Value.AverageDurationMs > 0)
                {
                    _commandDuration
                        .WithLabels(cmd.Key)
                        .Observe(cmd.Value.AverageDurationMs);
                }
            }
            
            // Update rejection reasons
            foreach (KeyValuePair<string, long> rejection in _collector.RejectionReasons)
            {
                _rejectionsTotal.WithLabels(rejection.Key).IncTo(rejection.Value);
            }
            
            // Update throughput
            ThroughputMetrics throughput = _collector.GetThroughput(TimeSpan.FromMinutes(1));
            _throughputMessages.Set(throughput.MessagesPerSecond);
            _throughputBytes.Set(throughput.BytesPerSecond);
        }

        /// <summary>
        /// Records a command execution
        /// </summary>
        public void RecordCommand(string command, bool success, double durationMs)
        {
            string status = success ? "success" : "failure";
            _commandsTotal.WithLabels(command, status).Inc();
            _commandDuration.WithLabels(command).Observe(durationMs);
        }

        /// <summary>
        /// Records a message
        /// </summary>
        public void RecordMessage(long sizeBytes, bool delivered)
        {
            string status = delivered ? "delivered" : "rejected";
            _messagesTotal.WithLabels(status).Inc();
            _messageSize.Observe(sizeBytes);
            _bytesTotal.WithLabels("in").Inc(sizeBytes);
        }

        /// <summary>
        /// Records a session
        /// </summary>
        public void RecordSession(double durationSeconds)
        {
            _sessionsTotal.Inc();
            _sessionDuration.Observe(durationSeconds);
        }

        /// <summary>
        /// Records an authentication attempt
        /// </summary>
        public void RecordAuthentication(string mechanism, bool success)
        {
            string status = success ? "success" : "failure";
            _authenticationsTotal.WithLabels(mechanism, status).Inc();
        }

        /// <summary>
        /// Records a connection
        /// </summary>
        public void RecordConnection(bool accepted)
        {
            string status = accepted ? "accepted" : "rejected";
            _connectionsTotal.WithLabels(status).Inc();
        }

        /// <summary>
        /// Records a TLS upgrade
        /// </summary>
        public void RecordTlsUpgrade(bool success)
        {
            string status = success ? "success" : "failure";
            _tlsUpgradesTotal.WithLabels(status).Inc();
        }

        /// <summary>
        /// Records a rejection
        /// </summary>
        public void RecordRejection(string reason)
        {
            _rejectionsTotal.WithLabels(reason).Inc();
        }

        /// <summary>
        /// Updates active sessions gauge
        /// </summary>
        public void SetActiveSessions(int count)
        {
            _activeSessions.Set(count);
        }

        /// <summary>
        /// Updates memory usage gauge
        /// </summary>
        public void SetMemoryUsage(long bytes)
        {
            _memoryUsage.Set(bytes);
        }

        public void Dispose()
        {
            _metricServer?.Stop();
            _metricServer?.Dispose();
        }
    }
}