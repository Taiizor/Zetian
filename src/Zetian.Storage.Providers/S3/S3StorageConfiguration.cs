using System;
using Zetian.Storage.Providers.Common;

namespace Zetian.Storage.Providers.S3
{
    /// <summary>
    /// Configuration for Amazon S3 message storage
    /// </summary>
    public class S3StorageConfiguration : BaseStorageConfiguration
    {
        /// <summary>
        /// Gets or sets the AWS access key ID
        /// </summary>
        public string? AccessKeyId { get; set; }

        /// <summary>
        /// Gets or sets the AWS secret access key
        /// </summary>
        public string? SecretAccessKey { get; set; }

        /// <summary>
        /// Gets or sets the S3 bucket name
        /// </summary>
        public string BucketName { get; set; } = "smtp-messages";

        /// <summary>
        /// Gets or sets the AWS region
        /// </summary>
        public string Region { get; set; } = "us-east-1";

        /// <summary>
        /// Gets or sets whether to create the bucket if it doesn't exist
        /// </summary>
        public bool AutoCreateBucket { get; set; } = true;

        /// <summary>
        /// Gets or sets the object key prefix
        /// </summary>
        public string KeyPrefix { get; set; } = "messages/";

        /// <summary>
        /// Gets or sets the object naming format
        /// </summary>
        public S3NamingFormat NamingFormat { get; set; } = S3NamingFormat.DateHierarchy;

        /// <summary>
        /// Gets or sets the storage class for new objects
        /// </summary>
        public S3StorageClass StorageClass { get; set; } = S3StorageClass.Standard;

        /// <summary>
        /// Gets or sets whether to enable server-side encryption
        /// </summary>
        public bool EnableServerSideEncryption { get; set; } = true;

        /// <summary>
        /// Gets or sets the KMS key ID for encryption (optional)
        /// </summary>
        public string? KmsKeyId { get; set; }

        /// <summary>
        /// Gets or sets whether to compress message body
        /// </summary>
        public bool CompressMessageBody { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum message size in MB (0 = unlimited)
        /// </summary>
        public int MaxMessageSizeMB { get; set; } = 100;

        /// <summary>
        /// Gets or sets whether to enable versioning
        /// </summary>
        public bool EnableVersioning { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to enable lifecycle rules
        /// </summary>
        public bool EnableLifecycleRules { get; set; } = false;

        /// <summary>
        /// Gets or sets the number of days before transitioning to IA storage
        /// </summary>
        public int TransitionToIADays { get; set; } = 30;

        /// <summary>
        /// Gets or sets the number of days before transitioning to Glacier
        /// </summary>
        public int TransitionToGlacierDays { get; set; } = 90;

        /// <summary>
        /// Gets or sets the number of days before expiration
        /// </summary>
        public int ExpirationDays { get; set; } = 365;

        /// <summary>
        /// Gets or sets whether to use transfer acceleration
        /// </summary>
        public bool UseTransferAcceleration { get; set; } = false;

        /// <summary>
        /// Gets or sets the custom S3 endpoint (for S3-compatible services)
        /// </summary>
        public string? ServiceUrl { get; set; }

        /// <summary>
        /// Gets or sets whether to use path-style addressing
        /// </summary>
        public bool ForcePathStyle { get; set; } = false;

        /// <summary>
        /// Validates the configuration
        /// </summary>
        public override void Validate()
        {
            base.Validate();

            if (string.IsNullOrWhiteSpace(ServiceUrl))
            {
                // AWS S3 - need credentials unless using IAM roles
                if (string.IsNullOrWhiteSpace(AccessKeyId) && !IsRunningOnEC2())
                {
                    throw new ArgumentException("AccessKeyId is required when not using IAM roles");
                }

                if (string.IsNullOrWhiteSpace(SecretAccessKey) && !IsRunningOnEC2())
                {
                    throw new ArgumentException("SecretAccessKey is required when not using IAM roles");
                }
            }

            if (string.IsNullOrWhiteSpace(BucketName))
                throw new ArgumentException("BucketName is required");

            if (!IsValidBucketName(BucketName))
                throw new ArgumentException("BucketName must be 3-63 characters, lowercase letters, numbers, periods, and hyphens only");

            if (string.IsNullOrWhiteSpace(Region))
                throw new ArgumentException("Region is required");

            if (MaxMessageSizeMB < 0)
                throw new ArgumentException("MaxMessageSizeMB must be non-negative");

            if (TransitionToIADays < 0)
                throw new ArgumentException("TransitionToIADays must be non-negative");

            if (TransitionToGlacierDays < 0)
                throw new ArgumentException("TransitionToGlacierDays must be non-negative");

            if (ExpirationDays < 0)
                throw new ArgumentException("ExpirationDays must be non-negative");
        }

        /// <summary>
        /// Gets the S3 object key for a message
        /// </summary>
        public string GetObjectKey(string messageId, DateTime receivedDate)
        {
            var baseName = NamingFormat switch
            {
                S3NamingFormat.Flat => $"{messageId}.eml",
                S3NamingFormat.DateHierarchy => $"{receivedDate:yyyy/MM/dd}/{messageId}.eml",
                S3NamingFormat.YearMonth => $"{receivedDate:yyyy-MM}/{messageId}.eml",
                S3NamingFormat.SessionPrefix => $"{messageId[..8]}/{messageId}.eml",
                S3NamingFormat.HourlyPartition => $"{receivedDate:yyyy/MM/dd/HH}/{messageId}.eml",
                _ => $"{messageId}.eml"
            };

            return $"{KeyPrefix}{baseName}";
        }

        private bool IsValidBucketName(string name)
        {
            if (name.Length < 3 || name.Length > 63)
                return false;

            foreach (char c in name)
            {
                if (!char.IsLetterOrDigit(c) && c != '-' && c != '.')
                    return false;

                if (char.IsUpper(c))
                    return false;
            }

            return !name.StartsWith("-") && !name.EndsWith("-") && 
                   !name.StartsWith(".") && !name.EndsWith(".") && 
                   !name.Contains("..") && !name.Contains(".-") && !name.Contains("-.");
        }

        private bool IsRunningOnEC2()
        {
            // Simple check for EC2 instance metadata service
            try
            {
                var ec2InstanceId = Environment.GetEnvironmentVariable("EC2_INSTANCE_ID");
                return !string.IsNullOrEmpty(ec2InstanceId);
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// S3 object naming format options
    /// </summary>
    public enum S3NamingFormat
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
        SessionPrefix,

        /// <summary>
        /// Hourly partitions: yyyy/MM/dd/HH/messageId.eml
        /// </summary>
        HourlyPartition
    }

    /// <summary>
    /// S3 storage classes
    /// </summary>
    public enum S3StorageClass
    {
        /// <summary>
        /// Standard storage for frequently accessed data
        /// </summary>
        Standard,

        /// <summary>
        /// Standard-IA for infrequently accessed data
        /// </summary>
        StandardIA,

        /// <summary>
        /// Intelligent-Tiering for automatic cost optimization
        /// </summary>
        IntelligentTiering,

        /// <summary>
        /// One Zone-IA for infrequently accessed data in a single AZ
        /// </summary>
        OneZoneIA,

        /// <summary>
        /// Glacier Instant Retrieval
        /// </summary>
        GlacierInstantRetrieval,

        /// <summary>
        /// Glacier Flexible Retrieval
        /// </summary>
        GlacierFlexible,

        /// <summary>
        /// Glacier Deep Archive for long-term retention
        /// </summary>
        GlacierDeepArchive
    }
}
