using System.Diagnostics;
using System.Net;
using Zetian.Configuration;
using Zetian.HealthCheck.Extensions;

namespace Zetian.HealthCheck.Examples
{
    /// <summary>
    /// Example with custom health check options
    /// </summary>
    public class CustomHealthCheckExample
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("=== SMTP Server with Custom Health Check Options ===\n");

            SmtpServerConfiguration config = new()
            {
                Port = 2525,
                ServerName = "Zetian Custom Health Check",
                MaxConnections = 100
            };

            using SmtpServer smtpServer = new(config);
            await smtpServer.StartAsync();
            Console.WriteLine($"SMTP server running on {smtpServer.Endpoint}\n");

            // Custom health check options
            SmtpHealthCheckOptions healthCheckOptions = new()
            {
                DegradedThresholdPercent = 60,  // Mark as degraded at 60% utilization
                UnhealthyThresholdPercent = 85, // Mark as unhealthy at 85% utilization
                CheckMemoryUsage = true
            };

            HealthCheckServiceOptions serviceOptions = new()
            {
                Prefixes = new()
                {
                    "http://localhost:8080/health/"
                },
                DegradedStatusCode = 218  // "This is fine" status code
            };

            Console.WriteLine("Configuring health check with custom options:");
            Console.WriteLine("  - Degraded threshold: 60% utilization");
            Console.WriteLine("  - Unhealthy threshold: 85% utilization");
            Console.WriteLine("  - Memory usage tracking: Enabled");
            Console.WriteLine("  - Degraded status code: 218");
            Console.WriteLine();

            HealthCheckService healthCheckService = smtpServer.EnableHealthCheck(serviceOptions, healthCheckOptions);

            try
            {
                await healthCheckService.StartAsync();
                Console.WriteLine("Health check service started with custom configuration");
                Console.WriteLine("Health check available on localhost at port 8080");
                Console.WriteLine();
            }
            catch (HttpListenerException ex) when (ex.ErrorCode == 5)
            {
                Console.WriteLine("\n❌ Access Denied: Administrator privileges required!");
                Console.WriteLine("\nTo fix this, run one of the following:");
                Console.WriteLine("1. Run this application as Administrator");
                Console.WriteLine("2. Or add URL reservation (run as admin):");
                Console.WriteLine("   netsh http add urlacl url=http://+:8080/health/ user=Everyone");
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

            // Add custom health checks
            healthCheckService.AddHealthCheck("system_resources", async (ct) =>
            {
                try
                {
                    // Get system memory info
                    Process process = System.Diagnostics.Process.GetCurrentProcess();
                    long memoryMB = process.WorkingSet64 / (1024 * 1024);

                    Dictionary<string, object> data = new()
                    {
                        ["memoryMB"] = memoryMB,
                        ["threadCount"] = process.Threads.Count,
                        ["handleCount"] = process.HandleCount
                    };

                    if (memoryMB > 500)
                    {
                        return HealthCheckResult.Unhealthy($"High memory usage: {memoryMB}MB", data: data);
                    }

                    if (memoryMB > 200)
                    {
                        return HealthCheckResult.Degraded($"Moderate memory usage: {memoryMB}MB", data: data);
                    }

                    return HealthCheckResult.Healthy($"Memory usage is normal: {memoryMB}MB", data: data);
                }
                catch (Exception ex)
                {
                    return HealthCheckResult.Unhealthy("Failed to check system resources", ex);
                }
            });

            // Add network connectivity check
            healthCheckService.AddHealthCheck("network", async (ct) =>
            {
                try
                {
                    // Simulate network check
                    await Task.Delay(20, ct);

                    Random random = new();
                    int latency = random.Next(1, 100);

                    Dictionary<string, object> data = new()
                    {
                        ["latencyMs"] = latency,
                        ["endpoint"] = "gateway"
                    };

                    if (latency > 50)
                    {
                        return HealthCheckResult.Degraded($"High network latency: {latency}ms", data: data);
                    }

                    return HealthCheckResult.Healthy($"Network is healthy: {latency}ms", data: data);
                }
                catch (Exception ex)
                {
                    return HealthCheckResult.Unhealthy("Network check failed", ex);
                }
            });

            // Health check already started above
            Console.WriteLine("Available endpoints:");
            Console.WriteLine("  - http://localhost:8080/health/");
            Console.WriteLine("  - http://localhost:8080/health/livez");
            Console.WriteLine("  - http://localhost:8080/health/readyz");
            Console.WriteLine();
            Console.WriteLine("You can test with:");
            Console.WriteLine("  curl http://localhost:8080/health/");
            Console.WriteLine();

            // Simulate some activity
            Console.WriteLine("Simulating server activity...");
            await Task.Delay(2000);

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