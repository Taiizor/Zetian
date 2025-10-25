using System;
using Zetian.Storage.Providers.Common;

namespace Zetian.Storage.Providers.PostgreSQL
{
    /// <summary>
    /// Configuration for PostgreSQL message storage
    /// </summary>
    public class PostgreSqlStorageConfiguration : BaseStorageConfiguration
    {
        /// <summary>
        /// Gets or sets the connection string to the PostgreSQL database
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the table to store messages
        /// </summary>
        public string TableName { get; set; } = "smtp_messages";

        /// <summary>
        /// Gets or sets the schema name for the table
        /// </summary>
        public string SchemaName { get; set; } = "public";

        /// <summary>
        /// Gets or sets whether to automatically create the table if it doesn't exist
        /// </summary>
        public bool AutoCreateTable { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to use JSONB for headers storage
        /// </summary>
        public bool UseJsonbForHeaders { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to store message body as compressed data
        /// </summary>
        public bool CompressMessageBody { get; set; } = false;

        /// <summary>
        /// Gets or sets the maximum message size in MB (0 = unlimited)
        /// </summary>
        public int MaxMessageSizeMB { get; set; } = 25;

        /// <summary>
        /// Gets or sets whether to enable table partitioning by date
        /// </summary>
        public bool EnablePartitioning { get; set; } = false;

        /// <summary>
        /// Gets or sets the partition interval (daily, monthly, yearly)
        /// </summary>
        public PartitionInterval PartitionInterval { get; set; } = PartitionInterval.Monthly;

        /// <summary>
        /// Gets or sets whether to create indexes on common query fields
        /// </summary>
        public bool CreateIndexes { get; set; } = true;

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

            if (string.IsNullOrWhiteSpace(TableName))
            {
                throw new ArgumentException("TableName is required");
            }

            if (string.IsNullOrWhiteSpace(SchemaName))
            {
                throw new ArgumentException("SchemaName is required");
            }

            if (MaxMessageSizeMB < 0)
            {
                throw new ArgumentException("MaxMessageSizeMB must be non-negative");
            }
        }

        /// <summary>
        /// Gets the fully qualified table name
        /// </summary>
        public string GetFullTableName()
        {
            return $"\"{SchemaName}\".\"{TableName}\"";
        }
    }

    /// <summary>
    /// Partition interval for PostgreSQL tables
    /// </summary>
    public enum PartitionInterval
    {
        Daily,
        Monthly,
        Yearly
    }
}
