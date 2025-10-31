using Zetian.AntiSpam.Builders;
using Zetian.AntiSpam.Checkers;
using Zetian.AntiSpam.Extensions;
using Zetian.AntiSpam.Services;
using Zetian.Server;

namespace Zetian.AntiSpam.Examples
{
    internal class Testing
    {
        public static async Task Test()
        {
            // Create SMTP server
            SmtpServer server = new SmtpServerBuilder()
                .Port(25011)
                .ServerName("Test Server")
                .Build();

            // Create AntiSpam service with builder
            AntiSpamBuilder antiSpamBuilder = new AntiSpamBuilder()
                .EnableSpf()
                .EnableDkim()
                .EnableDmarc()
                .EnableBayesian()
                .EnableRbl();

            AntiSpamService antiSpamService = antiSpamBuilder.Build();

            // Add anti-spam to server
            server.AddAntiSpam(builder => builder
                .EnableBayesian()
                .EnableSpf()
                .EnableDkim()
                .EnableDmarc());

            // Get anti-spam statistics
            // NOTE: This currently returns empty stats because GetAntiSpamService() is not fully implemented
            // The internal service tracking needs to be properly implemented for this to work
            AntiSpamStatistics stats = server.GetAntiSpamStatistics();
            Console.WriteLine("\n=== Anti-Spam Statistics ===");
            Console.WriteLine($"Messages Checked: {stats.MessagesChecked}");
            Console.WriteLine($"Messages Blocked: {stats.MessagesBlocked}");
            Console.WriteLine($"Block Rate: {stats.BlockRate:F1}%");
            Console.WriteLine("(Note: Statistics are currently empty due to implementation limitations)");

            // If you need to work with Bayesian directly, create it separately
            BayesianSpamFilter bayesianFilter = new();

            // Train Bayesian filter
            await TrainBayesianFilter(bayesianFilter);

            // Get Bayesian statistics
            BayesianStatistics bayesianStats = bayesianFilter.GetStatistics();
            Console.WriteLine("\n=== Bayesian Statistics ===");
            Console.WriteLine($"Total Spam Messages: {bayesianStats.TotalSpamMessages}");
            Console.WriteLine($"Total Ham Messages: {bayesianStats.TotalHamMessages}");
            Console.WriteLine($"Unique Words: {bayesianStats.UniqueWords}");

            // Display most spammy words
            if (bayesianStats.MostSpammyWords.Any())
            {
                Console.WriteLine("\nMost Spammy Words:");
                foreach (KeyValuePair<string, double> word in bayesianStats.MostSpammyWords.Take(5))
                {
                    Console.WriteLine($"  {word.Key}: {word.Value:P1}");
                }
            }

            await server.StartAsync();
            Console.WriteLine($"\nServer running on port 25011");
            Console.WriteLine("Press any key to stop...");
            Console.ReadKey();
            await server.StopAsync();
        }

        private static async Task TrainBayesianFilter(BayesianSpamFilter filter)
        {
            // Sample spam training
            string[] spamSamples = new[]
            {
                "Get rich quick! Make money fast!",
                "Viagra pills cheap online pharmacy",
                "You won the lottery! Click here now!",
                "Free money! No credit check required!"
            };

            // Sample ham training
            string[] hamSamples = new[]
            {
                "Meeting scheduled for tomorrow at 2 PM",
                "Please review the attached document",
                "Thank you for your recent order",
                "Project update: We're on track for delivery"
            };

            Console.WriteLine("\n=== Training Bayesian Filter ===");

            foreach (string? spam in spamSamples)
            {
                await filter.TrainSpamAsync(spam);
            }
            Console.WriteLine($"Trained with {spamSamples.Length} spam samples");

            foreach (string? ham in hamSamples)
            {
                await filter.TrainHamAsync(ham);
            }
            Console.WriteLine($"Trained with {hamSamples.Length} ham samples");
        }
    }
}