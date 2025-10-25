# Zetian.Storage.SqlServer

SQL Server storage provider for Zetian SMTP Server.

## Installation

```bash
dotnet add package Zetian.Storage.SqlServer
```

## Configuration

```csharp
using Zetian.Storage.SqlServer.Extensions;

var server = new SmtpServerBuilder()
    .Port(25)
    .WithSqlServerStorage(
        "Server=localhost;Database=SmtpDb;Trusted_Connection=true;",
        config =>
        {
            config.SchemaName = "mail";
            config.TableName = "Messages";
            config.CompressMessageBody = true;
            config.MaxMessageSizeMB = 50;
            config.StoreAttachmentsSeparately = true;
        })
    .Build();
```

## Features

- **Automatic table creation** - Tables are created automatically if they don't exist
- **Message compression** - Optional GZIP compression for message bodies
- **Separate attachment storage** - Store attachments in a separate table
- **Configurable retry logic** - Built-in retry mechanism for transient failures
- **Size limits** - Configurable maximum message size
- **Full-text indexing** - Indexes on key fields for fast searching

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| ConnectionString | string | - | SQL Server connection string (required) |
| TableName | string | "SmtpMessages" | Name of the messages table |
| SchemaName | string | "dbo" | Database schema name |
| AutoCreateTable | bool | true | Automatically create tables if they don't exist |
| CompressMessageBody | bool | false | Compress message bodies using GZIP |
| MaxMessageSizeMB | int | 25 | Maximum message size in MB (0 = unlimited) |
| StoreAttachmentsSeparately | bool | false | Store attachments in separate table |
| AttachmentsTableName | string | "SmtpAttachments" | Name of the attachments table |

## Table Schema

### Messages Table

```sql
CREATE TABLE [mail].[Messages] (
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
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
)
```

## Requirements

- SQL Server 2016 or later
- .NET 6.0 or later

## License

MIT
