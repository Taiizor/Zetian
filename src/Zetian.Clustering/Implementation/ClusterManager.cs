using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Zetian.Abstractions;
using Zetian.Clustering.Abstractions;
using Zetian.Clustering.Enums;
using Zetian.Clustering.Models;
using Zetian.Clustering.Models.EventArgs;
using Zetian.Clustering.Options;

namespace Zetian.Clustering.Implementation
{
    /// <summary>
    /// Main implementation of cluster manager
    /// </summary>
    public class ClusterManager : IClusterManager
    {
        private readonly ISmtpServer _server;
        private readonly ClusterOptions _options;
        private readonly ILogger<ClusterManager> _logger;
        private readonly ConcurrentDictionary<string, ClusterNode> _nodes;
        private readonly ConcurrentDictionary<string, SessionInfo> _sessions;
        private readonly SemaphoreSlim _stateLock;
        private bool _disposed;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _heartbeatTask;
        private Task? _healthCheckTask;

        public ClusterManager(ISmtpServer server, ClusterOptions options, ILogger<ClusterManager> logger)
        {
            _server = server ?? throw new ArgumentNullException(nameof(server));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            NodeId = _options.NodeId;
            _nodes = new ConcurrentDictionary<string, ClusterNode>();
            _sessions = new ConcurrentDictionary<string, SessionInfo>();
            _stateLock = new SemaphoreSlim(1, 1);
            State = ClusterState.Forming;
        }

        public string NodeId { get; }
        public bool IsLeader { get; private set; }
        public string? LeaderNodeId { get; private set; }
        public ClusterState State { get; private set; }
        public IReadOnlyCollection<IClusterNode> Nodes => (IReadOnlyCollection<IClusterNode>)_nodes.Values;
        public int NodeCount => _nodes.Count;
        public bool IsInMaintenance { get; private set; }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            await _stateLock.WaitAsync(cancellationToken);
            try
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(ClusterManager));
                }

                _logger.LogInformation("Starting cluster manager for node {NodeId}", NodeId);

                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                // Add self to nodes
                ClusterNode selfNode = new()
                {
                    Id = NodeId,
                    Endpoint = new IPEndPoint(IPAddress.Any, _options.ClusterPort),
                    State = NodeState.Active,
                    JoinTime = DateTime.UtcNow
                };
                _nodes.TryAdd(NodeId, selfNode);

                // Start background tasks
                _heartbeatTask = Task.Run(() => HeartbeatLoopAsync(_cancellationTokenSource.Token));
                _healthCheckTask = Task.Run(() => HealthCheckLoopAsync(_cancellationTokenSource.Token));

                // Perform initial discovery
                await DiscoverNodesAsync(cancellationToken);

                // Update state
                UpdateClusterState();

                _logger.LogInformation("Cluster manager started successfully");
            }
            finally
            {
                _stateLock.Release();
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            await _stateLock.WaitAsync(cancellationToken);
            try
            {
                _logger.LogInformation("Stopping cluster manager");

                _cancellationTokenSource?.Cancel();

                if (_heartbeatTask != null)
                {
                    await _heartbeatTask;
                }

                if (_healthCheckTask != null)
                {
                    await _healthCheckTask;
                }

                State = ClusterState.ShuttingDown;

                _logger.LogInformation("Cluster manager stopped");
            }
            finally
            {
                _stateLock.Release();
            }
        }

        public async Task<bool> JoinAsync(string seedNode, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Attempting to join cluster via seed node {SeedNode}", seedNode);

                // Implementation would connect to seed node and exchange cluster information
                // For now, return true to indicate success
                await Task.Delay(100, cancellationToken); // Simulate network operation

                NodeEventArgs nodeJoinedArgs = new()
                {
                    NodeId = NodeId,
                    State = NodeState.Active,
                    SourceNodeId = NodeId
                };
                OnNodeJoined(nodeJoinedArgs);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to join cluster via {SeedNode}", seedNode);
                return false;
            }
        }

        public async Task LeaveAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Leaving cluster");

            // Notify other nodes
            await NotifyLeavingAsync(cancellationToken);

            // Remove self from nodes
            _nodes.TryRemove(NodeId, out _);

            NodeEventArgs nodeLeftArgs = new()
            {
                NodeId = NodeId,
                State = NodeState.Leaving,
                Reason = "Graceful leave",
                SourceNodeId = NodeId
            };
            OnNodeLeft(nodeLeftArgs);
        }

        public async Task RegisterSessionAsync(ISmtpSession session, CancellationToken cancellationToken = default)
        {
            // Extract IP address from RemoteEndPoint
            IPAddress clientIp = IPAddress.Any;
            int clientPort = 0;

            if (session.RemoteEndPoint is IPEndPoint ipEndPoint)
            {
                clientIp = ipEndPoint.Address;
                clientPort = ipEndPoint.Port;
            }

            SessionInfo sessionInfo = new()
            {
                SessionId = session.Id,
                NodeId = NodeId,
                ClientIp = clientIp,
                ClientPort = clientPort,
                StartTime = session.StartTime,
                Priority = 1, // Default priority
                EstimatedSize = 0 // Will be updated as session progresses
                // Metadata is initialized via private _metadata field default value
            };

            // Apply affinity resolver if configured
            if (_options.Properties.TryGetValue("AffinityResolver", out object? resolver) && resolver is Func<ISessionInfo, string> affinityResolver)
            {
                string affinityKey = affinityResolver(sessionInfo);
                sessionInfo.AffinityKey = affinityKey;
                _logger.LogDebug("Session {SessionId} affinity key: {AffinityKey}", session.Id, affinityKey);
            }

            _sessions.TryAdd(session.Id, sessionInfo);

            // Replicate session info if needed
            if (_options.ReplicationFactor > 1)
            {
                await ReplicateSessionInfoAsync(sessionInfo, cancellationToken);
            }

            _logger.LogDebug("Registered session {SessionId} on node {NodeId}", session.Id, NodeId);
        }

        public async Task UnregisterSessionAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            if (_sessions.TryRemove(sessionId, out _))
            {
                _logger.LogDebug("Unregistered session {SessionId}", sessionId);

                // Remove replicated session info
                if (_options.ReplicationFactor > 1)
                {
                    await RemoveReplicatedSessionAsync(sessionId, cancellationToken);
                }
            }
        }

        public async Task<bool> CheckRateLimitAsync(IPAddress clientIp, int requestsPerHour, CancellationToken cancellationToken = default)
        {
            // Distributed rate limiting implementation
            // Would coordinate with other nodes to track global limits
            await Task.Delay(1, cancellationToken); // Placeholder
            return true;
        }

        public async Task<bool> ReplicateStateAsync(string key, byte[] data, ReplicationOptions? options = null, CancellationToken cancellationToken = default)
        {
            options ??= new ReplicationOptions();

            // Check for custom replication config
            if (_options.Properties.TryGetValue("ReplicationConfig", out object? replicationConfig) && replicationConfig is ReplicationConfig config)
            {
                // Apply custom replication settings
                if (config.CustomReplicationFactor > 0)
                {
                    // Use custom replication factor
                    options.ConsistencyLevel = config.PreferredConsistencyLevel ?? options.ConsistencyLevel;
                }
            }

            // Replicate to required number of nodes
            List<ClusterNode> targetNodes = SelectReplicationTargets(options.ConsistencyLevel);
            IEnumerable<Task<bool>> replicationTasks = targetNodes.Select(node =>
                ReplicateToNodeAsync(node, key, data, options, cancellationToken)
            );

            bool[] results = await Task.WhenAll(replicationTasks);
            int successCount = results.Count(r => r);

            return successCount >= _options.MinReplicasForWrite;
        }

        public async Task<byte[]?> GetStateAsync(string key, CancellationToken cancellationToken = default)
        {
            // Try local storage first
            if (_options.StateStore != null)
            {
                return await _options.StateStore.GetAsync(key, cancellationToken);
            }

            return null;
        }

        public async Task<int> MigrateSessionsAsync(string fromNodeId, CancellationToken cancellationToken = default)
        {
            List<SessionInfo> sessionsToMigrate = _sessions.Values.Where(s => s.NodeId == fromNodeId).ToList();
            List<ClusterNode> targetNodes = SelectMigrationTargets(sessionsToMigrate.Count);
            int migratedCount = 0;

            foreach (SessionInfo session in sessionsToMigrate)
            {
                ClusterNode targetNode = targetNodes[migratedCount % targetNodes.Count];
                if (await MigrateSessionToNodeAsync(session, targetNode, cancellationToken))
                {
                    migratedCount++;
                }
            }

            _logger.LogInformation("Migrated {Count} sessions from {FromNode}", migratedCount, fromNodeId);
            return migratedCount;
        }

        public async Task DistributeConfigurationAsync<T>(T config, CancellationToken cancellationToken = default) where T : class
        {
            // Distribute configuration to all nodes
            IEnumerable<Task> tasks = _nodes.Values
                .Where(n => n.Id != NodeId)
                .Select(n => SendConfigurationToNodeAsync(n, config, cancellationToken));

            await Task.WhenAll(tasks);

            ConfigurationEventArgs configArgs = new()
            {
                ConfigurationType = typeof(T).Name,
                NewValue = config,
                Success = true,
                UpdatedNodes = _nodes.Keys.ToList(),
                SourceNodeId = NodeId
            };
            OnConfigurationUpdated(configArgs);
        }

        public async Task<ClusterHealth> GetHealthAsync(CancellationToken cancellationToken = default)
        {
            ClusterHealth health = new()
            {
                Status = State,
                TotalNodes = _nodes.Count,
                HealthyNodes = _nodes.Values.Count(n => n.State == NodeState.Active),
                UnhealthyNodes = _nodes.Values.Count(n => n.State is NodeState.Failed or NodeState.Suspected),
                HasQuorum = HasQuorum(),
                LeaderNodeId = LeaderNodeId,
                LastCheckTime = DateTime.UtcNow
            };

            foreach (ClusterNode node in _nodes.Values)
            {
                health.NodeStatuses[node.Id] = new NodeHealthStatus
                {
                    NodeId = node.Id,
                    State = node.State,
                    IsHealthy = node.State == NodeState.Active,
                    CpuUsage = node.CpuUsage,
                    MemoryUsage = node.MemoryUsage,
                    ActiveSessions = node.ActiveSessions,
                    LastSuccessfulCheck = node.LastHeartbeat
                };
            }

            await Task.CompletedTask;
            return health;
        }

        public ClusterMetrics GetMetrics()
        {
            return new ClusterMetrics
            {
                TotalSessions = _sessions.Count,
                MessagesPerSecond = CalculateMessagesPerSecond(),
                AverageLoad = CalculateAverageLoad(),
                NetworkBandwidth = CalculateNetworkBandwidth(),
                TotalMessagesProcessed = GetTotalMessagesProcessed(),
                NodeFailures = _nodes.Values.Count(n => n.State == NodeState.Failed),
                ClusterUptime = DateTime.UtcNow - _nodes.Values.Min(n => n.JoinTime)
            };
        }

        public async Task EnterMaintenanceModeAsync(MaintenanceOptions? options = null, CancellationToken cancellationToken = default)
        {
            options ??= new MaintenanceOptions();

            IsInMaintenance = true;
            _logger.LogInformation("Entering maintenance mode: {Reason}", options.Reason);

            if (options.MigrateSessions)
            {
                await MigrateLocalSessionsAsync(cancellationToken);
            }

            // Update node state
            if (_nodes.TryGetValue(NodeId, out ClusterNode? node))
            {
                node.State = NodeState.Maintenance;
                node.IsInMaintenance = true;
            }
        }

        public async Task ExitMaintenanceModeAsync(CancellationToken cancellationToken = default)
        {
            IsInMaintenance = false;
            _logger.LogInformation("Exiting maintenance mode");

            // Update node state
            if (_nodes.TryGetValue(NodeId, out ClusterNode? node))
            {
                node.State = NodeState.Active;
                node.IsInMaintenance = false;
            }

            await Task.CompletedTask;
        }

        #region Configuration Methods

        public void SetLoadBalancingStrategy(LoadBalancingStrategy strategy, LoadBalancingOptions? options = null)
        {
            _options.LoadBalancing.Strategy = strategy;
            if (options != null)
            {
                _options.LoadBalancing = options;
            }
        }

        public void SetCustomLoadBalancer(ILoadBalancer loadBalancer)
        {
            // Store custom load balancer
            _options.Properties["CustomLoadBalancer"] = loadBalancer;
        }

        public void SetAffinityResolver(Func<ISessionInfo, string> resolver)
        {
            _options.Properties["AffinityResolver"] = resolver;
        }

        public void ConfigureLeaderElection(Action<LeaderElectionOptions> configure)
        {
            configure(_options.LeaderElection);
        }

        public void ConfigureReplication(Action<ReplicationConfig> configure)
        {
            ReplicationConfig config = new();
            configure(config);
            _options.Properties["ReplicationConfig"] = config;
        }

        public void ConfigureHealthChecks(Action<HealthCheckOptions> configure)
        {
            configure(_options.HealthCheck);
        }

        public void ConfigureAffinity(Action<AffinityOptions> configure)
        {
            configure(_options.LoadBalancing.Affinity);
        }

        public void ConfigureRegions(Action<RegionConfig> configure)
        {
            RegionConfig config = new();
            configure(config);
            _options.Properties["RegionConfig"] = config;
        }

        public void EnableDistributedRateLimiting(Action<RateLimitConfig> configure)
        {
            RateLimitConfig config = new();
            configure(config);
            _options.Properties["RateLimitConfig"] = config;
        }

        public void EnableMetricsExport(Action<MetricsExportConfig> configure)
        {
            MetricsExportConfig config = new();
            configure(config);
            _options.Properties["MetricsExportConfig"] = config;
        }

        public void EnableDebugLogging(Action<DebugLoggingOptions> configure)
        {
            DebugLoggingOptions config = new();
            configure(config);
            _options.Properties["DebugLoggingOptions"] = config;
        }

        #endregion

        #region Events

        public event EventHandler<NodeEventArgs>? NodeJoined;
        public event EventHandler<NodeEventArgs>? NodeLeft;
        public event EventHandler<NodeEventArgs>? NodeFailed;
        public event EventHandler<LeaderChangedEventArgs>? LeaderChanged;
        public event EventHandler<ClusterStateEventArgs>? StateChanged;
        public event EventHandler<RebalancingEventArgs>? RebalancingStarted;
        public event EventHandler<RebalancingEventArgs>? RebalancingCompleted;
        public event EventHandler<ConfigurationEventArgs>? ConfigurationUpdated;
        public event EventHandler<PartitionEventArgs>? PartitionDetected;
        public event EventHandler<PartitionEventArgs>? PartitionHealed;

        protected virtual void OnNodeJoined(NodeEventArgs e)
        {
            NodeJoined?.Invoke(this, e);
        }

        protected virtual void OnNodeLeft(NodeEventArgs e)
        {
            NodeLeft?.Invoke(this, e);
        }

        protected virtual void OnNodeFailed(NodeEventArgs e)
        {
            NodeFailed?.Invoke(this, e);
        }

        protected virtual void OnLeaderChanged(LeaderChangedEventArgs e)
        {
            LeaderChanged?.Invoke(this, e);
        }

        protected virtual void OnStateChanged(ClusterStateEventArgs e)
        {
            StateChanged?.Invoke(this, e);
        }

        protected virtual void OnRebalancingStarted(RebalancingEventArgs e)
        {
            RebalancingStarted?.Invoke(this, e);
        }

        protected virtual void OnRebalancingCompleted(RebalancingEventArgs e)
        {
            RebalancingCompleted?.Invoke(this, e);
        }

        protected virtual void OnConfigurationUpdated(ConfigurationEventArgs e)
        {
            ConfigurationUpdated?.Invoke(this, e);
        }

        protected virtual void OnPartitionDetected(PartitionEventArgs e)
        {
            PartitionDetected?.Invoke(this, e);
        }

        protected virtual void OnPartitionHealed(PartitionEventArgs e)
        {
            PartitionHealed?.Invoke(this, e);
        }

        #endregion

        #region Private Methods

        private async Task HeartbeatLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await SendHeartbeatsAsync(cancellationToken);
                    await Task.Delay(_options.LeaderElection.HeartbeatInterval, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in heartbeat loop");
                }
            }
        }

        private async Task HealthCheckLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await CheckNodesHealthAsync(cancellationToken);
                    await Task.Delay(_options.HealthCheck.CheckInterval, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in health check loop");
                }
            }
        }

        private async Task DiscoverNodesAsync(CancellationToken cancellationToken)
        {
            // Implementation would depend on discovery method
            await Task.CompletedTask;
        }

        private async Task SendHeartbeatsAsync(CancellationToken cancellationToken)
        {
            // Check for debug logging
            if (_options.Properties.TryGetValue("DebugLoggingOptions", out object? debugOptions) && debugOptions is DebugLoggingOptions debug)
            {
                if (debug.IncludeHeartbeats)
                {
                    _logger.LogDebug("Sending heartbeats to {Count} nodes", _nodes.Count - 1);
                }
            }

            // Send heartbeats to other nodes
            await Task.CompletedTask;
        }

        private async Task CheckNodesHealthAsync(CancellationToken cancellationToken)
        {
            // Check for rate limit config
            if (_options.Properties.TryGetValue("RateLimitConfig", out object? rateLimitConfig) && rateLimitConfig is RateLimitConfig rlConfig)
            {
                // Apply rate limiting checks
                _logger.LogDebug("Checking nodes with rate limit config: {MaxRequests}/sec", rlConfig.MaxRequestsPerSecond);
            }

            // Check for metrics export config
            if (_options.Properties.TryGetValue("MetricsExportConfig", out object? metricsConfig) && metricsConfig is MetricsExportConfig mConfig)
            {
                // Export metrics if configured
                if (mConfig.Exporters?.Count > 0)
                {
                    _logger.LogDebug("Exporting metrics to {Count} exporters", mConfig.Exporters.Count);
                }
            }

            // Check health of other nodes
            await Task.CompletedTask;
        }

        private async Task NotifyLeavingAsync(CancellationToken cancellationToken)
        {
            // Notify other nodes that this node is leaving
            await Task.CompletedTask;
        }

        private async Task ReplicateSessionInfoAsync(SessionInfo session, CancellationToken cancellationToken)
        {
            // Replicate session information to other nodes
            await Task.CompletedTask;
        }

        private async Task RemoveReplicatedSessionAsync(string sessionId, CancellationToken cancellationToken)
        {
            // Remove replicated session from other nodes
            await Task.CompletedTask;
        }

        private async Task<bool> ReplicateToNodeAsync(ClusterNode node, string key, byte[] data, ReplicationOptions options, CancellationToken cancellationToken)
        {
            // Replicate data to specific node
            await Task.Delay(1, cancellationToken); // Placeholder
            return true;
        }

        private async Task<bool> MigrateSessionToNodeAsync(SessionInfo session, ClusterNode targetNode, CancellationToken cancellationToken)
        {
            // Migrate session to target node
            await Task.Delay(1, cancellationToken); // Placeholder
            return true;
        }

        private async Task SendConfigurationToNodeAsync<T>(ClusterNode node, T config, CancellationToken cancellationToken) where T : class
        {
            // Send configuration to specific node
            await Task.Delay(1, cancellationToken); // Placeholder
        }

        private async Task MigrateLocalSessionsAsync(CancellationToken cancellationToken)
        {
            // Migrate all local sessions to other nodes
            await Task.CompletedTask;
        }

        private List<ClusterNode> SelectReplicationTargets(ConsistencyLevel consistency)
        {
            // Check for custom load balancer
            if (_options.Properties.TryGetValue("CustomLoadBalancer", out object? loadBalancer) && loadBalancer is ILoadBalancer customLb)
            {
                // Use custom load balancer to select replication targets
                List<ClusterNode> selectedNodes = [];
                List<ClusterNode> availableNodes = _nodes.Values.Where(n => n.Id != NodeId && n.State == NodeState.Active).ToList();

                for (int i = 0; i < Math.Min(_options.ReplicationFactor - 1, availableNodes.Count); i++)
                {
                    IClusterNode? node = customLb.SelectNodeAsync(null!, availableNodes, CancellationToken.None).Result;
                    if (node is not null and ClusterNode clusterNode)
                    {
                        selectedNodes.Add(clusterNode);
                        availableNodes.Remove(clusterNode);
                    }
                }
                return selectedNodes;
            }

            // Default selection based on consistency level
            return _nodes.Values
                .Where(n => n.Id != NodeId && n.State == NodeState.Active)
                .Take(_options.ReplicationFactor - 1)
                .ToList();
        }

        private List<ClusterNode> SelectMigrationTargets(int sessionCount)
        {
            // Check for custom load balancer
            if (_options.Properties.TryGetValue("CustomLoadBalancer", out object? loadBalancer) && loadBalancer is ILoadBalancer customLb)
            {
                List<ClusterNode> availableNodes = _nodes.Values
                    .Where(n => n.Id != NodeId && n.State == NodeState.Active && !n.IsInMaintenance)
                    .ToList();
                return availableNodes.Cast<ClusterNode>().ToList();
            }

            // Default selection based on active sessions
            return _nodes.Values
                .Where(n => n.Id != NodeId && n.State == NodeState.Active && !n.IsInMaintenance)
                .OrderBy(n => n.ActiveSessions)
                .ToList();
        }

        private void UpdateClusterState()
        {
            ClusterState oldState = State;

            if (_nodes.Count == 0)
            {
                State = ClusterState.Forming;
            }
            else if (HasQuorum())
            {
                State = ClusterState.Healthy;
            }
            else
            {
                State = ClusterState.NoQuorum;
            }

            if (oldState != State)
            {
                OnStateChanged(new ClusterStateEventArgs
                {
                    OldState = oldState,
                    NewState = State,
                    ActiveNodes = _nodes.Values.Count(n => n.State == NodeState.Active),
                    HasQuorum = HasQuorum(),
                    SourceNodeId = NodeId
                });
            }
        }

        private bool HasQuorum()
        {
            int activeNodes = _nodes.Values.Count(n => n.State == NodeState.Active);
            int requiredQuorum = (_nodes.Count / 2) + 1;
            return activeNodes >= requiredQuorum;
        }

        private double CalculateMessagesPerSecond()
        {
            // Calculate messages per second across cluster
            return 0; // Placeholder
        }

        private double CalculateAverageLoad()
        {
            // Calculate average load across nodes
            if (_nodes.Count == 0)
            {
                return 0;
            }

            return _nodes.Values.Average(n => n.CurrentLoad) * 100;
        }

        private long CalculateNetworkBandwidth()
        {
            // Calculate total network bandwidth
            return _nodes.Values.Sum(n => n.NetworkBandwidth);
        }

        private long GetTotalMessagesProcessed()
        {
            // Get total messages processed
            return 0; // Placeholder
        }

        #endregion

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _stateLock?.Dispose();
        }

        #region Internal Classes

        private class ClusterNode : IClusterNode
        {
            public string Id { get; set; } = string.Empty;
            public IPEndPoint Endpoint { get; set; } = new IPEndPoint(IPAddress.None, 0);
            public NodeState State { get; set; }
            public bool IsLeader { get; set; }
            public double CurrentLoad { get; set; }
            public int ActiveSessions { get; set; }
            public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;
            public IReadOnlyDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
            public Version Version { get; set; } = new Version(1, 0, 0);
            public string? Region { get; set; }
            public string? AvailabilityZone { get; set; }
            public double CpuUsage { get; set; }
            public double MemoryUsage { get; set; }
            public long NetworkBandwidth { get; set; }
            public DateTime JoinTime { get; set; }
            public int Weight { get; set; } = 1;
            public bool IsInMaintenance { get; set; }
        }

        private class SessionInfo : ISessionInfo
        {
            public string SessionId { get; set; } = string.Empty;
            public string NodeId { get; set; } = string.Empty;
            public IPAddress ClientIp { get; set; } = IPAddress.Any;
            public int ClientPort { get; set; }
            public long EstimatedSize { get; set; }
            public int Priority { get; set; } = 1;
            private Dictionary<string, string> _metadata = [];
            public IReadOnlyDictionary<string, string> Metadata => _metadata;
            public string? AffinityKey { get; set; }
            public IPEndPoint? ClientEndpoint { get; set; }
            public DateTime StartTime { get; set; }
            public long BytesTransferred { get; set; }
            public int MessageCount { get; set; }
        }

        #endregion
    }
}