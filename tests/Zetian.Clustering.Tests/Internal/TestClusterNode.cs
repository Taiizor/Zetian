using System.Net;
using Zetian.Clustering.Abstractions;
using Zetian.Clustering.Enums;

namespace Zetian.Clustering.Tests.Internal
{
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
}