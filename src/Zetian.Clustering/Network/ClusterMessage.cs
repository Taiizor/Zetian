using System;
using System.Collections.Generic;
using Zetian.Clustering.Enums;

namespace Zetian.Clustering.Network
{
    /// <summary>
    /// Base message for cluster communication
    /// </summary>
    public class ClusterMessage
    {
        /// <summary>
        /// Message ID
        /// </summary>
        public string MessageId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Message type
        /// </summary>
        public MessageType Type { get; set; }

        /// <summary>
        /// Source node ID
        /// </summary>
        public string SourceNodeId { get; set; } = string.Empty;

        /// <summary>
        /// Target node ID (null for broadcast)
        /// </summary>
        public string? TargetNodeId { get; set; }

        /// <summary>
        /// Message timestamp
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Message TTL (Time To Live)
        /// </summary>
        public TimeSpan Ttl { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Message payload
        /// </summary>
        public object? Payload { get; set; }

        /// <summary>
        /// Message headers
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = [];

        /// <summary>
        /// Requires acknowledgment
        /// </summary>
        public bool RequiresAck { get; set; }

        /// <summary>
        /// Retry count
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// Maximum retries
        /// </summary>
        public int MaxRetries { get; set; } = 3;
    }

    /// <summary>
    /// Heartbeat message payload
    /// </summary>
    public class HeartbeatPayload
    {
        public NodeState State { get; set; }
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
        public int ActiveSessions { get; set; }
        public long NetworkBandwidth { get; set; }
        public DateTime LastUpdate { get; set; }
        public Dictionary<string, object> Metrics { get; set; } = [];
    }

    /// <summary>
    /// Session replication payload
    /// </summary>
    public class SessionReplicationPayload
    {
        public string SessionId { get; set; } = string.Empty;
        public string NodeId { get; set; } = string.Empty;
        public string ClientIp { get; set; } = string.Empty;
        public int ClientPort { get; set; }
        public DateTime StartTime { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = [];
        public long BytesTransferred { get; set; }
        public int MessageCount { get; set; }
    }

    /// <summary>
    /// State replication payload
    /// </summary>
    public class StateReplicationPayload
    {
        public string Key { get; set; } = string.Empty;
        public byte[] Data { get; set; } = [];
        public DateTime Timestamp { get; set; }
        public TimeSpan? Ttl { get; set; }
        public ConsistencyLevel ConsistencyLevel { get; set; }
        public int Version { get; set; }
    }

    /// <summary>
    /// Configuration update payload
    /// </summary>
    public class ConfigurationPayload
    {
        public string ConfigurationType { get; set; } = string.Empty;
        public object Configuration { get; set; } = new();
        public DateTime EffectiveTime { get; set; }
        public bool RequiresRestart { get; set; }
    }

    /// <summary>
    /// Acknowledgment message
    /// </summary>
    public class AcknowledgmentMessage : ClusterMessage
    {
        public string OriginalMessageId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public object? Result { get; set; }
    }
}