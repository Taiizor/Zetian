using Zetian.Abstractions;
using Zetian.Monitoring.Extensions;
using Zetian.Monitoring.Models;
using Zetian.Monitoring.Services;
using Zetian.Server;

namespace Zetian.Monitoring.Examples
{
    /// <summary>
    /// Example showing custom metrics collection
    /// </summary>
    public class CustomMetricsExample
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("\n=== Custom Metrics Collection Example ===\n");

            // Create SMTP server
            SmtpServer server = new SmtpServerBuilder()
                .Port(25)
                .ServerName("Custom Metrics Server")
                .Build();

            // Enable monitoring with custom configuration
            server.EnableMonitoring(builder => builder
                .EnableDetailedMetrics()
                .EnableCommandMetrics()
                .WithLabels(
                    ("datacenter", "us-east-1"),
                    ("cluster", "production"),
                    ("node", "smtp-01"))
                .WithCommandDurationBuckets(1, 5, 10, 25, 50, 100, 250, 500, 1000, 2500, 5000)
                .WithMessageSizeBuckets(1024, 10240, 51200, 102400, 512000, 1048576, 5242880));

            await server.StartAsync();
            Console.WriteLine("✓ Server started with custom metrics configuration\n");

            // Demonstrate custom metric recording
            MetricsCollector? collector = server.GetMetricsCollector();
            if (collector != null)
            {
                Console.WriteLine("Recording custom metrics...\n");

                // Custom command metrics
                Console.WriteLine("1. Recording custom SMTP extension commands:");
                RecordCustomCommand(server, "XFORWARD", true, 3.5);
                RecordCustomCommand(server, "XFORWARD", false, 2.1);
                RecordCustomCommand(server, "XCLIENT", true, 4.2);
                RecordCustomCommand(server, "XAUTH", true, 15.3);
                Console.WriteLine("   ✓ Custom commands recorded\n");

                // Custom rejection reasons
                Console.WriteLine("2. Recording custom rejection reasons:");
                collector.RecordRejection("SPF_FAIL");
                collector.RecordRejection("DKIM_INVALID");
                collector.RecordRejection("DMARC_REJECT");
                collector.RecordRejection("RATE_LIMIT");
                collector.RecordRejection("BLACKLIST");
                Console.WriteLine("   ✓ Rejection reasons recorded\n");

                // Custom authentication mechanisms
                Console.WriteLine("3. Recording custom authentication mechanisms:");
                collector.RecordAuthentication(true, "OAUTH2");
                collector.RecordAuthentication(true, "KERBEROS");
                collector.RecordAuthentication(false, "CUSTOM_TOKEN");
                collector.RecordAuthenticatedUser("user@example.com", "OAUTH2");
                collector.RecordAuthenticatedUser("admin@example.com", "KERBEROS");
                Console.WriteLine("   ✓ Authentication mechanisms recorded\n");

                // Custom connection tracking
                Console.WriteLine("4. Recording connections from different sources:");
                string[] ipRanges = {
                    "192.168.1.0/24",
                    "10.0.0.0/8",
                    "172.16.0.0/12",
                    "203.0.113.0/24"
                };

                foreach (string range in ipRanges)
                {
                    string baseIp = range.Split('/')[0];
                    collector.RecordConnection(baseIp, true);
                    Console.WriteLine($"   ✓ Connection from {range}");
                }
                collector.RecordRateLimitedConnection();
                Console.WriteLine("   ✓ Rate-limited connection recorded\n");

                // Simulate business-specific metrics
                Console.WriteLine("5. Recording business-specific events:");

                // Track message categories
                Dictionary<string, int> messageCategories = new()
                {
                    { "transactional", 150 },
                    { "marketing", 75 },
                    { "notification", 200 },
                    { "system", 50 }
                };

                foreach (KeyValuePair<string, int> category in messageCategories)
                {
                    for (int i = 0; i < category.Value; i++)
                    {
                        collector.RecordCommand($"DATA_{category.Key.ToUpper()}", true, Random.Shared.Next(10, 100));
                    }
                    Console.WriteLine($"   ✓ {category.Value} {category.Key} messages");
                }
                Console.WriteLine();

                // Display collected metrics
                DisplayCustomMetrics(collector);
            }

            Console.WriteLine("\nPress any key to stop...");
            Console.ReadKey();

            await server.StopAsync();
            Console.WriteLine("\n✓ Server stopped");
        }

        private static void RecordCustomCommand(ISmtpServer server, string command, bool success, double duration)
        {
            server.RecordMetric(command, success, duration);
            Console.WriteLine($"   ✓ {command}: {(success ? "success" : "failure")} in {duration}ms");
        }

        private static void DisplayCustomMetrics(Services.MetricsCollector collector)
        {
            ServerStatistics stats = collector.GetStatistics();

            Console.WriteLine("=== Custom Metrics Summary ===\n");

            // Command metrics
            Console.WriteLine("Command Metrics:");
            foreach (KeyValuePair<string, CommandMetrics> cmd in stats.CommandMetrics.OrderBy(x => x.Key))
            {
                Console.WriteLine($"  {cmd.Key}:");
                Console.WriteLine($"    Total: {cmd.Value.TotalCount}");
                Console.WriteLine($"    Success Rate: {cmd.Value.SuccessRate:F1}%");
                Console.WriteLine($"    Avg Duration: {cmd.Value.AverageDurationMs:F2}ms");
            }

            // Rejection reasons
            if (stats.RejectionReasons.Count > 0)
            {
                Console.WriteLine("\nRejection Reasons:");
                foreach (KeyValuePair<string, long> reason in stats.RejectionReasons.OrderByDescending(x => x.Value))
                {
                    Console.WriteLine($"  {reason.Key}: {reason.Value}");
                }
            }

            // Authentication metrics
            Console.WriteLine("\nAuthentication Metrics:");
            Console.WriteLine($"  Total Attempts: {stats.AuthenticationMetrics.TotalAttempts}");
            Console.WriteLine($"  Success Rate: {stats.AuthenticationMetrics.SuccessRate:F1}%");
            Console.WriteLine($"  Unique Users: {stats.AuthenticationMetrics.UniqueUsers}");

            if (stats.AuthenticationMetrics.PerMechanism.Count > 0)
            {
                Console.WriteLine("\n  Per Mechanism:");
                foreach (KeyValuePair<string, MechanismMetrics> mech in stats.AuthenticationMetrics.PerMechanism)
                {
                    Console.WriteLine($"    {mech.Key}: {mech.Value.Attempts} attempts, " +
                                    $"{mech.Value.SuccessRate:F1}% success");
                }
            }

            // Connection metrics
            Console.WriteLine("\nConnection Metrics:");
            Console.WriteLine($"  Total Attempts: {stats.ConnectionMetrics.TotalAttempts}");
            Console.WriteLine($"  Accepted: {stats.ConnectionMetrics.AcceptedCount}");
            Console.WriteLine($"  Rejected: {stats.ConnectionMetrics.RejectedCount}");
            Console.WriteLine($"  Rate Limited: {stats.ConnectionMetrics.RateLimitedCount}");
            Console.WriteLine($"  Acceptance Rate: {stats.ConnectionMetrics.AcceptanceRate:F1}%");

            if (stats.ConnectionMetrics.ConnectionsByIp.Count > 0)
            {
                Console.WriteLine("\n  Top IPs:");
                IEnumerable<KeyValuePair<string, long>> topIps = stats.ConnectionMetrics.ConnectionsByIp
                    .OrderByDescending(x => x.Value)
                    .Take(5);

                foreach (KeyValuePair<string, long> ip in topIps)
                {
                    Console.WriteLine($"    {ip.Key}: {ip.Value} connections");
                }
            }

            // Throughput
            ThroughputMetrics throughput = collector.GetThroughput(TimeSpan.FromMinutes(1));
            if (throughput.TotalCommands > 0)
            {
                Console.WriteLine($"\nThroughput (Last Minute):");
                Console.WriteLine($"  Messages/sec: {throughput.MessagesPerSecond:F2}");
                Console.WriteLine($"  Commands/sec: {throughput.CommandsPerSecond:F2}");
                Console.WriteLine($"  Connections/sec: {throughput.ConnectionsPerSecond:F2}");
                Console.WriteLine($"  Avg Message Size: {throughput.AverageMessageSize:F0} bytes");
            }

            // Memory and system
            Console.WriteLine($"\nSystem Metrics:");
            Console.WriteLine($"  Uptime: {stats.Uptime}");
            Console.WriteLine($"  Memory Usage: {stats.MemoryUsageBytes / (1024.0 * 1024.0):F1} MB");
            Console.WriteLine($"  Thread Count: {stats.ThreadCount}");
            Console.WriteLine($"  Error Rate: {stats.ErrorRate:F2} per session");
        }
    }
}