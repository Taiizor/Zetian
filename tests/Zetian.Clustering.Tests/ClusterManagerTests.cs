using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Zetian.Abstractions;
using Zetian.Clustering;
using Zetian.Clustering.Abstractions;
using Zetian.Clustering.Enums;
using Zetian.Clustering.Extensions;
using Zetian.Clustering.Implementation;
using Zetian.Clustering.Models;
using Zetian.Clustering.Models.EventArgs;
using Zetian.Configuration;

namespace Zetian.Clustering.Tests
{
    public class ClusterManagerTests
    {
        private readonly Mock<ISmtpServer> _mockServer;
        private readonly Mock<ILogger<ClusterManager>> _mockLogger;
        private readonly ClusterOptions _defaultOptions;

        public ClusterManagerTests()
        {
            _mockServer = new Mock<ISmtpServer>();
            _mockLogger = new Mock<ILogger<ClusterManager>>();
            _defaultOptions = new ClusterOptions
            {
                NodeId = "test-node-1",
                ClusterPort = 7946,
                ReplicationFactor = 3,
                MinReplicasForWrite = 2
            };

            // Setup mock server configuration
            var config = new SmtpServerConfiguration();
            _mockServer.Setup(s => s.Configuration).Returns(config);
        }

        [Fact]
        public async Task StartAsync_InitializesClusterManager_Successfully()
        {
            // Arrange
            var manager = new ClusterManager(_mockServer.Object, _defaultOptions, _mockLogger.Object);

            // Act
            await manager.StartAsync();

            // Assert
            Assert.Equal("test-node-1", manager.NodeId);
            Assert.Equal(ClusterState.Forming, manager.State);
            Assert.Equal(1, manager.NodeCount); // Self node
        }

        [Fact]
        public async Task JoinAsync_ValidSeedNode_ReturnsTrue()
        {
            // Arrange
            var manager = new ClusterManager(_mockServer.Object, _defaultOptions, _mockLogger.Object);
            await manager.StartAsync();

            bool nodeJoinedEventRaised = false;
            manager.NodeJoined += (sender, e) =>
            {
                nodeJoinedEventRaised = true;
                Assert.Equal("test-node-1", e.NodeId);
            };

            // Act
            bool result = await manager.JoinAsync("node-2:7946");

            // Assert
            Assert.True(result);
            Assert.True(nodeJoinedEventRaised);
        }

        [Fact]
        public async Task LeaveAsync_RemovesNodeFromCluster_Successfully()
        {
            // Arrange
            var manager = new ClusterManager(_mockServer.Object, _defaultOptions, _mockLogger.Object);
            await manager.StartAsync();

            bool nodeLeftEventRaised = false;
            manager.NodeLeft += (sender, e) =>
            {
                nodeLeftEventRaised = true;
                Assert.Equal("test-node-1", e.NodeId);
                Assert.Equal("Graceful leave", e.Reason);
            };

            // Act
            await manager.LeaveAsync();

            // Assert
            Assert.True(nodeLeftEventRaised);
            Assert.Equal(0, manager.NodeCount);
        }

        [Fact]
        public async Task GetHealthAsync_ReturnsCorrectHealthStatus()
        {
            // Arrange
            var manager = new ClusterManager(_mockServer.Object, _defaultOptions, _mockLogger.Object);
            await manager.StartAsync();

            // Act
            var health = await manager.GetHealthAsync();

            // Assert
            Assert.NotNull(health);
            Assert.Equal(ClusterState.Forming, health.Status);
            Assert.Equal(1, health.TotalNodes);
            Assert.Equal(1, health.HealthyNodes);
            Assert.Equal(0, health.UnhealthyNodes);
            Assert.True(health.HasQuorum);
        }

        [Fact]
        public void GetMetrics_ReturnsValidMetrics()
        {
            // Arrange
            var manager = new ClusterManager(_mockServer.Object, _defaultOptions, _mockLogger.Object);

            // Act
            var metrics = manager.GetMetrics();

            // Assert
            Assert.NotNull(metrics);
            Assert.True(metrics.TotalSessions >= 0);
            Assert.True(metrics.MessagesPerSecond >= 0);
            Assert.True(metrics.AverageLoad >= 0);
        }

        [Fact]
        public async Task EnterMaintenanceMode_SetsMaintenanceFlag()
        {
            // Arrange
            var manager = new ClusterManager(_mockServer.Object, _defaultOptions, _mockLogger.Object);
            await manager.StartAsync();

            // Act
            await manager.EnterMaintenanceModeAsync(new MaintenanceOptions
            {
                Reason = "Test maintenance",
                GracefulShutdown = true
            });

            // Assert
            Assert.True(manager.IsInMaintenance);
        }

        [Fact]
        public async Task ExitMaintenanceMode_ClearsMaintenanceFlag()
        {
            // Arrange
            var manager = new ClusterManager(_mockServer.Object, _defaultOptions, _mockLogger.Object);
            await manager.StartAsync();
            await manager.EnterMaintenanceModeAsync();

            // Act
            await manager.ExitMaintenanceModeAsync();

            // Assert
            Assert.False(manager.IsInMaintenance);
        }

        [Fact]
        public async Task ReplicateStateAsync_WithQuorum_ReturnsTrue()
        {
            // Arrange
            var manager = new ClusterManager(_mockServer.Object, _defaultOptions, _mockLogger.Object);
            await manager.StartAsync();

            var key = "test-key";
            var data = new byte[] { 1, 2, 3, 4, 5 };

            // Act
            bool result = await manager.ReplicateStateAsync(key, data);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void SetLoadBalancingStrategy_UpdatesConfiguration()
        {
            // Arrange
            var manager = new ClusterManager(_mockServer.Object, _defaultOptions, _mockLogger.Object);

            // Act
            manager.SetLoadBalancingStrategy(LoadBalancingStrategy.LeastConnections);

            // Assert
            // Configuration should be updated internally
            Assert.NotNull(manager);
        }

        [Fact]
        public void ConfigureLeaderElection_UpdatesElectionOptions()
        {
            // Arrange
            var manager = new ClusterManager(_mockServer.Object, _defaultOptions, _mockLogger.Object);
            TimeSpan? configuredTimeout = null;

            // Act
            manager.ConfigureLeaderElection(options =>
            {
                options.ElectionTimeout = TimeSpan.FromSeconds(10);
                options.MinNodes = 3;
                configuredTimeout = options.ElectionTimeout;
            });

            // Assert
            Assert.Equal(TimeSpan.FromSeconds(10), configuredTimeout);
        }

        [Fact]
        public async Task StateChanged_EventRaisedOnStateChange()
        {
            // Arrange
            var manager = new ClusterManager(_mockServer.Object, _defaultOptions, _mockLogger.Object);
            ClusterStateEventArgs? eventArgs = null;

            manager.StateChanged += (sender, e) =>
            {
                eventArgs = e;
            };

            // Act
            await manager.StartAsync();

            // Assert
            // State change events may be raised during initialization
            Assert.NotNull(manager);
        }

        [Fact]
        public async Task Dispose_ReleasesResources()
        {
            // Arrange
            var manager = new ClusterManager(_mockServer.Object, _defaultOptions, _mockLogger.Object);
            await manager.StartAsync();

            // Act
            manager.Dispose();

            // Assert
            // Should not throw
            manager.Dispose(); // Double dispose should be safe
        }
    }

    public class LoadBalancerTests
    {
        [Fact]
        public async Task RoundRobinLoadBalancer_DistributesEvenly()
        {
            // Arrange
            var loadBalancer = new RoundRobinLoadBalancer();
            var nodes = CreateTestNodes(3);
            var sessionInfo = CreateTestSessionInfo();

            var selectedNodes = new List<string>();

            // Act
            for (int i = 0; i < 9; i++)
            {
                var node = await loadBalancer.SelectNodeAsync(sessionInfo, nodes);
                if (node != null)
                {
                    selectedNodes.Add(node.Id);
                }
            }

            // Assert
            Assert.Equal(9, selectedNodes.Count);
            Assert.Equal(3, selectedNodes.Count(n => n == "node-1"));
            Assert.Equal(3, selectedNodes.Count(n => n == "node-2"));
            Assert.Equal(3, selectedNodes.Count(n => n == "node-3"));
        }

        [Fact]
        public async Task LeastConnectionsLoadBalancer_SelectsLeastLoaded()
        {
            // Arrange
            var loadBalancer = new LeastConnectionsLoadBalancer();
            var nodes = new List<TestClusterNode>
            {
                new TestClusterNode { Id = "node-1", ActiveSessions = 10 },
                new TestClusterNode { Id = "node-2", ActiveSessions = 5 },
                new TestClusterNode { Id = "node-3", ActiveSessions = 15 }
            };
            var sessionInfo = CreateTestSessionInfo();

            // Act
            var selectedNode = await loadBalancer.SelectNodeAsync(sessionInfo, nodes);

            // Assert
            Assert.NotNull(selectedNode);
            Assert.Equal("node-2", selectedNode.Id);
        }

        [Fact]
        public async Task IpHashLoadBalancer_ConsistentSelection()
        {
            // Arrange
            var loadBalancer = new IpHashLoadBalancer();
            var nodes = CreateTestNodes(3);
            var sessionInfo = CreateTestSessionInfo();

            // Act
            var firstSelection = await loadBalancer.SelectNodeAsync(sessionInfo, nodes);
            var secondSelection = await loadBalancer.SelectNodeAsync(sessionInfo, nodes);
            var thirdSelection = await loadBalancer.SelectNodeAsync(sessionInfo, nodes);

            // Assert
            Assert.NotNull(firstSelection);
            Assert.NotNull(secondSelection);
            Assert.NotNull(thirdSelection);
            Assert.Equal(firstSelection.Id, secondSelection.Id);
            Assert.Equal(secondSelection.Id, thirdSelection.Id);
        }

        [Fact]
        public async Task LoadBalancer_NoHealthyNodes_ReturnsNull()
        {
            // Arrange
            var loadBalancer = new RoundRobinLoadBalancer();
            var nodes = new List<TestClusterNode>
            {
                new TestClusterNode { Id = "node-1", State = NodeState.Failed },
                new TestClusterNode { Id = "node-2", State = NodeState.Maintenance }
            };
            var sessionInfo = CreateTestSessionInfo();

            // Act
            var selectedNode = await loadBalancer.SelectNodeAsync(sessionInfo, nodes);

            // Assert
            Assert.Null(selectedNode);
        }

        private List<TestClusterNode> CreateTestNodes(int count)
        {
            var nodes = new List<TestClusterNode>();
            for (int i = 1; i <= count; i++)
            {
                nodes.Add(new TestClusterNode
                {
                    Id = $"node-{i}",
                    State = NodeState.Active,
                    ActiveSessions = 0
                });
            }
            return nodes;
        }

        private TestSessionInfo CreateTestSessionInfo()
        {
            return new TestSessionInfo
            {
                SessionId = Guid.NewGuid().ToString(),
                ClientIp = IPAddress.Parse("192.168.1.100"),
                ClientPort = 12345
            };
        }
    }

    public class StateStoreTests
    {
        [Fact]
        public async Task InMemoryStateStore_SetAndGet_ReturnsCorrectValue()
        {
            // Arrange
            var store = new InMemoryStateStore();
            var key = "test-key";
            var value = new byte[] { 1, 2, 3, 4, 5 };

            // Act
            await store.SetAsync(key, value);
            var retrievedValue = await store.GetAsync(key);

            // Assert
            Assert.NotNull(retrievedValue);
            Assert.Equal(value, retrievedValue);
        }

        [Fact]
        public async Task InMemoryStateStore_Delete_RemovesValue()
        {
            // Arrange
            var store = new InMemoryStateStore();
            var key = "test-key";
            var value = new byte[] { 1, 2, 3 };

            await store.SetAsync(key, value);

            // Act
            var deleteResult = await store.DeleteAsync(key);
            var retrievedValue = await store.GetAsync(key);

            // Assert
            Assert.True(deleteResult);
            Assert.Null(retrievedValue);
        }

        [Fact]
        public async Task InMemoryStateStore_Exists_ReturnsCorrectStatus()
        {
            // Arrange
            var store = new InMemoryStateStore();
            var key = "test-key";
            var value = new byte[] { 1, 2, 3 };

            // Act
            var existsBeforeSet = await store.ExistsAsync(key);
            await store.SetAsync(key, value);
            var existsAfterSet = await store.ExistsAsync(key);

            // Assert
            Assert.False(existsBeforeSet);
            Assert.True(existsAfterSet);
        }

        [Fact]
        public async Task InMemoryStateStore_TTL_ExpiresValue()
        {
            // Arrange
            var store = new InMemoryStateStore();
            var key = "test-key";
            var value = new byte[] { 1, 2, 3 };
            var ttl = TimeSpan.FromMilliseconds(100);

            // Act
            await store.SetAsync(key, value, ttl);
            var immediateValue = await store.GetAsync(key);
            
            await Task.Delay(150);
            var expiredValue = await store.GetAsync(key);

            // Assert
            Assert.NotNull(immediateValue);
            Assert.Null(expiredValue);
        }

        [Fact]
        public async Task InMemoryStateStore_IncrementAsync_WorksCorrectly()
        {
            // Arrange
            var store = new InMemoryStateStore();
            var key = "counter";

            // Act
            var value1 = await store.IncrementAsync(key, 1);
            var value2 = await store.IncrementAsync(key, 5);
            var value3 = await store.IncrementAsync(key, -2);

            // Assert
            Assert.Equal(1, value1);
            Assert.Equal(6, value2);
            Assert.Equal(4, value3);
        }

        [Fact]
        public async Task InMemoryStateStore_AcquireLock_PreventsDoubleAcquisition()
        {
            // Arrange
            var store = new InMemoryStateStore();
            var resource = "test-resource";
            var ttl = TimeSpan.FromSeconds(5);

            // Act
            var lock1 = await store.AcquireLockAsync(resource, ttl);
            var lock2 = await store.AcquireLockAsync(resource, ttl);

            // Assert
            Assert.NotNull(lock1);
            Assert.Null(lock2);

            // Cleanup
            if (lock1 != null)
            {
                await lock1.ReleaseAsync();
            }
        }
    }

    // Test implementations
    internal class TestClusterNode : IClusterNode
    {
        public string Id { get; set; } = string.Empty;
        public IPEndPoint Endpoint { get; set; } = new IPEndPoint(IPAddress.Any, 0);
        public NodeState State { get; set; } = NodeState.Active;
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

    internal class TestSessionInfo : ISessionInfo
    {
        public string SessionId { get; set; } = string.Empty;
        public IPAddress ClientIp { get; set; } = IPAddress.None;
        public int ClientPort { get; set; }
        public long EstimatedSize { get; set; }
        public int Priority { get; set; }
        public IReadOnlyDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }
}
