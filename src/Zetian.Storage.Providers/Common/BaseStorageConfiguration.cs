using System;

namespace Zetian.Storage.Providers.Common
{
    /// <summary>
    /// Base configuration for all storage providers
    /// </summary>
    public abstract class BaseStorageConfiguration
    {
        /// <summary>
        /// Gets or sets whether to enable automatic retry on failure
        /// </summary>
        public bool EnableRetry { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of retry attempts
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Gets or sets the delay between retry attempts in milliseconds
        /// </summary>
        public int RetryDelayMs { get; set; } = 1000;

        /// <summary>
        /// Gets or sets the connection timeout in seconds
        /// </summary>
        public int ConnectionTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Gets or sets whether to log errors to the console
        /// </summary>
        public bool LogErrors { get; set; } = true;

        /// <summary>
        /// Validates the configuration
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