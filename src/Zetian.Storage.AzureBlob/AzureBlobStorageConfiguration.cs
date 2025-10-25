using System;
using Zetian.Storage.Common;

namespace Zetian.Storage.AzureBlob
{
    /// <summary>
    /// Configuration for Azure Blob Storage message storage
    /// </summary>
    public class AzureBlobStorageConfiguration : BaseStorageConfiguration
    {
        /// <summary>
        /// Connection string to Azure Storage Account
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Container name for storing messages
        /// </summary>
        public string ContainerName { get; set; } = "smtp-messages";

        /// <summary>
        /// Whether to use Azure AD authentication instead of connection string
        /// </summary>
        public bool UseAzureAdAuthentication { get; set; } = false;

        /// <summary>
        /// Storage account name (required when using Azure AD authentication)
        /// </summary>
        public string StorageAccountName { get; set; } = string.Empty;

        /// <summary>
        /// Whether to automatically create the container if it doesn't exist
        /// </summary>
        public bool AutoCreateContainer { get; set; } = true;

        /// <summary>
        /// Blob naming format
        /// </summary>
        public BlobNamingFormat NamingFormat { get; set; } = BlobNamingFormat.DateHierarchy;

        /// <summary>
        /// Access tier for blobs
        /// </summary>
        public BlobAccessTier AccessTier { get; set; } = BlobAccessTier.Hot;

        /// <summary>
        /// Whether to store message metadata as blob properties
        /// </summary>
        public bool StoreMetadataAsBlobProperties { get; set; } = true;

        /// <summary>
        /// Whether to enable soft delete
        /// </summary>
        public bool EnableSoftDelete { get; set; } = false;

        /// <summary>
        /// Soft delete retention period in days
        /// </summary>
        public int SoftDeleteRetentionDays { get; set; } = 7;

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
        /// Gets the blob name based on the naming format
        /// </summary>
        public string GetBlobName(string messageId, DateTime receivedDate)
        {
            return NamingFormat switch
            {
                BlobNamingFormat.Flat => $"{messageId}.eml",
                BlobNamingFormat.DateHierarchy => $"{receivedDate:yyyy/MM/dd}/{messageId}.eml",
                BlobNamingFormat.YearMonth => $"{receivedDate:yyyy-MM}/{messageId}.eml",
                BlobNamingFormat.DomainBased => $"messages/{messageId}.eml",
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
        /// Flat structure (all blobs in root)
        /// </summary>
        Flat,

        /// <summary>
        /// Hierarchical by date (yyyy/MM/dd/)
        /// </summary>
        DateHierarchy,

        /// <summary>
        /// Year and month folders (yyyy-MM/)
        /// </summary>
        YearMonth,

        /// <summary>
        /// Domain-based folders
        /// </summary>
        DomainBased
    }

    /// <summary>
    /// Azure Blob access tier options
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