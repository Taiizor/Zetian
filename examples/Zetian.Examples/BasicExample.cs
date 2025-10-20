namespace Zetian.Examples
{
    /// <summary>
    /// Basic SMTP server example - accepts all emails without authentication
    /// </summary>
    public static class BasicExample
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("Starting Basic SMTP Server on port 25...");
            Console.WriteLine("This server accepts all emails without authentication.");
            Console.WriteLine();

            // Create a basic SMTP server
            using SmtpServer server = new SmtpServerBuilder()
                .Port(25)
                .ServerName("Basic SMTP Server")
                .MaxMessageSizeMB(10)
                .MaxRecipients(100)
                .Build();

            // Subscribe to events
            server.SessionCreated += (sender, e) =>
            {
                Console.WriteLine($"[SESSION] New connection from {e.Session.RemoteEndPoint}");
            };

            server.SessionCompleted += (sender, e) =>
            {
                Console.WriteLine($"[SESSION] Connection closed from {e.Session.RemoteEndPoint}");
            };

            server.MessageReceived += (sender, e) =>
            {
                Console.WriteLine($"[MESSAGE] Received message:");
                Console.WriteLine($"  ID: {e.Message.Id}");
                Console.WriteLine($"  From: {e.Message.From?.Address ?? "Unknown"}");
                Console.WriteLine($"  To: {string.Join(", ", e.Message.Recipients)}");
                Console.WriteLine($"  Subject: {e.Message.Subject ?? "No subject"}");
                Console.WriteLine($"  Size: {e.Message.Size} bytes");
                Console.WriteLine();
            };

            server.ErrorOccurred += (sender, e) =>
            {
                Console.WriteLine($"[ERROR] {e.Exception.Message}");
            };

            // Start the server
            CancellationTokenSource cts = new();
            await server.StartAsync(cts.Token);

            Console.WriteLine($"Server is running on {server.Endpoint}");
            Console.WriteLine("Press any key to stop...");
            Console.ReadKey();

            // Stop the server
            await server.StopAsync();
            Console.WriteLine("Server stopped.");
        }
    }
}