using Zetian.AntiSpam.Extensions;
using Zetian.Server;

namespace Zetian.AntiSpam.Examples
{
    /// <summary>
    /// Basic example of using Zetian.AntiSpam with default settings
    /// </summary>
    public class BasicAntiSpamExample
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("Starting SMTP Server with Basic Anti-Spam Protection...");

            // Create SMTP server with basic anti-spam protection
            using SmtpServer server = new SmtpServerBuilder()
                .Port(2525)
                .ServerName("Anti-Spam SMTP Server")
                .WithBasicAntiSpam()  // Adds SPF, RBL, and Bayesian checks with default settings
                .Build();

            // Handle messages that pass anti-spam checks
            server.MessageReceived += (sender, e) =>
            {
                Console.WriteLine($"[RECEIVED] From: {e.Message.From}");
                Console.WriteLine($"[RECEIVED] Subject: {e.Message.Subject}");

                // Check if message was marked as spam
                if (e.Message.Headers.ContainsKey("X-Spam-Status"))
                {
                    string spamStatus = e.Message.Headers["X-Spam-Status"];
                    string spamScore = e.Message.Headers.ContainsKey("X-Spam-Score")
                        ? e.Message.Headers["X-Spam-Score"]
                        : "N/A";

                    Console.WriteLine($"[SPAM CHECK] Status: {spamStatus}, Score: {spamScore}");
                }
            };

            // Start the server
            await server.StartAsync();
            Console.WriteLine($"Server is running on port 2525");
            Console.WriteLine("Press 'Q' to quit");

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
    }
}