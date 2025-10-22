using Microsoft.Extensions.Logging;
using System.Net;
using Zetian.Configuration;
using Zetian.HealthCheck.Extensions;

namespace Zetian.HealthCheck.Examples
{
    /// <summary>
    /// Example of using the health check feature with SMTP server
    /// </summary>
    public class HealthCheckExample
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("=== SMTP Server with Health Check Example ===\n");

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

            // Method 1: Start server and health check separately
            Console.WriteLine("Starting SMTP server on port 2525...");
            await smtpServer.StartAsync();
            Console.WriteLine($"SMTP server running on {smtpServer.Endpoint}\n");

            // Enable health check on port 8080
            Console.WriteLine("Enabling health check on http://localhost:8080/health/");
            HealthCheckService healthCheckService = smtpServer.EnableHealthCheck(8080, "/status/");

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

            await healthCheckService.StartAsync();
            Console.WriteLine("Health check service started\n");

            // Print health check endpoints
            Console.WriteLine("Available health check endpoints:");
            Console.WriteLine("  - http://localhost:8080/health/        (Full health check)");
            Console.WriteLine("  - http://localhost:8080/health/livez   (Liveness probe)");
            Console.WriteLine("  - http://localhost:8080/health/readyz  (Readiness probe)");
            Console.WriteLine();

            // Method 2: Alternative - Start both together
            // var healthCheckService = await smtpServer.StartWithHealthCheckAsync(8080);

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

        /// <summary>
        /// Example with specific IP/Hostname binding
        /// </summary>
        public static async Task RunWithSpecificBindingAsync()
        {
            Console.WriteLine("=== SMTP Server with Health Check on Specific IP/Hostname ===");

            SmtpServerConfiguration config = new()
            {
                Port = 2525,
                IpAddress = IPAddress.Any, // SMTP listens on all interfaces
                ServerName = "Zetian IP Binding Example",
                MaxConnections = 100
            };

            using SmtpServer smtpServer = new(config);
            await smtpServer.StartAsync();
            Console.WriteLine($"SMTP server running on {smtpServer.Endpoint}\n");

            // Example 1: Bind health check to specific IP
            IPAddress specificIP = IPAddress.Parse("127.0.0.1");
            Console.WriteLine($"Starting health check on IP: {specificIP}");
            HealthCheckService healthCheckOnIP = smtpServer.EnableHealthCheck(specificIP, 8081);
            await healthCheckOnIP.StartAsync();
            Console.WriteLine("Health check available at: http://127.0.0.1:8081/health/\n");

            // Example 2: Bind health check to all interfaces
            Console.WriteLine("Starting health check on all interfaces");
            HealthCheckService healthCheckOnAll = smtpServer.EnableHealthCheck("0.0.0.0", 8082);
            await healthCheckOnAll.StartAsync();
            Console.WriteLine("Health check available at: http://0.0.0.0:8082/health/");
            Console.WriteLine("                      and: http://localhost:8082/health/");
            Console.WriteLine("                      and: http://[your-ip]:8082/health/\n");
            Console.ReadKey();

            // Example 3: Bind to specific hostname
            Console.WriteLine("Starting health check with hostname");
            HealthCheckService healthCheckWithHost = smtpServer.EnableHealthCheck("localhost", 8083);
            await healthCheckWithHost.StartAsync();
            Console.WriteLine("Health check available at: http://localhost:8083/health/\n");

            // Example 4: IPv6 binding (if supported)
            try
            {
                IPAddress ipv6Address = IPAddress.IPv6Loopback;
                Console.WriteLine($"Starting health check on IPv6: {ipv6Address}");
                HealthCheckService healthCheckIPv6 = smtpServer.EnableHealthCheck(ipv6Address, 8084);
                await healthCheckIPv6.StartAsync();
                Console.WriteLine("Health check available at: http://[::1]:8084/health/\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"IPv6 not supported: {ex.Message}\n");
            }

            Console.WriteLine("\nPress any key to stop...");
            Console.ReadKey();

            // Stop all health check services
            await healthCheckOnIP.StopAsync();
            await healthCheckOnAll.StopAsync();
            await healthCheckWithHost.StopAsync();
            await smtpServer.StopAsync();
        }

        /// <summary>
        /// Example with custom health check options
        /// </summary>
        public static async Task RunWithCustomOptionsAsync()
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
                    "http://localhost:8080/health/",
                    "http://+:8080/health/"  // Listen on all interfaces
                },
                DegradedStatusCode = 218  // "This is fine" status code
            };

            HealthCheckService healthCheckService = smtpServer.EnableHealthCheck(serviceOptions, healthCheckOptions);
            await healthCheckService.StartAsync();

            Console.WriteLine("Server running with custom health check configuration");
            Console.WriteLine("Health check available on all interfaces at port 8080");
            Console.WriteLine("\nPress any key to stop...");
            Console.ReadKey();

            await healthCheckService.StopAsync();
            await smtpServer.StopAsync();
        }
    }
}