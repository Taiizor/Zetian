# Zetian.Storage.Providers - Database & Cloud Storage for SMTP

[![NuGet-Version](https://img.shields.io/nuget/v/Zetian.Storage.Providers.svg?label=NuGet)](https://www.nuget.org/packages/Zetian.Storage.Providers)
[![NuGet-Download](https://img.shields.io/nuget/dt/Zetian.Storage.Providers?label=Download)](https://www.nuget.org/api/v2/package/Zetian.Storage.Providers)
[![License](https://img.shields.io/github/license/Taiizor/Zetian.svg?label=License)](https://github.com/Taiizor/Zetian/blob/develop/LICENSE)

Professional storage providers for Zetian SMTP Server, enabling persistent message storage in various databases and cloud storage systems. Store your SMTP messages in SQL Server, PostgreSQL, MongoDB, Redis, Azure Blob Storage, or Amazon S3.

## ⚡ Features

- 🗄️ **SQL Server** - Full-featured relational database storage with compression
- 🐘 **PostgreSQL** - Advanced storage with JSONB support and partitioning
- 🍃 **MongoDB** - NoSQL storage with GridFS for large attachments
- 🔴 **Redis** - High-performance caching and temporary storage
- ☁️ **Azure Blob** - Scalable cloud storage with Azure integration
- 🪣 **Amazon S3** - AWS cloud storage with lifecycle management
- 🔄 **Automatic Retry** - Built-in retry mechanism for resilience
- 🗜️ **Compression** - Optional message compression to save space
- 📊 **Auto-indexing** - Automatic index creation for optimal performance
- 🔒 **Thread-safe** - Concurrent access handling

## 📦 Installation

```bash
# Install Zetian SMTP Server (required)
dotnet add package Zetian

# Install Storage Providers
dotnet add package Zetian.Storage.Providers
```

## 🚀 Quick Start

### SQL Server Storage

```csharp
using Zetian.Server;
using Zetian.Storage.Providers.Extensions;

var server = new SmtpServerBuilder()
    .Port(25)
    .WithSqlServerStorage(
        "Server=localhost;Database=SmtpDb;Trusted_Connection=true;",
        config =>
        {
            config.TableName = "EmailMessages";
            config.CompressMessageBody = true;
            config.MaxMessageSizeMB = 50;
        })
    .Build();

await server.StartAsync();
```

### PostgreSQL Storage

```csharp
var server = new SmtpServerBuilder()
    .Port(25)
    .WithPostgreSqlStorage(
        "Host=localhost;Database=smtp_db;Username=postgres;Password=secret",
        config =>
        {
            config.UseJsonbForHeaders = true;
            config.EnablePartitioning = true;
            config.PartitionInterval = PartitionInterval.Monthly;
        })
    .Build();
```

### MongoDB Storage

```csharp
var server = new SmtpServerBuilder()
    .Port(25)
    .WithMongoDbStorage(
        "mongodb://localhost:27017",
        "smtp_database",
        config =>
        {
            config.UseGridFsForLargeMessages = true;
            config.GridFsThresholdMB = 10;
            config.EnableTTL = true;
            config.TTLDays = 90;
        })
    .Build();
```

## 🛠️ Advanced Configuration

### SQL Server with Full Options

```csharp
var sqlConfig = new SqlServerStorageConfiguration
{
    ConnectionString = "Server=localhost;Database=SmtpDb;Trusted_Connection=true;",
    TableName = "SmtpMessages",
    SchemaName = "mail",
    AutoCreateTable = true,
    CompressMessageBody = true,
    MaxMessageSizeMB = 100,
    StoreAttachmentsSeparately = true,
    AttachmentsTableName = "SmtpAttachments",
    EnableRetry = true,
    MaxRetryAttempts = 3,
    RetryDelayMs = 1000
};

var store = new SqlServerMessageStore(sqlConfig);

var server = new SmtpServerBuilder()
    .MessageStore(store)
    .Build();
```

### PostgreSQL with Partitioning

```csharp
var pgConfig = new PostgreSqlStorageConfiguration
{
    ConnectionString = "Host=localhost;Database=smtp_db;Username=postgres;Password=secret",
    TableName = "messages",
    SchemaName = "smtp",
    EnablePartitioning = true,
    PartitionInterval = PartitionInterval.Monthly,
    UseJsonbForHeaders = true,
    CreateIndexes = true,
    CompressMessageBody = false
};

var store = new PostgreSqlMessageStore(pgConfig);
```

### MongoDB with GridFS

```csharp
var mongoConfig = new MongoDbStorageConfiguration
{
    ConnectionString = "mongodb://localhost:27017",
    DatabaseName = "smtp_server",
    CollectionName = "messages",
    UseGridFsForLargeMessages = true,
    GridFsThresholdMB = 5, // Use GridFS for messages > 5MB
    GridFsBucketName = "email_attachments",
    EnableTTL = true,
    TTLDays = 30, // Auto-delete after 30 days
    EnableSharding = true,
    ShardKeyField = "received_date"
};

var store = new MongoDbMessageStore(mongoConfig);
```

## 📊 Storage Provider Comparison

| Provider | Best For | Pros | Cons |
|----------|----------|------|------|
| **SQL Server** | Enterprise environments, Windows-based systems | Full ACID compliance, great tooling, compression | License costs, Windows-centric |
| **PostgreSQL** | Open-source projects, complex queries | Free, JSONB support, partitioning | Setup complexity |
| **MongoDB** | Large attachments, flexible schemas | GridFS, TTL, horizontal scaling | Memory usage, eventual consistency |
| **Redis** | Caching, temporary storage | Ultra-fast, simple | Not for permanent storage |
| **Azure Blob** | Cloud-native apps, large scale | Unlimited storage, geo-redundancy | Cloud costs, latency |
| **S3** | AWS environments, archival | Lifecycle policies, versioning | Cloud costs, complexity |

## 🔧 Configuration Options

### Common Options (All Providers)

- `EnableRetry` - Automatic retry on failure
- `MaxRetryAttempts` - Number of retry attempts
- `RetryDelayMs` - Delay between retries
- `ConnectionTimeoutSeconds` - Connection timeout
- `LogErrors` - Error logging

### SQL Server Specific

- `StoreAttachmentsSeparately` - Separate table for attachments
- `CompressMessageBody` - GZip compression

### PostgreSQL Specific

- `UseJsonbForHeaders` - Store headers as JSONB
- `EnablePartitioning` - Table partitioning by date
- `PartitionInterval` - Daily/Monthly/Yearly

### MongoDB Specific

- `UseGridFsForLargeMessages` - GridFS for large files
- `EnableTTL` - Auto-expiration
- `EnableSharding` - Horizontal scaling

## 🏗️ Database Schema

### SQL Server Schema

```sql
CREATE TABLE SmtpMessages (
    Id BIGINT IDENTITY PRIMARY KEY,
    MessageId NVARCHAR(255) NOT NULL,
    SessionId NVARCHAR(255) NOT NULL,
    FromAddress NVARCHAR(500),
    ToAddresses NVARCHAR(MAX),
    Subject NVARCHAR(1000),
    ReceivedDate DATETIME2,
    MessageSize BIGINT,
    MessageBody VARBINARY(MAX),
    IsCompressed BIT,
    Headers NVARCHAR(MAX),
    HasAttachments BIT,
    AttachmentCount INT,
    Priority NVARCHAR(50)
)
```

### PostgreSQL Schema

```sql
CREATE TABLE smtp_messages (
    id BIGSERIAL PRIMARY KEY,
    message_id VARCHAR(255) NOT NULL,
    session_id VARCHAR(255) NOT NULL,
    from_address VARCHAR(500),
    to_addresses TEXT,
    subject VARCHAR(1000),
    received_date TIMESTAMPTZ,
    message_size BIGINT,
    message_body BYTEA,
    is_compressed BOOLEAN,
    headers JSONB,
    has_attachments BOOLEAN,
    attachment_count INTEGER,
    priority VARCHAR(50)
)
```

## 📈 Performance Tips

1. **Enable Compression** for text-heavy emails
2. **Use GridFS** in MongoDB for large attachments
3. **Enable Partitioning** in PostgreSQL for time-series data
4. **Create Indexes** on frequently queried fields
5. **Set TTL** in MongoDB to auto-clean old messages
6. **Use Connection Pooling** for database connections

## 🔍 Querying Stored Messages

### SQL Server

```csharp
using (var connection = new SqlConnection(connectionString))
{
    var messages = await connection.QueryAsync<Message>(
        "SELECT * FROM SmtpMessages WHERE ReceivedDate > @Date",
        new { Date = DateTime.UtcNow.AddDays(-7) }
    );
}
```

### MongoDB

```csharp
var filter = Builders<BsonDocument>.Filter.Gte("received_date", DateTime.UtcNow.AddDays(-7));
var messages = await collection.Find(filter).ToListAsync();
```

## 📦 Requirements

- .NET 6.0, 7.0, 8.0, 9.0, or 10.0
- Zetian SMTP Server
- Database/Storage system of choice

## 📚 Documentation & Support

- **Issues**: [GitHub Issues](https://github.com/Taiizor/Zetian/issues)
- **Examples**: [GitHub Examples](https://github.com/Taiizor/Zetian/tree/develop/examples)
- **Documentation**: [Zetian Documentation](https://zetian.soferity.com)

## 🔒 Security Considerations

- Always use **connection string encryption**
- Enable **SSL/TLS** for database connections
- Use **least-privilege** database accounts
- Consider **data encryption at rest**
- Implement **access controls** and auditing
- Regular **backup and recovery** procedures

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
