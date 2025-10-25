# Zetian.Storage.SqlServer - SQL Server Message Storage

[![NuGet-Version](https://img.shields.io/nuget/v/Zetian.Storage.SqlServer.svg?label=NuGet)](https://www.nuget.org/packages/Zetian.Storage.SqlServer)
[![NuGet-Download](https://img.shields.io/nuget/dt/Zetian.Storage.SqlServer?label=Download)](https://www.nuget.org/api/v2/package/Zetian.Storage.SqlServer)
[![License](https://img.shields.io/github/license/Taiizor/Zetian.svg?label=License)](https://github.com/Taiizor/Zetian/blob/develop/LICENSE)

SQL Server storage provider for Zetian SMTP Server. Provides high-performance message persistence to Microsoft SQL Server databases with features including automatic table creation, message compression, separate attachment storage, retry logic, and full ACID compliance. Perfect for enterprise environments requiring reliable message storage with SQL Server infrastructure.

## ⚡ Features

- 🗺️ **Automatic Setup** - Tables and indexes created automatically
- 🗜️ **Compression** - GZIP compression for message bodies
- 📎 **Attachment Handling** - Separate table for efficient attachment storage
- 🔄 **Retry Logic** - Built-in retry mechanism for transient failures
- 🔒 **ACID Compliance** - Full transactional support
- 🚀 **High Performance** - Optimized queries and indexing
- 📏 **Size Management** - Configurable message size limits
- 🔍 **Full-Text Search** - Indexed fields for fast searching
- 📦 **Batch Operations** - Efficient bulk insert/update support
- 🔐 **Security** - Support for Windows and SQL authentication

## 📦 Installation

```bash
# Install SMTP Server and Storage Provider
dotnet add package Zetian
dotnet add package Zetian.Storage.SqlServer
```

## 🚀 Quick Start

### Basic Configuration

```csharp
using Zetian.Server;
using Zetian.Storage.SqlServer.Extensions;

// Configure with connection string
var server = new SmtpServerBuilder()
    .Port(25)
    .WithSqlServerStorage("Server=localhost;Database=SmtpDb;Trusted_Connection=true;")
    .Build();

await server.StartAsync();
```

### Advanced Configuration

```csharp
var server = new SmtpServerBuilder()
    .Port(25)
    .WithSqlServerStorage(
        "Server=localhost;Database=SmtpStorage;Integrated Security=true;",
        config =>
        {
            config.TableName = "SmtpMessages";
            config.AttachmentsTableName = "SmtpAttachments";
            config.AutoCreateTables = true;
            config.EnableCompression = true;
            config.MaxMessageSizeMB = 50;
            config.UseJsonHeaders = true;
            config.EnableRetry = true;
            config.MaxRetryAttempts = 3;
        })
    .Build();
```

## 🛠️ Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `ConnectionString` | string | *required* | SQL Server connection string |
| `TableName` | string | "SmtpMessages" | Name of the messages table |
| `SchemaName` | string | "dbo" | Database schema name |
| `AutoCreateTable` | bool | true | Automatically create tables if they don't exist |
| `CompressMessageBody` | bool | false | Compress message bodies using GZIP |
| `MaxMessageSizeMB` | double | 25.0 | Maximum message size in MB (0 = unlimited) |
| `StoreAttachmentsSeparately` | bool | false | Store attachments in separate table |
| `AttachmentsTableName` | string | "SmtpAttachments" | Name of the attachments table |
| `EnableRetry` | bool | true | Enable retry for transient failures |
| `MaxRetryAttempts` | int | 3 | Maximum number of retry attempts |
| `RetryDelayMs` | int | 1000 | Delay between retries in milliseconds |
| `ConnectionTimeoutSeconds` | int | 30 | Connection timeout in seconds |
| `CommandTimeoutSeconds` | int | 30 | Command execution timeout |
| `BulkCopyTimeoutSeconds` | int | 60 | Bulk copy operation timeout |
| `LogErrors` | bool | true | Log database errors |

## 📏 Database Schema

### Messages Table

```sql
CREATE TABLE [mail].[Messages] (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    MessageId NVARCHAR(255) NOT NULL UNIQUE,
    SessionId NVARCHAR(255) NOT NULL,
    FromAddress NVARCHAR(500) NULL,
    ToAddresses NVARCHAR(MAX) NOT NULL,
    CcAddresses NVARCHAR(MAX) NULL,
    BccAddresses NVARCHAR(MAX) NULL,
    Subject NVARCHAR(1000) NULL,
    ReceivedDate DATETIME2 NOT NULL,
    MessageSize BIGINT NOT NULL,
    MessageBody VARBINARY(MAX) NOT NULL,
    IsCompressed BIT NOT NULL DEFAULT 0,
    Headers NVARCHAR(MAX) NULL,
    HasAttachments BIT NOT NULL DEFAULT 0,
    AttachmentCount INT NOT NULL DEFAULT 0,
    Priority NVARCHAR(50) NULL,
    RemoteIP NVARCHAR(45) NULL,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    INDEX IX_MessageId NONCLUSTERED (MessageId),
    INDEX IX_ReceivedDate NONCLUSTERED (ReceivedDate DESC),
    INDEX IX_FromAddress NONCLUSTERED (FromAddress),
    INDEX IX_SessionId NONCLUSTERED (SessionId)
)
```

### Attachments Table (Optional)

```sql
CREATE TABLE [mail].[Attachments] (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    MessageId NVARCHAR(255) NOT NULL,
    FileName NVARCHAR(500) NOT NULL,
    ContentType NVARCHAR(255) NOT NULL,
    FileSize BIGINT NOT NULL,
    FileContent VARBINARY(MAX) NOT NULL,
    IsCompressed BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY (MessageId) REFERENCES [mail].[Messages](MessageId) ON DELETE CASCADE,
    INDEX IX_MessageId NONCLUSTERED (MessageId)
)
```

## 🎯 Usage Examples

### Connection String Options

```csharp
// Windows Authentication
.WithSqlServerStorage(
    "Server=localhost;Database=SmtpDb;Trusted_Connection=true;",
    config => { })

// SQL Server Authentication
.WithSqlServerStorage(
    "Server=localhost;Database=SmtpDb;User Id=sa;Password=YourPassword;",
    config => { })

// Azure SQL Database
.WithSqlServerStorage(
    "Server=tcp:myserver.database.windows.net,1433;Database=SmtpDb;User Id=admin;Password=YourPassword;Encrypt=True;",
    config => { })

// With Connection Pooling
.WithSqlServerStorage(
    "Server=localhost;Database=SmtpDb;Trusted_Connection=true;Min Pool Size=5;Max Pool Size=100;",
    config => { })
```

### Retrieving Messages

```csharp
// Get message by ID
var message = await messageStore.GetMessageAsync("msg-123");

// List recent messages
var filter = new MessageFilter
{
    StartDate = DateTime.UtcNow.AddDays(-7),
    EndDate = DateTime.UtcNow,
    MaxResults = 100
};
var messages = await messageStore.ListMessagesAsync(filter);

// Count messages
var count = await messageStore.GetMessageCountAsync(filter);

// Check if message exists
var exists = await messageStore.MessageExistsAsync("msg-123");
```

### Handling Large Messages

```csharp
var server = new SmtpServerBuilder()
    .Port(25)
    .WithSqlServerStorage(
        connectionString,
        config =>
        {
            config.MaxMessageSizeMB = 100; // Allow up to 100MB
            config.EnableCompression = true; // Enable compression
            config.StoreAttachmentsSeparately = true; // Store attachments separately
            config.BulkCopyTimeoutSeconds = 120; // Increase timeout for large messages
        })
    .Build();
```

## 🔧 Performance Optimization

### Connection Pooling

```csharp
// Optimize connection pooling
.WithSqlServerStorage(
    "Server=localhost;Database=SmtpDb;" +
    "Trusted_Connection=true;" +
    "Min Pool Size=10;" +
    "Max Pool Size=100;" +
    "Connect Timeout=30;",
    config => { })
```

### Index Recommendations

```sql
-- Additional indexes for better performance
CREATE INDEX IX_Subject ON [mail].[Messages] (Subject);
CREATE INDEX IX_ToAddresses ON [mail].[Messages] (ToAddresses);
CREATE INDEX IX_CreatedAt ON [mail].[Messages] (CreatedAt DESC);

-- Full-text index for searching
CREATE FULLTEXT INDEX ON [mail].[Messages]
    (Subject, MessageBody)
    KEY INDEX PK_Messages;
```

### Maintenance Scripts

```sql
-- Clean up old messages
DELETE FROM [mail].[Messages]
WHERE ReceivedDate < DATEADD(MONTH, -6, GETUTCDATE());

-- Rebuild indexes
ALTER INDEX ALL ON [mail].[Messages] REBUILD;

-- Update statistics
UPDATE STATISTICS [mail].[Messages];
```

## ☁️ Azure SQL Database

### Connection Configuration

```csharp
.WithSqlServerStorage(
    "Server=tcp:myserver.database.windows.net,1433;" +
    "Initial Catalog=SmtpDb;" +
    "Persist Security Info=False;" +
    "User ID=admin;" +
    "Password={your_password};" +
    "MultipleActiveResultSets=False;" +
    "Encrypt=True;" +
    "TrustServerCertificate=False;" +
    "Connection Timeout=30;",
    config =>
    {
        // Azure-optimized settings
        config.EnableRetry = true;
        config.MaxRetryAttempts = 5; // More retries for cloud
        config.ConnectionTimeoutSeconds = 60; // Longer timeout
    })
```

### Using Azure AD Authentication

```csharp
// Using Azure AD Managed Identity
.WithSqlServerStorage(
    "Server=tcp:myserver.database.windows.net,1433;" +
    "Authentication=Active Directory Managed Identity;" +
    "Database=SmtpDb;",
    config => { })
```

## 🔒 Security Best Practices

### Connection String Security

```csharp
// Store in configuration
var connectionString = Configuration.GetConnectionString("SmtpDatabase");

// Use Azure Key Vault
var connectionString = await keyVaultClient.GetSecretAsync("smtp-db-connection");

// Use environment variables
var connectionString = Environment.GetEnvironmentVariable("SMTP_DB_CONNECTION");
```

### SQL Injection Prevention

The provider uses parameterized queries throughout to prevent SQL injection:

```csharp
// Safe parameterized query example (internal)
using var command = new SqlCommand(
    "SELECT * FROM Messages WHERE MessageId = @MessageId", 
    connection);
command.Parameters.AddWithValue("@MessageId", messageId);
```

## 🔍 Monitoring & Diagnostics

### Enable Logging

```csharp
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.AddDebug();
});

// Configure storage with logging
.WithSqlServerStorage(
    connectionString,
    config =>
    {
        config.LogErrors = true;
    })
```

### Performance Metrics

```sql
-- Monitor table sizes
SELECT 
    t.NAME AS TableName,
    p.rows AS RowCounts,
    SUM(a.total_pages) * 8 AS TotalSpaceKB,
    SUM(a.used_pages) * 8 AS UsedSpaceKB
FROM sys.tables t
INNER JOIN sys.indexes i ON t.OBJECT_ID = i.object_id
INNER JOIN sys.partitions p ON i.object_id = p.OBJECT_ID AND i.index_id = p.index_id
INNER JOIN sys.allocation_units a ON p.partition_id = a.container_id
WHERE t.NAME IN ('Messages', 'Attachments')
GROUP BY t.Name, p.Rows;
```

## 🎯 Migration from Other Providers

### From File System

```csharp
// Migrate existing file-based messages
var files = Directory.GetFiles("./smtp-messages", "*.eml");
foreach (var file in files)
{
    var message = ParseEmlFile(file);
    await messageStore.SaveMessageAsync(message);
}
```

### From Another Database

```csharp
// Example migration from PostgreSQL
var pgMessages = await PostgreSqlStore.GetAllMessagesAsync();
foreach (var message in pgMessages)
{
    await sqlServerStore.SaveMessageAsync(message);
}
```

## 🔧 Troubleshooting

### Common Issues

**Connection Timeout**
```csharp
// Increase connection timeout
config.ConnectionTimeoutSeconds = 60;
config.CommandTimeoutSeconds = 120;
```

**Table Creation Failed**
```csharp
// Ensure proper permissions
GRANT CREATE TABLE TO [smtp_user];
GRANT ALTER ON SCHEMA::mail TO [smtp_user];
```

**Performance Issues**
```sql
-- Check for missing indexes
SELECT * FROM sys.dm_db_missing_index_details;

-- Check for blocking
SELECT * FROM sys.dm_exec_requests WHERE blocking_session_id > 0;
```

## 📋 Requirements

- SQL Server 2016 or later (2019+ recommended)
- Azure SQL Database (all tiers)
- .NET 6.0, 7.0, 8.0, 9.0, or 10.0
- Zetian SMTP Server package
- Microsoft.Data.SqlClient package (included)

## 📚 Documentation & Support

- **Documentation**: [Zetian Documentation](https://zetian.soferity.com)
- **SQL Server Docs**: [Microsoft SQL Documentation](https://docs.microsoft.com/sql)
- **Examples**: [GitHub Examples](https://github.com/Taiizor/Zetian/tree/develop/examples)
- **Issues**: [GitHub Issues](https://github.com/Taiizor/Zetian/issues)
- **Discussions**: [GitHub Discussions](https://github.com/Taiizor/Zetian/discussions)

## 📄 License

MIT License - see [LICENSE](https://github.com/Taiizor/Zetian/blob/develop/LICENSE)

---

**Built with ❤️ for the .NET community**