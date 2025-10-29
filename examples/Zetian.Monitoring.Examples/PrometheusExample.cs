using Zetian.Monitoring.Extensions;
using Zetian.Monitoring.Services;
using Zetian.Server;

namespace Zetian.Monitoring.Examples
{
    /// <summary>
    /// Example showing Prometheus metrics export
    /// </summary>
    public class PrometheusExample
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("\n=== Prometheus Metrics Export Example ===\n");

            // Create SMTP server
            SmtpServer server = new SmtpServerBuilder()
                .Port(25)
                .ServerName("Prometheus-Enabled SMTP")
                .MaxConnections(100)
                .Build();

            // Enable Prometheus metrics on port 9090
            server.EnablePrometheus(9090);

            // Start server
            await server.StartAsync();
            Console.WriteLine("✓ SMTP Server started on port 25");
            Console.WriteLine("✓ Prometheus metrics endpoint: http://localhost:9090/metrics\n");

            // Simulate some activity
            Console.WriteLine("Simulating server activity...");
            MetricsCollector? collector = server.GetMetricsCollector();
            if (collector != null)
            {
                // Simulate commands
                collector.RecordCommand("HELO", true, 5.2);
                collector.RecordCommand("AUTH", true, 12.8);
                collector.RecordCommand("MAIL", true, 3.1);
                collector.RecordCommand("RCPT", true, 2.4);
                collector.RecordCommand("DATA", true, 45.6);
                collector.RecordCommand("QUIT", true, 1.2);

                // Simulate failed commands
                collector.RecordCommand("AUTH", false, 8.3);
                collector.RecordCommand("RCPT", false, 1.9);

                // Simulate connections
                collector.RecordConnection("192.168.1.100", true);
                collector.RecordConnection("192.168.1.101", true);
                collector.RecordConnection("192.168.1.102", false); // Rejected

                // Simulate authentications
                collector.RecordAuthentication(true, "PLAIN");
                collector.RecordAuthentication(false, "PLAIN");
                collector.RecordAuthentication(true, "LOGIN");

                // Simulate TLS upgrades
                collector.RecordTlsUpgrade(true);
                collector.RecordTlsUpgrade(true);
                collector.RecordTlsUpgrade(false);
            }

            Console.WriteLine("✓ Activity simulated\n");

            // Fetch and display metrics
            try
            {
                using HttpClient client = new();
                string metricsResponse = await client.GetStringAsync("http://localhost:9090/metrics");

                Console.WriteLine("=== Sample Prometheus Metrics ===");
                string[] lines = metricsResponse.Split('\n');
                int displayCount = 0;

                foreach (string line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
                    {
                        Console.WriteLine(line);
                        displayCount++;

                        if (displayCount >= 20)
                        {
                            Console.WriteLine("... (more metrics available at http://localhost:9090/metrics)");
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not fetch metrics: {ex.Message}");
            }

            Console.WriteLine("\n=== Prometheus Scrape Configuration ===");
            Console.WriteLine("Add this to your prometheus.yml:");
            Console.WriteLine(@"
scrape_configs:
  - job_name: 'zetian-smtp'
    static_configs:
      - targets: ['localhost:9090']
        labels:
          service: 'smtp-server'
          environment: 'production'
");

            Console.WriteLine("\n=== Grafana Dashboard ===");
            Console.WriteLine("Example queries for Grafana:");
            Console.WriteLine("- rate(zetian_messages_total[5m]) - Messages per second");
            Console.WriteLine("- zetian_active_sessions - Current active sessions");
            Console.WriteLine("- rate(zetian_bytes_total[5m]) - Bytes per second");
            Console.WriteLine("- histogram_quantile(0.95, rate(zetian_command_duration_milliseconds_bucket[5m])) - P95 latency");

            Console.WriteLine("\nPress 'M' to display more metrics, or any other key to stop...");
            ConsoleKeyInfo key = Console.ReadKey();

            if (key.Key == ConsoleKey.M)
            {
                ServerStatistics? stats = server.GetStatistics();
                if (stats != null)
                {
                    Console.WriteLine($"\n\n=== Detailed Statistics ===");
                    Console.WriteLine($"Total Sessions: {stats.TotalSessions}");
                    Console.WriteLine($"Active Sessions: {stats.ActiveSessions}");
                    Console.WriteLine($"Connection Accept Rate: {stats.ConnectionMetrics.AcceptanceRate:F1}%");
                    Console.WriteLine($"Auth Success Rate: {stats.AuthenticationMetrics.SuccessRate:F1}%");
                    Console.WriteLine($"TLS Usage Rate: {stats.ConnectionMetrics.TlsUsageRate:F1}%");

                    foreach (KeyValuePair<string, CommandMetrics> cmd in stats.CommandMetrics)
                    {
                        Console.WriteLine($"\n{cmd.Key} Command:");
                        Console.WriteLine($"  Count: {cmd.Value.TotalCount}");
                        Console.WriteLine($"  Success Rate: {cmd.Value.SuccessRate:F1}%");
                        Console.WriteLine($"  Avg Duration: {cmd.Value.AverageDurationMs:F2}ms");
                    }
                }

                Console.WriteLine("\nPress any key to stop...");
                Console.ReadKey();
            }

            // Stop server
            await server.StopAsync();
            Console.WriteLine("\n✓ Server stopped");
        }
    }
}