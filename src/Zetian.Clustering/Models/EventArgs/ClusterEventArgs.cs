using System;
using System.Collections.Generic;
using System.Net;
using Zetian.Clustering.Enums;

namespace Zetian.Clustering.Models.EventArgs
{
    /// <summary>
    /// Base class for cluster event arguments
    /// </summary>
    public abstract class ClusterEventArgs : System.EventArgs
    {
        /// <summary>
        /// Event timestamp
        /// </summary>
        public DateTime Timestamp { get; } = DateTime.UtcNow;

        /// <summary>
        /// Source node ID
        /// </summary>
        public string SourceNodeId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Event arguments for node events
    /// </summary>
    public class NodeEventArgs : ClusterEventArgs
    {
        /// <summary>
        /// Node ID
        /// </summary>
        public string NodeId { get; set; } = string.Empty;

        /// <summary>
        /// Node address
        /// </summary>
        public IPEndPoint? Address { get; set; }

        /// <summary>
        /// Node state
        /// </summary>
        public NodeState State { get; set; }

        /// <summary>
        /// Reason for the event
        /// </summary>
        public string? Reason { get; set; }

        /// <summary>
        /// Node metadata
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = [];
    }

    /// <summary>
    /// Event arguments for leader change
    /// </summary>
    public class LeaderChangedEventArgs : ClusterEventArgs
    {
        /// <summary>
        /// Previous leader node ID
        /// </summary>
        public string? PreviousLeaderNodeId { get; set; }

        /// <summary>
        /// New leader node ID
        /// </summary>
        public string? NewLeaderNodeId { get; set; }

        /// <summary>
        /// Election term
        /// </summary>
        public long Term { get; set; }

        /// <summary>
        /// Election duration
        /// </summary>
        public TimeSpan ElectionDuration { get; set; }

        /// <summary>
        /// Number of votes received
        /// </summary>
        public int VotesReceived { get; set; }
    }

    /// <summary>
    /// Event arguments for cluster state changes
    /// </summary>
    public class ClusterStateEventArgs : ClusterEventArgs
    {
        /// <summary>
        /// Previous state
        /// </summary>
        public ClusterState OldState { get; set; }

        /// <summary>
        /// New state
        /// </summary>
        public ClusterState NewState { get; set; }

        /// <summary>
        /// Reason for state change
        /// </summary>
        public string? Reason { get; set; }

        /// <summary>
        /// Number of active nodes
        /// </summary>
        public int ActiveNodes { get; set; }

        /// <summary>
        /// Whether cluster has quorum
        /// </summary>
        public bool HasQuorum { get; set; }
    }

    /// <summary>
    /// Event arguments for rebalancing operations
    /// </summary>
    public class RebalancingEventArgs : ClusterEventArgs
    {
        /// <summary>
        /// Reason for rebalancing
        /// </summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// Number of sessions to migrate
        /// </summary>
        public int SessionsToMigrate { get; set; }

        /// <summary>
        /// Number of sessions migrated
        /// </summary>
        public int SessionsMigrated { get; set; }

        /// <summary>
        /// Source nodes
        /// </summary>
        public List<string> SourceNodes { get; set; } = [];

        /// <summary>
        /// Target nodes
        /// </summary>
        public List<string> TargetNodes { get; set; } = [];

        /// <summary>
        /// Whether rebalancing completed successfully
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Duration of rebalancing operation
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Error message if failed
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Event arguments for configuration updates
    /// </summary>
    public class ConfigurationEventArgs : ClusterEventArgs
    {
        /// <summary>
        /// Configuration type
        /// </summary>
        public string ConfigurationType { get; set; } = string.Empty;

        /// <summary>
        /// Configuration key
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Old value
        /// </summary>
        public object? OldValue { get; set; }

        /// <summary>
        /// New value
        /// </summary>
        public object? NewValue { get; set; }

        /// <summary>
        /// Whether update was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Nodes that were updated
        /// </summary>
        public List<string> UpdatedNodes { get; set; } = [];
    }

    /// <summary>
    /// Event arguments for partition events
    /// </summary>
    public class PartitionEventArgs : ClusterEventArgs
    {
        /// <summary>
        /// Partition ID
        /// </summary>
        public string PartitionId { get; set; } = string.Empty;

        /// <summary>
        /// Nodes in partition A
        /// </summary>
        public List<string> PartitionA { get; set; } = [];

        /// <summary>
        /// Nodes in partition B
        /// </summary>
        public List<string> PartitionB { get; set; } = [];

        /// <summary>
        /// Whether partition was healed
        /// </summary>
        public bool IsHealed { get; set; }

        /// <summary>
        /// Duration of partition
        /// </summary>
        public TimeSpan? Duration { get; set; }

        /// <summary>
        /// Data loss occurred
        /// </summary>
        public bool DataLossOccurred { get; set; }

        /// <summary>
        /// Number of sessions affected
        /// </summary>
        public int AffectedSessions { get; set; }
    }

    /// <summary>
    /// Event arguments for session migration
    /// </summary>
    public class SessionMigrationEventArgs : ClusterEventArgs
    {
        /// <summary>
        /// Session ID
        /// </summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// Source node ID
        /// </summary>
        public string FromNodeId { get; set; } = string.Empty;

        /// <summary>
        /// Target node ID
        /// </summary>
        public string ToNodeId { get; set; } = string.Empty;

        /// <summary>
        /// Whether migration was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Migration duration
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Error message if failed
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Event arguments for health check events
    /// </summary>
    public class HealthCheckEventArgs : ClusterEventArgs
    {
        /// <summary>
        /// Node ID being checked
        /// </summary>
        public string NodeId { get; set; } = string.Empty;

        /// <summary>
        /// Health status
        /// </summary>
        public NodeHealthStatus HealthStatus { get; set; } = new();

        /// <summary>
        /// Previous health state
        /// </summary>
        public bool WasHealthy { get; set; }

        /// <summary>
        /// Current health state
        /// </summary>
        public bool IsHealthy { get; set; }

        /// <summary>
        /// Check duration
        /// </summary>
        public TimeSpan CheckDuration { get; set; }
    }
}