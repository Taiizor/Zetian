using Zetian.AntiSpam.Checkers;
using Zetian.AntiSpam.Extensions;
using Zetian.Server;

namespace Zetian.AntiSpam.Examples
{
    /// <summary>
    /// Bayesian filter training example
    /// </summary>
    public class BayesianTrainingExample
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("=== Bayesian Filter Training Example ===\n");

            // Create Bayesian filter
            BayesianSpamFilter bayesianFilter = new(
                spamThreshold: 0.9,
                unknownWordProbability: 0.5);

            // Training data samples
            List<string> spamSamples = GetSpamSamples();
            List<string> hamSamples = GetHamSamples();

            Console.WriteLine($"Training with {spamSamples.Count} spam samples...");
            foreach (string spam in spamSamples)
            {
                await bayesianFilter.TrainSpamAsync(spam);
            }

            Console.WriteLine($"Training with {hamSamples.Count} ham samples...");
            foreach (string ham in hamSamples)
            {
                await bayesianFilter.TrainHamAsync(ham);
            }

            // Display statistics
            BayesianStatistics stats = bayesianFilter.GetStatistics();
            Console.WriteLine("\n=== Training Statistics ===");
            Console.WriteLine($"Total Spam Messages: {stats.TotalSpamMessages}");
            Console.WriteLine($"Total Ham Messages: {stats.TotalHamMessages}");
            Console.WriteLine($"Unique Words: {stats.UniqueWords}");

            Console.WriteLine("\nMost Spammy Words:");
            foreach (KeyValuePair<string, double> word in stats.MostSpammyWords)
            {
                Console.WriteLine($"  {word.Key}: {word.Value:P1}");
            }

            Console.WriteLine("\nMost Ham Words:");
            foreach (KeyValuePair<string, double> word in stats.MostHammyWords)
            {
                Console.WriteLine($"  {word.Key}: {word.Value:P1}");
            }

            // Create server with trained filter
            SmtpServer server = new SmtpServerBuilder()
                .Port(25002)
                .ServerName("Bayesian Training Server")
                .Build();

            // Add the trained Bayesian filter
            server.AddSpamChecker(bayesianFilter);

            // Test messages
            List<string> testMessages = GetTestMessages();

            Console.WriteLine("\n=== Testing Messages ===");
            server.MessageReceived += (sender, e) =>
            {
                Console.WriteLine($"\nMessage: {e.Message.Subject}");

                if (e.Cancel)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[SPAM] {e.Response}");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("[HAM] Message looks legitimate");
                }
                Console.ResetColor();
            };

            await server.StartAsync();
            Console.WriteLine($"\nServer running on port 25002 with trained Bayesian filter");
            Console.WriteLine("The filter has been trained and is ready to classify messages");
            Console.WriteLine("\nPress any key to stop...\n");

            Console.ReadKey();
            await server.StopAsync();
        }

        private static List<string> GetSpamSamples()
        {
            return
            [
                // Nigerian prince scam
                @"Subject: URGENT BUSINESS PROPOSAL
                Dear Sir/Madam,
                I am Prince Johnson from Nigeria. I have $10,000,000 USD that I need to transfer.
                Please send your bank account details and a small fee of $500 to process.
                This is 100% legitimate and risk-free opportunity!!!
                Reply immediately for instant wealth!",

                // Lottery scam
                @"Subject: YOU WON $5,000,000!!!
                CONGRATULATIONS!!! You have won the international lottery!
                Click here immediately: http://scam-lottery.fake/claim
                Send $200 processing fee to claim your millions!
                Act now! This offer expires in 24 hours!
                100% guaranteed payout!!!",

                // Pharmaceutical spam
                @"Subject: Cheap Pills - 80% OFF Today Only!
                Buy Viagra, Cialis, and other medications online!
                No prescription needed! Discrete shipping!
                Lowest prices guaranteed!
                Order now: http://fake-pharmacy.scam
                Limited time offer - ACT NOW!!!",

                // Weight loss scam
                @"Subject: Lose 30 Pounds in 30 Days - GUARANTEED!
                Amazing new diet pill burns fat while you sleep!
                No exercise needed! Eat whatever you want!
                Celebrities hate this one weird trick!
                Click here for free trial: http://diet-scam.fake
                Limited supplies - ORDER NOW!!!",

                // Phishing attempt
                @"Subject: Your Account Will Be Suspended
                Dear Customer,
                Your account has been compromised. Click here immediately to verify:
                http://phishing-site.fake/verify
                Enter your password and credit card to confirm identity.
                Failure to act within 24 hours will result in permanent suspension!",

                // Investment scam
                @"Subject: Make $10,000 Per Week From Home!
                Secret trading system revealed!
                No experience necessary! Work only 1 hour per day!
                Guaranteed profits or your money back!
                Join now for only $97: http://trading-scam.fake
                Limited spots available!!!",

                // Adult content spam
                @"Subject: Hot Singles In Your Area Want To Meet!
                Thousands of beautiful women are waiting for you!
                No credit card required! 100% free!
                Click here now: http://dating-scam.fake
                Adults only! Must be 18+",

                // Tech support scam
                @"Subject: WARNING: Your Computer Is Infected!
                We detected 147 viruses on your system!
                Download our antivirus NOW to protect your data!
                http://fake-antivirus.scam/download
                Act immediately before your files are deleted!",

                // SEO spam
                @"Subject: Boost Your Website to #1 on Google!
                Guaranteed first page rankings in 30 days!
                Thousands of backlinks for only $49!
                Increase traffic by 500%!
                Order now: http://seo-scam.fake",

                // Cryptocurrency scam
                @"Subject: Double Your Bitcoin in 24 Hours!
                Exclusive trading algorithm generates 200% returns daily!
                Minimum investment only 0.1 BTC
                Send to: 1FakeWalletAddress123xyz
                Limited time offer - Don't miss out!!!"
            ];
        }

        private static List<string> GetHamSamples()
        {
            return
            [
                // Business email
                @"Subject: Project Status Update - Q4 2024
                Hi Team,
                I wanted to provide an update on our current project status.
                We've completed the design phase and are moving into development.
                The timeline remains on track for December delivery.
                Please let me know if you have any questions.
                Best regards,
                John",

                // Newsletter
                @"Subject: Monthly Newsletter - November Edition
                Dear Subscribers,
                Welcome to our November newsletter. This month we're featuring:
                - Industry trends and analysis
                - Customer success stories
                - Upcoming webinar schedule
                - Product updates and improvements
                Thank you for your continued support.
                The Newsletter Team",

                // Order confirmation
                @"Subject: Order Confirmation #12345
                Thank you for your order!
                Your order has been confirmed and will ship within 2-3 business days.
                Order details:
                - Product: Wireless Headphones
                - Quantity: 1
                - Total: $89.99
                You can track your order at our website.
                Customer Service",

                // Meeting invitation
                @"Subject: Team Meeting - Thursday 2:00 PM
                Hi everyone,
                Please join us for our weekly team meeting.
                Agenda:
                - Sprint review
                - Upcoming milestones
                - Resource planning
                Location: Conference Room B
                See you there,
                Sarah",

                // Personal email
                @"Subject: Weekend Plans
                Hey Mike,
                Are we still on for hiking this weekend?
                The weather forecast looks great for Saturday.
                Let me know what time works for you.
                I can pick you up around 8 AM.
                Talk soon,
                Dave",

                // Support ticket response
                @"Subject: Re: Support Request #789
                Hello,
                Thank you for contacting support.
                We've reviewed your issue and found a solution.
                Please try restarting the application while holding the shift key.
                If the problem persists, we can schedule a call to troubleshoot further.
                Support Team",

                // Job application response
                @"Subject: Thank you for your application
                Dear Applicant,
                We've received your application for the Software Engineer position.
                Our hiring team will review your qualifications.
                We'll contact you within 5-7 business days regarding next steps.
                Thank you for your interest in our company.
                HR Department",

                // Invoice
                @"Subject: Invoice #2024-1123
                Please find attached the invoice for services rendered in October.
                Amount due: $2,500
                Payment terms: Net 30
                Please remit payment to the account details provided.
                Thank you for your business.
                Accounting Department",

                // Event invitation
                @"Subject: You're Invited - Annual Conference 2024
                We're pleased to invite you to our annual conference.
                Date: December 15-17, 2024
                Location: Convention Center
                Registration is now open at our website.
                Early bird discount available until November 30.
                We hope to see you there!",

                // Feedback request
                @"Subject: How was your recent purchase?
                Hi valued customer,
                We hope you're enjoying your recent purchase.
                We'd appreciate if you could take a moment to share your feedback.
                Your opinion helps us improve our products and services.
                Click here to complete a brief survey.
                Thank you,
                Customer Experience Team"
            ];
        }

        private static List<string> GetTestMessages()
        {
            return
            [
                // Should be spam
                "WIN FREE MONEY!!! Click here now for millions!!!",
                "Cheap medications online - no prescription needed!",
                
                // Should be ham
                "Meeting scheduled for tomorrow at 10 AM in the conference room",
                "Thank you for your recent order. It will ship tomorrow."
            ];
        }
    }
}