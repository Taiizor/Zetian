using Microsoft.Extensions.Logging;
using Zetian.Configuration;
using Zetian.HealthCheck.Extensions;
using Zetian.HealthCheck.Models;
using Zetian.HealthCheck.Options;
using Zetian.HealthCheck.Services;
using Zetian.Server;

namespace Zetian.HealthCheck.Examples
{
    /// <summary>
    /// Example demonstrating health check timeout configurations
    /// </summary>
    public class TimeoutHealthCheckExample
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("=== Health Check Timeout Configuration Example ===\n");
            Console.WriteLine("This example demonstrates how health checks handle timeouts.\n");

            ILoggerFactory? loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            // Create SMTP server configuration
            SmtpServerConfiguration config = new()
            {
                ServerName = "Timeout Example SMTP",
                Port = 2525,
                LoggerFactory = loggerFactory
            };

            // Build the server
            SmtpServer server = new SmtpServerBuilder()
                .Port(config.Port)
                .ServerName(config.ServerName)
                .LoggerFactory(config.LoggerFactory)
                .Build();

            // Configure health check with custom timeout settings
            HealthCheckServiceOptions healthCheckOptions = new()
            {
                Prefixes = ["http://localhost:8080/health/"],
                TotalTimeout = TimeSpan.FromSeconds(5),         // Total timeout for all checks
                IndividualCheckTimeout = TimeSpan.FromSeconds(2), // Timeout per check
                FailFastOnTimeout = true,                        // Stop on first timeout
                TimeoutStatusCode = 503,                         // Service Unavailable on timeout
                DegradedStatusCode = 200
            };

            HealthCheckService healthService = new(healthCheckOptions, loggerFactory);

            // Add SMTP health check
            healthService.AddHealthCheck("smtp_server", async (ct) =>
            {
                await Task.Delay(100, ct); // Quick check
                return server.IsRunning
                    ? HealthCheckResult.Healthy("SMTP server is running")
                    : HealthCheckResult.Unhealthy("SMTP server is not running");
            });

            // Add a fast health check
            healthService.AddHealthCheck("fast_check", async (ct) =>
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Fast check started");
                await Task.Delay(500, ct); // 500ms
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Fast check completed");
                return HealthCheckResult.Healthy("Fast check passed");
            });

            // Add a slow health check (will timeout)
            healthService.AddHealthCheck("slow_database", async (ct) =>
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Slow database check started");
                try
                {
                    await Task.Delay(3000, ct); // 3 seconds (exceeds individual timeout of 2s)
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Slow database check completed");
                    return HealthCheckResult.Healthy("Database is responsive");
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Slow database check cancelled");
                    throw;
                }
            });

            // Add another slow check
            healthService.AddHealthCheck("slow_external_api", async (ct) =>
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] External API check started");
                try
                {
                    await Task.Delay(4000, ct); // 4 seconds
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] External API check completed");
                    return HealthCheckResult.Healthy("External API is responsive");
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] External API check cancelled");
                    throw;
                }
            });

            // Add readiness checks with different timeout behavior
            healthService.AddReadinessCheck("quick_ready", async (ct) =>
            {
                await Task.Delay(200, ct);
                return HealthCheckResult.Healthy("Service is ready");
            });

            healthService.AddReadinessCheck("slow_ready", async (ct) =>
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Slow readiness check started");
                try
                {
                    await Task.Delay(2500, ct); // Will timeout
                    return HealthCheckResult.Healthy("Ready but slow");
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Slow readiness check cancelled");
                    throw;
                }
            });

            // Start services
            await server.StartAsync();
            await healthService.StartAsync();

            Console.WriteLine("✅ SMTP Server started on port 2525");
            Console.WriteLine("✅ Health check service started on port 8080\n");

            Console.WriteLine("📍 Configured Timeouts:");
            Console.WriteLine($"   Total Timeout: {healthCheckOptions.TotalTimeout.TotalSeconds} seconds");
            Console.WriteLine($"   Individual Check Timeout: {healthCheckOptions.IndividualCheckTimeout.TotalSeconds} seconds");
            Console.WriteLine($"   Fail Fast on Timeout: {healthCheckOptions.FailFastOnTimeout}");
            Console.WriteLine($"   Timeout Status Code: {healthCheckOptions.TimeoutStatusCode}\n");

            Console.WriteLine("📍 Available endpoints:");
            Console.WriteLine("   http://localhost:8080/health/        - Health check (with timeouts)");
            Console.WriteLine("   http://localhost:8080/health/readyz  - Readiness check");
            Console.WriteLine("   http://localhost:8080/health/livez   - Liveness check\n");

            Console.WriteLine("🧪 Test scenarios:");
            Console.WriteLine("   1. Health check will show some checks timing out");
            Console.WriteLine("   2. Response will include 'timedOut: true' field");
            Console.WriteLine("   3. HTTP status will be 503 on timeout\n");

            Console.WriteLine("💡 Try these commands:");
            Console.WriteLine("   curl -v http://localhost:8080/health/");
            Console.WriteLine("   curl -v http://localhost:8080/health/readyz");
            Console.WriteLine();

            // Demonstrate timeout handling
            Console.WriteLine("📊 Running automated test in 3 seconds...");
            await Task.Delay(3000);

            Console.WriteLine("\n🔍 Making health check request...");
            try
            {
                using HttpClient client = new();
                client.Timeout = TimeSpan.FromSeconds(10);

                HttpResponseMessage healthResponse = await client.GetAsync("http://localhost:8080/health/");
                string healthContent = await healthResponse.Content.ReadAsStringAsync();

                Console.WriteLine($"\n📥 Health Response:");
                Console.WriteLine($"   Status Code: {(int)healthResponse.StatusCode} {healthResponse.StatusCode}");
                Console.WriteLine($"   Content: {healthContent[..Math.Min(200, healthContent.Length)]}...");

                if (healthResponse.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    Console.WriteLine("\n⚠️  Health check returned 503 - Some checks timed out!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error testing health endpoint: {ex.Message}");
            }

            Console.WriteLine("\n🔍 Making readiness check request...");
            try
            {
                using HttpClient client = new();
                client.Timeout = TimeSpan.FromSeconds(10);

                HttpResponseMessage readyResponse = await client.GetAsync("http://localhost:8080/health/readyz");
                string readyContent = await readyResponse.Content.ReadAsStringAsync();

                Console.WriteLine($"\n📥 Readiness Response:");
                Console.WriteLine($"   Status Code: {(int)readyResponse.StatusCode} {readyResponse.StatusCode}");
                Console.WriteLine($"   Content: {readyContent[..Math.Min(200, readyContent.Length)]}...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error testing readiness endpoint: {ex.Message}");
            }

            Console.WriteLine("\nPress any key to stop...");
            Console.ReadKey();

            // Stop services
            await healthService.StopAsync();
            await server.StopAsync();

            Console.WriteLine("\n🛑 Services stopped.");
        }
    }
}