# Zetian.Storage.AzureBlob - Azure Cloud Storage

[![NuGet-Version](https://img.shields.io/nuget/v/Zetian.Storage.AzureBlob.svg?label=NuGet)](https://www.nuget.org/packages/Zetian.Storage.AzureBlob)
[![NuGet-Download](https://img.shields.io/nuget/dt/Zetian.Storage.AzureBlob?label=Download)](https://www.nuget.org/api/v2/package/Zetian.Storage.AzureBlob)
[![License](https://img.shields.io/github/license/Taiizor/Zetian.svg?label=License)](https://github.com/Taiizor/Zetian/blob/develop/LICENSE)

Azure Blob Storage provider for Zetian SMTP Server. Provides enterprise-grade cloud storage with features including hierarchical namespace support, access tier management, soft delete capabilities, Azure AD authentication, lifecycle management, and built-in encryption. Perfect for organizations using Microsoft Azure requiring scalable, secure, and cost-effective message archival with seamless integration into Azure ecosystem.

## ⚡ Features

- 📸 **Snapshots** - Point-in-time recovery
- 🗑️ **Soft Delete** - Recover deleted messages
- 📈 **Analytics** - Storage analytics and metrics
- 🌍 **Geo-Redundancy** - Cross-region replication
- 🔒 **Encryption** - At-rest and in-transit encryption
- ❄️ **Access Tiers** - Hot, Cool, and Archive tier support
- ☁️ **Azure Native** - Full Azure Blob Storage integration
- 📊 **Lifecycle Management** - Automatic tier transitions
- 🗂️ **Hierarchical Structure** - Organized folder-like storage
- 🔐 **Azure AD Auth** - Managed identity and service principal

## 📦 Installation

```bash
# Install SMTP Server and Storage Provider
dotnet add package Zetian
dotnet add package Zetian.Storage.AzureBlob
```

## 🚀 Quick Start

### Basic Configuration

```csharp
using Zetian.Server;
using Zetian.Storage.AzureBlob.Extensions;

// Configure with connection string
var server = new SmtpServerBuilder()
    .Port(25)
    .WithAzureBlobStorage("DefaultEndpointsProtocol=https;AccountName=myaccount;AccountKey=mykey;EndpointSuffix=core.windows.net")
    .Build();

await server.StartAsync();
```

### Advanced Configuration

```csharp
var server = new SmtpServerBuilder()
    .Port(25)
    .WithAzureBlobStorage(
        "DefaultEndpointsProtocol=https;AccountName=myaccount;AccountKey=mykey;EndpointSuffix=core.windows.net",
        config =>
        {
            config.MaxMessageSizeMB = 100;
            config.EnableSoftDelete = true;
            config.CompressMessageBody = true;
            config.SoftDeleteRetentionDays = 7;
            config.AccessTier = BlobAccessTier.Hot;
            config.ContainerName = "smtp-messages";
            config.StoreMetadataAsBlobProperties = true;
            config.NamingFormat = BlobNamingFormat.DateHierarchy;
        })
    .Build();
```

### Azure AD Authentication

```csharp
// Using Azure AD Authentication
var server = new SmtpServerBuilder()
    .Port(25)
    .WithAzureBlobStorageAD(
        "mystorageaccount",
        config =>
        {
            config.ContainerName = "email-archive";
            config.AccessTier = BlobAccessTier.Cool;
            config.NamingFormat = BlobNamingFormat.YearMonth;
        })
    .Build();
```

## 🛠️ Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `ConnectionString` | string | - | Azure Storage connection string |
| `ContainerName` | string | "smtp-messages" | Container name for messages |
| `UseAzureAdAuthentication` | bool | false | Use Azure AD authentication |
| `StorageAccountName` | string | - | Storage account name (for AD auth) |
| `AutoCreateContainer` | bool | true | Auto-create container if needed |
| `NamingFormat` | enum | DateHierarchy | Blob naming format |
| `AccessTier` | enum | Hot | Default blob access tier |
| `StoreMetadataAsBlobProperties` | bool | true | Store metadata as blob properties |
| `EnableSoftDelete` | bool | false | Enable soft delete feature |
| `SoftDeleteRetentionDays` | int | 7 | Days to retain deleted blobs |
| `MaxMessageSizeMB` | double | 100 | Maximum message size in MB |
| `CompressMessageBody` | bool | false | Compress message bodies |
| `EnableRetry` | bool | true | Enable retry logic |
| `MaxRetryAttempts` | int | 3 | Maximum retry attempts |
| `RetryDelayMs` | int | 1000 | Delay between retries |
| `ConnectionTimeoutSeconds` | int | 30 | Connection timeout |
| `LogErrors` | bool | true | Whether to log errors |

## 🎯 Usage Examples

### Connection Options

```csharp
// Connection string with SAS token
.WithAzureBlobStorage(
    "BlobEndpoint=https://myaccount.blob.core.windows.net/;" +
    "SharedAccessSignature=sv=2020-08-04&ss=b&srt=sco&sp=rwdlacx&se=2024-12-31")

// Emulator for local development
.WithAzureBlobStorage("UseDevelopmentStorage=true")

// Azure Government Cloud
.WithAzureBlobStorage(
    "DefaultEndpointsProtocol=https;AccountName=myaccount;" +
    "AccountKey=mykey;EndpointSuffix=core.usgovcloudapi.net")
```

### Blob Naming Strategies

```csharp
// Flat structure: /msg-123.eml
config.NamingFormat = BlobNamingFormat.Flat;

// Year-Month: /2024-01/msg-123.eml
config.NamingFormat = BlobNamingFormat.YearMonth;

// Domain-based naming
config.NamingFormat = BlobNamingFormat.DomainBased;

// Date hierarchy: /2024/01/15/msg-123.eml
config.NamingFormat = BlobNamingFormat.DateHierarchy;
```

## 🔐 Authentication Methods

### Azure AD with Managed Identity

```csharp
// Azure AD authentication (uses DefaultAzureCredential)
.WithAzureBlobStorageAD("mystorageaccount", config =>
{
    config.ContainerName = "smtp-messages";
    config.NamingFormat = BlobNamingFormat.DateHierarchy;
})

// With specific configuration
.WithAzureBlobStorageAD("mystorageaccount", config =>
{
    config.ContainerName = "email-archive";
    config.AccessTier = BlobAccessTier.Cool;
    config.EnableSoftDelete = true;
})
```

### Connection String with Options

```csharp
.WithAzureBlobStorage(
    "DefaultEndpointsProtocol=https;AccountName=myaccount;AccountKey=mykey",
    config =>
    {
        config.ContainerName = "custom-container";
        config.NamingFormat = BlobNamingFormat.Flat;
    })
```

## 🗑️ Soft Delete & Recovery

```csharp
// Enable soft delete
config.EnableSoftDelete = true;
config.SoftDeleteRetentionDays = 30;

// Recover deleted blob
foreach (var blob in containerClient.GetBlobs(states: BlobStates.Deleted))
{
    var blobClient = containerClient.GetBlobClient(blob.Name);
    await blobClient.UndeleteAsync();
}
```

## 🌍 Geo-Redundancy

```csharp
// Read from secondary region (RA-GRS/RA-GZRS)
config.ConnectionString = 
    "DefaultEndpointsProtocol=https;AccountName=myaccount;" +
    "AccountKey=mykey;EndpointSuffix=core.windows.net;" +
    "BlobEndpoint=https://myaccount-secondary.blob.core.windows.net/";
```

## 📈 Monitoring & Analytics

```csharp
// Enable storage analytics
var serviceProperties = new BlobServiceProperties
{
    Logging = new BlobAnalyticsLogging
    {
        Read = true,
        Write = true,
        Delete = true,
        RetentionPolicy = new BlobRetentionPolicy
        {
            Enabled = true,
            Days = 7
        }
    }
};

await blobServiceClient.SetPropertiesAsync(serviceProperties);
```

## 🔒 Security Features

### Private Endpoints

```csharp
// Connect via private endpoint
config.ConnectionString = 
    "DefaultEndpointsProtocol=https;" +
    "AccountName=myaccount;" +
    "AccountKey=mykey;" +
    "BlobEndpoint=https://myaccount.privatelink.blob.core.windows.net/";
```

## 🚀 Performance Optimization

### Parallel Upload

```csharp
// Configure parallel upload
var uploadOptions = new BlobUploadOptions
{
    TransferOptions = new StorageTransferOptions
    {
        MaximumConcurrency = 8,
        InitialTransferSize = 256 * 1024,
        MaximumTransferSize = 4 * 1024 * 1024
    }
};

await blobClient.UploadAsync(stream, uploadOptions);
```

### Batch Operations

```csharp
// Batch delete
var batchClient = blobServiceClient.GetBlobBatchClient();
var batch = batchClient.CreateBatch();

foreach (var blobName in blobsToDelete)
{
    batch.DeleteBlob(containerName, blobName);
}

await batchClient.SubmitBatchAsync(batch);
```

## 🔧 Troubleshooting

### Common Issues

**Authentication Failed**
```csharp
// Check Azure AD permissions
// Verify tenant ID and client credentials
// Storage Blob Data Contributor role required
```

**Archive Tier Access**
```csharp
// Rehydrate before access
if (blobProperties.AccessTier == AccessTier.Archive)
{
    await blobClient.SetAccessTierAsync(AccessTier.Hot);
    // Wait for rehydration (can take hours)
}
```

## 📋 Requirements

- .NET 6.0, 7.0, 8.0, 9.0, or 10.0
- Zetian SMTP Server package
- Azure.Storage.Blobs package (included)
- For managed identity: Azure AD configuration
- For Data Lake: Hierarchical namespace enabled
- Azure Storage account (General Purpose v2 recommended)

## 📚 Documentation & Support

- **Issues**: [GitHub Issues](https://github.com/Taiizor/Zetian/issues)
- **Examples**: [GitHub Examples](https://github.com/Taiizor/Zetian/tree/develop/examples)
- **Discussions**: [GitHub Discussions](https://github.com/Taiizor/Zetian/discussions)
- **Documentation**: [Zetian Documentation](https://zetian.soferity.com)
- **Azure Docs**: [Azure Blob Storage Documentation](https://docs.microsoft.com/azure/storage/blobs)

## 📄 License

MIT License - see [LICENSE](https://github.com/Taiizor/Zetian/blob/develop/LICENSE)

---

**Built with ❤️ for the .NET community**