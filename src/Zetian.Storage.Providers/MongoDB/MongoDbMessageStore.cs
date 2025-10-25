using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Zetian.Abstractions;

namespace Zetian.Storage.Providers.MongoDB
{
    /// <summary>
    /// MongoDB implementation of IMessageStore
    /// </summary>
    public class MongoDbMessageStore : IMessageStore, IDisposable
    {
        private readonly MongoDbStorageConfiguration _configuration;
        private readonly ILogger<MongoDbMessageStore>? _logger;
        private readonly IMongoClient _client;
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<MessageDocument> _collection;
        private readonly IGridFSBucket _gridFsBucket;
        private bool _indexesCreated = false;
        private readonly SemaphoreSlim _indexLock = new(1, 1);

        public MongoDbMessageStore(MongoDbStorageConfiguration configuration, ILogger<MongoDbMessageStore>? logger = null)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _configuration.Validate();
            _logger = logger;

            // Initialize MongoDB client
            _client = new MongoClient(_configuration.ConnectionString);
            _database = _client.GetDatabase(_configuration.DatabaseName);
            _collection = _database.GetCollection<MessageDocument>(_configuration.CollectionName);

            // Initialize GridFS
            GridFSBucketOptions gridFsOptions = new()
            {
                BucketName = _configuration.GridFsBucketName
            };
            _gridFsBucket = new GridFSBucket(_database, gridFsOptions);
        }

        public async Task<bool> SaveAsync(ISmtpSession session, ISmtpMessage message, CancellationToken cancellationToken = default)
        {
            try
            {
                // Ensure indexes exist
                await EnsureIndexesAsync(cancellationToken).ConfigureAwait(false);

                // Get message data
                byte[] rawData = await message.GetRawDataAsync().ConfigureAwait(false);

                // Check size limit
                if (_configuration.MaxMessageSizeMB > 0)
                {
                    double sizeMB = rawData.Length / (1024.0 * 1024.0);
                    if (sizeMB > _configuration.MaxMessageSizeMB)
                    {
                        _logger?.LogWarning("Message {MessageId} exceeds size limit ({Size:F2}MB > {Limit}MB)",
                            message.Id, sizeMB, _configuration.MaxMessageSizeMB);
                        return false;
                    }
                }

                // Prepare document
                MessageDocument document = new()
                {
                    MessageId = message.Id,
                    SessionId = session.Id,
                    FromAddress = message.From?.Address,
                    ToAddresses = message.Recipients.Select(r => r.Address).ToList(),
                    Subject = message.Subject,
                    ReceivedDate = DateTime.UtcNow,
                    MessageSize = rawData.Length,
                    Headers = message.Headers,
                    HasAttachments = message.HasAttachments,
                    AttachmentCount = message.AttachmentCount,
                    Priority = message.Priority.ToString(),
                    TextBody = message.TextBody,
                    HtmlBody = message.HtmlBody
                };

                // Determine storage strategy
                double sizeMB2 = rawData.Length / (1024.0 * 1024.0);
                bool useGridFs = _configuration.UseGridFsForLargeMessages && sizeMB2 >= _configuration.GridFsThresholdMB;

                if (useGridFs)
                {
                    // Store in GridFS
                    ObjectId gridFsId = await StoreInGridFsAsync(message.Id, rawData, cancellationToken).ConfigureAwait(false);
                    document.GridFsId = gridFsId;
                    document.IsStoredInGridFs = true;
                }
                else
                {
                    // Store inline
                    if (_configuration.CompressMessageBody)
                    {
                        document.MessageBody = CompressData(rawData);
                        document.IsCompressed = true;
                    }
                    else
                    {
                        document.MessageBody = rawData;
                        document.IsCompressed = false;
                    }
                }

                // Set TTL if configured
                if (_configuration.EnableTTL)
                {
                    document.ExpiresAt = DateTime.UtcNow.AddDays(_configuration.TTLDays);
                }

                // Insert document
                await _collection.InsertOneAsync(document, cancellationToken: cancellationToken).ConfigureAwait(false);

                _logger?.LogInformation("Message {MessageId} saved to MongoDB", message.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error saving message {MessageId} to MongoDB", message.Id);

                if (_configuration.EnableRetry)
                {
                    return await RetryAsync(session, message, cancellationToken).ConfigureAwait(false);
                }

                return false;
            }
        }

        private async Task<ObjectId> StoreInGridFsAsync(string messageId, byte[] data, CancellationToken cancellationToken)
        {
            GridFSUploadOptions options = new()
            {
                Metadata = new BsonDocument
                {
                    { "messageId", messageId },
                    { "uploadDate", DateTime.UtcNow }
                }
            };

            using MemoryStream stream = new(data);
            ObjectId objectId = await _gridFsBucket.UploadFromStreamAsync(
                $"{messageId}.eml",
                stream,
                options,
                cancellationToken).ConfigureAwait(false);

            return objectId;
        }

        private async Task<bool> RetryAsync(ISmtpSession session, ISmtpMessage message, CancellationToken cancellationToken)
        {
            for (int i = 0; i < _configuration.MaxRetryAttempts; i++)
            {
                await Task.Delay(_configuration.RetryDelayMs * (i + 1), cancellationToken).ConfigureAwait(false);

                try
                {
                    _logger?.LogInformation("Retry attempt {Attempt} for message {MessageId}", i + 1, message.Id);

                    // Try again without recursion
                    _configuration.EnableRetry = false;
                    bool result = await SaveAsync(session, message, cancellationToken).ConfigureAwait(false);
                    _configuration.EnableRetry = true;

                    if (result)
                    {
                        return true;
                    }
                }
                catch
                {
                    // Continue retry
                }
            }

            return false;
        }

        private async Task EnsureIndexesAsync(CancellationToken cancellationToken)
        {
            if (_indexesCreated || !_configuration.AutoCreateIndexes)
            {
                return;
            }

            await _indexLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_indexesCreated)
                {
                    return;
                }

                List<CreateIndexModel<MessageDocument>> indexKeys =
                [
                    new(Builders<MessageDocument>.IndexKeys.Ascending(x => x.MessageId)),
                    new(Builders<MessageDocument>.IndexKeys.Ascending(x => x.SessionId)),
                    new(Builders<MessageDocument>.IndexKeys.Descending(x => x.ReceivedDate)),
                    new(Builders<MessageDocument>.IndexKeys.Ascending(x => x.FromAddress)),
                    new(Builders<MessageDocument>.IndexKeys.Text(x => x.Subject))
                ];

                // Add TTL index if enabled
                if (_configuration.EnableTTL)
                {
                    CreateIndexOptions ttlIndexOptions = new()
                    {
                        ExpireAfter = TimeSpan.FromDays(_configuration.TTLDays)
                    };
                    indexKeys.Add(new CreateIndexModel<MessageDocument>(
                        Builders<MessageDocument>.IndexKeys.Ascending(x => x.ExpiresAt),
                        ttlIndexOptions));
                }

                await _collection.Indexes.CreateManyAsync(indexKeys, cancellationToken).ConfigureAwait(false);

                // Enable sharding if configured
                if (_configuration.EnableSharding)
                {
                    await EnableShardingAsync(cancellationToken).ConfigureAwait(false);
                }

                _logger?.LogInformation("Created MongoDB indexes for collection {Collection}", _configuration.CollectionName);
                _indexesCreated = true;
            }
            finally
            {
                _indexLock.Release();
            }
        }

        private async Task EnableShardingAsync(CancellationToken cancellationToken)
        {
            try
            {
                IMongoDatabase adminDb = _client.GetDatabase("admin");

                // Enable sharding on database
                BsonDocument enableShardingCommand = new()
                {
                    { "enableSharding", _configuration.DatabaseName }
                };
                await adminDb.RunCommandAsync<BsonDocument>(enableShardingCommand, cancellationToken: cancellationToken).ConfigureAwait(false);

                // Shard the collection
                BsonDocument shardCollectionCommand = new()
                {
                    { "shardCollection", $"{_configuration.DatabaseName}.{_configuration.CollectionName}" },
                    { "key", new BsonDocument { { _configuration.ShardKeyField, 1 } } }
                };
                await adminDb.RunCommandAsync<BsonDocument>(shardCollectionCommand, cancellationToken: cancellationToken).ConfigureAwait(false);

                _logger?.LogInformation("Enabled sharding for collection {Collection} on field {Field}",
                    _configuration.CollectionName, _configuration.ShardKeyField);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Could not enable sharding (may require admin privileges)");
            }
        }

        private byte[] CompressData(byte[] data)
        {
            using MemoryStream output = new();
            using (GZipStream compressor = new(output, CompressionLevel.Optimal))
            {
                compressor.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }

        public void Dispose()
        {
            _indexLock?.Dispose();
        }

        /// <summary>
        /// MongoDB document model for storing messages
        /// </summary>
        private class MessageDocument
        {
            public ObjectId Id { get; set; }
            public string MessageId { get; set; } = string.Empty;
            public string SessionId { get; set; } = string.Empty;
            public string? FromAddress { get; set; }
            public List<string> ToAddresses { get; set; } = [];
            public string? Subject { get; set; }
            public DateTime ReceivedDate { get; set; }
            public long MessageSize { get; set; }
            public byte[]? MessageBody { get; set; }
            public bool IsCompressed { get; set; }
            public IDictionary<string, string>? Headers { get; set; }
            public bool HasAttachments { get; set; }
            public int AttachmentCount { get; set; }
            public string? Priority { get; set; }
            public string? TextBody { get; set; }
            public string? HtmlBody { get; set; }
            public ObjectId? GridFsId { get; set; }
            public bool IsStoredInGridFs { get; set; }
            public DateTime? ExpiresAt { get; set; }
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        }
    }
}
