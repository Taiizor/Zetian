using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Zetian.Abstractions;
using Zetian.Models.EventArgs;
using Zetian.WebUI.Options;
using ErrorEventArgs = Zetian.Models.EventArgs.ErrorEventArgs;

namespace Zetian.WebUI.Services
{
    /// <summary>
    /// Implementation of the WebUI service
    /// </summary>
    public class WebUIService : IWebUIService
    {
        private readonly ISmtpServer _smtpServer;
        private IHost? _host;
        private readonly List<Action<WebApplication>> _appConfigurations = [];

        public WebUIService(ISmtpServer smtpServer, WebUIOptions options)
        {
            _smtpServer = smtpServer ?? throw new ArgumentNullException(nameof(smtpServer));
            Options = options ?? throw new ArgumentNullException(nameof(options));

            // Subscribe to SMTP server events
            SubscribeToSmtpEvents();
        }

        public WebUIOptions Options { get; }

        public bool IsRunning => _host != null;

        public string Url
        {
            get
            {
                string scheme = Options.UseHttps ? "https" : "http";
                string host = string.IsNullOrEmpty(Options.HostName) ? "localhost" : Options.HostName;
                string basePath = Options.BasePath.TrimEnd('/');
                return $"{scheme}://{host}:{Options.Port}{basePath}";
            }
        }

        public event EventHandler<ClientEventArgs>? OnClientConnected;
        public event EventHandler<ClientEventArgs>? OnClientDisconnected;
        public event EventHandler<ApiRequestEventArgs>? OnApiRequest;

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (_host != null)
            {
                throw new InvalidOperationException("WebUI service is already running");
            }

            WebApplicationBuilder builder = WebApplication.CreateBuilder();

            // Configure Kestrel
            builder.WebHost.ConfigureKestrel(options =>
            {
                if (Options.UseHttps && Options.Certificate != null)
                {
                    options.ListenAnyIP(Options.Port, listenOptions =>
                    {
                        listenOptions.UseHttps(Options.Certificate);
                    });
                }
                else
                {
                    options.ListenAnyIP(Options.Port);
                }
            });

            // Add services
            ConfigureServices(builder.Services);

            WebApplication app = builder.Build();

            // Configure middleware pipeline
            ConfigureApp(app);

            // Apply custom configurations
            foreach (Action<WebApplication> configure in _appConfigurations)
            {
                configure(app);
            }

            _host = app;
            await _host.StartAsync(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            if (_host == null)
            {
                return;
            }

            await _host.StopAsync(cancellationToken);
            _host.Dispose();
            _host = null;
        }

        public void ConfigureApp(Action<WebApplication> configure)
        {
            _appConfigurations.Add(configure);
        }

        public void Dispose()
        {
            _host?.Dispose();
            UnsubscribeFromSmtpEvents();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(_smtpServer);
            services.AddSingleton(Options);
            services.AddSingleton<IWebUIService>(this);

            // Add core services
            services.AddSingleton<IDashboardService, DashboardService>();
            services.AddSingleton<IStatisticsService, StatisticsService>();
            services.AddSingleton<ISessionManager, SessionManager>();
            services.AddSingleton<IMessageQueueService, MessageQueueService>();
            services.AddSingleton<ILogService, LogService>();
            services.AddSingleton<IAuthenticationService, AuthenticationService>();
            services.AddSingleton<IConfigurationService, ConfigurationService>();
            services.AddSingleton<INotificationService, NotificationService>();

            // Add controllers
            services.AddControllers();

            // Add SignalR if enabled
            if (Options.EnableSignalR)
            {
                services.AddSignalR();
            }

            // Add Swagger if enabled
            if (Options.EnableSwagger)
            {
                services.AddEndpointsApiExplorer();
                services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new()
                    {
                        Title = "Zetian SMTP Server API",
                        Version = "v1",
                        Description = "REST API for managing Zetian SMTP Server"
                    });

                    if (Options.RequireAuthentication)
                    {
                        c.AddSecurityDefinition("Bearer", new()
                        {
                            Description = "JWT Authorization header using the Bearer scheme",
                            Name = "Authorization",
                            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                            Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                            Scheme = "Bearer"
                        });

                        c.AddSecurityRequirement(new()
                        {
                            {
                                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                                {
                                    Reference = new()
                                    {
                                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                        Id = "Bearer"
                                    }
                                },
                                Array.Empty<string>()
                            }
                        });
                    }
                });
            }

            // Add authentication if enabled
            if (Options.RequireAuthentication)
            {
                services.AddAuthentication("Bearer")
                    .AddJwtBearer("Bearer", options =>
                    {
                        options.TokenValidationParameters = new()
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            ValidIssuer = Options.JwtIssuer,
                            ValidAudience = Options.JwtAudience,
                            IssuerSigningKey = new SymmetricSecurityKey(
                                Encoding.UTF8.GetBytes(Options.JwtSecretKey))
                        };
                    });

                services.AddAuthorization();
            }

            // Add CORS if enabled
            if (Options.EnableCors)
            {
                services.AddCors(options =>
                {
                    options.AddDefaultPolicy(builder =>
                    {
                        if (Options.CorsOrigins?.Length > 0)
                        {
                            builder.WithOrigins(Options.CorsOrigins);
                        }
                        else
                        {
                            builder.AllowAnyOrigin();
                        }

                        builder.AllowAnyMethod()
                               .AllowAnyHeader();
                    });
                });
            }
        }

        private void ConfigureApp(WebApplication app)
        {
            // Development mode
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // CORS
            if (Options.EnableCors)
            {
                app.UseCors();
            }

            // Static files
            app.UseStaticFiles();
            app.UseRouting();

            // Authentication & Authorization
            if (Options.RequireAuthentication)
            {
                app.UseAuthentication();
                app.UseAuthorization();
            }

            // Swagger
            if (Options.EnableSwagger)
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Zetian SMTP API v1");
                });
            }

            // Map endpoints
            app.MapControllers();

            if (Options.EnableSignalR)
            {
                app.MapHub<SmtpHub>("/hubs/smtp");
            }

            if (Options.EnableMetricsEndpoint)
            {
                app.MapGet("/metrics", async context =>
                {
                    IStatisticsService service = context.RequestServices.GetRequiredService<IStatisticsService>();
                    string metrics = await service.GetPrometheusMetricsAsync();
                    context.Response.ContentType = "text/plain";
                    await context.Response.WriteAsync(metrics);
                });
            }

            // Health check endpoints
            app.MapGet("/health", async context =>
            {
                IDashboardService dashboard = context.RequestServices.GetRequiredService<IDashboardService>();
                HealthStatus health = await dashboard.GetHealthStatusAsync();
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(health);
            });

            app.MapGet("/health/ready", context =>
            {
                context.Response.StatusCode = _smtpServer.IsRunning ? 200 : 503;
                return Task.CompletedTask;
            });

            app.MapGet("/health/live", context =>
            {
                context.Response.StatusCode = 200;
                return Task.CompletedTask;
            });

            // Fallback to index.html for SPA
            app.MapFallbackToFile("index.html");
        }

        private void SubscribeToSmtpEvents()
        {
            _smtpServer.SessionCreated += OnSessionCreated;
            _smtpServer.SessionCompleted += OnSessionCompleted;
            _smtpServer.MessageReceived += OnMessageReceived;
            _smtpServer.ErrorOccurred += OnErrorOccurred;
            _smtpServer.AuthenticationSucceeded += OnAuthenticationSucceeded;
            _smtpServer.AuthenticationFailed += OnAuthenticationFailed;
        }

        private void UnsubscribeFromSmtpEvents()
        {
            _smtpServer.SessionCreated -= OnSessionCreated;
            _smtpServer.SessionCompleted -= OnSessionCompleted;
            _smtpServer.MessageReceived -= OnMessageReceived;
            _smtpServer.ErrorOccurred -= OnErrorOccurred;
            _smtpServer.AuthenticationSucceeded -= OnAuthenticationSucceeded;
            _smtpServer.AuthenticationFailed -= OnAuthenticationFailed;
        }

        private void OnSessionCreated(object? sender, SessionEventArgs e)
        {
            // Broadcast to SignalR clients
            BroadcastToHub("SessionCreated", e);
        }

        private void OnSessionCompleted(object? sender, SessionEventArgs e)
        {
            BroadcastToHub("SessionCompleted", e);
        }

        private void OnMessageReceived(object? sender, MessageEventArgs e)
        {
            BroadcastToHub("MessageReceived", e);
        }

        private void OnErrorOccurred(object? sender, ErrorEventArgs e)
        {
            BroadcastToHub("ErrorOccurred", e);
        }

        private void OnAuthenticationSucceeded(object? sender, AuthenticationEventArgs e)
        {
            BroadcastToHub("AuthenticationSucceeded", e);
        }

        private void OnAuthenticationFailed(object? sender, AuthenticationEventArgs e)
        {
            BroadcastToHub("AuthenticationFailed", e);
        }

        private void BroadcastToHub(string eventName, object data)
        {
            // Broadcast to SignalR hub if available and SignalR is enabled
            if (Options.EnableSignalR && _host != null)
            {
                try
                {
                    IHubContext<SmtpHub>? hubContext = _host.Services.GetService<IHubContext<SmtpHub>>();
                    if (hubContext != null)
                    {
                        // Fire and forget - we don't wait for the broadcast
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await hubContext.Clients.All.SendAsync(eventName, data);
                            }
                            catch (Exception ex)
                            {
                                // Log error but don't throw
                                Console.WriteLine($"Error broadcasting to hub: {ex.Message}");
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    // Log error but don't throw - broadcasting errors should not affect server operation
                    Console.WriteLine($"Error getting hub context: {ex.Message}");
                }
            }
        }
    }
}