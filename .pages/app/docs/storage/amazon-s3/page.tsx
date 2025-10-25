'use client';

import Link from 'next/link';
import { Cloud, Globe, CheckCircle, Zap, Server, Lock, Shield } from 'lucide-react';
import CodeBlock from '@/components/CodeBlock';

const installCommand = `dotnet add package Zetian
dotnet add package Zetian.Storage.AmazonS3`;

const quickStartExample = `using Zetian.Server;
using Zetian.Storage.AmazonS3.Extensions;

// Basic S3 setup
var server = new SmtpServerBuilder()
    .Port(25)
    .WithS3Storage(
        "AKIAIOSFODNN7EXAMPLE",
        "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
        "my-smtp-bucket")
    .Build();

await server.StartAsync();`;

const s3CompatibleExample = `// MinIO, Wasabi, or other S3-compatible services
var server = new SmtpServerBuilder()
    .Port(25)
    .WithS3CompatibleStorage(
        "http://localhost:9000", // MinIO endpoint
        "minioadmin",
        "minioadmin",
        "smtp-messages",
        config =>
        {
            config.ForcePathStyle = true; // Required for MinIO
            config.Region = "us-east-1";
        })
    .Build();

// Wasabi example
var server = new SmtpServerBuilder()
    .Port(25)
    .WithS3CompatibleStorage(
        "https://s3.wasabisys.com",
        "accessKey",
        "secretKey",
        "my-bucket",
        config =>
        {
            config.Region = "us-east-1";
        })
    .Build();`;

const advancedExample = `var server = new SmtpServerBuilder()
    .Port(25)
    .WithS3Storage(
        accessKeyId: Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID"),
        secretAccessKey: Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY"),
        bucketName: "smtp-messages",
        config =>
        {
            config.Region = "us-west-2";
            config.KeyPrefix = "emails/";
            
            // Server-side encryption
            config.EnableServerSideEncryption = true;
            config.KmsKeyId = "alias/my-kms-key";
            
            // Versioning
            config.EnableVersioning = true;
            
            // Storage class
            config.StorageClass = S3StorageClass.StandardInfrequentAccess;
            
            // Performance
            config.UseTransferAcceleration = true;
            config.CompressMessageBody = true;
        })
    .Build();`;

const lifecycleExample = `// Lifecycle rules for automatic management
{
  "Rules": [{
    "Id": "ArchiveOldEmails",
    "Status": "Enabled",
    "Transitions": [
      {
        "Days": 30,
        "StorageClass": "STANDARD_IA"
      },
      {
        "Days": 90,
        "StorageClass": "GLACIER"
      },
      {
        "Days": 365,
        "StorageClass": "DEEP_ARCHIVE"
      }
    ],
    "Expiration": {
      "Days": 730
    }
  }]
}

// Apply lifecycle policy
var lifecycleConfig = new LifecycleConfiguration
{
    Rules = new List<LifecycleRule>
    {
        new LifecycleRule
        {
            Id = "EmailArchiving",
            Status = LifecycleRuleStatus.Enabled,
            Transitions = new List<Transition>
            {
                new Transition { Days = 30, StorageClass = S3StorageClass.StandardInfrequentAccess },
                new Transition { Days = 90, StorageClass = S3StorageClass.Glacier }
            }
        }
    }
};`;

const encryptionExample = `// KMS encryption for sensitive data
var putRequest = new PutObjectRequest
{
    BucketName = "smtp-messages",
    Key = "email-123.eml",
    ServerSideEncryptionMethod = ServerSideEncryptionMethod.AWSKMS,
    ServerSideEncryptionKeyManagementServiceKeyId = "arn:aws:kms:us-west-2:111122223333:key/abc123"
};

// Customer-provided encryption keys
var putRequest = new PutObjectRequest
{
    BucketName = "smtp-messages",
    Key = "email-123.eml",
    ServerSideEncryptionCustomerMethod = ServerSideEncryptionCustomerMethod.AES256,
    ServerSideEncryptionCustomerProvidedKey = "base64-encoded-256-bit-key",
    ServerSideEncryptionCustomerProvidedKeyMD5 = "base64-encoded-md5"
};`;

export default function AmazonS3StoragePage() {
  return (
    <div className="min-h-screen py-12 bg-gray-50 dark:bg-gray-950">
      <div className="container mx-auto px-4 max-w-5xl">
        {/* Header */}
        <div className="mb-12">
          <div className="flex items-center gap-2 text-sm text-gray-600 dark:text-gray-400 mb-4">
            <Link href="/docs" className="hover:text-blue-600 dark:hover:text-blue-400">Documentation</Link>
            <span>/</span>
            <Link href="/docs/storage" className="hover:text-blue-600 dark:hover:text-blue-400">Storage</Link>
            <span>/</span>
            <span>Amazon S3</span>
          </div>
          
          <h1 className="text-4xl font-bold text-gray-900 dark:text-white mb-4">Amazon S3 Storage Provider</h1>
          <p className="text-xl text-gray-600 dark:text-gray-400">
            Highly scalable object storage with S3 and S3-compatible services support.
          </p>
        </div>

        {/* Features */}
        <div className="bg-orange-50 dark:bg-orange-900/20 border border-orange-200 dark:border-orange-800 rounded-lg p-6 mb-8">
          <div className="flex items-start gap-3">
            <Cloud className="h-5 w-5 text-orange-600 dark:text-orange-400 mt-0.5" />
            <div>
              <h3 className="font-semibold text-orange-900 dark:text-orange-100 mb-2">Universal S3 Support</h3>
              <div className="flex flex-wrap gap-2">
                <span className="text-xs px-2 py-1 bg-orange-100 dark:bg-orange-800 text-orange-700 dark:text-orange-300 rounded">
                  AWS S3
                </span>
                <span className="text-xs px-2 py-1 bg-orange-100 dark:bg-orange-800 text-orange-700 dark:text-orange-300 rounded">
                  MinIO
                </span>
                <span className="text-xs px-2 py-1 bg-orange-100 dark:bg-orange-800 text-orange-700 dark:text-orange-300 rounded">
                  Wasabi
                </span>
                <span className="text-xs px-2 py-1 bg-orange-100 dark:bg-orange-800 text-orange-700 dark:text-orange-300 rounded">
                  KMS Encryption
                </span>
                <span className="text-xs px-2 py-1 bg-orange-100 dark:bg-orange-800 text-orange-700 dark:text-orange-300 rounded">
                  Lifecycle Rules
                </span>
              </div>
            </div>
          </div>
        </div>

        {/* Installation */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4">Installation</h2>
          <CodeBlock code={installCommand} language="bash" showLineNumbers={false} />
        </section>

        {/* Quick Start */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
            <Zap className="h-6 w-6" />
            Quick Start
          </h2>
          <CodeBlock code={quickStartExample} language="csharp" filename="QuickStart.cs" />
        </section>

        {/* S3 Compatible */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
            <Globe className="h-6 w-6" />
            S3-Compatible Services
          </h2>
          <p className="text-gray-600 dark:text-gray-400 mb-4">
            Works with MinIO, Wasabi, DigitalOcean Spaces, and more:
          </p>
          <CodeBlock code={s3CompatibleExample} language="csharp" filename="S3Compatible.cs" />
          
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mt-6">
            <div className="p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800">
              <h4 className="font-semibold text-gray-900 dark:text-white mb-2">MinIO</h4>
              <p className="text-sm text-gray-600 dark:text-gray-400">Self-hosted S3 alternative</p>
            </div>
            <div className="p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800">
              <h4 className="font-semibold text-gray-900 dark:text-white mb-2">Wasabi</h4>
              <p className="text-sm text-gray-600 dark:text-gray-400">80% cheaper than S3</p>
            </div>
            <div className="p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800">
              <h4 className="font-semibold text-gray-900 dark:text-white mb-2">Backblaze B2</h4>
              <p className="text-sm text-gray-600 dark:text-gray-400">Cost-effective storage</p>
            </div>
          </div>
        </section>

        {/* Advanced Configuration */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
            <Server className="h-6 w-6" />
            Advanced Configuration
          </h2>
          <CodeBlock code={advancedExample} language="csharp" filename="AdvancedConfig.cs" />
        </section>

        {/* Lifecycle Management */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4">Lifecycle Management</h2>
          <p className="text-gray-600 dark:text-gray-400 mb-4">
            Automatically transition objects between storage classes:
          </p>
          <CodeBlock code={lifecycleExample} language="json" filename="Lifecycle.json" />
          
          <div className="mt-6 p-4 bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg">
            <h4 className="font-semibold text-blue-900 dark:text-blue-100 mb-2">Storage Classes</h4>
            <div className="space-y-2 text-sm text-blue-800 dark:text-blue-200">
              <div>• <strong>Standard:</strong> Frequently accessed data</div>
              <div>• <strong>Standard-IA:</strong> Infrequent access (30+ days)</div>
              <div>• <strong>Glacier Instant:</strong> Archive with millisecond retrieval</div>
              <div>• <strong>Glacier Flexible:</strong> 1-12 hour retrieval</div>
              <div>• <strong>Glacier Deep Archive:</strong> 12-48 hour retrieval</div>
            </div>
          </div>
        </section>

        {/* Encryption */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
            <Lock className="h-6 w-6" />
            Encryption Options
          </h2>
          <CodeBlock code={encryptionExample} language="csharp" filename="Encryption.cs" />
        </section>

        {/* Configuration Table */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4">Configuration Options</h2>
          <div className="overflow-x-auto">
            <table className="min-w-full bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800">
              <thead>
                <tr className="border-b border-gray-200 dark:border-gray-800">
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Option</th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Default</th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Description</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-200 dark:divide-gray-800">
                <tr>
                  <td className="px-4 py-3 text-sm font-mono text-gray-900 dark:text-gray-100">BucketName</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">required</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">S3 bucket name</td>
                </tr>
                <tr>
                  <td className="px-4 py-3 text-sm font-mono text-gray-900 dark:text-gray-100">Region</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">"us-east-1"</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">AWS region</td>
                </tr>
                <tr>
                  <td className="px-4 py-3 text-sm font-mono text-gray-900 dark:text-gray-100">KeyPrefix</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">"smtp/"</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">Object key prefix</td>
                </tr>
                <tr>
                  <td className="px-4 py-3 text-sm font-mono text-gray-900 dark:text-gray-100">StorageClass</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">Standard</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">Default storage class</td>
                </tr>
                <tr>
                  <td className="px-4 py-3 text-sm font-mono text-gray-900 dark:text-gray-100">EnableServerSideEncryption</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">true</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">Encryption method</td>
                </tr>
                <tr>
                  <td className="px-4 py-3 text-sm font-mono text-gray-900 dark:text-gray-100">EnableVersioning</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">false</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">Enable object versioning</td>
                </tr>
              </tbody>
            </table>
          </div>
        </section>

        {/* Security Best Practices */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
            <Shield className="h-6 w-6" />
            Security Best Practices
          </h2>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="flex items-start gap-3 p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800">
              <CheckCircle className="h-5 w-5 text-green-500 mt-0.5" />
              <div>
                <h4 className="font-semibold text-gray-900 dark:text-white mb-1">Use IAM Roles</h4>
                <p className="text-sm text-gray-600 dark:text-gray-400">Avoid hardcoding credentials</p>
              </div>
            </div>
            <div className="flex items-start gap-3 p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800">
              <CheckCircle className="h-5 w-5 text-green-500 mt-0.5" />
              <div>
                <h4 className="font-semibold text-gray-900 dark:text-white mb-1">Enable Encryption</h4>
                <p className="text-sm text-gray-600 dark:text-gray-400">Use KMS for sensitive data</p>
              </div>
            </div>
            <div className="flex items-start gap-3 p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800">
              <CheckCircle className="h-5 w-5 text-green-500 mt-0.5" />
              <div>
                <h4 className="font-semibold text-gray-900 dark:text-white mb-1">Bucket Policies</h4>
                <p className="text-sm text-gray-600 dark:text-gray-400">Restrict access properly</p>
              </div>
            </div>
            <div className="flex items-start gap-3 p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800">
              <CheckCircle className="h-5 w-5 text-green-500 mt-0.5" />
              <div>
                <h4 className="font-semibold text-gray-900 dark:text-white mb-1">Enable Versioning</h4>
                <p className="text-sm text-gray-600 dark:text-gray-400">Protect against overwrites</p>
              </div>
            </div>
          </div>
        </section>

        {/* Cost Optimization */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4">Cost Optimization</h2>
          <div className="space-y-3">
            <div className="flex items-start gap-3">
              <span className="text-green-500 mt-1">💡</span>
              <div>
                <p className="font-semibold text-gray-900 dark:text-white">Use appropriate storage classes</p>
                <p className="text-sm text-gray-600 dark:text-gray-400">Standard-IA for infrequent access saves 45%</p>
              </div>
            </div>
            <div className="flex items-start gap-3">
              <span className="text-green-500 mt-1">💡</span>
              <div>
                <p className="font-semibold text-gray-900 dark:text-white">Set lifecycle policies</p>
                <p className="text-sm text-gray-600 dark:text-gray-400">Automatically transition to cheaper tiers</p>
              </div>
            </div>
            <div className="flex items-start gap-3">
              <span className="text-green-500 mt-1">💡</span>
              <div>
                <p className="font-semibold text-gray-900 dark:text-white">Consider S3-compatible alternatives</p>
                <p className="text-sm text-gray-600 dark:text-gray-400">Wasabi or Backblaze B2 for lower costs</p>
              </div>
            </div>
            <div className="flex items-start gap-3">
              <span className="text-green-500 mt-1">💡</span>
              <div>
                <p className="font-semibold text-gray-900 dark:text-white">Enable compression</p>
                <p className="text-sm text-gray-600 dark:text-gray-400">Reduce storage and transfer costs</p>
              </div>
            </div>
          </div>
        </section>

        {/* Next Steps */}
        <div className="mt-12 grid grid-cols-1 md:grid-cols-2 gap-4">
          <Link href="/docs/storage" className="p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 hover:shadow-md transition-all group">
            <div className="flex items-center justify-between">
              <div>
                <h4 className="font-medium text-gray-900 dark:text-white group-hover:text-blue-600 dark:group-hover:text-blue-400">← Storage Overview</h4>
                <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">Back to all providers</p>
              </div>
              <Cloud className="h-5 w-5 text-gray-400" />
            </div>
          </Link>
          <Link href="/docs/message-processing" className="p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 hover:shadow-md transition-all group">
            <div className="flex items-center justify-between">
              <div>
                <h4 className="font-medium text-gray-900 dark:text-white group-hover:text-blue-600 dark:group-hover:text-blue-400">Message Processing →</h4>
                <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">Handle received messages</p>
              </div>
              <Server className="h-5 w-5 text-gray-400" />
            </div>
          </Link>
        </div>
      </div>
    </div>
  );
}
