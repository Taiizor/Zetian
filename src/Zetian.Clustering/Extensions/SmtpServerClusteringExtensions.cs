using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Zetian.Abstractions;
using Zetian.Clustering.Abstractions;
using Zetian.Clustering.Implementation;
using Zetian.Clustering.Options;

namespace Zetian.Clustering.Extensions
{
    /// <summary>
    /// Extension methods for enabling clustering on SMTP server
    /// </summary>
    public static class SmtpServerClusteringExtensions
    {
        /// <summary>
        /// Enables clustering for the SMTP server
        /// </summary>
        public static async Task<IClusterManager> EnableClusteringAsync(
            this ISmtpServer server,
            Action<ClusterOptions>? configure = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(server);

            // Create and configure options
            ClusterOptions options = new();
            configure?.Invoke(options);
            options.Validate();

            // Create logger
            ILoggerFactory loggerFactory = server.Configuration.LoggerFactory ??
                                Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;
            ILogger<ClusterManager> logger = loggerFactory.CreateLogger<ClusterManager>();

            // Create cluster manager
            ClusterManager clusterManager = new(server, options, logger);

            // Hook up server events for session management
            server.SessionCreated += async (sender, e) =>
            {
                await clusterManager.RegisterSessionAsync(e.Session, cancellationToken);
            };

            server.SessionCompleted += async (sender, e) =>
            {
                await clusterManager.UnregisterSessionAsync(e.Session.Id, cancellationToken);
            };

            // Store cluster manager in server properties for later access
            server.Configuration.Properties["ClusterManager"] = clusterManager;

            // Start the cluster
            await clusterManager.StartAsync(cancellationToken);

            return clusterManager;
        }

        /// <summary>
        /// Gets the cluster manager if clustering is enabled
        /// </summary>
        public static IClusterManager? GetClusterManager(this ISmtpServer server)
        {
            if (server?.Configuration?.Properties == null)
            {
                return null;
            }

            return server.Configuration.Properties.TryGetValue("ClusterManager", out var manager)
                ? manager as IClusterManager
                : null;
        }

        /// <summary>
        /// Checks if clustering is enabled
        /// </summary>
        public static bool IsClusteringEnabled(this ISmtpServer server)
        {
            return server.GetClusterManager() != null;
        }

        /// <summary>
        /// Gets cluster metrics if clustering is enabled
        /// </summary>
        public static Models.ClusterMetrics? GetClusterMetrics(this ISmtpServer server)
        {
            return server.GetClusterManager()?.GetMetrics();
        }

        /// <summary>
        /// Gets cluster health if clustering is enabled
        /// </summary>
        public static async Task<Models.ClusterHealth?> GetClusterHealthAsync(
            this ISmtpServer server,
            CancellationToken cancellationToken = default)
        {
            IClusterManager? manager = server.GetClusterManager();
            if (manager == null)
            {
                return null;
            }

            return await manager.GetHealthAsync(cancellationToken);
        }

        /// <summary>
        /// Checks if the current node is the cluster leader
        /// </summary>
        public static bool IsClusterLeader(this ISmtpServer server)
        {
            return server.GetClusterManager()?.IsLeader ?? false;
        }

        /// <summary>
        /// Gets the number of nodes in the cluster
        /// </summary>
        public static int GetClusterNodeCount(this ISmtpServer server)
        {
            return server.GetClusterManager()?.NodeCount ?? 1;
        }

        /// <summary>
        /// Enters maintenance mode on the cluster node
        /// </summary>
        public static async Task EnterClusterMaintenanceModeAsync(
            this ISmtpServer server,
            Models.MaintenanceOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            IClusterManager? manager = server.GetClusterManager();
            if (manager != null)
            {
                await manager.EnterMaintenanceModeAsync(options, cancellationToken);
            }
        }

        /// <summary>
        /// Exits maintenance mode on the cluster node
        /// </summary>
        public static async Task ExitClusterMaintenanceModeAsync(
            this ISmtpServer server,
            CancellationToken cancellationToken = default)
        {
            IClusterManager? manager = server.GetClusterManager();
            if (manager != null)
            {
                await manager.ExitMaintenanceModeAsync(cancellationToken);
            }
        }

        /// <summary>
        /// Gracefully leaves the cluster
        /// </summary>
        public static async Task LeaveClusterAsync(
            this ISmtpServer server,
            CancellationToken cancellationToken = default)
        {
            IClusterManager? manager = server.GetClusterManager();
            if (manager != null)
            {
                await manager.LeaveAsync(cancellationToken);
                server.Configuration.Properties.Remove("ClusterManager");
                manager.Dispose();
            }
        }
    }
}