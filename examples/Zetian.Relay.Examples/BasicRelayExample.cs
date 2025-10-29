using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using Zetian.Abstractions;
using Zetian.Relay.Configuration;
using Zetian.Relay.Extensions;
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
                    config.DefaultSmartHost = new SmartHostConfiguration
                    {
                        Host = "smtp.relay.provider.com",
                        Port = 587,
                        UseStartTls = true,
                        Credentials = new NetworkCredential("username", "password")
                    };

                    // Set local domains (no relay needed)
                    config.LocalDomains.Add("local.domain");
                    config.LocalDomains.Add("internal.domain");

                    // Configure relay behavior
                    config.MaxRetryCount = 10;
                    config.EnableBounceMessages = true;
                });

            // Handle message received event
            server.MessageReceived += async (sender, e) =>
            {
                Console.WriteLine($"[SERVER] Message received from {e.Message.From?.Address}");
                Console.WriteLine($"[SERVER] Recipients: {string.Join(", ", e.Message.Recipients)}");
                Console.WriteLine($"[SERVER] Subject: {e.Message.Subject}");

                // Message will be automatically queued for relay if needed
                await Task.CompletedTask;
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

            // Wait a bit for processing
            await Task.Delay(2000);

            // Check relay queue statistics
            Console.WriteLine();
            Console.WriteLine("[INFO] Checking relay queue statistics...");
            RelayQueueStatistics? stats = await server.GetRelayStatisticsAsync();

            if (stats != null)
            {
                Console.WriteLine($"  Total Messages: {stats.TotalMessages}");
                Console.WriteLine($"  Queued: {stats.QueuedMessages}");
                Console.WriteLine($"  In Progress: {stats.InProgressMessages}");
                Console.WriteLine($"  Delivered: {stats.DeliveredMessages}");
                Console.WriteLine($"  Failed: {stats.FailedMessages}");
                Console.WriteLine($"  Deferred: {stats.DeferredMessages}");
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
                    // Show current statistics
                    stats = await server.GetRelayStatisticsAsync();
                    if (stats != null)
                    {
                        Console.WriteLine($"[STATS] Queue: {stats.QueuedMessages}, Progress: {stats.InProgressMessages}, Delivered: {stats.DeliveredMessages}");
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