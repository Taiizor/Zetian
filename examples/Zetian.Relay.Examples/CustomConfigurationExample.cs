using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using System.Security.Authentication;
using Zetian.Abstractions;
using Zetian.Relay.Builder;
using Zetian.Relay.Configuration;
using Zetian.Relay.Enums;
using Zetian.Relay.Extensions;
using Zetian.Relay.Services;
using Zetian.Server;

namespace Zetian.Relay.Examples
{
    /// <summary>
    /// Demonstrates advanced custom relay configuration
    /// </summary>
    public static class CustomConfigurationExample
    {
        public static async Task RunAsync(ILoggerFactory loggerFactory)
        {
            Console.WriteLine("===========================================");
            Console.WriteLine("    Custom Relay Configuration Example");
            Console.WriteLine("===========================================");
            Console.WriteLine();
            Console.WriteLine("This example demonstrates:");
            Console.WriteLine("- Complete relay configuration");
            Console.WriteLine("- RelayBuilder fluent API");
            Console.WriteLine("- All configuration options");
            Console.WriteLine("- Production-ready setup");
            Console.WriteLine();

            // Build comprehensive relay configuration
            RelayConfiguration relayConfig = new RelayBuilder()
                // Basic settings
                .Enable(true)
                .LocalDomain("mail.company.com")

                // Primary smart host with full configuration
                .WithSmartHost("primary.smtp.provider.com", 587, "api_user", "api_key")

                // Additional smart hosts for failover
                .AddSmartHost(new SmartHostConfiguration
                {
                    Host = "backup1.smtp.provider.com",
                    Port = 587,
                    Priority = 20,
                    Weight = 100,
                    UseStartTls = true,
                    Credentials = new NetworkCredential("backup_user", "backup_pass"),
                    MaxConnections = 10,
                    MaxMessagesPerConnection = 100,
                    ConnectionTimeout = TimeSpan.FromMinutes(5),
                    Enabled = true
                })
                .AddSmartHost(new SmartHostConfiguration
                {
                    Host = "backup2.smtp.provider.com",
                    Port = 2525,
                    Priority = 30,
                    Weight = 50,
                    UseTls = false,
                    MaxConnections = 5,
                    MaxMessagesPerConnection = 50,
                    ConnectionTimeout = TimeSpan.FromMinutes(3),
                    Enabled = true
                })

                // Performance settings
                .MaxConcurrentDeliveries(25)
                .MaxRetries(8)
                .MessageLifetime(TimeSpan.FromDays(5))
                .ConnectionTimeout(TimeSpan.FromMinutes(10))
                .QueueProcessingInterval(TimeSpan.FromSeconds(15))
                .CleanupInterval(TimeSpan.FromHours(2))

                // Security settings
                .EnableTls(true, require: false)
                .RequireAuthentication(true)

                // DNS and MX routing
                .UseMxRouting(true)
                .AddDnsServer(
                    IPAddress.Parse("8.8.8.8"),
                    IPAddress.Parse("8.8.4.4"),
                    IPAddress.Parse("1.1.1.1"))

                // Local domains (not relayed)
                .AddLocalDomains(
                    "company.com",
                    "company.local",
                    "internal.company.com",
                    "localhost")

                // Relay domains (always allowed)
                .AddRelayDomains(
                    "partner1.com",
                    "partner2.net",
                    "subsidiary.org")

                // Relay networks (trusted IPs)
                .AddRelayNetworks(
                    IPAddress.Parse("192.168.1.0"),
                    IPAddress.Parse("192.168.2.0"),
                    IPAddress.Parse("10.0.0.0"))

                // Domain-specific routing
                .AddDomainRoute("gmail.com", "smtp.gmail.com", 587, "gmail_user", "gmail_pass")
                .AddDomainRoute("outlook.com", "smtp-mail.outlook.com", 587, "outlook_user", "outlook_pass")
                .AddDomainRoute("sendgrid.customers.com", "smtp.sendgrid.net", 587, "apikey", "SG.xxx")

                // Bounce and DSN settings
                .EnableBounce(true, "postmaster@company.com")
                .EnableDsn(true)

                .Build();

            // Validate configuration
            try
            {
                relayConfig.Validate();
                Console.WriteLine("[✓] Configuration validation passed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[✗] Configuration validation failed: {ex.Message}");
                return;
            }

            // Create server with custom relay configuration
            ISmtpServer server = new SmtpServerBuilder()
                .Port(25034)
                .ServerName("custom-relay.local")
                .MaxMessageSize(25 * 1024 * 1024) // 25MB
                .MaxConnections(100)
                .ConnectionTimeout(TimeSpan.FromMinutes(5))
                .CommandTimeout(TimeSpan.FromMinutes(1))
                .DataTimeout(TimeSpan.FromMinutes(3))
                .LoggerFactory(loggerFactory)
                .Build()
                .EnableRelay(relayConfig);

            // Additional manual configuration
            server.Configuration.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
            server.Configuration.EnableVerboseLogging = false;
            // RequireEncryption is not supported, use RequireSecureConnection instead
            // server.Configuration.RequireSecureConnection = false;

            // Event handlers for monitoring
            // Note: SessionStarted event is not available in current API
            server.MessageReceived += async (sender, e) =>
            {
                Console.WriteLine($"[MESSAGE] {e.Message.From?.Address} → {string.Join(", ", e.Message.Recipients)}");

                // Determine routing
                foreach (MailAddress recipient in e.Message.Recipients)
                {
                    string domain = recipient.Host;
                    string routing;

                    if (relayConfig.LocalDomains.Contains(domain))
                    {
                        routing = "LOCAL";
                    }
                    else if (relayConfig.DomainRouting.ContainsKey(domain))
                    {
                        routing = $"CUSTOM ({relayConfig.DomainRouting[domain].Host})";
                    }
                    else if (relayConfig.RelayDomains.Contains(domain))
                    {
                        routing = "RELAY (Authorized)";
                    }
                    else if (relayConfig.UseMxRouting)
                    {
                        routing = "MX";
                    }
                    else
                    {
                        routing = "DEFAULT";
                    }

                    Console.WriteLine($"  {recipient.Address}: {routing}");
                }

                await Task.CompletedTask;
            };

            // Start server
            Console.WriteLine();
            Console.WriteLine("[INFO] Starting SMTP server with custom configuration...");
            RelayService relayService = await server.StartWithRelayAsync();
            Console.WriteLine("[INFO] Server started on port 25034");
            Console.WriteLine();

            // Display configuration summary
            DisplayConfigurationSummary(relayConfig);

            // Test the configuration
            Console.WriteLine("[TEST] Sending test messages...");
            await SendTestMessages(25034);

            // Display statistics
            Console.WriteLine();
            Console.WriteLine("[INFO] Server is running with custom configuration");
            Console.WriteLine("[INFO] Press 'S' for statistics, 'C' for config, 'Q' to quit");

            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.Q)
                {
                    break;
                }

                if (key.Key == ConsoleKey.S)
                {
                    await DisplayStatistics(server);
                }

                if (key.Key == ConsoleKey.C)
                {
                    DisplayConfigurationSummary(relayConfig);
                }
            }

            // Stop server
            Console.WriteLine();
            Console.WriteLine("[INFO] Stopping server...");
            await server.StopAsync();
            Console.WriteLine("[INFO] Server stopped.");
        }

        private static void DisplayConfigurationSummary(RelayConfiguration config)
        {
            Console.WriteLine("\n╔═══════════════════════════════════════════════╗");
            Console.WriteLine("║        RELAY CONFIGURATION SUMMARY            ║");
            Console.WriteLine("╠═══════════════════════════════════════════════╣");

            Console.WriteLine("║ GENERAL SETTINGS                              ║");
            Console.WriteLine($"║   Enabled: {config.Enabled,-35}║");
            Console.WriteLine($"║   Local Domain: {config.LocalDomain,-30}║");
            Console.WriteLine($"║   Max Concurrent: {config.MaxConcurrentDeliveries,-28}║");
            Console.WriteLine($"║   Max Retries: {config.MaxRetryCount,-31}║");
            Console.WriteLine($"║   Message Lifetime: {config.MessageLifetime.TotalDays} days{new string(' ', 22)}║");

            Console.WriteLine("║                                               ║");
            Console.WriteLine("║ SMART HOSTS                                   ║");
            if (config.DefaultSmartHost != null)
            {
                Console.WriteLine($"║   Primary: {config.DefaultSmartHost.Host}:{config.DefaultSmartHost.Port,-19}║");
            }
            Console.WriteLine($"║   Backups: {config.SmartHosts.Count} configured{new string(' ', 23)}║");

            Console.WriteLine("║                                               ║");
            Console.WriteLine("║ ROUTING                                       ║");
            Console.WriteLine($"║   MX Routing: {config.UseMxRouting,-32}║");
            Console.WriteLine($"║   DNS Servers: {config.DnsServers.Count,-31}║");
            Console.WriteLine($"║   Domain Routes: {config.DomainRouting.Count,-29}║");

            Console.WriteLine("║                                               ║");
            Console.WriteLine("║ DOMAINS                                       ║");
            Console.WriteLine($"║   Local Domains: {config.LocalDomains.Count,-29}║");
            Console.WriteLine($"║   Relay Domains: {config.RelayDomains.Count,-29}║");
            Console.WriteLine($"║   Relay Networks: {config.RelayNetworks.Count,-28}║");

            Console.WriteLine("║                                               ║");
            Console.WriteLine("║ SECURITY                                      ║");
            Console.WriteLine($"║   Require Auth: {config.RequireAuthentication,-30}║");
            Console.WriteLine($"║   Enable TLS: {config.EnableTls,-32}║");
            Console.WriteLine($"║   Require TLS: {config.RequireTls,-31}║");
            Console.WriteLine($"║   SSL Protocols: {config.SslProtocols,-28}║");

            Console.WriteLine("║                                               ║");
            Console.WriteLine("║ FEATURES                                      ║");
            Console.WriteLine($"║   Bounce Messages: {config.EnableBounceMessages,-27}║");
            Console.WriteLine($"║   DSN: {config.EnableDsn,-39}║");
            Console.WriteLine($"║   Bounce Sender: {config.BounceSender,-29}║");

            Console.WriteLine("╚═══════════════════════════════════════════════╝");
        }

        private static async Task SendTestMessages(int port)
        {
            using SmtpClient client = new("localhost", port)
            {
                EnableSsl = false
            };

            (string, string)[] testCases = new[]
            {
                ("local@company.com", "Local Domain Test"),
                ("relay@partner1.com", "Relay Domain Test"),
                ("mx@example.org", "MX Routing Test"),
                ("custom@gmail.com", "Custom Route Test"),
                ("default@unknown.com", "Default Route Test")
            };

            foreach ((string? to, string? subject) in testCases)
            {
                try
                {
                    MailMessage message = new()
                    {
                        From = new MailAddress("test@custom-relay.company.com"),
                        Subject = subject,
                        Body = $"Testing custom configuration - {DateTime.Now}",
                        IsBodyHtml = false
                    };
                    message.To.Add(to);

                    await client.SendMailAsync(message);
                    Console.WriteLine($"  ✓ {subject}: {to}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ✗ {subject}: {ex.Message}");
                }
            }
        }

        private static async Task DisplayStatistics(ISmtpServer server)
        {
            RelayQueueStatistics? stats = await server.GetRelayStatisticsAsync();
            if (stats == null)
            {
                Console.WriteLine("\n[ERROR] No relay statistics available");
                return;
            }

            Console.WriteLine($"\n[STATISTICS] {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"  Total Messages: {stats.TotalMessages}");
            Console.WriteLine($"  Queued: {stats.QueuedMessages}");
            Console.WriteLine($"  In Progress: {stats.InProgressMessages}");
            Console.WriteLine($"  Delivered: {stats.DeliveredMessages}");
            Console.WriteLine($"  Failed: {stats.FailedMessages}");
            Console.WriteLine($"  Deferred: {stats.DeferredMessages}");
            Console.WriteLine($"  Expired: {stats.ExpiredMessages}");

            if (stats.TotalSize > 0)
            {
                double sizeInMB = stats.TotalSize / (1024.0 * 1024.0);
                Console.WriteLine($"  Total Size: {sizeInMB:F2} MB");
            }

            if (stats.OldestMessageTime.HasValue)
            {
                TimeSpan age = DateTime.UtcNow - stats.OldestMessageTime.Value;
                Console.WriteLine($"  Oldest Message: {age.TotalMinutes:F1} minutes ago");
            }

            if (stats.AverageQueueTime.TotalSeconds > 0)
            {
                Console.WriteLine($"  Avg Queue Time: {stats.AverageQueueTime:mm\\:ss}");
            }

            if (stats.AverageRetryCount > 0)
            {
                Console.WriteLine($"  Avg Retry Count: {stats.AverageRetryCount:F2}");
            }

            if (stats.MessagesByPriority?.Count > 0)
            {
                Console.WriteLine("\n  By Priority:");
                foreach (KeyValuePair<RelayPriority, int> kvp in stats.MessagesByPriority)
                {
                    Console.WriteLine($"    {kvp.Key}: {kvp.Value}");
                }
            }

            if (stats.MessagesBySmartHost?.Count > 0)
            {
                Console.WriteLine("\n  By Smart Host:");
                foreach (KeyValuePair<string, int> kvp in stats.MessagesBySmartHost)
                {
                    Console.WriteLine($"    {kvp.Key}: {kvp.Value}");
                }
            }
        }
    }
}