using Zetian.Configuration;
using Zetian.HealthCheck.Checks;
using Zetian.HealthCheck.Options;
using Zetian.HealthCheck.Services;
using Zetian.Server;

namespace Zetian.AotExample
{
    class Program
    {
        static async Task Main()
        {
            Console.WriteLine("Starting Zetian SMTP Server with AOT Support...");

            // Create SMTP server configuration
            SmtpServerConfiguration config = new()
            {
                Port = 2525,
                MaxConnections = 100,
                MaxConnectionsPerIp = 10,
                ServerName = "aot-smtp.example.com"
            };

            // Create and configure SMTP server
            SmtpServer server = new(config);

            // Add event handlers
            server.MessageReceived += (sender, e) =>
            {
                Console.WriteLine($"Message received from {e.Message.From}");
            };

            // Create health check service (uses reflection for JSON - marked with attributes)
            HealthCheckServiceOptions healthOptions = new()
            {
                Prefixes = new()
                {
                    "http://localhost:8080/"
                }
            };

            HealthCheckService healthService = new(healthOptions);
            healthService.AddHealthCheck("smtp", new SmtpServerHealthCheck(server));

            // Start services
            await server.StartAsync();
            Console.WriteLine($"SMTP Server started on port {config.Port}");
            Console.WriteLine($"Active sessions: {server.ActiveSessionCount}");

            await healthService.StartAsync();
            Console.WriteLine("Health check service started on http://localhost:8080");
            Console.WriteLine("  - Health: http://localhost:8080/health");
            Console.WriteLine("  - Live: http://localhost:8080/livez or http://localhost:8080/health/livez");
            Console.WriteLine("  - Ready: http://localhost:8080/readyz or http://localhost:8080/health/readyz");

            // Keep running until cancelled
            CancellationTokenSource cts = new();
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            Console.WriteLine("\nPress Ctrl+C to stop...");

            try
            {
                await Task.Delay(Timeout.Infinite, cts.Token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Shutting down...");
            }

            // Cleanup
            await healthService.StopAsync();
            await server.StopAsync();
            healthService.Dispose();
            server.Dispose();

            Console.WriteLine("Server stopped.");
        }
    }
}