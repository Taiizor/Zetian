# Zetian - Professional SMTP Server for .NET

[![NuGet-Version](https://img.shields.io/nuget/v/Zetian.svg?label=NuGet)](https://www.nuget.org/packages/Zetian)
[![NuGet-Download](https://img.shields.io/nuget/dt/Zetian?label=Download)](https://www.nuget.org/api/v2/package/Zetian)
[![License](https://img.shields.io/github/license/Taiizor/Zetian.svg?label=License)](https://github.com/Taiizor/Zetian/blob/develop/LICENSE)

A professional, high-performance SMTP server library for .NET with minimal dependencies and modern async/await patterns.

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
- **Authentication** - PLAIN, LOGIN mechanisms + custom auth support
- **High Performance** - System.IO.Pipelines integration for optimized I/O
- **Message Storage** - Flexible message store abstraction with file system support
- **Mailbox Filtering** - Advanced sender/recipient filtering with domain rules
- **SMTPUTF8** - Full UTF-8 support for international email addresses
- **Composite Filters** - Chain multiple filters with AND/OR logic

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
    .SaveMessagesToDirectory(@"C:\smtp_messages")
    .AllowedFromDomains("trusted1.com", "trusted2.com")
    .BlockedFromDomains("spam.com", "junk.org")
    .Build();
```

### Message Storage

```csharp
// Save messages to file system with date structure
var server = new SmtpServerBuilder()
    .Port(25)
    .SaveMessagesToDirectory(@"C:\mail", createDateFolders: true)
    .Build();

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
// Domain-based filtering
var server = new SmtpServerBuilder()
    .AllowedFromDomains("company.com", "partner.com")
    .AllowedToDomains("mydomain.com")
    .BlockedFromDomains("spam.com")
    .Build();

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

## 🛡️ Extensions

### Rate Limiting

```csharp
server.AddRateLimiting(RateLimitConfiguration.PerHour(100));
```

### Message Filtering

```csharp
server.AddSpamFilter(new[] { "spam.com", "junk.org" });
server.AddSizeFilter(10 * 1024 * 1024); // 10MB max
```

### Domain Validation

```csharp
server.AddAllowedDomains("example.com", "mycompany.com");
```

### Message Storage

```csharp
server.SaveMessagesToDirectory(@"C:\smtp_messages");
```

## 📦 Requirements

- Windows, Linux, or macOS
- .NET 6.0, 7.0, 8.0, 9.0, or 10.0

## 📚 Documentation & Support

- **Issues**: [GitHub Issues](https://github.com/Taiizor/Zetian/issues)
- **Examples**: [GitHub Examples](https://github.com/Taiizor/Zetian/tree/develop/examples)
- **Documentation**: [GitHub Wiki](https://github.com/Taiizor/Zetian/wiki)
- **Discussions**: [GitHub Discussions](https://github.com/Taiizor/Zetian/discussions)

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