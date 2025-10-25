using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Zetian.Abstractions;
using Zetian.Storage.SqlServer.Configuration;

namespace Zetian.Storage.SqlServer.Storage
{
    /// <summary>
    /// SQL Server implementation of IMessageStore
    /// </summary>
    public class SqlServerMessageStore : IMessageStore, IDisposable
    {
        private readonly SqlServerStorageConfiguration _configuration;
        private readonly ILogger<SqlServerMessageStore>? _logger;
        private bool _tableChecked = false;
        private readonly SemaphoreSlim _tableLock = new(1, 1);

        public SqlServerMessageStore(SqlServerStorageConfiguration configuration, ILogger<SqlServerMessageStore>? logger = null)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _configuration.Validate();
            _logger = logger;
        }

        public async Task<bool> SaveAsync(ISmtpSession session, ISmtpMessage message, CancellationToken cancellationToken = default)
        {
            try
            {
                // Ensure table exists
                await EnsureTableExistsAsync(cancellationToken).ConfigureAwait(false);

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

                // Compress if configured
                byte[] dataToStore = rawData;
                bool isCompressed = false;

                if (_configuration.CompressMessageBody)
                {
                    dataToStore = CompressData(rawData);
                    isCompressed = true;
                }

                // Save to database
                using SqlConnection connection = new(_configuration.ConnectionString);
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                using SqlCommand command = connection.CreateCommand();
                command.CommandText = $@"
                    INSERT INTO {_configuration.GetFullTableName()}
                    (MessageId, SessionId, FromAddress, ToAddresses, Subject, ReceivedDate, 
                     MessageSize, MessageBody, IsCompressed, Headers, HasAttachments, AttachmentCount, Priority)
                    VALUES
                    (@MessageId, @SessionId, @FromAddress, @ToAddresses, @Subject, @ReceivedDate,
                     @MessageSize, @MessageBody, @IsCompressed, @Headers, @HasAttachments, @AttachmentCount, @Priority)";

                // Add parameters
                command.Parameters.AddWithValue("@MessageId", message.Id);
                command.Parameters.AddWithValue("@SessionId", session.Id);
                command.Parameters.AddWithValue("@FromAddress", (object?)message.From?.Address ?? DBNull.Value);
                command.Parameters.AddWithValue("@ToAddresses", string.Join(";", message.Recipients.Select(r => r.Address)));
                command.Parameters.AddWithValue("@Subject", (object?)message.Subject ?? DBNull.Value);
                command.Parameters.AddWithValue("@ReceivedDate", DateTime.UtcNow);
                command.Parameters.AddWithValue("@MessageSize", rawData.Length);
                command.Parameters.AddWithValue("@MessageBody", dataToStore);
                command.Parameters.AddWithValue("@IsCompressed", isCompressed);
                command.Parameters.AddWithValue("@Headers", SerializeHeaders(message));
                command.Parameters.AddWithValue("@HasAttachments", message.HasAttachments);
                command.Parameters.AddWithValue("@AttachmentCount", message.AttachmentCount);
                command.Parameters.AddWithValue("@Priority", message.Priority.ToString());

                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

                _logger?.LogInformation("Message {MessageId} saved to SQL Server", message.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error saving message {MessageId} to SQL Server", message.Id);

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

        private async Task EnsureTableExistsAsync(CancellationToken cancellationToken)
        {
            if (_tableChecked || !_configuration.AutoCreateTable)
            {
                return;
            }

            await _tableLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_tableChecked)
                {
                    return;
                }

                using SqlConnection connection = new(_configuration.ConnectionString);
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                // Check if table exists
                using SqlCommand checkCommand = connection.CreateCommand();
                checkCommand.CommandText = $@"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_SCHEMA = @Schema AND TABLE_NAME = @Table";

                checkCommand.Parameters.AddWithValue("@Schema", _configuration.SchemaName);
                checkCommand.Parameters.AddWithValue("@Table", _configuration.TableName);

                bool exists = (int)await checkCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) > 0;

                if (!exists)
                {
                    // Create table
                    using SqlCommand createCommand = connection.CreateCommand();
                    createCommand.CommandText = $@"
                        CREATE TABLE {_configuration.GetFullTableName()} (
                            Id BIGINT IDENTITY(1,1) PRIMARY KEY,
                            MessageId NVARCHAR(255) NOT NULL,
                            SessionId NVARCHAR(255) NOT NULL,
                            FromAddress NVARCHAR(500) NULL,
                            ToAddresses NVARCHAR(MAX) NOT NULL,
                            Subject NVARCHAR(1000) NULL,
                            ReceivedDate DATETIME2 NOT NULL,
                            MessageSize BIGINT NOT NULL,
                            MessageBody VARBINARY(MAX) NOT NULL,
                            IsCompressed BIT NOT NULL DEFAULT 0,
                            Headers NVARCHAR(MAX) NULL,
                            HasAttachments BIT NOT NULL DEFAULT 0,
                            AttachmentCount INT NOT NULL DEFAULT 0,
                            Priority NVARCHAR(50) NULL,
                            CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
                            INDEX IX_MessageId NONCLUSTERED (MessageId),
                            INDEX IX_SessionId NONCLUSTERED (SessionId),
                            INDEX IX_ReceivedDate NONCLUSTERED (ReceivedDate),
                            INDEX IX_FromAddress NONCLUSTERED (FromAddress)
                        )";

                    await createCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

                    _logger?.LogInformation("Created SQL Server table {TableName}", _configuration.GetFullTableName());
                }

                // Create attachments table if needed
                if (_configuration.StoreAttachmentsSeparately)
                {
                    checkCommand.Parameters.Clear();
                    checkCommand.Parameters.AddWithValue("@Schema", _configuration.SchemaName);
                    checkCommand.Parameters.AddWithValue("@Table", _configuration.AttachmentsTableName);

                    exists = (int)await checkCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) > 0;

                    if (!exists)
                    {
                        using SqlCommand createAttachCommand = connection.CreateCommand();
                        createAttachCommand.CommandText = $@"
                            CREATE TABLE {_configuration.GetFullAttachmentsTableName()} (
                                Id BIGINT IDENTITY(1,1) PRIMARY KEY,
                                MessageId NVARCHAR(255) NOT NULL,
                                FileName NVARCHAR(500) NOT NULL,
                                ContentType NVARCHAR(255) NULL,
                                FileSize BIGINT NOT NULL,
                                FileData VARBINARY(MAX) NOT NULL,
                                CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
                                INDEX IX_MessageId NONCLUSTERED (MessageId)
                            )";

                        await createAttachCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

                        _logger?.LogInformation("Created SQL Server attachments table {TableName}",
                            _configuration.GetFullAttachmentsTableName());
                    }
                }

                _tableChecked = true;
            }
            finally
            {
                _tableLock.Release();
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

        private string SerializeHeaders(ISmtpMessage message)
        {
            StringBuilder sb = new();
            foreach (KeyValuePair<string, string> header in message.Headers)
            {
                sb.AppendLine($"{header.Key}: {header.Value}");
            }
            return sb.ToString();
        }

        public void Dispose()
        {
            _tableLock?.Dispose();
        }
    }
}