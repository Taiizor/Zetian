using System.Net;
using Zetian.Extensions;
using Zetian.Models;
using Zetian.RateLimiting;
using Zetian.Server;

namespace Zetian.Examples
{
    /// <summary>
    /// Rate limited SMTP server example
    /// </summary>
    public static class RateLimitedExample
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("Starting Rate Limited SMTP Server on port 25...");
            Console.WriteLine("Rate limit: 5 messages per minute per IP");
            Console.WriteLine();

            // Create rate limiter
            InMemoryRateLimiter rateLimiter = new(
                RateLimitConfiguration.PerMinute(5)
            );

            // Create SMTP server with rate limiting
            using SmtpServer server = new SmtpServerBuilder()
                .Port(25)
                .ServerName("Rate Limited SMTP Server")
                .MaxConnections(5)
                .MaxMessageSizeMB(10)
                .Build();

            // Add rate limiting
            server.AddRateLimiting(rateLimiter);

            // Subscribe to events
            server.SessionCreated += async (sender, e) =>
            {
                Console.WriteLine($"[SESSION] New connection from {e.Session.RemoteEndPoint}");

                // Check rate limit on connection
                if (e.Session.RemoteEndPoint is IPEndPoint ipEndPoint)
                {
                    int remaining = await rateLimiter.GetRemainingAsync(ipEndPoint.Address.ToString());
                    if (remaining <= 0)
                    {
                        Console.WriteLine($"  [RATE LIMIT] Connection from {ipEndPoint.Address} rejected - rate limit exceeded!");
                    }
                }
            };

            server.MessageReceived += async (sender, e) =>
            {
                // Only log if message wasn't cancelled by rate limiting
                if (!e.Cancel && e.Session.RemoteEndPoint is IPEndPoint ipEndPoint)
                {
                    int remaining = await rateLimiter.GetRemainingAsync(ipEndPoint.Address.ToString());

                    Console.WriteLine($"[MESSAGE] Received message:");
                    Console.WriteLine($"  From IP: {ipEndPoint.Address}");
                    Console.WriteLine($"  Subject: {e.Message.Subject}");
                    Console.WriteLine($"  Remaining quota: {remaining} messages");

                    // Check if this was the last allowed message
                    if (remaining == 0)
                    {
                        Console.WriteLine($"  [WARNING] IP {ipEndPoint.Address} has reached rate limit!");
                    }
                    Console.WriteLine();
                }
                else if (e.Cancel && e.Session.RemoteEndPoint is IPEndPoint rejectedIp)
                {
                    Console.WriteLine($"[MESSAGE] Rejected message from {rejectedIp.Address} - rate limit exceeded");
                    Console.WriteLine();
                }
            };

            server.ErrorOccurred += (sender, e) =>
            {
                Console.WriteLine($"[ERROR] {e.Exception.Message}");
            };

            // Start the server
            CancellationTokenSource cts = new();
            await server.StartAsync(cts.Token);

            Console.WriteLine($"Server is running on {server.Endpoint}");
            Console.WriteLine("Try sending multiple emails quickly to test rate limiting.");
            Console.WriteLine("Press any key to stop...");
            Console.ReadKey();

            await server.StopAsync();
            rateLimiter.Dispose();
            Console.WriteLine("Server stopped.");
        }
    }
}