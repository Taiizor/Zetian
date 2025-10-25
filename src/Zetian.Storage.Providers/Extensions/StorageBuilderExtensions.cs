using Microsoft.Extensions.Logging;
using System;
using Zetian.Server;
using Zetian.Storage.Providers.Azure;
using Zetian.Storage.Providers.MongoDB;
using Zetian.Storage.Providers.PostgreSQL;
using Zetian.Storage.Providers.Redis;
using Zetian.Storage.Providers.S3;
using Zetian.Storage.Providers.SqlServer;

namespace Zetian.Storage.Providers.Extensions
{
    /// <summary>
    /// Extension methods for SmtpServerBuilder to easily configure storage providers
    /// </summary>
    public static class StorageBuilderExtensions
    {
        /// <summary>
        /// Configures SQL Server as the message storage provider
        /// </summary>
        public static SmtpServerBuilder WithSqlServerStorage(this SmtpServerBuilder builder, string connectionString, Action<SqlServerStorageConfiguration>? configure = null)
        {
            SqlServerStorageConfiguration configuration = new()
            {
                ConnectionString = connectionString
            };

            configure?.Invoke(configuration);
            configuration.Validate();

            ILogger<SqlServerMessageStore>? logger = builder.GetLogger<SqlServerMessageStore>();
            SqlServerMessageStore store = new(configuration, logger);

            return builder.MessageStore(store);
        }

        /// <summary>
        /// Configures PostgreSQL as the message storage provider
        /// </summary>
        public static SmtpServerBuilder WithPostgreSqlStorage(this SmtpServerBuilder builder, string connectionString, Action<PostgreSqlStorageConfiguration>? configure = null)
        {
            PostgreSqlStorageConfiguration configuration = new()
            {
                ConnectionString = connectionString
            };

            configure?.Invoke(configuration);
            configuration.Validate();

            ILogger<PostgreSqlMessageStore>? logger = builder.GetLogger<PostgreSqlMessageStore>();
            PostgreSqlMessageStore store = new(configuration, logger);

            return builder.MessageStore(store);
        }

        /// <summary>
        /// Configures MongoDB as the message storage provider
        /// </summary>
        public static SmtpServerBuilder WithMongoDbStorage(this SmtpServerBuilder builder, string connectionString, string databaseName, Action<MongoDbStorageConfiguration>? configure = null)
        {
            MongoDbStorageConfiguration configuration = new()
            {
                ConnectionString = connectionString,
                DatabaseName = databaseName
            };

            configure?.Invoke(configuration);
            configuration.Validate();

            ILogger<MongoDbMessageStore>? logger = builder.GetLogger<MongoDbMessageStore>();
            MongoDbMessageStore store = new(configuration, logger);

            return builder.MessageStore(store);
        }

        /// <summary>
        /// Configures Redis as the message storage/cache provider
        /// </summary>
        public static SmtpServerBuilder WithRedisStorage(this SmtpServerBuilder builder, string connectionString, Action<RedisStorageConfiguration>? configure = null)
        {
            RedisStorageConfiguration configuration = new()
            {
                ConnectionString = connectionString
            };

            configure?.Invoke(configuration);
            configuration.Validate();

            ILogger<RedisMessageStore>? logger = builder.GetLogger<RedisMessageStore>();
            RedisMessageStore store = new(configuration, logger);

            return builder.MessageStore(store);
        }

        /// <summary>
        /// Configures Azure Blob Storage as the message storage provider
        /// </summary>
        public static SmtpServerBuilder WithAzureBlobStorage(this SmtpServerBuilder builder, string connectionString, Action<AzureBlobStorageConfiguration>? configure = null)
        {
            AzureBlobStorageConfiguration configuration = new()
            {
                ConnectionString = connectionString
            };

            configure?.Invoke(configuration);
            configuration.Validate();

            ILogger<AzureBlobMessageStore>? logger = builder.GetLogger<AzureBlobMessageStore>();
            AzureBlobMessageStore store = new(configuration, logger);

            return builder.MessageStore(store);
        }

        /// <summary>
        /// Configures Azure Blob Storage with Azure AD authentication
        /// </summary>
        public static SmtpServerBuilder WithAzureBlobStorageAD(this SmtpServerBuilder builder, string storageAccountName, Action<AzureBlobStorageConfiguration>? configure = null)
        {
            AzureBlobStorageConfiguration configuration = new()
            {
                UseAzureAdAuthentication = true,
                StorageAccountName = storageAccountName
            };

            configure?.Invoke(configuration);
            configuration.Validate();

            ILogger<AzureBlobMessageStore>? logger = builder.GetLogger<AzureBlobMessageStore>();
            AzureBlobMessageStore store = new(configuration, logger);

            return builder.MessageStore(store);
        }

        /// <summary>
        /// Configures Amazon S3 as the message storage provider
        /// </summary>
        public static SmtpServerBuilder WithS3Storage(this SmtpServerBuilder builder, string accessKeyId, string secretAccessKey, string bucketName, Action<S3StorageConfiguration>? configure = null)
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
        public static SmtpServerBuilder WithS3CompatibleStorage(this SmtpServerBuilder builder, string serviceUrl, string accessKeyId, string secretAccessKey, string bucketName, Action<S3StorageConfiguration>? configure = null)
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

        /// <summary>
        /// Helper method to get logger from builder (would need to be implemented in SmtpServerBuilder)
        /// </summary>
        private static ILogger<T>? GetLogger<T>(this SmtpServerBuilder builder)
        {
            // This would need to be implemented in the main Zetian library
            // For now, return null (no logging)
            return null;
        }
    }
}
