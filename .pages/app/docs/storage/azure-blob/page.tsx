'use client';

import Link from 'next/link';
import { Cloud, Shield, CheckCircle, Zap, Server, Lock, Archive } from 'lucide-react';
import CodeBlock from '@/components/CodeBlock';

const installCommand = `dotnet add package Zetian
dotnet add package Zetian.Storage.AzureBlob`;

const quickStartExample = `using Zetian.Server;
using Zetian.Storage.AzureBlob.Extensions;

// Basic Azure Blob Storage setup
var server = new SmtpServerBuilder()
    .Port(25)
    .WithAzureBlobStorage(
        "DefaultEndpointsProtocol=https;AccountName=myaccount;AccountKey=mykey;")
    .Build();

await server.StartAsync();`;

const azureAdExample = `// Azure AD authentication (recommended)
var server = new SmtpServerBuilder()
    .Port(25)
    .WithAzureBlobStorageAD(
        "mystorageaccount", // Storage account name only
        config =>
        {
            config.ContainerName = "smtp-messages";
            config.UseAzureAdAuthentication = true;
        })
    .Build();`;

const advancedExample = `var server = new SmtpServerBuilder()
    .Port(25)
    .WithAzureBlobStorage(
        "DefaultEndpointsProtocol=https;AccountName=myaccount;AccountKey=mykey;",
        config =>
        {
            config.ContainerName = "smtp-messages";
            
            // Access tiers
            config.AccessTier = BlobAccessTier.Cool;
            
            // Soft delete
            config.EnableSoftDelete = true;
            config.SoftDeleteRetentionDays = 7;
            
            // Performance
            config.CompressMessageBody = true;
            config.MaxMessageSizeMB = 100;
        })
    .Build();`;

const accessTiersExample = `// Automatic tier management based on age
// Hot -> Cool -> Archive

// Upload to Hot tier (immediate access)
await blobClient.SetAccessTierAsync(AccessTier.Hot);

// Move to Cool after 7 days (lower cost, slower access)
await blobClient.SetAccessTierAsync(AccessTier.Cool);

// Archive after 30 days (lowest cost, hours to retrieve)
await blobClient.SetAccessTierAsync(AccessTier.Archive);

// Lifecycle policy (automatic)
{
  "rules": [{
    "name": "MoveToArchive",
    "type": "Lifecycle",
    "definition": {
      "actions": {
        "baseBlob": {
          "tierToCool": { "daysAfterModificationGreaterThan": 7 },
          "tierToArchive": { "daysAfterModificationGreaterThan": 30 },
          "delete": { "daysAfterModificationGreaterThan": 365 }
        }
      }
    }
  }]
}`;

const queryExample = `// Query messages using tags and metadata
var blobs = containerClient.GetBlobsAsync(
    traits: BlobTraits.Metadata | BlobTraits.Tags,
    prefix: "2024/01/");

await foreach (var blob in blobs)
{
    Console.WriteLine($"Message: {blob.Name}");
    Console.WriteLine($"Size: {blob.Properties.ContentLength}");
    Console.WriteLine($"From: {blob.Metadata["from"]}");
    Console.WriteLine($"Subject: {blob.Metadata["subject"]}");
}

// Search by tags
string query = @"""From"" = 'user@example.com' AND ""ReceivedDate"" > '2024-01-01'";
var taggedBlobs = containerClient.FindBlobsByTagsAsync(query);`;

export default function AzureBlobStoragePage() {
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
            <span>Azure Blob</span>
          </div>
          
          <h1 className="text-4xl font-bold text-gray-900 dark:text-white mb-4">Azure Blob Storage Provider</h1>
          <p className="text-xl text-gray-600 dark:text-gray-400">
            Scalable cloud object storage with advanced lifecycle management and Azure AD integration.
          </p>
        </div>

        {/* Features */}
        <div className="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-6 mb-8">
          <div className="flex items-start gap-3">
            <Cloud className="h-5 w-5 text-blue-600 dark:text-blue-400 mt-0.5" />
            <div>
              <h3 className="font-semibold text-blue-900 dark:text-blue-100 mb-2">Azure Cloud Storage</h3>
              <div className="flex flex-wrap gap-2">
                <span className="text-xs px-2 py-1 bg-blue-100 dark:bg-blue-800 text-blue-700 dark:text-blue-300 rounded">
                  Azure AD Auth
                </span>
                <span className="text-xs px-2 py-1 bg-blue-100 dark:bg-blue-800 text-blue-700 dark:text-blue-300 rounded">
                  Access Tiers
                </span>
                <span className="text-xs px-2 py-1 bg-blue-100 dark:bg-blue-800 text-blue-700 dark:text-blue-300 rounded">
                  Soft Delete
                </span>
                <span className="text-xs px-2 py-1 bg-blue-100 dark:bg-blue-800 text-blue-700 dark:text-blue-300 rounded">
                  Lifecycle Policies
                </span>
                <span className="text-xs px-2 py-1 bg-blue-100 dark:bg-blue-800 text-blue-700 dark:text-blue-300 rounded">
                  Geo-Redundancy
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

        {/* Azure AD Authentication */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
            <Shield className="h-6 w-6" />
            Azure AD Authentication
          </h2>
          <p className="text-gray-600 dark:text-gray-400 mb-4">
            Use Azure Active Directory for secure, passwordless authentication:
          </p>
          <CodeBlock code={azureAdExample} language="csharp" filename="AzureAD.cs" />
        </section>

        {/* Advanced Configuration */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
            <Server className="h-6 w-6" />
            Advanced Configuration
          </h2>
          <CodeBlock code={advancedExample} language="csharp" filename="AdvancedConfig.cs" />
        </section>

        {/* Access Tiers */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
            <Archive className="h-6 w-6" />
            Access Tiers & Lifecycle
          </h2>
          <p className="text-gray-600 dark:text-gray-400 mb-4">
            Optimize costs with automatic tier management:
          </p>
          <CodeBlock code={accessTiersExample} language="javascript" filename="Lifecycle.json" />
          
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mt-6">
            <div className="p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800">
              <div className="flex items-center justify-between mb-2">
                <h4 className="font-semibold text-gray-900 dark:text-white">Hot Tier</h4>
                <span className="text-xs text-red-500">$$$$</span>
              </div>
              <p className="text-sm text-gray-600 dark:text-gray-400">Frequent access, instant retrieval</p>
            </div>
            <div className="p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800">
              <div className="flex items-center justify-between mb-2">
                <h4 className="font-semibold text-gray-900 dark:text-white">Cool Tier</h4>
                <span className="text-xs text-yellow-500">$$</span>
              </div>
              <p className="text-sm text-gray-600 dark:text-gray-400">Infrequent access, 30+ days</p>
            </div>
            <div className="p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800">
              <div className="flex items-center justify-between mb-2">
                <h4 className="font-semibold text-gray-900 dark:text-white">Archive Tier</h4>
                <span className="text-xs text-green-500">$</span>
              </div>
              <p className="text-sm text-gray-600 dark:text-gray-400">Rare access, hours to retrieve</p>
            </div>
          </div>
        </section>

        {/* Query Examples */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4">Query & Search</h2>
          <CodeBlock code={queryExample} language="csharp" filename="Query.cs" />
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
                  <td className="px-4 py-3 text-sm font-mono text-gray-900 dark:text-gray-100">ContainerName</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">"smtp-messages"</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">Container name</td>
                </tr>
                <tr>
                  <td className="px-4 py-3 text-sm font-mono text-gray-900 dark:text-gray-100">UseAzureAdAuthentication</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">false</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">Use Azure AD auth</td>
                </tr>
                <tr>
                  <td className="px-4 py-3 text-sm font-mono text-gray-900 dark:text-gray-100">EnableSoftDelete</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">false</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">Enable soft delete</td>
                </tr>
              </tbody>
            </table>
          </div>
        </section>

        {/* Security Best Practices */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
            <Lock className="h-6 w-6" />
            Security Best Practices
          </h2>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="flex items-start gap-3 p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800">
              <CheckCircle className="h-5 w-5 text-green-500 mt-0.5" />
              <div>
                <h4 className="font-semibold text-gray-900 dark:text-white mb-1">Use Azure AD</h4>
                <p className="text-sm text-gray-600 dark:text-gray-400">Managed identities over keys</p>
              </div>
            </div>
            <div className="flex items-start gap-3 p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800">
              <CheckCircle className="h-5 w-5 text-green-500 mt-0.5" />
              <div>
                <h4 className="font-semibold text-gray-900 dark:text-white mb-1">Enable Soft Delete</h4>
                <p className="text-sm text-gray-600 dark:text-gray-400">Protect against accidental deletion</p>
              </div>
            </div>
            <div className="flex items-start gap-3 p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800">
              <CheckCircle className="h-5 w-5 text-green-500 mt-0.5" />
              <div>
                <h4 className="font-semibold text-gray-900 dark:text-white mb-1">Private Endpoints</h4>
                <p className="text-sm text-gray-600 dark:text-gray-400">Restrict network access</p>
              </div>
            </div>
            <div className="flex items-start gap-3 p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800">
              <CheckCircle className="h-5 w-5 text-green-500 mt-0.5" />
              <div>
                <h4 className="font-semibold text-gray-900 dark:text-white mb-1">Encryption</h4>
                <p className="text-sm text-gray-600 dark:text-gray-400">Customer-managed keys</p>
              </div>
            </div>
          </div>
        </section>

        {/* Next Steps */}
        <div className="mt-12 grid grid-cols-1 md:grid-cols-2 gap-4">
          <Link href="/docs/storage/amazon-s3" className="p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 hover:shadow-md transition-all group">
            <div className="flex items-center justify-between">
              <div>
                <h4 className="font-medium text-gray-900 dark:text-white group-hover:text-blue-600 dark:group-hover:text-blue-400">Amazon S3 →</h4>
                <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">S3-compatible storage</p>
              </div>
              <Cloud className="h-5 w-5 text-gray-400" />
            </div>
          </Link>
          <Link href="/docs/storage" className="p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 hover:shadow-md transition-all group">
            <div className="flex items-center justify-between">
              <div>
                <h4 className="font-medium text-gray-900 dark:text-white group-hover:text-blue-600 dark:group-hover:text-blue-400">← Storage Overview</h4>
                <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">Back to storage providers</p>
              </div>
              <Cloud className="h-5 w-5 text-gray-400" />
            </div>
          </Link>
        </div>
      </div>
    </div>
  );
}
