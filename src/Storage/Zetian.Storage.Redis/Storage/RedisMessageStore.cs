using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Zetian.Abstractions;
using Zetian.Storage.Redis.Configuration;

namespace Zetian.Storage.Redis.Storage
{
    /// <summary>
    /// Redis implementation of IMessageStore
    /// </summary>
    public class RedisMessageStore : IMessageStore, IDisposable
    {
        private readonly RedisStorageConfiguration _configuration;
        private readonly ILogger<RedisMessageStore>? _logger;
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _database;
        private readonly ISubscriber? _subscriber;

        public RedisMessageStore(RedisStorageConfiguration configuration, ILogger<RedisMessageStore>? logger = null)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _configuration.Validate();
            _logger = logger;

            // Initialize Redis connection
            ConfigurationOptions configOptions = ConfigurationOptions.Parse(_configuration.ConnectionString);
            configOptions.ConnectTimeout = _configuration.ConnectionTimeoutSeconds * 1000;
            configOptions.ConnectRetry = _configuration.MaxRetryAttempts;

            _redis = ConnectionMultiplexer.Connect(configOptions);
            _database = _redis.GetDatabase(_configuration.DatabaseNumber);

            // Initialize subscriber if Pub/Sub is enabled
            if (_configuration.EnablePubSub)
            {
                _subscriber = _redis.GetSubscriber();
            }

            _logger?.LogInformation("Initialized Redis client for database {Database}", _configuration.DatabaseNumber);
        }

        public async Task<bool> SaveAsync(ISmtpSession session, ISmtpMessage message, CancellationToken cancellationToken = default)
        {
            try
            {
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

                // Prepare metadata
                MessageMetadata metadata = new()
                {
                    MessageId = message.Id,
                    SessionId = session.Id,
                    FromAddress = message.From?.Address,
                    ToAddresses = message.Recipients.Select(r => r.Address).ToList(),
                    Subject = message.Subject,
                    ReceivedDate = DateTime.UtcNow,
                    MessageSize = rawData.Length,
                    HasAttachments = message.HasAttachments,
                    AttachmentCount = message.AttachmentCount,
                    Priority = message.Priority.ToString()
                };

                // Compress if configured
                byte[] dataToStore = rawData;
                if (_configuration.CompressMessageBody)
                {
                    dataToStore = CompressData(rawData);
                    metadata.IsCompressed = true;
                }

                // Store based on configuration
                if (_configuration.UseRedisStreams)
                {
                    await StoreInStreamAsync(message.Id, dataToStore, metadata, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await StoreAsKeyValueAsync(message.Id, dataToStore, metadata, cancellationToken).ConfigureAwait(false);
                }

                // Publish notification if enabled
                if (_configuration.EnablePubSub && _subscriber != null)
                {
                    await _subscriber.PublishAsync(_configuration.PubSubChannel,
                        JsonSerializer.Serialize(new { Event = "MessageStored", MessageId = message.Id })).ConfigureAwait(false);
                }

                _logger?.LogInformation("Message {MessageId} saved to Redis", message.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error saving message {MessageId} to Redis", message.Id);

                if (_configuration.EnableRetry)
                {
                    return await RetryAsync(session, message, cancellationToken).ConfigureAwait(false);
                }

                return false;
            }
        }

        private async Task StoreAsKeyValueAsync(string messageId, byte[] data, MessageMetadata metadata, CancellationToken cancellationToken)
        {
            ITransaction transaction = _database.CreateTransaction();

            // Store metadata as hash
            string metadataKey = _configuration.GetMetadataKey(messageId);
            HashEntry[] hashEntries = new[]
            {
                new HashEntry("MessageId", metadata.MessageId),
                new HashEntry("SessionId", metadata.SessionId),
                new HashEntry("FromAddress", metadata.FromAddress ?? ""),
                new HashEntry("Subject", metadata.Subject ?? ""),
                new HashEntry("ReceivedDate", metadata.ReceivedDate.ToString("O")),
                new HashEntry("MessageSize", metadata.MessageSize),
                new HashEntry("IsCompressed", metadata.IsCompressed),
                new HashEntry("HasAttachments", metadata.HasAttachments),
                new HashEntry("AttachmentCount", metadata.AttachmentCount),
                new HashEntry("Priority", metadata.Priority ?? "")
            };

            _ = transaction.HashSetAsync(metadataKey, hashEntries);

            if (_configuration.MessageTTLSeconds > 0)
            {
                _ = transaction.KeyExpireAsync(metadataKey, TimeSpan.FromSeconds(_configuration.MessageTTLSeconds));
            }

            // Store message data
            if (_configuration.EnableChunking && data.Length > _configuration.ChunkSizeKB * 1024)
            {
                // Store in chunks
                int chunkSize = _configuration.ChunkSizeKB * 1024;
                int totalChunks = (int)Math.Ceiling((double)data.Length / chunkSize);

                for (int i = 0; i < totalChunks; i++)
                {
                    int offset = i * chunkSize;
                    int length = Math.Min(chunkSize, data.Length - offset);
                    byte[] chunk = new byte[length];
                    Array.Copy(data, offset, chunk, 0, length);

                    string chunkKey = _configuration.GetChunkKey(messageId, i);
                    _ = transaction.StringSetAsync(chunkKey, chunk);

                    if (_configuration.MessageTTLSeconds > 0)
                    {
                        _ = transaction.KeyExpireAsync(chunkKey, TimeSpan.FromSeconds(_configuration.MessageTTLSeconds));
                    }
                }

                _ = transaction.HashSetAsync(metadataKey, "TotalChunks", totalChunks);
            }
            else
            {
                // Store as single key
                string messageKey = _configuration.GetMessageKey(messageId);
                _ = transaction.StringSetAsync(messageKey, data);

                if (_configuration.MessageTTLSeconds > 0)
                {
                    _ = transaction.KeyExpireAsync(messageKey, TimeSpan.FromSeconds(_configuration.MessageTTLSeconds));
                }
            }

            // Add to index if enabled
            if (_configuration.MaintainIndex)
            {
                _ = transaction.SortedSetAddAsync(_configuration.IndexKey, messageId, metadata.ReceivedDate.Ticks);

                // Optionally expire old entries from index
                _ = transaction.SortedSetRemoveRangeByScoreAsync(_configuration.IndexKey,
                    0,
                    DateTime.UtcNow.AddSeconds(-_configuration.MessageTTLSeconds).Ticks);
            }

            await transaction.ExecuteAsync().ConfigureAwait(false);
        }

        private async Task StoreInStreamAsync(string messageId, byte[] data, MessageMetadata metadata, CancellationToken cancellationToken)
        {
            NameValueEntry[] streamEntries = new NameValueEntry[]
            {
                new("MessageId", messageId),
                new("SessionId", metadata.SessionId),
                new("FromAddress", metadata.FromAddress ?? ""),
                new("Subject", metadata.Subject ?? ""),
                new("ReceivedDate", metadata.ReceivedDate.ToString("O")),
                new("MessageSize", metadata.MessageSize.ToString()),
                new("MessageData", Convert.ToBase64String(data)),
                new("IsCompressed", metadata.IsCompressed.ToString()),
                new("HasAttachments", metadata.HasAttachments.ToString()),
                new("AttachmentCount", metadata.AttachmentCount.ToString()),
                new("Priority", metadata.Priority ?? "")
            };

            RedisValue streamId = await _database.StreamAddAsync(_configuration.StreamKey, streamEntries).ConfigureAwait(false);

            // Trim stream if it gets too large (keep last 1000 messages)
            await _database.StreamTrimAsync(_configuration.StreamKey, 1000, true).ConfigureAwait(false);

            _logger?.LogDebug("Message {MessageId} added to Redis Stream with ID {StreamId}", messageId, streamId);
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
            _redis?.Dispose();
        }

        /// <summary>
        /// Message metadata for Redis storage
        /// </summary>
        private class MessageMetadata
        {
            public string MessageId { get; set; } = string.Empty;
            public string SessionId { get; set; } = string.Empty;
            public string? FromAddress { get; set; }
            public List<string> ToAddresses { get; set; } = [];
            public string? Subject { get; set; }
            public DateTime ReceivedDate { get; set; }
            public long MessageSize { get; set; }
            public bool IsCompressed { get; set; }
            public bool HasAttachments { get; set; }
            public int AttachmentCount { get; set; }
            public string? Priority { get; set; }
        }
    }
}