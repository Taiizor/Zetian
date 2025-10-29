using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Zetian.Monitoring
{
    /// <summary>
    /// Configuration for monitoring
    /// </summary>
    public class MonitoringConfiguration
    {
        /// <summary>
        /// Gets or sets whether Prometheus exporter is enabled
        /// </summary>
        public bool EnablePrometheus { get; set; }

        /// <summary>
        /// Gets or sets the Prometheus metrics port
        /// </summary>
        public int? PrometheusPort { get; set; }

        /// <summary>
        /// Gets or sets the Prometheus metrics URL
        /// </summary>
        public string? PrometheusUrl { get; set; }

        /// <summary>
        /// Gets or sets whether OpenTelemetry is enabled
        /// </summary>
        public bool EnableOpenTelemetry { get; set; }

        /// <summary>
        /// Gets or sets the OpenTelemetry endpoint
        /// </summary>
        public string? OpenTelemetryEndpoint { get; set; }

        /// <summary>
        /// Gets or sets the metric update interval
        /// </summary>
        public TimeSpan UpdateInterval { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Gets or sets the logger
        /// </summary>
        public ILogger? Logger { get; set; }

        /// <summary>
        /// Gets or sets whether detailed metrics are enabled
        /// </summary>
        public bool EnableDetailedMetrics { get; set; } = true;

        /// <summary>
        /// Gets or sets whether command-level metrics are enabled
        /// </summary>
        public bool EnableCommandMetrics { get; set; } = true;

        /// <summary>
        /// Gets or sets whether throughput metrics are enabled
        /// </summary>
        public bool EnableThroughputMetrics { get; set; } = true;

        /// <summary>
        /// Gets or sets whether histogram metrics are enabled
        /// </summary>
        public bool EnableHistograms { get; set; } = true;

        /// <summary>
        /// Gets or sets custom labels for metrics
        /// </summary>
        public Dictionary<string, string> CustomLabels { get; } = [];

        /// <summary>
        /// Gets or sets the service name for OpenTelemetry
        /// </summary>
        public string ServiceName { get; set; } = "Zetian.SMTP";

        /// <summary>
        /// Gets or sets the service version for OpenTelemetry
        /// </summary>
        public string ServiceVersion { get; set; } = "1.0.0";

        /// <summary>
        /// Gets or sets histogram buckets for command duration (milliseconds)
        /// </summary>
        public double[] CommandDurationBuckets { get; set; } = [1, 5, 10, 25, 50, 100, 250, 500, 1000, 2500, 5000];

        /// <summary>
        /// Gets or sets histogram buckets for message size (bytes)
        /// </summary>
        public double[] MessageSizeBuckets { get; set; } =
            [1024, 5120, 10240, 51200, 102400, 512000, 1024000, 5120000, 10240000, 52428800, 104857600];

        /// <summary>
        /// Gets or sets Properties for additional configuration
        /// </summary>
        public Dictionary<string, object> Properties { get; } = [];
    }
}