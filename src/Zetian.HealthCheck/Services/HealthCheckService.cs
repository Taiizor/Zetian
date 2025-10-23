using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Zetian.HealthCheck.Abstractions;
using Zetian.HealthCheck.Enums;
using Zetian.HealthCheck.Models;
using Zetian.HealthCheck.Options;

namespace Zetian.HealthCheck.Services
{
    /// <summary>
    /// HTTP endpoint service for health checks
    /// </summary>
    public class HealthCheckService : IDisposable
    {
        private readonly ILogger<HealthCheckService> _logger;
        private readonly HealthCheckServiceOptions _options;
        private readonly Dictionary<string, IHealthCheck> _healthChecks;
        private HttpListener? _listener;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _listenerTask;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of HealthCheckService
        /// </summary>
        public HealthCheckService(HealthCheckServiceOptions? options = null, ILoggerFactory? loggerFactory = null)
        {
            _options = options ?? new HealthCheckServiceOptions();
            _healthChecks = new();

            loggerFactory ??= NullLoggerFactory.Instance;
            _logger = loggerFactory.CreateLogger<HealthCheckService>();
        }

        /// <summary>
        /// Gets a value indicating whether the service is running
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Adds a health check
        /// </summary>
        public void AddHealthCheck(string name, IHealthCheck healthCheck)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            _healthChecks[name] = healthCheck ?? throw new ArgumentNullException(nameof(healthCheck));
            _logger.LogDebug($"Added health check: {name}");
        }

        /// <summary>
        /// Starts the health check service
        /// </summary>
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
#if NET6_0
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(HealthCheckService));
            }
#else
            ObjectDisposedException.ThrowIf(_disposed, this);
#endif

            if (IsRunning)
            {
                _logger.LogWarning("Health check service is already running");
                return;
            }

            try
            {
                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _listener = new HttpListener();

                foreach (string prefix in _options.Prefixes)
                {
                    _listener.Prefixes.Add(prefix);
                    _logger.LogInformation($"Health check listening on: {prefix}");
                }

                _listener.Start();
                IsRunning = true;

                _listenerTask = Task.Run(() => ListenAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);

                _logger.LogInformation("Health check service started");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start health check service");
                IsRunning = false;
                throw;
            }
        }

        /// <summary>
        /// Stops the health check service
        /// </summary>
        public async Task StopAsync()
        {
            if (!IsRunning)
            {
                return;
            }

            try
            {
                _logger.LogInformation("Stopping health check service");

                _cancellationTokenSource?.Cancel();
                _listener?.Stop();

                if (_listenerTask != null)
                {
                    await _listenerTask.ConfigureAwait(false);
                }

                IsRunning = false;
                _logger.LogInformation("Health check service stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping health check service");
            }
        }

        private async Task ListenAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _listener?.IsListening == true)
            {
                try
                {
                    HttpListenerContext context = await _listener.GetContextAsync().ConfigureAwait(false);
                    _ = Task.Run(() => HandleRequestAsync(context, cancellationToken), cancellationToken);
                }
                catch (HttpListenerException ex) when (ex.ErrorCode == 995) // Listener stopped
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in health check listener");
                }
            }
        }

        private async Task HandleRequestAsync(HttpListenerContext context, CancellationToken cancellationToken)
        {
            try
            {
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                // Only handle GET requests
                if (request.HttpMethod != "GET")
                {
                    response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                    response.Close();
                    return;
                }

                string fullPath = request.Url?.AbsolutePath ?? "/";

                // Extract the path after the prefix by checking what prefix was matched
                string effectivePath = fullPath;

                // Try to match against registered prefixes to extract the actual health check path
                foreach (string prefix in _options.Prefixes)
                {
                    Uri prefixUri = new(prefix);
                    string prefixPath = prefixUri.AbsolutePath.TrimEnd('/');

                    if (fullPath.StartsWith(prefixPath, StringComparison.OrdinalIgnoreCase))
                    {
                        // Extract the path after the prefix
                        effectivePath = fullPath[prefixPath.Length..];
                        if (!effectivePath.StartsWith('/'))
                        {
                            effectivePath = "/" + effectivePath;
                        }
                        break;
                    }
                }

                // Normalize path - remove trailing slash for comparison
                string normalizedPath = effectivePath.TrimEnd('/');

                // Route handling - support various path formats
                if (normalizedPath is "" or "/" or "/health" or "/healthz")
                {
                    await HandleHealthCheckAsync(response, cancellationToken);
                }
                else if (normalizedPath is "/readyz" or "/ready" or "/health/readyz" or "/health/ready")
                {
                    await HandleReadinessCheckAsync(response, cancellationToken);
                }
                else if (normalizedPath is "/livez" or "/live" or "/health/livez" or "/health/live")
                {
                    await HandleLivenessCheckAsync(response, cancellationToken);
                }
                else
                {
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    response.Close();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling health check request");

                try
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    context.Response.Close();
                }
                catch { }
            }
        }

        private async Task HandleHealthCheckAsync(HttpListenerResponse response, CancellationToken cancellationToken)
        {
            Dictionary<string, object> results = new();
            HealthStatus overallStatus = HealthStatus.Healthy;

            foreach (KeyValuePair<string, IHealthCheck> kvp in _healthChecks)
            {
                try
                {
                    HealthCheckResult result = await kvp.Value.CheckHealthAsync(cancellationToken);

                    results[kvp.Key] = new
                    {
                        status = result.Status.ToString(),
                        description = result.Description,
                        data = result.Data
                    };

                    if (result.Status > overallStatus)
                    {
                        overallStatus = result.Status;
                    }
                }
                catch (Exception ex)
                {
                    results[kvp.Key] = new
                    {
                        status = "Unhealthy",
                        error = ex.Message
                    };
                    overallStatus = HealthStatus.Unhealthy;
                }
            }

            var responseData = new
            {
                status = overallStatus.ToString(),
                timestamp = DateTimeOffset.UtcNow,
                checks = results
            };

            // Set status code based on health
            response.StatusCode = overallStatus switch
            {
                HealthStatus.Healthy => (int)HttpStatusCode.OK,
                HealthStatus.Degraded => _options.DegradedStatusCode,
                HealthStatus.Unhealthy => (int)HttpStatusCode.ServiceUnavailable,
                _ => (int)HttpStatusCode.ServiceUnavailable
            };

            await WriteJsonResponse(response, responseData);
        }

        private async Task HandleReadinessCheckAsync(HttpListenerResponse response, CancellationToken cancellationToken)
        {
            // For now, readiness = health. Can be customized later.
            await HandleHealthCheckAsync(response, cancellationToken);
        }

        private async Task HandleLivenessCheckAsync(HttpListenerResponse response, CancellationToken cancellationToken)
        {
            // Simple liveness check - if we can respond, we're alive
            response.StatusCode = (int)HttpStatusCode.OK;
            await WriteJsonResponse(response, new { status = "Alive", timestamp = DateTimeOffset.UtcNow });
        }

#if NET7_0_OR_GREATER
        [RequiresDynamicCode("JSON serialization/deserialization might require runtime code generation.")]
        [RequiresUnreferencedCode("JSON serialization/deserialization might require types that cannot be statically analyzed.")]
#endif
        private async Task WriteJsonResponse(HttpListenerResponse response, object data)
        {
            response.ContentType = "application/json";
            response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");

            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            byte[] buffer = Encoding.UTF8.GetBytes(json);
            response.ContentLength64 = buffer.Length;

            await response.OutputStream.WriteAsync(buffer);
            response.Close();
        }

        /// <summary>
        /// Disposes the health check service
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the health check service
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                StopAsync().GetAwaiter().GetResult();
                _cancellationTokenSource?.Dispose();
                _listener?.Close();
            }

            _disposed = true;
        }
    }
}