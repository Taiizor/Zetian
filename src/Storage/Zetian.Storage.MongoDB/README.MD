# Zetian.Storage.MongoDB

MongoDB NoSQL storage provider for Zetian SMTP Server.

## Installation

```bash
dotnet add package Zetian.Storage.MongoDB
```

## Configuration

```csharp
using Zetian.Storage.MongoDB.Extensions;

var server = new SmtpServerBuilder()
    .Port(25)
    .WithMongoDbStorage(
        "mongodb://localhost:27017",
        "smtp_database",
        config =>
        {
            config.CollectionName = "email_messages";
            config.UseGridFsForLargeMessages = true;
            config.GridFsThresholdMB = 5;
            config.EnableTTL = true;
            config.TTLDays = 30;
            config.EnableSharding = true;
            config.ShardKeyField = "received_date";
        })
    .Build();
```

## Features

- **GridFS support** - Automatically store large messages in GridFS
- **TTL (Time To Live)** - Automatic message expiration
- **Sharding support** - Horizontal scaling for large deployments
- **Flexible schema** - NoSQL flexibility for varying message structures
- **Automatic indexing** - Creates appropriate indexes for performance
- **Message compression** - Optional GZIP compression

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| ConnectionString | string | - | MongoDB connection string (required) |
| DatabaseName | string | - | Database name (required) |
| CollectionName | string | "messages" | Collection name for messages |
| UseGridFsForLargeMessages | bool | true | Use GridFS for large messages |
| GridFsThresholdMB | double | 10 | Size threshold for GridFS storage |
| GridFsBucketName | string | "message_attachments" | GridFS bucket name |
| EnableTTL | bool | false | Enable automatic message expiration |
| TTLDays | int | 30 | Days before automatic deletion |
| AutoCreateIndexes | bool | true | Create indexes automatically |
| EnableSharding | bool | false | Enable collection sharding |
| ShardKeyField | string | "received_date" | Field to use as shard key |
| CompressMessageBody | bool | false | Compress message bodies |
| MaxMessageSizeMB | double | 100 | Maximum message size in MB |

## Document Schema

```javascript
{
  _id: ObjectId,
  messageId: String,
  sessionId: String,
  fromAddress: String,
  toAddresses: [String],
  subject: String,
  receivedDate: Date,
  messageSize: Long,
  messageBody: BinData,  // When not in GridFS
  isCompressed: Boolean,
  headers: Object,
  hasAttachments: Boolean,
  attachmentCount: Number,
  priority: String,
  isStoredInGridFs: Boolean,
  gridFsId: ObjectId,  // When stored in GridFS
  createdAt: Date
}
```

## Indexes

The following indexes are created automatically:

- `messageId` - Ascending index for message lookups
- `sessionId` - Ascending index for session tracking
- `receivedDate` - Descending index for date queries
- `fromAddress` - Ascending index for sender queries
- `subject` - Text index for full-text search
- TTL index on `receivedDate` (when TTL is enabled)

## GridFS Usage

Messages larger than the configured threshold are automatically stored in GridFS:

- Small messages (< threshold): Stored directly in the document
- Large messages (>= threshold): Stored in GridFS with reference in document
- Attachments: Can be configured to always use GridFS

## Sharding

To enable sharding:

1. Configure sharding in MongoDB cluster
2. Set `EnableSharding = true` in configuration
3. Choose appropriate `ShardKeyField` (default: received_date)

## Requirements

- MongoDB 4.0 or later
- .NET 6.0 or later

## License

MIT