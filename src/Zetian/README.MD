# Zetian - Professional SMTP Server for .NET

[![NuGet-Version](https://img.shields.io/nuget/v/Zetian.svg?label=NuGet)](https://www.nuget.org/packages/Zetian)
[![NuGet-Download](https://img.shields.io/nuget/dt/Zetian?label=Download)](https://www.nuget.org/api/v2/package/Zetian)
[![License](https://img.shields.io/github/license/Taiizor/Zetian.svg?label=License)](https://github.com/Taiizor/Zetian/blob/develop/LICENSE)

A professional, high-performance SMTP server library for .NET with minimal dependencies. Build custom SMTP servers with ease using a fluent API and extensible architecture.

## ⚡ Quick Start

### Installation

```bash
dotnet add package Zetian
```

### Basic Usage

```csharp
using Zetian;

// Create a basic SMTP server
using var server = SmtpServerBuilder.CreateBasic();

server.MessageReceived += (sender, e) =>
{
    Console.WriteLine($"From: {e.Message.From}");
    Console.WriteLine($"Subject: {e.Message.Subject}");
};

await server.StartAsync();
Console.WriteLine($"Server running on {server.Endpoint}");
```

### With Authentication

```csharp
using var server = new SmtpServerBuilder()
    .Port(587)
    .RequireAuthentication()
    .AllowPlainTextAuthentication()
    .SimpleAuthentication("user", "password")
    .Build();

await server.StartAsync();
```

### Secure Server (TLS/SSL)

```csharp
using var server = new SmtpServerBuilder()
    .Port(587)
    .Certificate("certificate.pfx", "password")
    .RequireSecureConnection()
    .Build();

await server.StartAsync();
```

## 🎯 Key Features

- **Security** - Full TLS/SSL support with STARTTLS
- **Extensible** - Plugin architecture for custom logic
- **Rate Limiting** - Built-in protection against abuse
- **Multi-Framework** - Supports .NET 6.0, 7.0, 8.0, 9.0, 10.0
- **Event-Driven** - Rich event system for message processing
- **Composite Filters** - Chain multiple filters with AND/OR logic
- **SMTPUTF8** - Full UTF-8 support for international email addresses
- **Authentication** - PLAIN, LOGIN mechanisms + custom auth support
- **High Performance** - Efficient async/await patterns and optimized I/O
- **Mailbox Filtering** - Advanced sender/recipient filtering with domain rules
- **Message Storage** - Flexible message store abstraction with file system support
- **Health Check Ready** - Extensible with Zetian.HealthCheck package for monitoring

## 🔧 Advanced Configuration

```csharp
var server = new SmtpServerBuilder()
    .Port(587)
    .ServerName("My SMTP Server")
    .MaxMessageSizeMB(25)
    .MaxConnections(50)
    .RequireAuthentication()
    .AuthenticationHandler(async (user, pass) =>
    {
        // Custom authentication logic
        return await ValidateUser(user, pass)
            ? AuthenticationResult.Succeed(user)
            : AuthenticationResult.Fail();
    })
    // New features
    .EnableSmtpUtf8()
    .WithFileMessageStore(@"C:\smtp_messages")  // Protocol-level storage
    .WithSenderDomainWhitelist("trusted1.com", "trusted2.com")  // Protocol-level filtering
    .WithSenderDomainBlacklist("spam.com", "junk.org")
    .Build();
```

### Message Storage

```csharp
// Save messages to file system with date structure (Protocol-level)
var server = new SmtpServerBuilder()
    .Port(25)
    .WithFileMessageStore(@"C:\mail", createDateFolders: true)  // Saves at protocol level
    .Build();

// Or use the extension method (Event-based)
server.SaveMessagesToDirectory(@"C:\mail");  // Saves via event handler

// Or use custom message store
public class MongoMessageStore : IMessageStore
{
    public async Task<bool> SaveAsync(ISmtpSession session, ISmtpMessage message, CancellationToken cancellationToken)
    {
        // Save to MongoDB
        return true;
    }
}

var server = new SmtpServerBuilder()
    .MessageStore(new MongoMessageStore())
    .Build();
```

### Mailbox Filtering

```csharp
// Protocol-level domain filtering (rejects at SMTP command level)
var server = new SmtpServerBuilder()
    .WithSenderDomainWhitelist("company.com", "partner.com")
    .WithRecipientDomainWhitelist("mydomain.com")
    .WithSenderDomainBlacklist("spam.com")
    .Build();

// Event-based domain filtering (filters after message received)
server.AddAllowedDomains("example.com");  // Extension method
server.AddSpamFilter(new[] { "spam.com" });  // Extension method

// Custom mailbox filter
public class CustomMailboxFilter : IMailboxFilter
{
    public async Task<bool> CanAcceptFromAsync(ISmtpSession session, string from, long size, CancellationToken cancellationToken)
    {
        // Custom validation logic
        return !await IsBlacklisted(from);
    }
    
    public async Task<bool> CanDeliverToAsync(ISmtpSession session, string to, string from, CancellationToken cancellationToken)
    {
        // Check if recipient exists
        return await UserExists(to);
    }
}

var server = new SmtpServerBuilder()
    .MailboxFilter(new CustomMailboxFilter())
    .Build();
```

### Composite Filters

```csharp
// Combine multiple filters with AND logic
var compositeFilter = new CompositeMailboxFilter(CompositeMailboxFilter.CompositeMode.All)
    .AddFilter(new DomainMailboxFilter().AllowFromDomains("trusted.com"))
    .AddFilter(new CustomSpamFilter())
    .AddFilter(new RateLimitFilter());

var server = new SmtpServerBuilder()
    .MailboxFilter(compositeFilter)
    .Build();
```

## 📊 Message Processing

```csharp
server.MessageReceived += async (sender, e) =>
{
    var message = e.Message;
    
    // Access message details
    Console.WriteLine($"From: {message.From?.Address}");
    Console.WriteLine($"To: {string.Join(", ", message.Recipients)}");
    Console.WriteLine($"Subject: {message.Subject}");
    Console.WriteLine($"Size: {message.Size} bytes");
    
    // Get message content
    var textBody = message.TextBody;
    var htmlBody = message.HtmlBody;
    
    // Save to file
    await message.SaveToFileAsync($"{message.Id}.eml");
    
    // Reject if needed
    if (IsSpam(message))
    {
        e.Cancel = true;
        e.Response = new SmtpResponse(550, "Rejected as spam");
    }
};
```

## 🔍 Important: Two Filtering Approaches

Zetian provides two different filtering approaches:

1. **Protocol-Level Filtering** (via Builder) - Rejects at SMTP command level
   - Applied during MAIL FROM/RCPT TO commands
   - More efficient, saves bandwidth
   - Use `WithSenderDomainWhitelist`, `WithFileMessageStore` etc.

2. **Event-Based Filtering** (via Extensions) - Filters after message received
   - Applied after the entire message is received
   - More flexible for complex logic
   - Use `AddAllowedDomains`, `SaveMessagesToDirectory` etc.

Choose based on your needs:
- Use **Protocol-Level** for early rejection and better performance
- Use **Event-Based** for complex filtering logic or when you need the full message

## 🛡️ Extensions

### Rate Limiting

```csharp
server.AddRateLimiting(RateLimitConfiguration.PerHour(100));
```

### Message Filtering (Event-Based)

```csharp
server.AddSpamFilter(new[] { "spam.com", "junk.org" });
server.AddSizeFilter(10 * 1024 * 1024); // 10MB max
```

### Domain Validation (Event-Based)

```csharp
server.AddAllowedDomains("example.com", "mycompany.com");
```

### Message Storage (Event-Based)

```csharp
server.SaveMessagesToDirectory(@"C:\smtp_messages");
```

### Protocol-Level Filtering (New)

```csharp
var server = new SmtpServerBuilder()
    .WithFileMessageStore(@"C:\mail")  // Saves at protocol level
    .WithSenderDomainWhitelist("trusted.com")  // Rejects at MAIL FROM
    .WithRecipientDomainWhitelist("mydomain.com")  // Rejects at RCPT TO
    .Build();
```

## 🏥 Health Check Monitoring

Health monitoring is available through the separate **Zetian.HealthCheck** package.

### Installation

```bash
dotnet add package Zetian.HealthCheck
```

### Basic Health Check

```csharp
using Zetian.HealthCheck.Extensions;

// Enable health check on port 8080
var healthCheck = server.EnableHealthCheck(8080);
await healthCheck.StartAsync();

// Or start server with health check
var healthCheck = await server.StartWithHealthCheckAsync(8080);
```

### Health Check with IP/Hostname Binding

```csharp
// Bind to specific IP address
var healthCheck = server.EnableHealthCheck(IPAddress.Parse("192.168.1.100"), 8080);
await healthCheck.StartAsync();

// Bind to specific hostname
var healthCheck = server.EnableHealthCheck("myserver.local", 8080);
await healthCheck.StartAsync();

// Bind to all interfaces
var healthCheck = server.EnableHealthCheck("0.0.0.0", 8080);
await healthCheck.StartAsync();

// IPv6 support
var healthCheck = server.EnableHealthCheck(IPAddress.IPv6Loopback, 8080);
await healthCheck.StartAsync();

// Start server with health check on specific IP
var healthCheck = await server.StartWithHealthCheckAsync("192.168.1.100", 8080);
```

### Available Endpoints

- `http://localhost:8080/health/` - Full health check with all metrics
- `http://localhost:8080/health/livez` - Liveness probe (is server alive?)
- `http://localhost:8080/health/readyz` - Readiness probe (is server ready?)

### Custom Health Checks

```csharp
var healthCheck = server.EnableHealthCheck(8080);

// Add custom health checks
healthCheck.AddHealthCheck("database", async (ct) =>
{
    // Check database connectivity
    if (await CheckDatabaseAsync())
        return HealthCheckResult.Healthy("Database is connected");
    else
        return HealthCheckResult.Unhealthy("Database connection failed");
});

healthCheck.AddHealthCheck("disk_space", async (ct) =>
{
    var freeSpacePercent = GetDiskFreeSpacePercent();
    if (freeSpacePercent < 10)
        return HealthCheckResult.Unhealthy($"Low disk space: {freeSpacePercent}%");
    if (freeSpacePercent < 20)
        return HealthCheckResult.Degraded($"Disk space is getting low: {freeSpacePercent}%");
    return HealthCheckResult.Healthy($"Disk space is healthy: {freeSpacePercent}%");
});

await healthCheck.StartAsync();
```

### Health Check Response Example

```json
{
  "status": "Healthy",
  "timestamp": "2024-10-22T16:10:00Z",
  "checks": {
    "smtp_server": {
      "status": "Healthy",
      "description": "SMTP server is healthy",
      "data": {
        "status": "running",
        "uptime": "2d 3h 15m 42s",
        "activeSessions": 5,
        "maxSessions": 50,
        "utilizationPercent": 10.0
      }
    },
    "database": {
      "status": "Healthy",
      "description": "Database is connected"
    },
    "disk_space": {
      "status": "Healthy",
      "description": "Disk space is healthy: 65.3%"
    }
  }
}
```

### Kubernetes Integration

```yaml
apiVersion: v1
kind: Pod
spec:
  containers:
  - name: smtp-server
    livenessProbe:
      httpGet:
        path: /health/livez
        port: 8080
      initialDelaySeconds: 30
      periodSeconds: 10
    readinessProbe:
      httpGet:
        path: /health/readyz
        port: 8080
      initialDelaySeconds: 5
      periodSeconds: 5
```

## 📦 Requirements

- Windows, Linux, or macOS
- .NET 6.0, 7.0, 8.0, 9.0, or 10.0

## 📚 Documentation & Support

- **Issues**: [GitHub Issues](https://github.com/Taiizor/Zetian/issues)
- **Examples**: [GitHub Examples](https://github.com/Taiizor/Zetian/tree/develop/examples)
- **Discussions**: [GitHub Discussions](https://github.com/Taiizor/Zetian/discussions)
- **Documentation**: [Zetian Documentation](https://zetian.soferity.com)

## 🔒 Security

**Best Practices:**
- Configure rate limiting
- Keep the library updated
- Implement proper authentication
- Always use TLS/SSL in production

**Report Security Issues:** taiizor@vegalya.com

## 📄 License

MIT License - see [LICENSE](https://github.com/Taiizor/Zetian/blob/develop/LICENSE)

---

**Built with ❤️ for the .NET community**