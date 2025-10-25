using System;

namespace Zetian.Storage.Common
{
    /// <summary>
    /// Base configuration for all storage providers
    /// </summary>
    public abstract class BaseStorageConfiguration
    {
        /// <summary>
        /// Maximum message size in MB (0 = unlimited)
        /// </summary>
        public double MaxMessageSizeMB { get; set; } = 100;

        /// <summary>
        /// Whether to compress message body before storing
        /// </summary>
        public bool CompressMessageBody { get; set; } = false;

        /// <summary>
        /// Enable automatic retry on failure
        /// </summary>
        public bool EnableRetry { get; set; } = true;

        /// <summary>
        /// Maximum retry attempts
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Delay between retries in milliseconds
        /// </summary>
        public int RetryDelayMs { get; set; } = 1000;

        /// <summary>
        /// Connection timeout in seconds
        /// </summary>
        public int ConnectionTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Whether to log errors
        /// </summary>
        public bool LogErrors { get; set; } = true;

        /// <summary>
        /// Validate configuration
        /// </summary>
        public virtual void Validate()
        {
            if (MaxRetryAttempts < 0)
            {
                throw new ArgumentException("MaxRetryAttempts must be non-negative");
            }

            if (RetryDelayMs < 0)
            {
                throw new ArgumentException("RetryDelayMs must be non-negative");
            }

            if (ConnectionTimeoutSeconds <= 0)
            {
                throw new ArgumentException("ConnectionTimeoutSeconds must be positive");
            }
        }
    }
}