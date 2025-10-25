# Zetian.Storage.AmazonS3

Amazon S3 and S3-compatible storage provider for Zetian SMTP Server.

## Installation

```bash
dotnet add package Zetian.Storage.AmazonS3
```

## Configuration

### Amazon S3

```csharp
using Zetian.Storage.AmazonS3.Extensions;

var server = new SmtpServerBuilder()
    .Port(25)
    .WithS3Storage(
        "AKIAIOSFODNN7EXAMPLE",
        "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
        "my-smtp-bucket",
        config =>
        {
            config.Region = "us-west-2";
            config.KeyPrefix = "messages/";
            config.NamingFormat = S3NamingFormat.DateHierarchy;
            config.StorageClass = S3StorageClass.Standard;
            config.EnableServerSideEncryption = true;
            config.CompressMessageBody = true;
            config.EnableVersioning = true;
            config.EnableLifecycleRules = true;
            config.TransitionToIADays = 30;
            config.TransitionToGlacierDays = 90;
            config.ExpirationDays = 365;
        })
    .Build();
```

### S3-Compatible Storage (MinIO, Wasabi, etc.)

```csharp
var server = new SmtpServerBuilder()
    .Port(25)
    .WithS3CompatibleStorage(
        "http://localhost:9000", // MinIO endpoint
        "minioadmin",
        "minioadmin",
        "smtp-messages",
        config =>
        {
            config.Region = "us-east-1";
            config.NamingFormat = S3NamingFormat.YearMonth;
            config.CompressMessageBody = true;
            config.ForcePathStyle = true; // Required for MinIO
        })
    .Build();
```

## Features

- **AWS S3 Support** - Full Amazon S3 integration
- **S3-Compatible Services** - Works with MinIO, Wasabi, DigitalOcean Spaces, etc.
- **Server-side Encryption** - AES-256 or KMS encryption
- **Lifecycle Management** - Automatic archival and expiration
- **Versioning** - Keep multiple versions of messages
- **Transfer Acceleration** - Speed up uploads for global access
- **Object Tagging** - Tag messages for easy management
- **IAM Role Support** - Use EC2 instance roles for authentication

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| AccessKeyId | string | - | AWS Access Key ID |
| SecretAccessKey | string | - | AWS Secret Access Key |
| BucketName | string | - | S3 bucket name (required) |
| Region | string | "us-east-1" | AWS region |
| ServiceUrl | string | - | Custom endpoint for S3-compatible services |
| ForcePathStyle | bool | false | Use path-style addressing |
| KeyPrefix | string | "smtp/" | Prefix for all object keys |
| NamingFormat | enum | DateHierarchy | Object naming strategy |
| StorageClass | enum | Standard | S3 storage class |
| EnableServerSideEncryption | bool | true | Enable encryption at rest |
| KmsKeyId | string | - | KMS key ID for encryption |
| AutoCreateBucket | bool | true | Create bucket if it doesn't exist |
| EnableVersioning | bool | false | Enable object versioning |
| UseTransferAcceleration | bool | false | Enable transfer acceleration |
| EnableLifecycleRules | bool | false | Configure lifecycle rules |
| TransitionToIADays | int | 30 | Days before moving to Infrequent Access |
| TransitionToGlacierDays | int | 90 | Days before moving to Glacier |
| ExpirationDays | int | 365 | Days before automatic deletion |
| CompressMessageBody | bool | false | Compress message bodies |
| MaxMessageSizeMB | double | 100 | Maximum message size in MB |

## Storage Classes

- **Standard** - High-performance for frequently accessed data
- **StandardIA** - Lower cost for infrequently accessed data
- **IntelligentTiering** - Automatic cost optimization
- **OneZoneIA** - Lower cost, single availability zone
- **Glacier** - Long-term archive storage
- **GlacierDeepArchive** - Lowest cost for rare access

## Naming Formats

### Flat
All messages in prefix:
```
smtp/message-123.eml
smtp/message-456.eml
```

### DateHierarchy (Default)
Organized by date:
```
smtp/2024/01/15/message-123.eml
smtp/2024/01/15/message-456.eml
```

### YearMonth
Organized by year and month:
```
smtp/2024-01/message-123.eml
smtp/2024-01/message-456.eml
```

### HourlyPartition
For high volume:
```
smtp/2024/01/15/14/message-123.eml
smtp/2024/01/15/14/message-456.eml
```

## Lifecycle Rules

When enabled, automatically manages object lifecycle:

1. **Transition to Infrequent Access**: After 30 days (configurable)
2. **Archive to Glacier**: After 90 days (configurable)
3. **Expire**: After 365 days (configurable)

## Authentication Methods

### Access Keys
Traditional authentication:
```csharp
.WithS3Storage(accessKeyId, secretAccessKey, bucketName)
```

### IAM Roles
For EC2 instances with IAM roles:
```csharp
.WithS3Storage("", "", bucketName) // Empty credentials
```

## S3-Compatible Services

### MinIO
```csharp
.WithS3CompatibleStorage("http://localhost:9000", ...)
```

### Wasabi
```csharp
.WithS3CompatibleStorage("https://s3.wasabisys.com", ...)
```

### DigitalOcean Spaces
```csharp
.WithS3CompatibleStorage("https://nyc3.digitaloceanspaces.com", ...)
```

## Security

### Encryption
- **SSE-S3**: AES-256 encryption managed by AWS
- **SSE-KMS**: Encryption with AWS Key Management Service
- **Client-side**: Compress/encrypt before upload

### Access Control
- Use IAM policies to restrict access
- Enable bucket versioning for data protection
- Configure bucket lifecycle policies

## Performance Tips

- **Enable Transfer Acceleration** for global uploads
- **Use appropriate storage class** based on access patterns
- **Enable compression** to reduce storage costs
- **Set proper lifecycle rules** for automatic archival
- **Use multipart uploads** for large files (handled by SDK)

## Cost Optimization

1. Use lifecycle rules to move old data to cheaper storage
2. Enable compression to reduce storage size
3. Choose the right storage class for your access patterns
4. Set expiration rules for automatic cleanup
5. Use S3 Intelligent-Tiering for automatic optimization

## Requirements

- AWS Account or S3-compatible service
- .NET 6.0 or later

## License

MIT