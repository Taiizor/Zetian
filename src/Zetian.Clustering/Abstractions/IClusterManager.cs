using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Zetian.Clustering.Enums;
using Zetian.Clustering.Models;
using Zetian.Clustering.Models.EventArgs;
using Zetian.Clustering.Options;

namespace Zetian.Clustering.Abstractions
{
    /// <summary>
    /// Main interface for cluster management
    /// </summary>
    public interface IClusterManager : IDisposable
    {
        /// <summary>
        /// Current node ID
        /// </summary>
        string NodeId { get; }

        /// <summary>
        /// Whether this node is the leader
        /// </summary>
        bool IsLeader { get; }

        /// <summary>
        /// Current leader node ID
        /// </summary>
        string? LeaderNodeId { get; }

        /// <summary>
        /// Current cluster state
        /// </summary>
        ClusterState State { get; }

        /// <summary>
        /// All nodes in the cluster
        /// </summary>
        IReadOnlyCollection<IClusterNode> Nodes { get; }

        /// <summary>
        /// Number of active nodes
        /// </summary>
        int NodeCount { get; }

        /// <summary>
        /// Whether the node is in maintenance mode
        /// </summary>
        bool IsInMaintenance { get; }

        /// <summary>
        /// Starts the cluster manager
        /// </summary>
        Task StartAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops the cluster manager
        /// </summary>
        Task StopAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Joins an existing cluster
        /// </summary>
        Task<bool> JoinAsync(string seedNode, CancellationToken cancellationToken = default);

        /// <summary>
        /// Leaves the cluster gracefully
        /// </summary>
        Task LeaveAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Distributes configuration across the cluster
        /// </summary>
        Task DistributeConfigurationAsync<T>(T config, CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Checks rate limit across the cluster
        /// </summary>
        Task<bool> CheckRateLimitAsync(IPAddress clientIp, int requestsPerHour, CancellationToken cancellationToken = default);

        /// <summary>
        /// Replicates state across the cluster
        /// </summary>
        Task<bool> ReplicateStateAsync(string key, byte[] data, ReplicationOptions? options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets replicated state
        /// </summary>
        Task<byte[]?> GetStateAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Migrates sessions from a failed node
        /// </summary>
        Task<int> MigrateSessionsAsync(string fromNodeId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets cluster health information
        /// </summary>
        Task<ClusterHealth> GetHealthAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets cluster metrics
        /// </summary>
        ClusterMetrics GetMetrics();

        /// <summary>
        /// Enters maintenance mode
        /// </summary>
        Task EnterMaintenanceModeAsync(MaintenanceOptions? options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Exits maintenance mode
        /// </summary>
        Task ExitMaintenanceModeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets load balancing strategy
        /// </summary>
        void SetLoadBalancingStrategy(LoadBalancingStrategy strategy, LoadBalancingOptions? options = null);

        /// <summary>
        /// Sets a custom load balancer
        /// </summary>
        void SetCustomLoadBalancer(ILoadBalancer loadBalancer);

        /// <summary>
        /// Sets affinity resolver
        /// </summary>
        void SetAffinityResolver(Func<ISessionInfo, string> resolver);

        /// <summary>
        /// Configures leader election
        /// </summary>
        void ConfigureLeaderElection(Action<LeaderElectionOptions> configure);

        /// <summary>
        /// Configures state replication
        /// </summary>
        void ConfigureReplication(Action<ReplicationConfig> configure);

        /// <summary>
        /// Configures health checks
        /// </summary>
        void ConfigureHealthChecks(Action<HealthCheckOptions> configure);

        /// <summary>
        /// Configures session affinity
        /// </summary>
        void ConfigureAffinity(Action<AffinityOptions> configure);

        /// <summary>
        /// Configures regions for multi-region deployment
        /// </summary>
        void ConfigureRegions(Action<RegionConfig> configure);

        /// <summary>
        /// Enables distributed rate limiting
        /// </summary>
        void EnableDistributedRateLimiting(Action<RateLimitConfig> configure);

        /// <summary>
        /// Enables metrics export
        /// </summary>
        void EnableMetricsExport(Action<MetricsExportConfig> configure);

        /// <summary>
        /// Enables debug logging
        /// </summary>
        void EnableDebugLogging(Action<DebugLoggingOptions> configure);

        #region Events

        /// <summary>
        /// Occurs when a node joins the cluster
        /// </summary>
        event EventHandler<NodeEventArgs>? NodeJoined;

        /// <summary>
        /// Occurs when a node leaves the cluster
        /// </summary>
        event EventHandler<NodeEventArgs>? NodeLeft;

        /// <summary>
        /// Occurs when a node fails
        /// </summary>
        event EventHandler<NodeEventArgs>? NodeFailed;

        /// <summary>
        /// Occurs when the leader changes
        /// </summary>
        event EventHandler<LeaderChangedEventArgs>? LeaderChanged;

        /// <summary>
        /// Occurs when cluster state changes
        /// </summary>
        event EventHandler<ClusterStateEventArgs>? StateChanged;

        /// <summary>
        /// Occurs when rebalancing starts
        /// </summary>
        event EventHandler<RebalancingEventArgs>? RebalancingStarted;

        /// <summary>
        /// Occurs when rebalancing completes
        /// </summary>
        event EventHandler<RebalancingEventArgs>? RebalancingCompleted;

        /// <summary>
        /// Occurs when configuration is updated
        /// </summary>
        event EventHandler<ConfigurationEventArgs>? ConfigurationUpdated;

        /// <summary>
        /// Occurs when a partition is detected
        /// </summary>
        event EventHandler<PartitionEventArgs>? PartitionDetected;

        /// <summary>
        /// Occurs when a partition is healed
        /// </summary>
        event EventHandler<PartitionEventArgs>? PartitionHealed;

        #endregion
    }
}