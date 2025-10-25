# Zetian.Storage.PostgreSQL

PostgreSQL storage provider for Zetian SMTP Server.

## Installation

```bash
dotnet add package Zetian.Storage.PostgreSQL
```

## Configuration

```csharp
using Zetian.Storage.PostgreSQL.Extensions;

var server = new SmtpServerBuilder()
    .Port(25)
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
```

## Features

- **JSONB support** - Store headers as JSONB for efficient querying
- **Table partitioning** - Automatic time-based partitioning for large datasets
- **Automatic indexing** - Creates appropriate indexes for fast queries
- **Message compression** - Optional GZIP compression for message bodies
- **Schema management** - Automatic schema and table creation
- **GIN indexing** - Full-text search on JSONB headers

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| ConnectionString | string | - | PostgreSQL connection string (required) |
| TableName | string | "smtp_messages" | Name of the messages table |
| SchemaName | string | "public" | Database schema name |
| AutoCreateTable | bool | true | Automatically create tables if they don't exist |
| UseJsonbForHeaders | bool | true | Store headers as JSONB instead of text |
| EnablePartitioning | bool | false | Enable time-based table partitioning |
| PartitionInterval | enum | Monthly | Partition interval (Daily, Weekly, Monthly, Yearly) |
| CreateIndexes | bool | true | Create database indexes automatically |
| CompressMessageBody | bool | false | Compress message bodies using GZIP |
| MaxMessageSizeMB | double | 100 | Maximum message size in MB |

## Table Schema

```sql
CREATE TABLE smtp_messages (
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
    headers JSONB,
    has_attachments BOOLEAN NOT NULL DEFAULT FALSE,
    attachment_count INTEGER NOT NULL DEFAULT 0,
    priority VARCHAR(50),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);
```

## Indexes

The following indexes are created automatically when `CreateIndexes` is enabled:

- `idx_smtp_messages_message_id` - Message ID lookup
- `idx_smtp_messages_session_id` - Session tracking
- `idx_smtp_messages_received_date` - Date range queries
- `idx_smtp_messages_from_address` - Sender queries
- `idx_smtp_messages_headers` - GIN index for JSONB headers (when JSONB is enabled)

## Partitioning

When partitioning is enabled, tables are automatically partitioned by `received_date`:

```sql
-- Example monthly partition
CREATE TABLE smtp_messages_2024_01 PARTITION OF smtp_messages
    FOR VALUES FROM ('2024-01-01') TO ('2024-02-01');
```

## Requirements

- PostgreSQL 11 or later (for partitioning support)
- PostgreSQL 9.4 or later (without partitioning)
- .NET 6.0 or later

## License

MIT