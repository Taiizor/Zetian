using System;
using Zetian.Storage.Providers.Common;

namespace Zetian.Storage.Providers.MongoDB
{
    /// <summary>
    /// Configuration for MongoDB message storage
    /// </summary>
    public class MongoDbStorageConfiguration : BaseStorageConfiguration
    {
        /// <summary>
        /// Gets or sets the MongoDB connection string
        /// </summary>
        public string ConnectionString { get; set; } = "mongodb://localhost:27017";

        /// <summary>
        /// Gets or sets the database name
        /// </summary>
        public string DatabaseName { get; set; } = "smtp_server";

        /// <summary>
        /// Gets or sets the collection name for messages
        /// </summary>
        public string CollectionName { get; set; } = "messages";

        /// <summary>
        /// Gets or sets whether to create indexes automatically
        /// </summary>
        public bool AutoCreateIndexes { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to use GridFS for large messages
        /// </summary>
        public bool UseGridFsForLargeMessages { get; set; } = true;

        /// <summary>
        /// Gets or sets the threshold in MB for using GridFS (default 10MB)
        /// </summary>
        public int GridFsThresholdMB { get; set; } = 10;

        /// <summary>
        /// Gets or sets the GridFS bucket name
        /// </summary>
        public string GridFsBucketName { get; set; } = "message_attachments";

        /// <summary>
        /// Gets or sets whether to enable TTL (Time To Live) for automatic message deletion
        /// </summary>
        public bool EnableTTL { get; set; } = false;

        /// <summary>
        /// Gets or sets the TTL in days (messages older than this will be auto-deleted)
        /// </summary>
        public int TTLDays { get; set; } = 90;

        /// <summary>
        /// Gets or sets whether to compress message body
        /// </summary>
        public bool CompressMessageBody { get; set; } = false;

        /// <summary>
        /// Gets or sets the maximum message size in MB (0 = unlimited)
        /// </summary>
        public int MaxMessageSizeMB { get; set; } = 25;

        /// <summary>
        /// Gets or sets whether to enable sharding
        /// </summary>
        public bool EnableSharding { get; set; } = false;

        /// <summary>
        /// Gets or sets the shard key field
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