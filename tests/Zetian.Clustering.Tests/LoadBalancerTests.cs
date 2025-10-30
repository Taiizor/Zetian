using System.Net;
using Xunit;
using Zetian.Clustering.Abstractions;
using Zetian.Clustering.Enums;
using Zetian.Clustering.Implementation;
using Zetian.Clustering.Tests.Internal;

namespace Zetian.Clustering.Tests
{
    public class LoadBalancerTests
    {
        [Fact]
        public async Task RoundRobinLoadBalancer_DistributesEvenly()
        {
            // Arrange
            RoundRobinLoadBalancer loadBalancer = new();
            List<TestClusterNode> nodes = CreateTestNodes(3);
            TestSessionInfo sessionInfo = CreateTestSessionInfo();

            List<string> selectedNodes = [];

            // Act
            for (int i = 0; i < 9; i++)
            {
                IClusterNode? node = await loadBalancer.SelectNodeAsync(sessionInfo, nodes);
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
            LeastConnectionsLoadBalancer loadBalancer = new();
            List<TestClusterNode> nodes =
            [
                new TestClusterNode { Id = "node-1", ActiveSessions = 10 },
                new TestClusterNode { Id = "node-2", ActiveSessions = 5 },
                new TestClusterNode { Id = "node-3", ActiveSessions = 15 }
            ];
            TestSessionInfo sessionInfo = CreateTestSessionInfo();

            // Act
            IClusterNode? selectedNode = await loadBalancer.SelectNodeAsync(sessionInfo, nodes);

            // Assert
            Assert.NotNull(selectedNode);
            Assert.Equal("node-2", selectedNode.Id);
        }

        [Fact]
        public async Task IpHashLoadBalancer_ConsistentSelection()
        {
            // Arrange
            IpHashLoadBalancer loadBalancer = new();
            List<TestClusterNode> nodes = CreateTestNodes(3);
            TestSessionInfo sessionInfo = CreateTestSessionInfo();

            // Act
            IClusterNode? firstSelection = await loadBalancer.SelectNodeAsync(sessionInfo, nodes);
            IClusterNode? secondSelection = await loadBalancer.SelectNodeAsync(sessionInfo, nodes);
            IClusterNode? thirdSelection = await loadBalancer.SelectNodeAsync(sessionInfo, nodes);

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
            RoundRobinLoadBalancer loadBalancer = new();
            List<TestClusterNode> nodes =
            [
                new TestClusterNode { Id = "node-1", State = NodeState.Failed },
                new TestClusterNode { Id = "node-2", State = NodeState.Maintenance }
            ];
            TestSessionInfo sessionInfo = CreateTestSessionInfo();

            // Act
            IClusterNode? selectedNode = await loadBalancer.SelectNodeAsync(sessionInfo, nodes);

            // Assert
            Assert.Null(selectedNode);
        }

        private List<TestClusterNode> CreateTestNodes(int count)
        {
            List<TestClusterNode> nodes = [];
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
}