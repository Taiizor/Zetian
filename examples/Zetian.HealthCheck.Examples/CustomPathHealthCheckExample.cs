using System.Diagnostics;
using System.Net;
using Zetian.Configuration;
using Zetian.HealthCheck.Extensions;

namespace Zetian.HealthCheck.Examples
{
    /// <summary>
    /// Example with custom health check path
    /// </summary>
    public class CustomPathHealthCheckExample
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("=== SMTP Server with Custom Health Check Path ===\n");

            SmtpServerConfiguration config = new()
            {
                Port = 2525,
                ServerName = "Zetian Custom Path Example",
                MaxConnections = 50
            };

            using SmtpServer smtpServer = new(config);
            await smtpServer.StartAsync();
            Console.WriteLine($"SMTP server running on {smtpServer.Endpoint}\n");

            // Configure health check with custom path "/status/"
            HealthCheckServiceOptions serviceOptions = new()
            {
                Prefixes = new()
                {
                    "http://localhost:8080/status/"
                }
            };

            Console.WriteLine("Configuring health check with custom path:");
            Console.WriteLine("  - Base path: /status/ (instead of default /health/)");
            Console.WriteLine("  - Port: 8080");
            Console.WriteLine();

            HealthCheckService healthCheckService = smtpServer.EnableHealthCheck(serviceOptions);

            // Add custom health checks
            healthCheckService.AddHealthCheck("uptime", async (ct) =>
            {
                try
                {
                    TimeSpan uptime = (TimeSpan)(DateTime.UtcNow - smtpServer.StartTime);
                    Dictionary<string, object> data = new()
                    {
                        ["days"] = uptime.Days,
                        ["hours"] = uptime.Hours,
                        ["minutes"] = uptime.Minutes,
                        ["totalSeconds"] = uptime.TotalSeconds
                    };

                    string description = $"Server uptime: {uptime.Days}d {uptime.Hours}h {uptime.Minutes}m";
                    return HealthCheckResult.Healthy(description, data: data);
                }
                catch (Exception ex)
                {
                    return HealthCheckResult.Unhealthy("Failed to get uptime", ex);
                }
            });

            healthCheckService.AddHealthCheck("performance", async (ct) =>
            {
                try
                {
                    Process process = Process.GetCurrentProcess();

                    Dictionary<string, object> data = new()
                    {
                        ["cpuTimeSeconds"] = process.TotalProcessorTime.TotalSeconds,
                        ["threadCount"] = process.Threads.Count,
                        ["handleCount"] = process.HandleCount,
                        ["workingSetMB"] = process.WorkingSet64 / (1024 * 1024),
                        ["gcGen0"] = GC.CollectionCount(0),
                        ["gcGen1"] = GC.CollectionCount(1),
                        ["gcGen2"] = GC.CollectionCount(2)
                    };

                    return HealthCheckResult.Healthy("Performance metrics collected", data: data);
                }
                catch (Exception ex)
                {
                    return HealthCheckResult.Unhealthy("Failed to collect performance metrics", ex);
                }
            });

            // Start health check service
            try
            {
                await healthCheckService.StartAsync();
                Console.WriteLine("✓ Health check service started successfully");
                Console.WriteLine();
            }
            catch (HttpListenerException ex) when (ex.ErrorCode == 5)
            {
                Console.WriteLine("\n❌ Access Denied: Administrator privileges required!");
                Console.WriteLine("\nTo fix this, run one of the following:");
                Console.WriteLine("1. Run this application as Administrator");
                Console.WriteLine("2. Or add URL reservation (run as admin):");
                Console.WriteLine("   netsh http add urlacl url=http://+:8080/status/ user=Everyone");
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

            Console.WriteLine("Available endpoints with custom path:");
            Console.WriteLine("  - http://localhost:8080/status/        (Full health check)");
            Console.WriteLine("  - http://localhost:8080/status/livez   (Liveness probe)");
            Console.WriteLine("  - http://localhost:8080/status/readyz  (Readiness probe)");
            Console.WriteLine();
            Console.WriteLine("Note: The default /health/ path is NOT available!");
            Console.WriteLine();
            Console.WriteLine("You can test with:");
            Console.WriteLine("  curl http://localhost:8080/status/");
            Console.WriteLine("  curl http://localhost:8080/status/livez");
            Console.WriteLine("  curl http://localhost:8080/status/readyz");
            Console.WriteLine();

            // Test the custom path
            try
            {
                using HttpClient client = new();
                HttpResponseMessage response = await client.GetAsync("http://localhost:8080/status/");
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("✓ Self-test successful: Custom path is working!");
                    string json = await response.Content.ReadAsStringAsync();

                    // Pretty print first 500 chars
                    if (json.Length > 500)
                    {
                        Console.WriteLine($"Response preview: {json[..500]}...");
                    }
                    else
                    {
                        Console.WriteLine($"Response: {json}");
                    }
                }
                else
                {
                    Console.WriteLine($"⚠ Self-test failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Self-test error: {ex.Message}");
            }

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