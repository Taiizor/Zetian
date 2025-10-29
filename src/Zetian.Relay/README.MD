# Zetian.Relay - SMTP Relay and Proxy Extension

[![NuGet-Version](https://img.shields.io/nuget/v/Zetian.Relay.svg?label=NuGet)](https://www.nuget.org/packages/Zetian.Relay)
[![NuGet-Download](https://img.shields.io/nuget/dt/Zetian.Relay?label=Download)](https://www.nuget.org/api/v2/package/Zetian.Relay)
[![License](https://img.shields.io/github/license/Taiizor/Zetian.svg?label=License)](https://github.com/Taiizor/Zetian/blob/develop/LICENSE)

SMTP relay and proxy extension for Zetian SMTP Server with smart host support, queue management, load balancing, and failover mechanisms.

## ⚡ Features

- 📬 **Smart Host Support** - Route messages through configured relay servers
- 🔄 **Queue Management** - Persistent queue with retry mechanisms
- ⚖️ **Load Balancing** - Distribute load across multiple relay servers
- 🔀 **Failover** - Automatic failover to backup servers
- 📊 **MX Routing** - DNS MX record-based routing
- 🎯 **Domain Routing** - Custom routing rules per domain
- ⏱️ **Retry Logic** - Exponential backoff with configurable limits
- 📈 **Queue Statistics** - Real-time queue monitoring
- 🔐 **Authentication** - Support for AUTH PLAIN and LOGIN
- 🌐 **TLS/SSL** - Secure connections with STARTTLS support
- 💌 **Bounce Messages** - Automatic NDR generation
- 🎚️ **Priority Queuing** - Message priority support

## 📦 Installation

```bash
# Install Zetian SMTP Server (required)
dotnet add package Zetian

# Install Relay Extension
dotnet add package Zetian.Relay
```

## 🚀 Quick Start

### Basic Relay Setup

```csharp
using Zetian.Server;
using Zetian.Relay.Extensions;

// Create SMTP server with relay enabled
var server = SmtpServerBuilder
    .CreateBasic()
    .EnableRelay();

// Start server with relay service
var relayService = await server.StartWithRelayAsync();
```

### With Smart Host

```csharp
using Zetian.Relay.Configuration;

var server = SmtpServerBuilder
    .CreateBasic()
    .EnableRelay(config =>
    {
        config.DefaultSmartHost = new SmartHostConfiguration
        {
            Host = "smtp.example.com",
            Port = 587,
            Credentials = new NetworkCredential("user", "password"),
            UseTls = true
        };
    });

await server.StartAsync();
```

### Multiple Smart Hosts with Failover

```csharp
server.EnableRelay(config =>
{
    // Primary smart host
    config.DefaultSmartHost = new SmartHostConfiguration
    {
        Host = "primary.smtp.com",
        Port = 587,
        Priority = 10,
        Credentials = new NetworkCredential("user", "pass")
    };
    
    // Backup smart hosts
    config.SmartHosts.Add(new SmartHostConfiguration
    {
        Host = "backup1.smtp.com",
        Port = 587,
        Priority = 20,
        Credentials = new NetworkCredential("user", "pass")
    });
    
    config.SmartHosts.Add(new SmartHostConfiguration
    {
        Host = "backup2.smtp.com",
        Port = 587,
        Priority = 30
    });
});
```

## 🎯 Advanced Configuration

### Using Relay Builder

```csharp
using Zetian.Relay.Builder;

var relayConfig = new RelayBuilder()
    .WithSmartHost("smtp.office365.com", 587, "user@domain.com", "password")
    .MaxConcurrentDeliveries(20)
    .MaxRetries(5)
    .MessageLifetime(TimeSpan.FromDays(3))
    .ConnectionTimeout(TimeSpan.FromMinutes(10))
    .EnableTls(true, require: true)
    .LocalDomain("mail.mydomain.com")
    .AddLocalDomains("mydomain.com", "internal.local")
    .AddRelayDomains("partner.com", "customer.com")
    .RequireAuthentication(true)
    .EnableBounce(true, "postmaster@mydomain.com")
    .Build();

var server = SmtpServerBuilder
    .CreateBasic()
    .EnableRelay(relayConfig);
```

### Domain-Specific Routing

```csharp
server.EnableRelay(config =>
{
    // Route specific domains through different smart hosts
    config.DomainRouting["gmail.com"] = new SmartHostConfiguration
    {
        Host = "smtp.gmail.com",
        Port = 587,
        Credentials = new NetworkCredential("user@gmail.com", "app_password")
    };
    
    config.DomainRouting["outlook.com"] = new SmartHostConfiguration
    {
        Host = "smtp-mail.outlook.com",
        Port = 587,
        Credentials = new NetworkCredential("user@outlook.com", "password")
    };
    
    // Default for all other domains
    config.DefaultSmartHost = new SmartHostConfiguration
    {
        Host = "smtp.sendgrid.net",
        Port = 587,
        Credentials = new NetworkCredential("apikey", "SG.xxxxx")
    };
});
```

### MX-Based Routing

```csharp
server.EnableRelay(config =>
{
    // Use DNS MX records for routing
    config.UseMxRouting = true;
    
    // Optional: Custom DNS servers
    config.DnsServers.Add(IPAddress.Parse("8.8.8.8"));
    config.DnsServers.Add(IPAddress.Parse("1.1.1.1"));
    
    // Fallback smart host if MX lookup fails
    config.DefaultSmartHost = new SmartHostConfiguration
    {
        Host = "fallback.smtp.com",
        Port = 25
    };
});
```

### Relay Networks Configuration

```csharp
server.EnableRelay(config =>
{
    // Allow relay from specific IP addresses without authentication
    config.RelayNetworks.Add(IPAddress.Parse("192.168.1.0"));
    config.RelayNetworks.Add(IPAddress.Parse("10.0.0.0"));
    
    // Require authentication for all others
    config.RequireAuthentication = true;
});
```

## 📊 Queue Management

### Manual Queue Operations

```csharp
// Get relay service
var relayService = server.GetRelayService();

// Queue a message manually
var relayMessage = await server.QueueForRelayAsync(
    message,
    session,
    RelayPriority.High);

// Get queue statistics
var stats = await server.GetRelayStatisticsAsync();
Console.WriteLine($"Queued: {stats.QueuedMessages}");
Console.WriteLine($"In Progress: {stats.InProgressMessages}");
Console.WriteLine($"Delivered: {stats.DeliveredMessages}");
Console.WriteLine($"Failed: {stats.FailedMessages}");

// Get all messages in queue
var messages = await relayService.Queue.GetAllAsync();

// Get messages by status
var deferredMessages = await relayService.Queue.GetByStatusAsync(RelayStatus.Deferred);

// Remove a message
await relayService.Queue.RemoveAsync(queueId);

// Clear expired messages
var cleared = await relayService.Queue.ClearExpiredAsync();
```

### Message Priority

```csharp
server.MessageReceived += async (sender, e) =>
{
    // Determine priority based on sender or content
    var priority = e.Message.From?.Address?.EndsWith("@vip.com") == true
        ? RelayPriority.Urgent
        : RelayPriority.Normal;
    
    // Queue with priority
    await server.QueueForRelayAsync(e.Message, e.Session, priority);
};
```

## 🔄 Retry Configuration

```csharp
server.EnableRelay(config =>
{
    // Maximum retry attempts
    config.MaxRetryCount = 10;
    
    // Message lifetime before expiration
    config.MessageLifetime = TimeSpan.FromDays(4);
    
    // Connection timeout per attempt
    config.ConnectionTimeout = TimeSpan.FromMinutes(5);
    
    // Queue processing interval
    config.QueueProcessingInterval = TimeSpan.FromSeconds(30);
    
    // Cleanup expired messages interval
    config.CleanupInterval = TimeSpan.FromHours(1);
});
```

### Retry Schedule

The relay service uses exponential backoff for retries:
- 1st retry: 1 minute
- 2nd retry: 2 minutes
- 3rd retry: 4 minutes
- 4th retry: 8 minutes
- 5th retry: 16 minutes
- 6th retry: 32 minutes
- 7th+ retry: 1-4 hours (capped)

## 📬 Bounce Messages

```csharp
server.EnableRelay(config =>
{
    // Enable bounce message generation
    config.EnableBounceMessages = true;
    
    // Set bounce sender address
    config.BounceSender = "postmaster@mydomain.com";
    
    // Enable delivery status notifications
    config.EnableDsn = true;
});
```

## 🔐 Authentication Methods

### AUTH PLAIN

```csharp
var smartHost = new SmartHostConfiguration
{
    Host = "smtp.example.com",
    Port = 587,
    Credentials = new NetworkCredential("username", "password"),
    UseStartTls = true
};
```

### AUTH LOGIN

The relay client automatically detects and uses the appropriate authentication method based on server capabilities.

## 📈 Load Balancing

```csharp
server.EnableRelay(config =>
{
    // Configure multiple smart hosts with weights
    config.SmartHosts.AddRange(new[]
    {
        new SmartHostConfiguration
        {
            Host = "smtp1.example.com",
            Port = 25,
            Priority = 10,
            Weight = 50  // 50% of traffic
        },
        new SmartHostConfiguration
        {
            Host = "smtp2.example.com",
            Port = 25,
            Priority = 10,
            Weight = 30  // 30% of traffic
        },
        new SmartHostConfiguration
        {
            Host = "smtp3.example.com",
            Port = 25,
            Priority = 10,
            Weight = 20  // 20% of traffic
        }
    });
});
```

## 🌐 TLS/SSL Configuration

```csharp
server.EnableRelay(config =>
{
    // Enable TLS for outbound connections
    config.EnableTls = true;
    
    // Require TLS (fail if not available)
    config.RequireTls = true;
    
    // Configure SSL protocols
    config.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
    
    // Validate server certificates
    config.ValidateServerCertificate = true;
});
```

## 🎛️ Extension Methods

```csharp
// Add smart host
server.AddSmartHost("smtp.gmail.com", 587, 
    new NetworkCredential("user", "pass"));

// Set relay domains
server.SetRelayDomains("external.com", "partner.org");

// Set local domains (not relayed)
server.SetLocalDomains("mydomain.com", "internal.local");

// Add relay network
server.AddRelayNetwork(IPAddress.Parse("192.168.1.0"));

// Get relay statistics
var stats = await server.GetRelayStatisticsAsync();

// Get relay service
var relayService = server.GetRelayService();
```

## 🔍 Message Status

| Status | Description |
|--------|-------------|
| Queued | Message is waiting for delivery |
| InProgress | Message is currently being delivered |
| Delivered | Message successfully delivered |
| Failed | Permanent delivery failure |
| Deferred | Temporary failure, will retry |
| Expired | Message exceeded lifetime |
| Cancelled | Message was cancelled |
| PartiallyDelivered | Some recipients succeeded |

## 📊 Monitoring

```csharp
// Get comprehensive statistics
var stats = await relayService.Queue.GetStatisticsAsync();

Console.WriteLine($"Total Messages: {stats.TotalMessages}");
Console.WriteLine($"Queued: {stats.QueuedMessages}");
Console.WriteLine($"In Progress: {stats.InProgressMessages}");
Console.WriteLine($"Deferred: {stats.DeferredMessages}");
Console.WriteLine($"Delivered: {stats.DeliveredMessages}");
Console.WriteLine($"Failed: {stats.FailedMessages}");
Console.WriteLine($"Expired: {stats.ExpiredMessages}");
Console.WriteLine($"Total Size: {stats.TotalSize} bytes");
Console.WriteLine($"Oldest Message: {stats.OldestMessageTime}");
Console.WriteLine($"Average Queue Time: {stats.AverageQueueTime}");
Console.WriteLine($"Average Retry Count: {stats.AverageRetryCount}");

// Messages by priority
foreach (var kvp in stats.MessagesByPriority)
{
    Console.WriteLine($"Priority {kvp.Key}: {kvp.Value} messages");
}

// Messages by smart host
foreach (var kvp in stats.MessagesBySmartHost)
{
    Console.WriteLine($"Host {kvp.Key}: {kvp.Value} messages");
}
```

## 🚀 Performance Tips

1. **Connection Pooling** - Reuse SMTP client connections
2. **Concurrent Deliveries** - Adjust `MaxConcurrentDeliveries` based on resources
3. **Message Batching** - Group messages to same destination
4. **Smart Host Selection** - Use priority and weight for optimal routing
5. **DNS Caching** - Cache MX record lookups
6. **Queue Persistence** - Consider persistent queue for reliability

## 📚 Best Practices

1. **Always Configure Timeouts** - Prevent hanging connections
2. **Use TLS When Possible** - Secure message transmission
3. **Monitor Queue Size** - Prevent unbounded growth
4. **Set Message Lifetime** - Clean up undeliverable messages
5. **Configure Bounce Handling** - Inform senders of failures
6. **Implement Rate Limiting** - Prevent abuse of relay
7. **Regular Cleanup** - Remove expired messages periodically
8. **Log Relay Activity** - Audit trail for compliance

## 🔒 Security Considerations

- Always require authentication for relay access
- Use TLS/SSL for all outbound connections
- Validate server certificates
- Limit relay networks to trusted IPs
- Monitor for relay abuse
- Implement rate limiting
- Regular security audits

## 📋 Requirements

- Windows, Linux, or macOS
- .NET 6.0, 7.0, 8.0, 9.0, or 10.0
- Zetian SMTP Server package

## 📚 Documentation & Support

- **Issues**: [GitHub Issues](https://github.com/Taiizor/Zetian/issues)
- **Examples**: [GitHub Examples](https://github.com/Taiizor/Zetian/tree/develop/examples)
- **Discussions**: [GitHub Discussions](https://github.com/Taiizor/Zetian/discussions)
- **Documentation**: [Zetian Documentation](https://zetian.soferity.com)

## 📄 License

MIT License - see [LICENSE](https://github.com/Taiizor/Zetian/blob/develop/LICENSE)

---

**Built with ❤️ for the .NET community**
