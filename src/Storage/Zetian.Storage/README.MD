# Zetian.Storage - Core Storage Abstractions

[![NuGet-Version](https://img.shields.io/nuget/v/Zetian.Storage.svg?label=NuGet)](https://www.nuget.org/packages/Zetian.Storage)
[![NuGet-Download](https://img.shields.io/nuget/dt/Zetian.Storage?label=Download)](https://www.nuget.org/api/v2/package/Zetian.Storage)
[![License](https://img.shields.io/github/license/Taiizor/Zetian.svg?label=License)](https://github.com/Taiizor/Zetian/blob/develop/LICENSE)

Core storage abstraction layer for Zetian SMTP Server. Provides essential interfaces, base classes, and contracts for building custom message storage providers. This package serves as the foundation for all Zetian storage implementations.

## ⚡ Features

- 🔄 **Retry Logic** - Built-in retry mechanisms for reliability
- 🧩 **Extensible** - Easy to create custom storage providers
- 📊 **Attachment Handling** - Standardized attachment storage
- 🚀 **High Performance** - Optimized for SMTP message handling
- 🔧 **Base Classes** - Ready-to-use base configurations and helpers
- 📦 **Modular Design** - Install only the storage providers you need
- 🗜️ **Compression Support** - Optional message body compression
- 🎯 **Clean Abstractions** - Well-defined interfaces for storage implementations

## 📦 Installation

```bash
# Install the core package (required for custom providers)
dotnet add package Zetian
dotnet add package Zetian.Storage
```

## 🚀 Available Storage Providers

Choose from our pre-built storage providers:

### 💾 **Database Storage**
- **[Zetian.Storage.MongoDB](https://www.nuget.org/packages/Zetian.Storage.MongoDB)** - MongoDB NoSQL with GridFS support
- **[Zetian.Storage.PostgreSQL](https://www.nuget.org/packages/Zetian.Storage.PostgreSQL)** - PostgreSQL with JSONB and partitioning
- **[Zetian.Storage.SqlServer](https://www.nuget.org/packages/Zetian.Storage.SqlServer)** - Microsoft SQL Server with ACID compliance

### ⚡ **Cache Storage**
- **[Zetian.Storage.Redis](https://www.nuget.org/packages/Zetian.Storage.Redis)** - Redis for high-performance caching

### ☁️ **Cloud Storage**
- **[Zetian.Storage.AmazonS3](https://www.nuget.org/packages/Zetian.Storage.AmazonS3)** - Amazon S3 and S3-compatible services
- **[Zetian.Storage.AzureBlob](https://www.nuget.org/packages/Zetian.Storage.AzureBlob)** - Azure Blob Storage with tier management

## 🎯 Quick Start

### Using a Storage Provider

```csharp
using Zetian.Server;
using Zetian.Storage.SqlServer.Extensions;

// Configure SMTP server with SQL Server storage
var server = new SmtpServerBuilder()
    .Port(25)
    .WithSqlServerStorage(
        "Server=localhost;Database=SmtpStorage;Integrated Security=true;",
        config =>
        {
            config.MaxMessageSizeMB = 25;
            config.AutoCreateTables = true;
            config.EnableCompression = true;
        })
    .Build();

await server.StartAsync();
```

## 🛠️ Core Interfaces

### IMessageStore

The main interface all storage providers must implement:

```csharp
public interface IMessageStore
{
    /// <summary>
    /// Save the given message to the underlying storage system
    /// </summary>
    Task<bool> SaveAsync(
        ISmtpSession session,
        ISmtpMessage message,
        CancellationToken cancellationToken = default);
}
```

### BaseStorageConfiguration

Base configuration class for all storage providers:

```csharp
public abstract class BaseStorageConfiguration
{
    public double MaxMessageSizeMB { get; set; } = 25.0;
    public bool CompressMessageBody { get; set; } = false;
    public bool EnableRetry { get; set; } = true;
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
    public int ConnectionTimeoutSeconds { get; set; } = 30;
    public bool LogErrors { get; set; } = true;
    public string? CustomStoragePath { get; set; }
}
```

## 📝 Creating Custom Storage Providers

### Step 1: Implement IMessageStore

```csharp
using Zetian.Abstractions;
using System.Threading;
using System.Threading.Tasks;

public class MyCustomMessageStore : IMessageStore
{
    private readonly MyCustomConfiguration _config;
    
    public MyCustomMessageStore(MyCustomConfiguration config)
    {
        _config = config;
    }
    
    public async Task<bool> SaveAsync(
        ISmtpSession session,
        ISmtpMessage message,
        CancellationToken cancellationToken = default)
    {
        // Your implementation
        try
        {
            // Save message to your storage
            await SaveToCustomStorageAsync(session, message);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
```

### Step 2: Create Configuration Class

```csharp
using Zetian.Storage.Configuration;

public class MyCustomConfiguration : BaseStorageConfiguration
{
    public string ConnectionString { get; set; } = string.Empty;
    public string TableName { get; set; } = "SmtpMessages";
    public bool UseEncryption { get; set; } = false;
    // Add custom properties
}
```

### Step 3: Create Extension Method

```csharp
using Zetian.Server;
using Zetian.Storage.Extensions;
using Microsoft.Extensions.Logging;
using System;

public static class MyCustomStorageExtensions
{
    public static SmtpServerBuilder WithMyCustomStorage(
        this SmtpServerBuilder builder,
        string connectionString,
        Action<MyCustomConfiguration>? configure = null)
    {
        var configuration = new MyCustomConfiguration
        {
            ConnectionString = connectionString
        };
        
        configure?.Invoke(configuration);
        configuration.Validate();
        
        ILogger<MyCustomMessageStore>? logger = builder.GetLogger<MyCustomMessageStore>();
        var store = new MyCustomMessageStore(configuration, logger);
        
        return builder.MessageStore(store);
    }
}
```

### Step 4: Use Your Custom Provider

```csharp
var server = new SmtpServerBuilder()
    .Port(25)
    .WithMyCustomStorage(
        "custom://localhost",
        config =>
        {
            config.TableName = "EmailArchive";
            config.UseEncryption = true;
        })
    .Build();
```

## 🎨 Advanced Patterns

### Implementing Compression

```csharp
protected byte[] CompressData(byte[] data)
{
    if (!_config.CompressMessageBody || data.Length < 1024)
        return data;
        
    using var output = new MemoryStream();
    using (var gzip = new GZipStream(output, CompressionMode.Compress))
    {
        gzip.Write(data, 0, data.Length);
    }
    return output.ToArray();
}
```

### Implementing Retry Logic

```csharp
protected async Task<T> ExecuteWithRetryAsync<T>(
    Func<Task<T>> operation,
    CancellationToken cancellationToken = default)
{
    int attempts = 0;
    
    while (attempts < _config.MaxRetryAttempts)
    {
        try
        {
            return await operation();
        }
        catch (Exception ex) when (_config.EnableRetry && attempts < _config.MaxRetryAttempts - 1)
        {
            attempts++;
            await Task.Delay(_config.RetryDelayMs, cancellationToken);
            
            if (_config.LogErrors)
            {
                _logger?.LogWarning(
                    "Retry attempt {Attempt} after error: {Error}",
                    attempts, ex.Message);
            }
        }
    }
    
    throw new StorageException($"Operation failed after {attempts} attempts");
}
```

## 🔧 Best Practices

### Storage Provider Guidelines

1. **Validation** - Validate input parameters
2. **Cancellation** - Respect cancellation tokens
3. **Async/Await** - Use async operations throughout
4. **Logging** - Use structured logging for debugging
5. **Resource Cleanup** - Properly dispose of resources
6. **Connection Pooling** - Reuse connections when possible
7. **Error Handling** - Implement comprehensive error handling
8. **Thread Safety** - Ensure your implementation is thread-safe

### Performance Tips

- **Async I/O** - Use async file/network operations
- **Caching** - Cache frequently accessed messages
- **Connection Reuse** - Maintain connection pools
- **Compression** - Enable for large message bodies
- **Indexing** - Create appropriate indexes for queries
- **Batch Operations** - Process multiple messages in batches

## 📊 Message Model

```csharp
public class SmtpMessage
{
    public string Id { get; set; }
    public string From { get; set; }
    public List<string> To { get; set; }
    public List<string> Cc { get; set; }
    public List<string> Bcc { get; set; }
    public string Subject { get; set; }
    public string Body { get; set; }
    public Dictionary<string, string> Headers { get; set; }
    public List<SmtpAttachment> Attachments { get; set; }
    public DateTime ReceivedDate { get; set; }
    public long Size { get; set; }
    public string RemoteIP { get; set; }
}
```

## 📋 Requirements

- .NET 6.0, 7.0, 8.0, 9.0, or 10.0
- Zetian SMTP Server package
- Storage-specific dependencies based on provider

## 📚 Documentation & Support

- **Issues**: [GitHub Issues](https://github.com/Taiizor/Zetian/issues)
- **Examples**: [GitHub Examples](https://github.com/Taiizor/Zetian/tree/develop/examples)
- **Discussions**: [GitHub Discussions](https://github.com/Taiizor/Zetian/discussions)
- **Documentation**: [Zetian Documentation](https://zetian.soferity.com)

## 📄 License

MIT License - see [LICENSE](https://github.com/Taiizor/Zetian/blob/develop/LICENSE)

---

**Built with ❤️ for the .NET community**