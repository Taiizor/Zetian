using Zetian.Abstractions;
using Zetian.AntiSpam.Extensions;
using Zetian.Server;

namespace Zetian.AntiSpam.Examples
{
    /// <summary>
    /// Email authentication example using SPF, DKIM, and DMARC
    /// </summary>
    public class EmailAuthenticationExample
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("=== Email Authentication (SPF + DKIM + DMARC) Example ===\n");

            // Create SMTP server
            SmtpServer server = new SmtpServerBuilder()
                .Port(25010)
                .ServerName("Email Authentication Server")
                .Build();

            // Method 1: Add all authentication methods individually
            ConfigureIndividualAuthentication(server);

            // Method 2: Use combined authentication (comment out Method 1 to use)
            // ConfigureCombinedAuthentication(server);

            // Method 3: Use AntiSpam builder with authentication
            // ConfigureWithBuilder(server);

            // Statistics tracking
            AuthenticationStats authStats = new();

            // Handle messages with detailed authentication reporting
            server.MessageReceived += (sender, e) =>
            {
                authStats.Total++;

                Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss}] Authentication Check");
                Console.WriteLine($"From: {e.Message.From?.Address}");
                Console.WriteLine($"To: {string.Join(", ", e.Message.Recipients)}");

                // Display headers relevant to authentication
                if (e.Message.Headers.ContainsKey("DKIM-Signature"))
                {
                    Console.WriteLine("✓ DKIM-Signature present");
                }
                else
                {
                    Console.WriteLine("✗ No DKIM-Signature");
                }

                if (e.Cancel)
                {
                    authStats.Failed++;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\n[AUTHENTICATION FAILED] {e.Response}");
                    Console.ResetColor();
                }
                else
                {
                    authStats.Passed++;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\n[AUTHENTICATION PASSED] Message accepted");
                    Console.ResetColor();
                }

                // Display statistics
                Console.WriteLine($"\nStats: Total={authStats.Total}, Passed={authStats.Passed}, Failed={authStats.Failed}");
                Console.WriteLine($"Pass Rate: {authStats.PassRate:F1}%");
            };

            await server.StartAsync();
            DisplayInformation();

            Console.WriteLine("\nPress any key to stop...\n");
            Console.ReadKey();

            await server.StopAsync();
            DisplayFinalReport(authStats);
        }

        private static void ConfigureIndividualAuthentication(ISmtpServer server)
        {
            Console.WriteLine("Configuration: Individual authentication methods");

            // Add SPF checking
            server.AddSpfCheck(failScore: 50);

            // Add DKIM checking with strict mode
            server.AddDkimCheck(
                failScore: 40,
                strictMode: true);

            // Add DMARC checking with policy enforcement
            server.AddDmarcCheck(
                failScore: 70,
                quarantineScore: 50,
                enforcePolicy: true);
        }

        private static void ConfigureCombinedAuthentication(ISmtpServer server)
        {
            Console.WriteLine("Configuration: Combined email authentication");

            // This adds SPF + DKIM + DMARC in one call
            server.AddEmailAuthentication(
                strictMode: true,      // Strict DKIM validation
                enforcePolicy: true);  // Enforce DMARC policies
        }

        private static void ConfigureWithBuilder(ISmtpServer server)
        {
            Console.WriteLine("Configuration: AntiSpam builder with authentication");

            server.AddAntiSpam(builder => builder
                .EnableEmailAuthentication(strictMode: true, enforcePolicy: true)
                .EnableRbl("zen.spamhaus.org")  // Also add RBL
                .WithOptions(options =>
                {
                    options.RejectThreshold = 60;
                    options.EnableDetailedLogging = true;
                }));
        }

        private static void DisplayInformation()
        {
            Console.WriteLine($"\nServer running on port 25010 with email authentication");
            Console.WriteLine("\n=== Authentication Methods Active ===");
            Console.WriteLine("1. SPF (Sender Policy Framework)");
            Console.WriteLine("   - Validates sender's IP against domain's SPF record");
            Console.WriteLine("   - DNS lookup: TXT record with v=spf1");

            Console.WriteLine("\n2. DKIM (DomainKeys Identified Mail)");
            Console.WriteLine("   - Verifies digital signature in email headers");
            Console.WriteLine("   - DNS lookup: selector._domainkey.domain");
            Console.WriteLine("   - Checks signature validity and expiration");

            Console.WriteLine("\n3. DMARC (Domain-based Message Authentication)");
            Console.WriteLine("   - Combines SPF and DKIM results");
            Console.WriteLine("   - DNS lookup: _dmarc.domain");
            Console.WriteLine("   - Enforces domain policies (none/quarantine/reject)");
            Console.WriteLine("   - Checks alignment of authenticated domains");

            Console.WriteLine("\n=== How It Works ===");
            Console.WriteLine("• SPF checks if the sending server is authorized");
            Console.WriteLine("• DKIM verifies the message hasn't been tampered with");
            Console.WriteLine("• DMARC ensures SPF/DKIM align with From domain");
            Console.WriteLine("• All three work together for complete authentication");

            Console.WriteLine("\n=== Testing ===");
            Console.WriteLine("To test authentication:");
            Console.WriteLine("1. Send from a domain with proper SPF records");
            Console.WriteLine("2. Include valid DKIM signatures");
            Console.WriteLine("3. Have DMARC policy configured");
        }

        private static void DisplayFinalReport(AuthenticationStats stats)
        {
            Console.WriteLine("\n=== Final Authentication Report ===");
            Console.WriteLine($"Total Messages: {stats.Total}");
            Console.WriteLine($"Authenticated: {stats.Passed}");
            Console.WriteLine($"Failed: {stats.Failed}");
            Console.WriteLine($"Pass Rate: {stats.PassRate:F1}%");

            if (stats.Failed > 0)
            {
                Console.WriteLine("\nCommon failure reasons:");
                Console.WriteLine("• No SPF record or IP not authorized");
                Console.WriteLine("• Missing or invalid DKIM signature");
                Console.WriteLine("• DMARC alignment failure");
                Console.WriteLine("• Expired DKIM signatures");
            }
        }

        private class AuthenticationStats
        {
            public int Total { get; set; }
            public int Passed { get; set; }
            public int Failed { get; set; }
            public double PassRate => Total > 0 ? (double)Passed / Total * 100 : 0;
        }
    }
}