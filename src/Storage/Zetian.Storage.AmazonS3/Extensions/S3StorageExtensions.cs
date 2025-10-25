using Microsoft.Extensions.Logging;
using System;
using Zetian.Server;
using Zetian.Storage.AmazonS3.Configuration;
using Zetian.Storage.AmazonS3.Storage;
using Zetian.Storage.Extensions;

namespace Zetian.Storage.AmazonS3.Extensions
{
    /// <summary>
    /// Extension methods for configuring Amazon S3 storage
    /// </summary>
    public static class S3StorageExtensions
    {
        /// <summary>
        /// Configures Amazon S3 as the message storage provider
        /// </summary>
        public static SmtpServerBuilder WithS3Storage(
            this SmtpServerBuilder builder,
            string accessKeyId,
            string secretAccessKey,
            string bucketName,
            Action<S3StorageConfiguration>? configure = null)
        {
            S3StorageConfiguration configuration = new()
            {
                AccessKeyId = accessKeyId,
                SecretAccessKey = secretAccessKey,
                BucketName = bucketName
            };

            configure?.Invoke(configuration);
            configuration.Validate();

            ILogger<S3MessageStore>? logger = builder.GetLogger<S3MessageStore>();
            S3MessageStore store = new(configuration, logger);

            return builder.MessageStore(store);
        }

        /// <summary>
        /// Configures S3-compatible storage as the message storage provider
        /// </summary>
        public static SmtpServerBuilder WithS3CompatibleStorage(
            this SmtpServerBuilder builder,
            string serviceUrl,
            string accessKeyId,
            string secretAccessKey,
            string bucketName,
            Action<S3StorageConfiguration>? configure = null)
        {
            S3StorageConfiguration configuration = new()
            {
                ServiceUrl = serviceUrl,
                AccessKeyId = accessKeyId,
                SecretAccessKey = secretAccessKey,
                BucketName = bucketName,
                ForcePathStyle = true
            };

            configure?.Invoke(configuration);
            configuration.Validate();

            ILogger<S3MessageStore>? logger = builder.GetLogger<S3MessageStore>();
            S3MessageStore store = new(configuration, logger);

            return builder.MessageStore(store);
        }
    }
}