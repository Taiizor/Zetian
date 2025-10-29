using Microsoft.Extensions.Logging;
using System.Net.Mail;
using Zetian.Abstractions;
using Zetian.Relay.Abstractions;
using Zetian.Relay.Enums;
using Zetian.Relay.Extensions;
using Zetian.Relay.Models;
using Zetian.Relay.Services;
using Zetian.Server;

namespace Zetian.Relay.Examples
{
    /// <summary>
    /// Demonstrates queue management operations
    /// </summary>
    public static class QueueManagementExample
    {
        public static async Task RunAsync(ILoggerFactory loggerFactory)
        {
            Console.WriteLine("===========================================");
            Console.WriteLine("       Queue Management Example");
            Console.WriteLine("===========================================");
            Console.WriteLine();
            Console.WriteLine("This example demonstrates:");
            Console.WriteLine("- Queue operations (list, remove, reschedule)");
            Console.WriteLine("- Queue statistics and monitoring");
            Console.WriteLine("- Message status tracking");
            Console.WriteLine("- Expired message cleanup");
            Console.WriteLine();

            // Create server with relay enabled
            ISmtpServer server = new SmtpServerBuilder()
                .Port(25030)
                .ServerName("queue.local")
                .LoggerFactory(loggerFactory)
                .Build()
                .EnableRelay(config =>
                {
                    config.MessageLifetime = TimeSpan.FromMinutes(10); // Short lifetime for demo
                    config.CleanupInterval = TimeSpan.FromMinutes(1);
                    config.QueueProcessingInterval = TimeSpan.FromSeconds(5);
                    config.MaxConcurrentDeliveries = 5;
                    config.MaxRetryCount = 3;

                    // Use a non-existent smart host to keep messages in queue
                    config.DefaultSmartHost = new()
                    {
                        Host = "demo.nonexistent.smtp.com",
                        Port = 25,
                        ConnectionTimeout = TimeSpan.FromSeconds(5)
                    };
                });

            // Start server
            Console.WriteLine("[INFO] Starting SMTP server with queue management...");
            RelayService relayService = await server.StartWithRelayAsync();
            Console.WriteLine("[INFO] Server started on port 25029");
            Console.WriteLine();

            // Get relay service and queue
            RelayService? relay = server.GetRelayService();
            if (relay == null)
            {
                Console.WriteLine("[ERROR] Failed to get relay service");
                return;
            }

            IRelayQueue queue = relay.Queue;

            // Send test messages to populate queue
            Console.WriteLine("[TEST] Populating queue with test messages...");
            using SmtpClient client = new("localhost", 25030)
            {
                EnableSsl = false
            };

            for (int i = 1; i <= 5; i++)
            {
                MailMessage message = new()
                {
                    From = new MailAddress($"sender{i}@example.com"),
                    Subject = $"Queue Test Message #{i}",
                    Body = $"Message {i} for queue management demo - {DateTime.Now}",
                    Priority = i % 3 == 0 ? MailPriority.High : MailPriority.Normal
                };
                message.To.Add($"recipient{i}@test.com");

                try
                {
                    await client.SendMailAsync(message);
                    Console.WriteLine($"  ✓ Message #{i} added to queue");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ✗ Failed to add message #{i}: {ex.Message}");
                }

                await Task.Delay(100);
            }

            Console.WriteLine();
            Console.WriteLine("[INFO] Queue populated. Starting management interface...");
            Console.WriteLine();

            // Interactive queue management
            bool running = true;
            while (running)
            {
                Console.WriteLine("\n========= Queue Management Menu =========");
                Console.WriteLine("1. View queue statistics");
                Console.WriteLine("2. List all messages");
                Console.WriteLine("3. List messages by status");
                Console.WriteLine("4. View specific message");
                Console.WriteLine("5. Remove a message");
                Console.WriteLine("6. Reschedule a message");
                Console.WriteLine("7. Clear expired messages");
                Console.WriteLine("8. Send more test messages");
                Console.WriteLine("9. Auto-monitor (5 second updates)");
                Console.WriteLine("0. Exit");
                Console.WriteLine();
                Console.Write("Select option: ");

                string? choice = Console.ReadLine();
                Console.WriteLine();

                try
                {
                    switch (choice)
                    {
                        case "1": // View statistics
                            await ShowStatistics(queue);
                            break;

                        case "2": // List all messages
                            await ListAllMessages(queue);
                            break;

                        case "3": // List by status
                            await ListMessagesByStatus(queue);
                            break;

                        case "4": // View specific message
                            await ViewMessage(queue);
                            break;

                        case "5": // Remove message
                            await RemoveMessage(queue);
                            break;

                        case "6": // Reschedule message
                            await RescheduleMessage(queue);
                            break;

                        case "7": // Clear expired
                            await ClearExpiredMessages(queue);
                            break;

                        case "8": // Send more messages
                            await SendMoreMessages(client);
                            break;

                        case "9": // Auto-monitor
                            await AutoMonitor(queue);
                            break;

                        case "0": // Exit
                            running = false;
                            break;

                        default:
                            Console.WriteLine("[ERROR] Invalid option");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] {ex.Message}");
                }
            }

            // Stop server
            Console.WriteLine();
            Console.WriteLine("[INFO] Stopping server...");
            await server.StopAsync();
            Console.WriteLine("[INFO] Server stopped.");
        }

        private static async Task ShowStatistics(Zetian.Relay.Abstractions.IRelayQueue queue)
        {
            RelayQueueStatistics stats = await queue.GetStatisticsAsync();

            Console.WriteLine($"[STATISTICS] {DateTime.Now:HH:mm:ss}");
            Console.WriteLine($"  Total Messages: {stats.TotalMessages}");
            Console.WriteLine($"  Queued: {stats.QueuedMessages}");
            Console.WriteLine($"  In Progress: {stats.InProgressMessages}");
            Console.WriteLine($"  Deferred: {stats.DeferredMessages}");
            Console.WriteLine($"  Delivered: {stats.DeliveredMessages}");
            Console.WriteLine($"  Failed: {stats.FailedMessages}");
            Console.WriteLine($"  Expired: {stats.ExpiredMessages}");
            Console.WriteLine($"  Total Size: {stats.TotalSize:N0} bytes");

            if (stats.OldestMessageTime.HasValue)
            {
                Console.WriteLine($"  Oldest Message: {stats.OldestMessageTime.Value:yyyy-MM-dd HH:mm:ss}");
            }

            if (stats.AverageQueueTime.TotalSeconds > 0)
            {
                Console.WriteLine($"  Avg Queue Time: {stats.AverageQueueTime:mm\\:ss}");
            }

            if (stats.AverageRetryCount > 0)
            {
                Console.WriteLine($"  Avg Retry Count: {stats.AverageRetryCount:F2}");
            }

            if (stats.MessagesByPriority.Count > 0)
            {
                Console.WriteLine("\n  Messages by Priority:");
                foreach (KeyValuePair<RelayPriority, int> kvp in stats.MessagesByPriority)
                {
                    Console.WriteLine($"    {kvp.Key}: {kvp.Value}");
                }
            }
        }

        private static async Task ListAllMessages(Zetian.Relay.Abstractions.IRelayQueue queue)
        {
            IReadOnlyList<IRelayMessage> messages = await queue.GetAllAsync();

            if (messages.Count == 0)
            {
                Console.WriteLine("[INFO] Queue is empty");
                return;
            }

            Console.WriteLine($"[MESSAGES] Total: {messages.Count}");
            Console.WriteLine("┌────────────────────────────────┬──────────────────┬──────────┬─────────┬───────┐");
            Console.WriteLine("│ Queue ID                       │ From             │ Status   │ Priority│ Retry │");
            Console.WriteLine("├────────────────────────────────┼──────────────────┼──────────┼─────────┼───────┤");

            foreach (IRelayMessage? msg in messages.Take(20))
            {
                string queueId = msg.QueueId.Length > 30 ? msg.QueueId[..30] + "..." : msg.QueueId;
                string from = msg.From?.Address ?? "<>";
                if (from.Length > 16)
                {
                    from = from[..16] + "...";
                }

                Console.WriteLine($"│ {queueId,-30} │ {from,-16} │ {msg.Status,-8} │ {msg.Priority,-7} │ {msg.RetryCount,5} │");
            }

            Console.WriteLine("└────────────────────────────────┴──────────────────┴──────────┴─────────┴───────┘");

            if (messages.Count > 20)
            {
                Console.WriteLine($"... and {messages.Count - 20} more messages");
            }
        }

        private static async Task ListMessagesByStatus(Zetian.Relay.Abstractions.IRelayQueue queue)
        {
            Console.Write("Enter status (Queued/InProgress/Deferred/Delivered/Failed/Expired): ");
            string? statusStr = Console.ReadLine();

            if (!Enum.TryParse<RelayStatus>(statusStr, true, out RelayStatus status))
            {
                Console.WriteLine("[ERROR] Invalid status");
                return;
            }

            IReadOnlyList<IRelayMessage> messages = await queue.GetByStatusAsync(status);

            if (messages.Count == 0)
            {
                Console.WriteLine($"[INFO] No messages with status: {status}");
                return;
            }

            Console.WriteLine($"[MESSAGES] Status: {status}, Count: {messages.Count}");
            foreach (IRelayMessage? msg in messages.Take(10))
            {
                Console.WriteLine($"  ID: {msg.QueueId[..8]}... | From: {msg.From?.Address} | Recipients: {msg.Recipients.Count} | Retry: {msg.RetryCount}");

                if (msg.LastError != null)
                {
                    Console.WriteLine($"    Error: {msg.LastError}");
                }

                if (msg.NextDeliveryTime.HasValue)
                {
                    Console.WriteLine($"    Next Attempt: {msg.NextDeliveryTime.Value:HH:mm:ss}");
                }
            }

            if (messages.Count > 10)
            {
                Console.WriteLine($"... and {messages.Count - 10} more messages");
            }
        }

        private static async Task ViewMessage(Zetian.Relay.Abstractions.IRelayQueue queue)
        {
            Console.Write("Enter Queue ID (or first 8 characters): ");
            string? queueId = Console.ReadLine();

            if (string.IsNullOrEmpty(queueId))
            {
                Console.WriteLine("[ERROR] Queue ID required");
                return;
            }

            // If short ID provided, try to find full ID
            IReadOnlyList<IRelayMessage> messages = await queue.GetAllAsync();
            IRelayMessage? message = messages.FirstOrDefault(m => m.QueueId.StartsWith(queueId));

            if (message == null)
            {
                Console.WriteLine($"[ERROR] Message not found: {queueId}");
                return;
            }

            Console.WriteLine($"\n[MESSAGE DETAILS]");
            Console.WriteLine($"  Queue ID: {message.QueueId}");
            Console.WriteLine($"  Status: {message.Status}");
            Console.WriteLine($"  Priority: {message.Priority}");
            Console.WriteLine($"  From: {message.From?.Address ?? "<>"}");
            Console.WriteLine($"  Recipients: {string.Join(", ", message.Recipients.Select(r => r.Address))}");
            Console.WriteLine($"  Pending: {string.Join(", ", message.PendingRecipients.Select(r => r.Address))}");
            Console.WriteLine($"  Delivered: {string.Join(", ", message.DeliveredRecipients.Select(r => r.Address))}");
            Console.WriteLine($"  Failed: {string.Join(", ", message.FailedRecipients.Select(r => r.Address))}");
            Console.WriteLine($"  Queued Time: {message.QueuedTime:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"  Retry Count: {message.RetryCount}");
            Console.WriteLine($"  Is Expired: {message.IsExpired}");

            if (message.LastAttemptTime.HasValue)
            {
                Console.WriteLine($"  Last Attempt: {message.LastAttemptTime.Value:yyyy-MM-dd HH:mm:ss}");
            }

            if (message.NextDeliveryTime.HasValue)
            {
                Console.WriteLine($"  Next Delivery: {message.NextDeliveryTime.Value:yyyy-MM-dd HH:mm:ss}");
            }

            if (!string.IsNullOrEmpty(message.LastError))
            {
                Console.WriteLine($"  Last Error: {message.LastError}");
            }

            if (!string.IsNullOrEmpty(message.SmartHost))
            {
                Console.WriteLine($"  Smart Host: {message.SmartHost}");
            }
        }

        private static async Task RemoveMessage(Zetian.Relay.Abstractions.IRelayQueue queue)
        {
            Console.Write("Enter Queue ID to remove: ");
            string? queueId = Console.ReadLine();

            if (string.IsNullOrEmpty(queueId))
            {
                Console.WriteLine("[ERROR] Queue ID required");
                return;
            }

            bool removed = await queue.RemoveAsync(queueId);

            if (removed)
            {
                Console.WriteLine($"[SUCCESS] Message removed: {queueId}");
            }
            else
            {
                Console.WriteLine($"[ERROR] Message not found: {queueId}");
            }
        }

        private static async Task RescheduleMessage(Zetian.Relay.Abstractions.IRelayQueue queue)
        {
            Console.Write("Enter Queue ID to reschedule: ");
            string? queueId = Console.ReadLine();

            if (string.IsNullOrEmpty(queueId))
            {
                Console.WriteLine("[ERROR] Queue ID required");
                return;
            }

            Console.Write("Enter delay in minutes (default 5): ");
            string? delayStr = Console.ReadLine();
            int delay = int.TryParse(delayStr, out int minutes) ? minutes : 5;

            await queue.RescheduleAsync(queueId, TimeSpan.FromMinutes(delay));
            Console.WriteLine($"[SUCCESS] Message rescheduled for delivery in {delay} minutes");
        }

        private static async Task ClearExpiredMessages(Zetian.Relay.Abstractions.IRelayQueue queue)
        {
            int count = await queue.ClearExpiredAsync();
            Console.WriteLine($"[INFO] Cleared {count} expired messages");
        }

        private static async Task SendMoreMessages(SmtpClient client)
        {
            Console.Write("How many messages to send? ");
            if (!int.TryParse(Console.ReadLine(), out int count))
            {
                count = 3;
            }

            for (int i = 1; i <= count; i++)
            {
                MailMessage message = new()
                {
                    From = new MailAddress($"batch{i}@example.com"),
                    Subject = $"Batch Message #{i}",
                    Body = $"Additional test message {i} - {DateTime.Now}",
                    Priority = Random.Shared.Next(3) switch
                    {
                        0 => MailPriority.High,
                        1 => MailPriority.Low,
                        _ => MailPriority.Normal
                    }
                };
                message.To.Add($"batch{i}@test.com");

                try
                {
                    await client.SendMailAsync(message);
                    Console.WriteLine($"  ✓ Sent message #{i}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ✗ Failed: {ex.Message}");
                }
            }
        }

        private static async Task AutoMonitor(Zetian.Relay.Abstractions.IRelayQueue queue)
        {
            Console.WriteLine("[MONITOR] Auto-monitoring queue (Press any key to stop)...");

            while (!Console.KeyAvailable)
            {
                RelayQueueStatistics stats = await queue.GetStatisticsAsync();

                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write($"[{DateTime.Now:HH:mm:ss}] Total: {stats.TotalMessages} | " +
                             $"Q: {stats.QueuedMessages} | P: {stats.InProgressMessages} | " +
                             $"Def: {stats.DeferredMessages} | Del: {stats.DeliveredMessages} | " +
                             $"Fail: {stats.FailedMessages}     ");

                await Task.Delay(5000);
            }

            Console.ReadKey(true);
            Console.WriteLine("\n[INFO] Monitoring stopped");
        }
    }
}