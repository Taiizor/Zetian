using Zetian.Server;
using Zetian.WebUI.Extensions;
using Zetian.WebUI.Services;

namespace Zetian.WebUI.Examples
{
    /// <summary>
    /// WebUI with custom authentication example
    /// </summary>
    public class AuthenticatedWebUIExample
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("=== Authenticated WebUI Example ===");

            // Create SMTP server with authentication
            SmtpServer server = new SmtpServerBuilder()
                .Port(587)
                .RequireAuthentication()
                .AllowPlainTextAuthentication()
                .SimpleAuthentication("smtp_user", "smtp_pass")
                .Build();

            // Configure WebUI with custom authentication
            IWebUIService webUI = server.EnableWebUI(options =>
            {
                options.Port = 8443;
                options.UseHttps = false; // Set to true with certificate for production
                options.RequireAuthentication = true;
                options.AdminUsername = "administrator";
                options.AdminPassword = "SecureP@ssw0rd!";
                options.EnableApiKey = true;
                options.ApiKey = "my-secure-api-key-" + Guid.NewGuid();
                options.SessionTimeout = TimeSpan.FromHours(2);
                options.EnableSwagger = true;
                options.EnableSignalR = true;
            });

            // Start services
            await Task.WhenAll(
                server.StartAsync(),
                webUI.StartAsync()
            );

            Console.WriteLine("Services started:");
            Console.WriteLine($"- SMTP: {server.Endpoint}");
            Console.WriteLine($"- WebUI: {webUI.Url}");
            Console.WriteLine($"- Swagger: {webUI.Url}/swagger");
            Console.WriteLine($"- API Key: {webUI.Options.ApiKey}");
            Console.WriteLine("\nPress any key to stop...");

            Console.ReadKey();

            await Task.WhenAll(
                webUI.StopAsync(),
                server.StopAsync()
            );
        }
    }
}