using System;
using System.Threading.Tasks;
using Zetian.Server;
using Zetian.AntiSpam.Extensions;

namespace Zetian.AntiSpam.Examples
{
    /// <summary>
    /// Basic anti-spam setup example
    /// </summary>
    public class BasicAntiSpamExample
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("=== Basic Anti-Spam Example ===\n");

            // Create SMTP server
            var server = new SmtpServerBuilder()
                .Port(25000)
                .ServerName("AntiSpam Test Server")
                .Build();

            // Add anti-spam with default settings
            server.AddAntiSpam();

            // Handle messages
            server.MessageReceived += (sender, e) =>
            {
                Console.WriteLine($"Message from: {e.Message.From?.Address}");
                Console.WriteLine($"Subject: {e.Message.Subject}");
                
                // The anti-spam system will automatically set e.Cancel = true
                // and e.Response if the message is spam
                if (e.Cancel)
                {
                    Console.WriteLine($"[SPAM BLOCKED] {e.Response}");
                }
                else
                {
                    Console.WriteLine("[CLEAN] Message accepted");
                }
            };

            await server.StartAsync();
            Console.WriteLine($"Server running on port 25000 with anti-spam protection");
            Console.WriteLine("Press any key to stop...\n");

            Console.ReadKey();
            await server.StopAsync();
        }
    }
}
