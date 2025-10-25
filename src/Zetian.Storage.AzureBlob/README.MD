# Zetian.Storage.AzureBlob

Azure Blob Storage provider for Zetian SMTP Server.

## Installation

```bash
dotnet add package Zetian.Storage.AzureBlob
```

## Configuration

### With Connection String

```csharp
using Zetian.Storage.AzureBlob.Extensions;

var server = new SmtpServerBuilder()
    .Port(25)
    .WithAzureBlobStorage(
        "DefaultEndpointsProtocol=https;AccountName=myaccount;AccountKey=mykey;EndpointSuffix=core.windows.net",
        config =>
        {
            config.ContainerName = "smtp-messages";
            config.NamingFormat = BlobNamingFormat.DateHierarchy;
            config.AccessTier = BlobAccessTier.Hot;
            config.CompressMessageBody = true;
            config.StoreMetadataAsBlobProperties = true;
            config.EnableSoftDelete = true;
            config.SoftDeleteRetentionDays = 7;
        })
    .Build();
```

### With Azure AD Authentication

```csharp
var server = new SmtpServerBuilder()
    .Port(25)
    .WithAzureBlobStorageAD(
        "mystorageaccount",
        config =>
        {
            config.ContainerName = "email-archive";
            config.NamingFormat = BlobNamingFormat.YearMonth;
            config.AccessTier = BlobAccessTier.Cool;
        })
    .Build();
```

## Features

- **Azure AD authentication** - Secure authentication without connection strings
- **Automatic container creation** - Containers created automatically if needed
- **Hierarchical storage** - Organize messages by date or domain
- **Access tier management** - Hot, Cool, or Archive tiers
- **Soft delete** - Recover accidentally deleted messages
- **Metadata storage** - Store message metadata as blob properties
- **Message compression** - Optional GZIP compression
- **Blob tagging** - Tag blobs for easy searching

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| ConnectionString | string | - | Azure Storage connection string |
| ContainerName | string | "smtp-messages" | Container name for messages |
| UseAzureAdAuthentication | bool | false | Use Azure AD authentication |
| StorageAccountName | string | - | Storage account name (for Azure AD) |
| AutoCreateContainer | bool | true | Create container if it doesn't exist |
| NamingFormat | enum | DateHierarchy | Blob naming strategy |
| AccessTier | enum | Hot | Storage access tier |
| StoreMetadataAsBlobProperties | bool | true | Store metadata as blob properties |
| EnableSoftDelete | bool | false | Enable soft delete feature |
| SoftDeleteRetentionDays | int | 7 | Soft delete retention period |
| CompressMessageBody | bool | false | Compress message bodies |
| MaxMessageSizeMB | double | 100 | Maximum message size in MB |

## Naming Formats

### Flat
All messages stored in container root:
```
message-123.eml
message-456.eml
```

### DateHierarchy (Default)
Organized by date:
```
2024/01/15/message-123.eml
2024/01/15/message-456.eml
```

### YearMonth
Organized by year and month:
```
2024-01/message-123.eml
2024-01/message-456.eml
```

### DomainBased
Organized by sender domain:
```
messages/message-123.eml
messages/message-456.eml
```

## Access Tiers

- **Hot**: Optimized for data that is accessed frequently
- **Cool**: Optimized for data that is infrequently accessed and stored for at least 30 days
- **Archive**: Optimized for data that is rarely accessed and stored for at least 180 days

## Metadata and Tags

When `StoreMetadataAsBlobProperties` is enabled:
- Message metadata is stored as blob properties
- Up to 10 tags are added for indexing
- Tags can be used for searching and filtering

## Soft Delete

When enabled:
- Deleted blobs are retained for the specified period
- Deleted blobs can be recovered within the retention period
- Requires appropriate permissions on the storage account

## Authentication

### Connection String
Traditional authentication using storage account key:
```csharp
.WithAzureBlobStorage("DefaultEndpointsProtocol=https;AccountName=...")
```

### Azure AD
Modern authentication using Azure Active Directory:
```csharp
.WithAzureBlobStorageAD("mystorageaccount")
```

Requires:
- Azure.Identity credential setup
- Appropriate RBAC permissions (Storage Blob Data Contributor)

## Performance Considerations

- **Compression**: Reduces storage costs but adds CPU overhead
- **Access Tier**: Choose based on access patterns
- **Metadata**: Blob properties are limited in size
- **Container size**: No practical limit on container size

## Requirements

- Azure Storage Account
- .NET 6.0 or later

## License

MIT
