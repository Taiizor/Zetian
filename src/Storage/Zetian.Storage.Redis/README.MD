# Zetian.Storage.Redis - High-Performance Cache Storage

[![NuGet-Version](https://img.shields.io/nuget/v/Zetian.Storage.Redis.svg?label=NuGet)](https://www.nuget.org/packages/Zetian.Storage.Redis)
[![NuGet-Download](https://img.shields.io/nuget/dt/Zetian.Storage.Redis?label=Download)](https://www.nuget.org/api/v2/package/Zetian.Storage.Redis)
[![License](https://img.shields.io/github/license/Taiizor/Zetian.svg?label=License)](https://github.com/Taiizor/Zetian/blob/develop/LICENSE)

Redis cache and storage provider for Zetian SMTP Server. Delivers ultra-fast, in-memory message caching and temporary storage with advanced features including automatic chunking for large messages, Redis Streams for event-driven architectures, Pub/Sub notifications for real-time updates, sorted set indexing, TTL-based expiration, and built-in compression. Ideal for high-throughput environments requiring lightning-fast message access and real-time event processing.

## ⚡ Features

- 🚀 **Ultra-Fast Performance** - In-memory storage with sub-millisecond latency
- 📦 **Chunking Support** - Automatic splitting of large messages
- 🌊 **Redis Streams** - Event-driven message processing
- 📢 **Pub/Sub** - Real-time message notifications
- ⏱️ **TTL Expiration** - Automatic cache cleanup
- 📊 **Sorted Sets** - Efficient time-based queries
- 🗜️ **Compression** - Optional GZIP compression
- 🔄 **Pipeline Support** - Batch operations for efficiency
- 🌍 **Cluster Support** - Horizontal scaling with Redis Cluster
- 💾 **Persistence Options** - RDB and AOF support

## 📦 Installation

```bash
# Install SMTP Server and Storage Provider
dotnet add package Zetian
dotnet add package Zetian.Storage.Redis
```

## 🚀 Quick Start

### Basic Configuration

```csharp
using Zetian.Server;
using Zetian.Storage.Redis.Extensions;

// Configure with connection string
var server = new SmtpServerBuilder()
    .Port(25)
    .WithRedisStorage("localhost:6379")
    .Build();

await server.StartAsync();
```

### Advanced Configuration

```csharp
var server = new SmtpServerBuilder()
    .Port(25)
    .WithRedisStorage(config =>
    {
        config.ConnectionString = "localhost:6379,password=mypassword";
        config.DatabaseNumber = 1;
        config.KeyPrefix = "smtp:msg:";
        config.MessageTTLSeconds = 3600; // 1 hour cache
        config.EnableChunking = true;
        config.ChunkSizeKB = 64;
        config.UseRedisStreams = true;
        config.EnablePubSub = true;
        config.MaintainIndex = true;
        config.CompressMessageBody = true;
    })
    .Build();
```

## 🛠️ Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| ConnectionString | string | - | Redis connection string (required) |
| DatabaseNumber | int | 0 | Redis database number (0-15) |
| KeyPrefix | string | "smtp:" | Prefix for all Redis keys |
| MessageTTLSeconds | int | 3600 | Message expiration time in seconds |
| EnableChunking | bool | true | Split large messages into chunks |
| ChunkSizeKB | int | 64 | Size of each chunk in KB |
| UseRedisStreams | bool | false | Use Redis Streams for storage |
| StreamKey | string | "smtp:stream" | Key for Redis Stream |
| EnablePubSub | bool | false | Enable Pub/Sub notifications |
| PubSubChannel | string | "smtp:notifications" | Channel for notifications |
| MaintainIndex | bool | true | Maintain sorted set index |
| IndexKey | string | "smtp:index" | Key for sorted set index |
| CompressMessageBody | bool | false | Compress message bodies |
| MaxMessageSizeMB | double | 100 | Maximum message size in MB |

## 🎯 Usage Examples

### Connection String Options

```csharp
// Standalone server
.WithRedisStorage("localhost:6379")

// With password
.WithRedisStorage("localhost:6379,password=mypassword")

// Multiple endpoints (replica)
.WithRedisStorage("server1:6379,server2:6379")

// With SSL/TLS
.WithRedisStorage("localhost:6379,ssl=true,sslHost=redis.example.com")

// Redis Sentinel
.WithRedisStorage("sentinel1:26379,sentinel2:26379,serviceName=mymaster")

// Redis Cluster
.WithRedisStorage("cluster1:7000,cluster2:7001,cluster3:7002")
```

### Storage Strategies

#### Key-Value Storage (Default)

```csharp
// Messages stored as regular Redis keys
config.UseRedisStreams = false;

// Storage pattern:
// smtp:msg:{messageId}        - Message data
// smtp:meta:{messageId}       - Message metadata
// smtp:chunk:{messageId}:{n}  - Message chunks (if chunked)
```

#### Redis Streams

```csharp
// Use Redis Streams for event-driven architectures
config.UseRedisStreams = true;
config.StreamKey = "smtp:stream";
config.StreamMaxLength = 10000; // Trim to prevent unlimited growth

// Consume messages from stream
var entries = await db.StreamReadAsync("smtp:stream", "0-0");
```

#### Chunking for Large Messages

```csharp
// Automatically chunk large messages
config.EnableChunking = true;
config.ChunkSizeKB = 64; // 64KB chunks
config.MaxMessageSizeMB = 50; // Support up to 50MB messages
```

## 📊 Redis Data Structures

### Message Storage

```lua
-- Key-Value storage
SET smtp:msg:123 "<message_body>"
HSET smtp:meta:123 "from" "sender@example.com" "size" "2048"

-- Chunked storage
SET smtp:chunk:123:0 "<chunk_0>"
SET smtp:chunk:123:1 "<chunk_1>"
HSET smtp:meta:123 "chunks" "2"

-- Sorted Set Index
ZADD smtp:index <timestamp> "123"

-- Redis Stream
XADD smtp:stream * messageId "123" data "<message_data>"
```

### Pub/Sub Notifications

```csharp
// Enable real-time notifications
config.EnablePubSub = true;
config.PubSubChannel = "smtp:notifications";

// Subscribe to notifications
var subscriber = redis.GetSubscriber();
subscriber.Subscribe("smtp:notifications", (channel, message) =>
{
    var notification = JsonSerializer.Deserialize<MessageNotification>(message);
    Console.WriteLine($"New message: {notification.MessageId}");
});

// Notification format
{
  "Event": "MessageStored",
  "MessageId": "msg-123",
  "Timestamp": "2024-01-01T12:00:00Z",
  "Size": 2048
}
```

## 🚀 Performance Optimization

### Connection Pooling

```csharp
config.ConnectionString = "localhost:6379," +
    "connectTimeout=5000," +
    "syncTimeout=5000," +
    "asyncTimeout=5000," +
    "keepAlive=60," +
    "abortConnect=false";
```

### Pipeline Operations

```csharp
// Batch multiple operations
var batch = db.CreateBatch();
var tasks = new List<Task>();

foreach (var message in messages)
{
    tasks.Add(batch.StringSetAsync($"smtp:msg:{message.Id}", message.Body));
    tasks.Add(batch.HashSetAsync($"smtp:meta:{message.Id}", metadata));
}

batch.Execute();
await Task.WhenAll(tasks);
```

### Memory Optimization

```csharp
// Use compression for large messages
config.CompressMessageBody = true;

// Set appropriate TTL
config.MessageTTLSeconds = 3600; // 1 hour

// Use eviction policies
// In redis.conf: maxmemory-policy allkeys-lru
```

## 🌍 Redis Cluster Support

```csharp
// Configure for Redis Cluster
config.ConnectionString = 
    "node1:7000,node2:7001,node3:7002," +
    "connectTimeout=5000," +
    "configCheckSeconds=60";

// Use hash tags for related keys
config.KeyPrefix = "smtp:{msg}:"; // All message keys on same slot
```

## 📈 Monitoring & Metrics

### Redis INFO Command

```csharp
// Get Redis server statistics
var info = await db.ExecuteAsync("INFO", "memory");
Console.WriteLine($"Memory usage: {info}");
```

### Custom Metrics

```csharp
// Track message count
await db.StringIncrementAsync("smtp:stats:message_count");

// Track total size
await db.StringIncrementAsync("smtp:stats:total_size", message.Size);

// Get statistics
var count = await db.StringGetAsync("smtp:stats:message_count");
var totalSize = await db.StringGetAsync("smtp:stats:total_size");
```

### Redis Monitor

```bash
# Monitor Redis commands in real-time
redis-cli monitor

# Check memory usage
redis-cli info memory

# Check connected clients
redis-cli client list
```

## ☁️ Cloud Redis Services

### Azure Cache for Redis

```csharp
config.ConnectionString = 
    "myredis.redis.cache.windows.net:6380," +
    "password=<access_key>," +
    "ssl=True," +
    "abortConnect=False";
```

### AWS ElastiCache

```csharp
config.ConnectionString = 
    "mycluster.abc123.ng.0001.use1.cache.amazonaws.com:6379";

// With encryption in-transit
config.ConnectionString = 
    "mycluster.abc123.ng.0001.use1.cache.amazonaws.com:6379," +
    "ssl=true";
```

### Google Cloud Memorystore

```csharp
config.ConnectionString = 
    "10.0.0.3:6379"; // Internal IP

// With AUTH
config.ConnectionString = 
    "10.0.0.3:6379,password=AUTH_STRING";
```

## 🔐 Security Best Practices

### Authentication

```csharp
// Use strong passwords
config.ConnectionString = "localhost:6379,password=StrongPassword123!";

// Use ACL (Redis 6.0+)
config.ConnectionString = "localhost:6379,user=smtp_app,password=AppPassword";
```

### Encryption

```csharp
// Enable SSL/TLS
config.ConnectionString = 
    "localhost:6379," +
    "ssl=true," +
    "sslHost=redis.example.com," +
    "sslProtocols=Tls12";
```

### Network Security

```bash
# Bind to specific interface (redis.conf)
bind 127.0.0.1 ::1

# Enable protected mode
protected-mode yes

# Firewall rules
iptables -A INPUT -p tcp --dport 6379 -s 10.0.0.0/8 -j ACCEPT
iptables -A INPUT -p tcp --dport 6379 -j DROP
```

## 🔧 Troubleshooting

### Common Issues

**Connection Timeout**
```csharp
// Increase timeout values
config.ConnectionString = "localhost:6379," +
    "connectTimeout=10000," +
    "syncTimeout=10000";
```

**Memory Issues**
```bash
# Check memory usage
redis-cli --bigkeys
redis-cli --memkeys

# Set memory limit
CONFIG SET maxmemory 2gb
CONFIG SET maxmemory-policy allkeys-lru
```

**Performance Issues**
```csharp
// Use pipelining
config.EnablePipelining = true;

// Reduce round trips
config.BatchSize = 100;

// Monitor slow queries
// redis-cli: SLOWLOG GET 10
```

## 📋 Requirements

- Redis 5.0 or later (for Streams support)
- Redis 3.2 or later (without Streams)
- Redis 6.0 or later (for ACL support)
- .NET 6.0, 7.0, 8.0, 9.0, or 10.0
- Zetian SMTP Server package
- StackExchange.Redis package (included)

## 📚 Documentation & Support

- **Documentation**: [Zetian Documentation](https://zetian.soferity.com)
- **Redis Docs**: [Redis Documentation](https://redis.io/documentation)
- **Examples**: [GitHub Examples](https://github.com/Taiizor/Zetian/tree/develop/examples)
- **Issues**: [GitHub Issues](https://github.com/Taiizor/Zetian/issues)
- **Discussions**: [GitHub Discussions](https://github.com/Taiizor/Zetian/discussions)

## 📄 License

MIT License - see [LICENSE](https://github.com/Taiizor/Zetian/blob/develop/LICENSE)

---

**Built with ❤️ for the .NET community**