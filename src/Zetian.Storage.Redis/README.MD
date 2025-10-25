# Zetian.Storage.Redis

Redis cache and storage provider for Zetian SMTP Server.

## Installation

```bash
dotnet add package Zetian.Storage.Redis
```

## Configuration

```csharp
using Zetian.Storage.Redis.Extensions;

var server = new SmtpServerBuilder()
    .Port(2525)
    .WithRedisStorage(
        "localhost:6379,password=mypassword",
        config =>
        {
            config.DatabaseNumber = 1;
            config.KeyPrefix = "smtp:msg:";
            config.MessageTTLSeconds = 3600; // 1 hour cache
            config.EnableChunking = true;
            config.ChunkSizeKB = 64;
            config.UseRedisStreams = true;
            config.EnablePubSub = true;
            config.MaintainIndex = true;
        })
    .Build();
```

## Features

- **High-performance caching** - In-memory storage with Redis
- **Chunking support** - Split large messages into chunks
- **Redis Streams** - Stream-based message storage
- **Pub/Sub notifications** - Real-time message notifications
- **TTL support** - Automatic message expiration
- **Sorted set indexing** - Fast message retrieval by date
- **Message compression** - Optional GZIP compression

## Configuration Options

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

## Storage Strategies

### Key-Value Storage (Default)

Messages are stored as regular Redis keys with optional chunking:
- Small messages: Single key-value pair
- Large messages: Split into multiple chunks
- Metadata: Stored as hash

```
smtp:msg:{messageId}        # Message data
smtp:meta:{messageId}       # Message metadata
smtp:chunk:{messageId}:{n}  # Message chunks (if chunked)
```

### Redis Streams

When `UseRedisStreams` is enabled:
- Messages are added to a Redis Stream
- Automatic trimming to prevent unlimited growth
- Useful for event-driven architectures

### Indexing

When `MaintainIndex` is enabled:
- Messages are indexed in a sorted set by date
- Fast retrieval of recent messages
- Automatic cleanup of expired entries

## Pub/Sub Notifications

When enabled, publishes events to Redis Pub/Sub:

```json
{
  "Event": "MessageStored",
  "MessageId": "msg-123"
}
```

Subscribe to notifications:

```csharp
subscriber.Subscribe("smtp:notifications", (channel, message) => {
    // Handle notification
});
```

## TTL and Expiration

Messages automatically expire after the configured TTL:
- `MessageTTLSeconds = 0`: No expiration
- `MessageTTLSeconds > 0`: Automatic cleanup

## Performance Considerations

- **Chunking**: Enable for large messages to avoid Redis protocol limits
- **Compression**: Reduces memory usage but adds CPU overhead
- **Streams vs Key-Value**: Streams are better for sequential access, Key-Value for random access
- **TTL**: Set appropriately to manage memory usage

## Requirements

- Redis 5.0 or later (for Streams support)
- Redis 3.2 or later (without Streams)
- .NET 6.0 or later

## License

MIT
