using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Zetian.Clustering.Abstractions;
using Zetian.Clustering.Enums;

namespace Zetian.Clustering.Implementation
{
    /// <summary>
    /// Round-robin load balancer implementation
    /// </summary>
    public class RoundRobinLoadBalancer : ILoadBalancer
    {
        private readonly ConcurrentDictionary<string, NodeStatistics> _statistics;
        private int _currentIndex;

        public RoundRobinLoadBalancer()
        {
            _statistics = new ConcurrentDictionary<string, NodeStatistics>();
            _currentIndex = 0;
        }

        public Task<IClusterNode?> SelectNodeAsync(
            ISessionInfo sessionInfo,
            IReadOnlyCollection<IClusterNode> availableNodes,
            CancellationToken cancellationToken = default)
        {
            if (availableNodes == null || availableNodes.Count == 0)
            {
                return Task.FromResult<IClusterNode?>(null);
            }

            // Filter healthy nodes
            List<IClusterNode> healthyNodes = availableNodes
                .Where(n => n.State == NodeState.Active && !n.IsInMaintenance)
                .ToList();

            if (healthyNodes.Count == 0)
            {
                return Task.FromResult<IClusterNode?>(null);
            }

            // Simple round-robin selection
            int index = Interlocked.Increment(ref _currentIndex) % healthyNodes.Count;
            IClusterNode selectedNode = healthyNodes[index];

            // Update statistics
            NodeStatistics stats = _statistics.GetOrAdd(selectedNode.Id, k => new NodeStatistics());
            stats.RequestCount++;
            stats.LastSelected = DateTime.UtcNow;

            return Task.FromResult<IClusterNode?>(selectedNode);
        }

        public Task UpdateStatisticsAsync(IClusterNode node, bool success, CancellationToken cancellationToken = default)
        {
            if (node == null)
            {
                return Task.CompletedTask;
            }

            NodeStatistics stats = _statistics.GetOrAdd(node.Id, k => new NodeStatistics());

            if (success)
            {
                stats.SuccessCount++;
            }
            else
            {
                stats.FailureCount++;
            }

            return Task.CompletedTask;
        }

        public Task ResetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            _statistics.Clear();
            Interlocked.Exchange(ref _currentIndex, 0);
            return Task.CompletedTask;
        }

        private class NodeStatistics
        {
            public long RequestCount { get; set; }
            public long SuccessCount { get; set; }
            public long FailureCount { get; set; }
            public DateTime LastSelected { get; set; }
        }
    }

    /// <summary>
    /// Least connections load balancer implementation
    /// </summary>
    public class LeastConnectionsLoadBalancer : ILoadBalancer
    {
        private readonly ConcurrentDictionary<string, NodeStatistics> _statistics;

        public LeastConnectionsLoadBalancer()
        {
            _statistics = new ConcurrentDictionary<string, NodeStatistics>();
        }

        public Task<IClusterNode?> SelectNodeAsync(
            ISessionInfo sessionInfo,
            IReadOnlyCollection<IClusterNode> availableNodes,
            CancellationToken cancellationToken = default)
        {
            if (availableNodes == null || availableNodes.Count == 0)
            {
                return Task.FromResult<IClusterNode?>(null);
            }

            // Filter healthy nodes
            List<IClusterNode> healthyNodes = availableNodes
                .Where(n => n.State == NodeState.Active && !n.IsInMaintenance)
                .ToList();

            if (healthyNodes.Count == 0)
            {
                return Task.FromResult<IClusterNode?>(null);
            }

            // Select node with least connections
            IClusterNode? selectedNode = healthyNodes
                .OrderBy(n => n.ActiveSessions)
                .ThenBy(n => n.CurrentLoad)
                .FirstOrDefault();

            if (selectedNode != null)
            {
                NodeStatistics stats = _statistics.GetOrAdd(selectedNode.Id, k => new NodeStatistics());
                stats.RequestCount++;
                stats.LastSelected = DateTime.UtcNow;
            }

            return Task.FromResult(selectedNode);
        }

        public Task UpdateStatisticsAsync(IClusterNode node, bool success, CancellationToken cancellationToken = default)
        {
            if (node == null)
            {
                return Task.CompletedTask;
            }

            NodeStatistics stats = _statistics.GetOrAdd(node.Id, k => new NodeStatistics());

            if (success)
            {
                stats.SuccessCount++;
            }
            else
            {
                stats.FailureCount++;
            }

            return Task.CompletedTask;
        }

        public Task ResetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            _statistics.Clear();
            return Task.CompletedTask;
        }

        private class NodeStatistics
        {
            public long RequestCount { get; set; }
            public long SuccessCount { get; set; }
            public long FailureCount { get; set; }
            public DateTime LastSelected { get; set; }
        }
    }

    /// <summary>
    /// Weighted round-robin load balancer implementation
    /// </summary>
    public class WeightedRoundRobinLoadBalancer : ILoadBalancer
    {
        private readonly ConcurrentDictionary<string, NodeStatistics> _statistics;
        private readonly Dictionary<string, int> _nodeWeights;
        private readonly List<string> _weightedNodeList;
        private int _currentIndex;

        public WeightedRoundRobinLoadBalancer(Dictionary<string, int>? nodeWeights = null)
        {
            _statistics = new ConcurrentDictionary<string, NodeStatistics>();
            _nodeWeights = nodeWeights ?? [];
            _weightedNodeList = [];
            _currentIndex = 0;

            BuildWeightedList();
        }

        public Task<IClusterNode?> SelectNodeAsync(
            ISessionInfo sessionInfo,
            IReadOnlyCollection<IClusterNode> availableNodes,
            CancellationToken cancellationToken = default)
        {
            if (availableNodes == null || availableNodes.Count == 0)
            {
                return Task.FromResult<IClusterNode?>(null);
            }

            // Filter healthy nodes
            Dictionary<string, IClusterNode> healthyNodes = availableNodes
                .Where(n => n.State == NodeState.Active && !n.IsInMaintenance)
                .ToDictionary(n => n.Id, n => n);

            if (healthyNodes.Count == 0)
            {
                return Task.FromResult<IClusterNode?>(null);
            }

            // Build weighted list if needed
            if (_weightedNodeList.Count == 0)
            {
                BuildWeightedListFromNodes(healthyNodes.Keys);
            }

            IClusterNode? selectedNode = null;

            // Try to find a healthy node from weighted list
            for (int attempts = 0; attempts < _weightedNodeList.Count; attempts++)
            {
                int index = Interlocked.Increment(ref _currentIndex) % _weightedNodeList.Count;
                string nodeId = _weightedNodeList[index];

                if (healthyNodes.TryGetValue(nodeId, out selectedNode))
                {
                    break;
                }
            }

            // Fallback to first healthy node if weighted selection failed
            selectedNode ??= healthyNodes.Values.FirstOrDefault();

            if (selectedNode != null)
            {
                NodeStatistics stats = _statistics.GetOrAdd(selectedNode.Id, k => new NodeStatistics());
                stats.RequestCount++;
                stats.LastSelected = DateTime.UtcNow;
            }

            return Task.FromResult(selectedNode);
        }

        public Task UpdateStatisticsAsync(IClusterNode node, bool success, CancellationToken cancellationToken = default)
        {
            if (node == null)
            {
                return Task.CompletedTask;
            }

            NodeStatistics stats = _statistics.GetOrAdd(node.Id, k => new NodeStatistics());

            if (success)
            {
                stats.SuccessCount++;
            }
            else
            {
                stats.FailureCount++;
            }

            return Task.CompletedTask;
        }

        public Task ResetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            _statistics.Clear();
            Interlocked.Exchange(ref _currentIndex, 0);
            return Task.CompletedTask;
        }

        public void UpdateWeights(Dictionary<string, int> nodeWeights)
        {
            _nodeWeights.Clear();
            foreach (KeyValuePair<string, int> kvp in nodeWeights)
            {
                _nodeWeights[kvp.Key] = kvp.Value;
            }
            BuildWeightedList();
        }

        private void BuildWeightedList()
        {
            _weightedNodeList.Clear();

            foreach (KeyValuePair<string, int> kvp in _nodeWeights)
            {
                int weight = Math.Max(1, kvp.Value); // Ensure minimum weight of 1
                for (int i = 0; i < weight; i++)
                {
                    _weightedNodeList.Add(kvp.Key);
                }
            }
        }

        private void BuildWeightedListFromNodes(IEnumerable<string> nodeIds)
        {
            _weightedNodeList.Clear();

            foreach (string nodeId in nodeIds)
            {
                int weight = _nodeWeights.TryGetValue(nodeId, out int w) ? w : 1;
                weight = Math.Max(1, weight);

                for (int i = 0; i < weight; i++)
                {
                    _weightedNodeList.Add(nodeId);
                }
            }
        }

        private class NodeStatistics
        {
            public long RequestCount { get; set; }
            public long SuccessCount { get; set; }
            public long FailureCount { get; set; }
            public DateTime LastSelected { get; set; }
        }
    }

    /// <summary>
    /// IP hash-based load balancer implementation
    /// </summary>
    public class IpHashLoadBalancer : ILoadBalancer
    {
        private readonly ConcurrentDictionary<string, NodeStatistics> _statistics;

        public IpHashLoadBalancer()
        {
            _statistics = new ConcurrentDictionary<string, NodeStatistics>();
        }

        public Task<IClusterNode?> SelectNodeAsync(
            ISessionInfo sessionInfo,
            IReadOnlyCollection<IClusterNode> availableNodes,
            CancellationToken cancellationToken = default)
        {
            if (availableNodes == null || availableNodes.Count == 0)
            {
                return Task.FromResult<IClusterNode?>(null);
            }

            // Filter healthy nodes
            List<IClusterNode> healthyNodes = availableNodes
                .Where(n => n.State == NodeState.Active && !n.IsInMaintenance)
                .ToList();

            if (healthyNodes.Count == 0)
            {
                return Task.FromResult<IClusterNode?>(null);
            }

            // Use IP hash to select node
            int hash = sessionInfo.ClientIp.GetHashCode();
            int index = Math.Abs(hash) % healthyNodes.Count;
            IClusterNode selectedNode = healthyNodes[index];

            NodeStatistics stats = _statistics.GetOrAdd(selectedNode.Id, k => new NodeStatistics());
            stats.RequestCount++;
            stats.LastSelected = DateTime.UtcNow;

            return Task.FromResult<IClusterNode?>(selectedNode);
        }

        public Task UpdateStatisticsAsync(IClusterNode node, bool success, CancellationToken cancellationToken = default)
        {
            if (node == null)
            {
                return Task.CompletedTask;
            }

            NodeStatistics stats = _statistics.GetOrAdd(node.Id, k => new NodeStatistics());

            if (success)
            {
                stats.SuccessCount++;
            }
            else
            {
                stats.FailureCount++;
            }

            return Task.CompletedTask;
        }

        public Task ResetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            _statistics.Clear();
            return Task.CompletedTask;
        }

        private class NodeStatistics
        {
            public long RequestCount { get; set; }
            public long SuccessCount { get; set; }
            public long FailureCount { get; set; }
            public DateTime LastSelected { get; set; }
        }
    }
}