using System;
using System.Text.RegularExpressions;
using Zetian.Storage.Configuration;
using Zetian.Storage.PostgreSQL.Enums;

namespace Zetian.Storage.PostgreSQL.Configuration
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

            // Validate table/schema names for SQL injection prevention
            if (!IsValidPostgreSqlIdentifier(TableName))
            {
                throw new ArgumentException($"Invalid table name: {TableName}. Only lowercase letters, numbers and underscores are allowed.");
            }

            if (!IsValidPostgreSqlIdentifier(SchemaName))
            {
                throw new ArgumentException($"Invalid schema name: {SchemaName}. Only lowercase letters, numbers and underscores are allowed.");
            }

            if (MaxMessageSizeMB < 0)
            {
                throw new ArgumentException("MaxMessageSizeMB must be non-negative");
            }
        }

        /// <summary>
        /// Validates PostgreSQL identifier to prevent SQL injection
        /// </summary>
        private static bool IsValidPostgreSqlIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return false;
            }

            // PostgreSQL prefers lowercase identifiers
            // Only allow lowercase letters, numbers and underscores
            // First character cannot be a number
            return Regex.IsMatch(identifier, @"^[a-z_][a-z0-9_]*$");
        }

        /// <summary>
        /// Escapes PostgreSQL identifier safely
        /// </summary>
        private static string EscapePostgreSqlIdentifier(string identifier)
        {
            // Replace any " with "" to properly escape
            return $"\"{identifier.Replace("\"", "\"\"")}\"";
        }

        /// <summary>
        /// Returns the escaped table name suitable for use in PostgreSQL queries.
        /// </summary>
        /// <remarks>Use this method to obtain a table name that is safely formatted for inclusion in SQL
        /// statements targeting PostgreSQL. This helps prevent issues with reserved keywords or special characters in
        /// table names.</remarks>
        /// <returns>A string containing the table name with PostgreSQL identifier escaping applied.</returns>
        public string GetTableName()
        {
            return EscapePostgreSqlIdentifier(TableName);
        }

        /// <summary>
        /// Returns the schema name formatted as a valid PostgreSQL identifier.
        /// </summary>
        /// <returns>A string containing the escaped schema name suitable for use in PostgreSQL queries.</returns>
        public string GetSchemaName()
        {
            return EscapePostgreSqlIdentifier(SchemaName);
        }

        /// <summary>
        /// Gets the fully qualified table name with proper escaping
        /// </summary>
        public string GetFullTableName()
        {
            return $"{GetSchemaName()}.{GetTableName()}";
        }
    }
}