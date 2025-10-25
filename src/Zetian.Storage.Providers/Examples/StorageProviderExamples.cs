using Amazon.S3;
using System;
using System.Threading.Tasks;
using Zetian.Server;
using Zetian.Storage.Providers.Azure;
using Zetian.Storage.Providers.Extensions;
using Zetian.Storage.Providers.PostgreSQL;
using Zetian.Storage.Providers.S3;

namespace Zetian.Storage.Providers.Examples
{
    /// <summary>
    /// Example usage of various storage providers with Zetian SMTP Server
    /// </summary>
    public class StorageProviderExamples
    {
        /// <summary>
        /// Example: SQL Server with compression and separate attachments
        /// </summary>
        public static async Task SqlServerExample()
        {
            SmtpServer server = new SmtpServerBuilder()
                .Port(25)
                .ServerName("SMTP Server with SQL Storage")
                .WithSqlServerStorage(
                    "Server=localhost;Database=SmtpDb;Trusted_Connection=true;",
                    config =>
                    {
                        config.SchemaName = "mail";
                        config.TableName = "Messages";
                        config.CompressMessageBody = true;
                        config.MaxMessageSizeMB = 50;
                        config.StoreAttachmentsSeparately = true;
                        config.EnableRetry = true;
                        config.MaxRetryAttempts = 3;
                    })
                .Build();

            server.MessageReceived += (sender, e) =>
            {
                Console.WriteLine($"Message {e.Message.Id} stored in SQL Server");
            };

            await server.StartAsync();
            Console.WriteLine("SMTP Server with SQL Server storage started on port 25");
        }

        /// <summary>
        /// Example: PostgreSQL with partitioning and JSONB headers
        /// </summary>
        public static async Task PostgreSqlExample()
        {
            SmtpServer server = new SmtpServerBuilder()
                .Port(587)
                .RequireAuthentication()
                .WithPostgreSqlStorage(
                    "Host=localhost;Database=smtp_db;Username=postgres;Password=secret",
                    config =>
                    {
                        config.UseJsonbForHeaders = true;
                        config.EnablePartitioning = true;
                        config.PartitionInterval = PartitionInterval.Monthly;
                        config.CreateIndexes = true;
                        config.CompressMessageBody = false;
                        config.MaxMessageSizeMB = 100;
                    })
                .Build();

            await server.StartAsync();
            Console.WriteLine("SMTP Server with PostgreSQL storage started on port 587");
        }

        /// <summary>
        /// Example: MongoDB with GridFS and TTL
        /// </summary>
        public static async Task MongoDbExample()
        {
            SmtpServer server = new SmtpServerBuilder()
                .Port(25)
                .WithMongoDbStorage(
                    "mongodb://localhost:27017",
                    "smtp_database",
                    config =>
                    {
                        config.CollectionName = "email_messages";
                        config.UseGridFsForLargeMessages = true;
                        config.GridFsThresholdMB = 5; // Use GridFS for messages > 5MB
                        config.EnableTTL = true;
                        config.TTLDays = 30; // Auto-delete after 30 days
                        config.EnableSharding = true;
                        config.ShardKeyField = "received_date";
                        config.CompressMessageBody = true;
                    })
                .Build();

            await server.StartAsync();
            Console.WriteLine("SMTP Server with MongoDB storage started on port 25");
        }

        /// <summary>
        /// Example: Redis for high-performance caching
        /// </summary>
        public static async Task RedisExample()
        {
            SmtpServer server = new SmtpServerBuilder()
                .Port(2525)
                .WithRedisStorage(
                    "localhost:6379,password=mypassword",
                    config =>
                    {
                        config.DatabaseNumber = 1;
                        config.KeyPrefix = "smtp:msg:";
                        config.MessageTTLSeconds = 3600; // 1 hour cache
                        config.CompressMessageBody = true;
                        config.MaxMessageSizeMB = 5; // Small limit for cache
                        config.EnableChunking = true;
                        config.ChunkSizeKB = 64;
                        config.UseRedisStreams = true;
                        config.EnablePubSub = true;
                        config.MaintainIndex = true;
                    })
                .Build();

            await server.StartAsync();
            Console.WriteLine("SMTP Server with Redis cache started on port 2525");
        }

        /// <summary>
        /// Example: Azure Blob Storage with hierarchical structure
        /// </summary>
        public static async Task AzureBlobExample()
        {
            SmtpServer server = new SmtpServerBuilder()
                .Port(25)
                .WithAzureBlobStorage(
                    "DefaultEndpointsProtocol=https;AccountName=myaccount;AccountKey=mykey;EndpointSuffix=core.windows.net",
                    config =>
                    {
                        config.ContainerName = "smtp-messages";
                        config.NamingFormat = BlobNamingFormat.DateHierarchy;
                        config.AccessTier = BlobAccessTier.Hot;
                        config.CompressMessageBody = true;
                        config.StoreMetadataAsBlobProperties = true;
                        config.EnableSoftDelete = true;
                        config.SoftDeleteRetentionDays = 7;
                    })
                .Build();

            await server.StartAsync();
            Console.WriteLine("SMTP Server with Azure Blob Storage started on port 25");
        }

        /// <summary>
        /// Example: Azure Blob Storage with Azure AD authentication
        /// </summary>
        public static async Task AzureBlobWithADExample()
        {
            SmtpServer server = new SmtpServerBuilder()
                .Port(25)
                .WithAzureBlobStorageAD(
                    "mystorageaccount",
                    config =>
                    {
                        config.ContainerName = "email-archive";
                        config.NamingFormat = BlobNamingFormat.YearMonth;
                        config.AccessTier = BlobAccessTier.Cool; // For archival
                        config.CompressMessageBody = true;
                    })
                .Build();

            await server.StartAsync();
            Console.WriteLine("SMTP Server with Azure Blob Storage (AD auth) started on port 25");
        }

        /// <summary>
        /// Example: Amazon S3 with lifecycle rules
        /// </summary>
        public static async Task AmazonS3Example()
        {
            SmtpServer server = new SmtpServerBuilder()
                .Port(25)
                .WithS3Storage(
                    "AKIAIOSFODNN7EXAMPLE",
                    "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
                    "my-smtp-bucket",
                    config =>
                    {
                        config.Region = "us-west-2";
                        config.KeyPrefix = "messages/";
                        config.NamingFormat = S3NamingFormat.DateHierarchy;
                        config.StorageClass = S3StorageClass.Standard;
                        config.EnableServerSideEncryption = true;
                        config.CompressMessageBody = true;
                        config.EnableVersioning = true;
                        config.EnableLifecycleRules = true;
                        config.TransitionToIADays = 30;
                        config.TransitionToGlacierDays = 90;
                        config.ExpirationDays = 365;
                    })
                .Build();

            await server.StartAsync();
            Console.WriteLine("SMTP Server with Amazon S3 storage started on port 25");
        }

        /// <summary>
        /// Example: S3-compatible storage (MinIO, Wasabi, etc.)
        /// </summary>
        public static async Task S3CompatibleExample()
        {
            SmtpServer server = new SmtpServerBuilder()
                .Port(25)
                .WithS3CompatibleStorage(
                    "http://localhost:9000", // MinIO endpoint
                    "minioadmin",
                    "minioadmin",
                    "smtp-messages",
                    config =>
                    {
                        config.Region = "us-east-1";
                        config.NamingFormat = S3NamingFormat.YearMonth;
                        config.CompressMessageBody = true;
                        config.EnableServerSideEncryption = false; // MinIO may not support all S3 features
                        config.ForcePathStyle = true; // Required for MinIO
                    })
                .Build();

            await server.StartAsync();
            Console.WriteLine("SMTP Server with MinIO storage started on port 25");
        }

        /// <summary>
        /// Example: Hybrid storage - Redis cache + SQL Server persistence
        /// </summary>
        public static async Task HybridStorageExample()
        {
            // First, create a custom composite storage that uses Redis for cache
            // and SQL Server for long-term storage
            // This would require implementing a custom IMessageStore

            Console.WriteLine("Hybrid storage example would require custom implementation");
            Console.WriteLine("Combine Redis for immediate caching with SQL/S3 for persistence");

            // Conceptual code:
            /*
            var server = new SmtpServerBuilder()
                .Port(25)
                .MessageStore(new CompositeMessageStore(
                    new RedisMessageStore(redisConfig),    // Primary (cache)
                    new SqlServerMessageStore(sqlConfig)    // Secondary (persistence)
                ))
                .Build();
            */

            await Task.CompletedTask;
        }

        /// <summary>
        /// Example: Production-ready configuration with monitoring
        /// </summary>
        public static async Task ProductionExample()
        {
            // Use environment variables for sensitive data
            string connectionString = Environment.GetEnvironmentVariable("SMTP_SQL_CONNECTION")
                ?? "Server=localhost;Database=SmtpProd;Trusted_Connection=true;";

            SmtpServer server = new SmtpServerBuilder()
                .Port(25)
                .ServerName("Production SMTP Server")
                .MaxMessageSizeMB(25)
                .MaxConnections(100)
                .RequireSecureConnection()
                .RequireAuthentication()
                .WithSqlServerStorage(connectionString, config =>
                {
                    config.CompressMessageBody = true;
                    config.MaxMessageSizeMB = 25;
                    config.EnableRetry = true;
                    config.MaxRetryAttempts = 5;
                    config.RetryDelayMs = 2000;
                    config.LogErrors = true;
                })
                .Build();

            // Add comprehensive event handling
            server.MessageReceived += (sender, e) =>
            {
                Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Message received: {e.Message.Id}");
                Console.WriteLine($"  From: {e.Message.From?.Address}");
                Console.WriteLine($"  To: {string.Join(", ", e.Message.Recipients)}");
                Console.WriteLine($"  Subject: {e.Message.Subject}");
                Console.WriteLine($"  Size: {e.Message.Size:N0} bytes");
            };

            server.SessionCreated += (sender, e) =>
            {
                Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Session created: {e.Session.Id}");
            };

            server.SessionCompleted += (sender, e) =>
            {
                Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Session completed: {e.Session.Id}");
            };

            await server.StartAsync();
            Console.WriteLine($"Production SMTP Server started on port 25");
            Console.WriteLine($"Storage: SQL Server");
            Console.WriteLine($"Security: TLS required, Authentication required");
            Console.WriteLine($"Limits: 25MB messages, 100 concurrent connections");
        }
    }
}