using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading;
using System.Threading.Tasks;
using Zetian.Monitoring.Abstractions;
using Zetian.Monitoring.Models;

namespace Zetian.Monitoring.Exporters
{
    /// <summary>
    /// OpenTelemetry metrics and tracing exporter for SMTP server
    /// </summary>
    public class OpenTelemetryExporter : IDisposable
    {
        private readonly IMetricsCollector _collector;
        private readonly MonitoringConfiguration _configuration;
        private readonly ILogger<OpenTelemetryExporter>? _logger;
        
        private TracerProvider? _tracerProvider;
        private MeterProvider? _meterProvider;
        
        private readonly ActivitySource _activitySource;
        private readonly Meter _meter;
        
        // Metrics instruments
        private readonly Counter<long> _sessionsCounter;
        private readonly Counter<long> _messagesCounter;
        private readonly Counter<long> _bytesCounter;
        private readonly Counter<long> _errorsCounter;
        private readonly Counter<long> _commandsCounter;
        private readonly Counter<long> _authenticationsCounter;
        private readonly Counter<long> _connectionsCounter;
        private readonly Counter<long> _tlsUpgradesCounter;
        private readonly Counter<long> _rejectionsCounter;
        
        private readonly ObservableGauge<int> _activeSessionsGauge;
        private readonly ObservableGauge<double> _uptimeGauge;
        private readonly ObservableGauge<long> _memoryGauge;
        private readonly ObservableGauge<double> _throughputMessagesGauge;
        private readonly ObservableGauge<double> _throughputBytesGauge;
        
        private readonly Histogram<double> _commandDurationHistogram;
        private readonly Histogram<long> _messageSizeHistogram;
        private readonly Histogram<double> _sessionDurationHistogram;
        
        private readonly DateTime _startTime = DateTime.UtcNow;
        private bool _disposed;

        public OpenTelemetryExporter(
            IMetricsCollector collector,
            MonitoringConfiguration configuration,
            ILogger<OpenTelemetryExporter>? logger = null)
        {
            _collector = collector ?? throw new ArgumentNullException(nameof(collector));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger;
            
            // Initialize ActivitySource for tracing
            _activitySource = new ActivitySource(
                _configuration.ServiceName,
                _configuration.ServiceVersion);
            
            // Initialize Meter for metrics
            _meter = new Meter(
                _configuration.ServiceName,
                _configuration.ServiceVersion);
            
            // Create metric instruments
            _sessionsCounter = _meter.CreateCounter<long>(
                "smtp.sessions.total",
                "sessions",
                "Total number of SMTP sessions");
            
            _messagesCounter = _meter.CreateCounter<long>(
                "smtp.messages.total",
                "messages",
                "Total number of messages processed");
            
            _bytesCounter = _meter.CreateCounter<long>(
                "smtp.bytes.total",
                "bytes",
                "Total bytes processed");
            
            _errorsCounter = _meter.CreateCounter<long>(
                "smtp.errors.total",
                "errors",
                "Total number of errors");
            
            _commandsCounter = _meter.CreateCounter<long>(
                "smtp.commands.total",
                "commands",
                "Total SMTP commands executed");
            
            _authenticationsCounter = _meter.CreateCounter<long>(
                "smtp.authentications.total",
                "attempts",
                "Total authentication attempts");
            
            _connectionsCounter = _meter.CreateCounter<long>(
                "smtp.connections.total",
                "connections",
                "Total connection attempts");
            
            _tlsUpgradesCounter = _meter.CreateCounter<long>(
                "smtp.tls.upgrades.total",
                "upgrades",
                "Total TLS upgrade attempts");
            
            _rejectionsCounter = _meter.CreateCounter<long>(
                "smtp.rejections.total",
                "rejections",
                "Total message rejections");
            
            // Observable gauges
            _activeSessionsGauge = _meter.CreateObservableGauge<int>(
                "smtp.sessions.active",
                () => _collector.GetStatistics()?.ActiveSessions ?? 0,
                "sessions",
                "Current active sessions");
            
            _uptimeGauge = _meter.CreateObservableGauge<double>(
                "smtp.uptime.seconds",
                () => (DateTime.UtcNow - _startTime).TotalSeconds,
                "seconds",
                "Server uptime in seconds");
            
            _memoryGauge = _meter.CreateObservableGauge<long>(
                "smtp.memory.bytes",
                () => GC.GetTotalMemory(false),
                "bytes",
                "Memory usage in bytes");
            
            _throughputMessagesGauge = _meter.CreateObservableGauge<double>(
                "smtp.throughput.messages_per_second",
                () => _collector.GetStatistics()?.CurrentThroughput?.MessagesPerSecond ?? 0,
                "messages/sec",
                "Current message throughput");
            
            _throughputBytesGauge = _meter.CreateObservableGauge<double>(
                "smtp.throughput.bytes_per_second",
                () => _collector.GetStatistics()?.CurrentThroughput?.BytesPerSecond ?? 0,
                "bytes/sec",
                "Current bytes throughput");
            
            // Histograms
            _commandDurationHistogram = _meter.CreateHistogram<double>(
                "smtp.command.duration",
                "milliseconds",
                "Command execution duration");
            
            _messageSizeHistogram = _meter.CreateHistogram<long>(
                "smtp.message.size",
                "bytes",
                "Message size distribution");
            
            _sessionDurationHistogram = _meter.CreateHistogram<double>(
                "smtp.session.duration",
                "seconds",
                "Session duration");
            
            // Initialize OpenTelemetry if endpoint is configured
            if (!string.IsNullOrEmpty(_configuration.OpenTelemetryEndpoint))
            {
                InitializeOpenTelemetry();
            }
        }

        private void InitializeOpenTelemetry()
        {
            try
            {
                var resourceBuilder = ResourceBuilder.CreateDefault()
                    .AddService(
                        serviceName: _configuration.ServiceName,
                        serviceVersion: _configuration.ServiceVersion);
                
                // Add custom labels as resource attributes
                foreach (var label in _configuration.CustomLabels)
                {
                    resourceBuilder.AddAttributes(new KeyValuePair<string, object>[]
                    {
                        new(label.Key, label.Value)
                    });
                }
                
                var resource = resourceBuilder.Build();
                
                // Configure tracing
                _tracerProvider = Sdk.CreateTracerProviderBuilder()
                    .SetResourceBuilder(resourceBuilder)
                    .AddSource(_activitySource.Name)
                    .AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(_configuration.OpenTelemetryEndpoint!);
                        options.Protocol = OtlpExportProtocol.Grpc;
                    })
                    .Build();
                
                // Configure metrics
                _meterProvider = Sdk.CreateMeterProviderBuilder()
                    .SetResourceBuilder(resourceBuilder)
                    .AddMeter(_meter.Name)
                    .AddOtlpExporter((exporterOptions, metricReaderOptions) =>
                    {
                        exporterOptions.Endpoint = new Uri(_configuration.OpenTelemetryEndpoint!);
                        exporterOptions.Protocol = OtlpExportProtocol.Grpc;
                        metricReaderOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 
                            (int)_configuration.UpdateInterval.TotalMilliseconds;
                    })
                    .Build();
                
                _logger?.LogInformation(
                    "OpenTelemetry initialized with endpoint: {Endpoint}",
                    _configuration.OpenTelemetryEndpoint);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to initialize OpenTelemetry");
                throw;
            }
        }

        /// <summary>
        /// Creates a new activity (span) for an operation
        /// </summary>
        public Activity? StartActivity(string operationName, ActivityKind kind = ActivityKind.Server)
        {
            return _activitySource.StartActivity(operationName, kind);
        }

        /// <summary>
        /// Records a session
        /// </summary>
        public void RecordSession(double durationSeconds)
        {
            _sessionsCounter.Add(1, new KeyValuePair<string, object?>("status", "completed"));
            _sessionDurationHistogram.Record(durationSeconds);
        }

        /// <summary>
        /// Records a message
        /// </summary>
        public void RecordMessage(long size, bool delivered, string? reason = null)
        {
            var tags = new List<KeyValuePair<string, object?>>
            {
                new("status", delivered ? "delivered" : "rejected")
            };
            
            if (!delivered && reason != null)
            {
                tags.Add(new("reason", reason));
            }
            
            _messagesCounter.Add(1, tags.ToArray());
            _messageSizeHistogram.Record(size);
        }

        /// <summary>
        /// Records bytes transferred
        /// </summary>
        public void RecordBytes(long bytes, string direction)
        {
            _bytesCounter.Add(bytes, new KeyValuePair<string, object?>("direction", direction));
        }

        /// <summary>
        /// Records a command execution
        /// </summary>
        public void RecordCommand(string command, bool success, double durationMs)
        {
            _commandsCounter.Add(1, 
                new KeyValuePair<string, object?>("command", command),
                new KeyValuePair<string, object?>("status", success ? "success" : "failure"));
            
            _commandDurationHistogram.Record(durationMs, 
                new KeyValuePair<string, object?>("command", command));
        }

        /// <summary>
        /// Records an authentication attempt
        /// </summary>
        public void RecordAuthentication(bool success, string mechanism)
        {
            _authenticationsCounter.Add(1,
                new KeyValuePair<string, object?>("mechanism", mechanism),
                new KeyValuePair<string, object?>("status", success ? "success" : "failure"));
        }

        /// <summary>
        /// Records a connection
        /// </summary>
        public void RecordConnection(bool accepted)
        {
            _connectionsCounter.Add(1,
                new KeyValuePair<string, object?>("status", accepted ? "accepted" : "rejected"));
        }

        /// <summary>
        /// Records a TLS upgrade
        /// </summary>
        public void RecordTlsUpgrade(bool success)
        {
            _tlsUpgradesCounter.Add(1,
                new KeyValuePair<string, object?>("status", success ? "success" : "failure"));
        }

        /// <summary>
        /// Records a rejection
        /// </summary>
        public void RecordRejection(string reason)
        {
            _rejectionsCounter.Add(1,
                new KeyValuePair<string, object?>("reason", reason));
        }

        /// <summary>
        /// Records an error
        /// </summary>
        public void RecordError(string type)
        {
            _errorsCounter.Add(1,
                new KeyValuePair<string, object?>("type", type));
        }

        /// <summary>
        /// Traces an SMTP session
        /// </summary>
        public Activity? TraceSession(string sessionId, string? remoteEndpoint = null)
        {
            var activity = StartActivity($"smtp.session.{sessionId}");
            
            if (activity != null)
            {
                activity.SetTag("session.id", sessionId);
                if (remoteEndpoint != null)
                {
                    activity.SetTag("net.peer.name", remoteEndpoint);
                }
            }
            
            return activity;
        }

        /// <summary>
        /// Traces an SMTP command
        /// </summary>
        public Activity? TraceCommand(string command, string? sessionId = null)
        {
            var activity = StartActivity($"smtp.command.{command}", ActivityKind.Internal);
            
            if (activity != null)
            {
                activity.SetTag("smtp.command", command);
                if (sessionId != null)
                {
                    activity.SetTag("session.id", sessionId);
                }
            }
            
            return activity;
        }

        /// <summary>
        /// Traces message processing
        /// </summary>
        public Activity? TraceMessage(string messageId, string? from = null, string? to = null)
        {
            var activity = StartActivity("smtp.message.process", ActivityKind.Internal);
            
            if (activity != null)
            {
                activity.SetTag("message.id", messageId);
                if (from != null)
                {
                    activity.SetTag("message.from", from);
                }
                if (to != null)
                {
                    activity.SetTag("message.to", to);
                }
            }
            
            return activity;
        }

        public void Dispose()
        {
            if (_disposed) return;
            
            _tracerProvider?.Dispose();
            _meterProvider?.Dispose();
            _activitySource?.Dispose();
            _meter?.Dispose();
            
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}