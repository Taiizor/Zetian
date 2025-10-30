# Zetian.Clustering - Distributed SMTP Server Clustering

[![NuGet-Version](https://img.shields.io/nuget/v/Zetian.Clustering.svg?label=NuGet)](https://www.nuget.org/packages/Zetian.Clustering)
[![NuGet-Download](https://img.shields.io/nuget/dt/Zetian.Clustering?label=Download)](https://www.nuget.org/api/v2/package/Zetian.Clustering)
[![License](https://img.shields.io/github/license/Taiizor/Zetian.svg?label=License)](https://github.com/Taiizor/Zetian/blob/develop/LICENSE)

Enterprise-grade clustering solution for Zetian SMTP Server, enabling high availability, load balancing, and horizontal scaling across multiple nodes.

## ⚡ Features

- 🔄 **Auto-Discovery** - Automatic node discovery and cluster formation
- ⚖️ **Load Balancing** - Intelligent distribution of connections across nodes
- 🔁 **Failover** - Automatic session migration on node failure
- 📊 **Distributed State** - Shared session and rate limiting state
- 🗳️ **Leader Election** - Raft consensus for cluster coordination
- 🔐 **Secure Communication** - TLS encryption between nodes
- 📈 **Monitoring** - Real-time cluster health and metrics
- 🌐 **Multi-Region** - Support for geo-distributed deployments
- 💾 **State Persistence** - Durable cluster configuration
- 🔧 **Zero Downtime** - Rolling updates and maintenance mode

## 📦 Installation

```bash
# Install Zetian SMTP Server (required)
dotnet add package Zetian

# Install Clustering Extension
dotnet add package Zetian.Clustering
```

## 🚀 Quick Start

### Basic Cluster Setup

```csharp
using Zetian.Server;
using Zetian.Clustering;

// Create clustered SMTP server
var server = new SmtpServerBuilder()
    .Port(25)
    .ServerName("Node-1")
    .Build();

// Enable clustering
var cluster = await server.EnableClusteringAsync(options =>
{
    options.NodeId = "node-1";
    options.ClusterPort = 7946;
    options.DiscoveryMethod = DiscoveryMethod.Multicast;
});

await server.StartAsync();
```

### Multi-Node Configuration

```csharp
// Node 1
var node1 = new SmtpServerBuilder()
    .Port(25)
    .ServerName("Node-1")
    .Build();

await node1.EnableClusteringAsync(options =>
{
    options.NodeId = "node-1";
    options.ClusterPort = 7946;
    options.Seeds = new[] { "node-2:7946", "node-3:7946" };
});

// Node 2
var node2 = new SmtpServerBuilder()
    .Port(25)
    .ServerName("Node-2")
    .Build();

await node2.EnableClusteringAsync(options =>
{
    options.NodeId = "node-2";
    options.ClusterPort = 7946;
    options.Seeds = new[] { "node-1:7946", "node-3:7946" };
});
```

## 🎯 Advanced Features

### Leader Election

```csharp
// Configure leader election
cluster.ConfigureLeaderElection(options =>
{
    options.ElectionTimeout = TimeSpan.FromSeconds(5);
    options.HeartbeatInterval = TimeSpan.FromSeconds(1);
    options.MinNodes = 3; // Minimum nodes for quorum
});

// Check if current node is leader
if (cluster.IsLeader)
{
    // Perform leader-only operations
    await cluster.DistributeConfigurationAsync(config);
}

// Subscribe to leader changes
cluster.LeaderChanged += (sender, e) =>
{
    Console.WriteLine($"New leader: {e.LeaderNodeId}");
};
```

### Session Affinity

```csharp
// Configure session affinity (sticky sessions)
cluster.ConfigureAffinity(options =>
{
    options.Method = AffinityMethod.SourceIp;
    options.FailoverMode = FailoverMode.Automatic;
    options.SessionTimeout = TimeSpan.FromMinutes(30);
});

// Custom affinity resolver
cluster.SetAffinityResolver((session) =>
{
    // Custom logic to determine target node
    return session.RemoteEndPoint.GetHashCode() % cluster.NodeCount;
});
```

### Distributed Rate Limiting

```csharp
// Enable distributed rate limiting
cluster.EnableDistributedRateLimiting(options =>
{
    options.SyncInterval = TimeSpan.FromSeconds(1);
    options.Algorithm = RateLimitAlgorithm.TokenBucket;
    options.GlobalLimit = 10000; // Cluster-wide limit
});

// Check rate limit across cluster
bool allowed = await cluster.CheckRateLimitAsync(
    clientIp,
    requestsPerHour: 100
);
```

### State Replication

```csharp
// Configure state replication
cluster.ConfigureReplication(options =>
{
    options.ReplicationFactor = 3;
    options.ConsistencyLevel = ConsistencyLevel.Quorum;
    options.SyncMode = SyncMode.Asynchronous;
});

// Replicate custom data
await cluster.ReplicateStateAsync("key", data, options =>
{
    options.Ttl = TimeSpan.FromMinutes(5);
    options.Priority = ReplicationPriority.High;
});
```

### Health Monitoring

```csharp
// Configure health checks
cluster.ConfigureHealthChecks(options =>
{
    options.CheckInterval = TimeSpan.FromSeconds(10);
    options.FailureThreshold = 3;
    options.SuccessThreshold = 2;
});

// Get cluster health
var health = await cluster.GetHealthAsync();
Console.WriteLine($"Healthy Nodes: {health.HealthyNodes}/{health.TotalNodes}");
Console.WriteLine($"Cluster Status: {health.Status}");

// Monitor individual nodes
foreach (var node in cluster.Nodes)
{
    Console.WriteLine($"{node.Id}: {node.Status} - Load: {node.CurrentLoad}");
}
```

### Load Balancing Strategies

```csharp
// Round-robin (default)
cluster.SetLoadBalancingStrategy(LoadBalancingStrategy.RoundRobin);

// Least connections
cluster.SetLoadBalancingStrategy(LoadBalancingStrategy.LeastConnections);

// Weighted round-robin
cluster.SetLoadBalancingStrategy(LoadBalancingStrategy.WeightedRoundRobin, options =>
{
    options.Weights = new Dictionary<string, int>
    {
        { "node-1", 3 },  // 3x capacity
        { "node-2", 2 },  // 2x capacity
        { "node-3", 1 }   // 1x capacity
    };
});

// Custom strategy
cluster.SetCustomLoadBalancer(new CustomLoadBalancer());
```

### Cluster Events

```csharp
// Node events
cluster.NodeJoined += (sender, e) =>
{
    Console.WriteLine($"Node joined: {e.NodeId} from {e.Address}");
};

cluster.NodeLeft += (sender, e) =>
{
    Console.WriteLine($"Node left: {e.NodeId} - Reason: {e.Reason}");
};

cluster.NodeFailed += async (sender, e) =>
{
    Console.WriteLine($"Node failed: {e.NodeId}");
    // Migrate sessions from failed node
    await cluster.MigrateSessionsAsync(e.NodeId);
};

// Cluster state events
cluster.StateChanged += (sender, e) =>
{
    Console.WriteLine($"Cluster state: {e.OldState} -> {e.NewState}");
};

// Rebalancing events
cluster.RebalancingStarted += (sender, e) =>
{
    Console.WriteLine($"Rebalancing started: {e.Reason}");
};

cluster.RebalancingCompleted += (sender, e) =>
{
    Console.WriteLine($"Rebalancing completed: {e.SessionsMigrated} sessions moved");
};
```

### Maintenance Mode

```csharp
// Put node in maintenance mode
await cluster.EnterMaintenanceModeAsync(options =>
{
    options.DrainTimeout = TimeSpan.FromMinutes(5);
    options.GracefulShutdown = true;
    options.MigrateSessions = true;
});

// Check maintenance status
if (cluster.IsInMaintenance)
{
    Console.WriteLine("Node is in maintenance mode");
}

// Exit maintenance mode
await cluster.ExitMaintenanceModeAsync();
```

### Cross-Region Deployment

```csharp
// Configure for multi-region
cluster.ConfigureRegions(options =>
{
    options.CurrentRegion = "us-east";
    options.Regions = new[]
    {
        new RegionConfig
        {
            Name = "us-east",
            Endpoints = new[] { "node1.us-east:7946", "node2.us-east:7946" }
        },
        new RegionConfig
        {
            Name = "eu-west",
            Endpoints = new[] { "node1.eu-west:7946", "node2.eu-west:7946" }
        }
    };
    options.PreferLocalRegion = true;
    options.CrossRegionTimeout = TimeSpan.FromSeconds(10);
});
```

## 🛠️ Configuration

### Complete Configuration Example

```csharp
var cluster = await server.EnableClusteringAsync(options =>
{
    // Basic settings
    options.NodeId = Environment.GetEnvironmentVariable("NODE_ID") ?? "node-1";
    options.ClusterPort = 7946;
    options.BindAddress = "0.0.0.0";
    
    // Discovery
    options.DiscoveryMethod = DiscoveryMethod.Dns;
    options.DiscoveryDns = "smtp-cluster.local";
    options.Seeds = new[] { "seed1:7946", "seed2:7946" };
    
    // Security
    options.EnableEncryption = true;
    options.SharedSecret = Environment.GetEnvironmentVariable("CLUSTER_SECRET");
    options.TlsCertificate = LoadCertificate();
    
    // Timeouts
    options.JoinTimeout = TimeSpan.FromSeconds(30);
    options.SyncInterval = TimeSpan.FromSeconds(5);
    options.FailureDetectionTimeout = TimeSpan.FromSeconds(10);
    
    // Replication
    options.ReplicationFactor = 3;
    options.MinReplicasForWrite = 2;
    
    // Performance
    options.MaxConcurrentSyncs = 10;
    options.BatchSize = 100;
    options.CompressionEnabled = true;
    
    // Storage
    options.StateStore = new RedisStateStore(redisConnection);
    options.PersistenceEnabled = true;
    options.SnapshotInterval = TimeSpan.FromMinutes(5);
});
```

## 📊 Monitoring & Metrics

```csharp
// Get cluster metrics
var metrics = cluster.GetMetrics();
Console.WriteLine($"Total Sessions: {metrics.TotalSessions}");
Console.WriteLine($"Messages/sec: {metrics.MessagesPerSecond}");
Console.WriteLine($"Cluster Load: {metrics.AverageLoad}%");
Console.WriteLine($"Network I/O: {metrics.NetworkBandwidth}");

// Export metrics
cluster.EnableMetricsExport(options =>
{
    options.Exporters = new[]
    {
        new PrometheusExporter(9090),
        new DatadogExporter(apiKey),
        new CloudWatchExporter(region)
    };
    options.ExportInterval = TimeSpan.FromSeconds(30);
});
```

## 🔒 Security Considerations

1. **Encryption**: Always enable TLS between nodes in production
2. **Authentication**: Use shared secrets or certificates for node authentication
3. **Network Isolation**: Deploy cluster nodes in a private network
4. **Firewall Rules**: Restrict cluster ports to member nodes only
5. **Secrets Management**: Use secure key management for cluster secrets

## 📈 Performance Tips

- Use SSD storage for state persistence
- Enable compression for cross-region deployments
- Tune batch sizes based on network latency
- Use local caching for read-heavy workloads
- Configure appropriate replication factors
- Monitor and adjust failure detection timeouts

## 🚨 Troubleshooting

### Common Issues

1. **Split Brain**: Ensure odd number of nodes for proper quorum
2. **Network Partitions**: Configure proper failure detection timeouts
3. **High Latency**: Check network connectivity between nodes
4. **State Inconsistency**: Verify replication factor and consistency levels
5. **Memory Issues**: Tune state cache sizes and eviction policies

### Debug Logging

```csharp
// Enable debug logging
cluster.EnableDebugLogging(options =>
{
    options.LogLevel = LogLevel.Debug;
    options.IncludeHeartbeats = true;
    options.IncludeStateSync = true;
    options.LogToFile = "/var/log/zetian-cluster.log";
});
```

## 📋 Requirements

- Windows, Linux, or macOS
- .NET 6.0, 7.0, 8.0, 9.0, or 10.0
- Zetian SMTP Server package
- Network connectivity between cluster nodes
- Sufficient bandwidth for state replication

## 📚 Documentation & Support

- **Issues**: [GitHub Issues](https://github.com/Taiizor/Zetian/issues)
- **Examples**: [GitHub Examples](https://github.com/Taiizor/Zetian/tree/develop/examples)
- **Discussions**: [GitHub Discussions](https://github.com/Taiizor/Zetian/discussions)
- **Documentation**: [Zetian Documentation](https://zetian.soferity.com)

## 📄 License

MIT License - see [LICENSE](https://github.com/Taiizor/Zetian/blob/develop/LICENSE)

---

**Built with ❤️ for high-availability SMTP deployments**.
