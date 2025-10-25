# Zetian.Storage.AmazonS3 - S3 Object Storage

[![NuGet-Version](https://img.shields.io/nuget/v/Zetian.Storage.AmazonS3.svg?label=NuGet)](https://www.nuget.org/packages/Zetian.Storage.AmazonS3)
[![NuGet-Download](https://img.shields.io/nuget/dt/Zetian.Storage.AmazonS3?label=Download)](https://www.nuget.org/api/v2/package/Zetian.Storage.AmazonS3)
[![License](https://img.shields.io/github/license/Taiizor/Zetian.svg?label=License)](https://github.com/Taiizor/Zetian/blob/develop/LICENSE)

Amazon S3 and S3-compatible storage provider for Zetian SMTP Server. Provides enterprise-grade object storage with features including server-side encryption, lifecycle management, versioning, transfer acceleration, intelligent tiering, and support for S3-compatible services like MinIO, Wasabi, and DigitalOcean Spaces. Perfect for organizations requiring scalable, durable, and cost-effective message storage with global accessibility.

## ⚡ Features

- 🌐 **AWS S3 Native** - Full Amazon S3 integration
- 🔀 **S3-Compatible** - Works with MinIO, Wasabi, DigitalOcean Spaces
- 🔐 **Encryption** - SSE-S3, SSE-KMS, and client-side encryption
- 📊 **Lifecycle Rules** - Automatic archival and expiration
- 📦 **Versioning** - Keep multiple versions of messages
- 🚀 **Transfer Acceleration** - Fast global uploads
- 🏷️ **Object Tagging** - Organize and manage messages
- ❄️ **Storage Classes** - Standard, IA, Glacier, Deep Archive
- 🌍 **Cross-Region Replication** - Geographic redundancy
- 📏 **Intelligent Tiering** - Automatic cost optimization

## 📦 Installation

```bash
# Install SMTP Server and Storage Provider
dotnet add package Zetian
dotnet add package Zetian.Storage.AmazonS3
```

## 🚀 Quick Start

### Basic Configuration

```csharp
using Zetian.Server;
using Zetian.Storage.AmazonS3.Extensions;

// Configure with AWS S3
var server = new SmtpServerBuilder()
    .Port(25)
    .WithS3Storage(
        "AKIAIOSFODNN7EXAMPLE",
        "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
        "my-smtp-bucket")
    .Build();

await server.StartAsync();
```

### Advanced Configuration

```csharp
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

### S3-Compatible Storage

```csharp
// MinIO
var server = new SmtpServerBuilder()
    .Port(25)
    .WithS3CompatibleStorage(
        "http://localhost:9000",
        "minioadmin",
        "minioadmin",
        "smtp-messages",
        config =>
        {
            config.ForcePathStyle = true; // Required for MinIO
            config.Region = "us-east-1";
            config.NamingFormat = S3NamingFormat.YearMonth;
            config.CompressMessageBody = true;
        })
    .Build();
```

## 🛠️ Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `AccessKeyId` | string | - | AWS Access Key ID |
| `SecretAccessKey` | string | - | AWS Secret Access Key |
| `BucketName` | string | - | S3 bucket name (required) |
| `Region` | string | "us-east-1" | AWS region |
| `ServiceUrl` | string | - | Custom endpoint for S3-compatible services |
| `ForcePathStyle` | bool | false | Use path-style addressing |
| `KeyPrefix` | string | "smtp/" | Prefix for all object keys |
| `NamingFormat` | enum | DateHierarchy | Object naming format |
| `StorageClass` | enum | Standard | S3 storage class |
| `EnableServerSideEncryption` | bool | true | Enable server-side encryption |
| `KmsKeyId` | string | - | KMS key ID for encryption |
| `AutoCreateBucket` | bool | true | Create bucket if it doesn't exist |
| `EnableVersioning` | bool | false | Enable object versioning |
| `UseTransferAcceleration` | bool | false | Enable transfer acceleration |
| `EnableLifecycleRules` | bool | false | Configure lifecycle rules |
| `TransitionToIADays` | int | 30 | Days before moving to Infrequent Access |
| `TransitionToGlacierDays` | int | 90 | Days before moving to Glacier |
| `ExpirationDays` | int | 365 | Days before automatic deletion |
| `CompressMessageBody` | bool | false | Compress message bodies |
| `MaxMessageSizeMB` | double | 100 | Maximum message size in MB |
| `EnableRetry` | bool | true | Enable retry logic |
| `MaxRetryAttempts` | int | 3 | Maximum retry attempts |
| `RetryDelayMs` | int | 1000 | Delay between retries |
| `ConnectionTimeoutSeconds` | int | 30 | Connection timeout |
| `LogErrors` | bool | true | Whether to log errors |

## 🎯 Usage Examples

### Storage Classes

```csharp
// Standard storage class
config.StorageClass = S3StorageClass.Standard;

// Infrequent Access
config.StorageClass = S3StorageClass.StandardIA;

// Intelligent Tiering (automatic)
config.StorageClass = S3StorageClass.IntelligentTiering;

// Archive storage
config.StorageClass = S3StorageClass.Glacier;
config.StorageClass = S3StorageClass.GlacierDeepArchive;
```

### Object Naming Strategies

```csharp
// Date hierarchy: /2024/01/15/msg-123.eml
config.NamingFormat = S3NamingFormat.DateHierarchy;

// Year-Month: /2024-01/msg-123.eml
config.NamingFormat = S3NamingFormat.YearMonth;

// Hourly partition: /2024/01/15/14/msg-123.eml
config.NamingFormat = S3NamingFormat.HourlyPartition;

// Flat structure - all messages in prefix
config.NamingFormat = S3NamingFormat.Flat;
```

## 🔐 Authentication Methods

### Access Keys

```csharp
// Traditional authentication
.WithS3Storage(
    accessKeyId: "AKIAIOSFODNN7EXAMPLE",
    secretAccessKey: "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
    bucketName: "my-smtp-bucket")
```

### IAM Roles (EC2/ECS/Lambda)

```csharp
// Use with IAM role (empty credentials)
.WithS3Storage(
    "",  // Empty access key for IAM role
    "",  // Empty secret key for IAM role
    "my-smtp-bucket",
    config =>
    {
        config.Region = "us-west-2";
    })
```

### AWS SSO/Identity Center

```csharp
// With minimal configuration
.WithS3Storage(
    "AKIAIOSFODNN7EXAMPLE",
    "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
    "my-smtp-bucket")  // Uses default region and settings
```

## 🔀 S3-Compatible Services

### MinIO

```csharp
.WithS3CompatibleStorage(
    serviceUrl: "http://localhost:9000",
    accessKeyId: "minioadmin",
    secretAccessKey: "minioadmin",
    bucketName: "smtp-messages",
    config =>
    {
        config.ForcePathStyle = true; // Required for MinIO
    })
```

### Wasabi

```csharp
.WithS3CompatibleStorage(
    serviceUrl: "https://s3.wasabisys.com",
    accessKeyId: "WASABI_ACCESS_KEY",
    secretAccessKey: "WASABI_SECRET_KEY",
    bucketName: "smtp-messages",
    config =>
    {
        config.Region = "us-east-1";
    })
```

### DigitalOcean Spaces

```csharp
.WithS3CompatibleStorage(
    serviceUrl: "https://nyc3.digitaloceanspaces.com",
    accessKeyId: "DO_SPACES_KEY",
    secretAccessKey: "DO_SPACES_SECRET",
    bucketName: "smtp-messages",
    config =>
    {
        config.Region = "nyc3";
    })
```

### Backblaze B2

```csharp
.WithS3CompatibleStorage(
    serviceUrl: "https://s3.us-west-000.backblazeb2.com",
    accessKeyId: "B2_KEY_ID",
    secretAccessKey: "B2_APPLICATION_KEY",
    bucketName: "smtp-messages")
```

## 📊 Lifecycle Management

### Configure Lifecycle Rules

```csharp
config.EnableLifecycleRules = true;

// Transition rules
config.LifecycleRules = new[]
{
    new S3LifecycleRule
    {
        Id = "MoveToIA",
        DaysAfterCreation = 30,
        Transition = S3StorageClass.StandardIA
    },
    new S3LifecycleRule
    {
        Id = "MoveToGlacier",
        DaysAfterCreation = 90,
        Transition = S3StorageClass.Glacier
    },
    new S3LifecycleRule
    {
        Id = "DeleteOld",
        DaysAfterCreation = 365,
        Expiration = true
    }
};
```

### Intelligent Tiering

```csharp
// Automatic cost optimization
config.StorageClass = S3StorageClass.IntelligentTiering;
config.IntelligentTieringConfiguration = new IntelligentTieringConfiguration
{
    Id = "SmtpMessageTiering",
    ArchiveAccessTierDays = 90,
    DeepArchiveAccessTierDays = 180
};
```

## 🔒 Security Features

### Server-Side Encryption

```csharp
// SSE-S3 (AES-256)
config.EnableServerSideEncryption = true;

// SSE-KMS
config.EnableServerSideEncryption = true;
config.KmsKeyId = "arn:aws:kms:us-west-2:111122223333:key/1234abcd-12ab-34cd-56ef-1234567890ab";

// SSE-C (Customer-provided key)
config.CustomerEncryptionKey = Convert.ToBase64String(encryptionKey);
```

### Bucket Policies

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Principal": {"AWS": "arn:aws:iam::123456789012:user/smtp-app"},
      "Action": ["s3:PutObject", "s3:GetObject"],
      "Resource": "arn:aws:s3:::my-smtp-bucket/*"
    }
  ]
}
```

### Object Lock

```csharp
// Enable compliance mode
config.EnableObjectLock = true;
config.ObjectLockMode = ObjectLockMode.Compliance;
config.ObjectLockRetentionDays = 30;
```

## 📦 Versioning & Recovery

```csharp
// Enable versioning
config.EnableVersioning = true;

// List all versions
var versions = await s3Client.ListObjectVersionsAsync(
    new ListObjectVersionsRequest
    {
        BucketName = "my-smtp-bucket",
        Prefix = "messages/"
    });

// Restore specific version
await s3Client.CopyObjectAsync(
    sourceBucket: "my-smtp-bucket",
    sourceKey: "messages/msg-123.eml",
    sourceVersionId: "versionId123",
    destinationBucket: "my-smtp-bucket",
    destinationKey: "messages/msg-123-restored.eml");
```

## 🌐 Cross-Region Replication

```csharp
// Configure replication
config.EnableReplication = true;
config.ReplicationConfiguration = new ReplicationConfiguration
{
    Role = "arn:aws:iam::123456789012:role/replication-role",
    Rules = new[]
    {
        new ReplicationRule
        {
            Id = "ReplicateAll",
            Priority = 1,
            Status = ReplicationRuleStatus.Enabled,
            Destination = new ReplicationDestination
            {
                BucketArn = "arn:aws:s3:::my-smtp-bucket-replica",
                StorageClass = S3StorageClass.StandardIA
            }
        }
    }
};
```

## 🚀 Performance Optimization

### Transfer Acceleration

```csharp
// Enable for faster global uploads
config.UseTransferAcceleration = true;

// Accelerated endpoint
config.AcceleratedEndpoint = "my-smtp-bucket.s3-accelerate.amazonaws.com";
```

### Multipart Upload

```csharp
// Configure multipart threshold
config.MultipartUploadThresholdMB = 100; // Files > 100MB
config.MultipartUploadPartSizeMB = 10; // 10MB parts

// Parallel parts
config.MaxConcurrentUploads = 10;
```

### Request Metrics

```csharp
// Enable CloudWatch metrics
config.EnableRequestMetrics = true;
config.MetricsConfiguration = new MetricsConfiguration
{
    Id = "SmtpStorageMetrics",
    Filter = new MetricsFilter
    {
        Prefix = "messages/"
    }
};
```

## 💰 Cost Optimization

### Storage Class Analysis

```csharp
// Enable storage class analysis
config.EnableStorageClassAnalysis = true;
config.AnalysisConfiguration = new AnalyticsConfiguration
{
    Id = "SmtpAnalysis",
    ExportConfiguration = new AnalyticsExportDestination
    {
        BucketArn = "arn:aws:s3:::analysis-bucket",
        Prefix = "analysis/smtp/"
    }
};
```

### Cost Allocation Tags

```csharp
// Tag objects for cost tracking
config.DefaultTags = new Dictionary<string, string>
{
    ["Application"] = "SMTP-Server",
    ["Environment"] = "Production",
    ["CostCenter"] = "IT-Operations"
};
```

## 📈 Monitoring & Logging

### CloudWatch Integration

```csharp
// Enable CloudWatch logging
config.EnableCloudWatchLogging = true;
config.CloudWatchLogGroup = "/aws/s3/smtp-storage";

// Custom metrics
config.CustomMetrics = new[]
{
    "MessageSize",
    "ProcessingTime",
    "StorageClass"
};
```

### S3 Access Logging

```csharp
// Enable access logging
config.EnableAccessLogging = true;
config.LoggingConfiguration = new S3BucketLoggingConfig
{
    TargetBucketName = "my-logs-bucket",
    TargetPrefix = "s3-access-logs/smtp/"
};
```

## 🔧 Troubleshooting

### Common Issues

**Access Denied**
```csharp
// Check IAM permissions
// Required: s3:PutObject, s3:GetObject, s3:DeleteObject
// For bucket creation: s3:CreateBucket, s3:PutBucketPolicy
```

**Slow Uploads**
```csharp
// Enable transfer acceleration
config.UseTransferAcceleration = true;

// Increase concurrent uploads
config.MaxConcurrentUploads = 20;

// Use appropriate region
config.Region = "us-east-1"; // Same as bucket region
```

**Glacier Retrieval**
```csharp
// Initiate restore
await s3Client.RestoreObjectAsync(new RestoreObjectRequest
{
    BucketName = "my-smtp-bucket",
    Key = "messages/msg-123.eml",
    Days = 1,
    RestoreRequest = new RestoreRequest
    {
        Days = 1,
        Tier = RestoreTier.Expedited // 1-5 minutes
        // Tier = RestoreTier.Standard // 3-5 hours
        // Tier = RestoreTier.Bulk // 5-12 hours
    }
});
```

## 📋 Requirements

- AWS Account or S3-compatible service
- .NET 6.0, 7.0, 8.0, 9.0, or 10.0
- Zetian SMTP Server package
- AWSSDK.S3 package (included)
- For Transfer Acceleration: Enabled on bucket
- For KMS: KMS key permissions

## 📚 Documentation & Support

- **Documentation**: [Zetian Documentation](https://zetian.soferity.com)
- **AWS S3 Docs**: [Amazon S3 Documentation](https://docs.aws.amazon.com/s3/)
- **Examples**: [GitHub Examples](https://github.com/Taiizor/Zetian/tree/develop/examples)
- **Issues**: [GitHub Issues](https://github.com/Taiizor/Zetian/issues)
- **Discussions**: [GitHub Discussions](https://github.com/Taiizor/Zetian/discussions)

## 📄 License

MIT License - see [LICENSE](https://github.com/Taiizor/Zetian/blob/develop/LICENSE)

---

**Built with ❤️ for the .NET community**