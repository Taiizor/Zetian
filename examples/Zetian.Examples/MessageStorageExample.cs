using Zetian.Extensions;

namespace Zetian.Examples
{
    /// <summary>
    /// SMTP server that stores received messages to disk
    /// </summary>
    public static class MessageStorageExample
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("Starting SMTP Server with Message Storage on port 25...");

            // Create storage directory
            string storageDir = Path.Combine(Directory.GetCurrentDirectory(), "smtp_messages");
            Directory.CreateDirectory(storageDir);

            Console.WriteLine($"Messages will be saved to: {storageDir}");
            Console.WriteLine();

            // Create SMTP server with message storage
            using SmtpServer server = new SmtpServerBuilder()
                .Port(25)
                .ServerName("Storage SMTP Server")
                .MaxMessageSizeMB(100)
                .Build();

            // Add message storage
            server.SaveMessagesToDirectory(storageDir);

            // Add domain filtering
            server.AddAllowedDomains("example.com", "test.com", "localhost");

            // Add spam filter
            server.AddSpamFilter(new[] { "spam.com", "junk.org" });

            // Add size filter (max 50MB)
            server.AddSizeFilter(50 * 1024 * 1024);

            // Subscribe to events
            server.SessionCreated += (sender, e) =>
            {
                Console.WriteLine($"[SESSION] New connection from {e.Session.RemoteEndPoint}");
            };

            server.MessageReceived += (sender, e) =>
            {
                Console.WriteLine($"[MESSAGE] Received and saved message:");
                Console.WriteLine($"  ID: {e.Message.Id}");
                Console.WriteLine($"  From: {e.Message.From?.Address}");
                Console.WriteLine($"  To: {string.Join(", ", e.Message.Recipients)}");
                Console.WriteLine($"  Subject: {e.Message.Subject}");
                Console.WriteLine($"  Size: {e.Message.Size:N0} bytes");
                Console.WriteLine($"  Has Attachments: {e.Message.HasAttachments}");

                if (e.Message.HasAttachments)
                {
                    Console.WriteLine($"  Attachment Count: {e.Message.AttachmentCount}");
                }

                string fileName = $"{e.Message.Id}.eml";
                Console.WriteLine($"  Saved as: {Path.Combine(storageDir, fileName)}");
                Console.WriteLine();
            };

            // Start the server
            CancellationTokenSource cts = new();
            await server.StartAsync(cts.Token);

            Console.WriteLine($"Server is running on {server.Endpoint}");
            Console.WriteLine("Allowed domains: example.com, test.com, localhost");
            Console.WriteLine("Blocked domains: spam.com, junk.org");
            Console.WriteLine("Max message size: 50MB");
            Console.WriteLine();
            Console.WriteLine("Press any key to stop...");
            Console.ReadKey();

            await server.StopAsync();
            Console.WriteLine("Server stopped.");
            Console.WriteLine($"Check {storageDir} for saved messages.");
        }
    }
}