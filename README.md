![Logo](.images/Logo.png)

![Dot-Net-Version](https://img.shields.io/badge/.NET-%3E%3D6.0-blue)
![C-Sharp-Version](https://img.shields.io/badge/C%23-Preview-blue.svg)
[![IDE-Version](https://img.shields.io/badge/IDE-VS2022-blue.svg)](https://visualstudio.microsoft.com/downloads)
[![NuGet-Version](https://img.shields.io/nuget/v/Zetian.svg?label=NuGet)](https://www.nuget.org/packages/Zetian)
[![NuGet-Download](https://img.shields.io/nuget/dt/Zetian?label=Download)](https://www.nuget.org/api/v2/package/Zetian)
[![Stack Overflow](https://img.shields.io/badge/Stack%20Overflow-Zetian-orange.svg)](https://stackoverflow.com/questions/tagged/zetian)

[![.NET](https://github.com/Taiizor/Zetian/actions/workflows/dotnet.yml/badge.svg)](https://github.com/Taiizor/Zetian/actions/workflows/dotnet.yml)
[![CodeQL](https://github.com/Taiizor/Zetian/actions/workflows/codeql-analysis.yml/badge.svg)](https://github.com/Taiizor/Zetian/actions/workflows/codeql-analysis.yml)
[![.NET Desktop](https://github.com/Taiizor/Zetian/actions/workflows/dotnet-desktop.yml/badge.svg)](https://github.com/Taiizor/Zetian/actions/workflows/dotnet-desktop.yml)

[![Discord-Server](https://img.shields.io/discord/932386235538878534?label=Discord)](https://discord.gg/nxG977byXb)

# Zetian SMTP Server

A professional, high-performance SMTP server library for .NET with minimal dependencies. Build custom SMTP servers with ease using a fluent API and extensible architecture.

## Features

- 🔒 **Security**: Full TLS/SSL support with STARTTLS
- 📦 **Minimal Dependencies**: Only essential packages required
- 🎯 **Multi-Framework**: Supports .NET 6.0, 7.0, 8.0, 9.0, and 10.0
- 🛡️ **Rate Limiting**: Protect against abuse with configurable rate limits
- 🔑 **Authentication**: Built-in PLAIN and LOGIN mechanisms, easily extensible
- 📊 **Event-Driven**: Rich event system for message processing and monitoring
- 🚀 **High Performance**: Efficient async/await patterns and optimized I/O operations
- 🔧 **Extensible**: Plugin architecture for custom authentication, filtering, and processing

## Installation

```bash
dotnet add package Zetian
```

Or via NuGet Package Manager:
```
Install-Package Zetian
```

## Quick Start

### Basic SMTP Server

```csharp
using Zetian.Server;

// Create and start a basic SMTP server
using var server = SmtpServerBuilder.CreateBasic();

server.MessageReceived += (sender, e) =>
{
    Console.WriteLine($"Received message from {e.Message.From}");
    Console.WriteLine($"Subject: {e.Message.Subject}");
};

await server.StartAsync();
Console.WriteLine($"Server running on {server.Endpoint}");

// Keep running...
Console.ReadKey();
await server.StopAsync();
```

### Authenticated SMTP Server

```csharp
using Zetian.Server;

using var server = new SmtpServerBuilder()
    .Port(587)
    .RequireAuthentication()
    .AllowPlainTextAuthentication()
    .SimpleAuthentication("user", "password")
    .Build();

await server.StartAsync();
```

### Secure SMTP Server with TLS

```csharp
using Zetian.Server;

using var server = new SmtpServerBuilder()
    .Port(587)
    .Certificate("certificate.pfx", "password")
    .RequireSecureConnection()
    .Build();

await server.StartAsync();
```

## Advanced Configuration

### Using the Fluent Builder

```csharp
using System.Net;
using Zetian.Models;
using Zetian.Server;

var server = new SmtpServerBuilder()
    .Port(587)
    .BindTo(IPAddress.Any)
    .ServerName("My SMTP Server")
    .MaxMessageSizeMB(25)
    .MaxRecipients(100)
    .MaxConnections(50)
    .MaxConnectionsPerIP(5)
    
    // Security
    .Certificate(certificate)
    .RequireAuthentication()
    .RequireSecureConnection()
    
    // Authentication
    .AddAuthenticationMechanism("PLAIN")
    .AddAuthenticationMechanism("LOGIN")
    .AuthenticationHandler(async (username, password) =>
    {
        // Your authentication logic
        if (await ValidateUser(username, password))
            return AuthenticationResult.Succeed(username);
        return AuthenticationResult.Fail();
    })
    
    // Features
    .EnablePipelining()
    .Enable8BitMime()
    
    // Timeouts
    .ConnectionTimeout(TimeSpan.FromMinutes(5))
    .CommandTimeout(TimeSpan.FromSeconds(30))
    .DataTimeout(TimeSpan.FromMinutes(2))
    
    // Retry Configuration
    .MaxRetryCount(3)
    
    // Logging
    .LoggerFactory(loggerFactory)
    .EnableVerboseLogging()
    
    .Build();
```

## Extensions

### Rate Limiting

```csharp
using Zetian.Models;
using Zetian.Extensions;

server.AddRateLimiting(
    RateLimitConfiguration.PerHour(100)
);
```

### Message Filtering

#### 🔍 Important: Two Filtering Approaches

Zetian provides two different filtering approaches:

1. **Protocol-Level Filtering** (via Builder) - Rejects at SMTP command level
   - Applied during MAIL FROM/RCPT TO commands
   - More efficient, saves bandwidth
   - Use `WithSenderDomainWhitelist`, `WithRecipientDomainWhitelist` etc.

2. **Event-Based Filtering** (via Extensions) - Filters after message received
   - Applied after the entire message is received
   - More flexible for complex logic
   - Use `AddSpamFilter`, `AddSizeFilter`, `AddMessageFilter` etc.

Choose based on your needs:
- Use **Protocol-Level** for early rejection and better performance
- Use **Event-Based** for complex filtering logic or when you need the full message

#### Protocol-Level Filtering (Early Rejection)

```csharp
// Configure filtering at build time - rejects at SMTP protocol level
var server = new SmtpServerBuilder()
    .Port(25)
    .WithSenderDomainWhitelist("trusted.com", "example.com")  // Rejects at MAIL FROM
    .WithSenderDomainBlacklist("spam.com", "junk.org")        // Rejects at MAIL FROM
    .WithRecipientDomainWhitelist("mydomain.com")             // Rejects at RCPT TO
    .MaxMessageSize(10 * 1024 * 1024)                         // Rejects at MAIL FROM
    .WithFileMessageStore(@"C:\mail")                         // Stores at protocol level
    .Build();
```

#### Event-Based Filtering (Late Rejection)

```csharp
// Configure filtering via extensions - processes after message is received
// Add spam filter
server.AddSpamFilter(new[] { "spam.com", "junk.org" });

// Add size filter (10MB max)
server.AddSizeFilter(10 * 1024 * 1024);

// Add custom filter
server.AddMessageFilter(message => 
{
    // Your filtering logic
    return !message.Subject?.Contains("SPAM") ?? true;
});
```

### Message Storage

```csharp
// Event-based approach - saves after message is received
server.SaveMessagesToDirectory(@"C:\smtp_messages");

// Protocol-level approach - integrated storage during SMTP transaction
var server = new SmtpServerBuilder()
    .Port(25)
    .WithFileMessageStore(@"C:\smtp_messages")  // Automatic storage
    .Build();

// Custom message processing (event-based)
server.MessageReceived += async (sender, e) =>
{
    // Save to database
    await SaveToDatabase(e.Message);
    
    // Forward to another service
    await ForwardMessage(e.Message);
    
    // Send notification
    await NotifyAdministrator(e.Message);
};
```

### Domain Validation

```csharp
// Event-based approach - validates after message is received
server.AddAllowedDomains("example.com", "mycompany.com");

// Custom recipient validation
server.AddRecipientValidation(recipient =>
{
    return IsValidRecipient(recipient.Address);
});

// Protocol-level approach - validates at SMTP command level
var server = new SmtpServerBuilder()
    .Port(25)
    .WithRecipientDomainWhitelist("example.com", "mycompany.com")
    .Build();
```

## Event Handling

```csharp
// Session events
server.SessionCreated += (s, e) =>
    Console.WriteLine($"New session from {e.Session.RemoteEndPoint}");

server.SessionCompleted += (s, e) =>
    Console.WriteLine($"Session completed: {e.Session.Id}");

// Message events
server.MessageReceived += (s, e) =>
{
    Console.WriteLine($"Message: {e.Message.Subject}");
    
    // Reject message if needed
    if (IsSpam(e.Message))
    {
        e.Cancel = true;
        e.Response = new SmtpResponse(550, "Message rejected as spam");
    }
};

// Error events
server.ErrorOccurred += (s, e) =>
    Console.WriteLine($"Error: {e.Exception.Message}");
```

## Custom Authentication

```csharp
// Option 1: Create a custom authenticator class
using Zetian.Models;
using Zetian.Abstractions;

public class CustomAuthenticator : IAuthenticator
{
    public string Mechanism => "CUSTOM";
    
    public async Task<AuthenticationResult> AuthenticateAsync(
        ISmtpSession session,
        string? initialResponse,
        StreamReader reader,
        StreamWriter writer,
        CancellationToken cancellationToken)
    {
        // Implement your custom authentication logic here
        // ...
    }
}

// Register custom authenticator
AuthenticatorFactory.Register("CUSTOM", () => 
    new CustomAuthenticator());

// Option 2: Use the authentication handler with builder
using Zetian.Models;
using Zetian.Server;

var server = new SmtpServerBuilder()
    .Port(587)
    .RequireAuthentication()
    .AuthenticationHandler(async (username, password) =>
    {
        // Check against database
        var user = await db.GetUser(username);
        if (user != null && VerifyPassword(password, user.PasswordHash))
        {
            return AuthenticationResult.Succeed(username);
        }
        return AuthenticationResult.Fail();
    })
    .AddAuthenticationMechanism("PLAIN")
    .AddAuthenticationMechanism("LOGIN")
    .Build();
```

## Message Processing

```csharp
server.MessageReceived += async (sender, e) =>
{
    var message = e.Message;
    
    // Access message properties
    Console.WriteLine($"ID: {message.Id}");
    Console.WriteLine($"From: {message.From?.Address}");
    Console.WriteLine($"To: {string.Join(", ", message.Recipients)}");
    Console.WriteLine($"Subject: {message.Subject}");
    Console.WriteLine($"Size: {message.Size} bytes");
    Console.WriteLine($"Has Attachments: {message.HasAttachments}");
    Console.WriteLine($"Priority: {message.Priority}");
    
    // Access headers
    var messageId = message.GetHeader("Message-Id");
    var contentType = message.GetHeader("Content-Type");
    
    // Get message content
    var textBody = message.TextBody;
    var htmlBody = message.HtmlBody;
    
    // Save message
    await message.SaveToFileAsync($"{message.Id}.eml");
    
    // Get raw data
    var rawData = await message.GetRawDataAsync();
};
```

## Examples

The `examples` directory contains comprehensive examples:

1. **SecureExample** - SMTP server with TLS/SSL support
2. **MessageStorageExample** - Saving messages to disk
3. **RateLimitedExample** - SMTP server with rate limiting
4. **AuthenticatedExample** - SMTP server with authentication
5. **BasicExample** - Simple SMTP server without authentication
6. **FullFeaturedExample** - Complete SMTP server with all features
7. **CustomProcessingExample** - Custom message processing and filtering
8. **MaxRetryCountExample** - Demonstrates retry mechanism configuration
9. **ProtocolLevelFilteringExample** - Demonstrates the difference between protocol-level and event-based filtering

## Performance

Zetian is built for high performance:

- Configurable buffer sizes
- Minimal memory allocations
- Connection pooling and throttling
- Optimized network I/O operations
- Efficient async/await patterns throughout

## Security Considerations

- Validate and sanitize all inputs
- Configure rate limiting to prevent abuse
- Implement proper logging and monitoring
- Implement proper authentication mechanisms
- Always use TLS/SSL in production environments
- Keep the library updated with latest security patches

## Requirements

- Windows, Linux, or macOS
- .NET 6.0, 7.0, 8.0, 9.0, or 10.0

## Contributing

Contributions are welcome! Please feel free to submit issues, feature requests, or pull requests.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

For support, please open an issue on GitHub or contact the maintainers.

## Acknowledgments

Built with ❤️ using modern .NET technologies.