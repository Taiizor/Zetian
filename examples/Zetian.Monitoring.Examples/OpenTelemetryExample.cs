using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Zetian.Monitoring.Extensions;
using Zetian.Monitoring.Models;
using Zetian.Protocol;
using Zetian.Server;

namespace Zetian.Monitoring.Examples
{
    /// <summary>
    /// Example demonstrating OpenTelemetry integration with Zetian SMTP Server
    /// </summary>
    public class OpenTelemetryExample
    {
        public static async Task RunAsync()
        {
            // Create a logger factory (optional)
            using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Information);
                builder.AddConsole();
            });

            ILogger<OpenTelemetryExample> logger = loggerFactory.CreateLogger<OpenTelemetryExample>();

            try
            {
                logger.LogInformation("Starting SMTP server with OpenTelemetry monitoring");

                // Create the SMTP server
                SmtpServer server = new SmtpServerBuilder()
                    .Port(25)
                    .ServerName("OpenTelemetry SMTP Server")
                    .MaxConnections(100)
                    .MaxMessageSizeMB(10)
                    .LoggerFactory(loggerFactory)
                    .Build();

                // Enable comprehensive monitoring with OpenTelemetry
                server.EnableMonitoring(builder => builder
                    // Enable OpenTelemetry with OTLP exporter
                    .EnableOpenTelemetry("http://localhost:4317")  // Default OTLP gRPC endpoint

                    // Also enable Prometheus for comparison (optional)
                    .EnablePrometheus(9090)

                    // Service identification for distributed tracing
                    .WithServiceName("smtp-production")
                    .WithServiceVersion("1.0.0")

                    // Enable all metric types
                    .EnableDetailedMetrics()
                    .EnableCommandMetrics()
                    .EnableThroughputMetrics()
                    .EnableHistograms()

                    // Faster updates for demo
                    .WithUpdateInterval(TimeSpan.FromSeconds(5))

                    // Custom labels for all metrics and traces
                    .WithLabels(
                        ("environment", "production"),
                        ("region", "us-east-1"),
                        ("instance", "smtp-01"),
                        ("datacenter", "aws"))

                    // Configure histogram buckets
                    .WithCommandDurationBuckets(1, 5, 10, 25, 50, 100, 250, 500, 1000, 2500, 5000)
                    .WithMessageSizeBuckets(1024, 10240, 102400, 1048576, 10485760, 52428800));

                // Add custom tracing for message processing
                server.MessageReceived += (sender, e) =>
                {
                    // Start a custom activity (span) for message processing
                    using Activity? activity = server.StartActivity("message.custom_processing");

                    if (activity != null)
                    {
                        // Add custom tags
                        activity.SetTag("message.id", e.Message.Id);
                        activity.SetTag("message.size", e.Message.Size);
                        activity.SetTag("sender.address", e.Message.From?.ToString());
                        activity.SetTag("recipients.count", e.Message.Recipients?.Count ?? 0);

                        // Add custom events
                        if (e.Message.Size > 5_000_000)
                        {
                            activity.AddEvent(new ActivityEvent("large_message_detected",
                                default,
                                new ActivityTagsCollection
                                {
                                    { "size_mb", e.Message.Size / 1_048_576.0 }
                                }));
                        }

                        // Simulate some processing
                        if (e.Message.From?.Address?.Contains("@spam.com") == true)
                        {
                            activity.SetTag("spam.detected", true);
                            activity.AddEvent(new ActivityEvent("spam_sender_detected"));
                            e.Cancel = true;
                            e.Response = new SmtpResponse(550, "Message rejected: spam detected");
                        }

                        // Record custom metrics
                        server.RecordMetric("CUSTOM_CHECK", success: !e.Cancel, durationMs: 5.2);
                    }

                    logger.LogInformation(
                        "Message {MessageId} from {From} - Size: {Size} bytes",
                        e.Message.Id,
                        e.Message.From,
                        e.Message.Size);
                };

                // Note: Authentication events can be tracked through session and message events
                // There's no separate AuthenticationSucceeded event in the current SMTP server implementation
                // Authentication status can be checked via session properties

                // Add custom tracing for sessions
                server.SessionCreated += (sender, e) =>
                {
                    logger.LogInformation(
                        "Session {SessionId} created from {RemoteEndPoint}",
                        e.Session.Id,
                        e.Session.RemoteEndPoint);
                };

                server.SessionCompleted += (sender, e) =>
                {
                    TimeSpan duration = DateTime.UtcNow - e.Session.StartTime;
                    logger.LogInformation(
                        "Session {SessionId} completed - Duration: {Duration:F2}s, Messages: {MessageCount}",
                        e.Session.Id,
                        duration.TotalSeconds,
                        e.Session.MessageCount);
                };

                // Start the server
                await server.StartAsync();

                Console.WriteLine("==========================================");
                Console.WriteLine("SMTP Server with OpenTelemetry Monitoring");
                Console.WriteLine("==========================================");
                Console.WriteLine($"SMTP Server: localhost:{server.Configuration.Port}");
                Console.WriteLine($"Prometheus Metrics: http://localhost:9090/metrics");
                Console.WriteLine($"OpenTelemetry Endpoint: http://localhost:4317");
                Console.WriteLine();
                Console.WriteLine("To view traces and metrics:");
                Console.WriteLine("1. Start Jaeger: docker run -p 16686:16686 -p 4317:4317 jaegertracing/all-in-one");
                Console.WriteLine("2. Open Jaeger UI: http://localhost:16686");
                Console.WriteLine("3. Or use any OTLP-compatible backend (Grafana Tempo, etc.)");
                Console.WriteLine();
                Console.WriteLine("Press 'S' for statistics, 'Q' to quit");
                Console.WriteLine("==========================================");

                // Monitor loop
                while (true)
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);

                    if (key.Key == ConsoleKey.Q)
                    {
                        break;
                    }
                    else if (key.Key == ConsoleKey.S)
                    {
                        // Display current statistics
                        ServerStatistics? stats = server.GetStatistics();
                        if (stats != null)
                        {
                            Console.WriteLine();
                            Console.WriteLine("=== Server Statistics ===");
                            Console.WriteLine($"Uptime: {stats.Uptime:dd\\.hh\\:mm\\:ss}");
                            Console.WriteLine($"Active Sessions: {stats.ActiveSessions}");
                            Console.WriteLine($"Total Sessions: {stats.TotalSessions}");
                            Console.WriteLine($"Total Messages: {stats.TotalMessagesReceived}");
                            Console.WriteLine($"  - Delivered: {stats.TotalMessagesDelivered}");
                            Console.WriteLine($"  - Rejected: {stats.TotalMessagesRejected}");
                            Console.WriteLine($"Delivery Rate: {stats.DeliveryRate:F2}%");
                            Console.WriteLine($"Messages/sec: {stats.CurrentThroughput?.MessagesPerSecond:F2}");
                            Console.WriteLine($"Bytes/sec: {stats.CurrentThroughput?.BytesPerSecond:F0}");
                            Console.WriteLine($"Memory Usage: {stats.MemoryUsage / 1_048_576:F2} MB");
                            Console.WriteLine();

                            // Show command metrics
                            if (stats.CommandMetrics.Count > 0)
                            {
                                Console.WriteLine("Command Metrics:");
                                foreach (KeyValuePair<string, CommandMetrics> cmd in stats.CommandMetrics)
                                {
                                    Console.WriteLine($"  {cmd.Key}: {cmd.Value.TotalCount} calls, " +
                                                    $"{cmd.Value.AverageDurationMs:F2}ms avg, " +
                                                    $"{cmd.Value.SuccessRate:F1}% success");
                                }
                                Console.WriteLine();
                            }
                        }
                    }
                }

                logger.LogInformation("Shutting down server");
                await server.StopAsync();

                // Cleanup OpenTelemetry resources
                server.GetOpenTelemetryExporter()?.Dispose();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Server error");
                Console.WriteLine($"Error: {ex.Message}");
            }

            Console.WriteLine("Server stopped. Press any key to exit...");
            Console.ReadKey();
        }
    }
}