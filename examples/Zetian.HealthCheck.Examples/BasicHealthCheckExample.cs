using Microsoft.Extensions.Logging;
using System.Net;
using Zetian.Configuration;
using Zetian.HealthCheck.Extensions;
using Zetian.HealthCheck.Models;
using Zetian.HealthCheck.Services;
using Zetian.Server;

namespace Zetian.HealthCheck.Examples
{
    /// <summary>
    /// Basic health check example
    /// </summary>
    public class BasicHealthCheckExample
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("=== SMTP Server with Basic Health Check ===\n");

            // Create logger factory for better visibility
            using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole().SetMinimumLevel(LogLevel.Information);
            });

            // Create SMTP server configuration
            SmtpServerConfiguration config = new()
            {
                Port = 2525,
                ServerName = "Zetian Health Check Example",
                MaxConnections = 50,
                MaxMessageSize = 10 * 1024 * 1024, // 10 MB
                RequireAuthentication = false,
                LoggerFactory = loggerFactory
            };

            // Create and start SMTP server
            using SmtpServer smtpServer = new(config);

            // Add message received handler
            smtpServer.MessageReceived += (sender, e) =>
            {
                Console.WriteLine($"Message received from: {e.Message.From?.Address}");
                Console.WriteLine($"  Subject: {e.Message.Subject}");
                Console.WriteLine($"  Recipients: {string.Join(", ", e.Message.Recipients)}");
            };

            // Start server and health check separately
            Console.WriteLine("Starting SMTP server on port 2525...");
            await smtpServer.StartAsync();
            Console.WriteLine($"SMTP server running on {smtpServer.Endpoint}\n");

            // Enable health check on port 8080
            Console.WriteLine("Enabling health check on http://localhost:8080/health/");
            HealthCheckService healthCheckService = smtpServer.EnableHealthCheck(8080);

            // Add custom health checks
            healthCheckService.AddHealthCheck("disk_space", async (ct) =>
            {
                try
                {
                    DriveInfo drive = new(Path.GetPathRoot(Environment.CurrentDirectory) ?? "C:\\");
                    double freeSpacePercent = (double)drive.AvailableFreeSpace / drive.TotalSize * 100;

                    Dictionary<string, object> data = new()
                    {
                        ["totalSpace"] = drive.TotalSize,
                        ["freeSpace"] = drive.AvailableFreeSpace,
                        ["freeSpacePercent"] = Math.Round(freeSpacePercent, 2)
                    };

                    if (freeSpacePercent < 10)
                    {
                        return HealthCheckResult.Unhealthy($"Low disk space: {freeSpacePercent:F2}%", data: data);
                    }

                    if (freeSpacePercent < 20)
                    {
                        return HealthCheckResult.Degraded($"Disk space is getting low: {freeSpacePercent:F2}%", data: data);
                    }

                    return HealthCheckResult.Healthy($"Disk space is healthy: {freeSpacePercent:F2}%", data: data);
                }
                catch (Exception ex)
                {
                    return HealthCheckResult.Unhealthy("Failed to check disk space", ex);
                }
            });

            // Add database connectivity check (simulated)
            healthCheckService.AddHealthCheck("database", async (ct) =>
            {
                try
                {
                    // Simulate database check
                    await Task.Delay(50, ct);

                    // Random simulation for demonstration
                    Random random = new();
                    int responseTime = random.Next(10, 100);

                    Dictionary<string, object> data = new()
                    {
                        ["responseTimeMs"] = responseTime,
                        ["connectionString"] = "simulated"
                    };

                    if (responseTime > 80)
                    {
                        return HealthCheckResult.Degraded($"Database response is slow: {responseTime}ms", data: data);
                    }

                    return HealthCheckResult.Healthy($"Database is responding well: {responseTime}ms", data: data);
                }
                catch (Exception ex)
                {
                    return HealthCheckResult.Unhealthy("Database connection failed", ex);
                }
            });

            try
            {
                await healthCheckService.StartAsync();
                Console.WriteLine("✓ Health check service started successfully\n");
            }
            catch (HttpListenerException ex) when (ex.ErrorCode == 5)
            {
                Console.WriteLine("\n❌ Access Denied: Cannot start health check service!");
                Console.WriteLine("\nSolution: Run this application as Administrator");
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
                await smtpServer.StopAsync();
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Failed to start health check: {ex.Message}");
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
                await smtpServer.StopAsync();
                return;
            }

            // Print health check endpoints
            Console.WriteLine("Available health check endpoints:");
            Console.WriteLine("  - http://localhost:8080/health/        (Full health check)");
            Console.WriteLine("  - http://localhost:8080/health/livez   (Liveness probe)");
            Console.WriteLine("  - http://localhost:8080/health/readyz  (Readiness probe)");
            Console.WriteLine();

            Console.WriteLine("Server is running. Health check endpoints are available.");
            Console.WriteLine("You can test with: curl http://localhost:8080/health/");
            Console.WriteLine("\nPress any key to stop...");
            Console.ReadKey();

            // Stop services
            Console.WriteLine("\nStopping health check service...");
            await healthCheckService.StopAsync();

            Console.WriteLine("Stopping SMTP server...");
            await smtpServer.StopAsync();

            Console.WriteLine("Example completed.");
        }
    }
}