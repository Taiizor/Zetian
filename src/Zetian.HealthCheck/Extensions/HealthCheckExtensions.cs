using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Zetian.Abstractions;
using Zetian.HealthCheck.Abstractions;
using Zetian.HealthCheck.Checks;
using Zetian.HealthCheck.Models;
using Zetian.HealthCheck.Options;
using Zetian.HealthCheck.Services;

namespace Zetian.HealthCheck.Extensions
{
    /// <summary>
    /// Extension methods for adding health check support
    /// </summary>
    public static class HealthCheckExtensions
    {
        /// <summary>
        /// Enables health check endpoint for the SMTP server
        /// </summary>
        /// <param name="server">The SMTP server</param>
        /// <param name="port">The port for health check endpoint (default: 8080)</param>
        /// <param name="path">The path for health check endpoint (default: /health/)</param>
        /// <returns>The health check service</returns>
        public static HealthCheckService EnableHealthCheck(
            this ISmtpServer server,
            int port = 8080,
            string path = "/health/")
        {
            return EnableHealthCheck(server, "localhost", port, path);
        }

        /// <summary>
        /// Enables health check endpoint for the SMTP server with specific hostname
        /// </summary>
        /// <param name="server">The SMTP server</param>
        /// <param name="hostname">The hostname to bind to</param>
        /// <param name="port">The port for health check endpoint</param>
        /// <param name="path">The path for health check endpoint (default: /health/)</param>
        /// <returns>The health check service</returns>
        public static HealthCheckService EnableHealthCheck(
            this ISmtpServer server,
            string hostname,
            int port,
            string path = "/health/")
        {
            ArgumentNullException.ThrowIfNull(server);

            if (string.IsNullOrWhiteSpace(hostname))
            {
                throw new ArgumentNullException(nameof(hostname));
            }

            // Ensure path ends with /
            if (!path.EndsWith('/'))
            {
                path += '/';
            }

            HealthCheckServiceOptions options = new()
            {
                Prefixes = new() { $"http://{hostname}:{port}{path}" }
            };

            HealthCheckService service = new(options, server.Configuration.LoggerFactory);

            // Add SMTP server health check
            SmtpServerHealthCheck smtpHealthCheck = new(server);
            service.AddHealthCheck("smtp_server", smtpHealthCheck);

            return service;
        }

        /// <summary>
        /// Enables health check endpoint for the SMTP server with specific IP address
        /// </summary>
        /// <param name="server">The SMTP server</param>
        /// <param name="ipAddress">The IP address to bind to</param>
        /// <param name="port">The port for health check endpoint</param>
        /// <param name="path">The path for health check endpoint (default: /health/)</param>
        /// <returns>The health check service</returns>
        public static HealthCheckService EnableHealthCheck(
            this ISmtpServer server,
            IPAddress ipAddress,
            int port,
            string path = "/health/")
        {
            ArgumentNullException.ThrowIfNull(server);

            ArgumentNullException.ThrowIfNull(ipAddress);

            string hostname = ipAddress.ToString();
            if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
            {
                hostname = $"[{hostname}]";
            }

            return EnableHealthCheck(server, hostname, port, path);
        }

        /// <summary>
        /// Enables health check endpoint with custom options
        /// </summary>
        /// <param name="server">The SMTP server</param>
        /// <param name="serviceOptions">Health check service options</param>
        /// <param name="healthCheckOptions">SMTP health check options</param>
        /// <returns>The health check service</returns>
        public static HealthCheckService EnableHealthCheck(
            this ISmtpServer server,
            HealthCheckServiceOptions serviceOptions,
            SmtpHealthCheckOptions? healthCheckOptions = null)
        {
            ArgumentNullException.ThrowIfNull(server);

            ArgumentNullException.ThrowIfNull(serviceOptions);

            HealthCheckService service = new(serviceOptions, server.Configuration.LoggerFactory);

            // Add SMTP server health check
            SmtpServerHealthCheck smtpHealthCheck = new(server, healthCheckOptions);
            service.AddHealthCheck("smtp_server", smtpHealthCheck);

            return service;
        }

        /// <summary>
        /// Starts the SMTP server with health check endpoint
        /// </summary>
        /// <param name="server">The SMTP server</param>
        /// <param name="healthCheckPort">The port for health check endpoint</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The health check service</returns>
        public static async Task<HealthCheckService> StartWithHealthCheckAsync(
            this ISmtpServer server,
            int healthCheckPort = 8080,
            CancellationToken cancellationToken = default)
        {
            return await StartWithHealthCheckAsync(server, healthCheckPort, null, cancellationToken);
        }

        /// <summary>
        /// Starts the SMTP server with health check endpoint and configures health checks
        /// </summary>
        /// <param name="server">The SMTP server</param>
        /// <param name="healthCheckPort">The port for health check endpoint</param>
        /// <param name="configureHealthChecks">Action to configure health checks</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The health check service</returns>
        public static async Task<HealthCheckService> StartWithHealthCheckAsync(
            this ISmtpServer server,
            int healthCheckPort,
            Action<HealthCheckService>? configureHealthChecks,
            CancellationToken cancellationToken = default)
        {
            // Start SMTP server
            await server.StartAsync(cancellationToken);

            // Enable and start health check
            HealthCheckService healthCheckService = server.EnableHealthCheck(healthCheckPort);

            // Configure health checks if provided
            configureHealthChecks?.Invoke(healthCheckService);

            await healthCheckService.StartAsync(cancellationToken);

            return healthCheckService;
        }

        /// <summary>
        /// Starts the SMTP server with health check endpoint on specific hostname
        /// </summary>
        /// <param name="server">The SMTP server</param>
        /// <param name="hostname">The hostname to bind health check to</param>
        /// <param name="healthCheckPort">The port for health check endpoint</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The health check service</returns>
        public static async Task<HealthCheckService> StartWithHealthCheckAsync(
            this ISmtpServer server,
            string hostname,
            int healthCheckPort,
            CancellationToken cancellationToken = default)
        {
            return await StartWithHealthCheckAsync(server, hostname, healthCheckPort, null, cancellationToken);
        }

        /// <summary>
        /// Starts the SMTP server with health check endpoint on specific hostname and configures health checks
        /// </summary>
        /// <param name="server">The SMTP server</param>
        /// <param name="hostname">The hostname to bind health check to</param>
        /// <param name="healthCheckPort">The port for health check endpoint</param>
        /// <param name="configureHealthChecks">Action to configure health checks</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The health check service</returns>
        public static async Task<HealthCheckService> StartWithHealthCheckAsync(
            this ISmtpServer server,
            string hostname,
            int healthCheckPort,
            Action<HealthCheckService>? configureHealthChecks,
            CancellationToken cancellationToken = default)
        {
            // Start SMTP server
            await server.StartAsync(cancellationToken);

            // Enable and start health check
            HealthCheckService healthCheckService = server.EnableHealthCheck(hostname, healthCheckPort);

            // Configure health checks if provided
            configureHealthChecks?.Invoke(healthCheckService);

            await healthCheckService.StartAsync(cancellationToken);

            return healthCheckService;
        }

        /// <summary>
        /// Starts the SMTP server with health check endpoint on specific IP address
        /// </summary>
        /// <param name="server">The SMTP server</param>
        /// <param name="ipAddress">The IP address to bind health check to</param>
        /// <param name="healthCheckPort">The port for health check endpoint</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The health check service</returns>
        public static async Task<HealthCheckService> StartWithHealthCheckAsync(
            this ISmtpServer server,
            IPAddress ipAddress,
            int healthCheckPort,
            CancellationToken cancellationToken = default)
        {
            return await StartWithHealthCheckAsync(server, ipAddress, healthCheckPort, null, cancellationToken);
        }

        /// <summary>
        /// Starts the SMTP server with health check endpoint on specific IP address and configures health checks
        /// </summary>
        /// <param name="server">The SMTP server</param>
        /// <param name="ipAddress">The IP address to bind health check to</param>
        /// <param name="healthCheckPort">The port for health check endpoint</param>
        /// <param name="configureHealthChecks">Action to configure health checks</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The health check service</returns>
        public static async Task<HealthCheckService> StartWithHealthCheckAsync(
            this ISmtpServer server,
            IPAddress ipAddress,
            int healthCheckPort,
            Action<HealthCheckService>? configureHealthChecks,
            CancellationToken cancellationToken = default)
        {
            // Start SMTP server
            await server.StartAsync(cancellationToken);

            // Enable and start health check
            HealthCheckService healthCheckService = server.EnableHealthCheck(ipAddress, healthCheckPort);

            // Configure health checks if provided
            configureHealthChecks?.Invoke(healthCheckService);

            await healthCheckService.StartAsync(cancellationToken);

            return healthCheckService;
        }

        /// <summary>
        /// Adds a custom health check to the service
        /// </summary>
        /// <param name="service">The health check service</param>
        /// <param name="name">The name of the health check</param>
        /// <param name="healthCheckFunc">The health check function</param>
        /// <returns>The health check service</returns>
        public static HealthCheckService AddHealthCheck(
            this HealthCheckService service,
            string name,
            Func<CancellationToken, Task<HealthCheckResult>> healthCheckFunc)
        {
            ArgumentNullException.ThrowIfNull(service);

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            ArgumentNullException.ThrowIfNull(healthCheckFunc);

            FunctionalHealthCheck functionalHealthCheck = new(healthCheckFunc);
            service.AddHealthCheck(name, functionalHealthCheck);

            return service;
        }
    }

    /// <summary>
    /// Functional health check implementation
    /// </summary>
    internal class FunctionalHealthCheck(Func<CancellationToken, Task<HealthCheckResult>> checkFunc) : IHealthCheck
    {
        private readonly Func<CancellationToken, Task<HealthCheckResult>> _checkFunc = checkFunc ?? throw new ArgumentNullException(nameof(checkFunc));

        public Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            return _checkFunc(cancellationToken);
        }
    }
}