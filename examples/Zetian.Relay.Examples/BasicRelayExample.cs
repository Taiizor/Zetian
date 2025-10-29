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
    /// Demonstrates basic relay functionality
    /// </summary>
    public static class BasicRelayExample
    {
        public static async Task RunAsync(ILoggerFactory loggerFactory)
        {
            Console.WriteLine("===========================================");
            Console.WriteLine("        Basic Relay Example");
            Console.WriteLine("===========================================");
            Console.WriteLine();
            Console.WriteLine("This example demonstrates:");
            Console.WriteLine("- Basic relay setup");
            Console.WriteLine("- Sending messages through relay");
            Console.WriteLine("- Queue monitoring");
            Console.WriteLine();

            // Create server with relay enabled
            ISmtpServer server = new SmtpServerBuilder()
                .Port(25025)
                .ServerName("relay.local")
                .LoggerFactory(loggerFactory)
                .Build()
                .EnableRelay(config =>
                {
                    // Configure relay
                    // Note: Using a non-existent host to keep messages in queue for demo
                    config.DefaultSmartHost = new SmartHostConfiguration
                    {
                        Host = "demo.smtp.example.com",  // Non-existent for demo
                        Port = 587,
                        UseStartTls = true,
                        Credentials = new NetworkCredential("username", "password"),
                        ConnectionTimeout = TimeSpan.FromSeconds(5)  // Short timeout for demo
                    };

                    // Set local domains (no relay needed)
                    config.LocalDomains.Add("relay.local");  // Server's own domain
                    config.LocalDomains.Add("localhost");     // Localhost is always local

                    // Configure relay behavior
                    config.MaxRetryCount = 3;  // Fewer retries for demo
                    config.EnableBounceMessages = true;
                    config.QueueProcessingInterval = TimeSpan.FromSeconds(10);  // Process queue faster
                });

            // Handle message received event - just for logging
            // The actual relay queuing is handled by EnableRelay's event handler
            server.MessageReceived += (sender, e) =>
            {
                Console.WriteLine($"[SERVER] Message received from {e.Message.From?.Address}");
                Console.WriteLine($"[SERVER] Recipients: {string.Join(", ", e.Message.Recipients)}");
                Console.WriteLine($"[SERVER] Subject: {e.Message.Subject}");

                // Check if this will be relayed
                bool isExternal = e.Message.Recipients.Any(r =>
                    !r.Host.Equals("relay.local", StringComparison.OrdinalIgnoreCase) &&
                    !r.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase));

                if (isExternal)
                {
                    Console.WriteLine($"[SERVER] Message will be queued for relay");
                }
                else
                {
                    Console.WriteLine($"[SERVER] Message for local delivery");
                }
            };

            Console.WriteLine("[INFO] Starting SMTP server with relay on port 25025...");
            RelayService relayService = await server.StartWithRelayAsync();
            Console.WriteLine("[INFO] Server started successfully!");
            Console.WriteLine();

            // Create a test client to send message
            Console.WriteLine("[INFO] Sending test message...");
            using SmtpClient client = new("localhost", 25025)
            {
                EnableSsl = false
            };

            MailMessage message = new()
            {
                From = new MailAddress("sender@example.com", "Test Sender"),
                Subject = "Test Relay Message",
                Body = "This is a test message that will be relayed.",
                IsBodyHtml = false
            };
            message.To.Add(new MailAddress("recipient@external.com", "External Recipient"));

            try
            {
                await client.SendMailAsync(message);
                Console.WriteLine("[INFO] Message sent to relay queue!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to send message: {ex.Message}");
            }

            // Wait a bit for message to be queued
            await Task.Delay(2000);

            // Check relay queue statistics directly from relay service
            Console.WriteLine();
            Console.WriteLine("[INFO] Checking relay queue statistics...");

            // Get statistics directly from relay service queue
            if (relayService != null && relayService.Queue != null)
            {
                RelayQueueStatistics stats = await relayService.Queue.GetStatisticsAsync();
                Console.WriteLine($"  Total Messages: {stats.TotalMessages}");
                Console.WriteLine($"  Queued: {stats.QueuedMessages}");
                Console.WriteLine($"  In Progress: {stats.InProgressMessages}");
                Console.WriteLine($"  Delivered: {stats.DeliveredMessages}");
                Console.WriteLine($"  Failed: {stats.FailedMessages}");
                Console.WriteLine($"  Deferred: {stats.DeferredMessages}");
                Console.WriteLine($"  Expired: {stats.ExpiredMessages}");

                if (stats.TotalMessages == 0)
                {
                    Console.WriteLine("\n[WARNING] No messages in queue. Possible reasons:");
                    Console.WriteLine("  - Message was for local delivery (not relayed)");
                    Console.WriteLine("  - Message relay failed immediately");
                    Console.WriteLine("  - Relay service is not properly configured");
                }
            }
            else
            {
                Console.WriteLine("[ERROR] Could not get relay service queue statistics");
            }

            // Keep server running for a while
            Console.WriteLine();
            Console.WriteLine("[INFO] Server is running. Press 'Q' to stop...");

            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Q)
                {
                    break;
                }

                if (key.Key == ConsoleKey.S)
                {
                    // Show current statistics from relay service
                    if (relayService != null && relayService.Queue != null)
                    {
                        RelayQueueStatistics stats = await relayService.Queue.GetStatisticsAsync();
                        Console.WriteLine($"[STATS] Total: {stats.TotalMessages} | Queue: {stats.QueuedMessages} | InProgress: {stats.InProgressMessages} | Delivered: {stats.DeliveredMessages} | Failed: {stats.FailedMessages} | Deferred: {stats.DeferredMessages}");
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