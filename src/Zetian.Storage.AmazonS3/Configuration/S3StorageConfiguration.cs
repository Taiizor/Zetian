using Amazon.S3;
using System;
using Zetian.Storage.Common;

namespace Zetian.Storage.AmazonS3
{
    /// <summary>
    /// Configuration for Amazon S3 message storage
    /// </summary>
    public class S3StorageConfiguration : BaseStorageConfiguration
    {
        /// <summary>
        /// AWS Access Key ID
        /// </summary>
        public string AccessKeyId { get; set; } = string.Empty;

        /// <summary>
        /// AWS Secret Access Key
        /// </summary>
        public string SecretAccessKey { get; set; } = string.Empty;

        /// <summary>
        /// S3 bucket name
        /// </summary>
        public string BucketName { get; set; } = string.Empty;

        /// <summary>
        /// AWS Region
        /// </summary>
        public string Region { get; set; } = "us-east-1";

        /// <summary>
        /// Service URL (for S3-compatible services)
        /// </summary>
        public string? ServiceUrl { get; set; }

        /// <summary>
        /// Whether to use path-style addressing (required for some S3-compatible services)
        /// </summary>
        public bool ForcePathStyle { get; set; } = false;

        /// <summary>
        /// Key prefix for all objects
        /// </summary>
        public string KeyPrefix { get; set; } = "smtp/";

        /// <summary>
        /// Object naming format
        /// </summary>
        public S3NamingFormat NamingFormat { get; set; } = S3NamingFormat.DateHierarchy;

        /// <summary>
        /// Storage class for objects
        /// </summary>
        public S3StorageClass StorageClass { get; set; } = S3StorageClass.Standard;

        /// <summary>
        /// Whether to enable server-side encryption
        /// </summary>
        public bool EnableServerSideEncryption { get; set; } = true;

        /// <summary>
        /// KMS Key ID for encryption (optional)
        /// </summary>
        public string? KmsKeyId { get; set; }

        /// <summary>
        /// Whether to automatically create the bucket if it doesn't exist
        /// </summary>
        public bool AutoCreateBucket { get; set; } = true;

        /// <summary>
        /// Whether to enable versioning
        /// </summary>
        public bool EnableVersioning { get; set; } = false;

        /// <summary>
        /// Whether to enable transfer acceleration
        /// </summary>
        public bool UseTransferAcceleration { get; set; } = false;

        /// <summary>
        /// Whether to enable lifecycle rules
        /// </summary>
        public bool EnableLifecycleRules { get; set; } = false;

        /// <summary>
        /// Days before transitioning to Infrequent Access
        /// </summary>
        public int TransitionToIADays { get; set; } = 30;

        /// <summary>
        /// Days before transitioning to Glacier
        /// </summary>
        public int TransitionToGlacierDays { get; set; } = 90;

        /// <summary>
        /// Days before expiration
        /// </summary>
        public int ExpirationDays { get; set; } = 365;

        /// <summary>
        /// Validates the configuration
        /// </summary>
        public override void Validate()
        {
            base.Validate();

            // For S3-compatible services with ServiceUrl, credentials might be optional
            if (string.IsNullOrWhiteSpace(ServiceUrl))
            {
                // AWS S3 - check for credentials or IAM role
                if (string.IsNullOrWhiteSpace(AccessKeyId) || string.IsNullOrWhiteSpace(SecretAccessKey))
                {
                    // Check if running on EC2 with IAM role
                    if (!IsRunningOnEc2())
                    {
                        throw new ArgumentException("AccessKeyId and SecretAccessKey are required when not using IAM roles");
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(BucketName))
            {
                throw new ArgumentException("BucketName is required");
            }

            if (!IsValidBucketName(BucketName))
            {
                throw new ArgumentException("BucketName must be 3-63 characters, lowercase letters, numbers, periods, and hyphens only");
            }

            if (string.IsNullOrWhiteSpace(Region))
            {
                throw new ArgumentException("Region is required");
            }

            if (MaxMessageSizeMB < 0)
            {
                throw new ArgumentException("MaxMessageSizeMB must be non-negative");
            }

            if (TransitionToIADays < 0)
            {
                throw new ArgumentException("TransitionToIADays must be non-negative");
            }

            if (TransitionToGlacierDays < 0)
            {
                throw new ArgumentException("TransitionToGlacierDays must be non-negative");
            }

            if (ExpirationDays < 0)
            {
                throw new ArgumentException("ExpirationDays must be non-negative");
            }
        }

        /// <summary>
        /// Gets the object key based on the naming format
        /// </summary>
        public string GetObjectKey(string messageId, DateTime receivedDate)
        {
            string baseName = NamingFormat switch
            {
                S3NamingFormat.Flat => $"{messageId}.eml",
                S3NamingFormat.DateHierarchy => $"{receivedDate:yyyy/MM/dd}/{messageId}.eml",
                S3NamingFormat.YearMonth => $"{receivedDate:yyyy-MM}/{messageId}.eml",
                S3NamingFormat.HourlyPartition => $"{receivedDate:yyyy/MM/dd/HH}/{messageId}.eml",
                _ => $"{messageId}.eml"
            };

            return string.IsNullOrEmpty(KeyPrefix) ? baseName : $"{KeyPrefix}{baseName}";
        }

        private bool IsValidBucketName(string name)
        {
            if (name.Length is < 3 or > 63)
            {
                return false;
            }

            foreach (char c in name)
            {
                if (!char.IsLetterOrDigit(c) && c != '-' && c != '.')
                {
                    return false;
                }

                if (char.IsUpper(c))
                {
                    return false;
                }
            }

            return !name.StartsWith("-") && !name.EndsWith("-") &&
                   !name.StartsWith(".") && !name.EndsWith(".") &&
                   !name.Contains("..") && !name.Contains(".-") && !name.Contains("-.");
        }

        private bool IsRunningOnEc2()
        {
            // Simple check for EC2 instance metadata service
            try
            {
                string? ec2InstanceId = Environment.GetEnvironmentVariable("EC2_INSTANCE_ID");
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
        /// Flat structure (all objects in prefix)
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
        /// Hourly partitions for high volume (yyyy/MM/dd/HH/)
        /// </summary>
        HourlyPartition
    }
}