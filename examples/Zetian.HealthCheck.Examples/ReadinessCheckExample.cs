using Microsoft.Extensions.Logging;
using Zetian.Configuration;
using Zetian.Extensions;
using Zetian.HealthCheck.Extensions;
using Zetian.HealthCheck.Models;
using Zetian.HealthCheck.Services;
using Zetian.Models;
using Zetian.Server;
using Zetian.Storage;

namespace Zetian.HealthCheck.Examples
{
    /// <summary>
    /// Example demonstrating custom readiness checks for Kubernetes deployments
    /// </summary>
    public class ReadinessCheckExample
    {
        // Simulated external service states
        private static bool _databaseReady = false;
        private static bool _cacheReady = false;
        private static bool _warmupComplete = false;
        private static int _activeConnections = 0;
        private static readonly int _maxConnections = 100;

        public static async Task RunAsync()
        {
            Console.WriteLine("=== Readiness Check Example ===\n");
            Console.WriteLine("This example demonstrates the difference between health and readiness checks.");
            Console.WriteLine("Readiness checks are used by Kubernetes to determine if a pod should receive traffic.\n");

            ILoggerFactory? loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            // Create SMTP server configuration
            SmtpServerConfiguration config = new()
            {
                ServerName = "Readiness Example SMTP",
                Port = 2525,
                LoggerFactory = loggerFactory
            };

            // Build the server with basic components
            SmtpServer server = new SmtpServerBuilder()
                .Port(config.Port)
                .ServerName(config.ServerName)
                .LoggerFactory(config.LoggerFactory)
                .AuthenticationHandler(AuthenticateUser)
                .RequireAuthentication(true)
                .AllowPlainTextAuthentication(true)
                .MessageStore(new NullMessageStore())
                .Build();

            // Add rate limiting using extension method
            server.AddRateLimiting(RateLimitConfiguration.PerMinute(100));

            // Enable health check with readiness checks
            HealthCheckService healthService = server.EnableHealthCheck(8080);

            // Add standard health checks (these will appear in /health)
            healthService.AddHealthCheck("smtp_server", async (ct) =>
            {
                // Basic SMTP server health
                return server.IsRunning
                    ? HealthCheckResult.Healthy("SMTP server is running")
                    : HealthCheckResult.Unhealthy("SMTP server is not running");
            });

            healthService.AddHealthCheck("memory", async (ct) =>
            {
                // Memory check
                GC.Collect();
                long memoryUsed = GC.GetTotalMemory(false);
                double memoryMB = memoryUsed / (1024.0 * 1024.0);

                if (memoryMB > 500)
                {
                    return HealthCheckResult.Unhealthy($"High memory usage: {memoryMB:F2} MB");
                }
                if (memoryMB > 300)
                {
                    return HealthCheckResult.Degraded($"Elevated memory usage: {memoryMB:F2} MB");
                }

                return HealthCheckResult.Healthy($"Memory usage: {memoryMB:F2} MB");
            });

            // Add readiness-specific checks (these will appear in /readyz)
            // These checks are more strict and determine if the service can handle traffic
            healthService.AddReadinessCheck("database", async (ct) =>
            {
                // Check database connection
                if (!_databaseReady)
                {
                    return HealthCheckResult.Unhealthy("Database connection not established");
                }

                // Simulate database ping
                await Task.Delay(10, ct);
                return HealthCheckResult.Healthy("Database is ready");
            });

            healthService.AddReadinessCheck("cache", async (ct) =>
            {
                // Check cache service
                if (!_cacheReady)
                {
                    return HealthCheckResult.Unhealthy("Cache service not available");
                }

                await Task.Delay(5, ct);
                return HealthCheckResult.Healthy("Cache is ready");
            });

            healthService.AddReadinessCheck("warmup", async (ct) =>
            {
                // Check if warmup is complete
                if (!_warmupComplete)
                {
                    return HealthCheckResult.Unhealthy("Application warmup not complete");
                }

                return HealthCheckResult.Healthy("Warmup complete");
            });

            healthService.AddReadinessCheck("capacity", async (ct) =>
            {
                // Check if we have capacity for more connections
                double utilization = (double)_activeConnections / _maxConnections;

                if (utilization > 0.9)
                {
                    return HealthCheckResult.Unhealthy($"At capacity: {_activeConnections}/{_maxConnections} connections");
                }

                if (utilization > 0.7)
                {
                    // Note: In readiness check, degraded = not ready
                    return HealthCheckResult.Degraded($"High load: {_activeConnections}/{_maxConnections} connections");
                }

                return HealthCheckResult.Healthy($"Normal load: {_activeConnections}/{_maxConnections} connections");
            });

            // Start services
            await server.StartAsync();
            await healthService.StartAsync();

            Console.WriteLine("✅ SMTP Server started on port 2525");
            Console.WriteLine("✅ Health check service started on port 8080\n");

            Console.WriteLine("📍 Available endpoints:");
            Console.WriteLine("   http://localhost:8080/health/        - General health (liveness)");
            Console.WriteLine("   http://localhost:8080/health/readyz  - Readiness check");
            Console.WriteLine("   http://localhost:8080/health/livez   - Liveness check\n");

            Console.WriteLine("📊 Service initialization timeline:");

            // Simulate gradual service startup
            _ = Task.Run(async () =>
            {
                // Database comes online after 3 seconds
                Console.WriteLine("   ⏳ Connecting to database...");
                await Task.Delay(3000);
                _databaseReady = true;
                Console.WriteLine("   ✅ Database connected (3s)");

                // Cache comes online after 5 seconds total
                Console.WriteLine("   ⏳ Initializing cache service...");
                await Task.Delay(2000);
                _cacheReady = true;
                Console.WriteLine("   ✅ Cache service ready (5s)");

                // Warmup completes after 7 seconds total
                Console.WriteLine("   ⏳ Performing application warmup...");
                await Task.Delay(2000);
                _warmupComplete = true;
                Console.WriteLine("   ✅ Application warmup complete (7s)\n");

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("🚀 Service is now READY to accept traffic!");
                Console.WriteLine("   /readyz endpoint will now return HTTP 200 OK");
                Console.ResetColor();
                Console.WriteLine();
            });

            // Simulate connection activity
            _ = Task.Run(async () =>
            {
                Random random = new();
                while (true)
                {
                    await Task.Delay(random.Next(2000, 5000));

                    // Randomly add or remove connections
                    int change = random.Next(-5, 10);
                    int newCount = _activeConnections + change;

                    if (newCount >= 0 && newCount <= _maxConnections)
                    {
                        _activeConnections = newCount;

                        // Log when crossing thresholds
                        double utilization = (double)_activeConnections / _maxConnections;
                        if (utilization > 0.9 && (_activeConnections - change) <= 90)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"⚠️  CAPACITY WARNING: {_activeConnections}/{_maxConnections} connections - Service marked NOT READY");
                            Console.ResetColor();
                        }
                        else if (utilization <= 0.9 && (_activeConnections - change) > 90)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"✅  Capacity restored: {_activeConnections}/{_maxConnections} connections - Service marked READY");
                            Console.ResetColor();
                        }
                    }
                }
            });

            Console.WriteLine("\n💡 Try these while the service is running:");
            Console.WriteLine("   1. curl http://localhost:8080/health/       (always returns OK if service is alive)");
            Console.WriteLine("   2. curl http://localhost:8080/health/readyz (returns 503 until all checks pass)");
            Console.WriteLine("   3. Watch the readyz endpoint transition from NotReady to Ready as services initialize");
            Console.WriteLine("\nPress any key to stop...");
            Console.ReadKey();

            // Stop services
            await healthService.StopAsync();
            await server.StopAsync();

            Console.WriteLine("\n🛑 Services stopped.");
        }

        private static async Task<AuthenticationResult> AuthenticateUser(string username, string password)
        {
            // Simple authentication for demo
            await Task.CompletedTask; // Async operation placeholder

            if (username == "demo" && password == "password")
            {
                return AuthenticationResult.Succeed(username);
            }

            return AuthenticationResult.Fail("Invalid credentials");
        }
    }
}