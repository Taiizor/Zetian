using System;
using System.Collections.Generic;
using System.Net;
using Zetian.Clustering.Enums;

namespace Zetian.Clustering.Abstractions
{
    /// <summary>
    /// Represents a node in the cluster
    /// </summary>
    public interface IClusterNode
    {
        /// <summary>
        /// Unique identifier for the node
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Node's network endpoint
        /// </summary>
        IPEndPoint Endpoint { get; }

        /// <summary>
        /// Current state of the node
        /// </summary>
        NodeState State { get; }

        /// <summary>
        /// Whether this node is the leader
        /// </summary>
        bool IsLeader { get; }

        /// <summary>
        /// Node's current load (0.0 to 1.0)
        /// </summary>
        double CurrentLoad { get; }

        /// <summary>
        /// Number of active sessions on this node
        /// </summary>
        int ActiveSessions { get; }

        /// <summary>
        /// Last heartbeat received
        /// </summary>
        DateTime LastHeartbeat { get; }

        /// <summary>
        /// Node metadata
        /// </summary>
        IReadOnlyDictionary<string, string> Metadata { get; }

        /// <summary>
        /// Node version
        /// </summary>
        Version Version { get; }

        /// <summary>
        /// Node's region (for multi-region deployments)
        /// </summary>
        string? Region { get; }

        /// <summary>
        /// Node's availability zone
        /// </summary>
        string? AvailabilityZone { get; }

        /// <summary>
        /// CPU usage percentage
        /// </summary>
        double CpuUsage { get; }

        /// <summary>
        /// Memory usage percentage
        /// </summary>
        double MemoryUsage { get; }

        /// <summary>
        /// Network bandwidth usage (bytes/sec)
        /// </summary>
        long NetworkBandwidth { get; }

        /// <summary>
        /// Node join time
        /// </summary>
        DateTime JoinTime { get; }

        /// <summary>
        /// Node weight for load balancing
        /// </summary>
        int Weight { get; }

        /// <summary>
        /// Whether the node is in maintenance mode
        /// </summary>
        bool IsInMaintenance { get; }
    }
}