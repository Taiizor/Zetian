using Zetian.Server;
using Zetian.WebUI.Extensions;
using Zetian.WebUI.Services;

namespace Zetian.WebUI.Examples
{
    /// <summary>
    /// Basic WebUI example
    /// </summary>
    public class BasicWebUIExample
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("=== Basic WebUI Example ===");

            // Create SMTP server
            SmtpServer server = new SmtpServerBuilder()
                .Port(25)
                .ServerName("Zetian SMTP with WebUI")
                .MaxMessageSizeMB(25)
                .MaxConnections(100)
                .Build();

            // Enable WebUI with default settings
            IWebUIService webUI = server.EnableWebUI(8080);

            // Register event handlers
            server.MessageReceived += (sender, e) =>
            {
                Console.WriteLine($"[SMTP] Message received from {e.Message.From}: {e.Message.Subject}");
            };

            webUI.OnClientConnected += (sender, e) =>
            {
                Console.WriteLine($"[WebUI] Client connected: {e.ClientId} from {e.IpAddress}");
            };

            webUI.OnApiRequest += (sender, e) =>
            {
                Console.WriteLine($"[WebUI] API Request: {e.Method} {e.Path} -> {e.StatusCode}");
            };

            // Start both services
            await server.StartAsync();
            await webUI.StartAsync();

            Console.WriteLine($"SMTP Server running on {server.Endpoint}");
            Console.WriteLine($"WebUI available at {webUI.Url}");
            Console.WriteLine("Default credentials: admin/admin");
            Console.WriteLine("Press any key to stop...");

            Console.ReadKey();

            // Stop services
            await webUI.StopAsync();
            await server.StopAsync();
        }
    }
}