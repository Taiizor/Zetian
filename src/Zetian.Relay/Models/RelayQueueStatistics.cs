using System;
using System.Collections.Generic;
using Zetian.Relay.Enums;

namespace Zetian.Relay
{
    /// <summary>
    /// Represents statistics for the relay queue
    /// </summary>
    public class RelayQueueStatistics
    {
        /// <summary>
        /// Gets or sets the total number of messages in queue
        /// </summary>
        public int TotalMessages { get; set; }

        /// <summary>
        /// Gets or sets the number of queued messages
        /// </summary>
        public int QueuedMessages { get; set; }

        /// <summary>
        /// Gets or sets the number of messages in progress
        /// </summary>
        public int InProgressMessages { get; set; }

        /// <summary>
        /// Gets or sets the number of deferred messages
        /// </summary>
        public int DeferredMessages { get; set; }

        /// <summary>
        /// Gets or sets the number of delivered messages
        /// </summary>
        public int DeliveredMessages { get; set; }

        /// <summary>
        /// Gets or sets the number of failed messages
        /// </summary>
        public int FailedMessages { get; set; }

        /// <summary>
        /// Gets or sets the number of expired messages
        /// </summary>
        public int ExpiredMessages { get; set; }

        /// <summary>
        /// Gets or sets the total size of queued messages in bytes
        /// </summary>
        public long TotalSize { get; set; }

        /// <summary>
        /// Gets or sets the oldest message time
        /// </summary>
        public DateTime? OldestMessageTime { get; set; }

        /// <summary>
        /// Gets or sets the average queue time
        /// </summary>
        public TimeSpan AverageQueueTime { get; set; }

        /// <summary>
        /// Gets or sets the average retry count
        /// </summary>
        public double AverageRetryCount { get; set; }

        /// <summary>
        /// Gets or sets messages grouped by priority
        /// </summary>
        public Dictionary<RelayPriority, int> MessagesByPriority { get; set; } = [];

        /// <summary>
        /// Gets or sets messages grouped by smart host
        /// </summary>
        public Dictionary<string, int> MessagesBySmartHost { get; set; } = [];

        /// <summary>
        /// Gets or sets the last queue update time
        /// </summary>
        public DateTime LastUpdateTime { get; set; } = DateTime.UtcNow;
    }
}