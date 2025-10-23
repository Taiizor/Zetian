using System.Net;
using Zetian.Configuration;
using Zetian.HealthCheck.Extensions;
using Zetian.HealthCheck.Services;
using Zetian.Server;

namespace Zetian.HealthCheck.Examples
{
    /// <summary>
    /// Example with specific IP/Hostname binding
    /// </summary>
    public class HealthCheckWithBindingExample
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("=== SMTP Server with Health Check on Specific IP/Hostname ===\n");

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

            try
            {
                await healthCheckOnIP.StartAsync();
                Console.WriteLine("Health check available at: http://127.0.0.1:8081/health/\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to start health check on {specificIP}: {ex.Message}\n");
            }

            // Example 2: Bind health check to all interfaces
            Console.WriteLine("Starting health check on all interfaces (0.0.0.0)");
            HealthCheckService? healthCheckOnAll = null;

            try
            {
                healthCheckOnAll = smtpServer.EnableHealthCheck("0.0.0.0", 8082);
                await healthCheckOnAll.StartAsync();
                Console.WriteLine("✓ Health check available at:");
                Console.WriteLine("  - http://localhost:8082/health/");
                Console.WriteLine("  - http://[your-ip]:8082/health/\n");
            }
            catch (HttpListenerException ex) when (ex.ErrorCode == 5)
            {
                Console.WriteLine("❌ Access Denied: Administrator privileges required!");
                Console.WriteLine("To listen on all interfaces, you need to:");
                Console.WriteLine("1. Run as Administrator, or");
                Console.WriteLine("2. Add URL reservation (run cmd as admin):");
                Console.WriteLine("   netsh http add urlacl url=http://+:8082/health/ user=Everyone\n");
            }
            catch (HttpListenerException ex) when (ex.ErrorCode == 183)
            {
                Console.WriteLine("❌ Cannot create a file when that file already exists.");
                Console.WriteLine("Another application may be using port 8082.\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed: {ex.Message}\n");
            }

            // Example 3: Bind to specific hostname
            Console.WriteLine("Starting health check with hostname (localhost)");
            HealthCheckService? healthCheckWithHost = null;

            try
            {
                healthCheckWithHost = smtpServer.EnableHealthCheck("localhost", 8083);
                await healthCheckWithHost.StartAsync();
                Console.WriteLine("✓ Health check available at: http://localhost:8083/health/\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to start health check on localhost: {ex.Message}\n");
            }

            // Example 4: IPv6 binding (if supported)
            HealthCheckService? healthCheckIPv6 = null;

            try
            {
                IPAddress ipv6Address = IPAddress.IPv6Loopback;
                Console.WriteLine($"Starting health check on IPv6: {ipv6Address}");
                healthCheckIPv6 = smtpServer.EnableHealthCheck(ipv6Address, 8084);
                await healthCheckIPv6.StartAsync();
                Console.WriteLine("✓ Health check available at: http://[::1]:8084/health/\n");
            }
            catch (HttpListenerException ex)
            {
                Console.WriteLine($"❌ IPv6 binding failed: {ex.Message}");
                Console.WriteLine("IPv6 may not be supported or enabled on this system.\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ IPv6 not supported: {ex.Message}\n");
            }

            // Show summary
            Console.WriteLine("=====================================");
            Console.WriteLine("Summary:");
            Console.WriteLine("ℹ Localhost bindings usually work without admin rights");
            Console.WriteLine("ℹ Binding to 0.0.0.0 or specific IPs requires admin rights");
            Console.WriteLine("ℹ Use URL ACL reservations for non-admin access");
            Console.WriteLine("=====================================");

            Console.WriteLine("\nPress any key to stop...");
            Console.ReadKey();

            // Stop all health check services
            Console.WriteLine("\nStopping services...");

            try { await healthCheckOnIP.StopAsync(); } catch { }
            if (healthCheckOnAll != null)
            {
                try { await healthCheckOnAll.StopAsync(); } catch { }
            }

            if (healthCheckWithHost != null)
            {
                try { await healthCheckWithHost.StopAsync(); } catch { }
            }

            if (healthCheckIPv6 != null)
            {
                try { await healthCheckIPv6.StopAsync(); } catch { }
            }

            await smtpServer.StopAsync();

            Console.WriteLine("Example completed.");
        }
    }
}