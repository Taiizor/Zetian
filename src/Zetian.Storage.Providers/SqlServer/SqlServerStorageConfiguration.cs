using System;
using Zetian.Storage.Providers.Common;

namespace Zetian.Storage.Providers.SqlServer
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
        /// Gets or sets whether to store message body as compressed data
        /// </summary>
        public bool CompressMessageBody { get; set; } = false;

        /// <summary>
        /// Gets or sets the maximum message size in MB (0 = unlimited)
        /// </summary>
        public int MaxMessageSizeMB { get; set; } = 25;

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
            return $"[{SchemaName}].[{TableName}]";
        }

        /// <summary>
        /// Gets the fully qualified attachments table name
        /// </summary>
        public string GetFullAttachmentsTableName()
        {
            return $"[{SchemaName}].[{AttachmentsTableName}]";
        }
    }
}
