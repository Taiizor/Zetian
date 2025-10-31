'use client';

import { 
  Network, Shield, Activity, Zap, Settings,
  Users, Globe, Database, RefreshCw, AlertTriangle,
  Cpu, HardDrive, GitBranch, Cloud
} from 'lucide-react';
import CodeBlock from '@/components/CodeBlock';
import Link from 'next/link';

const quickStartExample = `using Zetian.Server;
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

await server.StartAsync();`;

const multiNodeExample = `// Node 1 - Primary seed node
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

// Node 2 - Secondary node
var node2 = new SmtpServerBuilder()
    .Port(25)
    .ServerName("Node-2")
    .Build();

await node2.EnableClusteringAsync(options =>
{
    options.NodeId = "node-2";
    options.ClusterPort = 7946;
    options.Seeds = new[] { "node-1:7946", "node-3:7946" };
});`;

const leaderElectionExample = `// Configure leader election with Raft consensus
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
    Console.WriteLine($"New leader: {e.NewLeaderNodeId}");
};`;

const loadBalancingExample = `// Round-robin (default)
cluster.SetLoadBalancingStrategy(LoadBalancingStrategy.RoundRobin);

// Least connections
cluster.SetLoadBalancingStrategy(LoadBalancingStrategy.LeastConnections);

// Weighted round-robin
cluster.SetLoadBalancingStrategy(LoadBalancingStrategy.WeightedRoundRobin, 
    new LoadBalancingOptions
    {
        NodeWeights = new Dictionary<string, int>
        {
            { "node-1", 3 },  // Gets 3x traffic
            { "node-2", 1 },  // Gets 1x traffic
            { "node-3", 2 }   // Gets 2x traffic
        }
    });

// IP Hash (sticky sessions)
cluster.SetLoadBalancingStrategy(LoadBalancingStrategy.IpHash);`;

const sessionAffinityExample = `// Configure session affinity (sticky sessions)
cluster.ConfigureAffinity(options =>
{
    options.Method = AffinityMethod.SourceIp;
    options.FailoverMode = FailoverMode.Automatic;
    options.SessionTimeout = TimeSpan.FromMinutes(30);
});

// Custom affinity resolver
cluster.SetAffinityResolver((session) =>
{
    // Route based on sender domain
    var domain = session.From?.Host;
    if (domain != null)
    {
        var hash = domain.GetHashCode();
        var nodeIndex = Math.Abs(hash) % cluster.NodeCount;
        return cluster.Nodes.ElementAt(nodeIndex).Id;
    }
    
    // Fallback to IP-based routing
    return cluster.Nodes.ElementAt(
        Math.Abs(session.ClientIp.GetHashCode()) % cluster.NodeCount
    ).Id;
});`;

const stateReplicationExample = `// Configure state replication
cluster.ConfigureReplication(options =>
{
    options.ReplicationFactor = 3;  // Replicate to 3 nodes
    options.ConsistencyLevel = ConsistencyLevel.Quorum;
    options.SyncMode = SyncMode.Asynchronous;
});

// Replicate session data
await cluster.ReplicateStateAsync("session:" + sessionId, sessionData, 
    new ReplicationOptions
    {
        Ttl = TimeSpan.FromHours(1),
        Priority = ReplicationPriority.High,
        WaitForAck = true
    });

// Retrieve replicated data
var data = await cluster.GetReplicatedStateAsync<SessionData>(
    "session:" + sessionId,
    ConsistencyLevel.One  // Fast read from any replica
);`;

const distributedRateLimitExample = `// Enable distributed rate limiting
cluster.EnableDistributedRateLimiting(options =>
{
    options.SyncInterval = TimeSpan.FromSeconds(1);
    options.Algorithm = RateLimitAlgorithm.TokenBucket;
    options.GlobalLimit = 10000; // Cluster-wide limit per hour
});

// Check rate limit across entire cluster
bool allowed = await cluster.CheckRateLimitAsync(
    key: clientIp.ToString(),
    requestsPerHour: 100
);

if (!allowed)
{
    // Rate limit exceeded across cluster
    return SmtpResponse.ServiceNotAvailable;
}`;

const healthMonitoringExample = `// Configure health checks
cluster.ConfigureHealthChecks(options =>
{
    options.CheckInterval = TimeSpan.FromSeconds(10);
    options.FailureThreshold = 3;  // Mark unhealthy after 3 failures
    options.SuccessThreshold = 2;  // Mark healthy after 2 successes
});

// Get cluster health
var health = await cluster.GetHealthAsync();
Console.WriteLine($"Cluster Status: {health.Status}");
Console.WriteLine($"Healthy Nodes: {health.HealthyNodes}/{health.TotalNodes}");

// Monitor individual nodes
foreach (var node in cluster.Nodes)
{
    Console.WriteLine($"{node.Id}: {node.State} - Load: {node.CurrentLoad}");
}`;

const maintenanceExample = `// Put node in maintenance mode for updates
await cluster.EnterMaintenanceModeAsync(new MaintenanceOptions
{
    DrainTimeout = TimeSpan.FromMinutes(5),
    GracefulShutdown = true,
    MigrateSessions = true,
    RejectNewConnections = true
});

// Monitor drain progress
cluster.DrainProgress += (sender, e) =>
{
    Console.WriteLine($"Draining: {e.SessionsRemaining} sessions remaining");
};

// Perform maintenance tasks
await UpdateSoftwareAsync();

// Exit maintenance mode and rejoin cluster
await cluster.ExitMaintenanceModeAsync();`;

const multiRegionExample = `// Configure for multi-region deployment
cluster.ConfigureRegions(options =>
{
    options.CurrentRegion = "us-east";
    
    options.Regions = new List<RegionConfig>
    {
        new RegionConfig
        {
            Name = "us-east",
            Priority = 1,  // Primary region
            Endpoints = new[] { "node1.us-east:7946", "node2.us-east:7946" }
        },
        new RegionConfig
        {
            Name = "eu-west",
            Priority = 2,  // Secondary region
            Endpoints = new[] { "node1.eu-west:7946", "node2.eu-west:7946" }
        }
    };
    
    options.PreferLocalRegion = true;
    options.CrossRegionTimeout = TimeSpan.FromSeconds(10);
    options.RegionFailoverEnabled = true;
});`;

export default function ClusteringPage() {
  return (
    <div className="min-h-screen py-12 bg-gray-50 dark:bg-gray-950">
      <div className="container mx-auto px-4">
        {/* Header */}
        <div className="text-center mb-12">
          <div className="inline-flex items-center justify-center p-2 bg-primary-100 dark:bg-primary-900/30 rounded-lg mb-4">
            <Network className="h-8 w-8 text-primary-600 dark:text-primary-400" />
          </div>
          <h1 className="text-4xl font-bold text-gray-900 dark:text-white mb-4">
            Clustering
          </h1>
          <p className="text-xl text-gray-600 dark:text-gray-400 max-w-3xl mx-auto">
            Enterprise-grade clustering solution for high availability, load balancing, and horizontal scaling across multiple SMTP server nodes.
          </p>
        </div>

        {/* Features Grid */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 mb-12">
          <div className="bg-white dark:bg-gray-900 rounded-lg p-6 shadow-sm border border-gray-200 dark:border-gray-800">
            <GitBranch className="h-6 w-6 text-blue-600 dark:text-blue-400 mb-3" />
            <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-2">Leader Election</h3>
            <p className="text-gray-600 dark:text-gray-400">
              Raft consensus algorithm for automatic leader election and cluster coordination.
            </p>
          </div>

          <div className="bg-white dark:bg-gray-900 rounded-lg p-6 shadow-sm border border-gray-200 dark:border-gray-800">
            <RefreshCw className="h-6 w-6 text-green-600 dark:text-green-400 mb-3" />
            <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-2">Auto-Failover</h3>
            <p className="text-gray-600 dark:text-gray-400">
              Automatic session migration and failover when nodes fail or become unavailable.
            </p>
          </div>

          <div className="bg-white dark:bg-gray-900 rounded-lg p-6 shadow-sm border border-gray-200 dark:border-gray-800">
            <Activity className="h-6 w-6 text-purple-600 dark:text-purple-400 mb-3" />
            <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-2">Load Balancing</h3>
            <p className="text-gray-600 dark:text-gray-400">
              Multiple strategies including round-robin, least connections, and weighted distribution.
            </p>
          </div>

          <div className="bg-white dark:bg-gray-900 rounded-lg p-6 shadow-sm border border-gray-200 dark:border-gray-800">
            <Database className="h-6 w-6 text-orange-600 dark:text-orange-400 mb-3" />
            <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-2">State Replication</h3>
            <p className="text-gray-600 dark:text-gray-400">
              Distributed state with configurable replication factor and consistency levels.
            </p>
          </div>

          <div className="bg-white dark:bg-gray-900 rounded-lg p-6 shadow-sm border border-gray-200 dark:border-gray-800">
            <Shield className="h-6 w-6 text-red-600 dark:text-red-400 mb-3" />
            <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-2">Secure Communication</h3>
            <p className="text-gray-600 dark:text-gray-400">
              TLS encryption and authentication between cluster nodes for secure communication.
            </p>
          </div>

          <div className="bg-white dark:bg-gray-900 rounded-lg p-6 shadow-sm border border-gray-200 dark:border-gray-800">
            <Globe className="h-6 w-6 text-indigo-600 dark:text-indigo-400 mb-3" />
            <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-2">Multi-Region</h3>
            <p className="text-gray-600 dark:text-gray-400">
              Support for geo-distributed deployments with region-aware routing and failover.
            </p>
          </div>
        </div>

        {/* Installation */}
        <div className="mb-16">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-6">Installation</h2>
          <div className="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 p-6">
            <CodeBlock 
              code={`dotnet add package Zetian
dotnet add package Zetian.Clustering`}
              language="bash"
              showLineNumbers={false}
            />
          </div>
        </div>

        {/* Quick Start */}
        <div className="mb-16">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-6">Quick Start</h2>
          <div className="space-y-6">
            <div>
              <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-3">Basic Cluster Setup</h3>
              <CodeBlock code={quickStartExample} language="csharp" />
            </div>

            <div>
              <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-3">Multi-Node Configuration</h3>
              <CodeBlock code={multiNodeExample} language="csharp" />
            </div>
          </div>
        </div>

        {/* Advanced Features */}
        <div className="mb-16">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-6">Advanced Features</h2>
          
          <div className="space-y-8">
            <div>
              <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-3 flex items-center gap-2">
                <GitBranch className="h-5 w-5 text-primary-600 dark:text-primary-400" />
                Leader Election
              </h3>
              <CodeBlock code={leaderElectionExample} language="csharp" />
            </div>

            <div>
              <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-3 flex items-center gap-2">
                <Activity className="h-5 w-5 text-primary-600 dark:text-primary-400" />
                Load Balancing Strategies
              </h3>
              <CodeBlock code={loadBalancingExample} language="csharp" />
            </div>

            <div>
              <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-3 flex items-center gap-2">
                <Users className="h-5 w-5 text-primary-600 dark:text-primary-400" />
                Session Affinity
              </h3>
              <CodeBlock code={sessionAffinityExample} language="csharp" />
            </div>

            <div>
              <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-3 flex items-center gap-2">
                <Database className="h-5 w-5 text-primary-600 dark:text-primary-400" />
                State Replication
              </h3>
              <CodeBlock code={stateReplicationExample} language="csharp" />
            </div>

            <div>
              <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-3 flex items-center gap-2">
                <Zap className="h-5 w-5 text-primary-600 dark:text-primary-400" />
                Distributed Rate Limiting
              </h3>
              <CodeBlock code={distributedRateLimitExample} language="csharp" />
            </div>

            <div>
              <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-3 flex items-center gap-2">
                <Activity className="h-5 w-5 text-primary-600 dark:text-primary-400" />
                Health Monitoring
              </h3>
              <CodeBlock code={healthMonitoringExample} language="csharp" />
            </div>

            <div>
              <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-3 flex items-center gap-2">
                <Settings className="h-5 w-5 text-primary-600 dark:text-primary-400" />
                Maintenance Mode
              </h3>
              <CodeBlock code={maintenanceExample} language="csharp" />
            </div>

            <div>
              <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-3 flex items-center gap-2">
                <Globe className="h-5 w-5 text-primary-600 dark:text-primary-400" />
                Multi-Region Deployment
              </h3>
              <CodeBlock code={multiRegionExample} language="csharp" />
            </div>
          </div>
        </div>

        {/* Configuration Reference */}
        <div className="mb-16">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-6">Configuration Reference</h2>
          
          <div className="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 overflow-hidden">
            <table className="w-full">
              <thead className="bg-gray-50 dark:bg-gray-800">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Property</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Type</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Default</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Description</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
                <tr className="hover:bg-gray-50 dark:hover:bg-gray-800">
                  <td className="px-6 py-4 text-sm font-mono text-gray-900 dark:text-gray-100">NodeId</td>
                  <td className="px-6 py-4 text-sm text-gray-600 dark:text-gray-400">string</td>
                  <td className="px-6 py-4 text-sm text-gray-600 dark:text-gray-400">Required</td>
                  <td className="px-6 py-4 text-sm text-gray-600 dark:text-gray-400">Unique identifier for this node</td>
                </tr>
                <tr className="hover:bg-gray-50 dark:hover:bg-gray-800">
                  <td className="px-6 py-4 text-sm font-mono text-gray-900 dark:text-gray-100">ClusterPort</td>
                  <td className="px-6 py-4 text-sm text-gray-600 dark:text-gray-400">int</td>
                  <td className="px-6 py-4 text-sm text-gray-600 dark:text-gray-400">7946</td>
                  <td className="px-6 py-4 text-sm text-gray-600 dark:text-gray-400">Port for cluster communication</td>
                </tr>
                <tr className="hover:bg-gray-50 dark:hover:bg-gray-800">
                  <td className="px-6 py-4 text-sm font-mono text-gray-900 dark:text-gray-100">DiscoveryMethod</td>
                  <td className="px-6 py-4 text-sm text-gray-600 dark:text-gray-400">enum</td>
                  <td className="px-6 py-4 text-sm text-gray-600 dark:text-gray-400">Multicast</td>
                  <td className="px-6 py-4 text-sm text-gray-600 dark:text-gray-400">Node discovery method (Static, DNS, Multicast, Kubernetes, Consul)</td>
                </tr>
                <tr className="hover:bg-gray-50 dark:hover:bg-gray-800">
                  <td className="px-6 py-4 text-sm font-mono text-gray-900 dark:text-gray-100">Seeds</td>
                  <td className="px-6 py-4 text-sm text-gray-600 dark:text-gray-400">string[]</td>
                  <td className="px-6 py-4 text-sm text-gray-600 dark:text-gray-400">Empty</td>
                  <td className="px-6 py-4 text-sm text-gray-600 dark:text-gray-400">Seed nodes for cluster join</td>
                </tr>
                <tr className="hover:bg-gray-50 dark:hover:bg-gray-800">
                  <td className="px-6 py-4 text-sm font-mono text-gray-900 dark:text-gray-100">ReplicationFactor</td>
                  <td className="px-6 py-4 text-sm text-gray-600 dark:text-gray-400">int</td>
                  <td className="px-6 py-4 text-sm text-gray-600 dark:text-gray-400">3</td>
                  <td className="px-6 py-4 text-sm text-gray-600 dark:text-gray-400">Number of replicas for state</td>
                </tr>
                <tr className="hover:bg-gray-50 dark:hover:bg-gray-800">
                  <td className="px-6 py-4 text-sm font-mono text-gray-900 dark:text-gray-100">ConsistencyLevel</td>
                  <td className="px-6 py-4 text-sm text-gray-600 dark:text-gray-400">enum</td>
                  <td className="px-6 py-4 text-sm text-gray-600 dark:text-gray-400">Quorum</td>
                  <td className="px-6 py-4 text-sm text-gray-600 dark:text-gray-400">Read/write consistency (One, Two, Three, Quorum, All)</td>
                </tr>
                <tr className="hover:bg-gray-50 dark:hover:bg-gray-800">
                  <td className="px-6 py-4 text-sm font-mono text-gray-900 dark:text-gray-100">EnableEncryption</td>
                  <td className="px-6 py-4 text-sm text-gray-600 dark:text-gray-400">bool</td>
                  <td className="px-6 py-4 text-sm text-gray-600 dark:text-gray-400">true</td>
                  <td className="px-6 py-4 text-sm text-gray-600 dark:text-gray-400">Enable TLS encryption between nodes</td>
                </tr>
                <tr className="hover:bg-gray-50 dark:hover:bg-gray-800">
                  <td className="px-6 py-4 text-sm font-mono text-gray-900 dark:text-gray-100">HeartbeatInterval</td>
                  <td className="px-6 py-4 text-sm text-gray-600 dark:text-gray-400">TimeSpan</td>
                  <td className="px-6 py-4 text-sm text-gray-600 dark:text-gray-400">1 second</td>
                  <td className="px-6 py-4 text-sm text-gray-600 dark:text-gray-400">Interval between heartbeat messages</td>
                </tr>
                <tr className="hover:bg-gray-50 dark:hover:bg-gray-800">
                  <td className="px-6 py-4 text-sm font-mono text-gray-900 dark:text-gray-100">ElectionTimeout</td>
                  <td className="px-6 py-4 text-sm text-gray-600 dark:text-gray-400">TimeSpan</td>
                  <td className="px-6 py-4 text-sm text-gray-600 dark:text-gray-400">5 seconds</td>
                  <td className="px-6 py-4 text-sm text-gray-600 dark:text-gray-400">Timeout for leader election</td>
                </tr>
                <tr className="hover:bg-gray-50 dark:hover:bg-gray-800">
                  <td className="px-6 py-4 text-sm font-mono text-gray-900 dark:text-gray-100">FailureDetectionTimeout</td>
                  <td className="px-6 py-4 text-sm text-gray-600 dark:text-gray-400">TimeSpan</td>
                  <td className="px-6 py-4 text-sm text-gray-600 dark:text-gray-400">10 seconds</td>
                  <td className="px-6 py-4 text-sm text-gray-600 dark:text-gray-400">Time to detect node failure</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        {/* Cluster States */}
        <div className="mb-16">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-6">Cluster States</h2>
          
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div className="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 p-6">
              <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">Node States</h3>
              <div className="space-y-3">
                <div>
                  <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-800 dark:bg-gray-800 dark:text-gray-300">Initializing</span>
                  <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">Node is starting up and discovering cluster</p>
                </div>
                <div>
                  <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-yellow-100 text-yellow-800 dark:bg-yellow-900/30 dark:text-yellow-400">Joining</span>
                  <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">Node is joining the cluster</p>
                </div>
                <div>
                  <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400">Active</span>
                  <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">Node is healthy and serving traffic</p>
                </div>
                <div>
                  <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-400">Maintenance</span>
                  <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">Node is in maintenance mode</p>
                </div>
                <div>
                  <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-orange-100 text-orange-800 dark:bg-orange-900/30 dark:text-orange-400">Draining</span>
                  <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">Node is draining sessions</p>
                </div>
                <div>
                  <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-400">Failed</span>
                  <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">Node has failed and is offline</p>
                </div>
              </div>
            </div>

            <div className="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 p-6">
              <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">Cluster States</h3>
              <div className="space-y-3">
                <div>
                  <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-800 dark:bg-gray-800 dark:text-gray-300">Forming</span>
                  <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">Cluster is being formed</p>
                </div>
                <div>
                  <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400">Healthy</span>
                  <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">All nodes are healthy and synchronized</p>
                </div>
                <div>
                  <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-yellow-100 text-yellow-800 dark:bg-yellow-900/30 dark:text-yellow-400">Degraded</span>
                  <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">Some nodes are unhealthy but cluster is operational</p>
                </div>
                <div>
                  <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-orange-100 text-orange-800 dark:bg-orange-900/30 dark:text-orange-400">Rebalancing</span>
                  <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">Cluster is redistributing sessions</p>
                </div>
                <div>
                  <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-purple-100 text-purple-800 dark:bg-purple-900/30 dark:text-purple-400">Split Brain</span>
                  <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">Network partition detected</p>
                </div>
                <div>
                  <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-400">Failed</span>
                  <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">Cluster has lost quorum</p>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Performance Tips */}
        <div className="mb-16">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-6">Performance Optimization</h2>
          
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div className="bg-blue-50 dark:bg-blue-900/20 rounded-lg p-6 border border-blue-200 dark:border-blue-800">
              <h3 className="text-lg font-semibold text-blue-900 dark:text-blue-100 mb-3 flex items-center gap-2">
                <HardDrive className="h-5 w-5" />
                Storage
              </h3>
              <ul className="space-y-2 text-sm text-blue-800 dark:text-blue-200">
                <li>• Use SSD storage for state persistence</li>
                <li>• Enable compression for large state objects</li>
                <li>• Configure appropriate snapshot intervals</li>
                <li>• Use memory caching for hot data</li>
              </ul>
            </div>

            <div className="bg-green-50 dark:bg-green-900/20 rounded-lg p-6 border border-green-200 dark:border-green-800">
              <h3 className="text-lg font-semibold text-green-900 dark:text-green-100 mb-3 flex items-center gap-2">
                <Network className="h-5 w-5" />
                Network
              </h3>
              <ul className="space-y-2 text-sm text-green-800 dark:text-green-200">
                <li>• Use dedicated network for cluster traffic</li>
                <li>• Enable compression for cross-region deployments</li>
                <li>• Tune batch sizes based on latency</li>
                <li>• Configure appropriate timeouts</li>
              </ul>
            </div>

            <div className="bg-purple-50 dark:bg-purple-900/20 rounded-lg p-6 border border-purple-200 dark:border-purple-800">
              <h3 className="text-lg font-semibold text-purple-900 dark:text-purple-100 mb-3 flex items-center gap-2">
                <Cpu className="h-5 w-5" />
                Compute
              </h3>
              <ul className="space-y-2 text-sm text-purple-800 dark:text-purple-200">
                <li>• Scale horizontally for CPU-intensive workloads</li>
                <li>• Use appropriate replication factors</li>
                <li>• Enable async replication for non-critical data</li>
                <li>• Monitor and adjust thread pool sizes</li>
              </ul>
            </div>

            <div className="bg-orange-50 dark:bg-orange-900/20 rounded-lg p-6 border border-orange-200 dark:border-orange-800">
              <h3 className="text-lg font-semibold text-orange-900 dark:text-orange-100 mb-3 flex items-center gap-2">
                <Settings className="h-5 w-5" />
                Configuration
              </h3>
              <ul className="space-y-2 text-sm text-orange-800 dark:text-orange-200">
                <li>• Use odd number of nodes for quorum</li>
                <li>• Configure health check intervals appropriately</li>
                <li>• Set realistic failure detection timeouts</li>
                <li>• Use weighted load balancing for heterogeneous nodes</li>
              </ul>
            </div>
          </div>
        </div>

        {/* Security Best Practices */}
        <div className="mb-16">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-6">Security Best Practices</h2>
          
          <div className="bg-red-50 dark:bg-red-900/20 rounded-lg p-6 border border-red-200 dark:border-red-800">
            <div className="flex items-start gap-3">
              <AlertTriangle className="h-6 w-6 text-red-600 dark:text-red-400 flex-shrink-0 mt-0.5" />
              <div className="space-y-4">
                <div>
                  <h3 className="text-lg font-semibold text-red-900 dark:text-red-100 mb-2">Network Security</h3>
                  <ul className="space-y-1 text-sm text-red-800 dark:text-red-200">
                    <li>• Restrict cluster ports (7946) to member nodes only</li>
                    <li>• Deploy cluster nodes in a private network/VLAN</li>
                    <li>• Use network segmentation between clusters</li>
                    <li>• Enable firewall rules for cluster communication</li>
                  </ul>
                </div>
                
                <div>
                  <h3 className="text-lg font-semibold text-red-900 dark:text-red-100 mb-2">Encryption & Authentication</h3>
                  <ul className="space-y-1 text-sm text-red-800 dark:text-red-200">
                    <li>• Always enable TLS between nodes in production</li>
                    <li>• Use shared secrets or certificates for node authentication</li>
                    <li>• Rotate cluster secrets regularly</li>
                    <li>• Store secrets in secure key management systems</li>
                  </ul>
                </div>
                
                <div>
                  <h3 className="text-lg font-semibold text-red-900 dark:text-red-100 mb-2">Access Control</h3>
                  <ul className="space-y-1 text-sm text-red-800 dark:text-red-200">
                    <li>• Implement role-based access control (RBAC)</li>
                    <li>• Audit all cluster operations</li>
                    <li>• Monitor for unauthorized access attempts</li>
                    <li>• Use separate credentials for each node</li>
                  </ul>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Help Section */}
        <div className="mt-16 text-center">
          <div className="inline-flex items-center gap-2 px-4 py-2 bg-blue-100 dark:bg-blue-900/30 rounded-full text-sm">
            <Cloud className="h-4 w-4 text-blue-600 dark:text-blue-400" />
            <span className="text-blue-700 dark:text-blue-300">
              Need help with clustering setup? Check out our
            </span>
            <Link 
              href="/examples"
              className="text-blue-600 dark:text-blue-400 hover:underline font-medium"
            >
              examples
            </Link>
            <span className="text-blue-700 dark:text-blue-300">and</span>
            <a 
              href="https://github.com/Taiizor/Zetian/discussions"
              target="_blank"
              rel="noopener noreferrer"
              className="text-blue-600 dark:text-blue-400 hover:underline font-medium"
            >
              community discussions
            </a>
          </div>
        </div>
      </div>
    </div>
  );
}