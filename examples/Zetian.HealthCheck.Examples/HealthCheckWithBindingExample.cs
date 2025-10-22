using System.Net;
using Zetian.Configuration;
using Zetian.HealthCheck.Extensions;

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
            await healthCheckOnIP.StartAsync();
            Console.WriteLine("Health check available at: http://127.0.0.1:8081/health/\n");

            // Example 2: Bind health check to all interfaces
            Console.WriteLine("Starting health check on all interfaces");
            HealthCheckService healthCheckOnAll = smtpServer.EnableHealthCheck("0.0.0.0", 8082);
            await healthCheckOnAll.StartAsync();
            Console.WriteLine("Health check available at: http://0.0.0.0:8082/health/");
            Console.WriteLine("                      and: http://localhost:8082/health/");
            Console.WriteLine("                      and: http://[your-ip]:8082/health/\n");

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

            Console.WriteLine("Example completed.");
        }
    }
}