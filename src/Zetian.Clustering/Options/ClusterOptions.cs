using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Zetian.Clustering.Abstractions;
using Zetian.Clustering.Enums;

namespace Zetian.Clustering.Options
{
    /// <summary>
    /// Configuration options for cluster setup
    /// </summary>
    public class ClusterOptions
    {
        /// <summary>
        /// Unique identifier for this node
        /// </summary>
        public string NodeId { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>
        /// Port for cluster communication
        /// </summary>
        public int ClusterPort { get; set; } = 7946;

        /// <summary>
        /// IP address to bind for cluster communication
        /// </summary>
        public string BindAddress { get; set; } = "0.0.0.0";

        /// <summary>
        /// Advertise address for other nodes to connect
        /// </summary>
        public string? AdvertiseAddress { get; set; }

        /// <summary>
        /// Discovery method for finding cluster nodes
        /// </summary>
        public DiscoveryMethod DiscoveryMethod { get; set; } = DiscoveryMethod.Static;

        /// <summary>
        /// DNS name for DNS-based discovery
        /// </summary>
        public string? DiscoveryDns { get; set; }

        /// <summary>
        /// Multicast address for multicast discovery
        /// </summary>
        public string MulticastAddress { get; set; } = "239.255.1.1";

        /// <summary>
        /// Initial seed nodes for bootstrapping
        /// </summary>
        public IList<string> Seeds { get; set; } = [];

        /// <summary>
        /// Enable encryption between nodes
        /// </summary>
        public bool EnableEncryption { get; set; } = true;

        /// <summary>
        /// Shared secret for node authentication
        /// </summary>
        public string? SharedSecret { get; set; }

        /// <summary>
        /// TLS certificate for secure communication
        /// </summary>
        public X509Certificate2? TlsCertificate { get; set; }

        /// <summary>
        /// Timeout for joining the cluster
        /// </summary>
        public TimeSpan JoinTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Interval for state synchronization
        /// </summary>
        public TimeSpan SyncInterval { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Timeout for failure detection
        /// </summary>
        public TimeSpan FailureDetectionTimeout { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Replication factor for state
        /// </summary>
        public int ReplicationFactor { get; set; } = 3;

        /// <summary>
        /// Minimum replicas required for write operations
        /// </summary>
        public int MinReplicasForWrite { get; set; } = 2;

        /// <summary>
        /// Maximum concurrent sync operations
        /// </summary>
        public int MaxConcurrentSyncs { get; set; } = 10;

        /// <summary>
        /// Batch size for sync operations
        /// </summary>
        public int BatchSize { get; set; } = 100;

        /// <summary>
        /// Enable compression for network communication
        /// </summary>
        public bool CompressionEnabled { get; set; } = true;

        /// <summary>
        /// State store implementation
        /// </summary>
        public IStateStore? StateStore { get; set; }

        /// <summary>
        /// Enable persistence of cluster state
        /// </summary>
        public bool PersistenceEnabled { get; set; } = true;

        /// <summary>
        /// Interval for taking snapshots
        /// </summary>
        public TimeSpan SnapshotInterval { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Directory for storing cluster data
        /// </summary>
        public string DataDirectory { get; set; } = "./cluster-data";

        /// <summary>
        /// Maximum number of snapshots to retain
        /// </summary>
        public int MaxSnapshots { get; set; } = 5;

        /// <summary>
        /// Enable automatic rebalancing
        /// </summary>
        public bool AutoRebalance { get; set; } = true;

        /// <summary>
        /// Threshold for triggering rebalancing
        /// </summary>
        public double RebalanceThreshold { get; set; } = 0.2;

        /// <summary>
        /// Leader election configuration
        /// </summary>
        public LeaderElectionOptions LeaderElection { get; set; } = new();

        /// <summary>
        /// Load balancing configuration
        /// </summary>
        public LoadBalancingOptions LoadBalancing { get; set; } = new();

        /// <summary>
        /// Health check configuration
        /// </summary>
        public HealthCheckOptions HealthCheck { get; set; } = new();

        /// <summary>
        /// Additional properties for extensibility
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = [];

        /// <summary>
        /// Validates the configuration
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(NodeId))
            {
                throw new ArgumentException("NodeId cannot be empty");
            }

            if (ClusterPort is <= 0 or > 65535)
            {
                throw new ArgumentException("ClusterPort must be between 1 and 65535");
            }

            if (ReplicationFactor < 1)
            {
                throw new ArgumentException("ReplicationFactor must be at least 1");
            }

            if (MinReplicasForWrite < 1 || MinReplicasForWrite > ReplicationFactor)
            {
                throw new ArgumentException("MinReplicasForWrite must be between 1 and ReplicationFactor");
            }

            if (EnableEncryption && string.IsNullOrWhiteSpace(SharedSecret) && TlsCertificate == null)
            {
                throw new ArgumentException("SharedSecret or TlsCertificate is required when encryption is enabled");
            }

            LeaderElection?.Validate();
            LoadBalancing?.Validate();
            HealthCheck?.Validate();
        }
    }

    /// <summary>
    /// Leader election configuration
    /// </summary>
    public class LeaderElectionOptions
    {
        /// <summary>
        /// Enable leader election
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Election timeout
        /// </summary>
        public TimeSpan ElectionTimeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Heartbeat interval
        /// </summary>
        public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Minimum nodes required for quorum
        /// </summary>
        public int MinNodes { get; set; } = 1;

        /// <summary>
        /// Maximum election retries
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Validates the configuration
        /// </summary>
        public void Validate()
        {
            if (ElectionTimeout <= TimeSpan.Zero)
            {
                throw new ArgumentException("ElectionTimeout must be positive");
            }

            if (HeartbeatInterval <= TimeSpan.Zero || HeartbeatInterval >= ElectionTimeout)
            {
                throw new ArgumentException("HeartbeatInterval must be positive and less than ElectionTimeout");
            }

            if (MinNodes < 1)
            {
                throw new ArgumentException("MinNodes must be at least 1");
            }
        }
    }

    /// <summary>
    /// Load balancing configuration
    /// </summary>
    public class LoadBalancingOptions
    {
        /// <summary>
        /// Load balancing strategy
        /// </summary>
        public LoadBalancingStrategy Strategy { get; set; } = LoadBalancingStrategy.RoundRobin;

        /// <summary>
        /// Session affinity configuration
        /// </summary>
        public AffinityOptions Affinity { get; set; } = new();

        /// <summary>
        /// Node weights for weighted strategies
        /// </summary>
        public Dictionary<string, int> NodeWeights { get; set; } = [];

        /// <summary>
        /// Health-based routing
        /// </summary>
        public bool HealthBasedRouting { get; set; } = true;

        /// <summary>
        /// Maximum load per node (percentage)
        /// </summary>
        public double MaxLoadPerNode { get; set; } = 0.8;

        /// <summary>
        /// Validates the configuration
        /// </summary>
        public void Validate()
        {
            if (MaxLoadPerNode is <= 0 or > 1)
            {
                throw new ArgumentException("MaxLoadPerNode must be between 0 and 1");
            }

            Affinity?.Validate();
        }
    }

    /// <summary>
    /// Session affinity configuration
    /// </summary>
    public class AffinityOptions
    {
        /// <summary>
        /// Enable session affinity
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Affinity method
        /// </summary>
        public AffinityMethod Method { get; set; } = AffinityMethod.SourceIp;

        /// <summary>
        /// Session timeout
        /// </summary>
        public TimeSpan SessionTimeout { get; set; } = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Failover mode when target node is unavailable
        /// </summary>
        public FailoverMode FailoverMode { get; set; } = FailoverMode.Automatic;

        /// <summary>
        /// Validates the configuration
        /// </summary>
        public void Validate()
        {
            if (SessionTimeout <= TimeSpan.Zero)
            {
                throw new ArgumentException("SessionTimeout must be positive");
            }
        }
    }

    /// <summary>
    /// Health check configuration
    /// </summary>
    public class HealthCheckOptions
    {
        /// <summary>
        /// Enable health checks
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Check interval
        /// </summary>
        public TimeSpan CheckInterval { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Number of failures before marking unhealthy
        /// </summary>
        public int FailureThreshold { get; set; } = 3;

        /// <summary>
        /// Number of successes before marking healthy
        /// </summary>
        public int SuccessThreshold { get; set; } = 2;

        /// <summary>
        /// Timeout for health check operations
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Validates the configuration
        /// </summary>
        public void Validate()
        {
            if (CheckInterval <= TimeSpan.Zero)
            {
                throw new ArgumentException("CheckInterval must be positive");
            }

            if (Timeout <= TimeSpan.Zero || Timeout >= CheckInterval)
            {
                throw new ArgumentException("Timeout must be positive and less than CheckInterval");
            }

            if (FailureThreshold < 1)
            {
                throw new ArgumentException("FailureThreshold must be at least 1");
            }

            if (SuccessThreshold < 1)
            {
                throw new ArgumentException("SuccessThreshold must be at least 1");
            }
        }
    }
}