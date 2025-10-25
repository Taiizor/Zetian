using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Zetian.Abstractions;
using Zetian.Storage.AmazonS3.Configuration;

namespace Zetian.Storage.AmazonS3.Storage
{
    /// <summary>
    /// Amazon S3 implementation of IMessageStore
    /// </summary>
    public class S3MessageStore : IMessageStore, IDisposable
    {
        private readonly S3StorageConfiguration _configuration;
        private readonly ILogger<S3MessageStore>? _logger;
        private readonly IAmazonS3 _s3Client;
        private bool _bucketChecked = false;
        private readonly SemaphoreSlim _bucketLock = new(1, 1);

        public S3MessageStore(S3StorageConfiguration configuration, ILogger<S3MessageStore>? logger = null)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _configuration.Validate();
            _logger = logger;

            // Initialize S3 client
            _s3Client = CreateS3Client();

            _logger?.LogInformation("Initialized S3 client for bucket {BucketName} in region {Region}",
                _configuration.BucketName, _configuration.Region);
        }

        private IAmazonS3 CreateS3Client()
        {
            AmazonS3Config config = new()
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(_configuration.Region),
                ForcePathStyle = _configuration.ForcePathStyle,
                UseAccelerateEndpoint = _configuration.UseTransferAcceleration
            };

            // Set custom endpoint if provided (for S3-compatible services)
            if (!string.IsNullOrWhiteSpace(_configuration.ServiceUrl))
            {
                config.ServiceURL = _configuration.ServiceUrl;
            }

            // Create client with appropriate credentials
            if (!string.IsNullOrWhiteSpace(_configuration.AccessKeyId) &&
                !string.IsNullOrWhiteSpace(_configuration.SecretAccessKey))
            {
                BasicAWSCredentials credentials = new(_configuration.AccessKeyId, _configuration.SecretAccessKey);
                return new AmazonS3Client(credentials, config);
            }
            else
            {
                // Use IAM role or default credentials
                return new AmazonS3Client(config);
            }
        }

        public async Task<bool> SaveAsync(ISmtpSession session, ISmtpMessage message, CancellationToken cancellationToken = default)
        {
            try
            {
                // Ensure bucket exists
                await EnsureBucketExistsAsync(cancellationToken).ConfigureAwait(false);

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

                // Prepare object key
                string objectKey = _configuration.GetObjectKey(message.Id, DateTime.UtcNow);

                // Compress if configured
                byte[] dataToUpload = rawData;
                bool isCompressed = false;

                if (_configuration.CompressMessageBody)
                {
                    dataToUpload = CompressData(rawData);
                    isCompressed = true;
                }

                // Prepare metadata
                Dictionary<string, string> metadata = new()
                {
                    ["message-id"] = message.Id,
                    ["session-id"] = session.Id,
                    ["from-address"] = message.From?.Address ?? "",
                    ["subject"] = TruncateForMetadata(message.Subject) ?? "",
                    ["received-date"] = DateTime.UtcNow.ToString("O"),
                    ["message-size"] = rawData.Length.ToString(),
                    ["is-compressed"] = isCompressed.ToString(),
                    ["has-attachments"] = message.HasAttachments.ToString(),
                    ["attachment-count"] = message.AttachmentCount.ToString(),
                    ["priority"] = message.Priority.ToString()
                };

                // Prepare tags
                List<Tag> tags =
                [
                    new Tag { Key = "MessageId", Value = message.Id },
                    new Tag { Key = "SessionId", Value = session.Id },
                    new Tag { Key = "ReceivedDate", Value = DateTime.UtcNow.ToString("yyyy-MM-dd") }
                ];

                if (message.From?.Address != null)
                {
                    tags.Add(new Tag { Key = "FromDomain", Value = GetDomainFromEmail(message.From.Address) });
                }

                // Create put request
                PutObjectRequest putRequest = new()
                {
                    BucketName = _configuration.BucketName,
                    Key = objectKey,
                    ContentType = "message/rfc822",
                    StorageClass = _configuration.StorageClass
                };

                // Add metadata
                foreach (KeyValuePair<string, string> kvp in metadata)
                {
                    putRequest.Metadata.Add(kvp.Key, kvp.Value);
                }

                // Set server-side encryption
                if (_configuration.EnableServerSideEncryption)
                {
                    if (!string.IsNullOrWhiteSpace(_configuration.KmsKeyId))
                    {
                        putRequest.ServerSideEncryptionMethod = ServerSideEncryptionMethod.AWSKMS;
                        putRequest.ServerSideEncryptionKeyManagementServiceKeyId = _configuration.KmsKeyId;
                    }
                    else
                    {
                        putRequest.ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256;
                    }
                }

                // Set content encoding if compressed
                if (isCompressed)
                {
                    putRequest.Headers.ContentEncoding = "gzip";
                }

                // Upload data
                using MemoryStream stream = new(dataToUpload);
                putRequest.InputStream = stream;

                PutObjectResponse response = await _s3Client.PutObjectAsync(putRequest, cancellationToken).ConfigureAwait(false);

                // Add tags (separate request)
                if (tags.Count > 0)
                {
                    PutObjectTaggingRequest taggingRequest = new()
                    {
                        BucketName = _configuration.BucketName,
                        Key = objectKey,
                        Tagging = new Tagging { TagSet = tags }
                    };

                    await _s3Client.PutObjectTaggingAsync(taggingRequest, cancellationToken).ConfigureAwait(false);
                }

                _logger?.LogInformation("Message {MessageId} uploaded to S3 as {ObjectKey} with ETag {ETag}",
                    message.Id, objectKey, response.ETag);

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error saving message {MessageId} to S3", message.Id);

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

        private async Task EnsureBucketExistsAsync(CancellationToken cancellationToken)
        {
            if (_bucketChecked || !_configuration.AutoCreateBucket)
            {
                return;
            }

            await _bucketLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_bucketChecked)
                {
                    return;
                }

                // Check if bucket exists
                try
                {
                    await _s3Client.HeadBucketAsync(new HeadBucketRequest
                    {
                        BucketName = _configuration.BucketName
                    }, cancellationToken).ConfigureAwait(false);
                }
                catch (Amazon.S3.AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // Create bucket
                    PutBucketRequest createRequest = new()
                    {
                        BucketName = _configuration.BucketName,
                        UseClientRegion = true
                    };

                    await _s3Client.PutBucketAsync(createRequest, cancellationToken).ConfigureAwait(false);

                    _logger?.LogInformation("Created S3 bucket {BucketName}", _configuration.BucketName);

                    // Configure bucket settings
                    await ConfigureBucketAsync(cancellationToken).ConfigureAwait(false);
                }

                _bucketChecked = true;
            }
            finally
            {
                _bucketLock.Release();
            }
        }

        private async Task ConfigureBucketAsync(CancellationToken cancellationToken)
        {
            // Enable versioning if configured
            if (_configuration.EnableVersioning)
            {
                try
                {
                    PutBucketVersioningRequest versioningRequest = new()
                    {
                        BucketName = _configuration.BucketName,
                        VersioningConfig = new S3BucketVersioningConfig
                        {
                            Status = VersionStatus.Enabled
                        }
                    };

                    await _s3Client.PutBucketVersioningAsync(versioningRequest, cancellationToken).ConfigureAwait(false);

                    _logger?.LogInformation("Enabled versioning for bucket {BucketName}", _configuration.BucketName);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Could not enable versioning (may require additional permissions)");
                }
            }

            // Configure lifecycle rules if enabled
            if (_configuration.EnableLifecycleRules)
            {
                try
                {
                    LifecycleConfiguration lifecycleConfig = new()
                    {
                        Rules = []
                    };

                    LifecycleRule rule = new()
                    {
                        Id = "smtp-message-lifecycle",
                        Status = LifecycleRuleStatus.Enabled,
                        Prefix = _configuration.KeyPrefix,
                        Transitions = []
                    };

                    // Add transitions
                    if (_configuration.TransitionToIADays > 0)
                    {
                        rule.Transitions.Add(new LifecycleTransition
                        {
                            Days = _configuration.TransitionToIADays,
                            StorageClass = S3StorageClass.StandardInfrequentAccess
                        });
                    }

                    if (_configuration.TransitionToGlacierDays > 0)
                    {
                        rule.Transitions.Add(new LifecycleTransition
                        {
                            Days = _configuration.TransitionToGlacierDays,
                            StorageClass = S3StorageClass.Glacier
                        });
                    }

                    // Add expiration
                    if (_configuration.ExpirationDays > 0)
                    {
                        rule.Expiration = new LifecycleRuleExpiration
                        {
                            Days = _configuration.ExpirationDays
                        };
                    }

                    lifecycleConfig.Rules.Add(rule);

                    PutLifecycleConfigurationRequest lifecycleRequest = new()
                    {
                        BucketName = _configuration.BucketName,
                        Configuration = lifecycleConfig
                    };

                    await _s3Client.PutLifecycleConfigurationAsync(lifecycleRequest, cancellationToken).ConfigureAwait(false);

                    _logger?.LogInformation("Configured lifecycle rules for bucket {BucketName}", _configuration.BucketName);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Could not configure lifecycle rules (may require additional permissions)");
                }
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

        private string? TruncateForMetadata(string? value, int maxLength = 2048)
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

        public void Dispose()
        {
            _s3Client?.Dispose();
            _bucketLock?.Dispose();
        }
    }
}