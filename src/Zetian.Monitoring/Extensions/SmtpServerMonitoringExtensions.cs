using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Zetian.Abstractions;
using Zetian.Monitoring.Exporters;
using Zetian.Monitoring.Models;
using Zetian.Monitoring.Services;
using Zetian.Server;

namespace Zetian.Monitoring.Extensions
{
    /// <summary>
    /// Extension methods for adding monitoring to SMTP server
    /// </summary>
    public static class SmtpServerMonitoringExtensions
    {
        /// <summary>
        /// Enables monitoring with default settings
        /// </summary>
        public static ISmtpServer EnableMonitoring(this ISmtpServer server)
        {
            return server.EnableMonitoring(builder => { });
        }

        /// <summary>
        /// Enables monitoring with custom configuration
        /// </summary>
        public static ISmtpServer EnableMonitoring(
            this ISmtpServer server,
            Action<MonitoringBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(server);
            ArgumentNullException.ThrowIfNull(configure);

            MonitoringBuilder builder = new();
            configure(builder);
            MonitoringConfiguration config = builder.Build();

            // Create metrics collector
            ILogger<MetricsCollector>? logger = config.Logger as ILogger<MetricsCollector>;
            MetricsCollector collector = new(logger);

            // Create Prometheus exporter if enabled
            PrometheusExporter? prometheusExporter = null;
            if (config.EnablePrometheus)
            {
                prometheusExporter = new PrometheusExporter(
                    collector,
                    config.PrometheusPort,
                    config.PrometheusHost,
                    config.PrometheusUrl);
            }

            // Create OpenTelemetry exporter if enabled
            OpenTelemetryExporter? openTelemetryExporter = null;
            if (config.EnableOpenTelemetry && !string.IsNullOrEmpty(config.OpenTelemetryEndpoint))
            {
                ILogger<OpenTelemetryExporter>? otLogger = config.Logger as ILogger<OpenTelemetryExporter>;
                openTelemetryExporter = new OpenTelemetryExporter(collector, config, otLogger);
            }

            // Wire up event handlers
            server.SessionCreated += (sender, e) =>
            {
                collector.RecordSession();
                collector.RecordConnection(
                    e.Session.RemoteEndPoint?.ToString() ?? "unknown",
                    true);

                if (prometheusExporter != null)
                {
                    prometheusExporter.RecordConnection(true);
                    prometheusExporter.SetActiveSessions(collector.ActiveSessions);
                }

                // Start OpenTelemetry trace for session
                if (openTelemetryExporter != null)
                {
                    Activity? activity = openTelemetryExporter.TraceSession(
                        e.Session.Id,
                        e.Session.RemoteEndPoint?.ToString());

                    // Store activity in server properties using session ID as key
                    if (activity != null && server is SmtpServer smtpSvr)
                    {
                        smtpSvr.Configuration.Properties[$"OTelActivity_{e.Session.Id}"] = activity;
                    }

                    openTelemetryExporter.RecordConnection(true);
                }
            };

            server.SessionCompleted += (sender, e) =>
            {
                collector.SessionCompleted();
                double duration = (DateTime.UtcNow - e.Session.StartTime).TotalSeconds;

                if (prometheusExporter != null)
                {
                    prometheusExporter.RecordSession(duration);
                    prometheusExporter.SetActiveSessions(collector.ActiveSessions);
                }

                if (openTelemetryExporter != null)
                {
                    // Complete session trace
                    if (server is SmtpServer smtpSvr &&
                        smtpSvr.Configuration.Properties.TryGetValue($"OTelActivity_{e.Session.Id}", out object? activityObj) &&
                        activityObj is Activity activity)
                    {
                        activity.SetTag("session.duration", duration);
                        activity.SetTag("session.messages", e.Session.MessageCount);
                        activity.Dispose();

                        // Clean up the stored activity
                        smtpSvr.Configuration.Properties.Remove($"OTelActivity_{e.Session.Id}");
                    }

                    openTelemetryExporter.RecordSession(duration);
                }
            };

            server.MessageReceived += (sender, e) =>
            {
                if (e.Cancel)
                {
                    collector.RecordRejection(e.Response?.Message ?? "Unknown");
                    prometheusExporter?.RecordMessage(e.Message.Size, false);
                    prometheusExporter?.RecordRejection(e.Response?.Message ?? "Unknown");
                    openTelemetryExporter?.RecordMessage(e.Message.Size, false, e.Response?.Message ?? "Unknown");
                    openTelemetryExporter?.RecordRejection(e.Response?.Message ?? "Unknown");
                }
                else
                {
                    collector.RecordMessage(e.Message);
                    prometheusExporter?.RecordMessage(e.Message.Size, true);

                    // Trace message processing
                    if (openTelemetryExporter != null)
                    {
                        using Activity? activity = openTelemetryExporter.TraceMessage(
                            e.Message.Id,
                            e.Message.From?.ToString(),
                            e.Message.Recipients?.FirstOrDefault()?.ToString());

                        openTelemetryExporter.RecordMessage(e.Message.Size, true);
                    }
                }
            };

            server.ErrorOccurred += (sender, e) =>
            {
                collector.RecordError(e.Exception);
                openTelemetryExporter?.RecordError(e.Exception.GetType().Name);
            };

            // Store collector and exporters in server properties for later access
            if (server is SmtpServer smtpServer)
            {
                smtpServer.Configuration.Properties["MetricsCollector"] = collector;
                smtpServer.Configuration.Properties["PrometheusExporter"] = prometheusExporter;
                smtpServer.Configuration.Properties["OpenTelemetryExporter"] = openTelemetryExporter;
            }

            // Set up periodic metric updates if Prometheus is enabled
            if (prometheusExporter != null && config.UpdateInterval > TimeSpan.Zero)
            {
                _ = Task.Run(async () =>
                {
                    while (server.IsRunning)
                    {
                        try
                        {
                            prometheusExporter.UpdateMetrics();
                            await Task.Delay(config.UpdateInterval);
                        }
                        catch (Exception ex)
                        {
                            config.Logger?.LogError(ex, "Error updating Prometheus metrics");
                        }
                    }
                });
            }

            return server;
        }

        /// <summary>
        /// Enables Prometheus metrics exporter with default host (localhost)
        /// </summary>
        public static ISmtpServer EnablePrometheus(
            this ISmtpServer server,
            int port = 9090)
        {
            return server.EnableMonitoring(builder => builder
                .EnablePrometheus(port));
        }

        /// <summary>
        /// Enables Prometheus metrics exporter with custom host and port
        /// </summary>
        public static ISmtpServer EnablePrometheus(
            this ISmtpServer server,
            string host,
            int port = 9090)
        {
            return server.EnableMonitoring(builder => builder
                .EnablePrometheus(host, port));
        }

        /// <summary>
        /// Gets the metrics collector if monitoring is enabled
        /// </summary>
        public static MetricsCollector? GetMetricsCollector(this ISmtpServer server)
        {
            if (server is SmtpServer smtpServer &&
                smtpServer.Configuration.Properties.TryGetValue("MetricsCollector", out object? value))
            {
                return value as MetricsCollector;
            }
            return null;
        }

        /// <summary>
        /// Gets current server statistics
        /// </summary>
        public static ServerStatistics? GetStatistics(this ISmtpServer server)
        {
            return server.GetMetricsCollector()?.GetStatistics();
        }

        /// <summary>
        /// Records a custom metric
        /// </summary>
        public static void RecordMetric(
            this ISmtpServer server,
            string command,
            bool success,
            double durationMs)
        {
            MetricsCollector? collector = server.GetMetricsCollector();
            collector?.RecordCommand(command, success, durationMs);

            // Also update Prometheus if available
            if (server is SmtpServer smtpServer &&
                smtpServer.Configuration.Properties.TryGetValue("PrometheusExporter", out object? value) &&
                value is PrometheusExporter exporter)
            {
                exporter.RecordCommand(command, success, durationMs);
            }

            // Also update OpenTelemetry if available
            if (server is SmtpServer smtpSvr &&
                smtpSvr.Configuration.Properties.TryGetValue("OpenTelemetryExporter", out object? otValue) &&
                otValue is OpenTelemetryExporter otExporter)
            {
                using Activity? activity = otExporter.TraceCommand(command);
                otExporter.RecordCommand(command, success, durationMs);
            }
        }

        /// <summary>
        /// Enables OpenTelemetry exporter
        /// </summary>
        public static ISmtpServer EnableOpenTelemetry(
            this ISmtpServer server,
            string endpoint)
        {
            return server.EnableMonitoring(builder => builder
                .EnableOpenTelemetry(endpoint));
        }

        /// <summary>
        /// Gets the OpenTelemetry exporter if monitoring is enabled
        /// </summary>
        public static OpenTelemetryExporter? GetOpenTelemetryExporter(this ISmtpServer server)
        {
            if (server is SmtpServer smtpServer &&
                smtpServer.Configuration.Properties.TryGetValue("OpenTelemetryExporter", out object? value))
            {
                return value as OpenTelemetryExporter;
            }
            return null;
        }

        /// <summary>
        /// Starts a new activity (trace span) for custom operations
        /// </summary>
        public static Activity? StartActivity(
            this ISmtpServer server,
            string operationName,
            ActivityKind kind = ActivityKind.Internal)
        {
            return server.GetOpenTelemetryExporter()?.StartActivity(operationName, kind);
        }
    }
}