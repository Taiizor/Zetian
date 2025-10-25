'use client';

import Link from 'next/link';
import { 
  Database,
  Cloud,
  Zap,
  CheckCircle,
  ArrowRight,
  Package,
  Server,
  Shield,
  Settings,
  Gauge
} from 'lucide-react';
import CodeBlock from '@/components/CodeBlock';

const basicStorageExample = `using Zetian.Server;
using Zetian.Storage.SqlServer.Extensions;

// Quick setup with SQL Server storage
var server = new SmtpServerBuilder()
    .Port(25)
    .WithSqlServerStorage(
        "Server=localhost;Database=SmtpDb;Trusted_Connection=true;")
    .Build();

await server.StartAsync();`;

const multiProviderExample = `using Zetian.Server;
using Zetian.Storage.Redis.Extensions;
using Zetian.Storage.SqlServer.Extensions;

// Primary storage with Redis cache
var server = new SmtpServerBuilder()
    .Port(25)
    // Redis for fast cache
    .WithRedisStorage("localhost:6379", config =>
    {
        config.MessageTTLSeconds = 3600; // 1 hour cache
        config.EnablePubSub = true;
    })
    // SQL Server for persistence
    .WithSqlServerStorage(connectionString, config =>
    {
        config.AutoCreateTable = true;
        config.CompressMessageBody = true;
    })
    .Build();`;

const customStorageExample = `using Zetian.Abstractions;
using Zetian.Storage.Configuration;

// Create custom storage provider
public class CustomMessageStore : IMessageStore
{
    public async Task<bool> SaveAsync(
        ISmtpSession session,
        ISmtpMessage message,
        CancellationToken cancellationToken = default)
    {
        // Your custom storage logic
        await SaveToCustomBackend(message);
        return true;
    }
}

// Use custom storage
var server = new SmtpServerBuilder()
    .Port(25)
    .MessageStore(new CustomMessageStore())
    .Build();`;

const storageProviders = [
  {
    category: 'Database Storage',
    icon: Database,
    color: 'from-blue-500 to-blue-600',
    providers: [
      {
        name: 'MongoDB',
        package: 'Zetian.Storage.MongoDB',
        description: 'MongoDB NoSQL with GridFS for attachments',
        href: '/docs/storage/mongodb',
        features: ['GridFS support', 'Sharding', 'TTL indexes', 'Flexible schema']
      },
      {
        name: 'SQL Server',
        package: 'Zetian.Storage.SqlServer',
        description: 'Microsoft SQL Server with ACID compliance',
        href: '/docs/storage/sqlserver',
        features: ['ACID compliance', 'Auto table creation', 'Message compression', 'Full-text search']
      },
      {
        name: 'PostgreSQL',
        package: 'Zetian.Storage.PostgreSQL',
        description: 'PostgreSQL with JSONB and partitioning support',
        href: '/docs/storage/postgresql',
        features: ['JSONB headers', 'Table partitioning', 'GIN indexing', 'Time-based retention']
      }
    ]
  },
  {
    category: 'Cache Storage',
    icon: Zap,
    color: 'from-yellow-500 to-orange-600',
    providers: [
      {
        name: 'Redis',
        package: 'Zetian.Storage.Redis',
        description: 'High-performance in-memory caching',
        href: '/docs/storage/redis',
        features: ['Sub-ms latency', 'Pub/Sub', 'Redis Streams', 'Auto chunking']
      }
    ]
  },
  {
    category: 'Cloud Storage',
    icon: Cloud,
    color: 'from-purple-500 to-pink-600',
    providers: [
      {
        name: 'Azure Blob',
        package: 'Zetian.Storage.AzureBlob',
        description: 'Azure Blob Storage with tier management',
        href: '/docs/storage/azure-blob',
        features: ['AD authentication', 'Access tiers', 'Lifecycle management', 'Soft delete']
      },
      {
        name: 'Amazon S3',
        package: 'Zetian.Storage.AmazonS3',
        description: 'S3 and S3-compatible storage',
        href: '/docs/storage/amazon-s3',
        features: ['S3 compatible', 'KMS encryption', 'Lifecycle rules', 'Versioning']
      }
    ]
  }
];

const features = [
  {
    icon: Shield,
    title: 'Security First',
    description: 'Encryption at rest and in transit, secure authentication'
  },
  {
    icon: Gauge,
    title: 'High Performance',
    description: 'Optimized for SMTP workloads with batching and compression'
  },
  {
    icon: Settings,
    title: 'Flexible Configuration',
    description: 'Fine-tune each provider with extensive configuration options'
  },
  {
    icon: Package,
    title: 'Modular Design',
    description: 'Install only the providers you need, keep deployments lean'
  }
];

export default function StoragePage() {
  return (
    <div className="min-h-screen py-12 bg-gray-50 dark:bg-gray-950">
      <div className="container mx-auto px-4 max-w-5xl">
        {/* Header */}
        <div className="mb-12">
          <div className="flex items-center gap-2 text-sm text-gray-600 dark:text-gray-400 mb-4">
            <Link href="/docs" className="hover:text-blue-600 dark:hover:text-blue-400">
              Documentation
            </Link>
            <span>/</span>
            <span>Storage</span>
          </div>
          
          <h1 className="text-4xl font-bold text-gray-900 dark:text-white mb-4">
            Storage Providers
          </h1>
          <p className="text-xl text-gray-600 dark:text-gray-400">
            Flexible message storage with support for databases, cache, and cloud platforms.
          </p>
        </div>

        {/* Quick Start */}
        <div className="bg-gradient-to-r from-blue-50 to-indigo-50 dark:from-blue-900/20 dark:to-indigo-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-6 mb-8">
          <div className="flex items-start gap-3">
            <Zap className="h-5 w-5 text-blue-600 dark:text-blue-400 mt-0.5" />
            <div className="flex-1">
              <h3 className="font-semibold text-blue-900 dark:text-blue-100 mb-2">Quick Start</h3>
              <p className="text-sm text-blue-800 dark:text-blue-200 mb-4">
                Get started with message storage in seconds. Choose a provider and configure with a single line.
              </p>
              <CodeBlock 
                code={basicStorageExample}
                language="csharp"
                filename="QuickStart.cs"
              />
            </div>
          </div>
        </div>

        {/* Storage Providers Grid */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-6">
            Available Storage Providers
          </h2>
          
          {storageProviders.map((category) => (
            <div key={category.category} className="mb-8">
              <div className="flex items-center gap-2 mb-4">
                <category.icon className="h-5 w-5 text-gray-600 dark:text-gray-400" />
                <h3 className="text-lg font-semibold text-gray-900 dark:text-white">
                  {category.category}
                </h3>
              </div>
              
              <div className="grid gap-4">
                {category.providers.map((provider) => (
                  <Link
                    key={provider.name}
                    href={provider.href}
                    className="block p-6 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 hover:shadow-md transition-all group"
                  >
                    <div className="flex items-start justify-between mb-3">
                      <div>
                        <h4 className="text-lg font-semibold text-gray-900 dark:text-white group-hover:text-blue-600 dark:group-hover:text-blue-400 mb-1">
                          {provider.name}
                        </h4>
                        <p className="text-sm text-gray-600 dark:text-gray-400 mb-2">
                          {provider.description}
                        </p>
                        <code className="text-xs bg-gray-100 dark:bg-gray-800 px-2 py-1 rounded">
                          {provider.package}
                        </code>
                      </div>
                      <ArrowRight className="h-5 w-5 text-gray-400 group-hover:text-blue-600 dark:group-hover:text-blue-400 transition-colors" />
                    </div>
                    
                    <div className="flex flex-wrap gap-2 mt-3">
                      {provider.features.map((feature) => (
                        <span
                          key={feature}
                          className="text-xs px-2 py-1 bg-gray-100 dark:bg-gray-800 text-gray-700 dark:text-gray-300 rounded"
                        >
                          {feature}
                        </span>
                      ))}
                    </div>
                  </Link>
                ))}
              </div>
            </div>
          ))}
        </section>

        {/* Core Features */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-6">
            Core Features
          </h2>
          
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {features.map((feature) => (
              <div key={feature.title} className="flex items-start gap-3 p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800">
                <feature.icon className="h-5 w-5 text-blue-600 dark:text-blue-400 mt-0.5" />
                <div>
                  <h3 className="font-semibold text-gray-900 dark:text-white mb-1">
                    {feature.title}
                  </h3>
                  <p className="text-sm text-gray-600 dark:text-gray-400">
                    {feature.description}
                  </p>
                </div>
              </div>
            ))}
          </div>
        </section>

        {/* Multi-Provider Example */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
            <Server className="h-6 w-6" />
            Multi-Provider Setup
          </h2>
          
          <p className="text-gray-600 dark:text-gray-400 mb-4">
            Combine multiple storage providers for optimal performance and reliability:
          </p>
          
          <CodeBlock 
            code={multiProviderExample}
            language="csharp"
            filename="MultiProvider.cs"
          />
          
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mt-6">
            <div className="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-4">
              <h4 className="font-semibold text-blue-900 dark:text-blue-100 mb-2">Cache Layer</h4>
              <p className="text-sm text-blue-800 dark:text-blue-200">
                Use Redis for ultra-fast recent message access
              </p>
            </div>
            
            <div className="bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg p-4">
              <h4 className="font-semibold text-green-900 dark:text-green-100 mb-2">Persistence Layer</h4>
              <p className="text-sm text-green-800 dark:text-green-200">
                SQL Server or PostgreSQL for long-term storage
              </p>
            </div>
            
            <div className="bg-purple-50 dark:bg-purple-900/20 border border-purple-200 dark:border-purple-800 rounded-lg p-4">
              <h4 className="font-semibold text-purple-900 dark:text-purple-100 mb-2">Archive Layer</h4>
              <p className="text-sm text-purple-800 dark:text-purple-200">
                S3 or Azure Blob for cost-effective archival
              </p>
            </div>
          </div>
        </section>

        {/* Custom Storage Provider */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
            <Package className="h-6 w-6" />
            Custom Storage Provider
          </h2>
          
          <p className="text-gray-600 dark:text-gray-400 mb-4">
            Create your own storage provider by implementing the IMessageStore interface:
          </p>
          
          <CodeBlock 
            code={customStorageExample}
            language="csharp"
            filename="CustomStorage.cs"
          />
        </section>

        {/* Installation */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4">
            Installation
          </h2>
          
          <p className="text-gray-600 dark:text-gray-400 mb-4">
            Install only the storage providers you need:
          </p>
          
          <div className="bg-gray-900 rounded-lg p-4 overflow-x-auto">
            <pre className="text-sm text-gray-100">
{`# Core SMTP Server
dotnet add package Zetian

# Choose your storage provider(s)
dotnet add package Zetian.Storage.Redis
dotnet add package Zetian.Storage.MongoDB
dotnet add package Zetian.Storage.AmazonS3
dotnet add package Zetian.Storage.AzureBlob
dotnet add package Zetian.Storage.SqlServer
dotnet add package Zetian.Storage.PostgreSQL`}
            </pre>
          </div>
        </section>

        {/* Common Configuration */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4">
            Common Configuration Options
          </h2>
          
          <p className="text-gray-600 dark:text-gray-400 mb-4">
            All storage providers share these base configuration options:
          </p>
          
          <div className="overflow-x-auto">
            <table className="min-w-full bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800">
              <thead>
                <tr className="border-b border-gray-200 dark:border-gray-800">
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                    Option
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                    Type
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                    Default
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                    Description
                  </th>
                </tr>
              </thead>
              <tbody>
                <tr className="border-b border-gray-200 dark:border-gray-800">
                  <td className="px-4 py-3 text-sm font-mono text-gray-900 dark:text-gray-100">MaxMessageSizeMB</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">double</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">25.0</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">Maximum message size in megabytes</td>
                </tr>
                <tr className="border-b border-gray-200 dark:border-gray-800">
                  <td className="px-4 py-3 text-sm font-mono text-gray-900 dark:text-gray-100">CompressMessageBody</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">bool</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">false</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">Enable GZIP compression</td>
                </tr>
                <tr className="border-b border-gray-200 dark:border-gray-800">
                  <td className="px-4 py-3 text-sm font-mono text-gray-900 dark:text-gray-100">EnableRetry</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">bool</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">true</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">Retry on transient failures</td>
                </tr>
                <tr className="border-b border-gray-200 dark:border-gray-800">
                  <td className="px-4 py-3 text-sm font-mono text-gray-900 dark:text-gray-100">MaxRetryAttempts</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">int</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">3</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">Maximum retry attempts</td>
                </tr>
                <tr className="border-b border-gray-200 dark:border-gray-800">
                  <td className="px-4 py-3 text-sm font-mono text-gray-900 dark:text-gray-100">RetryDelayMs</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">int</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">1000</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">Delay between retries (ms)</td>
                </tr>
                <tr>
                  <td className="px-4 py-3 text-sm font-mono text-gray-900 dark:text-gray-100">LogErrors</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">bool</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">true</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">Log storage errors</td>
                </tr>
              </tbody>
            </table>
          </div>
        </section>

        {/* Best Practices */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4">
            Best Practices
          </h2>
          
          <div className="space-y-4">
            <div className="flex items-start gap-3">
              <CheckCircle className="h-5 w-5 text-green-500 mt-0.5 flex-shrink-0" />
              <div>
                <h4 className="font-semibold text-gray-900 dark:text-white">Choose the Right Provider</h4>
                <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">
                  Select based on your requirements: Redis for speed, SQL for reliability, Cloud for scale.
                </p>
              </div>
            </div>
            
            <div className="flex items-start gap-3">
              <CheckCircle className="h-5 w-5 text-green-500 mt-0.5 flex-shrink-0" />
              <div>
                <h4 className="font-semibold text-gray-900 dark:text-white">Enable Compression</h4>
                <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">
                  For large messages, enable compression to reduce storage costs and improve performance.
                </p>
              </div>
            </div>
            
            <div className="flex items-start gap-3">
              <CheckCircle className="h-5 w-5 text-green-500 mt-0.5 flex-shrink-0" />
              <div>
                <h4 className="font-semibold text-gray-900 dark:text-white">Implement Retention Policies</h4>
                <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">
                  Configure TTL or lifecycle policies to automatically clean up old messages.
                </p>
              </div>
            </div>
            
            <div className="flex items-start gap-3">
              <CheckCircle className="h-5 w-5 text-green-500 mt-0.5 flex-shrink-0" />
              <div>
                <h4 className="font-semibold text-gray-900 dark:text-white">Monitor Performance</h4>
                <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">
                  Track storage metrics and adjust configuration based on actual usage patterns.
                </p>
              </div>
            </div>
          </div>
        </section>

        {/* Next Steps */}
        <div className="mt-12 grid grid-cols-1 md:grid-cols-2 gap-4">
          <Link 
            href="/docs/storage/sqlserver"
            className="p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 hover:shadow-md transition-all group"
          >
            <div className="flex items-center justify-between">
              <div>
                <h4 className="font-medium text-gray-900 dark:text-white group-hover:text-blue-600 dark:group-hover:text-blue-400">
                  SQL Server Storage →
                </h4>
                <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">
                  Enterprise-grade SQL Server storage
                </p>
              </div>
              <Database className="h-5 w-5 text-gray-400" />
            </div>
          </Link>
          
          <Link 
            href="/docs/storage/redis"
            className="p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 hover:shadow-md transition-all group"
          >
            <div className="flex items-center justify-between">
              <div>
                <h4 className="font-medium text-gray-900 dark:text-white group-hover:text-blue-600 dark:group-hover:text-blue-400">
                  Redis Cache →
                </h4>
                <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">
                  High-performance in-memory storage
                </p>
              </div>
              <Zap className="h-5 w-5 text-gray-400" />
            </div>
          </Link>
        </div>
      </div>
    </div>
  );
}