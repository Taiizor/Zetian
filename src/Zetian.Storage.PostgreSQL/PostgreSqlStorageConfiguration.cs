using System;
using Zetian.Storage.Common;

namespace Zetian.Storage.PostgreSQL
{
    /// <summary>
    /// Configuration for PostgreSQL message storage
    /// </summary>
    public class PostgreSqlStorageConfiguration : BaseStorageConfiguration
    {
        /// <summary>
        /// Connection string to the PostgreSQL database
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Name of the table to store messages
        /// </summary>
        public string TableName { get; set; } = "smtp_messages";

        /// <summary>
        /// Schema name for the table
        /// </summary>
        public string SchemaName { get; set; } = "public";

        /// <summary>
        /// Whether to automatically create the table if it doesn't exist
        /// </summary>
        public bool AutoCreateTable { get; set; } = true;

        /// <summary>
        /// Whether to use JSONB for storing headers (PostgreSQL specific)
        /// </summary>
        public bool UseJsonbForHeaders { get; set; } = true;

        /// <summary>
        /// Whether to enable table partitioning
        /// </summary>
        public bool EnablePartitioning { get; set; } = false;

        /// <summary>
        /// Partition interval for time-based partitioning
        /// </summary>
        public PartitionInterval PartitionInterval { get; set; } = PartitionInterval.Monthly;

        /// <summary>
        /// Whether to create indexes automatically
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
    /// Partition interval options for PostgreSQL
    /// </summary>
    public enum PartitionInterval
    {
        Daily,
        Weekly,
        Monthly,
        Yearly
    }
}