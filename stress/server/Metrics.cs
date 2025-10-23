using Prometheus;

namespace Zetian.StressTestServer;

/// <summary>
/// Prometheus metrics for SMTP server monitoring
/// </summary>
public static class ServerMetrics
{
    // Connection metrics
    public static readonly Gauge ActiveConnections = Metrics
        .CreateGauge("smtp_active_connections", "Number of active SMTP connections");

    public static readonly Counter TotalConnections = Metrics
        .CreateCounter("smtp_total_connections", "Total number of SMTP connections");

    // Message metrics
    public static readonly Counter MessagesReceived = Metrics
        .CreateCounter("smtp_messages_received_total", "Total number of messages received");

    public static readonly Histogram MessageSize = Metrics
        .CreateHistogram("smtp_message_size_bytes", "Size of received messages in bytes",
            new HistogramConfiguration
            {
                Buckets = Histogram.ExponentialBuckets(1024, 2, 20) // 1KB to 1GB
            });

    public static readonly Histogram MessageProcessingTime = Metrics
        .CreateHistogram("smtp_message_processing_seconds", "Time to process messages",
            new HistogramConfiguration
            {
                Buckets = Histogram.ExponentialBuckets(0.001, 2, 15) // 1ms to 16s
            });


    // Error metrics
    public static readonly Counter Errors = Metrics
        .CreateCounter("smtp_errors_total", "Total errors by type",
            new CounterConfiguration
            {
                LabelNames = new[] { "error_type" }
            });

    // Resource metrics
    public static readonly Gauge MemoryUsage = Metrics
        .CreateGauge("smtp_memory_usage_bytes", "Memory usage in bytes");

    // Performance metrics
    public static readonly Summary Throughput = Metrics
        .CreateSummary("smtp_throughput_messages_per_second", "Message throughput",
            new SummaryConfiguration
            {
                MaxAge = TimeSpan.FromMinutes(1),
                Objectives = new[]
                {
                    new QuantileEpsilonPair(0.5, 0.05),
                    new QuantileEpsilonPair(0.9, 0.05),
                    new QuantileEpsilonPair(0.95, 0.01),
                    new QuantileEpsilonPair(0.99, 0.01)
                }
            });

    private static readonly System.Timers.Timer _metricsTimer = new(1000);
    private static long _lastMessageCount = 0;

    static ServerMetrics()
    {
        _metricsTimer.Elapsed += (_, _) => UpdateThroughput();
        _metricsTimer.Start();
    }

    private static void UpdateThroughput()
    {
        long currentCount = (long)MessagesReceived.Value;
        long throughput = currentCount - _lastMessageCount;
        if (throughput > 0)
        {
            Throughput.Observe(throughput);
        }
        _lastMessageCount = currentCount;

        // Update memory usage
        MemoryUsage.Set(GC.GetTotalMemory(false));
    }
}