using Zetian.AntiSpam.Extensions;
using Zetian.Server;

namespace Zetian.AntiSpam.Examples
{
    /// <summary>
    /// Example of using quarantine mode for spam messages
    /// </summary>
    public class QuarantineExample
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("Starting SMTP Server with Quarantine Mode...");

            // Create quarantine directory
            string quarantinePath = Path.Combine(Directory.GetCurrentDirectory(), "quarantine");
            Directory.CreateDirectory(quarantinePath);
            Console.WriteLine($"Quarantine folder: {quarantinePath}");

            // Create SMTP server with strict anti-spam and quarantine mode
            using SmtpServer server = new SmtpServerBuilder()
                .Port(2525)
                .ServerName("Quarantine SMTP Server")
                .WithAntiSpam(antiSpam => antiSpam
                    .AddDefaultCheckers()
                    .WithSpamThreshold(30)      // Low threshold for demonstration
                    .WithRejectThreshold(90)     // High reject threshold
                    .Configure(config =>
                    {
                        config.QuarantineThreshold = 40;  // Quarantine between 40-90
                    })
                )
                .Build();

            // Enable quarantine mode
            server.UseQuarantineMode(quarantinePath);

            // Track quarantined messages
            int quarantinedCount = 0;
            int processedCount = 0;

            server.MessageReceived += (sender, e) =>
            {
                processedCount++;

                Console.WriteLine($"\n[MESSAGE {processedCount}] From: {e.Message.From?.Address}");
                Console.WriteLine($"[MESSAGE {processedCount}] Subject: {e.Message.Subject}");

                // Check if message was quarantined
                if (e.Message.Headers.ContainsKey("X-Spam-Quarantine"))
                {
                    quarantinedCount++;
                    string quarantineFile = e.Message.Headers["X-Spam-Quarantine"];
                    string spamScore = e.Message.Headers.ContainsKey("X-Spam-Score")
                        ? e.Message.Headers["X-Spam-Score"]
                        : "Unknown";

                    Console.WriteLine($"[QUARANTINED] Message quarantined!");
                    Console.WriteLine($"[QUARANTINED] Spam Score: {spamScore}");
                    Console.WriteLine($"[QUARANTINED] File: {Path.GetFileName(quarantineFile)}");
                    Console.WriteLine($"[QUARANTINED] Total quarantined: {quarantinedCount}");
                }
                else
                {
                    string spamStatus = e.Message.Headers.ContainsKey("X-Spam-Status")
                        ? e.Message.Headers["X-Spam-Status"]
                        : "No";

                    if (spamStatus == "Yes")
                    {
                        Console.WriteLine("[MARKED] Message marked as spam but delivered");
                    }
                    else
                    {
                        Console.WriteLine("[CLEAN] Message passed all checks");
                    }
                }
            };

            // Start the server
            await server.StartAsync();
            Console.WriteLine($"Server is running on port 2525");
            Console.WriteLine("\nQuarantine Configuration:");
            Console.WriteLine("- Messages scoring 40-90 will be quarantined");
            Console.WriteLine("- Messages scoring >90 will be rejected");
            Console.WriteLine("- Messages scoring <40 will be delivered");
            Console.WriteLine($"- Quarantine folder: {quarantinePath}");
            Console.WriteLine("\nPress 'L' to list quarantined messages");
            Console.WriteLine("Press 'C' to clear quarantine folder");
            Console.WriteLine("Press 'Q' to quit");

            // Handle user commands
            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.Q)
                {
                    break;
                }
                else if (key.Key == ConsoleKey.L)
                {
                    ListQuarantinedMessages(quarantinePath);
                }
                else if (key.Key == ConsoleKey.C)
                {
                    ClearQuarantine(quarantinePath);
                    quarantinedCount = 0;
                }
            }

            // Stop the server
            await server.StopAsync();
            Console.WriteLine($"\nServer stopped.");
            Console.WriteLine($"Total messages processed: {processedCount}");
            Console.WriteLine($"Total messages quarantined: {quarantinedCount}");
        }

        private static void ListQuarantinedMessages(string quarantinePath)
        {
            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine("Quarantined Messages:");

            string[] files = Directory.GetFiles(quarantinePath, "*.eml");

            if (files.Length == 0)
            {
                Console.WriteLine("No messages in quarantine.");
            }
            else
            {
                foreach (string file in files)
                {
                    FileInfo fileInfo = new(file);
                    Console.WriteLine($"- {fileInfo.Name} ({fileInfo.Length:N0} bytes) - {fileInfo.CreationTime}");

                    // Try to read basic info from the file
                    try
                    {
                        string[] lines = File.ReadAllLines(file);
                        string? fromLine = lines.FirstOrDefault(l => l.StartsWith("From:", StringComparison.OrdinalIgnoreCase));
                        string? subjectLine = lines.FirstOrDefault(l => l.StartsWith("Subject:", StringComparison.OrdinalIgnoreCase));
                        string? spamScoreLine = lines.FirstOrDefault(l => l.StartsWith("X-Spam-Score:", StringComparison.OrdinalIgnoreCase));

                        if (fromLine != null)
                        {
                            Console.WriteLine($"  {fromLine}");
                        }

                        if (subjectLine != null)
                        {
                            Console.WriteLine($"  {subjectLine}");
                        }

                        if (spamScoreLine != null)
                        {
                            Console.WriteLine($"  {spamScoreLine}");
                        }
                    }
                    catch
                    {
                        // Ignore errors reading file
                    }
                }

                Console.WriteLine($"\nTotal: {files.Length} message(s) in quarantine");
            }

            Console.WriteLine(new string('=', 60));
        }

        private static void ClearQuarantine(string quarantinePath)
        {
            string[] files = Directory.GetFiles(quarantinePath, "*.eml");

            if (files.Length == 0)
            {
                Console.WriteLine("\nQuarantine folder is already empty.");
                return;
            }

            Console.WriteLine($"\nAre you sure you want to delete {files.Length} quarantined message(s)? (Y/N)");
            ConsoleKeyInfo confirm = Console.ReadKey(true);

            if (confirm.Key == ConsoleKey.Y)
            {
                foreach (string file in files)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deleting {Path.GetFileName(file)}: {ex.Message}");
                    }
                }

                Console.WriteLine($"Deleted {files.Length} quarantined message(s).");
            }
            else
            {
                Console.WriteLine("Quarantine clear cancelled.");
            }
        }
    }
}