using System;
using Zetian.Storage.Common;

namespace Zetian.Storage.Redis
{
    /// <summary>
    /// Configuration for Redis message storage
    /// </summary>
    public class RedisStorageConfiguration : BaseStorageConfiguration
    {
        /// <summary>
        /// Redis connection string
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Database number (0-15)
        /// </summary>
        public int DatabaseNumber { get; set; } = 0;

        /// <summary>
        /// Key prefix for all Redis keys
        /// </summary>
        public string KeyPrefix { get; set; } = "smtp:";

        /// <summary>
        /// Message TTL in seconds (0 = no expiration)
        /// </summary>
        public int MessageTTLSeconds { get; set; } = 3600; // 1 hour default

        /// <summary>
        /// Whether to enable chunking for large messages
        /// </summary>
        public bool EnableChunking { get; set; } = true;

        /// <summary>
        /// Chunk size in KB
        /// </summary>
        public int ChunkSizeKB { get; set; } = 64;

        /// <summary>
        /// Whether to use Redis Streams
        /// </summary>
        public bool UseRedisStreams { get; set; } = false;

        /// <summary>
        /// Stream key for Redis Streams
        /// </summary>
        public string StreamKey { get; set; } = "smtp:stream";

        /// <summary>
        /// Whether to enable Pub/Sub notifications
        /// </summary>
        public bool EnablePubSub { get; set; } = false;

        /// <summary>
        /// Pub/Sub channel for notifications
        /// </summary>
        public string PubSubChannel { get; set; } = "smtp:notifications";

        /// <summary>
        /// Whether to maintain an index of messages
        /// </summary>
        public bool MaintainIndex { get; set; } = true;

        /// <summary>
        /// Index key for sorted set
        /// </summary>
        public string IndexKey { get; set; } = "smtp:index";

        /// <summary>
        /// Validates the configuration
        /// </summary>
        public override void Validate()
        {
            base.Validate();

            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                throw new ArgumentException("ConnectionString is required");
            }

            if (DatabaseNumber is < 0 or > 15)
            {
                throw new ArgumentException("DatabaseNumber must be between 0 and 15");
            }

            if (string.IsNullOrWhiteSpace(KeyPrefix))
            {
                throw new ArgumentException("KeyPrefix is required");
            }

            if (MessageTTLSeconds < 0)
            {
                throw new ArgumentException("MessageTTLSeconds must be non-negative");
            }

            if (MaxMessageSizeMB < 0)
            {
                throw new ArgumentException("MaxMessageSizeMB must be non-negative");
            }

            if (ChunkSizeKB <= 0)
            {
                throw new ArgumentException("ChunkSizeKB must be positive");
            }
        }

        /// <summary>
        /// Gets the full key for a message
        /// </summary>
        public string GetMessageKey(string messageId)
        {
            return $"{KeyPrefix}msg:{messageId}";
        }

        /// <summary>
        /// Gets the metadata key for a message
        /// </summary>
        public string GetMetadataKey(string messageId)
        {
            return $"{KeyPrefix}meta:{messageId}";
        }

        /// <summary>
        /// Gets the chunk key for a message chunk
        /// </summary>
        public string GetChunkKey(string messageId, int chunkIndex)
        {
            return $"{KeyPrefix}chunk:{messageId}:{chunkIndex}";
        }
    }
}
