using Microsoft.Extensions.Logging;
using Zetian.Abstractions;
using Zetian.Clustering.Abstractions;
using Zetian.Clustering.Enums;
using Zetian.Clustering.Extensions;
using Zetian.Clustering.Models;
using Zetian.Clustering.Options;
using Zetian.Server;

namespace Zetian.Clustering.Examples
{
    /// <summary>
    /// Example of setting up a clustered SMTP server
    /// </summary>
    public class ClusteredSmtpExample
    {
        public static async Task Main(string[] args)
        {
            // Get node configuration from environment or args
            string nodeId = Environment.GetEnvironmentVariable("NODE_ID") ?? "node-1";
            int smtpPort = int.Parse(Environment.GetEnvironmentVariable("SMTP_PORT") ?? "25");
            int clusterPort = int.Parse(Environment.GetEnvironmentVariable("CLUSTER_PORT") ?? "7946");
            string[] seedNodes = Environment.GetEnvironmentVariable("SEED_NODES")?.Split(',') ?? Array.Empty<string>();

            // Create logger
            using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            // Create SMTP server
            SmtpServer server = new SmtpServerBuilder()
                .Port(smtpPort)
                .ServerName($"Clustered-SMTP-{nodeId}")
                .MaxConnections(1000)
                .MaxConnectionsPerIP(10)
                .LoggerFactory(loggerFactory)
                .Build();

            // Enable clustering
            IClusterManager cluster = await server.EnableClusteringAsync(options =>
            {
                // Basic configuration
                options.NodeId = nodeId;
                options.ClusterPort = clusterPort;
                options.Seeds = seedNodes;

                // Discovery
                options.DiscoveryMethod = DiscoveryMethod.Static; // Use Static for simplicity

                // Security
                options.EnableEncryption = true;
                options.SharedSecret = Environment.GetEnvironmentVariable("CLUSTER_SECRET") ?? "my-secure-secret";

                // Replication
                options.ReplicationFactor = 3;
                options.MinReplicasForWrite = 2;

                // Performance
                options.CompressionEnabled = true;
                options.BatchSize = 100;

                // Persistence
                options.PersistenceEnabled = true;
                options.DataDirectory = $"./cluster-data/{nodeId}";

                // Leader Election
                options.LeaderElection = new LeaderElectionOptions
                {
                    Enabled = true,
                    ElectionTimeout = TimeSpan.FromSeconds(5),
                    HeartbeatInterval = TimeSpan.FromSeconds(1),
                    MinNodes = 2
                };

                // Load Balancing
                options.LoadBalancing = new LoadBalancingOptions
                {
                    Strategy = LoadBalancingStrategy.LeastConnections,
                    HealthBasedRouting = true,
                    MaxLoadPerNode = 0.8,
                    Affinity = new AffinityOptions
                    {
                        Enabled = true,
                        Method = AffinityMethod.SourceIp,
                        SessionTimeout = TimeSpan.FromMinutes(30),
                        FailoverMode = FailoverMode.Automatic
                    }
                };

                // Health Checks
                options.HealthCheck = new HealthCheckOptions
                {
                    Enabled = true,
                    CheckInterval = TimeSpan.FromSeconds(10),
                    FailureThreshold = 3,
                    SuccessThreshold = 2,
                    Timeout = TimeSpan.FromSeconds(5)
                };
            });

            // Subscribe to cluster events
            cluster.NodeJoined += (sender, e) =>
            {
                Console.WriteLine($"[CLUSTER] Node joined: {e.NodeId} from {e.Address}");
            };

            cluster.NodeLeft += (sender, e) =>
            {
                Console.WriteLine($"[CLUSTER] Node left: {e.NodeId} - Reason: {e.Reason}");
            };

            cluster.NodeFailed += async (sender, e) =>
            {
                Console.WriteLine($"[CLUSTER] Node failed: {e.NodeId}");

                // Automatically migrate sessions from failed node
                if (cluster.IsLeader)
                {
                    int migrated = await cluster.MigrateSessionsAsync(e.NodeId);
                    Console.WriteLine($"[CLUSTER] Migrated {migrated} sessions from {e.NodeId}");
                }
            };

            cluster.LeaderChanged += (sender, e) =>
            {
                Console.WriteLine($"[CLUSTER] Leader changed from {e.PreviousLeaderNodeId} to {e.NewLeaderNodeId}");
                if (cluster.IsLeader)
                {
                    Console.WriteLine("[CLUSTER] This node is now the leader!");
                }
            };

            cluster.StateChanged += (sender, e) =>
            {
                Console.WriteLine($"[CLUSTER] State changed: {e.OldState} -> {e.NewState} (Active nodes: {e.ActiveNodes})");
            };

            cluster.RebalancingStarted += (sender, e) =>
            {
                Console.WriteLine($"[CLUSTER] Rebalancing started: {e.Reason} ({e.SessionsToMigrate} sessions)");
            };

            cluster.RebalancingCompleted += (sender, e) =>
            {
                Console.WriteLine($"[CLUSTER] Rebalancing completed: {e.SessionsMigrated} sessions moved in {e.Duration.TotalSeconds:F1}s");
            };

            // Subscribe to SMTP server events
            server.MessageReceived += (sender, e) =>
            {
                string clusterInfo = cluster.IsLeader ? "[LEADER]" : $"[NODE-{nodeId}]";
                Console.WriteLine($"{clusterInfo} Message from {e.Message.From}: {e.Message.Subject}");
                Console.WriteLine($"  Cluster nodes: {cluster.NodeCount}, Leader: {cluster.LeaderNodeId}");
            };

            server.SessionCreated += (sender, e) =>
            {
                Console.WriteLine($"[SESSION] New session {e.Session.Id} from {e.Session.RemoteEndPoint}");
            };

            // Start the SMTP server
            await server.StartAsync();
            Console.WriteLine($"[SERVER] SMTP server started on port {smtpPort}");
            Console.WriteLine($"[CLUSTER] Node {nodeId} listening on cluster port {clusterPort}");

            // Join seed nodes if provided
            foreach (string seed in seedNodes)
            {
                if (!string.IsNullOrEmpty(seed) && seed != $"{nodeId}:{clusterPort}")
                {
                    Console.WriteLine($"[CLUSTER] Attempting to join seed node: {seed}");
                    if (await cluster.JoinAsync(seed))
                    {
                        Console.WriteLine($"[CLUSTER] Successfully joined via {seed}");
                        break;
                    }
                }
            }

            // Start monitoring task
            _ = Task.Run(async () => await MonitorClusterAsync(server, cluster));

            // Wait for termination
            Console.WriteLine("Press 'q' to quit, 'm' for maintenance mode, 's' for stats...");

            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);

                switch (key.Key)
                {
                    case ConsoleKey.Q:
                        Console.WriteLine("[SERVER] Shutting down...");
                        await cluster.LeaveAsync();
                        await server.StopAsync();
                        return;

                    case ConsoleKey.M:
                        if (!cluster.IsInMaintenance)
                        {
                            Console.WriteLine("[CLUSTER] Entering maintenance mode...");
                            await cluster.EnterMaintenanceModeAsync(new MaintenanceOptions
                            {
                                DrainTimeout = TimeSpan.FromMinutes(2),
                                GracefulShutdown = true,
                                MigrateSessions = true,
                                Reason = "Manual maintenance"
                            });
                        }
                        else
                        {
                            Console.WriteLine("[CLUSTER] Exiting maintenance mode...");
                            await cluster.ExitMaintenanceModeAsync();
                        }
                        break;

                    case ConsoleKey.S:
                        await DisplayClusterStatsAsync(cluster);
                        break;

                    case ConsoleKey.H:
                        await DisplayClusterHealthAsync(cluster);
                        break;

                    case ConsoleKey.R:
                        // Test replication
                        string testKey = $"test-{Guid.NewGuid()}";
                        byte[] testData = System.Text.Encoding.UTF8.GetBytes($"Test data from {nodeId}");
                        bool replicated = await cluster.ReplicateStateAsync(testKey, testData, new ReplicationOptions
                        {
                            Priority = ReplicationPriority.High,
                            ConsistencyLevel = ConsistencyLevel.Quorum
                        });
                        Console.WriteLine($"[TEST] Replication test: {(replicated ? "Success" : "Failed")}");
                        break;
                }
            }
        }

        private static async Task MonitorClusterAsync(ISmtpServer server, IClusterManager cluster)
        {
            while (server.IsRunning)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(1));

                    ClusterMetrics metrics = cluster.GetMetrics();
                    ClusterHealth health = await cluster.GetHealthAsync();

                    Console.WriteLine($"[MONITOR] Cluster: {health.Status}, Nodes: {health.HealthyNodes}/{health.TotalNodes}, " +
                                    $"Sessions: {metrics.TotalSessions}, Load: {metrics.AverageLoad:F1}%");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MONITOR] Error: {ex.Message}");
                }
            }
        }

        private static async Task DisplayClusterStatsAsync(IClusterManager cluster)
        {
            ClusterMetrics metrics = cluster.GetMetrics();

            Console.WriteLine("\n=== CLUSTER STATISTICS ===");
            Console.WriteLine($"Total Sessions: {metrics.TotalSessions}");
            Console.WriteLine($"Messages/sec: {metrics.MessagesPerSecond:F1}");
            Console.WriteLine($"Average Load: {metrics.AverageLoad:F1}%");
            Console.WriteLine($"Network I/O: {FormatBytes(metrics.NetworkBandwidth)}/s");
            Console.WriteLine($"Total Messages: {metrics.TotalMessagesProcessed}");
            Console.WriteLine($"Total Transfer: {FormatBytes(metrics.TotalBytesTransferred)}");
            Console.WriteLine($"Failed Sessions: {metrics.FailedSessions}");
            Console.WriteLine($"Rebalancing Ops: {metrics.RebalancingOperations}");
            Console.WriteLine($"Leader Elections: {metrics.LeaderElections}");
            Console.WriteLine($"Node Failures: {metrics.NodeFailures}");
            Console.WriteLine($"Uptime: {metrics.ClusterUptime.TotalHours:F1} hours");
            Console.WriteLine("========================\n");

            await Task.CompletedTask;
        }

        private static async Task DisplayClusterHealthAsync(IClusterManager cluster)
        {
            ClusterHealth health = await cluster.GetHealthAsync();

            Console.WriteLine("\n=== CLUSTER HEALTH ===");
            Console.WriteLine($"Status: {health.Status}");
            Console.WriteLine($"Healthy Nodes: {health.HealthyNodes}/{health.TotalNodes}");
            Console.WriteLine($"Has Quorum: {(health.HasQuorum ? "Yes" : "No")}");
            Console.WriteLine($"Leader: {health.LeaderNodeId ?? "None"}");
            Console.WriteLine($"Check Latency: {health.CheckLatencyMs:F1}ms");

            Console.WriteLine("\nNode Status:");
            foreach (NodeHealthStatus node in health.NodeStatuses.Values)
            {
                string status = node.IsHealthy ? "✓" : "✗";
                Console.WriteLine($"  {status} {node.NodeId}: {node.State}, " +
                                $"CPU: {node.CpuUsage:F1}%, Mem: {node.MemoryUsage:F1}%, " +
                                $"Sessions: {node.ActiveSessions}, Response: {node.ResponseTimeMs:F1}ms");
            }
            Console.WriteLine("===================\n");
        }

        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }

            return $"{len:F1} {sizes[order]}";
        }
    }
}