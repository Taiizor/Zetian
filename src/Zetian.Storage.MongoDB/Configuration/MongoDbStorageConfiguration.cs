using System;
using Zetian.Storage.Configuration;

namespace Zetian.Storage.MongoDB.Configuration
{
    /// <summary>
    /// Configuration for MongoDB message storage
    /// </summary>
    public class MongoDbStorageConfiguration : BaseStorageConfiguration
    {
        /// <summary>
        /// MongoDB connection string
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Database name
        /// </summary>
        public string DatabaseName { get; set; } = "smtp_database";

        /// <summary>
        /// Collection name for storing messages
        /// </summary>
        public string CollectionName { get; set; } = "messages";

        /// <summary>
        /// Use GridFS for large messages
        /// </summary>
        public bool UseGridFsForLargeMessages { get; set; } = true;

        /// <summary>
        /// Threshold in MB for using GridFS
        /// </summary>
        public double GridFsThresholdMB { get; set; } = 10;

        /// <summary>
        /// GridFS bucket name
        /// </summary>
        public string GridFsBucketName { get; set; } = "message_attachments";

        /// <summary>
        /// Whether to enable TTL (Time To Live) for automatic deletion
        /// </summary>
        public bool EnableTTL { get; set; } = false;

        /// <summary>
        /// TTL in days
        /// </summary>
        public int TTLDays { get; set; } = 30;

        /// <summary>
        /// Whether to create indexes automatically
        /// </summary>
        public bool AutoCreateIndexes { get; set; } = true;

        /// <summary>
        /// Whether to enable sharding
        /// </summary>
        public bool EnableSharding { get; set; } = false;

        /// <summary>
        /// Shard key field
        /// </summary>
        public string ShardKeyField { get; set; } = "received_date";

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

            if (string.IsNullOrWhiteSpace(DatabaseName))
            {
                throw new ArgumentException("DatabaseName is required");
            }

            if (string.IsNullOrWhiteSpace(CollectionName))
            {
                throw new ArgumentException("CollectionName is required");
            }

            if (GridFsThresholdMB <= 0)
            {
                throw new ArgumentException("GridFsThresholdMB must be positive");
            }

            if (TTLDays <= 0 && EnableTTL)
            {
                throw new ArgumentException("TTLDays must be positive when TTL is enabled");
            }

            if (MaxMessageSizeMB < 0)
            {
                throw new ArgumentException("MaxMessageSizeMB must be non-negative");
            }
        }
    }
}