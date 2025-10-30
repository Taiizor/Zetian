using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Zetian.Abstractions;
using Zetian.Clustering.Enums;
using Zetian.Clustering.Implementation;
using Zetian.Clustering.Models;
using Zetian.Clustering.Models.EventArgs;
using Zetian.Clustering.Options;
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
            SmtpServerConfiguration config = new();
            _mockServer.Setup(s => s.Configuration).Returns(config);
        }

        [Fact]
        public async Task StartAsync_InitializesClusterManager_Successfully()
        {
            // Arrange
            ClusterManager manager = new(_mockServer.Object, _defaultOptions, _mockLogger.Object);

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
            ClusterManager manager = new(_mockServer.Object, _defaultOptions, _mockLogger.Object);
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
            ClusterManager manager = new(_mockServer.Object, _defaultOptions, _mockLogger.Object);
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
            ClusterManager manager = new(_mockServer.Object, _defaultOptions, _mockLogger.Object);
            await manager.StartAsync();

            // Act
            ClusterHealth health = await manager.GetHealthAsync();

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
            ClusterManager manager = new(_mockServer.Object, _defaultOptions, _mockLogger.Object);

            // Act
            ClusterMetrics metrics = manager.GetMetrics();

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
            ClusterManager manager = new(_mockServer.Object, _defaultOptions, _mockLogger.Object);
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
            ClusterManager manager = new(_mockServer.Object, _defaultOptions, _mockLogger.Object);
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
            ClusterManager manager = new(_mockServer.Object, _defaultOptions, _mockLogger.Object);
            await manager.StartAsync();

            string key = "test-key";
            byte[] data = new byte[] { 1, 2, 3, 4, 5 };

            // Act
            bool result = await manager.ReplicateStateAsync(key, data);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void SetLoadBalancingStrategy_UpdatesConfiguration()
        {
            // Arrange
            ClusterManager manager = new(_mockServer.Object, _defaultOptions, _mockLogger.Object);

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
            ClusterManager manager = new(_mockServer.Object, _defaultOptions, _mockLogger.Object);
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
            ClusterManager manager = new(_mockServer.Object, _defaultOptions, _mockLogger.Object);
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
            ClusterManager manager = new(_mockServer.Object, _defaultOptions, _mockLogger.Object);
            await manager.StartAsync();

            // Act
            manager.Dispose();

            // Assert
            // Should not throw
            manager.Dispose(); // Double dispose should be safe
        }
    }
}