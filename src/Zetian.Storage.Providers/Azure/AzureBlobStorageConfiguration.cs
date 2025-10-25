using System;
using Zetian.Storage.Providers.Common;

namespace Zetian.Storage.Providers.Azure
{
    /// <summary>
    /// Configuration for Azure Blob Storage message storage
    /// </summary>
    public class AzureBlobStorageConfiguration : BaseStorageConfiguration
    {
        /// <summary>
        /// Gets or sets the Azure Storage connection string
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the container name
        /// </summary>
        public string ContainerName { get; set; } = "smtp-messages";

        /// <summary>
        /// Gets or sets whether to create the container if it doesn't exist
        /// </summary>
        public bool AutoCreateContainer { get; set; } = true;

        /// <summary>
        /// Gets or sets the blob naming format
        /// </summary>
        public BlobNamingFormat NamingFormat { get; set; } = BlobNamingFormat.DateHierarchy;

        /// <summary>
        /// Gets or sets the blob tier for new blobs
        /// </summary>
        public BlobAccessTier AccessTier { get; set; } = BlobAccessTier.Hot;

        /// <summary>
        /// Gets or sets whether to compress message body
        /// </summary>
        public bool CompressMessageBody { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum message size in MB (0 = unlimited)
        /// </summary>
        public int MaxMessageSizeMB { get; set; } = 100;

        /// <summary>
        /// Gets or sets whether to store metadata as blob metadata
        /// </summary>
        public bool StoreMetadataAsBlobProperties { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable soft delete
        /// </summary>
        public bool EnableSoftDelete { get; set; } = true;

        /// <summary>
        /// Gets or sets the soft delete retention in days
        /// </summary>
        public int SoftDeleteRetentionDays { get; set; } = 7;

        /// <summary>
        /// Gets or sets whether to use Azure AD authentication
        /// </summary>
        public bool UseAzureAdAuthentication { get; set; } = false;

        /// <summary>
        /// Gets or sets the storage account name (for Azure AD auth)
        /// </summary>
        public string? StorageAccountName { get; set; }

        /// <summary>
        /// Validates the configuration
        /// </summary>
        public override void Validate()
        {
            base.Validate();

            if (!UseAzureAdAuthentication && string.IsNullOrWhiteSpace(ConnectionString))
            {
                throw new ArgumentException("ConnectionString is required when not using Azure AD authentication");
            }

            if (UseAzureAdAuthentication && string.IsNullOrWhiteSpace(StorageAccountName))
            {
                throw new ArgumentException("StorageAccountName is required when using Azure AD authentication");
            }

            if (string.IsNullOrWhiteSpace(ContainerName))
            {
                throw new ArgumentException("ContainerName is required");
            }

            if (!IsValidContainerName(ContainerName))
            {
                throw new ArgumentException("ContainerName must be 3-63 characters, lowercase letters, numbers, and hyphens only");
            }

            if (MaxMessageSizeMB < 0)
            {
                throw new ArgumentException("MaxMessageSizeMB must be non-negative");
            }

            if (SoftDeleteRetentionDays <= 0)
            {
                throw new ArgumentException("SoftDeleteRetentionDays must be positive");
            }
        }

        /// <summary>
        /// Gets the blob name for a message
        /// </summary>
        public string GetBlobName(string messageId, DateTime receivedDate)
        {
            return NamingFormat switch
            {
                BlobNamingFormat.Flat => $"{messageId}.eml",
                BlobNamingFormat.DateHierarchy => $"{receivedDate:yyyy/MM/dd}/{messageId}.eml",
                BlobNamingFormat.YearMonth => $"{receivedDate:yyyy-MM}/{messageId}.eml",
                BlobNamingFormat.SessionPrefix => $"{messageId[..8]}/{messageId}.eml",
                _ => $"{messageId}.eml"
            };
        }

        private bool IsValidContainerName(string name)
        {
            if (name.Length is < 3 or > 63)
            {
                return false;
            }

            foreach (char c in name)
            {
                if (!char.IsLetterOrDigit(c) && c != '-')
                {
                    return false;
                }

                if (char.IsUpper(c))
                {
                    return false;
                }
            }

            return !name.StartsWith("-") && !name.EndsWith("-") && !name.Contains("--");
        }
    }

    /// <summary>
    /// Blob naming format options
    /// </summary>
    public enum BlobNamingFormat
    {
        /// <summary>
        /// Flat structure: messageId.eml
        /// </summary>
        Flat,

        /// <summary>
        /// Date hierarchy: yyyy/MM/dd/messageId.eml
        /// </summary>
        DateHierarchy,

        /// <summary>
        /// Year-Month folders: yyyy-MM/messageId.eml
        /// </summary>
        YearMonth,

        /// <summary>
        /// Session prefix folders: sessionPrefix/messageId.eml
        /// </summary>
        SessionPrefix
    }

    /// <summary>
    /// Azure Blob access tiers
    /// </summary>
    public enum BlobAccessTier
    {
        /// <summary>
        /// Hot tier for frequently accessed data
        /// </summary>
        Hot,

        /// <summary>
        /// Cool tier for infrequently accessed data
        /// </summary>
        Cool,

        /// <summary>
        /// Archive tier for rarely accessed data
        /// </summary>
        Archive
    }
}