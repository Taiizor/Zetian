using Microsoft.Extensions.Logging;
using System.Net.Mail;
using Zetian.Abstractions;
using Zetian.Relay.Builder;
using Zetian.Relay.Configuration;
using Zetian.Relay.Extensions;
using Zetian.Relay.Services;
using Zetian.Server;

namespace Zetian.Relay.Examples
{
    /// <summary>
    /// Demonstrates domain-specific routing
    /// </summary>
    public static class DomainRoutingExample
    {
        public static async Task RunAsync(ILoggerFactory loggerFactory)
        {
            Console.WriteLine("===========================================");
            Console.WriteLine("       Domain Routing Example");
            Console.WriteLine("===========================================");
            Console.WriteLine();
            Console.WriteLine("This example demonstrates:");
            Console.WriteLine("- Domain-specific routing rules");
            Console.WriteLine("- Different smart hosts per domain");
            Console.WriteLine("- Local vs relay domain configuration");
            Console.WriteLine();

            // Build relay configuration with domain routing
            RelayConfiguration relayConfig = new RelayBuilder()
                .Enable(true)
                .LocalDomain("mail.company.local")

                // Define local domains (delivered locally, not relayed)
                .AddLocalDomains(
                    "company.local",
                    "internal.local",
                    "localhost")

                // Define relay domains (always relayed even without auth)
                .AddRelayDomains(
                    "partner.com",
                    "customer.net",
                    "vendor.org")

                // Gmail-specific routing
                .AddDomainRoute(
                    "gmail.com",
                    "smtp.gmail.com",
                    587,
                    "your_gmail@gmail.com",
                    "app_specific_password")

                // Office 365 routing
                .AddDomainRoute(
                    "outlook.com",
                    "smtp-mail.outlook.com",
                    587,
                    "your_outlook@outlook.com",
                    "password")

                // SendGrid for marketing domains
                .AddDomainRoute(
                    "marketing.example.com",
                    "smtp.sendgrid.net",
                    587,
                    "apikey",
                    "SG.your_sendgrid_api_key")

                // Default smart host for all other domains
                .WithSmartHost("default.smtp.provider.com", 25)
                .MaxConcurrentDeliveries(20)
                .MessageLifetime(TimeSpan.FromDays(3))
                .Build();

            // Create server with domain routing
            ISmtpServer server = new SmtpServerBuilder()
                .Port(25028)
                .ServerName("routing.local")
                .LoggerFactory(loggerFactory)
                .Build()
                .EnableRelay(config => relayConfig);

            // Log routing decisions
            server.MessageReceived += async (sender, e) =>
            {
                Console.WriteLine($"\n[ROUTING] Analyzing message from {e.Message.From?.Address}");

                foreach (MailAddress recipient in e.Message.Recipients)
                {
                    string domain = recipient.Host;
                    Console.WriteLine($"  Recipient: {recipient.Address}");

                    // Determine routing
                    if (relayConfig.LocalDomains.Contains(domain))
                    {
                        Console.WriteLine($"    -> LOCAL delivery (domain: {domain})");
                    }
                    else if (relayConfig.DomainRouting.ContainsKey(domain))
                    {
                        SmartHostConfiguration route = relayConfig.DomainRouting[domain];
                        Console.WriteLine($"    -> CUSTOM route via {route.Host}:{route.Port}");
                    }
                    else if (relayConfig.RelayDomains.Contains(domain))
                    {
                        Console.WriteLine($"    -> RELAY domain (authorized for relay)");
                    }
                    else
                    {
                        Console.WriteLine($"    -> DEFAULT smart host");
                    }
                }

                await Task.CompletedTask;
            };

            // Start server
            Console.WriteLine("[INFO] Starting SMTP server with domain routing...");
            RelayService relayService = await server.StartWithRelayAsync();
            Console.WriteLine("[INFO] Server started on port 25028");
            Console.WriteLine();

            // Display routing table
            Console.WriteLine("[CONFIG] Domain Routing Table:");
            Console.WriteLine("┌─────────────────────────┬──────────────────────────┬──────┐");
            Console.WriteLine("│ Domain                  │ Smart Host               │ Port │");
            Console.WriteLine("├─────────────────────────┼──────────────────────────┼──────┤");
            Console.WriteLine("│ *.company.local         │ LOCAL DELIVERY           │  -   │");
            Console.WriteLine("│ *.internal.local        │ LOCAL DELIVERY           │  -   │");
            Console.WriteLine("│ gmail.com               │ smtp.gmail.com           │ 587  │");
            Console.WriteLine("│ outlook.com             │ smtp-mail.outlook.com    │ 587  │");
            Console.WriteLine("│ marketing.example.com   │ smtp.sendgrid.net        │ 587  │");
            Console.WriteLine("│ partner.com             │ RELAY AUTHORIZED         │  -   │");
            Console.WriteLine("│ customer.net            │ RELAY AUTHORIZED         │  -   │");
            Console.WriteLine("│ vendor.org              │ RELAY AUTHORIZED         │  -   │");
            Console.WriteLine("│ * (all others)          │ default.smtp.provider.com│  25  │");
            Console.WriteLine("└─────────────────────────┴──────────────────────────┴──────┘");
            Console.WriteLine();

            // Test different domain routings
            Console.WriteLine("[TEST] Sending test messages to different domains...");

            using SmtpClient client = new("localhost", 25028)
            {
                EnableSsl = false
            };

            // Test cases
            (string, string)[] testCases = new[]
            {
                ("local@company.local", "Local Domain Test"),
                ("user@gmail.com", "Gmail Routing Test"),
                ("contact@outlook.com", "Outlook Routing Test"),
                ("sales@partner.com", "Partner Relay Test"),
                ("info@unknown-domain.com", "Default Routing Test"),
                ("marketing@marketing.example.com", "SendGrid Routing Test")
            };

            foreach ((string? recipient, string? subject) in testCases)
            {
                MailMessage message = new()
                {
                    From = new MailAddress("sender@domain-routing.local"),
                    Subject = subject,
                    Body = $@"Testing domain-specific routing for: {recipient}
                    
This message should be routed according to the domain routing table.
Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                    IsBodyHtml = false
                };
                message.To.Add(recipient);

                try
                {
                    await client.SendMailAsync(message);
                    Console.WriteLine($"  ✓ Queued: {recipient} - {subject}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ✗ Failed: {recipient} - {ex.Message}");
                }

                await Task.Delay(200);
            }

            // Monitor routing
            Console.WriteLine();
            Console.WriteLine("[MONITOR] Monitoring domain routing...");
            Console.WriteLine("[INFO] Press 'S' for statistics by domain, 'Q' to quit");

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

                        if (stats.MessagesBySmartHost != null && stats.MessagesBySmartHost.Count > 0)
                        {
                            Console.WriteLine("\n  Messages by Smart Host:");
                            foreach (KeyValuePair<string, int> kvp in stats.MessagesBySmartHost)
                            {
                                Console.WriteLine($"    {kvp.Key}: {kvp.Value} messages");
                            }
                        }

                        Console.WriteLine($"\n  Status Distribution:");
                        Console.WriteLine($"    Queued: {stats.QueuedMessages}");
                        Console.WriteLine($"    In Progress: {stats.InProgressMessages}");
                        Console.WriteLine($"    Delivered: {stats.DeliveredMessages}");
                        Console.WriteLine($"    Failed: {stats.FailedMessages}");
                        Console.WriteLine($"    Deferred: {stats.DeferredMessages}");
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