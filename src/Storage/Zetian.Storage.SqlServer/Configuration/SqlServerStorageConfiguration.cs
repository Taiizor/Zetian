using System;
using Zetian.Storage.Configuration;

namespace Zetian.Storage.SqlServer.Configuration
{
    /// <summary>
    /// Configuration for SQL Server message storage
    /// </summary>
    public class SqlServerStorageConfiguration : BaseStorageConfiguration
    {
        /// <summary>
        /// Gets or sets the connection string to the SQL Server database
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the table to store messages
        /// </summary>
        public string TableName { get; set; } = "SmtpMessages";

        /// <summary>
        /// Gets or sets the schema name for the table
        /// </summary>
        public string SchemaName { get; set; } = "dbo";

        /// <summary>
        /// Gets or sets whether to automatically create the table if it doesn't exist
        /// </summary>
        public bool AutoCreateTable { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to store attachments separately
        /// </summary>
        public bool StoreAttachmentsSeparately { get; set; } = false;

        /// <summary>
        /// Gets or sets the attachments table name
        /// </summary>
        public string AttachmentsTableName { get; set; } = "SmtpAttachments";

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
            if (!IsValidSqlIdentifier(TableName))
            {
                throw new ArgumentException($"Invalid table name: {TableName}. Only alphanumeric characters and underscores are allowed.");
            }

            if (!IsValidSqlIdentifier(SchemaName))
            {
                throw new ArgumentException($"Invalid schema name: {SchemaName}. Only alphanumeric characters and underscores are allowed.");
            }

            if (!IsValidSqlIdentifier(AttachmentsTableName))
            {
                throw new ArgumentException($"Invalid attachments table name: {AttachmentsTableName}. Only alphanumeric characters and underscores are allowed.");
            }

            if (MaxMessageSizeMB < 0)
            {
                throw new ArgumentException("MaxMessageSizeMB must be non-negative");
            }
        }

        /// <summary>
        /// Validates SQL identifier to prevent SQL injection
        /// </summary>
        private static bool IsValidSqlIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return false;
            }

            // Only allow alphanumeric characters and underscores
            // First character cannot be a number
            return System.Text.RegularExpressions.Regex.IsMatch(identifier, @"^[a-zA-Z_][a-zA-Z0-9_]*$");
        }

        /// <summary>
        /// Escapes SQL Server identifier safely
        /// </summary>
        private static string EscapeSqlServerIdentifier(string identifier)
        {
            // Replace any ] with ]] to properly escape
            return $"[{identifier.Replace("]", "]]")}]";
        }

        /// <summary>
        /// Gets the fully qualified table name with proper escaping
        /// </summary>
        public string GetFullTableName()
        {
            return $"{EscapeSqlServerIdentifier(SchemaName)}.{EscapeSqlServerIdentifier(TableName)}";
        }

        /// <summary>
        /// Gets the fully qualified attachments table name with proper escaping
        /// </summary>
        public string GetFullAttachmentsTableName()
        {
            return $"{EscapeSqlServerIdentifier(SchemaName)}.{EscapeSqlServerIdentifier(AttachmentsTableName)}";
        }
    }
}