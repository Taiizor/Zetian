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
using Zetian.Clustering.Network;
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
        private readonly ConcurrentDictionary<string, byte[]> _replicatedState;
        private readonly SemaphoreSlim _stateLock;
        private IClusterNetworkClient? _networkClient;
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
            _replicatedState = new ConcurrentDictionary<string, byte[]>();
            _stateLock = new SemaphoreSlim(1, 1);
            State = ClusterState.Forming;

            // Initialize network client if provided in options
            if (_options.Properties.TryGetValue("NetworkClient", out object? networkClient) && networkClient is IClusterNetworkClient client)
            {
                _networkClient = client;
                _networkClient.MessageReceived += OnNetworkMessageReceived;
            }
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
#if NET6_0
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(ClusterManager));
                }
#else
                ObjectDisposedException.ThrowIf(_disposed, this);
#endif

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
                _heartbeatTask = Task.Run(() => HeartbeatLoopAsync(_cancellationTokenSource.Token), cancellationToken);
                _healthCheckTask = Task.Run(() => HealthCheckLoopAsync(_cancellationTokenSource.Token), cancellationToken);

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

            // If no target nodes available (single node cluster), consider it successful if we can store locally
            if (targetNodes.Count == 0)
            {
                // Store locally if possible
                if (_options.StateStore != null)
                {
                    await _options.StateStore.SetAsync(key, data, cancellationToken: cancellationToken);
                    return true;
                }
                // Single node cluster without storage - always succeed
                // This is acceptable for testing and development scenarios
                return true;
            }

            IEnumerable<Task<bool>> replicationTasks = targetNodes.Select(node =>
                ReplicateToNodeAsync(node, key, data, options, cancellationToken)
            );

            bool[] results = await Task.WhenAll(replicationTasks);
            int successCount = results.Count(r => r) + 1; // +1 for the local node

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
                ClusterUptime = !_nodes.IsEmpty ? DateTime.UtcNow - _nodes.Values.Min(n => n.JoinTime) : TimeSpan.Zero
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
            // Check if seed nodes are configured
            if (_options.Properties.TryGetValue("SeedNodes", out object? seedNodes) && seedNodes is List<string> seeds)
            {
                foreach (string seed in seeds)
                {
                    try
                    {
                        // Parse seed node endpoint
                        string[] parts = seed.Split(':');
                        if (parts.Length == 2 && IPAddress.TryParse(parts[0], out IPAddress? ip) && int.TryParse(parts[1], out int port))
                        {
                            IPEndPoint endpoint = new(ip, port);

                            // Send join request to seed node
                            if (_networkClient != null)
                            {
                                ClusterMessage joinMessage = new()
                                {
                                    Type = MessageType.Join,
                                    SourceNodeId = NodeId,
                                    RequiresAck = true,
                                    Payload = new
                                    {
                                        NodeId = NodeId,
                                        Endpoint = new IPEndPoint(IPAddress.Any, _options.ClusterPort).ToString(),
                                        Version = GetType().Assembly.GetName().Version?.ToString() ?? "1.0.0",
                                        Capabilities = new[] { "replication", "migration", "monitoring" }
                                    }
                                };

                                AcknowledgmentMessage? ack = await _networkClient.SendMessageAsync(endpoint, joinMessage, cancellationToken);

                                if (ack?.Success == true && ack.Result != null)
                                {
                                    // Process cluster topology from seed node
                                    _logger.LogInformation("Successfully joined cluster via seed node {Seed}", seed);
                                    break;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to connect to seed node {Seed}", seed);
                    }
                }
            }

            // Start network listener if configured
            if (_networkClient != null && _options.ClusterPort > 0)
            {
                await _networkClient.StartListeningAsync(_options.ClusterPort, cancellationToken);
                _logger.LogInformation("Started listening on cluster port {Port}", _options.ClusterPort);
            }
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

            if (_networkClient == null)
            {
                return;
            }

            HeartbeatPayload payload = new()
            {
                State = _nodes.TryGetValue(NodeId, out ClusterNode? self) ? self.State : NodeState.Active,
                CpuUsage = self?.CpuUsage ?? 0,
                MemoryUsage = self?.MemoryUsage ?? 0,
                ActiveSessions = _sessions.Count,
                NetworkBandwidth = self?.NetworkBandwidth ?? 0,
                LastUpdate = DateTime.UtcNow,
                Metrics = new Dictionary<string, object>
                {
                    ["MessageCount"] = _sessions.Values.Sum(s => s.MessageCount),
                    ["BytesTransferred"] = _sessions.Values.Sum(s => s.BytesTransferred),
                    ["IsLeader"] = IsLeader
                }
            };

            ClusterMessage heartbeat = new()
            {
                Type = MessageType.Heartbeat,
                SourceNodeId = NodeId,
                Payload = payload,
                RequiresAck = false
            };

            // Send heartbeat to all other nodes
            List<Task> tasks = [];
            foreach (ClusterNode node in _nodes.Values.Where(n => n.Id != NodeId))
            {
                tasks.Add(_networkClient.SendMessageAsync(node.Endpoint, heartbeat, cancellationToken));
            }

            await Task.WhenAll(tasks);
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

            // Check health of other nodes based on heartbeat timestamps
            DateTime now = DateTime.UtcNow;
            TimeSpan heartbeatTimeout = _options.HealthCheck.FailureThreshold;

            foreach (ClusterNode node in _nodes.Values.Where(n => n.Id != NodeId))
            {
                TimeSpan timeSinceLastHeartbeat = now - node.LastHeartbeat;

                if (timeSinceLastHeartbeat > heartbeatTimeout)
                {
                    if (node.State == NodeState.Active)
                    {
                        node.State = NodeState.Suspected;
                        _logger.LogWarning("Node {NodeId} is suspected (no heartbeat for {Duration})", node.Id, timeSinceLastHeartbeat);

                        OnNodeFailed(new NodeEventArgs
                        {
                            NodeId = node.Id,
                            State = NodeState.Suspected,
                            Reason = "Heartbeat timeout",
                            SourceNodeId = NodeId
                        });
                    }
                    else if (node.State == NodeState.Suspected && timeSinceLastHeartbeat > TimeSpan.FromSeconds(heartbeatTimeout.TotalSeconds * 2))
                    {
                        node.State = NodeState.Failed;
                        _logger.LogError("Node {NodeId} has failed (no heartbeat for {Duration})", node.Id, timeSinceLastHeartbeat);

                        OnNodeFailed(new NodeEventArgs
                        {
                            NodeId = node.Id,
                            State = NodeState.Failed,
                            Reason = "Node unresponsive",
                            SourceNodeId = NodeId
                        });

                        // Trigger session migration from failed node
                        _ = Task.Run(() => MigrateSessionsAsync(node.Id, cancellationToken), cancellationToken);
                    }
                }
            }

            await Task.CompletedTask;
        }

        private async Task NotifyLeavingAsync(CancellationToken cancellationToken)
        {
            if (_networkClient == null)
            {
                return;
            }

            ClusterMessage leaveMessage = new()
            {
                Type = MessageType.Leave,
                SourceNodeId = NodeId,
                RequiresAck = false,
                Payload = new
                {
                    Reason = "Graceful shutdown",
                    Timestamp = DateTime.UtcNow
                }
            };

            // Notify all other nodes
            await _networkClient.BroadcastMessageAsync(leaveMessage, cancellationToken);

            _logger.LogInformation("Notified cluster nodes about leaving");
        }

        private async Task ReplicateSessionInfoAsync(SessionInfo session, CancellationToken cancellationToken)
        {
            if (_networkClient == null || _nodes.Count <= 1)
            {
                return;
            }

            SessionReplicationPayload payload = new()
            {
                SessionId = session.SessionId,
                NodeId = session.NodeId,
                ClientIp = session.ClientIp.ToString(),
                ClientPort = session.ClientPort,
                StartTime = session.StartTime,
                Metadata = session.Metadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                BytesTransferred = session.BytesTransferred,
                MessageCount = session.MessageCount
            };

            ClusterMessage message = new()
            {
                Type = MessageType.SessionReplicate,
                SourceNodeId = NodeId,
                Payload = payload,
                RequiresAck = true
            };

            // Select replication targets
            List<ClusterNode> targets = SelectReplicationTargets(ConsistencyLevel.Quorum);

            List<Task<AcknowledgmentMessage?>> tasks = [];
            foreach (ClusterNode target in targets)
            {
                tasks.Add(_networkClient.SendMessageAsync(target.Endpoint, message, cancellationToken));
            }

            AcknowledgmentMessage?[] results = await Task.WhenAll(tasks);
            int successCount = results.Count(r => r?.Success == true);

            _logger.LogDebug("Replicated session {SessionId} to {Count}/{Total} nodes",
                session.SessionId, successCount, targets.Count);
        }

        private async Task RemoveReplicatedSessionAsync(string sessionId, CancellationToken cancellationToken)
        {
            if (_networkClient == null || _nodes.Count <= 1)
            {
                return;
            }

            ClusterMessage message = new()
            {
                Type = MessageType.SessionRemove,
                SourceNodeId = NodeId,
                Payload = new { SessionId = sessionId },
                RequiresAck = false
            };

            await _networkClient.BroadcastMessageAsync(message, cancellationToken);

            _logger.LogDebug("Removed replicated session {SessionId} from cluster", sessionId);
        }

        private async Task<bool> ReplicateToNodeAsync(ClusterNode node, string key, byte[] data, ReplicationOptions options, CancellationToken cancellationToken)
        {
            if (_networkClient == null)
            {
                return false;
            }

            StateReplicationPayload payload = new()
            {
                Key = key,
                Data = data,
                Timestamp = DateTime.UtcNow,
                Ttl = options.Ttl,
                ConsistencyLevel = options.ConsistencyLevel,
                Version = options.Version ?? 1
            };

            ClusterMessage message = new()
            {
                Type = MessageType.StateReplicate,
                SourceNodeId = NodeId,
                TargetNodeId = node.Id,
                Payload = payload,
                RequiresAck = true,
                Ttl = options.Timeout ?? TimeSpan.FromSeconds(5)
            };

            try
            {
                AcknowledgmentMessage? ack = await _networkClient.SendMessageAsync(node.Endpoint, message, cancellationToken);
                return ack?.Success ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to replicate state to node {NodeId}", node.Id);
                return false;
            }
        }

        private async Task<bool> MigrateSessionToNodeAsync(SessionInfo session, ClusterNode targetNode, CancellationToken cancellationToken)
        {
            if (_networkClient == null)
            {
                return false;
            }

            ClusterMessage message = new()
            {
                Type = MessageType.SessionMigrate,
                SourceNodeId = NodeId,
                TargetNodeId = targetNode.Id,
                Payload = new SessionReplicationPayload
                {
                    SessionId = session.SessionId,
                    NodeId = targetNode.Id, // New owner node
                    ClientIp = session.ClientIp.ToString(),
                    ClientPort = session.ClientPort,
                    StartTime = session.StartTime,
                    Metadata = session.Metadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                    BytesTransferred = session.BytesTransferred,
                    MessageCount = session.MessageCount
                },
                RequiresAck = true,
                Ttl = TimeSpan.FromSeconds(10)
            };

            try
            {
                AcknowledgmentMessage? ack = await _networkClient.SendMessageAsync(targetNode.Endpoint, message, cancellationToken);

                if (ack?.Success == true)
                {
                    // Remove session from local storage
                    _sessions.TryRemove(session.SessionId, out _);

                    _logger.LogInformation("Migrated session {SessionId} to node {TargetNode}",
                        session.SessionId, targetNode.Id);

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to migrate session {SessionId} to node {TargetNode}",
                    session.SessionId, targetNode.Id);
                return false;
            }
        }

        private async Task SendConfigurationToNodeAsync<T>(ClusterNode node, T config, CancellationToken cancellationToken) where T : class
        {
            if (_networkClient == null)
            {
                return;
            }

            ConfigurationPayload payload = new()
            {
                ConfigurationType = typeof(T).Name,
                Configuration = config,
                EffectiveTime = DateTime.UtcNow,
                RequiresRestart = false
            };

            ClusterMessage message = new()
            {
                Type = MessageType.ConfigurationUpdate,
                SourceNodeId = NodeId,
                TargetNodeId = node.Id,
                Payload = payload,
                RequiresAck = true
            };

            try
            {
                AcknowledgmentMessage? ack = await _networkClient.SendMessageAsync(node.Endpoint, message, cancellationToken);

                if (ack?.Success == true)
                {
                    _logger.LogDebug("Sent configuration {Type} to node {NodeId}", typeof(T).Name, node.Id);
                }
                else
                {
                    _logger.LogWarning("Failed to send configuration {Type} to node {NodeId}", typeof(T).Name, node.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending configuration to node {NodeId}", node.Id);
            }
        }

        private async Task MigrateLocalSessionsAsync(CancellationToken cancellationToken)
        {
            List<SessionInfo> localSessions = _sessions.Values.Where(s => s.NodeId == NodeId).ToList();

            if (localSessions.Count == 0)
            {
                _logger.LogDebug("No local sessions to migrate");
                return;
            }

            List<ClusterNode> targetNodes = SelectMigrationTargets(localSessions.Count);

            if (targetNodes.Count == 0)
            {
                _logger.LogWarning("No available nodes for session migration");
                return;
            }

            _logger.LogInformation("Migrating {Count} local sessions", localSessions.Count);

            int migratedCount = 0;
            List<Task<bool>> migrationTasks = [];

            for (int i = 0; i < localSessions.Count; i++)
            {
                SessionInfo session = localSessions[i];
                ClusterNode targetNode = targetNodes[i % targetNodes.Count];
                migrationTasks.Add(MigrateSessionToNodeAsync(session, targetNode, cancellationToken));
            }

            bool[] results = await Task.WhenAll(migrationTasks);
            migratedCount = results.Count(r => r);

            _logger.LogInformation("Successfully migrated {Migrated}/{Total} sessions",
                migratedCount, localSessions.Count);
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

            if (_nodes.IsEmpty)
            {
                State = ClusterState.Forming;
            }
            else if (_nodes.Count == 1)
            {
                // Single node cluster is still forming until more nodes join
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
            if (_nodes.IsEmpty)
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
            return _sessions.Values.Sum(s => s.MessageCount);
        }

        private void OnNetworkMessageReceived(object? sender, MessageReceivedEventArgs e)
        {
            try
            {
                switch (e.Message.Type)
                {
                    case MessageType.Heartbeat:
                        HandleHeartbeatMessage(e.Message, e.RemoteEndPoint);
                        break;

                    case MessageType.Join:
                        HandleJoinMessage(e.Message, e.RemoteEndPoint);
                        break;

                    case MessageType.Leave:
                        HandleLeaveMessage(e.Message);
                        break;

                    case MessageType.SessionReplicate:
                        e.Response = HandleSessionReplicationMessage(e.Message);
                        break;

                    case MessageType.SessionRemove:
                        HandleSessionRemoveMessage(e.Message);
                        break;

                    case MessageType.SessionMigrate:
                        e.Response = HandleSessionMigrationMessage(e.Message);
                        break;

                    case MessageType.StateReplicate:
                        e.Response = HandleStateReplicationMessage(e.Message);
                        break;

                    case MessageType.ConfigurationUpdate:
                        e.Response = HandleConfigurationUpdateMessage(e.Message);
                        break;

                    case MessageType.HealthCheck:
                        e.Response = HandleHealthCheckMessage(e.Message);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling message of type {Type} from {Source}",
                    e.Message.Type, e.Message.SourceNodeId);
            }
        }

        private void HandleHeartbeatMessage(ClusterMessage message, IPEndPoint remoteEndPoint)
        {
            if (message.Payload is HeartbeatPayload payload && _nodes.TryGetValue(message.SourceNodeId, out ClusterNode? node))
            {
                node.LastHeartbeat = DateTime.UtcNow;
                node.State = payload.State;
                node.CpuUsage = payload.CpuUsage;
                node.MemoryUsage = payload.MemoryUsage;
                node.ActiveSessions = payload.ActiveSessions;
                node.NetworkBandwidth = payload.NetworkBandwidth;

                _logger.LogTrace("Received heartbeat from {NodeId}", message.SourceNodeId);
            }
            else if (!_nodes.ContainsKey(message.SourceNodeId))
            {
                // New node discovered
                ClusterNode newNode = new()
                {
                    Id = message.SourceNodeId,
                    Endpoint = remoteEndPoint,
                    State = NodeState.Active,
                    LastHeartbeat = DateTime.UtcNow,
                    JoinTime = DateTime.UtcNow
                };

                if (_nodes.TryAdd(message.SourceNodeId, newNode))
                {
                    _logger.LogInformation("Discovered new node {NodeId}", message.SourceNodeId);
                    OnNodeJoined(new NodeEventArgs
                    {
                        NodeId = message.SourceNodeId,
                        State = NodeState.Active,
                        SourceNodeId = NodeId
                    });
                }
            }
        }

        private void HandleJoinMessage(ClusterMessage message, IPEndPoint remoteEndPoint)
        {
            // Handle node join request
            ClusterNode newNode = new()
            {
                Id = message.SourceNodeId,
                Endpoint = remoteEndPoint,
                State = NodeState.Active,
                LastHeartbeat = DateTime.UtcNow,
                JoinTime = DateTime.UtcNow
            };

            if (_nodes.TryAdd(message.SourceNodeId, newNode))
            {
                _logger.LogInformation("Node {NodeId} joined the cluster", message.SourceNodeId);
                OnNodeJoined(new NodeEventArgs
                {
                    NodeId = message.SourceNodeId,
                    State = NodeState.Active,
                    SourceNodeId = NodeId
                });
            }
        }

        private void HandleLeaveMessage(ClusterMessage message)
        {
            if (_nodes.TryRemove(message.SourceNodeId, out _))
            {
                _logger.LogInformation("Node {NodeId} left the cluster", message.SourceNodeId);
                OnNodeLeft(new NodeEventArgs
                {
                    NodeId = message.SourceNodeId,
                    State = NodeState.Leaving,
                    Reason = "Graceful leave",
                    SourceNodeId = message.SourceNodeId
                });
            }
        }

        private AcknowledgmentMessage HandleSessionReplicationMessage(ClusterMessage message)
        {
            try
            {
                if (message.Payload is SessionReplicationPayload payload)
                {
                    SessionInfo sessionInfo = new()
                    {
                        SessionId = payload.SessionId,
                        NodeId = payload.NodeId,
                        ClientIp = IPAddress.Parse(payload.ClientIp),
                        ClientPort = payload.ClientPort,
                        StartTime = payload.StartTime,
                        BytesTransferred = payload.BytesTransferred,
                        MessageCount = payload.MessageCount
                    };

                    _sessions.TryAdd(payload.SessionId, sessionInfo);

                    return new AcknowledgmentMessage
                    {
                        OriginalMessageId = message.MessageId,
                        Success = true
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle session replication");
            }

            return new AcknowledgmentMessage
            {
                OriginalMessageId = message.MessageId,
                Success = false,
                ErrorMessage = "Failed to replicate session"
            };
        }

        private void HandleSessionRemoveMessage(ClusterMessage message)
        {
            if (message.Payload is { } payload)
            {
                string? sessionId = payload.GetType().GetProperty("SessionId")?.GetValue(payload)?.ToString();
                if (sessionId != null && _sessions.TryRemove(sessionId, out _))
                {
                    _logger.LogDebug("Removed replicated session {SessionId}", sessionId);
                }
            }
        }

        private AcknowledgmentMessage HandleSessionMigrationMessage(ClusterMessage message)
        {
            try
            {
                if (message.Payload is SessionReplicationPayload payload)
                {
                    SessionInfo sessionInfo = new()
                    {
                        SessionId = payload.SessionId,
                        NodeId = NodeId, // This node is the new owner
                        ClientIp = IPAddress.Parse(payload.ClientIp),
                        ClientPort = payload.ClientPort,
                        StartTime = payload.StartTime,
                        BytesTransferred = payload.BytesTransferred,
                        MessageCount = payload.MessageCount
                    };

                    _sessions[payload.SessionId] = sessionInfo;

                    _logger.LogInformation("Accepted migrated session {SessionId}", payload.SessionId);

                    return new AcknowledgmentMessage
                    {
                        OriginalMessageId = message.MessageId,
                        Success = true
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle session migration");
            }

            return new AcknowledgmentMessage
            {
                OriginalMessageId = message.MessageId,
                Success = false,
                ErrorMessage = "Failed to accept session migration"
            };
        }

        private AcknowledgmentMessage HandleStateReplicationMessage(ClusterMessage message)
        {
            try
            {
                if (message.Payload is StateReplicationPayload payload)
                {
                    _replicatedState[payload.Key] = payload.Data;

                    return new AcknowledgmentMessage
                    {
                        OriginalMessageId = message.MessageId,
                        Success = true
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle state replication");
            }

            return new AcknowledgmentMessage
            {
                OriginalMessageId = message.MessageId,
                Success = false,
                ErrorMessage = "Failed to replicate state"
            };
        }

        private AcknowledgmentMessage HandleConfigurationUpdateMessage(ClusterMessage message)
        {
            try
            {
                if (message.Payload is ConfigurationPayload payload)
                {
                    _logger.LogInformation("Received configuration update: {Type}", payload.ConfigurationType);

                    OnConfigurationUpdated(new ConfigurationEventArgs
                    {
                        ConfigurationType = payload.ConfigurationType,
                        NewValue = payload.Configuration,
                        Success = true,
                        UpdatedNodes = new[] { NodeId },
                        SourceNodeId = message.SourceNodeId
                    });

                    return new AcknowledgmentMessage
                    {
                        OriginalMessageId = message.MessageId,
                        Success = true
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle configuration update");
            }

            return new AcknowledgmentMessage
            {
                OriginalMessageId = message.MessageId,
                Success = false,
                ErrorMessage = "Failed to apply configuration"
            };
        }

        private AcknowledgmentMessage HandleHealthCheckMessage(ClusterMessage message)
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

            return new AcknowledgmentMessage
            {
                OriginalMessageId = message.MessageId,
                Success = true,
                Result = health
            };
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