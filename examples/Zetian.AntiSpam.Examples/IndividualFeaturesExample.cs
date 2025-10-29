using Zetian.Abstractions;
using Zetian.AntiSpam.Abstractions;
using Zetian.AntiSpam.Extensions;
using Zetian.AntiSpam.Models;
using Zetian.Server;

namespace Zetian.AntiSpam.Examples
{
    /// <summary>
    /// Examples of individual anti-spam features
    /// </summary>
    public class IndividualFeaturesExample
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("=== Individual Anti-Spam Features Example ===\n");
            Console.WriteLine("Select a feature to test:");
            Console.WriteLine("1. SPF Validation Only");
            Console.WriteLine("2. RBL/DNSBL Checking Only");
            Console.WriteLine("3. Greylisting Only");
            Console.WriteLine("4. Custom Spam Checker");
            Console.Write("\nChoice: ");

            string? choice = Console.ReadLine();
            Console.Clear();

            switch (choice)
            {
                case "1":
                    await TestSpfValidation();
                    break;
                case "2":
                    await TestRblChecking();
                    break;
                case "3":
                    await TestGreylisting();
                    break;
                case "4":
                    await TestCustomChecker();
                    break;
                default:
                    Console.WriteLine("Invalid choice");
                    break;
            }
        }

        private static async Task TestSpfValidation()
        {
            Console.WriteLine("=== SPF Validation Example ===\n");

            SmtpServer server = new SmtpServerBuilder()
                .Port(25003)
                .ServerName("SPF Test Server")
                .Build();

            // Add only SPF checking
            server.AddSpfCheck(failScore: 60);

            server.MessageReceived += (sender, e) =>
            {
                Console.WriteLine($"Message from: {e.Message.From?.Address}");
                Console.WriteLine($"SPF Check Result:");

                if (e.Cancel)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  [FAIL] {e.Response}");
                    Console.WriteLine("  Explanation: Sender's IP is not authorized by the domain's SPF record");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("  [PASS] SPF check passed or not applicable");
                }
                Console.ResetColor();
            };

            await server.StartAsync();
            Console.WriteLine("Server running with SPF validation only");
            Console.WriteLine("\nSPF validates that the sending server is authorized to send mail for the domain.");
            Console.WriteLine("It checks the domain's DNS records for SPF policies.\n");
            Console.WriteLine("Press any key to stop...");

            Console.ReadKey();
            await server.StopAsync();
        }

        private static async Task TestRblChecking()
        {
            Console.WriteLine("=== RBL/DNSBL Checking Example ===\n");

            SmtpServer server = new SmtpServerBuilder()
                .Port(25004)
                .ServerName("RBL Test Server")
                .Build();

            // Add RBL checking with specific providers
            server.AddRblCheck(
                "zen.spamhaus.org",
                "bl.spamcop.net"
            );

            int checkedCount = 0;
            int blockedCount = 0;

            server.SessionCreated += (sender, e) =>
            {
                Console.WriteLine($"New connection from: {e.Session.RemoteEndPoint}");
            };

            server.MessageReceived += (sender, e) =>
            {
                checkedCount++;
                Console.WriteLine($"\nChecking IP against RBLs...");
                Console.WriteLine($"Client IP: {e.Session.RemoteEndPoint}");

                if (e.Cancel)
                {
                    blockedCount++;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[BLOCKED] {e.Response}");
                    Console.WriteLine("This IP is listed in one or more blacklists");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("[CLEAN] IP not found in RBLs");
                }
                Console.ResetColor();

                Console.WriteLine($"Stats: Checked={checkedCount}, Blocked={blockedCount}");
            };

            await server.StartAsync();
            Console.WriteLine("Server running with RBL checking");
            Console.WriteLine("\nRBL (Realtime Blackhole Lists) check IP addresses against");
            Console.WriteLine("known spam sources and compromised servers.\n");
            Console.WriteLine("Checking against:");
            Console.WriteLine("  - Spamhaus ZEN");
            Console.WriteLine("  - SpamCop");
            Console.WriteLine("\nPress any key to stop...");

            Console.ReadKey();
            await server.StopAsync();
        }

        private static async Task TestGreylisting()
        {
            Console.WriteLine("=== Greylisting Example ===\n");

            SmtpServer server = new SmtpServerBuilder()
                .Port(25005)
                .ServerName("Greylisting Test Server")
                .Build();

            // Add greylisting with short delay for testing
            server.AddGreylisting(initialDelay: TimeSpan.FromMinutes(1));

            Dictionary<string, int> greylistStats = [];

            server.MessageReceived += (sender, e) =>
            {
                string from = e.Message.From?.Address ?? "unknown";

                if (!greylistStats.ContainsKey(from))
                {
                    greylistStats[from] = 0;
                }

                greylistStats[from]++;

                Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss}] Message from: {from}");
                Console.WriteLine($"Attempt #{greylistStats[from]} for this sender");

                if (e.Cancel)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[GREYLISTED] {e.Response}");
                    Console.WriteLine("Legitimate servers will retry after the delay period");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("[ACCEPTED] Sender passed greylisting");
                    Console.WriteLine("This sender is now whitelisted for future messages");
                }
                Console.ResetColor();
            };

            await server.StartAsync();
            Console.WriteLine("Server running with greylisting");
            Console.WriteLine("\nGreylisting temporarily rejects messages from unknown senders.");
            Console.WriteLine("Legitimate servers will retry, while spammers typically won't.");
            Console.WriteLine("Initial delay: 1 minute (for testing - normally 5-15 minutes)\n");
            Console.WriteLine("Try sending multiple messages from the same address to see it work.\n");
            Console.WriteLine("Press any key to stop...");

            Console.ReadKey();
            await server.StopAsync();
        }

        private static async Task TestCustomChecker()
        {
            Console.WriteLine("=== Custom Spam Checker Example ===\n");

            SmtpServer server = new SmtpServerBuilder()
                .Port(25006)
                .ServerName("Custom Checker Server")
                .Build();

            // Create and add a custom spam checker
            KeywordSpamChecker customChecker = new();
            server.AddSpamChecker(customChecker);

            server.MessageReceived += (sender, e) =>
            {
                Console.WriteLine($"\nMessage Subject: {e.Message.Subject}");
                Console.WriteLine($"Message Size: {e.Message.Size} bytes");

                if (e.Cancel)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[SPAM] {e.Response}");
                    Console.WriteLine("Custom rules triggered:");
                    Console.WriteLine("  - Suspicious keywords detected");
                    Console.WriteLine("  - Excessive capitalization");
                    Console.WriteLine("  - Multiple exclamation marks");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("[CLEAN] Message passed custom checks");
                }
                Console.ResetColor();
            };

            await server.StartAsync();
            Console.WriteLine("Server running with custom spam checker");
            Console.WriteLine("\nCustom checker looks for:");
            Console.WriteLine("  - Spam keywords (viagra, lottery, winner, etc.)");
            Console.WriteLine("  - Excessive CAPITALIZATION");
            Console.WriteLine("  - Multiple exclamation marks!!!");
            Console.WriteLine("  - Suspicious phrases\n");
            Console.WriteLine("Try sending messages with these patterns to test.\n");
            Console.WriteLine("Press any key to stop...");

            Console.ReadKey();
            await server.StopAsync();
        }

        /// <summary>
        /// Custom spam checker that looks for keywords
        /// </summary>
        private class KeywordSpamChecker : ISpamChecker
        {
            private readonly string[] _spamKeywords =
            {
                "viagra", "cialis", "lottery", "winner", "congratulations",
                "click here", "act now", "limited time", "free money",
                "weight loss", "make money", "work from home", "bitcoin"
            };

            public string Name => "Keyword Filter";
            public bool IsEnabled { get; set; } = true;

            public Task<SpamCheckResult> CheckAsync(
                ISmtpMessage message,
                ISmtpSession session,
                CancellationToken cancellationToken = default)
            {
                double score = 0;
                List<string> reasons = [];

                // Check subject and body for spam keywords
                string content = $"{message.Subject} {message.TextBody}".ToLower();

                foreach (string keyword in _spamKeywords)
                {
                    if (content.Contains(keyword))
                    {
                        score += 20;
                        reasons.Add($"Keyword '{keyword}' found");
                    }
                }

                // Check for excessive capitalization
                if (message.Subject != null)
                {
                    int capsCount = message.Subject.Count(char.IsUpper);
                    if (capsCount > message.Subject.Length * 0.5)
                    {
                        score += 30;
                        reasons.Add("Excessive capitalization");
                    }
                }

                // Check for multiple exclamation marks
                if (content.Contains("!!!") || content.Contains("???"))
                {
                    score += 25;
                    reasons.Add("Multiple punctuation marks");
                }

                // Check for suspicious URLs
                if (content.Contains("http://") && !content.Contains("https://"))
                {
                    score += 15;
                    reasons.Add("Non-secure URL detected");
                }

                if (score >= 50)
                {
                    return Task.FromResult(
                        SpamCheckResult.Spam(
                            score,
                            $"Custom rules triggered: {string.Join(", ", reasons)}"));
                }

                return Task.FromResult(
                    SpamCheckResult.Clean(score));
            }
        }
    }
}