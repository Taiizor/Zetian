using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using Zetian.Abstractions;
using Zetian.Relay.Extensions;
using Zetian.Relay.Models;
using Zetian.Relay.Services;
using Zetian.Server;

namespace Zetian.Relay.Examples
{
    /// <summary>
    /// Demonstrates MX record-based routing
    /// </summary>
    public static class MxRoutingExample
    {
        public static async Task RunAsync(ILoggerFactory loggerFactory)
        {
            Console.WriteLine("===========================================");
            Console.WriteLine("         MX Routing Example");
            Console.WriteLine("===========================================");
            Console.WriteLine();
            Console.WriteLine("This example demonstrates:");
            Console.WriteLine("- DNS MX record lookups for routing");
            Console.WriteLine("- Direct delivery to recipient domains");
            Console.WriteLine("- Custom DNS server configuration");
            Console.WriteLine("- Fallback smart host on MX failure");
            Console.WriteLine();

            // Create server with MX routing enabled
            ISmtpServer server = new SmtpServerBuilder()
                .Port(25029)
                .ServerName("mx-router.local")
                .LoggerFactory(loggerFactory)
                .Build()
                .EnableRelay(config =>
                {
                    config.RequireAuthentication = false; // Allow unauthenticated relay for testing

                    // Enable MX-based routing
                    config.UseMxRouting = true;

                    // Configure DNS servers for MX lookups
                    config.DnsServers.Add(IPAddress.Parse("8.8.8.8"));     // Google DNS
                    config.DnsServers.Add(IPAddress.Parse("8.8.4.4"));     // Google DNS secondary
                    config.DnsServers.Add(IPAddress.Parse("1.1.1.1"));     // Cloudflare DNS
                    config.DnsServers.Add(IPAddress.Parse("208.67.222.222")); // OpenDNS

                    // Fallback smart host if MX lookup fails
                    config.DefaultSmartHost = new()
                    {
                        Host = "fallback.smtp.provider.com",
                        Port = 25,
                        Priority = 100
                    };

                    // General settings
                    config.MaxConcurrentDeliveries = 10;
                    config.ConnectionTimeout = TimeSpan.FromSeconds(30);
                    config.EnableTls = true;
                    config.RequireTls = false;
                    config.LocalDomain = "mx-routing.local";
                });

            // Log MX routing decisions
            server.MessageReceived += async (sender, e) =>
            {
                Console.WriteLine($"\n[MX-ROUTING] Processing message from {e.Message.From?.Address}");

                foreach (MailAddress recipient in e.Message.Recipients)
                {
                    string domain = recipient.Host;
                    Console.WriteLine($"  Recipient: {recipient.Address}");
                    Console.WriteLine($"  Domain: {domain}");

                    // Simulate MX lookup
                    Console.WriteLine($"  Performing MX lookup for {domain}...");

                    // In real implementation, this would do actual DNS MX query
                    if (IsWellKnownDomain(domain))
                    {
                        ShowMxRecords(domain);
                    }
                    else
                    {
                        Console.WriteLine($"    No MX records found, will use fallback smart host");
                    }
                }

                await Task.CompletedTask;
            };

            // Start server
            Console.WriteLine("[INFO] Starting SMTP server with MX routing...");
            RelayService relayService = await server.StartWithRelayAsync();
            Console.WriteLine("[INFO] Server started on port 25029");
            Console.WriteLine();

            // Display configuration
            Console.WriteLine("[CONFIG] MX Routing Configuration:");
            Console.WriteLine("  MX Routing: Enabled");
            Console.WriteLine("  DNS Servers:");
            Console.WriteLine("    - 8.8.8.8 (Google)");
            Console.WriteLine("    - 8.8.4.4 (Google)");
            Console.WriteLine("    - 1.1.1.1 (Cloudflare)");
            Console.WriteLine("    - 208.67.222.222 (OpenDNS)");
            Console.WriteLine("  Fallback: fallback.smtp.provider.com:25");
            Console.WriteLine();

            // Test MX routing
            Console.WriteLine("[TEST] Sending test messages to various domains...");
            Console.WriteLine();

            using SmtpClient client = new("localhost", 25029)
            {
                EnableSsl = false
            };

            // Test different domains
            (string, string)[] testDomains = new[]
            {
                ("gmail.com", "Google Mail"),
                ("outlook.com", "Microsoft Outlook"),
                ("yahoo.com", "Yahoo Mail"),
                ("protonmail.com", "ProtonMail"),
                ("example.com", "Example Domain"),
                ("nonexistent.domain.test", "Non-existent Domain"),
                ("internal.company.local", "Internal Domain")
            };

            foreach ((string? domain, string? description) in testDomains)
            {
                string recipient = $"test@{domain}";
                MailMessage message = new()
                {
                    From = new MailAddress("sender@mx-routing.local"),
                    Subject = $"MX Routing Test to {description}",
                    Body = $@"Testing MX-based routing to {domain}

This message demonstrates DNS MX record-based routing.
The system will:
1. Query DNS servers for MX records of {domain}
2. Sort MX records by priority
3. Attempt delivery to MX hosts in order
4. Fall back to smart host if all MX attempts fail

Domain: {domain}
Description: {description}
Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                    IsBodyHtml = false
                };
                message.To.Add(recipient);

                try
                {
                    await client.SendMailAsync(message);
                    Console.WriteLine($"  ✓ Queued for {domain} ({description})");

                    // Show expected MX routing
                    if (IsWellKnownDomain(domain))
                    {
                        Console.WriteLine($"    → Will route via MX records");
                    }
                    else
                    {
                        Console.WriteLine($"    → Will use fallback smart host");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ✗ Failed for {domain}: {ex.Message}");
                }

                Console.WriteLine();
                await Task.Delay(500);
            }

            // Interactive MX lookup demo
            Console.WriteLine("[DEMO] Interactive MX Lookup");
            Console.WriteLine("Enter a domain to simulate MX lookup (or 'quit' to exit):");
            Console.WriteLine();

            while (true)
            {
                Console.Write("Domain: ");
                string? input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input) || input.Equals("quit", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                Console.WriteLine($"\n[MX-LOOKUP] Querying MX records for {input}...");

                // Simulate MX lookup results
                if (IsWellKnownDomain(input))
                {
                    ShowMxRecords(input);
                    Console.WriteLine("\n  Routing Decision: Use MX records for direct delivery");
                }
                else
                {
                    Console.WriteLine("  No MX records found");
                    Console.WriteLine("  Routing Decision: Use fallback smart host (fallback.smtp.provider.com)");
                }

                Console.WriteLine();
            }

            // Show queue statistics
            Console.WriteLine();
            Console.WriteLine("[INFO] Checking relay queue statistics...");
            RelayQueueStatistics? stats = await server.GetRelayStatisticsAsync();

            if (stats != null)
            {
                Console.WriteLine($"  Total Messages: {stats.TotalMessages}");
                Console.WriteLine($"  Messages by Smart Host:");

                if (stats.MessagesBySmartHost.Count > 0)
                {
                    foreach (KeyValuePair<string, int> kvp in stats.MessagesBySmartHost)
                    {
                        Console.WriteLine($"    {kvp.Key}: {kvp.Value} messages");
                    }
                }
                else
                {
                    Console.WriteLine("    (MX routing - varies by recipient domain)");
                }
            }

            // Stop server
            Console.WriteLine();
            Console.WriteLine("[INFO] Press any key to stop the server...");
            Console.ReadKey(true);

            Console.WriteLine("[INFO] Stopping server...");
            await server.StopAsync();
            Console.WriteLine("[INFO] Server stopped.");
        }

        private static bool IsWellKnownDomain(string domain)
        {
            string[] wellKnown = new[]
            {
                "gmail.com", "outlook.com", "yahoo.com", "hotmail.com",
                "aol.com", "icloud.com", "protonmail.com", "mail.com",
                "yandex.com", "zoho.com"
            };

            return Array.Exists(wellKnown, d => d.Equals(domain, StringComparison.OrdinalIgnoreCase));
        }

        private static void ShowMxRecords(string domain)
        {
            // Simulate MX records for demonstration
            switch (domain.ToLower())
            {
                case "gmail.com":
                    Console.WriteLine("  MX Records found:");
                    Console.WriteLine("    5  gmail-smtp-in.l.google.com");
                    Console.WriteLine("    10 alt1.gmail-smtp-in.l.google.com");
                    Console.WriteLine("    20 alt2.gmail-smtp-in.l.google.com");
                    Console.WriteLine("    30 alt3.gmail-smtp-in.l.google.com");
                    Console.WriteLine("    40 alt4.gmail-smtp-in.l.google.com");
                    break;

                case "outlook.com":
                case "hotmail.com":
                    Console.WriteLine("  MX Records found:");
                    Console.WriteLine("    5  outlook-com.olc.protection.outlook.com");
                    break;

                case "yahoo.com":
                    Console.WriteLine("  MX Records found:");
                    Console.WriteLine("    1  mta5.am0.yahoodns.net");
                    Console.WriteLine("    1  mta6.am0.yahoodns.net");
                    Console.WriteLine("    1  mta7.am0.yahoodns.net");
                    break;

                case "protonmail.com":
                    Console.WriteLine("  MX Records found:");
                    Console.WriteLine("    5  mail.protonmail.ch");
                    Console.WriteLine("    10 mailsec.protonmail.ch");
                    break;

                default:
                    Console.WriteLine("  MX Records found:");
                    Console.WriteLine($"    10 mx1.{domain}");
                    Console.WriteLine($"    20 mx2.{domain}");
                    break;
            }
        }
    }
}