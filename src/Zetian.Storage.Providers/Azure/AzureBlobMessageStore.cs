using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Zetian.Abstractions;

namespace Zetian.Storage.Providers.Azure
{
    /// <summary>
    /// Azure Blob Storage implementation of IMessageStore
    /// </summary>
    public class AzureBlobMessageStore : IMessageStore, IDisposable
    {
        private readonly AzureBlobStorageConfiguration _configuration;
        private readonly ILogger<AzureBlobMessageStore>? _logger;
        private readonly BlobContainerClient _containerClient;
        private bool _containerChecked = false;
        private readonly SemaphoreSlim _containerLock = new(1, 1);

        public AzureBlobMessageStore(AzureBlobStorageConfiguration configuration, ILogger<AzureBlobMessageStore>? logger = null)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _configuration.Validate();
            _logger = logger;

            // Initialize Azure Blob client
            BlobServiceClient serviceClient;

            if (_configuration.UseAzureAdAuthentication)
            {
                Uri uri = new($"https://{_configuration.StorageAccountName}.blob.core.windows.net");
                serviceClient = new BlobServiceClient(uri, new DefaultAzureCredential());
            }
            else
            {
                serviceClient = new BlobServiceClient(_configuration.ConnectionString);
            }

            _containerClient = serviceClient.GetBlobContainerClient(_configuration.ContainerName);

            _logger?.LogInformation("Initialized Azure Blob Storage client for container {ContainerName}", _configuration.ContainerName);
        }

        public async Task<bool> SaveAsync(ISmtpSession session, ISmtpMessage message, CancellationToken cancellationToken = default)
        {
            try
            {
                // Ensure container exists
                await EnsureContainerExistsAsync(cancellationToken).ConfigureAwait(false);

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

                // Prepare blob name
                string blobName = _configuration.GetBlobName(message.Id, DateTime.UtcNow);
                BlobClient blobClient = _containerClient.GetBlobClient(blobName);

                // Compress if configured
                byte[] dataToUpload = rawData;
                bool isCompressed = false;

                if (_configuration.CompressMessageBody)
                {
                    dataToUpload = CompressData(rawData);
                    isCompressed = true;
                }

                // Prepare metadata
                Dictionary<string, string> metadata = [];
                Dictionary<string, string> tags = [];

                if (_configuration.StoreMetadataAsBlobProperties)
                {
                    metadata["MessageId"] = message.Id;
                    metadata["SessionId"] = session.Id;
                    metadata["FromAddress"] = message.From?.Address ?? "";
                    metadata["Subject"] = TruncateForMetadata(message.Subject) ?? "";
                    metadata["ReceivedDate"] = DateTime.UtcNow.ToString("O");
                    metadata["MessageSize"] = rawData.Length.ToString();
                    metadata["IsCompressed"] = isCompressed.ToString();
                    metadata["HasAttachments"] = message.HasAttachments.ToString();
                    metadata["AttachmentCount"] = message.AttachmentCount.ToString();
                    metadata["Priority"] = message.Priority.ToString();

                    // Add tags for indexing (Azure supports up to 10 tags)
                    tags["MessageId"] = message.Id;
                    tags["SessionId"] = session.Id;
                    if (message.From?.Address != null)
                    {
                        tags["FromDomain"] = GetDomainFromEmail(message.From.Address);
                    }
                }

                // Upload options
                BlobUploadOptions uploadOptions = new()
                {
                    Metadata = metadata,
                    Tags = tags,
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = "message/rfc822",
                        ContentEncoding = isCompressed ? "gzip" : null
                    },
                    // Set access tier
                    AccessTier = _configuration.AccessTier switch
                    {
                        BlobAccessTier.Cool => AccessTier.Cool,
                        BlobAccessTier.Archive => AccessTier.Archive,
                        _ => AccessTier.Hot
                    }
                };

                // Upload blob
                using MemoryStream stream = new(dataToUpload);
                await blobClient.UploadAsync(stream, uploadOptions, cancellationToken).ConfigureAwait(false);

                _logger?.LogInformation("Message {MessageId} uploaded to Azure Blob Storage as {BlobName}", message.Id, blobName);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error saving message {MessageId} to Azure Blob Storage", message.Id);

                if (_configuration.EnableRetry)
                {
                    return await RetryAsync(session, message, cancellationToken).ConfigureAwait(false);
                }

                return false;
            }
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

        private async Task EnsureContainerExistsAsync(CancellationToken cancellationToken)
        {
            if (_containerChecked || !_configuration.AutoCreateContainer)
            {
                return;
            }

            await _containerLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_containerChecked)
                {
                    return;
                }

                // Check if container exists
                Response<bool> exists = await _containerClient.ExistsAsync(cancellationToken).ConfigureAwait(false);

                if (!exists)
                {
                    // Create container
                    await _containerClient.CreateAsync(PublicAccessType.None, cancellationToken: cancellationToken).ConfigureAwait(false);

                    _logger?.LogInformation("Created Azure Blob container {ContainerName}", _configuration.ContainerName);

                    // Configure soft delete if enabled
                    if (_configuration.EnableSoftDelete)
                    {
                        try
                        {
                            BlobServiceClient serviceClient = _containerClient.GetParentBlobServiceClient();
                            Response<BlobServiceProperties> properties = await serviceClient.GetPropertiesAsync(cancellationToken).ConfigureAwait(false);

                            properties.Value.DeleteRetentionPolicy = new BlobRetentionPolicy
                            {
                                Enabled = true,
                                Days = _configuration.SoftDeleteRetentionDays
                            };

                            await serviceClient.SetPropertiesAsync(properties.Value, cancellationToken).ConfigureAwait(false);

                            _logger?.LogInformation("Enabled soft delete with {Days} days retention", _configuration.SoftDeleteRetentionDays);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogWarning(ex, "Could not enable soft delete (may require additional permissions)");
                        }
                    }
                }

                _containerChecked = true;
            }
            finally
            {
                _containerLock.Release();
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

        private string? TruncateForMetadata(string? value, int maxLength = 1024)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return value.Length <= maxLength ? value : value[..maxLength];
        }

        private string GetDomainFromEmail(string email)
        {
            int atIndex = email.IndexOf('@');
            return atIndex > 0 && atIndex < email.Length - 1
                ? email[(atIndex + 1)..]
                : "unknown";
        }

        /// <summary>
        /// Downloads a message from Azure Blob Storage (for testing/verification)
        /// </summary>
        public async Task<byte[]?> DownloadMessageAsync(string messageId, DateTime? receivedDate = null)
        {
            try
            {
                string blobName = _configuration.GetBlobName(messageId, receivedDate ?? DateTime.UtcNow);
                BlobClient blobClient = _containerClient.GetBlobClient(blobName);

                Response<bool> exists = await blobClient.ExistsAsync().ConfigureAwait(false);
                if (!exists)
                {
                    return null;
                }

                Response<BlobDownloadResult> response = await blobClient.DownloadContentAsync().ConfigureAwait(false);
                byte[] data = response.Value.Content.ToArray();

                // Check if compressed
                if (response.Value.Details.ContentEncoding?.Contains("gzip") == true)
                {
                    using MemoryStream input = new(data);
                    using MemoryStream output = new();
                    using (GZipStream decompressor = new(input, CompressionMode.Decompress))
                    {
                        await decompressor.CopyToAsync(output).ConfigureAwait(false);
                    }
                    return output.ToArray();
                }

                return data;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error downloading message {MessageId} from Azure Blob Storage", messageId);
                return null;
            }
        }

        /// <summary>
        /// Lists messages in the container
        /// </summary>
        public async Task<List<string>> ListMessagesAsync(string? prefix = null, int maxResults = 100)
        {
            List<string> messages = [];

            await foreach (BlobItem blobItem in _containerClient.GetBlobsAsync(prefix: prefix))
            {
                messages.Add(blobItem.Name);
                if (messages.Count >= maxResults)
                {
                    break;
                }
            }

            return messages;
        }

        /// <summary>
        /// Deletes a message from Azure Blob Storage
        /// </summary>
        public async Task<bool> DeleteMessageAsync(string messageId, DateTime? receivedDate = null)
        {
            try
            {
                string blobName = _configuration.GetBlobName(messageId, receivedDate ?? DateTime.UtcNow);
                BlobClient blobClient = _containerClient.GetBlobClient(blobName);

                Response<bool> response = await blobClient.DeleteIfExistsAsync().ConfigureAwait(false);

                if (response.Value)
                {
                    _logger?.LogInformation("Deleted message {MessageId} from Azure Blob Storage", messageId);
                }

                return response.Value;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deleting message {MessageId} from Azure Blob Storage", messageId);
                return false;
            }
        }

        public void Dispose()
        {
            _containerLock?.Dispose();
        }
    }
}