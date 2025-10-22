using Zetian.Extensions;
using Zetian.Server;

namespace Zetian.Examples
{
    /// <summary>
    /// Example demonstrating protocol-level filtering vs event-based filtering
    /// </summary>
    public static class ProtocolLevelFilteringExample
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("=== Protocol-Level Filtering Example ===");
            Console.WriteLine("This example shows the difference between protocol-level and event-based filtering");
            Console.WriteLine();

            // Create storage directory
            string storageDir = Path.Combine(Directory.GetCurrentDirectory(), "protocol_messages");
            Directory.CreateDirectory(storageDir);

            // Create server with PROTOCOL-LEVEL filtering (new approach)
            using SmtpServer server = new SmtpServerBuilder()
                .Port(25)
                .ServerName("Protocol Filter SMTP Server")

                // Protocol-level filtering - rejects at SMTP command level
                .WithFileMessageStore(storageDir, createDateFolders: true)  // Saves during DATA command
                .WithSenderDomainWhitelist("trusted.com", "example.com", "localhost")  // Checked at MAIL FROM
                .WithSenderDomainBlacklist("spam.com", "junk.org")  // Checked at MAIL FROM
                .WithRecipientDomainWhitelist("mydomain.com", "example.com", "localhost")  // Checked at RCPT TO

                // Message size limit
                .MaxMessageSizeMB(25)
                .Build();

            // Add EVENT-BASED filtering (existing approach) for comparison
            server.SaveMessagesToDirectory(Path.Combine(storageDir, "event_based"));  // Additional save via event
            server.AddSpamFilter(new[] { "phishing.net", "malware.org" });  // Additional check after message received

            // Track what happens
            server.SessionCreated += (sender, e) =>
            {
                Console.WriteLine($"[SESSION] New connection from {e.Session.RemoteEndPoint}");
            };

            server.MessageReceived += (sender, e) =>
            {
                Console.WriteLine($"[EVENT-BASED] Message passed all protocol checks and was received:");
                Console.WriteLine($"  From: {e.Message.From?.Address}");
                Console.WriteLine($"  To: {string.Join(", ", e.Message.Recipients)}");
                Console.WriteLine($"  Subject: {e.Message.Subject}");
                Console.WriteLine($"  Note: Event-based filters run here, after full message transfer");
                Console.WriteLine();
            };

            server.ErrorOccurred += (sender, e) =>
            {
                if (e.Exception.Message.Contains("rejected") || e.Exception.Message.Contains("Sender rejected") || e.Exception.Message.Contains("Recipient rejected"))
                {
                    Console.WriteLine($"[PROTOCOL-LEVEL] Rejected at SMTP command level: {e.Exception.Message}");
                    Console.WriteLine($"  Note: Message body was NOT transferred, saving bandwidth");
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine($"[ERROR] {e.Exception.Message}");
                }
            };

            // Start server
            CancellationTokenSource cts = new();
            await server.StartAsync(cts.Token);

            Console.WriteLine($"Server is running on {server.Endpoint}");
            Console.WriteLine();
            Console.WriteLine("=== Protocol-Level Filtering (Early Rejection) ===");
            Console.WriteLine("  Allowed sender domains: trusted.com, example.com, localhost");
            Console.WriteLine("  Blocked sender domains: spam.com, junk.org");
            Console.WriteLine("  Allowed recipient domains: mydomain.com, example.com, localhost");
            Console.WriteLine("  -> Rejects at MAIL FROM/RCPT TO commands (saves bandwidth)");
            Console.WriteLine();
            Console.WriteLine("=== Event-Based Filtering (Late Rejection) ===");
            Console.WriteLine("  Additional spam domains: phishing.net, malware.org");
            Console.WriteLine("  -> Rejects after full message received (more flexible)");
            Console.WriteLine();
            Console.WriteLine("Try sending from:");
            Console.WriteLine("  ✅ user@trusted.com -> user@mydomain.com (will work)");
            Console.WriteLine("  ❌ user@spam.com -> user@mydomain.com (rejected at MAIL FROM)");
            Console.WriteLine("  ❌ user@trusted.com -> user@external.com (rejected at RCPT TO)");
            Console.WriteLine("  ❌ user@phishing.net -> user@mydomain.com (rejected after DATA)");
            Console.WriteLine();
            Console.WriteLine("Press any key to stop...");
            Console.ReadKey();

            await server.StopAsync();
            Console.WriteLine("Server stopped.");
        }
    }
}