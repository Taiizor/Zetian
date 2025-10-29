using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Zetian.Abstractions;
using Zetian.Monitoring.Exporters;
using Zetian.Monitoring.Services;

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
            MetricsCollector collector = new(config.Logger);

            // Create Prometheus exporter if enabled
            PrometheusExporter? prometheusExporter = null;
            if (config.EnablePrometheus)
            {
                prometheusExporter = new PrometheusExporter(
                    collector,
                    config.PrometheusPort,
                    config.PrometheusUrl);
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
            };

            server.SessionCompleted += (sender, e) =>
            {
                collector.SessionCompleted();
                
                if (prometheusExporter != null)
                {
                    double duration = (DateTime.UtcNow - e.Session.StartTime).TotalSeconds;
                    prometheusExporter.RecordSession(duration);
                    prometheusExporter.SetActiveSessions(collector.ActiveSessions);
                }
            };

            server.MessageReceived += (sender, e) =>
            {
                if (e.Cancel)
                {
                    collector.RecordRejection(e.Response?.Description ?? "Unknown");
                    prometheusExporter?.RecordMessage(e.Message.Size, false);
                    prometheusExporter?.RecordRejection(e.Response?.Description ?? "Unknown");
                }
                else
                {
                    collector.RecordMessage(e.Message);
                    prometheusExporter?.RecordMessage(e.Message.Size, true);
                }
            };

            server.ErrorOccurred += (sender, e) =>
            {
                collector.RecordError(e.Exception);
            };

            // Store collector in server properties for later access
            if (server is Server.SmtpServer smtpServer)
            {
                smtpServer.Configuration.Properties["MetricsCollector"] = collector;
                smtpServer.Configuration.Properties["PrometheusExporter"] = prometheusExporter;
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
        /// Enables Prometheus metrics exporter
        /// </summary>
        public static ISmtpServer EnablePrometheus(
            this ISmtpServer server,
            int port = 9090)
        {
            return server.EnableMonitoring(builder => builder
                .EnablePrometheus(port));
        }

        /// <summary>
        /// Gets the metrics collector if monitoring is enabled
        /// </summary>
        public static MetricsCollector? GetMetricsCollector(this ISmtpServer server)
        {
            if (server is Server.SmtpServer smtpServer &&
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
            if (server is Server.SmtpServer smtpServer &&
                smtpServer.Configuration.Properties.TryGetValue("PrometheusExporter", out object? value) &&
                value is PrometheusExporter exporter)
            {
                exporter.RecordCommand(command, success, durationMs);
            }
        }
    }
}