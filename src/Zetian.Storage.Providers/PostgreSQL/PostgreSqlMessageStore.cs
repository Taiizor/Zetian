using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using Zetian.Abstractions;

namespace Zetian.Storage.Providers.PostgreSQL
{
    /// <summary>
    /// PostgreSQL implementation of IMessageStore
    /// </summary>
    public class PostgreSqlMessageStore : IMessageStore, IDisposable
    {
        private readonly PostgreSqlStorageConfiguration _configuration;
        private readonly ILogger<PostgreSqlMessageStore>? _logger;
        private bool _tableChecked = false;
        private readonly SemaphoreSlim _tableLock = new(1, 1);

        public PostgreSqlMessageStore(PostgreSqlStorageConfiguration configuration, ILogger<PostgreSqlMessageStore>? logger = null)
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
                var rawData = await message.GetRawDataAsync().ConfigureAwait(false);

                // Check size limit
                if (_configuration.MaxMessageSizeMB > 0)
                {
                    var sizeMB = rawData.Length / (1024.0 * 1024.0);
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
                await using var connection = new NpgsqlConnection(_configuration.ConnectionString);
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                await using var command = connection.CreateCommand();
                
                if (_configuration.UseJsonbForHeaders)
                {
                    command.CommandText = $@"
                        INSERT INTO {_configuration.GetFullTableName()}
                        (message_id, session_id, from_address, to_addresses, subject, received_date,
                         message_size, message_body, is_compressed, headers, has_attachments, attachment_count, priority)
                        VALUES
                        (@message_id, @session_id, @from_address, @to_addresses, @subject, @received_date,
                         @message_size, @message_body, @is_compressed, @headers::jsonb, @has_attachments, @attachment_count, @priority)";
                }
                else
                {
                    command.CommandText = $@"
                        INSERT INTO {_configuration.GetFullTableName()}
                        (message_id, session_id, from_address, to_addresses, subject, received_date,
                         message_size, message_body, is_compressed, headers, has_attachments, attachment_count, priority)
                        VALUES
                        (@message_id, @session_id, @from_address, @to_addresses, @subject, @received_date,
                         @message_size, @message_body, @is_compressed, @headers, @has_attachments, @attachment_count, @priority)";
                }

                // Add parameters
                command.Parameters.AddWithValue("@message_id", message.Id);
                command.Parameters.AddWithValue("@session_id", session.Id);
                command.Parameters.AddWithValue("@from_address", NpgsqlDbType.Text, (object?)message.From?.Address ?? DBNull.Value);
                command.Parameters.AddWithValue("@to_addresses", string.Join(";", message.Recipients.Select(r => r.Address)));
                command.Parameters.AddWithValue("@subject", NpgsqlDbType.Text, (object?)message.Subject ?? DBNull.Value);
                command.Parameters.AddWithValue("@received_date", DateTime.UtcNow);
                command.Parameters.AddWithValue("@message_size", rawData.Length);
                command.Parameters.AddWithValue("@message_body", dataToStore);
                command.Parameters.AddWithValue("@is_compressed", isCompressed);
                
                if (_configuration.UseJsonbForHeaders)
                {
                    command.Parameters.AddWithValue("@headers", NpgsqlDbType.Text, JsonSerializer.Serialize(message.Headers));
                }
                else
                {
                    command.Parameters.AddWithValue("@headers", SerializeHeaders(message));
                }
                
                command.Parameters.AddWithValue("@has_attachments", message.HasAttachments);
                command.Parameters.AddWithValue("@attachment_count", message.AttachmentCount);
                command.Parameters.AddWithValue("@priority", message.Priority.ToString());

                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

                _logger?.LogInformation("Message {MessageId} saved to PostgreSQL", message.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error saving message {MessageId} to PostgreSQL", message.Id);

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
                    var result = await SaveAsync(session, message, cancellationToken).ConfigureAwait(false);
                    _configuration.EnableRetry = true;
                    
                    if (result) return true;
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
                return;

            await _tableLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_tableChecked)
                    return;

                await using var connection = new NpgsqlConnection(_configuration.ConnectionString);
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                // Check if schema exists
                await using var schemaCommand = connection.CreateCommand();
                schemaCommand.CommandText = "SELECT COUNT(*) FROM information_schema.schemata WHERE schema_name = @schema";
                schemaCommand.Parameters.AddWithValue("@schema", _configuration.SchemaName);
                
                var schemaExists = (long)await schemaCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) > 0;
                if (!schemaExists)
                {
                    await using var createSchemaCommand = connection.CreateCommand();
                    createSchemaCommand.CommandText = $"CREATE SCHEMA IF NOT EXISTS \"{_configuration.SchemaName}\"";
                    await createSchemaCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }

                // Check if table exists
                await using var checkCommand = connection.CreateCommand();
                checkCommand.CommandText = @"
                    SELECT COUNT(*) 
                    FROM information_schema.tables 
                    WHERE table_schema = @schema AND table_name = @table";
                
                checkCommand.Parameters.AddWithValue("@schema", _configuration.SchemaName);
                checkCommand.Parameters.AddWithValue("@table", _configuration.TableName);

                var exists = (long)await checkCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) > 0;

                if (!exists)
                {
                    // Create table
                    await using var createCommand = connection.CreateCommand();
                    
                    var headersType = _configuration.UseJsonbForHeaders ? "JSONB" : "TEXT";
                    
                    createCommand.CommandText = $@"
                        CREATE TABLE {_configuration.GetFullTableName()} (
                            id BIGSERIAL PRIMARY KEY,
                            message_id VARCHAR(255) NOT NULL,
                            session_id VARCHAR(255) NOT NULL,
                            from_address VARCHAR(500),
                            to_addresses TEXT NOT NULL,
                            subject VARCHAR(1000),
                            received_date TIMESTAMP WITH TIME ZONE NOT NULL,
                            message_size BIGINT NOT NULL,
                            message_body BYTEA NOT NULL,
                            is_compressed BOOLEAN NOT NULL DEFAULT FALSE,
                            headers {headersType},
                            has_attachments BOOLEAN NOT NULL DEFAULT FALSE,
                            attachment_count INTEGER NOT NULL DEFAULT 0,
                            priority VARCHAR(50),
                            created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
                        )";

                    // Add partitioning if enabled
                    if (_configuration.EnablePartitioning)
                    {
                        string partitionBy = _configuration.PartitionInterval switch
                        {
                            PartitionInterval.Daily => "RANGE (received_date) PARTITION BY RANGE (DATE(received_date))",
                            PartitionInterval.Monthly => "RANGE (received_date) PARTITION BY RANGE (DATE_TRUNC('month', received_date))",
                            PartitionInterval.Yearly => "RANGE (received_date) PARTITION BY RANGE (DATE_TRUNC('year', received_date))",
                            _ => ""
                        };

                        if (!string.IsNullOrEmpty(partitionBy))
                        {
                            createCommand.CommandText += $" PARTITION BY {partitionBy}";
                        }
                    }

                    await createCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

                    // Create indexes if configured
                    if (_configuration.CreateIndexes)
                    {
                        var indexes = new[]
                        {
                            $"CREATE INDEX idx_{_configuration.TableName}_message_id ON {_configuration.GetFullTableName()} (message_id)",
                            $"CREATE INDEX idx_{_configuration.TableName}_session_id ON {_configuration.GetFullTableName()} (session_id)",
                            $"CREATE INDEX idx_{_configuration.TableName}_received_date ON {_configuration.GetFullTableName()} (received_date)",
                            $"CREATE INDEX idx_{_configuration.TableName}_from_address ON {_configuration.GetFullTableName()} (from_address)"
                        };

                        if (_configuration.UseJsonbForHeaders)
                        {
                            indexes = indexes.Append($"CREATE INDEX idx_{_configuration.TableName}_headers ON {_configuration.GetFullTableName()} USING gin (headers)").ToArray();
                        }

                        foreach (var index in indexes)
                        {
                            await using var indexCommand = connection.CreateCommand();
                            indexCommand.CommandText = index;
                            await indexCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                        }
                    }

                    _logger?.LogInformation("Created PostgreSQL table {TableName} with indexes", _configuration.GetFullTableName());
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
            using var output = new MemoryStream();
            using (var compressor = new GZipStream(output, CompressionLevel.Optimal))
            {
                compressor.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }

        private string SerializeHeaders(ISmtpMessage message)
        {
            var sb = new StringBuilder();
            foreach (var header in message.Headers)
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
