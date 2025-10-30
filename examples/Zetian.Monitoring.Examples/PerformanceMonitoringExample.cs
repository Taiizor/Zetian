using System.Net.Mail;
using Zetian.Abstractions;
using Zetian.Monitoring.Extensions;
using Zetian.Monitoring.Models;
using Zetian.Monitoring.Services;
using Zetian.Server;

namespace Zetian.Monitoring.Examples
{
    /// <summary>
    /// Example showing real-time performance monitoring
    /// </summary>
    public class PerformanceMonitoringExample
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("\n=== Real-time Performance Monitoring ===\n");

            // Create SMTP server with monitoring
            SmtpServer server = new SmtpServerBuilder()
                .Port(25)
                .ServerName("Performance Monitor")
                .Build();

            // Enable monitoring with frequent updates
            server.EnableMonitoring(builder => builder
                .WithUpdateInterval(TimeSpan.FromSeconds(1))
                .EnableDetailedMetrics()
                .EnableThroughputMetrics()
                .EnableCommandMetrics());

            await server.StartAsync();
            Console.WriteLine("✓ Server started with performance monitoring\n");

            // Start performance monitoring task
            CancellationTokenSource cts = new();
            Task monitoringTask = Task.Run(async () => await MonitorPerformance(server, cts.Token));

            // Start load simulation task
            Task loadTask = Task.Run(async () => await SimulateLoad(server, cts.Token));

            Console.WriteLine("Performance monitoring active. Press any key to stop...\n");
            Console.ReadKey();

            // Stop tasks
            cts.Cancel();
            await Task.WhenAll(monitoringTask, loadTask);

            // Final statistics
            DisplayFinalStatistics(server);

            await server.StopAsync();
            Console.WriteLine("\n✓ Server stopped");
        }

        private static async Task MonitorPerformance(ISmtpServer server, CancellationToken cancellationToken)
        {
            long lastMessageCount = 0L;
            long lastByteCount = 0L;
            DateTime lastTime = DateTime.UtcNow;

            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(2000, cancellationToken);

                MetricsCollector? collector = server.GetMetricsCollector();
                if (collector == null)
                {
                    continue;
                }

                ServerStatistics stats = collector.GetStatistics();
                DateTime currentTime = DateTime.UtcNow;
                double timeDiff = (currentTime - lastTime).TotalSeconds;

                // Calculate rates
                double messageRate = (stats.TotalMessagesReceived - lastMessageCount) / timeDiff;
                double byteRate = (stats.TotalBytesReceived - lastByteCount) / timeDiff;

                // Display performance metrics
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write($"[{currentTime:HH:mm:ss}] ");
                Console.Write($"Sessions: {stats.ActiveSessions,3}/{stats.TotalSessions,5} | ");
                Console.Write($"Msg/s: {messageRate,6:F2} | ");
                Console.Write($"KB/s: {byteRate / 1024,7:F2} | ");
                Console.Write($"Errors: {stats.TotalErrors,3} | ");
                Console.Write($"Mem: {stats.MemoryUsageBytes / (1024.0 * 1024.0),6:F1}MB");
                Console.WriteLine("     "); // Clear line remainder

                // Display throughput if available
                if (stats.CurrentThroughput != null)
                {
                    ThroughputMetrics throughput = stats.CurrentThroughput;
                    Console.WriteLine($"         Throughput - Msg: {throughput.MessagesPerSecond:F2}/s, " +
                                    $"Bytes: {throughput.BytesPerSecond:F0}/s, " +
                                    $"Cmd: {throughput.CommandsPerSecond:F2}/s");
                }

                lastMessageCount = stats.TotalMessagesReceived;
                lastByteCount = stats.TotalBytesReceived;
                lastTime = currentTime;
            }
        }

        private static async Task SimulateLoad(ISmtpServer server, CancellationToken cancellationToken)
        {
            MetricsCollector? collector = server.GetMetricsCollector();
            if (collector == null)
            {
                return;
            }

            Random random = new();
            string[] commands = new[] { "HELO", "EHLO", "AUTH", "MAIL", "RCPT", "DATA", "RSET", "QUIT" };
            string[] ipAddresses = new[] { "192.168.1.100", "192.168.1.101", "10.0.0.50", "172.16.0.25" };

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Simulate varying load
                    int delay = random.Next(10, 200);
                    await Task.Delay(delay, cancellationToken);

                    // Simulate session
                    collector.RecordSession();
                    collector.RecordConnection(ipAddresses[random.Next(ipAddresses.Length)], random.Next(10) > 0);

                    // Simulate commands with varying latencies
                    foreach (string? command in commands)
                    {
                        if (random.Next(5) == 0)
                        {
                            continue; // Skip some commands
                        }

                        int duration = command switch
                        {
                            "DATA" => random.Next(20, 200),
                            "AUTH" => random.Next(10, 50),
                            _ => random.Next(1, 20)
                        };

                        bool success = random.Next(20) > 0; // 95% success rate
                        collector.RecordCommand(command, success, duration);

                        if (!success && random.Next(3) == 0)
                        {
                            collector.RecordError(new Exception($"Simulated {command} error"));
                        }
                    }

                    // Simulate message
                    if (random.Next(3) == 0)
                    {
                        SimulatedMessage message = new()
                        {
                            Size = random.Next(1024, 102400)
                        };
                        collector.RecordMessage(message);
                    }

                    // Simulate authentication
                    if (random.Next(5) == 0)
                    {
                        collector.RecordAuthentication(random.Next(10) > 1, random.Next(2) == 0 ? "PLAIN" : "LOGIN");
                    }

                    // Simulate TLS upgrade
                    if (random.Next(10) == 0)
                    {
                        collector.RecordTlsUpgrade(random.Next(10) > 0);
                    }

                    // Complete session
                    collector.SessionCompleted();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private static void DisplayFinalStatistics(ISmtpServer server)
        {
            ServerStatistics? stats = server.GetStatistics();
            if (stats == null)
            {
                return;
            }

            Console.WriteLine("\n\n=== Final Performance Statistics ===");
            Console.WriteLine($"Total Runtime: {stats.Uptime}");
            Console.WriteLine($"Total Sessions: {stats.TotalSessions:N0}");
            Console.WriteLine($"Total Messages: {stats.TotalMessagesReceived:N0}");
            Console.WriteLine($"Total Bytes: {stats.TotalBytesReceived:N0} ({stats.TotalBytesReceived / (1024.0 * 1024.0):F2} MB)");
            Console.WriteLine($"Total Errors: {stats.TotalErrors:N0}");

            Console.WriteLine("\n--- Average Performance ---");
            double avgSessionsPerMinute = stats.TotalSessions / stats.Uptime.TotalMinutes;
            double avgMessagesPerMinute = stats.TotalMessagesReceived / stats.Uptime.TotalMinutes;
            double avgBytesPerSecond = stats.TotalBytesReceived / stats.Uptime.TotalSeconds;

            Console.WriteLine($"Sessions/min: {avgSessionsPerMinute:F2}");
            Console.WriteLine($"Messages/min: {avgMessagesPerMinute:F2}");
            Console.WriteLine($"Throughput: {avgBytesPerSecond:F0} bytes/sec");

            Console.WriteLine("\n--- Connection Statistics ---");
            Console.WriteLine($"Accept Rate: {stats.ConnectionMetrics.AcceptanceRate:F1}%");
            Console.WriteLine($"Peak Concurrent: {stats.ConnectionMetrics.PeakConcurrentConnections}");
            Console.WriteLine($"TLS Upgrades: {stats.ConnectionMetrics.TlsUpgrades}");

            if (stats.CommandMetrics.Count > 0)
            {
                Console.WriteLine("\n--- Command Performance (Top 5 by count) ---");
                IEnumerable<KeyValuePair<string, CommandMetrics>> topCommands = stats.CommandMetrics
                    .OrderByDescending(x => x.Value.TotalCount)
                    .Take(5);

                foreach (KeyValuePair<string, CommandMetrics> cmd in topCommands)
                {
                    Console.WriteLine($"{cmd.Key,-10} Count: {cmd.Value.TotalCount,6} | " +
                                    $"Avg: {cmd.Value.AverageDurationMs,6:F2}ms | " +
                                    $"Min: {cmd.Value.MinDurationMs,6:F2}ms | " +
                                    $"Max: {cmd.Value.MaxDurationMs,6:F2}ms");
                }
            }
        }

        private class SimulatedMessage : ISmtpMessage
        {
            public string Id => Guid.NewGuid().ToString();
            public MailAddress? From => new("sender@example.com");
            public IReadOnlyList<MailAddress> Recipients => new[] { new MailAddress("recipient@example.com") };
            public long Size { get; set; }
            public Stream GetRawDataStream()
            {
                return new MemoryStream();
            }

            public byte[] GetRawData()
            {
                return new byte[Size];
            }

            public Task<byte[]> GetRawDataAsync()
            {
                return Task.FromResult(GetRawData());
            }

            public IDictionary<string, string> Headers => new Dictionary<string, string>();
            public string? GetHeader(string name)
            {
                return null;
            }

            public IEnumerable<string> GetHeaders(string name)
            {
                return Enumerable.Empty<string>();
            }

            public string? Subject => "Test Message";
            public string? TextBody => "Test body";
            public string? HtmlBody => null;
            public bool HasAttachments => false;
            public int AttachmentCount => 0;
            public DateTime? Date => DateTime.UtcNow;
            public MailPriority Priority => MailPriority.Normal;
            public void SaveToFile(string path) { }
            public Task SaveToFileAsync(string path)
            {
                return Task.CompletedTask;
            }

            public void SaveToStream(Stream stream) { }
            public Task SaveToStreamAsync(Stream stream)
            {
                return Task.CompletedTask;
            }
        }
    }
}