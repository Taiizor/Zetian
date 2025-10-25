using System;
using Zetian.Storage.Providers.Common;

namespace Zetian.Storage.Providers.Redis
{
    /// <summary>
    /// Configuration for Redis message storage/caching
    /// </summary>
    public class RedisStorageConfiguration : BaseStorageConfiguration
    {
        /// <summary>
        /// Gets or sets the Redis connection string
        /// </summary>
        public string ConnectionString { get; set; } = "localhost:6379";

        /// <summary>
        /// Gets or sets the database number to use (0-15)
        /// </summary>
        public int DatabaseNumber { get; set; } = 0;

        /// <summary>
        /// Gets or sets the key prefix for all messages
        /// </summary>
        public string KeyPrefix { get; set; } = "smtp:msg:";

        /// <summary>
        /// Gets or sets the key prefix for message metadata
        /// </summary>
        public string MetadataKeyPrefix { get; set; } = "smtp:meta:";

        /// <summary>
        /// Gets or sets the TTL for messages in seconds (0 = no expiration)
        /// </summary>
        public int MessageTTLSeconds { get; set; } = 86400; // 24 hours default

        /// <summary>
        /// Gets or sets whether to compress message body
        /// </summary>
        public bool CompressMessageBody { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum message size in MB (0 = unlimited)
        /// </summary>
        public int MaxMessageSizeMB { get; set; } = 10; // Redis is memory-based, so limit by default

        /// <summary>
        /// Gets or sets whether to use Redis Streams for message queue
        /// </summary>
        public bool UseRedisStreams { get; set; } = false;

        /// <summary>
        /// Gets or sets the stream key name when using Redis Streams
        /// </summary>
        public string StreamKey { get; set; } = "smtp:stream";

        /// <summary>
        /// Gets or sets whether to store message in chunks for large messages
        /// </summary>
        public bool EnableChunking { get; set; } = true;

        /// <summary>
        /// Gets or sets the chunk size in KB
        /// </summary>
        public int ChunkSizeKB { get; set; } = 64;

        /// <summary>
        /// Gets or sets whether to use Redis pub/sub for notifications
        /// </summary>
        public bool EnablePubSub { get; set; } = false;

        /// <summary>
        /// Gets or sets the pub/sub channel name
        /// </summary>
        public string PubSubChannel { get; set; } = "smtp:notifications";

        /// <summary>
        /// Gets or sets whether to maintain an index of messages
        /// </summary>
        public bool MaintainIndex { get; set; } = true;

        /// <summary>
        /// Gets or sets the sorted set key for message index
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
        /// Gets the full Redis key for a message
        /// </summary>
        public string GetMessageKey(string messageId)
        {
            return $"{KeyPrefix}{messageId}";
        }

        /// <summary>
        /// Gets the full Redis key for message metadata
        /// </summary>
        public string GetMetadataKey(string messageId)
        {
            return $"{MetadataKeyPrefix}{messageId}";
        }

        /// <summary>
        /// Gets the chunk key for large messages
        /// </summary>
        public string GetChunkKey(string messageId, int chunkIndex)
        {
            return $"{KeyPrefix}{messageId}:chunk:{chunkIndex}";
        }
    }
}
