using Zetian.AntiSpam.Checkers;
using Zetian.AntiSpam.Extensions;
using Zetian.AntiSpam.Models;
using Zetian.AntiSpam.Services;
using Zetian.Server;

namespace Zetian.AntiSpam.Examples
{
    /// <summary>
    /// Example of training and testing the Bayesian spam filter
    /// </summary>
    public class BayesianTrainingExample
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("Bayesian Filter Training and Testing");
            Console.WriteLine("====================================\n");

            // Create a standalone Bayesian checker for demonstration
            BayesianChecker bayesianChecker = new(new BayesianConfiguration
            {
                SpamThreshold = 0.7,
                UseBiGrams = true,
                MaxTokensToConsider = 20
            });

            // Train the filter with comprehensive dataset
            TrainWithDataset(bayesianChecker);

            // Test the trained filter
            await TestFilter(bayesianChecker);

            // Create and run an SMTP server with the trained filter
            await RunServerWithTrainedFilter(bayesianChecker);
        }

        private static void TrainWithDataset(BayesianChecker checker)
        {
            Console.WriteLine("Training Bayesian filter...\n");

            // Spam training data
            List<string> spamSamples =
            [
                // Financial scams
                "Make money fast! Earn $5000 per week working from home!",
                "Congratulations! You've won $1,000,000 in our lottery!",
                "Get rich quick with this one simple trick!",
                "Financial freedom is just one click away!",
                "Double your income in 30 days guaranteed!",
                
                // Pharmaceutical spam
                "Buy cheap viagra online without prescription",
                "Discount pharmacy - all medications 80% off",
                "Lose weight fast with our miracle pills",
                "Get prescription drugs without a doctor",
                "Natural enhancement products - results guaranteed",
                
                // Phishing attempts
                "URGENT: Your account will be suspended. Click here to verify",
                "Security alert: Suspicious activity detected. Confirm identity now",
                "Your payment has been declined. Update billing information immediately",
                "Tax refund available - claim within 24 hours",
                "Bank notification: Your account has been locked",
                
                // Adult content
                "Hot singles in your area want to meet you",
                "Adult content - must be 18+ to view",
                "Meet lonely housewives tonight",
                "Free adult webcams - no credit card required",
                
                // General spam patterns
                "This is not spam! 100% legitimate offer!",
                "Act now! Limited time offer expires soon!",
                "FREE FREE FREE - No strings attached!",
                "Click here now for amazing deals!!!",
                "You have been specially selected for this offer"
            ];

            // Ham (legitimate) training data
            List<string> hamSamples =
            [
                // Business emails
                "Please find attached the quarterly report for review",
                "Meeting scheduled for tomorrow at 2 PM in conference room B",
                "Thank you for your order. Your receipt is attached",
                "Project update: We've completed the first milestone",
                "Could we reschedule our call to next Tuesday?",
                
                // Development/Technical
                "Pull request #234 has been approved and merged",
                "Build failed on branch develop - please check the logs",
                "New issue created: Bug in authentication module",
                "Code review requested for feature branch",
                "Deployment to staging environment completed successfully",
                
                // Customer service
                "Thank you for contacting our support team",
                "Your ticket has been updated with a response",
                "We appreciate your feedback about our service",
                "Order shipped - tracking number included below",
                "Your subscription has been renewed successfully",
                
                // Personal/Professional
                "Looking forward to our lunch meeting on Friday",
                "Happy birthday! Hope you have a great day",
                "Following up on our conversation from yesterday",
                "Please review and sign the attached contract",
                "Reminder: Team building event next week",
                
                // Notifications
                "Your password will expire in 7 days",
                "Weekly digest of activity in your projects",
                "New comment on your post",
                "Backup completed successfully",
                "System maintenance scheduled for this weekend"
            ];

            // Train with spam samples
            foreach (string spam in spamSamples)
            {
                checker.Train(spam, isSpam: true);
            }
            Console.WriteLine($"✓ Trained with {spamSamples.Count} spam samples");

            // Train with ham samples
            foreach (string ham in hamSamples)
            {
                checker.Train(ham, isSpam: false);
            }
            Console.WriteLine($"✓ Trained with {hamSamples.Count} legitimate samples\n");
        }

        private static async Task TestFilter(BayesianChecker checker)
        {
            Console.WriteLine("Testing Bayesian Filter");
            Console.WriteLine("-----------------------\n");

            var testMessages = new[]
            {
                // Should be detected as spam
                new { Content = "WINNER! You've won £1000! Click here to claim your prize!", ExpectedSpam = true },
                new { Content = "Viagra for sale - no prescription needed", ExpectedSpam = true },
                new { Content = "Make $$$ fast working from home!", ExpectedSpam = true },
                new { Content = "Hot singles near you want to chat", ExpectedSpam = true },
                new { Content = "URGENT: Verify your account or it will be closed", ExpectedSpam = true },
                
                // Should be detected as ham
                new { Content = "Meeting agenda for tomorrow's standup", ExpectedSpam = false },
                new { Content = "Your order has been shipped and will arrive tomorrow", ExpectedSpam = false },
                new { Content = "Please review the attached proposal", ExpectedSpam = false },
                new { Content = "Thank you for your recent purchase", ExpectedSpam = false },
                new { Content = "Reminder: Dentist appointment next Tuesday at 3pm", ExpectedSpam = false },
                
                // Borderline cases
                new { Content = "Special offer just for you - 50% discount this week", ExpectedSpam = true },
                new { Content = "Free trial of our premium service - no credit card required", ExpectedSpam = true }
            };

            int correctPredictions = 0;
            int totalTests = testMessages.Length;

            foreach (var test in testMessages)
            {
                SpamCheckContext context = new()
                {
                    MessageBody = test.Content,
                    Subject = test.Content // Use content as subject for simplicity
                };

                SpamCheckResult result = await checker.CheckAsync(context);
                bool prediction = result.IsSpam;
                bool correct = prediction == test.ExpectedSpam;
                correctPredictions += correct ? 1 : 0;

                Console.WriteLine($"Message: \"{test.Content[..Math.Min(50, test.Content.Length)]}...\"");
                Console.WriteLine($"  Expected: {(test.ExpectedSpam ? "SPAM" : "HAM")}");
                Console.WriteLine($"  Predicted: {(prediction ? "SPAM" : "HAM")} (Score: {result.Score:F1}, Confidence: {result.Confidence:P})");
                Console.WriteLine($"  Result: {(correct ? "✓ CORRECT" : "✗ INCORRECT")}");
                Console.WriteLine();
            }

            double accuracy = (double)correctPredictions / totalTests * 100;
            Console.WriteLine($"Accuracy: {correctPredictions}/{totalTests} ({accuracy:F1}%)\n");
        }

        private static async Task RunServerWithTrainedFilter(BayesianChecker trainedChecker)
        {
            Console.WriteLine("\nStarting SMTP Server with Trained Bayesian Filter");
            Console.WriteLine("==================================================\n");

            // Create SMTP server with the trained Bayesian filter
            using SmtpServer server = new SmtpServerBuilder()
                .Port(2525)
                .ServerName("Bayesian Training Example Server")
                .WithAntiSpam(antiSpam => antiSpam
                    // Add the pre-trained Bayesian checker
                    .AddCustomChecker(trainedChecker)
                    // Optionally add other checkers
                    .AddSpfChecker(spf => spf.Enabled = false)  // Disable for this example
                    .AddRblChecker(rbl => rbl.Enabled = false)   // Disable for this example
                    .WithSpamThreshold(50)
                    .WithRejectThreshold(80)
                )
                .Build();

            // Display results
            server.MessageReceived += (sender, e) =>
            {
                Console.WriteLine($"\n[RECEIVED] From: {e.Message.From?.Address}");
                Console.WriteLine($"[RECEIVED] Subject: {e.Message.Subject}");

                AntiSpamResult? spamResult = e.Message.GetSpamResult();
                if (spamResult != null)
                {
                    SpamCheckResult? bayesianResult = spamResult.CheckResults.FirstOrDefault(r => r.CheckerName == "Bayesian");
                    if (bayesianResult != null)
                    {
                        Console.WriteLine($"[BAYESIAN] Score: {bayesianResult.Score:F1}");
                        Console.WriteLine($"[BAYESIAN] Is Spam: {bayesianResult.IsSpam}");
                        Console.WriteLine($"[BAYESIAN] Confidence: {bayesianResult.Confidence:P}");

                        if (bayesianResult.Details.TryGetValue("bayesian_probability", out object prob))
                        {
                            Console.WriteLine($"[BAYESIAN] Probability: {prob:P}");
                        }
                    }
                }
            };

            await server.StartAsync();
            Console.WriteLine($"Server is running on port 2525");
            Console.WriteLine("The Bayesian filter is trained and active");
            Console.WriteLine("\nPress 'T' to test with a sample message");
            Console.WriteLine("Press 'Q' to quit");

            // Handle user commands
            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.Q)
                {
                    break;
                }
                else if (key.Key == ConsoleKey.T)
                {
                    await TestWithSampleMessage();
                }
            }

            await server.StopAsync();
            Console.WriteLine("\nServer stopped.");
        }

        private static async Task TestWithSampleMessage()
        {
            Console.WriteLine("\n--- Testing with Sample Message ---");
            Console.WriteLine("Enter a message to test (or press Enter for default):");
            string? message = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(message))
            {
                message = "Special offer! Get 50% off all products today only!";
                Console.WriteLine($"Using default message: \"{message}\"");
            }

            // Create a context and test
            SpamCheckContext context = new()
            {
                MessageBody = message,
                Subject = message
            };

            BayesianChecker checker = new();
            TrainWithDataset(checker); // Quick train for testing

            SpamCheckResult result = await checker.CheckAsync(context);

            Console.WriteLine($"\nBayesian Analysis Result:");
            Console.WriteLine($"- Spam Score: {result.Score:F1}/100");
            Console.WriteLine($"- Is Spam: {(result.IsSpam ? "YES" : "NO")}");
            Console.WriteLine($"- Confidence: {result.Confidence:P}");
            Console.WriteLine($"- Action: {result.Action}");

            if (result.Details.TryGetValue("bayesian_probability", out object? probability))
            {
                Console.WriteLine($"- Spam Probability: {probability:P}");
            }

            Console.WriteLine("--- End of Test ---\n");
        }
    }
}
