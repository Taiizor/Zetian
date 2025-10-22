using Zetian.Models;
using Zetian.Server;

namespace Zetian.Examples
{
    /// <summary>
    /// Authenticated SMTP server example - requires authentication
    /// </summary>
    public static class AuthenticatedExample
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("Starting Authenticated SMTP Server on port 587...");
            Console.WriteLine("Username: admin");
            Console.WriteLine("Password: password123");
            Console.WriteLine();

            // Create an authenticated SMTP server
            using SmtpServer server = new SmtpServerBuilder()
                .Port(587)
                .ServerName("Authenticated SMTP Server")
                .MaxMessageSizeMB(25)
                .RequireAuthentication()
                .AddAuthenticationMechanism("PLAIN")
                .AddAuthenticationMechanism("LOGIN")
                .AuthenticationHandler(async (username, password) =>
                {
                    Console.WriteLine($"[AUTH] Authentication attempt - Username: {username}");

                    // Simple authentication logic
                    if (username == "admin" && password == "password123")
                    {
                        Console.WriteLine($"[AUTH] Authentication successful for {username}");
                        return AuthenticationResult.Succeed(username);
                    }

                    Console.WriteLine($"[AUTH] Authentication failed for {username}");
                    return AuthenticationResult.Fail("Invalid credentials");
                })
                .AllowPlainTextAuthentication() // For demo purposes only!
                .Build();

            // Subscribe to events
            server.SessionCreated += (sender, e) =>
            {
                Console.WriteLine($"[SESSION] New connection from {e.Session.RemoteEndPoint}");
            };

            server.MessageReceived += (sender, e) =>
            {
                Console.WriteLine($"[MESSAGE] Received authenticated message:");
                Console.WriteLine($"  Authenticated User: {e.Session.AuthenticatedIdentity}");
                Console.WriteLine($"  From: {e.Message.From?.Address}");
                Console.WriteLine($"  To: {string.Join(", ", e.Message.Recipients)}");
                Console.WriteLine($"  Subject: {e.Message.Subject}");
                Console.WriteLine();
            };

            // Start the server
            CancellationTokenSource cts = new();
            await server.StartAsync(cts.Token);

            Console.WriteLine($"Server is running on {server.Endpoint}");
            Console.WriteLine("Test with telnet or email client:");
            Console.WriteLine("  telnet localhost 587");
            Console.WriteLine("  EHLO client");
            Console.WriteLine("  AUTH LOGIN");
            Console.WriteLine("  (enter base64 encoded username and password)");
            Console.WriteLine();
            Console.WriteLine("Press any key to stop...");
            Console.ReadKey();

            await server.StopAsync();
            Console.WriteLine("Server stopped.");
        }
    }
}