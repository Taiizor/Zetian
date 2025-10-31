using Zetian.Abstractions;
using Zetian.Monitoring.Extensions;
using Zetian.Monitoring.Models;
using Zetian.Monitoring.Services;
using Zetian.Server;

namespace Zetian.Monitoring.Examples
{
    /// <summary>
    /// Basic monitoring example with Prometheus
    /// </summary>
    public class BasicMonitoringExample
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("=== Zetian SMTP Server with Monitoring ===\n");

            // Create SMTP server
            SmtpServer server = new SmtpServerBuilder()
                .Port(25)
                .ServerName("Monitored SMTP Server")
                .Build();

            // Enable monitoring with Prometheus on port 9090
            server.EnableMonitoring(builder => builder
                .EnablePrometheus("localhost", 9090)  // Specify host explicitly
                                                      // .EnablePrometheus(9090)  // Or use default host (localhost)
                                                      // .EnablePrometheus("0.0.0.0", 9090)  // Listen on all IPs (requires admin)
                .WithUpdateInterval(TimeSpan.FromSeconds(5))
                .WithServiceName("smtp-example")
                .WithServiceVersion("1.0.0")
                .EnableDetailedMetrics()
                .EnableCommandMetrics()
                .EnableThroughputMetrics()
                .WithLabels(
                    ("environment", "development"),
                    ("instance", "local")));

            // Subscribe to events to see monitoring in action
            server.SessionCreated += (sender, e) =>
            {
                Console.WriteLine($"[MONITOR] Session created: {e.Session.Id}");
            };

            server.SessionCompleted += (sender, e) =>
            {
                Console.WriteLine($"[MONITOR] Session completed: {e.Session.Id}");

                // Display current stats
                ServerStatistics? stats = server.GetStatistics();
                if (stats != null)
                {
                    Console.WriteLine($"  Total Sessions: {stats.TotalSessions}");
                    Console.WriteLine($"  Active Sessions: {stats.ActiveSessions}");
                    Console.WriteLine($"  Uptime: {stats.Uptime}");
                }
            };

            server.MessageReceived += (sender, e) =>
            {
                Console.WriteLine($"[MONITOR] Message received: {e.Message.Id}");
                Console.WriteLine($"  From: {e.Message.From}");
                Console.WriteLine($"  Size: {e.Message.Size} bytes");

                // Display throughput
                ServerStatistics? stats = server.GetStatistics();
                if (stats?.CurrentThroughput != null)
                {
                    Console.WriteLine($"  Throughput: {stats.CurrentThroughput.MessagesPerSecond:F2} msg/sec");
                }
            };

            // Start server
            await server.StartAsync();
            Console.WriteLine($"✓ SMTP Server started on port 25");
            Console.WriteLine($"✓ Prometheus metrics available at http://localhost:9090/metrics\n");

            // Display initial statistics
            DisplayStatistics(server);

            // Run monitoring loop
            Console.WriteLine("\nPress 'S' for statistics, 'R' to reset metrics, or 'Q' to quit\n");

            bool running = true;
            while (running)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                switch (key.Key)
                {
                    case ConsoleKey.S:
                        DisplayStatistics(server);
                        break;

                    case ConsoleKey.R:
                        MetricsCollector? collector = server.GetMetricsCollector();
                        collector?.Reset();
                        Console.WriteLine("✓ Metrics reset\n");
                        break;

                    case ConsoleKey.Q:
                        running = false;
                        break;
                }
            }

            // Stop server
            await server.StopAsync();
            Console.WriteLine("\n✓ Server stopped");
        }

        private static void DisplayStatistics(ISmtpServer server)
        {
            ServerStatistics? stats = server.GetStatistics();
            if (stats == null)
            {
                Console.WriteLine("No statistics available");
                return;
            }

            Console.WriteLine("\n=== Server Statistics ===");
            Console.WriteLine($"Uptime: {stats.Uptime}");
            Console.WriteLine($"Start Time: {stats.StartTime}");
            Console.WriteLine($"Last Updated: {stats.LastUpdated}");

            Console.WriteLine("\n--- Sessions ---");
            Console.WriteLine($"Total: {stats.TotalSessions}");
            Console.WriteLine($"Active: {stats.ActiveSessions}");

            Console.WriteLine("\n--- Messages ---");
            Console.WriteLine($"Received: {stats.TotalMessagesReceived}");
            Console.WriteLine($"Delivered: {stats.TotalMessagesDelivered}");
            Console.WriteLine($"Rejected: {stats.TotalMessagesRejected}");
            Console.WriteLine($"Delivery Rate: {stats.DeliveryRate:F1}%");
            Console.WriteLine($"Rejection Rate: {stats.RejectionRate:F1}%");

            Console.WriteLine("\n--- Connections ---");
            Console.WriteLine($"Attempts: {stats.ConnectionMetrics.TotalAttempts}");
            Console.WriteLine($"Accepted: {stats.ConnectionMetrics.AcceptedCount}");
            Console.WriteLine($"Rejected: {stats.ConnectionMetrics.RejectedCount}");
            Console.WriteLine($"Acceptance Rate: {stats.ConnectionMetrics.AcceptanceRate:F1}%");
            Console.WriteLine($"Active: {stats.ConnectionMetrics.ActiveConnections}");
            Console.WriteLine($"Peak Concurrent: {stats.ConnectionMetrics.PeakConcurrentConnections}");
            Console.WriteLine($"TLS Upgrades: {stats.ConnectionMetrics.TlsUpgrades}");
            Console.WriteLine($"TLS Usage: {stats.ConnectionMetrics.TlsUsageRate:F1}%");

            Console.WriteLine("\n--- Authentication ---");
            Console.WriteLine($"Attempts: {stats.AuthenticationMetrics.TotalAttempts}");
            Console.WriteLine($"Success: {stats.AuthenticationMetrics.SuccessCount}");
            Console.WriteLine($"Failure: {stats.AuthenticationMetrics.FailureCount}");
            Console.WriteLine($"Success Rate: {stats.AuthenticationMetrics.SuccessRate:F1}%");
            Console.WriteLine($"Unique Users: {stats.AuthenticationMetrics.UniqueUsers}");

            if (stats.CommandMetrics.Count > 0)
            {
                Console.WriteLine("\n--- Command Metrics ---");
                foreach (KeyValuePair<string, CommandMetrics> cmd in stats.CommandMetrics)
                {
                    Console.WriteLine($"{cmd.Key}:");
                    Console.WriteLine($"  Total: {cmd.Value.TotalCount}");
                    Console.WriteLine($"  Success Rate: {cmd.Value.SuccessRate:F1}%");
                    Console.WriteLine($"  Avg Duration: {cmd.Value.AverageDurationMs:F2}ms");
                    Console.WriteLine($"  Min/Max: {cmd.Value.MinDurationMs:F2}ms / {cmd.Value.MaxDurationMs:F2}ms");
                }
            }

            if (stats.CurrentThroughput != null)
            {
                Console.WriteLine($"\n--- Throughput (Last {stats.CurrentThroughput.Window}) ---");
                Console.WriteLine($"Messages/sec: {stats.CurrentThroughput.MessagesPerSecond:F2}");
                Console.WriteLine($"Bytes/sec: {stats.CurrentThroughput.BytesPerSecond:F0}");
                Console.WriteLine($"Connections/sec: {stats.CurrentThroughput.ConnectionsPerSecond:F2}");
                Console.WriteLine($"Commands/sec: {stats.CurrentThroughput.CommandsPerSecond:F2}");
                Console.WriteLine($"Avg Message Size: {stats.CurrentThroughput.AverageMessageSize:F0} bytes");
            }

            Console.WriteLine("\n--- System ---");
            Console.WriteLine($"Memory: {stats.MemoryUsageBytes / (1024.0 * 1024.0):F1} MB");
            Console.WriteLine($"Threads: {stats.ThreadCount}");
            Console.WriteLine($"Total Errors: {stats.TotalErrors}");
            Console.WriteLine($"Error Rate: {stats.ErrorRate:F2} per session");

            if (stats.RejectionReasons.Count > 0)
            {
                Console.WriteLine("\n--- Rejection Reasons ---");
                foreach (KeyValuePair<string, long> reason in stats.RejectionReasons)
                {
                    Console.WriteLine($"{reason.Key}: {reason.Value}");
                }
            }

            Console.WriteLine();
        }
    }
}