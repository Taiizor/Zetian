using Microsoft.Extensions.Logging;
using System.Net.Mail;
using Zetian.Abstractions;
using Zetian.Relay.Abstractions;
using Zetian.Relay.Enums;
using Zetian.Relay.Extensions;
using Zetian.Relay.Services;
using Zetian.Server;

namespace Zetian.Relay.Examples
{
    /// <summary>
    /// Demonstrates priority-based message queuing
    /// </summary>
    public static class PriorityQueueExample
    {
        public static async Task RunAsync(ILoggerFactory loggerFactory)
        {
            Console.WriteLine("===========================================");
            Console.WriteLine("       Priority Queue Example");
            Console.WriteLine("===========================================");
            Console.WriteLine();
            Console.WriteLine("This example demonstrates:");
            Console.WriteLine("- Priority-based message queuing");
            Console.WriteLine("- Different priority levels (Urgent, High, Normal, Low)");
            Console.WriteLine("- Priority-based delivery order");
            Console.WriteLine();

            // Create server with priority queue enabled
            ISmtpServer server = new SmtpServerBuilder()
                .Port(25031)
                .ServerName("priority.local")
                .LoggerFactory(loggerFactory)
                .Build()
                .EnableRelay(config =>
                {
                    config.MaxConcurrentDeliveries = 2; // Low concurrency to see priority effect
                    config.QueueProcessingInterval = TimeSpan.FromSeconds(2);

                    // Use non-existent host to keep messages in queue
                    config.DefaultSmartHost = new()
                    {
                        Host = "priority.demo.smtp.com",
                        Port = 25,
                        ConnectionTimeout = TimeSpan.FromSeconds(3)
                    };
                });

            // Start server
            Console.WriteLine("[INFO] Starting SMTP server with priority queue...");
            RelayService relayService = await server.StartWithRelayAsync();
            Console.WriteLine("[INFO] Server started on port 25031");
            Console.WriteLine();

            // Send messages with different priorities
            Console.WriteLine("[TEST] Sending messages with different priorities...");
            Console.WriteLine();

            using SmtpClient client = new("localhost", 25031)
            {
                EnableSsl = false
            };

            // Define test messages with various priorities
            (string, MailPriority, RelayPriority, string)[] testMessages = new[]
            {
                ("Low Priority Task 1", MailPriority.Low, RelayPriority.Low, "routine@example.com"),
                ("Normal Priority Task 1", MailPriority.Normal, RelayPriority.Normal, "standard@example.com"),
                ("High Priority Task 1", MailPriority.High, RelayPriority.High, "important@example.com"),
                ("Low Priority Task 2", MailPriority.Low, RelayPriority.Low, "batch@example.com"),
                ("URGENT: Critical Issue", MailPriority.High, RelayPriority.Urgent, "critical@example.com"),
                ("Normal Priority Task 2", MailPriority.Normal, RelayPriority.Normal, "regular@example.com"),
                ("High Priority Task 2", MailPriority.High, RelayPriority.High, "priority@example.com"),
                ("Low Priority Task 3", MailPriority.Low, RelayPriority.Low, "background@example.com"),
                ("URGENT: Security Alert", MailPriority.High, RelayPriority.Urgent, "security@example.com"),
                ("Normal Priority Task 3", MailPriority.Normal, RelayPriority.Normal, "daily@example.com"),
            };

            // Send all messages
            foreach ((string? subject, MailPriority mailPriority, RelayPriority relayPriority, string? to) in testMessages)
            {
                MailMessage message = new()
                {
                    From = new MailAddress("sender@priority-queue.local"),
                    Subject = subject,
                    Body = $@"Priority Test Message
Subject: {subject}
Mail Priority: {mailPriority}
Relay Priority: {relayPriority}
Timestamp: {DateTime.Now:HH:mm:ss.fff}

This message demonstrates priority-based queuing.
Higher priority messages should be processed first.",
                    Priority = mailPriority,
                    IsBodyHtml = false
                };
                message.To.Add(to);

                // Set custom header for relay priority
                if (relayPriority == RelayPriority.Urgent)
                {
                    message.Headers.Add("X-Priority", "1");
                }
                else if (relayPriority == RelayPriority.High)
                {
                    message.Headers.Add("X-Priority", "2");
                }
                else if (relayPriority == RelayPriority.Low)
                {
                    message.Headers.Add("X-Priority", "4");
                }

                try
                {
                    await client.SendMailAsync(message);

                    // Color-coded output based on priority
                    ConsoleColor originalColor = Console.ForegroundColor;
                    Console.ForegroundColor = relayPriority switch
                    {
                        RelayPriority.Urgent => ConsoleColor.Red,
                        RelayPriority.High => ConsoleColor.Yellow,
                        RelayPriority.Normal => ConsoleColor.White,
                        RelayPriority.Low => ConsoleColor.Gray,
                        _ => ConsoleColor.White
                    };

                    Console.WriteLine($"  [{relayPriority,-6}] ✓ {subject}");
                    Console.ForegroundColor = originalColor;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  [ERROR] ✗ {subject}: {ex.Message}");
                }

                await Task.Delay(100); // Small delay between sends
            }

            Console.WriteLine();
            Console.WriteLine("[INFO] All messages queued. Observing priority-based processing...");
            Console.WriteLine("[INFO] Messages should be processed in this order:");
            Console.WriteLine("  1. URGENT messages (Red)");
            Console.WriteLine("  2. HIGH priority messages (Yellow)");
            Console.WriteLine("  3. NORMAL priority messages (White)");
            Console.WriteLine("  4. LOW priority messages (Gray)");
            Console.WriteLine();

            // Monitor queue processing
            RelayService? relay = server.GetRelayService();
            if (relay?.Queue != null)
            {
                Console.WriteLine("[MONITOR] Queue Processing Order:");
                Console.WriteLine("Time     | Priority | Status    | Subject");
                Console.WriteLine("---------|----------|-----------|--------------------------------");

                int processedCount = 0;
                DateTime lastUpdate = DateTime.Now;

                while (processedCount < testMessages.Length)
                {
                    await Task.Delay(1000);

                    IReadOnlyList<IRelayMessage> messages = await relay.Queue.GetAllAsync();
                    RelayQueueStatistics stats = await relay.Queue.GetStatisticsAsync();

                    // Show current queue state
                    if (DateTime.Now - lastUpdate > TimeSpan.FromSeconds(3))
                    {
                        lastUpdate = DateTime.Now;

                        Console.WriteLine($"\n[STATS] {DateTime.Now:HH:mm:ss}");

                        if (stats.MessagesByPriority.Count > 0)
                        {
                            Console.WriteLine("  Queue by Priority:");
                            foreach (KeyValuePair<RelayPriority, int> kvp in stats.MessagesByPriority.OrderBy(x => x.Key))
                            {
                                ConsoleColor color = kvp.Key switch
                                {
                                    RelayPriority.Urgent => ConsoleColor.Red,
                                    RelayPriority.High => ConsoleColor.Yellow,
                                    RelayPriority.Normal => ConsoleColor.White,
                                    RelayPriority.Low => ConsoleColor.Gray,
                                    _ => ConsoleColor.White
                                };

                                ConsoleColor originalColor = Console.ForegroundColor;
                                Console.ForegroundColor = color;
                                Console.WriteLine($"    {kvp.Key,-8}: {kvp.Value} messages");
                                Console.ForegroundColor = originalColor;
                            }
                        }

                        Console.WriteLine($"  Total: {stats.TotalMessages} | Queued: {stats.QueuedMessages} | " +
                                        $"InProgress: {stats.InProgressMessages} | Deferred: {stats.DeferredMessages}");
                        Console.WriteLine();
                    }

                    // Check for processed messages
                    foreach (IRelayMessage? msg in messages.Where(m => m.Status == RelayStatus.InProgress))
                    {
                        string? subject = msg.OriginalMessage.Subject;
                        RelayPriority priority = msg.Priority;

                        ConsoleColor originalColor = Console.ForegroundColor;
                        Console.ForegroundColor = priority switch
                        {
                            RelayPriority.Urgent => ConsoleColor.Red,
                            RelayPriority.High => ConsoleColor.Yellow,
                            RelayPriority.Normal => ConsoleColor.White,
                            RelayPriority.Low => ConsoleColor.Gray,
                            _ => ConsoleColor.White
                        };

                        Console.WriteLine($"{DateTime.Now:HH:mm:ss} | {priority,-8} | Processing | {subject}");
                        Console.ForegroundColor = originalColor;

                        processedCount++;
                    }

                    if (stats.TotalMessages == 0)
                    {
                        break;
                    }
                }
            }

            Console.WriteLine();
            Console.WriteLine("[INFO] Priority queue demonstration complete!");
            Console.WriteLine("[INFO] Press any key to stop the server...");
            Console.ReadKey(true);

            // Stop server
            Console.WriteLine();
            Console.WriteLine("[INFO] Stopping server...");
            await server.StopAsync();
            Console.WriteLine("[INFO] Server stopped.");
        }
    }
}