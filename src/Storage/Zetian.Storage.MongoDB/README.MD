# Zetian.Storage.MongoDB - NoSQL Message Storage

[![NuGet-Version](https://img.shields.io/nuget/v/Zetian.Storage.MongoDB.svg?label=NuGet)](https://www.nuget.org/packages/Zetian.Storage.MongoDB)
[![NuGet-Download](https://img.shields.io/nuget/dt/Zetian.Storage.MongoDB?label=Download)](https://www.nuget.org/api/v2/package/Zetian.Storage.MongoDB)
[![License](https://img.shields.io/github/license/Taiizor/Zetian.svg?label=License)](https://github.com/Taiizor/Zetian/blob/develop/LICENSE)

MongoDB NoSQL storage provider for Zetian SMTP Server. Provides flexible, schema-less message persistence with advanced features including GridFS for large attachment storage, automatic TTL-based message expiration, horizontal sharding for scalability, text indexing for full-text search, and built-in compression. Perfect for modern applications requiring flexible data models, automatic scaling, and document-oriented storage without rigid schemas.

## ⚡ Features

- 📂 **GridFS Support** - Automatic large file handling with GridFS
- ⏰ **TTL Expiration** - Automatic message cleanup after specified time
- 🔀 **Sharding** - Horizontal scaling for massive deployments
- 🌐 **Flexible Schema** - NoSQL document flexibility
- 🔍 **Text Search** - Full-text search capabilities
- 🗜️ **Compression** - Optional GZIP compression for messages
- 📊 **Aggregation** - Powerful aggregation pipeline support
- 🔄 **Change Streams** - Real-time change notifications
- 🚀 **High Performance** - Optimized for write-heavy workloads
- 🌍 **Replica Sets** - Built-in high availability support

## 📦 Installation

```bash
# Install SMTP Server and Storage Provider
dotnet add package Zetian
dotnet add package Zetian.Storage.MongoDB
```

## 🚀 Quick Start

### Basic Configuration

```csharp
using Zetian.Server;
using Zetian.Storage.MongoDB.Extensions;

// Configure with connection string and database
var server = new SmtpServerBuilder()
    .Port(25)
    .WithMongoDbStorage("mongodb://localhost:27017", "smtp_database")
    .Build();

await server.StartAsync();
```

### Advanced Configuration

```csharp
var server = new SmtpServerBuilder()
    .Port(25)
    .WithMongoDbStorage(config =>
    {
        config.ConnectionString = "mongodb://localhost:27017";
        config.DatabaseName = "smtp_database";
        config.CollectionName = "email_messages";
        config.UseGridFsForLargeMessages = true;
        config.GridFsThresholdMB = 5;
        config.EnableTTL = true;
        config.TTLDays = 30;
        config.EnableSharding = true;
        config.ShardKeyField = "received_date";
        config.CompressMessageBody = true;
        config.AutoCreateIndexes = true;
    })
    .Build();
```

## 🛠️ Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `ConnectionString` | string | *required* | MongoDB connection string |
| `DatabaseName` | string | *required* | Database name |
| `CollectionName` | string | "messages" | Collection name for messages |
| `UseGridFsForLargeMessages` | bool | true | Use GridFS for large messages |
| `GridFsThresholdMB` | double | 10.0 | Size threshold for GridFS storage |
| `GridFsBucketName` | string | "message_attachments" | GridFS bucket name |
| `EnableTTL` | bool | false | Enable automatic message expiration |
| `TTLDays` | int | 30 | Days before automatic deletion |
| `AutoCreateIndexes` | bool | true | Create indexes automatically |
| `EnableSharding` | bool | false | Enable collection sharding |
| `ShardKeyField` | string | "received_date" | Field to use as shard key |
| `CompressMessageBody` | bool | false | Compress message bodies |
| `MaxMessageSizeMB` | double | 100.0 | Maximum message size in MB |
| `WriteConcern` | string | "majority" | Write concern level |
| `ReadPreference` | string | "primary" | Read preference mode |
| `EnableRetry` | bool | true | Enable retry for transient failures |
| `MaxRetryAttempts` | int | 3 | Maximum retry attempts |
| `ConnectionTimeoutSeconds` | int | 30 | Connection timeout |

## 📏 Document Schema

```javascript
{
  _id: ObjectId("..."),
  messageId: "msg-123456",
  sessionId: "session-789",
  fromAddress: "sender@example.com",
  toAddresses: ["recipient1@example.com", "recipient2@example.com"],
  ccAddresses: [],
  bccAddresses: [],
  subject: "Email Subject",
  receivedDate: ISODate("2024-01-01T12:00:00Z"),
  messageSize: NumberLong(2048),
  messageBody: BinData(...),  // When small
  isCompressed: false,
  headers: {
    "From": "sender@example.com",
    "To": "recipient@example.com",
    "Subject": "Email Subject",
    "Message-ID": "<msg-123456@example.com>",
    "X-Priority": "Normal"
  },
  attachments: [
    {
      filename: "document.pdf",
      contentType: "application/pdf",
      size: 102400,
      gridFsId: ObjectId("...")  // If stored in GridFS
    }
  ],
  hasAttachments: true,
  attachmentCount: 1,
  priority: "normal",
  remoteIP: "192.168.1.100",
  isStoredInGridFs: false,
  gridFsId: null,  // ObjectId when body in GridFS
  createdAt: ISODate("2024-01-01T12:00:00Z"),
  metadata: {  // Custom metadata
    "processed": true,
    "spam_score": 0.1
  }
}
```

## 🎯 Usage Examples

### Connection String Options

```csharp
// Standalone server
.WithMongoDbStorage("mongodb://localhost:27017", "smtp_db")

// Replica set
.WithMongoDbStorage(
    "mongodb://node1:27017,node2:27017,node3:27017/?replicaSet=rs0", 
    "smtp_db")

// With authentication
.WithMongoDbStorage(
    "mongodb://username:password@localhost:27017/smtp_db?authSource=admin", 
    "smtp_db")

// MongoDB Atlas
.WithMongoDbStorage(
    "mongodb+srv://username:password@cluster.mongodb.net/smtp_db?retryWrites=true", 
    "smtp_db")

// With connection pooling
.WithMongoDbStorage(
    "mongodb://localhost:27017/?maxPoolSize=100&minPoolSize=10", 
    "smtp_db")
```

### GridFS for Large Messages

```csharp
// Configure GridFS
.WithMongoDbStorage(config =>
{
    config.UseGridFsForLargeMessages = true;
    config.GridFsThresholdMB = 5; // Store messages > 5MB in GridFS
    config.GridFsBucketName = "email_files";
})

// Retrieving messages with GridFS
var message = await messageStore.GetMessageAsync("msg-123");
if (message.IsStoredInGridFs)
{
    // Body automatically retrieved from GridFS
    var body = message.MessageBody;
}
```

### TTL (Time To Live) Configuration

```csharp
// Auto-delete messages after 30 days
.WithMongoDbStorage(config =>
{
    config.EnableTTL = true;
    config.TTLDays = 30;
})

// Different TTL strategies
.WithMongoDbStorage(config =>
{
    config.EnableTTL = true;
    config.TTLDays = 7;   // Delete after 1 week
    config.TTLField = "receivedDate";  // TTL based on received date
})
```

## 🔍 Querying Messages

### MongoDB Query Examples

```javascript
// Find messages from last 7 days
db.messages.find({
  receivedDate: { $gte: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000) }
})

// Full-text search
db.messages.find({
  $text: { $search: "important meeting" }
})

// Find by sender domain
db.messages.find({
  fromAddress: { $regex: "@example\.com$" }
})

// Aggregation pipeline
db.messages.aggregate([
  { $match: { receivedDate: { $gte: ISODate("2024-01-01") } } },
  { $group: {
    _id: "$fromAddress",
    count: { $sum: 1 },
    totalSize: { $sum: "$messageSize" }
  }},
  { $sort: { count: -1 } },
  { $limit: 10 }
])
```

## 🚀 Performance Optimization

### Indexing Strategy

```javascript
// Create compound indexes
db.messages.createIndex({ receivedDate: -1, fromAddress: 1 })
db.messages.createIndex({ sessionId: 1, messageId: 1 })

// Text index for full-text search
db.messages.createIndex({ subject: "text", "headers.From": "text" })

// Partial index for unread messages
db.messages.createIndex(
  { receivedDate: -1 },
  { partialFilterExpression: { "metadata.read": false } }
)

// Hashed index for sharding
db.messages.createIndex({ messageId: "hashed" })
```

### Sharding Configuration

```javascript
// Enable sharding on database
sh.enableSharding("smtp_database")

// Shard the collection
sh.shardCollection(
  "smtp_database.messages",
  { receivedDate: 1 }  // Range-based sharding
)

// Or use hashed sharding for even distribution
sh.shardCollection(
  "smtp_database.messages",
  { messageId: "hashed" }
)
```

### Write Performance

```csharp
// Optimize for write performance
.WithMongoDbStorage(config =>
{
    config.WriteConcern = "1"; // Acknowledge after primary write
    config.Journal = false; // Don't wait for journal
    config.EnableBulkWrites = true; // Batch inserts
    config.BulkWriteSize = 100; // Batch size
})

// Balance between performance and durability
.WithMongoDbStorage(config =>
{
    config.WriteConcern = "majority"; // Acknowledge after majority
    config.Journal = true; // Wait for journal
})
```

## 📊 Change Streams (Real-time)

```csharp
// Watch for new messages
var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>()
    .Match(change => change.OperationType == ChangeStreamOperationType.Insert);

using (var cursor = await collection.WatchAsync(pipeline))
{
    await cursor.ForEachAsync(change =>
    {
        Console.WriteLine($"New message: {change.FullDocument["messageId"]}");
    });
}
```

## 🌐 Replica Set Configuration

```csharp
// Connection to replica set
config.ConnectionString = 
    "mongodb://node1:27017,node2:27017,node3:27017/" +
    "?replicaSet=rs0" +
    "&readPreference=secondaryPreferred" +
    "&readConcernLevel=majority";

// Read preferences
config.ReadPreference = "secondaryPreferred"; // Read from secondaries when possible
config.ReadConcern = "majority"; // Read committed data
```

## ☁️ MongoDB Atlas Integration

```csharp
// Atlas connection
config.ConnectionString = 
    "mongodb+srv://username:password@cluster.mongodb.net/smtp_db" +
    "?retryWrites=true&w=majority";

// Atlas Search (Lucene-based)
var searchStage = new BsonDocument("$search", new BsonDocument
{
    { "index", "message_search" },
    { "text", new BsonDocument
    {
        { "query", "important" },
        { "path", new BsonArray { "subject", "messageBody" } }
    }}
});
```

## 📈 Monitoring & Maintenance

### Collection Statistics

```javascript
// Get collection stats
db.messages.stats()

// Check index usage
db.messages.aggregate([
  { $indexStats: {} }
])

// Find slow queries
db.currentOp({ 
  "secs_running": { $gte: 5 },
  "ns": "smtp_database.messages"
})
```

### Maintenance Tasks

```javascript
// Compact collection
db.runCommand({ compact: "messages" })

// Rebuild indexes
db.messages.reIndex()

// Validate collection
db.messages.validate({ full: true })

// Clean up orphaned GridFS chunks
db.fs.chunks.deleteMany({
  files_id: { $nin: db.fs.files.distinct("_id") }
})
```

## 🔒 Security Best Practices

### Authentication & Authorization

```javascript
// Create dedicated user
db.createUser({
  user: "smtp_app",
  pwd: "secure_password",
  roles: [
    { role: "readWrite", db: "smtp_database" },
    { role: "dbAdmin", db: "smtp_database" }
  ]
})

// Enable authentication in connection string
config.ConnectionString = 
    "mongodb://smtp_app:secure_password@localhost:27017/" +
    "smtp_database?authSource=smtp_database";
```

### Encryption

```csharp
// TLS/SSL connection
config.ConnectionString = 
    "mongodb://localhost:27017/?ssl=true&" +
    "sslCertificateAuthorityFile=/path/to/ca.pem";

// Field-level encryption (MongoDB 4.2+)
config.EnableFieldEncryption = true;
config.EncryptionKeyPath = "/path/to/keyfile";
```

## 🔧 Troubleshooting

### Common Issues

**Connection Timeout**
```csharp
// Increase connection timeout
config.ConnectionTimeoutSeconds = 60;
config.ServerSelectionTimeoutMs = 30000;
```

**Write Concern Errors**
```csharp
// Adjust write concern for network issues
config.WriteConcern = "1"; // Don't wait for replication
config.WriteConcernTimeoutMs = 10000;
```

**Memory Issues with Large Collections**
```javascript
// Use cursor with batch size
db.messages.find().batchSize(100).forEach(doc => {
  // Process document
})

// Limit query results
db.messages.find().limit(1000).hint({ receivedDate: -1 })
```

## 📋 Requirements

- MongoDB 4.0 or later (4.4+ recommended)
- .NET 6.0, 7.0, 8.0, 9.0, or 10.0
- Zetian SMTP Server package
- MongoDB.Driver package (included)
- For sharding: MongoDB sharded cluster
- For Atlas: MongoDB Atlas cluster

## 📚 Documentation & Support

- **Documentation**: [Zetian Documentation](https://zetian.soferity.com)
- **MongoDB Docs**: [MongoDB Documentation](https://docs.mongodb.com/)
- **Examples**: [GitHub Examples](https://github.com/Taiizor/Zetian/tree/develop/examples)
- **Issues**: [GitHub Issues](https://github.com/Taiizor/Zetian/issues)
- **Discussions**: [GitHub Discussions](https://github.com/Taiizor/Zetian/discussions)

## 📄 License

MIT License - see [LICENSE](https://github.com/Taiizor/Zetian/blob/develop/LICENSE)

---

**Built with ❤️ for the .NET community**