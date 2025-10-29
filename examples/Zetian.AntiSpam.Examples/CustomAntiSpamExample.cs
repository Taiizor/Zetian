using System;
using System.Threading.Tasks;
using Zetian.Server;
using Zetian.AntiSpam.Extensions;
using Zetian.AntiSpam.Builders;

namespace Zetian.AntiSpam.Examples
{
    /// <summary>
    /// Custom anti-spam configuration example
    /// </summary>
    public class CustomAntiSpamExample
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("=== Custom Anti-Spam Configuration Example ===\n");

            // Create SMTP server
            var server = new SmtpServerBuilder()
                .Port(25001)
                .ServerName("Custom AntiSpam Server")
                .Build();

            // Configure anti-spam with custom settings
            server.AddAntiSpam(builder => builder
                // SPF with custom scores
                .EnableSpf(
                    failScore: 60,      // Higher score for SPF fail
                    softFailScore: 35,  // Medium score for soft fail
                    neutralScore: 15,   // Low score for neutral
                    noneScore: 10)      // Small penalty for no SPF
                
                // RBL checking with specific providers
                .EnableRbl(
                    "zen.spamhaus.org",     // Spamhaus
                    "bl.spamcop.net",       // SpamCop
                    "b.barracudacentral.org" // Barracuda
                )
                
                // Bayesian filter with custom threshold
                .EnableBayesian(
                    spamThreshold: 0.85,    // 85% confidence for spam
                    unknownWordProbability: 0.4)
                
                // Greylisting with custom delays
                .EnableGreylisting(
                    initialDelay: TimeSpan.FromMinutes(3),
                    whitelistDuration: TimeSpan.FromDays(7),
                    maxRetryTime: TimeSpan.FromHours(2))
                
                // General options
                .WithOptions(options =>
                {
                    options.RejectThreshold = 70;        // Reject at score 70+
                    options.TempFailThreshold = 50;      // Temp fail at 50+
                    options.RunChecksInParallel = true;  // Parallel processing
                    options.CheckerTimeout = TimeSpan.FromSeconds(15);
                    options.EnableDetailedLogging = true;
                }));

            // Statistics tracking
            int totalMessages = 0;
            int spamMessages = 0;
            int cleanMessages = 0;

            // Handle messages
            server.MessageReceived += (sender, e) =>
            {
                totalMessages++;
                
                Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss}] Message #{totalMessages}");
                Console.WriteLine($"From: {e.Message.From?.Address}");
                Console.WriteLine($"To: {string.Join(", ", e.Message.Recipients)}");
                Console.WriteLine($"Subject: {e.Message.Subject}");
                Console.WriteLine($"Size: {e.Message.Size:N0} bytes");
                
                if (e.Cancel)
                {
                    spamMessages++;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[SPAM] {e.Response}");
                    Console.ResetColor();
                }
                else
                {
                    cleanMessages++;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("[CLEAN] Message accepted");
                    Console.ResetColor();
                }
                
                // Display statistics
                double spamRate = totalMessages > 0 ? (double)spamMessages / totalMessages * 100 : 0;
                Console.WriteLine($"\nStats: Total={totalMessages}, Spam={spamMessages}, Clean={cleanMessages}, SpamRate={spamRate:F1}%");
            };

            await server.StartAsync();
            Console.WriteLine($"Server running on port 25001 with custom anti-spam configuration");
            Console.WriteLine("\nAnti-Spam Features Enabled:");
            Console.WriteLine("✓ SPF Validation (custom scores)");
            Console.WriteLine("✓ RBL/DNSBL (3 providers)");
            Console.WriteLine("✓ Bayesian Filter (85% threshold)");
            Console.WriteLine("✓ Greylisting (3 min delay)");
            Console.WriteLine("\nPress any key to stop...\n");

            Console.ReadKey();
            
            // Display final statistics
            var stats = server.GetAntiSpamStatistics();
            Console.WriteLine("\n=== Final Statistics ===");
            Console.WriteLine($"Messages Checked: {stats.MessagesChecked}");
            Console.WriteLine($"Messages Blocked: {stats.MessagesBlocked}");
            Console.WriteLine($"Block Rate: {stats.BlockRate:F1}%");
            
            await server.StopAsync();
        }
    }
}
