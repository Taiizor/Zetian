using Microsoft.Extensions.Logging;
using System.Text;
using Zetian.Abstractions;
using Zetian.Clustering.Abstractions;
using Zetian.Clustering.Enums;
using Zetian.Clustering.Extensions;
using Zetian.Clustering.Models;
using Zetian.Clustering.Options;
using Zetian.Protocol;
using Zetian.Server;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Zetian.Clustering.Examples
{
    /// <summary>
    /// Example of setting up a clustered SMTP server
    /// </summary>
    public class ClusteredSmtpExample
    {
        public static async Task Main()
        {
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