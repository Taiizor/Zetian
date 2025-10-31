using Microsoft.AspNetCore.Builder;
using Zetian.Server;
using Zetian.WebUI.Extensions;
using Zetian.WebUI.Services;

namespace Zetian.WebUI.Examples
{
    /// <summary>
    /// WebUI with custom configuration example
    /// </summary>
    public class CustomWebUIExample
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("=== Custom WebUI Example ===");

            SmtpServer server = SmtpServerBuilder.CreateBasic();

            // Configure WebUI with all custom options
            IWebUIService webUI = server.EnableWebUI(options =>
            {
                // Server settings
                options.Port = 9000;
                options.BasePath = "/admin/";
                //options.HostName = "smtp.example.com";

                // Authentication
                options.RequireAuthentication = false;
                options.AdminUsername = "admin";
                options.AdminPassword = "admin123";
                options.JwtSecretKey = "my-256-bit-secret-key-for-jwt-signing";
                options.SessionTimeout = TimeSpan.FromHours(4);

                // Features
                options.EnableSwagger = true;
                options.EnableSignalR = true;
                options.EnableMetricsEndpoint = true;
                options.EnableLogViewer = true;

                // CORS
                options.EnableCors = true;
                options.CorsOrigins = new[] { "https://app.example.com", "http://localhost:9000" };

                // Limits
                options.MaxLogEntries = 10000;
                options.MaxSessionHistory = 2000;
                options.MetricsRetentionDays = 30;
                options.RefreshInterval = 3;

                // Rate limiting
                options.EnableRateLimiting = true;
                options.RateLimitPerMinute = 120;

                // UI customization
                options.Theme = "dark";
                options.Title = "My SMTP Server";
                options.ShowFooter = true;
            });

            // Configure custom app behavior
            webUI.ConfigureApp(app =>
            {
                // Add custom middleware
                app.Use(async (context, next) =>
                {
                    Console.WriteLine($"[Middleware] {context.Request.Method} {context.Request.Path}");
                    await next();
                });

                // Add custom endpoint
                app.MapGet("/api/custom/info", () => new
                {
                    custom = true,
                    timestamp = DateTime.UtcNow,
                    message = "This is a custom endpoint"
                });
            });

            await server.StartAsync();
            await webUI.StartAsync();

            Console.WriteLine("Custom WebUI Configuration:");
            Console.WriteLine($"- URL: {webUI.Url}");
            Console.WriteLine($"- Admin Path: {webUI.Url}{webUI.Options.BasePath}");
            Console.WriteLine($"- Swagger: {webUI.Url}/swagger");
            Console.WriteLine($"- Metrics: {webUI.Url}/metrics");
            Console.WriteLine($"- Health: {webUI.Url}/health");
            Console.WriteLine($"- Custom Endpoint: {webUI.Url}/api/custom/info");
            Console.WriteLine("\nPress any key to stop...");

            Console.ReadKey();

            await webUI.StopAsync();
            await server.StopAsync();
        }
    }
}