using Zetian.Server;
using Zetian.WebUI.Extensions;
using Zetian.WebUI.Services;

namespace Zetian.WebUI.Examples
{
    /// <summary>
    /// WebUI with real-time monitoring example
    /// </summary>
    public class RealTimeMonitoringExample
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("=== Real-Time Monitoring Example ===");

            SmtpServer server = new SmtpServerBuilder()
                .Port(25)
                .ServerName("Monitored SMTP Server")
                .Build();

            // Enable WebUI with SignalR for real-time updates
            IWebUIService webUI = server.StartWithWebUIAsync(8080, options =>
            {
                options.EnableSignalR = true;
                options.EnableMetricsEndpoint = true;
                options.RefreshInterval = 1; // Update every second
                options.RequireAuthentication = false; // Disable for demo
            }).Result;

            // Monitor SMTP events
            server.SessionCreated += (sender, e) =>
            {
                Console.WriteLine($"[Monitor] New session created: {e.Session.Id}");
            };

            server.MessageReceived += (sender, e) =>
            {
                Console.WriteLine($"[Monitor] Message received:");
                Console.WriteLine($"  From: {e.Message.From}");
                Console.WriteLine($"  To: {string.Join(", ", e.Message.Recipients)}");
                Console.WriteLine($"  Size: {e.Message.Size} bytes");
            };

            server.ErrorOccurred += (sender, e) =>
            {
                Console.WriteLine($"[Monitor] Error: {e.Exception.Message}");
            };

            Console.WriteLine("\nReal-time monitoring enabled:");
            Console.WriteLine($"- Dashboard: {webUI.Url}");
            Console.WriteLine($"- SignalR Hub: {webUI.Url}/hubs/smtp");
            Console.WriteLine($"- Metrics: {webUI.Url}/metrics");
            Console.WriteLine("\nOpen the dashboard in a browser to see real-time updates!");
            Console.WriteLine("Send test emails to port 25 to see live monitoring.");
            Console.WriteLine("\nPress any key to stop...");

            Console.ReadKey();

            await webUI.StopAsync();
            await server.StopAsync();
        }
    }
}