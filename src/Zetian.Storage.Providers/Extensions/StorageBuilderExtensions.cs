using System;
using Microsoft.Extensions.Logging;
using Zetian.Server;
using Zetian.Storage.Providers.SqlServer;
using Zetian.Storage.Providers.PostgreSQL;
using Zetian.Storage.Providers.MongoDB;
using Zetian.Storage.Providers.Redis;
using Zetian.Storage.Providers.Azure;
using Zetian.Storage.Providers.S3;

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
            var configuration = new SqlServerStorageConfiguration
            {
                ConnectionString = connectionString
            };
            
            configure?.Invoke(configuration);
            configuration.Validate();

            var logger = builder.GetLogger<SqlServerMessageStore>();
            var store = new SqlServerMessageStore(configuration, logger);
            
            return builder.MessageStore(store);
        }

        /// <summary>
        /// Configures PostgreSQL as the message storage provider
        /// </summary>
        public static SmtpServerBuilder WithPostgreSqlStorage(this SmtpServerBuilder builder, string connectionString, Action<PostgreSqlStorageConfiguration>? configure = null)
        {
            var configuration = new PostgreSqlStorageConfiguration
            {
                ConnectionString = connectionString
            };
            
            configure?.Invoke(configuration);
            configuration.Validate();

            var logger = builder.GetLogger<PostgreSqlMessageStore>();
            var store = new PostgreSqlMessageStore(configuration, logger);
            
            return builder.MessageStore(store);
        }

        /// <summary>
        /// Configures MongoDB as the message storage provider
        /// </summary>
        public static SmtpServerBuilder WithMongoDbStorage(this SmtpServerBuilder builder, string connectionString, string databaseName, Action<MongoDbStorageConfiguration>? configure = null)
        {
            var configuration = new MongoDbStorageConfiguration
            {
                ConnectionString = connectionString,
                DatabaseName = databaseName
            };
            
            configure?.Invoke(configuration);
            configuration.Validate();

            var logger = builder.GetLogger<MongoDbMessageStore>();
            var store = new MongoDbMessageStore(configuration, logger);
            
            return builder.MessageStore(store);
        }

        /// <summary>
        /// Configures Redis as the message storage/cache provider
        /// </summary>
        public static SmtpServerBuilder WithRedisStorage(this SmtpServerBuilder builder, string connectionString, Action<RedisStorageConfiguration>? configure = null)
        {
            var configuration = new RedisStorageConfiguration
            {
                ConnectionString = connectionString
            };
            
            configure?.Invoke(configuration);
            configuration.Validate();

            var logger = builder.GetLogger<RedisMessageStore>();
            var store = new RedisMessageStore(configuration, logger);
            
            return builder.MessageStore(store);
        }

        /// <summary>
        /// Configures Azure Blob Storage as the message storage provider
        /// </summary>
        public static SmtpServerBuilder WithAzureBlobStorage(this SmtpServerBuilder builder, string connectionString, Action<AzureBlobStorageConfiguration>? configure = null)
        {
            var configuration = new AzureBlobStorageConfiguration
            {
                ConnectionString = connectionString
            };
            
            configure?.Invoke(configuration);
            configuration.Validate();

            var logger = builder.GetLogger<AzureBlobMessageStore>();
            var store = new AzureBlobMessageStore(configuration, logger);
            
            return builder.MessageStore(store);
        }

        /// <summary>
        /// Configures Azure Blob Storage with Azure AD authentication
        /// </summary>
        public static SmtpServerBuilder WithAzureBlobStorageAD(this SmtpServerBuilder builder, string storageAccountName, Action<AzureBlobStorageConfiguration>? configure = null)
        {
            var configuration = new AzureBlobStorageConfiguration
            {
                UseAzureAdAuthentication = true,
                StorageAccountName = storageAccountName
            };
            
            configure?.Invoke(configuration);
            configuration.Validate();

            var logger = builder.GetLogger<AzureBlobMessageStore>();
            var store = new AzureBlobMessageStore(configuration, logger);
            
            return builder.MessageStore(store);
        }

        /// <summary>
        /// Configures Amazon S3 as the message storage provider
        /// </summary>
        public static SmtpServerBuilder WithS3Storage(this SmtpServerBuilder builder, string accessKeyId, string secretAccessKey, string bucketName, Action<S3StorageConfiguration>? configure = null)
        {
            var configuration = new S3StorageConfiguration
            {
                AccessKeyId = accessKeyId,
                SecretAccessKey = secretAccessKey,
                BucketName = bucketName
            };
            
            configure?.Invoke(configuration);
            configuration.Validate();

            var logger = builder.GetLogger<S3MessageStore>();
            var store = new S3MessageStore(configuration, logger);
            
            return builder.MessageStore(store);
        }

        /// <summary>
        /// Configures S3-compatible storage as the message storage provider
        /// </summary>
        public static SmtpServerBuilder WithS3CompatibleStorage(this SmtpServerBuilder builder, string serviceUrl, string accessKeyId, string secretAccessKey, string bucketName, Action<S3StorageConfiguration>? configure = null)
        {
            var configuration = new S3StorageConfiguration
            {
                ServiceUrl = serviceUrl,
                AccessKeyId = accessKeyId,
                SecretAccessKey = secretAccessKey,
                BucketName = bucketName,
                ForcePathStyle = true
            };
            
            configure?.Invoke(configuration);
            configuration.Validate();

            var logger = builder.GetLogger<S3MessageStore>();
            var store = new S3MessageStore(configuration, logger);
            
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
