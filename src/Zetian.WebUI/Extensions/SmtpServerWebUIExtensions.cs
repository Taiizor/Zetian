using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Zetian.Abstractions;
using Zetian.WebUI.Options;
using Zetian.WebUI.Services;

namespace Zetian.WebUI.Extensions
{
    /// <summary>
    /// Extension methods for adding WebUI to SMTP server
    /// </summary>
    public static class SmtpServerWebUIExtensions
    {
        /// <summary>
        /// Enables the WebUI for the SMTP server with default options
        /// </summary>
        public static IWebUIService EnableWebUI(this ISmtpServer server, int port = 8080)
        {
            WebUIOptions options = new() { Port = port };
            return EnableWebUI(server, options);
        }

        /// <summary>
        /// Enables the WebUI for the SMTP server with custom options
        /// </summary>
        public static IWebUIService EnableWebUI(this ISmtpServer server, Action<WebUIOptions> configureOptions)
        {
            WebUIOptions options = new();
            configureOptions(options);
            return EnableWebUI(server, options);
        }

        /// <summary>
        /// Enables the WebUI for the SMTP server
        /// </summary>
        public static IWebUIService EnableWebUI(this ISmtpServer server, WebUIOptions options)
        {
            if (server == null)
            {
                throw new ArgumentNullException(nameof(server));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            options.Validate();
            return new WebUIService(server, options);
        }

        /// <summary>
        /// Starts the SMTP server with WebUI enabled
        /// </summary>
        public static async Task<IWebUIService> StartWithWebUIAsync(
            this ISmtpServer server,
            int port = 8080,
            Action<WebUIOptions>? configureOptions = null)
        {
            WebUIOptions options = new() { Port = port };
            configureOptions?.Invoke(options);

            IWebUIService webUI = EnableWebUI(server, options);

            // Start both services
            await Task.WhenAll(
                server.StartAsync(),
                webUI.StartAsync()
            );

            return webUI;
        }

        /// <summary>
        /// Adds the WebUI services to the service collection
        /// </summary>
        public static IServiceCollection AddZetianWebUI(
            this IServiceCollection services,
            ISmtpServer server,
            Action<WebUIOptions>? configureOptions = null)
        {
            WebUIOptions options = new();
            configureOptions?.Invoke(options);
            options.Validate();

            services.AddSingleton(server);
            services.AddSingleton(options);
            services.AddSingleton<IWebUIService, WebUIService>();
            services.AddSingleton<IDashboardService, DashboardService>();
            services.AddSingleton<IStatisticsService, StatisticsService>();
            services.AddSingleton<ISessionManager, SessionManager>();
            services.AddSingleton<IMessageQueueService, MessageQueueService>();
            services.AddSingleton<ILogService, LogService>();
            services.AddSingleton<IAuthenticationService, AuthenticationService>();
            services.AddSingleton<IConfigurationService, ConfigurationService>();

            if (options.EnableSignalR)
            {
                services.AddSignalR();
            }

            if (options.EnableSwagger)
            {
                services.AddEndpointsApiExplorer();
                services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new()
                    {
                        Title = "Zetian SMTP Server API",
                        Version = "v1",
                        Description = "API for managing Zetian SMTP Server"
                    });
                });
            }

            services.AddAuthentication()
                .AddJwtBearer();

            services.AddAuthorization();

            if (options.EnableCors)
            {
                services.AddCors(cors =>
                {
                    cors.AddDefaultPolicy(policy =>
                    {
                        if (options.CorsOrigins?.Length > 0)
                        {
                            policy.WithOrigins(options.CorsOrigins);
                        }
                        else
                        {
                            policy.AllowAnyOrigin();
                        }

                        policy.AllowAnyMethod()
                              .AllowAnyHeader();
                    });
                });
            }

            return services;
        }

        /// <summary>
        /// Uses the WebUI middleware in the application pipeline
        /// </summary>
        public static IApplicationBuilder UseZetianWebUI(
            this IApplicationBuilder app,
            WebUIOptions? options = null)
        {
            options ??= app.ApplicationServices.GetRequiredService<WebUIOptions>();

            if (options.EnableCors)
            {
                app.UseCors();
            }

            app.UseAuthentication();
            app.UseAuthorization();

            if (options.EnableSwagger)
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Zetian SMTP API v1");
                    c.RoutePrefix = "swagger";
                });
            }

            app.UseStaticFiles();
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();

                if (options.EnableSignalR)
                {
                    endpoints.MapHub<SmtpHub>("/hubs/smtp");
                }

                if (options.EnableMetricsEndpoint)
                {
                    endpoints.MapGet("/metrics", async context =>
                    {
                        IStatisticsService service = context.RequestServices.GetRequiredService<IStatisticsService>();
                        string metrics = await service.GetPrometheusMetricsAsync();
                        context.Response.ContentType = "text/plain";
                        await context.Response.WriteAsync(metrics);
                    });
                }

                endpoints.MapFallbackToFile("index.html");
            });

            return app;
        }
    }
}