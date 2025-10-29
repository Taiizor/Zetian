using Zetian.AntiSpam.Extensions;
using Zetian.AntiSpam.Models;
using Zetian.Server;

namespace Zetian.AntiSpam.Examples
{
    /// <summary>
    /// Example of custom anti-spam configuration with specific rules
    /// </summary>
    public class CustomAntiSpamExample
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("Starting SMTP Server with Custom Anti-Spam Configuration...");

            // Create SMTP server with custom anti-spam configuration
            using SmtpServer server = new SmtpServerBuilder()
                .Port(2525)
                .ServerName("Custom Anti-Spam SMTP Server")
                .RequireAuthentication()
                .SimpleAuthentication("user", "password")
                .WithAntiSpam(antiSpam => antiSpam
                    // Configure SPF checking
                    .AddSpfChecker(spf =>
                    {
                        spf.Enabled = true;
                        spf.Weight = 1.5;  // Give more weight to SPF
                        spf.FailAction = SpamAction.Reject;
                        spf.SoftFailAction = SpamAction.Mark;
                        spf.NoRecordAsSpam = false;
                    })
                    // Configure RBL checking
                    .AddRblChecker(rbl =>
                    {
                        rbl.SpamThreshold = 40;  // Lower threshold for stricter checking
                        rbl.RejectThreshold = 2;  // Reject if listed in 2+ RBLs
                        rbl.SkipForAuthenticatedUsers = true;  // Skip RBL for authenticated users

                        // Use only specific RBL servers
                        rbl.Servers.Clear();
                        rbl.Servers.Add(new Checkers.RblServer
                        {
                            Name = "Spamhaus ZEN",
                            Host = "zen.spamhaus.org",
                            Score = 50,
                            Enabled = true
                        });
                        rbl.Servers.Add(new Checkers.RblServer
                        {
                            Name = "SpamCop",
                            Host = "bl.spamcop.net",
                            Score = 30,
                            Enabled = true
                        });
                    })
                    // Configure Bayesian filtering
                    .AddBayesianChecker(bayes =>
                    {
                        bayes.SpamThreshold = 0.75;  // 75% probability = spam
                        bayes.HighConfidenceThreshold = 0.90;  // Auto-reject at 90%
                        bayes.UseBiGrams = true;
                        bayes.MaxTokensToConsider = 20;
                    })
                    // Add custom content filter
                    .AddFunctionChecker("ContentFilter", context =>
                    {
                        string content = context.MessageBody?.ToLower() ?? "";
                        string subject = context.Subject?.ToLower() ?? "";

                        // Check for suspicious phrases
                        string[] suspiciousPhrases = new[]
                        {
                            "click here now",
                            "limited time offer",
                            "act now",
                            "congratulations winner",
                            "100% free"
                        };

                        double score = 0.0;
                        List<string> reasons = [];

                        foreach (string? phrase in suspiciousPhrases)
                        {
                            if (content.Contains(phrase) || subject.Contains(phrase))
                            {
                                score += 20;
                                reasons.Add($"Contains '{phrase}'");
                            }
                        }

                        // Check for excessive capitalization
                        if (subject.Length > 0)
                        {
                            int capsCount = subject.Count(char.IsUpper);
                            double capsRatio = (double)capsCount / subject.Length;
                            if (capsRatio > 0.5)
                            {
                                score += 30;
                                reasons.Add("Excessive capitalization in subject");
                            }
                        }

                        return score >= 50
                            ? SpamCheckResult.Spam("ContentFilter", score, reasons.ToArray())
                            : SpamCheckResult.NotSpam("ContentFilter", score);
                    })
                    // Configure thresholds
                    .WithSpamThreshold(40)      // Mark as spam above 40
                    .WithRejectThreshold(70)     // Reject above 70
                    .RunInParallel(true)         // Run checks in parallel
                    .WithCheckerTimeout(TimeSpan.FromSeconds(5))  // 5 second timeout per checker
                )
                .Build();

            // Add content-based filtering for specific keywords
            server.AddContentFilter(
                spamKeywords: new[] { "viagra", "casino", "lottery", "bitcoin", "forex" },
                scorePerKeyword: 15.0
            );

            // Add subject line filtering with regex patterns
            server.AddSubjectFilter(new[]
            {
                @"^\[SPAM\]",                    // Starts with [SPAM]
                @"(FREE|WINNER|URGENT)",         // Contains these words
                @"\${2,}",                        // Two or more dollar signs
                @"[!]{3,}",                       // Three or more exclamation marks
                @"\d{5,}.*\$"                     // 5+ digits followed by dollar sign
            });

            // Train the Bayesian filter with sample data
            TrainBayesianFilter(server);

            // Handle message events
            server.MessageReceived += (sender, e) =>
            {
                Console.WriteLine("\n" + new string('=', 60));
                Console.WriteLine($"[RECEIVED] From: {e.Message.From}");
                Console.WriteLine($"[RECEIVED] Subject: {e.Message.Subject}");

                // Get anti-spam results from message
                Services.AntiSpamResult? spamResult = e.Message.GetSpamResult();
                if (spamResult != null)
                {
                    Console.WriteLine($"[SPAM CHECK] Overall Score: {spamResult.TotalScore:F1}");
                    Console.WriteLine($"[SPAM CHECK] Is Spam: {spamResult.IsSpam}");
                    Console.WriteLine($"[SPAM CHECK] Action: {spamResult.Action}");
                    Console.WriteLine($"[SPAM CHECK] Confidence: {spamResult.Confidence:P}");

                    // Show individual checker results
                    Console.WriteLine("\nChecker Results:");
                    foreach (SpamCheckResult check in spamResult.CheckResults)
                    {
                        Console.WriteLine($"  - {check.CheckerName}: {check.Score:F1} (Spam: {check.IsSpam})");
                        if (check.Reasons.Any())
                        {
                            foreach (string reason in check.Reasons)
                            {
                                Console.WriteLine($"    * {reason}");
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("[SPAM CHECK] No spam check results available");
                }

                Console.WriteLine(new string('=', 60));
            };

            // Start the server
            await server.StartAsync();
            Console.WriteLine($"Server is running on port 2525");
            Console.WriteLine("Authentication required: user/password");
            Console.WriteLine("\nAnti-Spam Configuration:");
            Console.WriteLine("- SPF checking: Enabled (weight: 1.5)");
            Console.WriteLine("- RBL checking: Spamhaus, SpamCop");
            Console.WriteLine("- Bayesian filter: Trained with sample data");
            Console.WriteLine("- Content filter: Keyword checking");
            Console.WriteLine("- Subject filter: Pattern matching");
            Console.WriteLine("- Spam threshold: 40 (mark), 70 (reject)");
            Console.WriteLine("\nPress 'Q' to quit");

            // Wait for user to quit
            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Q)
                {
                    break;
                }
            }

            // Stop the server
            await server.StopAsync();
            Console.WriteLine("Server stopped.");
        }

        private static void TrainBayesianFilter(SmtpServer server)
        {
            Console.WriteLine("Training Bayesian filter...");

            // Train with spam samples
            string[] spamSamples = new[]
            {
                "Get rich quick! Make $5000 per week from home!",
                "Congratulations! You've won the lottery! Click here to claim.",
                "Buy cheap viagra online. No prescription needed.",
                "Hot singles in your area want to meet you!",
                "This is not a scam! Send us your bank details.",
                "URGENT: Your account will be suspended. Verify now!",
                "Lose 30 pounds in 30 days! Guaranteed results!",
                "Free iPhone! Just pay shipping and handling.",
                "Nigerian prince needs your help transferring funds.",
                "Earn money fast! Work from home opportunity!"
            };

            // Train with ham (legitimate) samples
            string[] hamSamples = new[]
            {
                "Meeting scheduled for tomorrow at 2 PM",
                "Please review the attached quarterly report",
                "Thank you for your order. Your receipt is attached.",
                "Project update: We've completed phase 1",
                "Can we reschedule our call to next week?",
                "Here's the presentation for Monday's meeting",
                "Your pull request has been approved and merged",
                "Reminder: Team lunch on Friday at noon",
                "Please find the signed contract attached",
                "Following up on our discussion from yesterday"
            };

            // Train the filter
            foreach (string? spam in spamSamples)
            {
                server.TrainBayesianFilter(spam, isSpam: true);
            }

            foreach (string? ham in hamSamples)
            {
                server.TrainBayesianFilter(ham, isSpam: false);
            }

            Console.WriteLine($"Trained with {spamSamples.Length} spam and {hamSamples.Length} ham samples");
        }
    }
}