using Microsoft.Extensions.Logging;
using System;

namespace Zetian.Monitoring
{
    /// <summary>
    /// Builder for configuring monitoring
    /// </summary>
    public class MonitoringBuilder
    {
        private readonly MonitoringConfiguration _configuration = new();

        /// <summary>
        /// Enables Prometheus metrics exporter
        /// </summary>
        public MonitoringBuilder EnablePrometheus(int port = 9090)
        {
            _configuration.EnablePrometheus = true;
            _configuration.PrometheusPort = port;
            return this;
        }

        /// <summary>
        /// Enables Prometheus metrics exporter with custom URL
        /// </summary>
        public MonitoringBuilder EnablePrometheus(string url)
        {
            _configuration.EnablePrometheus = true;
            _configuration.PrometheusUrl = url;
            return this;
        }

        /// <summary>
        /// Enables OpenTelemetry exporter
        /// </summary>
        public MonitoringBuilder EnableOpenTelemetry(string endpoint)
        {
            _configuration.EnableOpenTelemetry = true;
            _configuration.OpenTelemetryEndpoint = endpoint;
            return this;
        }

        /// <summary>
        /// Sets the metric update interval
        /// </summary>
        public MonitoringBuilder WithUpdateInterval(TimeSpan interval)
        {
            _configuration.UpdateInterval = interval;
            return this;
        }

        /// <summary>
        /// Sets the logger
        /// </summary>
        public MonitoringBuilder WithLogger(ILogger logger)
        {
            _configuration.Logger = logger;
            return this;
        }

        /// <summary>
        /// Enables detailed metrics collection
        /// </summary>
        public MonitoringBuilder EnableDetailedMetrics()
        {
            _configuration.EnableDetailedMetrics = true;
            return this;
        }

        /// <summary>
        /// Enables command-level metrics
        /// </summary>
        public MonitoringBuilder EnableCommandMetrics()
        {
            _configuration.EnableCommandMetrics = true;
            return this;
        }

        /// <summary>
        /// Enables throughput metrics
        /// </summary>
        public MonitoringBuilder EnableThroughputMetrics()
        {
            _configuration.EnableThroughputMetrics = true;
            return this;
        }

        /// <summary>
        /// Sets custom labels for metrics
        /// </summary>
        public MonitoringBuilder WithLabels(params (string Key, string Value)[] labels)
        {
            foreach (var (key, value) in labels)
            {
                _configuration.CustomLabels[key] = value;
            }
            return this;
        }

        /// <summary>
        /// Sets the service name for OpenTelemetry
        /// </summary>
        public MonitoringBuilder WithServiceName(string serviceName)
        {
            _configuration.ServiceName = serviceName;
            return this;
        }

        /// <summary>
        /// Sets the service version for OpenTelemetry
        /// </summary>
        public MonitoringBuilder WithServiceVersion(string version)
        {
            _configuration.ServiceVersion = version;
            return this;
        }

        /// <summary>
        /// Enables histogram metrics
        /// </summary>
        public MonitoringBuilder EnableHistograms()
        {
            _configuration.EnableHistograms = true;
            return this;
        }

        /// <summary>
        /// Sets histogram buckets for command duration
        /// </summary>
        public MonitoringBuilder WithCommandDurationBuckets(params double[] buckets)
        {
            _configuration.CommandDurationBuckets = buckets;
            return this;
        }

        /// <summary>
        /// Sets histogram buckets for message size
        /// </summary>
        public MonitoringBuilder WithMessageSizeBuckets(params double[] buckets)
        {
            _configuration.MessageSizeBuckets = buckets;
            return this;
        }

        /// <summary>
        /// Builds the configuration
        /// </summary>
        public MonitoringConfiguration Build()
        {
            return _configuration;
        }
    }
}