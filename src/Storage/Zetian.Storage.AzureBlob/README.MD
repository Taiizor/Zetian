# Zetian.Storage.AzureBlob - Azure Cloud Storage

[![NuGet-Version](https://img.shields.io/nuget/v/Zetian.Storage.AzureBlob.svg?label=NuGet)](https://www.nuget.org/packages/Zetian.Storage.AzureBlob)
[![NuGet-Download](https://img.shields.io/nuget/dt/Zetian.Storage.AzureBlob?label=Download)](https://www.nuget.org/api/v2/package/Zetian.Storage.AzureBlob)
[![License](https://img.shields.io/github/license/Taiizor/Zetian.svg?label=License)](https://github.com/Taiizor/Zetian/blob/develop/LICENSE)

Azure Blob Storage provider for Zetian SMTP Server. Provides enterprise-grade cloud storage with features including hierarchical namespace support, access tier management, soft delete capabilities, Azure AD authentication, lifecycle management, and built-in encryption. Perfect for organizations using Microsoft Azure requiring scalable, secure, and cost-effective message archival with seamless integration into Azure ecosystem.

## ⚡ Features

- ☁️ **Azure Native** - Full Azure Blob Storage integration
- 🗂️ **Hierarchical Structure** - Organized folder-like storage
- ❄️ **Access Tiers** - Hot, Cool, and Archive tier support
- 🔐 **Azure AD Auth** - Managed identity and service principal
- 🗑️ **Soft Delete** - Recover deleted messages
- 📊 **Lifecycle Management** - Automatic tier transitions
- 🔒 **Encryption** - At-rest and in-transit encryption
- 📸 **Snapshots** - Point-in-time recovery
- 🌍 **Geo-Redundancy** - Cross-region replication
- 📈 **Analytics** - Storage analytics and metrics

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
    .WithAzureBlobStorage(config =>
    {
        config.ConnectionString = "DefaultEndpointsProtocol=https;AccountName=myaccount;AccountKey=mykey;EndpointSuffix=core.windows.net";
        config.ContainerName = "smtp-messages";
        config.NamingFormat = BlobNamingFormat.DateHierarchy;
        config.AccessTier = BlobAccessTier.Hot;
        config.CompressMessageBody = true;
        config.StoreMetadataAsBlobProperties = true;
        config.EnableSoftDelete = true;
        config.SoftDeleteRetentionDays = 7;
        config.EnableVersioning = true;
        config.MaxMessageSizeMB = 100;
    })
    .Build();
```

### Azure AD Authentication

```csharp
// Using Managed Identity
var server = new SmtpServerBuilder()
    .Port(25)
    .WithAzureBlobStorageAD(
        "mystorageaccount",
        config =>
        {
            config.ContainerName = "email-archive";
            config.UseManagedIdentity = true;
            config.NamingFormat = BlobNamingFormat.YearMonth;
            config.AccessTier = BlobAccessTier.Cool;
        })
    .Build();
```

## 🛠️ Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `ConnectionString` | string | - | Azure Storage connection string |
| `StorageAccountName` | string | - | Storage account name (for AD auth) |
| `ContainerName` | string | "smtp-messages" | Container name for messages |
| `NamingFormat` | enum | DateHierarchy | Blob naming strategy |
| `BlobPrefix` | string | "messages/" | Prefix for all blobs |
| `AccessTier` | enum | Hot | Default blob access tier |
| `CompressMessageBody` | bool | false | Compress message bodies |
| `StoreMetadataAsBlobProperties` | bool | true | Store metadata as blob properties |
| `EnableSoftDelete` | bool | false | Enable soft delete feature |
| `SoftDeleteRetentionDays` | int | 7 | Days to retain deleted blobs |
| `EnableVersioning` | bool | false | Enable blob versioning |
| `UseHierarchicalNamespace` | bool | false | Use Data Lake Gen2 features |
| `MaxMessageSizeMB` | double | 100 | Maximum message size in MB |
| `UseManagedIdentity` | bool | false | Use Azure Managed Identity |
| `EnableRetry` | bool | true | Enable retry logic |
| `MaxRetryAttempts` | int | 3 | Maximum retry attempts |

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
// Date hierarchy: /2024/01/15/msg-123.eml
config.NamingFormat = BlobNamingFormat.DateHierarchy;

// Year-Month: /2024-01/msg-123.eml
config.NamingFormat = BlobNamingFormat.YearMonth;

// Flat structure: /msg-123.eml
config.NamingFormat = BlobNamingFormat.Flat;

// Custom format
config.CustomNamingFunction = (messageId, date) => 
    $"{date:yyyy/MM/dd}/{messageId}.eml";
```

## 🔐 Authentication Methods

### Azure AD with Managed Identity

```csharp
// System-assigned managed identity
.WithAzureBlobStorageAD("mystorageaccount", config =>
{
    config.UseManagedIdentity = true;
})

// User-assigned managed identity
.WithAzureBlobStorageAD("mystorageaccount", config =>
{
    config.UseManagedIdentity = true;
    config.ManagedIdentityClientId = "client-id";
})
```

### Service Principal

```csharp
.WithAzureBlobStorageAD("mystorageaccount", config =>
{
    config.TenantId = "tenant-id";
    config.ClientId = "client-id";
    config.ClientSecret = "client-secret";
})
```

## 📊 Lifecycle Management

```csharp
// Configure automatic lifecycle policies
config.LifecyclePolicies = new[]
{
    new LifecyclePolicy
    {
        Name = "MoveToCool",
        DaysAfterCreation = 30,
        Action = LifecycleAction.TierToCool
    },
    new LifecyclePolicy
    {
        Name = "MoveToArchive",
        DaysAfterCreation = 90,
        Action = LifecycleAction.TierToArchive
    },
    new LifecyclePolicy
    {
        Name = "DeleteOld",
        DaysAfterCreation = 365,
        Action = LifecycleAction.Delete
    }
};
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

## 📸 Snapshots & Versioning

```csharp
// Enable versioning
config.EnableVersioning = true;

// Create snapshot
var snapshotResponse = await blobClient.CreateSnapshotAsync();
var snapshotId = snapshotResponse.Value.Snapshot;

// Read from snapshot
var snapshotClient = blobClient.WithSnapshot(snapshotId);
var content = await snapshotClient.DownloadContentAsync();
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

### Encryption

```csharp
// Customer-managed keys
config.EncryptionScope = "my-encryption-scope";

// Client-side encryption
var encryptionOptions = new ClientSideEncryptionOptions(
    ClientSideEncryptionVersion.V2_0)
{
    KeyEncryptionKey = keyEncryptionKey,
    KeyResolver = keyResolver,
    KeyWrapAlgorithm = "RSA-OAEP"
};
```

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
// Storage Blob Data Contributor role required
// Verify tenant ID and client credentials
```

**Request Rate Exceeded**
```csharp
// Implement exponential backoff
config.RetryOptions = new RetryOptions
{
    MaxRetries = 5,
    Delay = TimeSpan.FromSeconds(1),
    MaxDelay = TimeSpan.FromSeconds(30),
    Mode = RetryMode.Exponential
};
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

- Azure Storage account (General Purpose v2 recommended)
- .NET 6.0, 7.0, 8.0, 9.0, or 10.0
- Zetian SMTP Server package
- Azure.Storage.Blobs package (included)
- For Data Lake: Hierarchical namespace enabled
- For managed identity: Azure AD configuration

## 📚 Documentation & Support

- **Documentation**: [Zetian Documentation](https://zetian.soferity.com)
- **Azure Docs**: [Azure Blob Storage Documentation](https://docs.microsoft.com/azure/storage/blobs/)
- **Examples**: [GitHub Examples](https://github.com/Taiizor/Zetian/tree/develop/examples)
- **Issues**: [GitHub Issues](https://github.com/Taiizor/Zetian/issues)
- **Discussions**: [GitHub Discussions](https://github.com/Taiizor/Zetian/discussions)

## 📄 License

MIT License - see [LICENSE](https://github.com/Taiizor/Zetian/blob/develop/LICENSE)

---

**Built with ❤️ for the .NET community**