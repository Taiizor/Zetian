# Zetian.Storage.PostgreSQL - PostgreSQL Message Storage

[![NuGet-Version](https://img.shields.io/nuget/v/Zetian.Storage.PostgreSQL.svg?label=NuGet)](https://www.nuget.org/packages/Zetian.Storage.PostgreSQL)
[![NuGet-Download](https://img.shields.io/nuget/dt/Zetian.Storage.PostgreSQL?label=Download)](https://www.nuget.org/api/v2/package/Zetian.Storage.PostgreSQL)
[![License](https://img.shields.io/github/license/Taiizor/Zetian.svg?label=License)](https://github.com/Taiizor/Zetian/blob/develop/LICENSE)

PostgreSQL storage provider for Zetian SMTP Server. Provides enterprise-grade message persistence to PostgreSQL databases with advanced features including JSONB header storage, automatic table partitioning, GIN indexing for full-text search, message compression, and built-in retry logic. Ideal for high-volume environments requiring scalable, open-source database storage with powerful querying capabilities.

## ⚡ Features

- 🎉 **JSONB Support** - Native JSONB storage for headers with fast querying
- 📈 **Table Partitioning** - Automatic time-based partitioning for scalability
- 🔍 **GIN Indexing** - Full-text search capabilities on JSONB fields
- 🗺️ **Automatic Setup** - Tables, partitions, and indexes created automatically
- 🗜️ **Compression** - Optional GZIP compression for message bodies
- 🌐 **Open Source** - Built on PostgreSQL, the world's most advanced open source database
- 🔄 **Retry Logic** - Built-in retry mechanism for transient failures
- 🗿 **Schema Support** - Organize tables in custom schemas
- 📦 **Batch Operations** - Efficient bulk insert with COPY command
- 🎯 **Advanced Queries** - Leverage PostgreSQL's powerful query capabilities

## 📦 Installation

```bash
# Install SMTP Server and Storage Provider
dotnet add package Zetian
dotnet add package Zetian.Storage.PostgreSQL
```

## 🚀 Quick Start

### Basic Configuration

```csharp
using Zetian.Server;
using Zetian.Storage.PostgreSQL.Extensions;

// Configure with connection string
var server = new SmtpServerBuilder()
    .Port(25)
    .WithPostgreSqlStorage("Host=localhost;Database=smtp_db;Username=postgres;Password=secret")
    .Build();

await server.StartAsync();
```

### Advanced Configuration

```csharp
var server = new SmtpServerBuilder()
    .Port(25)
    .WithPostgreSqlStorage(config =>
    {
        config.ConnectionString = "Host=localhost;Database=smtp_db;Username=postgres;Password=secret";
        config.SchemaName = "mail";
        config.TableName = "messages";
        config.UseJsonbForHeaders = true;
        config.EnablePartitioning = true;
        config.PartitionInterval = PartitionInterval.Monthly;
        config.CreateIndexes = true;
        config.CompressMessageBody = true;
        config.MaxMessageSizeMB = 100;
        config.EnableRetry = true;
    })
    .Build();
```

## 🛠️ Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `ConnectionString` | string | *required* | PostgreSQL connection string |
| `TableName` | string | "smtp_messages" | Name of the messages table |
| `SchemaName` | string | "public" | Database schema name |
| `AutoCreateTable` | bool | true | Automatically create tables if they don't exist |
| `UseJsonbForHeaders` | bool | true | Store headers as JSONB for advanced querying |
| `EnablePartitioning` | bool | false | Enable time-based table partitioning |
| `PartitionInterval` | enum | Monthly | Partition interval (Daily, Weekly, Monthly, Yearly) |
| `CreateIndexes` | bool | true | Create database indexes automatically |
| `CompressMessageBody` | bool | false | Compress message bodies using GZIP |
| `MaxMessageSizeMB` | double | 100 | Maximum message size in MB |
| `EnableRetry` | bool | true | Enable retry for transient failures |
| `MaxRetryAttempts` | int | 3 | Maximum number of retry attempts |
| `ConnectionTimeoutSeconds` | int | 30 | Connection timeout in seconds |
| `CommandTimeoutSeconds` | int | 30 | Command execution timeout |
| `LogErrors` | bool | true | Log database errors |

## 📏 Database Schema

### Messages Table

```sql
CREATE TABLE mail.messages (
    id BIGSERIAL PRIMARY KEY,
    message_id VARCHAR(255) NOT NULL UNIQUE,
    session_id VARCHAR(255) NOT NULL,
    from_address VARCHAR(500),
    to_addresses TEXT NOT NULL,
    cc_addresses TEXT,
    bcc_addresses TEXT,
    subject VARCHAR(1000),
    received_date TIMESTAMP WITH TIME ZONE NOT NULL,
    message_size BIGINT NOT NULL,
    message_body BYTEA NOT NULL,
    is_compressed BOOLEAN NOT NULL DEFAULT FALSE,
    headers JSONB,
    has_attachments BOOLEAN NOT NULL DEFAULT FALSE,
    attachment_count INTEGER NOT NULL DEFAULT 0,
    attachments JSONB,
    priority VARCHAR(50),
    remote_ip INET,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
) PARTITION BY RANGE (received_date);  -- When partitioning is enabled

-- Indexes
CREATE INDEX idx_messages_message_id ON mail.messages (message_id);
CREATE INDEX idx_messages_received_date ON mail.messages (received_date DESC);
CREATE INDEX idx_messages_from_address ON mail.messages (from_address);
CREATE INDEX idx_messages_session_id ON mail.messages (session_id);
CREATE INDEX idx_messages_headers ON mail.messages USING GIN (headers);  -- JSONB GIN index
```

## 🎯 Usage Examples

### Connection String Options

```csharp
// Basic connection
.WithPostgreSqlStorage("Host=localhost;Database=smtp_db;Username=postgres;Password=secret")

// With SSL
.WithPostgreSqlStorage("Host=localhost;Database=smtp_db;Username=postgres;Password=secret;SSL Mode=Require")

// With connection pooling
.WithPostgreSqlStorage("Host=localhost;Database=smtp_db;Username=postgres;Password=secret;Pooling=true;Minimum Pool Size=5;Maximum Pool Size=100")

// With specific schema
.WithPostgreSqlStorage("Host=localhost;Database=smtp_db;Username=postgres;Password=secret;Search Path=mail,public")
```

### JSONB Header Queries

```sql
-- Find messages with specific header
SELECT * FROM mail.messages 
WHERE headers @> '{"X-Priority": "High"}';

-- Find messages from specific domain
SELECT * FROM mail.messages 
WHERE headers->>'From' LIKE '%@example.com';

-- Complex JSONB query
SELECT message_id, headers->>'Subject' as subject
FROM mail.messages
WHERE headers @> '{"X-Spam-Score": "0"}'
AND received_date > NOW() - INTERVAL '7 days';
```

### Table Partitioning

```csharp
// Enable monthly partitioning
.WithPostgreSqlStorage(config =>
{
    config.EnablePartitioning = true;
    config.PartitionInterval = PartitionInterval.Monthly;
})

// Enable daily partitioning for high volume
.WithPostgreSqlStorage(config =>
{
    config.EnablePartitioning = true;
    config.PartitionInterval = PartitionInterval.Daily;
})
```

### Partition Management

```sql
-- Create future partitions
CREATE TABLE mail.messages_2024_02 PARTITION OF mail.messages
    FOR VALUES FROM ('2024-02-01') TO ('2024-03-01');

CREATE TABLE mail.messages_2024_03 PARTITION OF mail.messages
    FOR VALUES FROM ('2024-03-01') TO ('2024-04-01');

-- List partitions
SELECT 
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) as size
FROM pg_tables 
WHERE tablename LIKE 'messages_%'
ORDER BY tablename;

-- Drop old partitions
DROP TABLE mail.messages_2023_01;
```

## 🔧 Performance Optimization

### Connection Pooling

```csharp
config.ConnectionString = "Host=localhost;Database=smtp_db;" +
    "Username=postgres;Password=secret;" +
    "Pooling=true;" +
    "Minimum Pool Size=10;" +
    "Maximum Pool Size=100;" +
    "Connection Idle Lifetime=300;";
```

### Additional Indexes

```sql
-- Full-text search index
CREATE INDEX idx_messages_fulltext ON mail.messages 
USING GIN (to_tsvector('english', subject || ' ' || message_body));

-- Composite index for common queries
CREATE INDEX idx_messages_date_from ON mail.messages (received_date DESC, from_address);

-- Partial index for unread messages
CREATE INDEX idx_messages_unread ON mail.messages (received_date DESC) 
WHERE headers->>'X-Read' IS NULL;
```

### Query Optimization

```sql
-- Use EXPLAIN ANALYZE to optimize queries
EXPLAIN (ANALYZE, BUFFERS) 
SELECT * FROM mail.messages 
WHERE received_date > NOW() - INTERVAL '7 days';

-- Vacuum and analyze for statistics
VACUUM ANALYZE mail.messages;

-- Auto-vacuum configuration
ALTER TABLE mail.messages SET (
    autovacuum_vacuum_scale_factor = 0.1,
    autovacuum_analyze_scale_factor = 0.05
);
```

## 🌐 Replication & High Availability

### Streaming Replication Setup

```csharp
// Primary server
.WithPostgreSqlStorage("Host=primary.db;Database=smtp_db;Username=postgres;Password=secret")

// Read replica for queries
.WithPostgreSqlStorage("Host=replica.db;Database=smtp_db;Username=postgres;Password=secret;Target Session Attributes=read-only")
```

### Connection Failover

```csharp
// Multiple hosts with failover
config.ConnectionString = 
    "Host=primary.db,replica1.db,replica2.db;" +
    "Database=smtp_db;" +
    "Username=postgres;" +
    "Password=secret;" +
    "Target Session Attributes=read-write;" +
    "Load Balance Hosts=true;";
```

## 📊 Advanced Features

### LISTEN/NOTIFY for Real-time Updates

```sql
-- Create trigger for notifications
CREATE OR REPLACE FUNCTION notify_new_message() RETURNS trigger AS $$
BEGIN
    PERFORM pg_notify('new_message', NEW.message_id::text);
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER message_notify 
AFTER INSERT ON mail.messages 
FOR EACH ROW EXECUTE FUNCTION notify_new_message();
```

```csharp
// Listen for notifications in C#
await using var conn = new NpgsqlConnection(connectionString);
await conn.OpenAsync();
conn.Notification += (o, e) => 
{
    Console.WriteLine($"New message: {e.Payload}");
};
await using (var cmd = new NpgsqlCommand("LISTEN new_message", conn))
    await cmd.ExecuteNonQueryAsync();
```

### Row-Level Security

```sql
-- Enable row-level security
ALTER TABLE mail.messages ENABLE ROW LEVEL SECURITY;

-- Create policy for user access
CREATE POLICY user_messages ON mail.messages
    FOR ALL
    TO smtp_user
    USING (to_addresses LIKE '%' || current_user || '%');
```

## 🔍 Monitoring & Maintenance

### Health Checks

```sql
-- Check table sizes
SELECT 
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) as size,
    n_live_tup as row_count
FROM pg_stat_user_tables 
WHERE schemaname = 'mail';

-- Check slow queries
SELECT 
    query,
    calls,
    mean_exec_time,
    total_exec_time
FROM pg_stat_statements 
WHERE query LIKE '%messages%'
ORDER BY mean_exec_time DESC
LIMIT 10;
```

### Maintenance Tasks

```sql
-- Reindex periodically
REINDEX TABLE mail.messages;

-- Update table statistics
ANALYZE mail.messages;

-- Clean up old messages
DELETE FROM mail.messages 
WHERE received_date < NOW() - INTERVAL '6 months';

-- Vacuum to reclaim space
VACUUM (VERBOSE, ANALYZE) mail.messages;
```

## ☁️ Cloud Deployments

### Amazon RDS PostgreSQL

```csharp
config.ConnectionString = 
    "Host=mydb.123456789012.us-east-1.rds.amazonaws.com;" +
    "Port=5432;" +
    "Database=smtp_db;" +
    "Username=postgres;" +
    "Password=secret;" +
    "SSL Mode=Require;";
```

### Azure Database for PostgreSQL

```csharp
config.ConnectionString = 
    "Host=myserver.postgres.database.azure.com;" +
    "Database=smtp_db;" +
    "Username=postgres@myserver;" +
    "Password=secret;" +
    "SSL Mode=Require;";
```

### Google Cloud SQL

```csharp
config.ConnectionString = 
    "Host=/cloudsql/project:region:instance;" +
    "Database=smtp_db;" +
    "Username=postgres;" +
    "Password=secret;";
```

## 🔧 Troubleshooting

### Common Issues

**Connection Refused**
```bash
# Check PostgreSQL is running
sudo systemctl status postgresql

# Check pg_hba.conf for authentication
sudo nano /etc/postgresql/14/main/pg_hba.conf
```

**Permission Denied**
```sql
-- Grant necessary permissions
GRANT ALL PRIVILEGES ON DATABASE smtp_db TO smtp_user;
GRANT ALL ON SCHEMA mail TO smtp_user;
GRANT ALL ON ALL TABLES IN SCHEMA mail TO smtp_user;
GRANT USAGE ON ALL SEQUENCES IN SCHEMA mail TO smtp_user;
```

**Partition Creation Failed**
```sql
-- Check if table is already partitioned
SELECT relname, relkind 
FROM pg_class 
WHERE relname = 'messages';

-- Convert existing table to partitioned
-- (Requires PostgreSQL 11+ and data migration)
```

## 📋 Requirements

- PostgreSQL 11 or later (for partitioning)
- PostgreSQL 9.4 or later (without partitioning)
- .NET 6.0, 7.0, 8.0, 9.0, or 10.0
- Zetian SMTP Server package
- Npgsql package (included)

## 📚 Documentation & Support

- **Documentation**: [Zetian Documentation](https://zetian.soferity.com)
- **PostgreSQL Docs**: [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- **Examples**: [GitHub Examples](https://github.com/Taiizor/Zetian/tree/develop/examples)
- **Issues**: [GitHub Issues](https://github.com/Taiizor/Zetian/issues)
- **Discussions**: [GitHub Discussions](https://github.com/Taiizor/Zetian/discussions)

## 📄 License

MIT License - see [LICENSE](https://github.com/Taiizor/Zetian/blob/develop/LICENSE)

---

**Built with ❤️ for the .NET community**