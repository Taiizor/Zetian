using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using Zetian.Abstractions;
using Zetian.Relay.Configuration;
using Zetian.Relay.Extensions;
using Zetian.Relay.Models;
using Zetian.Relay.Services;
using Zetian.Server;

namespace Zetian.Relay.Examples
{
    /// <summary>
    /// Demonstrates failover with multiple smart hosts
    /// </summary>
    public static class FailoverExample
    {
        public static async Task RunAsync(ILoggerFactory loggerFactory)
        {
            Console.WriteLine("===========================================");
            Console.WriteLine("     Multiple Smart Hosts with Failover");
            Console.WriteLine("===========================================");
            Console.WriteLine();
            Console.WriteLine("This example demonstrates:");
            Console.WriteLine("- Multiple smart host configuration");
            Console.WriteLine("- Automatic failover to backup servers");
            Console.WriteLine("- Priority-based server selection");
            Console.WriteLine();

            // Create server with multiple smart hosts for failover
            ISmtpServer server = new SmtpServerBuilder()
                .Port(25027)
                .ServerName("failover.local")
                .LoggerFactory(loggerFactory)
                .Build()
                .EnableRelay(config =>
                {
                    // Primary smart host (highest priority)
                    config.DefaultSmartHost = new SmartHostConfiguration
                    {
                        Host = "primary.smtp.example.com",
                        Port = 587,
                        Priority = 10, // Lower value = higher priority
                        UseStartTls = true,
                        Credentials = new NetworkCredential("primary_user", "primary_pass"),
                        MaxConnections = 5,
                        Enabled = true
                    };

                    // Backup smart host 1
                    config.SmartHosts.Add(new SmartHostConfiguration
                    {
                        Host = "backup1.smtp.example.com",
                        Port = 587,
                        Priority = 20, // Will be used if primary fails
                        UseStartTls = true,
                        Credentials = new NetworkCredential("backup1_user", "backup1_pass"),
                        MaxConnections = 3,
                        Enabled = true
                    });

                    // Backup smart host 2
                    config.SmartHosts.Add(new SmartHostConfiguration
                    {
                        Host = "backup2.smtp.example.com",
                        Port = 25,
                        Priority = 30, // Last resort
                        UseStartTls = false,
                        MaxConnections = 2,
                        Enabled = true
                    });

                    // Failover settings
                    config.MaxConcurrentDeliveries = 10;
                    config.MaxRetryCount = 10;
                    config.ConnectionTimeout = TimeSpan.FromSeconds(30);
                    config.MessageLifetime = TimeSpan.FromDays(3);
                    config.QueueProcessingInterval = TimeSpan.FromSeconds(10);
                });

            // Add another smart host via extension method
            server.AddSmartHost("emergency.smtp.example.com", 2525);

            // Log relay events
            server.MessageReceived += async (sender, e) =>
            {
                Console.WriteLine($"[RELAY] Message queued from {e.Message.From?.Address}");
                Console.WriteLine("[RELAY] Smart hosts available:");
                Console.WriteLine("  1. primary.smtp.example.com:587 (Priority: 10)");
                Console.WriteLine("  2. backup1.smtp.example.com:587 (Priority: 20)");
                Console.WriteLine("  3. backup2.smtp.example.com:25 (Priority: 30)");
                Console.WriteLine("  4. emergency.smtp.example.com:2525");

                await Task.CompletedTask;
            };

            // Start server
            Console.WriteLine("[INFO] Starting SMTP server with failover configuration...");
            RelayService relayService = await server.StartWithRelayAsync();
            Console.WriteLine("[INFO] Server started on port 25027");
            Console.WriteLine();

            // Simulate sending messages
            Console.WriteLine("[TEST] Sending test messages to demonstrate failover...");

            using SmtpClient client = new("localhost", 25027)
            {
                EnableSsl = false
            };

            // Send multiple messages to test failover
            for (int i = 1; i <= 3; i++)
            {
                MailMessage message = new()
                {
                    From = new MailAddress($"sender{i}@example.com"),
                    Subject = $"Failover Test Message #{i}",
                    Body = $@"This is test message #{i} for failover demonstration.

The relay service will attempt to deliver this message using:
1. Primary smart host first
2. If primary fails, backup1 will be tried
3. If backup1 fails, backup2 will be tried
4. If all fail, the message will be retried later

Message ID: {Guid.NewGuid()}
Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                    IsBodyHtml = false
                };
                message.To.Add($"recipient{i}@test.com");

                try
                {
                    await client.SendMailAsync(message);
                    Console.WriteLine($"[SUCCESS] Message #{i} queued");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Failed to queue message #{i}: {ex.Message}");
                }

                await Task.Delay(500);
            }

            Console.WriteLine();
            Console.WriteLine("[INFO] Messages queued. The relay service will attempt delivery.");
            Console.WriteLine("[INFO] In a real scenario, if primary server is down,");
            Console.WriteLine("       messages will automatically failover to backup servers.");
            Console.WriteLine();

            // Monitor relay progress
            Console.WriteLine("[MONITOR] Watching relay progress...");
            Console.WriteLine("[INFO] Press 'S' for statistics, 'Q' to quit");

            Task monitorTask = Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(5000);
                    RelayQueueStatistics? stats = await server.GetRelayStatisticsAsync();
                    if (stats != null && stats.TotalMessages > 0)
                    {
                        Console.WriteLine($"\n[AUTO-UPDATE] {DateTime.Now:HH:mm:ss}");
                        Console.WriteLine($"  Total: {stats.TotalMessages}, Queued: {stats.QueuedMessages}, " +
                                        $"Progress: {stats.InProgressMessages}, Delivered: {stats.DeliveredMessages}, " +
                                        $"Failed: {stats.FailedMessages}, Deferred: {stats.DeferredMessages}");

                        // Show retry information
                        if (stats.AverageRetryCount > 0)
                        {
                            Console.WriteLine($"  Average Retries: {stats.AverageRetryCount:F1}");
                            Console.WriteLine("  (Higher retry count indicates failover occurred)");
                        }

                        // Show smart host distribution
                        if (stats.MessagesBySmartHost != null && stats.MessagesBySmartHost.Count > 0)
                        {
                            Console.WriteLine("  Distribution by Smart Host:");
                            foreach (KeyValuePair<string, int> kvp in stats.MessagesBySmartHost)
                            {
                                Console.WriteLine($"    {kvp.Key}: {kvp.Value} messages");
                            }
                        }
                    }
                }
            });

            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Q)
                {
                    break;
                }

                if (key.Key == ConsoleKey.S)
                {
                    RelayQueueStatistics? stats = await server.GetRelayStatisticsAsync();
                    if (stats != null)
                    {
                        Console.WriteLine($"\n[STATISTICS] {DateTime.Now:HH:mm:ss}");
                        Console.WriteLine($"  Total Messages: {stats.TotalMessages}");
                        Console.WriteLine($"  Queued: {stats.QueuedMessages}");
                        Console.WriteLine($"  In Progress: {stats.InProgressMessages}");
                        Console.WriteLine($"  Delivered: {stats.DeliveredMessages}");
                        Console.WriteLine($"  Failed: {stats.FailedMessages}");
                        Console.WriteLine($"  Deferred: {stats.DeferredMessages}");
                        Console.WriteLine($"  Expired: {stats.ExpiredMessages}");

                        if (stats.OldestMessageTime.HasValue)
                        {
                            Console.WriteLine($"  Oldest Message: {DateTime.Now - stats.OldestMessageTime.Value:hh\\:mm\\:ss} ago");
                        }

                        if (stats.AverageRetryCount > 0)
                        {
                            Console.WriteLine($"  Avg Retry Count: {stats.AverageRetryCount:F2}");
                        }
                    }
                }
            }

            // Stop server
            Console.WriteLine();
            Console.WriteLine("[INFO] Stopping server...");
            await server.StopAsync();
            Console.WriteLine("[INFO] Server stopped.");
        }
    }
}