using System;
using System.Collections.Generic;
using Zetian.Clustering.Abstractions;
using Zetian.Clustering.Enums;

namespace Zetian.Clustering.Models
{
    /// <summary>
    /// Cluster health information
    /// </summary>
    public class ClusterHealth
    {
        /// <summary>
        /// Overall cluster status
        /// </summary>
        public ClusterState Status { get; set; }

        /// <summary>
        /// Total number of nodes
        /// </summary>
        public int TotalNodes { get; set; }

        /// <summary>
        /// Number of healthy nodes
        /// </summary>
        public int HealthyNodes { get; set; }

        /// <summary>
        /// Number of unhealthy nodes
        /// </summary>
        public int UnhealthyNodes { get; set; }

        /// <summary>
        /// Whether cluster has quorum
        /// </summary>
        public bool HasQuorum { get; set; }

        /// <summary>
        /// Current leader node ID
        /// </summary>
        public string? LeaderNodeId { get; set; }

        /// <summary>
        /// Individual node health statuses
        /// </summary>
        public Dictionary<string, NodeHealthStatus> NodeStatuses { get; set; } = [];

        /// <summary>
        /// Last health check time
        /// </summary>
        public DateTime LastCheckTime { get; set; }

        /// <summary>
        /// Health check latency in milliseconds
        /// </summary>
        public double CheckLatencyMs { get; set; }
    }

    /// <summary>
    /// Node health status
    /// </summary>
    public class NodeHealthStatus
    {
        /// <summary>
        /// Node ID
        /// </summary>
        public string NodeId { get; set; } = string.Empty;

        /// <summary>
        /// Node state
        /// </summary>
        public NodeState State { get; set; }

        /// <summary>
        /// Whether node is healthy
        /// </summary>
        public bool IsHealthy { get; set; }

        /// <summary>
        /// CPU usage percentage
        /// </summary>
        public double CpuUsage { get; set; }

        /// <summary>
        /// Memory usage percentage
        /// </summary>
        public double MemoryUsage { get; set; }

        /// <summary>
        /// Active session count
        /// </summary>
        public int ActiveSessions { get; set; }

        /// <summary>
        /// Response time in milliseconds
        /// </summary>
        public double ResponseTimeMs { get; set; }

        /// <summary>
        /// Last successful health check
        /// </summary>
        public DateTime LastSuccessfulCheck { get; set; }

        /// <summary>
        /// Consecutive failure count
        /// </summary>
        public int ConsecutiveFailures { get; set; }
    }

    /// <summary>
    /// Cluster metrics
    /// </summary>
    public class ClusterMetrics
    {
        /// <summary>
        /// Total sessions across cluster
        /// </summary>
        public long TotalSessions { get; set; }

        /// <summary>
        /// Messages per second across cluster
        /// </summary>
        public double MessagesPerSecond { get; set; }

        /// <summary>
        /// Average cluster load percentage
        /// </summary>
        public double AverageLoad { get; set; }

        /// <summary>
        /// Network bandwidth usage (bytes/sec)
        /// </summary>
        public long NetworkBandwidth { get; set; }

        /// <summary>
        /// Total messages processed
        /// </summary>
        public long TotalMessagesProcessed { get; set; }

        /// <summary>
        /// Total bytes transferred
        /// </summary>
        public long TotalBytesTransferred { get; set; }

        /// <summary>
        /// Average session duration
        /// </summary>
        public TimeSpan AverageSessionDuration { get; set; }

        /// <summary>
        /// Failed session count
        /// </summary>
        public long FailedSessions { get; set; }

        /// <summary>
        /// Rebalancing operations performed
        /// </summary>
        public int RebalancingOperations { get; set; }

        /// <summary>
        /// Leader elections performed
        /// </summary>
        public int LeaderElections { get; set; }

        /// <summary>
        /// Node failures detected
        /// </summary>
        public int NodeFailures { get; set; }

        /// <summary>
        /// Uptime of the cluster
        /// </summary>
        public TimeSpan ClusterUptime { get; set; }
    }

    /// <summary>
    /// Replication options
    /// </summary>
    public class ReplicationOptions
    {
        /// <summary>
        /// Time to live for replicated data
        /// </summary>
        public TimeSpan? Ttl { get; set; }

        /// <summary>
        /// Replication priority
        /// </summary>
        public ReplicationPriority Priority { get; set; } = ReplicationPriority.Normal;

        /// <summary>
        /// Consistency level required
        /// </summary>
        public ConsistencyLevel ConsistencyLevel { get; set; } = ConsistencyLevel.Quorum;

        /// <summary>
        /// Whether to wait for replication to complete
        /// </summary>
        public bool WaitForReplication { get; set; } = true;

        /// <summary>
        /// Timeout for replication operation
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Gets or sets the version number associated with the current instance.
        /// </summary>
        public int Version { get; set; } = 1;
    }

    /// <summary>
    /// Maintenance mode options
    /// </summary>
    public class MaintenanceOptions
    {
        /// <summary>
        /// Timeout for draining connections
        /// </summary>
        public TimeSpan DrainTimeout { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Whether to perform graceful shutdown
        /// </summary>
        public bool GracefulShutdown { get; set; } = true;

        /// <summary>
        /// Whether to migrate sessions to other nodes
        /// </summary>
        public bool MigrateSessions { get; set; } = true;

        /// <summary>
        /// Target nodes for session migration
        /// </summary>
        public List<string>? TargetNodes { get; set; }

        /// <summary>
        /// Reason for maintenance
        /// </summary>
        public string? Reason { get; set; }

        /// <summary>
        /// Expected duration of maintenance
        /// </summary>
        public TimeSpan? ExpectedDuration { get; set; }
    }

    /// <summary>
    /// Region configuration
    /// </summary>
    public class RegionConfig
    {
        /// <summary>
        /// Region name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Region endpoints
        /// </summary>
        public List<string> Endpoints { get; set; } = [];

        /// <summary>
        /// Region weight for routing
        /// </summary>
        public int Weight { get; set; } = 1;

        /// <summary>
        /// Whether region is active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Current region name
        /// </summary>
        public string CurrentRegion { get; set; } = string.Empty;

        /// <summary>
        /// All configured regions
        /// </summary>
        public List<RegionConfig> Regions { get; set; } = [];

        /// <summary>
        /// Whether to prefer local region
        /// </summary>
        public bool PreferLocalRegion { get; set; } = true;

        /// <summary>
        /// Cross-region timeout
        /// </summary>
        public TimeSpan CrossRegionTimeout { get; set; } = TimeSpan.FromSeconds(10);
    }

    /// <summary>
    /// Replication configuration
    /// </summary>
    public class ReplicationConfig
    {
        /// <summary>
        /// Replication factor
        /// </summary>
        public int ReplicationFactor { get; set; } = 3;

        /// <summary>
        /// Custom replication factor (overrides default)
        /// </summary>
        public int CustomReplicationFactor { get; set; }

        /// <summary>
        /// Consistency level
        /// </summary>
        public ConsistencyLevel ConsistencyLevel { get; set; } = ConsistencyLevel.Quorum;

        /// <summary>
        /// Preferred consistency level
        /// </summary>
        public ConsistencyLevel? PreferredConsistencyLevel { get; set; }

        /// <summary>
        /// Synchronization mode
        /// </summary>
        public SyncMode SyncMode { get; set; } = SyncMode.Asynchronous;

        /// <summary>
        /// Write concern (minimum replicas for write)
        /// </summary>
        public int WriteConcern { get; set; } = 2;

        /// <summary>
        /// Read preference (primary, secondary, etc.)
        /// </summary>
        public string ReadPreference { get; set; } = "primary";

        /// <summary>
        /// Enable automatic failover
        /// </summary>
        public bool AutoFailover { get; set; } = true;

        /// <summary>
        /// Replication timeout
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Enable compression for replication
        /// </summary>
        public bool EnableCompression { get; set; } = true;

        /// <summary>
        /// Batch size for bulk replication
        /// </summary>
        public int BatchSize { get; set; } = 100;
    }

    /// <summary>
    /// Rate limit configuration
    /// </summary>
    public class RateLimitConfig
    {
        /// <summary>
        /// Synchronization interval
        /// </summary>
        public TimeSpan SyncInterval { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Rate limiting algorithm
        /// </summary>
        public RateLimitAlgorithm Algorithm { get; set; } = RateLimitAlgorithm.TokenBucket;

        /// <summary>
        /// Maximum requests per second
        /// </summary>
        public int MaxRequestsPerSecond { get; set; } = 1000;

        /// <summary>
        /// Maximum requests per IP per hour
        /// </summary>
        public int MaxRequestsPerIpPerHour { get; set; } = 3600;

        /// <summary>
        /// Enable distributed rate limiting
        /// </summary>
        public bool EnableDistributed { get; set; } = true;

        /// <summary>
        /// Window size for rate calculations
        /// </summary>
        public TimeSpan WindowSize { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Sliding window buckets
        /// </summary>
        public int BucketCount { get; set; } = 60;

        /// <summary>
        /// Whitelist IPs (no rate limiting)
        /// </summary>
        public List<string> WhitelistIps { get; set; } = [];

        /// <summary>
        /// Blacklist IPs (always blocked)
        /// </summary>
        public List<string> BlacklistIps { get; set; } = [];

        /// <summary>
        /// Global rate limit across cluster
        /// </summary>
        public int GlobalLimit { get; set; } = 10000;

        /// <summary>
        /// Per-node rate limit
        /// </summary>
        public int PerNodeLimit { get; set; } = 1000;

        /// <summary>
        /// Enable burst allowance
        /// </summary>
        public bool AllowBurst { get; set; } = true;

        /// <summary>
        /// Burst size
        /// </summary>
        public int BurstSize { get; set; } = 100;
    }

    /// <summary>
    /// Metrics export configuration
    /// </summary>
    public class MetricsExportConfig
    {
        /// <summary>
        /// Metric exporters
        /// </summary>
        public List<IMetricsExporter> Exporters { get; set; } = [];

        /// <summary>
        /// Export interval
        /// </summary>
        public TimeSpan ExportInterval { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Include detailed metrics
        /// </summary>
        public bool IncludeDetailedMetrics { get; set; } = false;

        /// <summary>
        /// Metric prefix
        /// </summary>
        public string MetricPrefix { get; set; } = "zetian_cluster_";
    }

    /// <summary>
    /// Debug logging options
    /// </summary>
    public class DebugLoggingOptions
    {
        /// <summary>
        /// Log level
        /// </summary>
        public Microsoft.Extensions.Logging.LogLevel LogLevel { get; set; } = Microsoft.Extensions.Logging.LogLevel.Debug;

        /// <summary>
        /// Include heartbeat messages
        /// </summary>
        public bool IncludeHeartbeats { get; set; } = false;

        /// <summary>
        /// Include state sync messages
        /// </summary>
        public bool IncludeStateSync { get; set; } = false;

        /// <summary>
        /// Log to file
        /// </summary>
        public string? LogToFile { get; set; }

        /// <summary>
        /// Maximum log file size
        /// </summary>
        public long MaxLogFileSize { get; set; } = 100 * 1024 * 1024; // 100MB

        /// <summary>
        /// Include performance metrics in logs
        /// </summary>
        public bool IncludePerformanceMetrics { get; set; } = false;
    }
}