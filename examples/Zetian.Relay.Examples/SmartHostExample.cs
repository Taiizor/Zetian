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
    /// Demonstrates smart host configuration for relay
    /// </summary>
    public static class SmartHostExample
    {
        public static async Task RunAsync(ILoggerFactory loggerFactory)
        {
            Console.WriteLine("===========================================");
            Console.WriteLine("        Smart Host Example");
            Console.WriteLine("===========================================");
            Console.WriteLine();
            Console.WriteLine("This example demonstrates:");
            Console.WriteLine("- Configuring a smart host for relay");
            Console.WriteLine("- Authentication with smart host");
            Console.WriteLine("- TLS/SSL configuration");
            Console.WriteLine();

            // Get smart host configuration from user
            Console.Write("Enter smart host (e.g., smtp.gmail.com): ");
            string host = Console.ReadLine() ?? "smtp.gmail.com";

            Console.Write("Enter port (587 for TLS, 465 for SSL, 25 for plain): ");
            if (!int.TryParse(Console.ReadLine(), out int port))
            {
                port = 587;
            }

            Console.Write("Enter username (or press Enter to skip auth): ");
            string? username = Console.ReadLine();

            string? password = null;
            if (!string.IsNullOrEmpty(username))
            {
                Console.Write("Enter password: ");
                password = ReadPassword();
                Console.WriteLine();
            }

            // Create SMTP server with smart host configuration
            ISmtpServer server = new SmtpServerBuilder()
                .Port(25026)
                .ServerName("smarthost-example.local")
                .LoggerFactory(loggerFactory)
                .Build()
                .EnableRelay(config =>
                {
                    // Configure smart host
                    config.DefaultSmartHost = new SmartHostConfiguration
                    {
                        Host = host,
                        Port = port,
                        UseTls = port == 465,
                        UseStartTls = port == 587,
                        Credentials = !string.IsNullOrEmpty(username)
                            ? new NetworkCredential(username, password)
                            : null,
                        ConnectionTimeout = TimeSpan.FromMinutes(5),
                        MaxMessagesPerConnection = 100,
                        MaxConnections = 5
                    };

                    // General relay settings
                    config.MaxConcurrentDeliveries = 10;
                    config.MaxRetryCount = 5;
                    config.MessageLifetime = TimeSpan.FromDays(2);
                    config.EnableTls = true;
                    config.RequireTls = false;
                    config.EnableBounceMessages = true;
                    config.LocalDomain = "smarthost-example.local";

                    config.RequireAuthentication = false; // No auth required for local submissions
                });

            // Log relay events
            server.MessageReceived += async (sender, e) =>
            {
                Console.WriteLine($"[RELAY] Queueing message from {e.Message.From?.Address}");
                Console.WriteLine($"[RELAY] Will relay through {host}:{port}");

                if (!string.IsNullOrEmpty(username))
                {
                    Console.WriteLine($"[RELAY] Using authentication as {username}");
                }

                await Task.CompletedTask;
            };

            // Start server
            Console.WriteLine($"[INFO] Starting SMTP server on port 25026...");
            Console.WriteLine($"[INFO] Smart host: {host}:{port}");
            RelayService relayService = await server.StartWithRelayAsync();
            Console.WriteLine("[INFO] Server started successfully!");
            Console.WriteLine();

            // Send test message
            Console.WriteLine("[TEST] Sending test message through smart host...");
            Console.Write("Enter recipient email address: ");
            string recipient = Console.ReadLine() ?? "test@example.com";

            using SmtpClient client = new("localhost", 25026)
            {
                EnableSsl = false
            };

            MailMessage message = new()
            {
                From = new MailAddress("sender@smarthost-example.local", "Smart Host Test"),
                Subject = $"Test from Smart Host Example - {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                Body = $@"This is a test message sent through a smart host.

Configuration:
- Smart Host: {host}:{port}
- TLS: {(port == 587 ? "STARTTLS" : port == 465 ? "SSL/TLS" : "None")}
- Authentication: {(!string.IsNullOrEmpty(username) ? "Enabled" : "Disabled")}
- Sent at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}

If you receive this message, the smart host relay is working correctly!",
                IsBodyHtml = false
            };
            message.To.Add(new MailAddress(recipient));

            try
            {
                await client.SendMailAsync(message);
                Console.WriteLine("[SUCCESS] Message queued for relay!");
                Console.WriteLine("[INFO] The message will be delivered through the smart host.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to queue message: {ex.Message}");
            }

            // Monitor relay progress
            Console.WriteLine();
            Console.WriteLine("[INFO] Monitoring relay progress...");
            Console.WriteLine("[INFO] Press 'S' for statistics, 'Q' to quit");

            DateTime lastCheck = DateTime.MinValue;
            while (true)
            {
                if (Console.KeyAvailable)
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
                            Console.WriteLine($"  Queued: {stats.QueuedMessages}");
                            Console.WriteLine($"  In Progress: {stats.InProgressMessages}");
                            Console.WriteLine($"  Delivered: {stats.DeliveredMessages}");
                            Console.WriteLine($"  Failed: {stats.FailedMessages}");
                            Console.WriteLine($"  Deferred: {stats.DeferredMessages}");

                            if (stats.AverageQueueTime.TotalSeconds > 0)
                            {
                                Console.WriteLine($"  Avg Queue Time: {stats.AverageQueueTime:mm\\:ss}");
                            }

                            if (stats.AverageRetryCount > 0)
                            {
                                Console.WriteLine($"  Avg Retry Count: {stats.AverageRetryCount:F1}");
                            }
                        }
                    }
                }

                // Auto-check every 5 seconds
                if (DateTime.Now - lastCheck > TimeSpan.FromSeconds(5))
                {
                    lastCheck = DateTime.Now;
                    RelayQueueStatistics? stats = await server.GetRelayStatisticsAsync();
                    if (stats != null && (stats.DeliveredMessages > 0 || stats.FailedMessages > 0))
                    {
                        Console.WriteLine($"[UPDATE] Delivered: {stats.DeliveredMessages}, Failed: {stats.FailedMessages}");
                    }
                }

                await Task.Delay(100);
            }

            // Stop server
            Console.WriteLine();
            Console.WriteLine("[INFO] Stopping server...");
            await server.StopAsync();
            Console.WriteLine("[INFO] Server stopped.");
        }

        private static string ReadPassword()
        {
            string password = "";
            ConsoleKeyInfo key;

            do
            {
                key = Console.ReadKey(true);

                if (key.Key is not ConsoleKey.Backspace and not ConsoleKey.Enter)
                {
                    password += key.KeyChar;
                    Console.Write("*");
                }
                else if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                {
                    password = password[0..^1];
                    Console.Write("\b \b");
                }
            }
            while (key.Key != ConsoleKey.Enter);

            return password;
        }
    }
}