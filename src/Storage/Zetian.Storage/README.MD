# Zetian.Storage

Core storage abstractions and base classes for Zetian SMTP Server.

## Overview

This package provides the core abstractions and base classes that are used by all Zetian storage providers. It does not include any concrete storage implementations.

## Available Storage Providers

Install the specific storage provider package you need:

- **Zetian.Storage.SqlServer** - Microsoft SQL Server storage
- **Zetian.Storage.PostgreSQL** - PostgreSQL database storage
- **Zetian.Storage.MongoDB** - MongoDB NoSQL storage
- **Zetian.Storage.Redis** - Redis cache/storage
- **Zetian.Storage.AzureBlob** - Azure Blob Storage
- **Zetian.Storage.AmazonS3** - Amazon S3 and S3-compatible storage

## Installation

```bash
# Install the core package (usually not needed directly)
dotnet add package Zetian.Storage

# Install specific storage providers
dotnet add package Zetian.Storage.SqlServer
dotnet add package Zetian.Storage.PostgreSQL
dotnet add package Zetian.Storage.MongoDB
# etc...
```

## Base Classes

### BaseStorageConfiguration

Base configuration class that all storage providers inherit from:

```csharp
public abstract class BaseStorageConfiguration
{
    public double MaxMessageSizeMB { get; set; }
    public bool CompressMessageBody { get; set; }
    public bool EnableRetry { get; set; }
    public int MaxRetryAttempts { get; set; }
    public int RetryDelayMs { get; set; }
    public int ConnectionTimeoutSeconds { get; set; }
    public bool LogErrors { get; set; }
}
```

## Creating Custom Storage Providers

To create a custom storage provider:

1. Reference the `Zetian.Storage` package
2. Implement the `IMessageStore` interface
3. Inherit from `BaseStorageConfiguration` for your configuration
4. Create extension methods for `SmtpServerBuilder`

## License

MIT