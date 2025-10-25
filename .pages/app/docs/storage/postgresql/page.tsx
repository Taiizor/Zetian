'use client';

import Link from 'next/link';
import { Database, Layers, CheckCircle, Zap, Server, Calendar, Search } from 'lucide-react';
import CodeBlock from '@/components/CodeBlock';

const installCommand = `dotnet add package Zetian
dotnet add package Zetian.Storage.PostgreSQL`;

const quickStartExample = `using Zetian.Server;
using Zetian.Storage.PostgreSQL.Extensions;

// Basic setup with auto table creation
var server = new SmtpServerBuilder()
    .Port(25)
    .WithPostgreSqlStorage(
        "Host=localhost;Database=smtp;Username=postgres;Password=secret;")
    .Build();

await server.StartAsync();`;

const advancedExample = `var server = new SmtpServerBuilder()
    .Port(25)
    .WithPostgreSqlStorage(
        "Host=localhost;Database=smtp;Username=postgres;Password=secret;",
        config =>
        {
            config.TableName = "smtp_messages";
            config.SchemaName = "mail";
            config.AutoCreateTable = true;
            
            // JSONB for flexible header storage
            config.UseJsonbForHeaders = true;
            
            // Enable partitioning
            config.EnablePartitioning = true;
            config.PartitionInterval = PartitionInterval.Monthly;
            
            // Performance
            config.CreateIndexes = true;
            config.CompressMessageBody = true;
            config.MaxMessageSizeMB = 100;
        })
    .Build();`;

const partitioningExample = `-- Monthly partitioning example
CREATE TABLE smtp_messages (
    id BIGSERIAL,
    message_id VARCHAR(255) NOT NULL,
    received_date TIMESTAMPTZ NOT NULL,
    headers JSONB,
    message_body BYTEA,
    PRIMARY KEY (id, received_date)
) PARTITION BY RANGE (received_date);

-- Create partitions
CREATE TABLE smtp_messages_2024_01 
PARTITION OF smtp_messages
FOR VALUES FROM ('2024-01-01') TO ('2024-02-01');

CREATE TABLE smtp_messages_2024_02 
PARTITION OF smtp_messages
FOR VALUES FROM ('2024-02-01') TO ('2024-03-01');`;

const jsonbExample = `-- Query JSONB headers
SELECT message_id, headers->>'From' as sender, 
       headers->>'Subject' as subject
FROM smtp_messages
WHERE headers @> '{"From": "user@example.com"}';

-- Search with GIN index
SELECT * FROM smtp_messages
WHERE headers @> '{"X-Spam-Score": "0"}';

-- Extract specific header
SELECT headers->'Received' as received_chain
FROM smtp_messages
WHERE message_id = 'ABC123';`;

export default function PostgreSQLStoragePage() {
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
            <span>PostgreSQL</span>
          </div>
          
          <h1 className="text-4xl font-bold text-gray-900 dark:text-white mb-4">PostgreSQL Storage Provider</h1>
          <p className="text-xl text-gray-600 dark:text-gray-400">
            Advanced PostgreSQL storage with JSONB, partitioning, and GIN indexing for scalable message storage.
          </p>
        </div>

        {/* Features */}
        <div className="bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg p-6 mb-8">
          <div className="flex items-start gap-3">
            <Database className="h-5 w-5 text-green-600 dark:text-green-400 mt-0.5" />
            <div>
              <h3 className="font-semibold text-green-900 dark:text-green-100 mb-2">Advanced Features</h3>
              <div className="flex flex-wrap gap-2">
                <span className="text-xs px-2 py-1 bg-green-100 dark:bg-green-800 text-green-700 dark:text-green-300 rounded">
                  JSONB Headers
                </span>
                <span className="text-xs px-2 py-1 bg-green-100 dark:bg-green-800 text-green-700 dark:text-green-300 rounded">
                  Table Partitioning
                </span>
                <span className="text-xs px-2 py-1 bg-green-100 dark:bg-green-800 text-green-700 dark:text-green-300 rounded">
                  Time-based Retention
                </span>
                <span className="text-xs px-2 py-1 bg-green-100 dark:bg-green-800 text-green-700 dark:text-green-300 rounded">
                  Full-Text Search
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

        {/* Advanced Configuration */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
            <Server className="h-6 w-6" />
            Advanced Configuration
          </h2>
          <CodeBlock code={advancedExample} language="csharp" filename="AdvancedConfig.cs" />
        </section>

        {/* Partitioning */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
            <Layers className="h-6 w-6" />
            Table Partitioning
          </h2>
          <p className="text-gray-600 dark:text-gray-400 mb-4">
            Automatically partition tables by time intervals for better performance:
          </p>
          <CodeBlock code={partitioningExample} language="sql" filename="Partitioning.sql" />
          
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mt-6">
            <div className="p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800">
              <Calendar className="h-5 w-5 text-blue-500 mb-2" />
              <h4 className="font-semibold text-gray-900 dark:text-white mb-1">Daily</h4>
              <p className="text-sm text-gray-600 dark:text-gray-400">High-volume environments</p>
            </div>
            <div className="p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800">
              <Calendar className="h-5 w-5 text-green-500 mb-2" />
              <h4 className="font-semibold text-gray-900 dark:text-white mb-1">Monthly</h4>
              <p className="text-sm text-gray-600 dark:text-gray-400">Balanced performance</p>
            </div>
            <div className="p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800">
              <Calendar className="h-5 w-5 text-purple-500 mb-2" />
              <h4 className="font-semibold text-gray-900 dark:text-white mb-1">Yearly</h4>
              <p className="text-sm text-gray-600 dark:text-gray-400">Long-term archival</p>
            </div>
          </div>
        </section>

        {/* JSONB Features */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
            <Search className="h-6 w-6" />
            JSONB Header Storage
          </h2>
          <p className="text-gray-600 dark:text-gray-400 mb-4">
            Flexible header storage with powerful query capabilities:
          </p>
          <CodeBlock code={jsonbExample} language="sql" filename="JsonbQueries.sql" />
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
                  <td className="px-4 py-3 text-sm font-mono text-gray-900 dark:text-gray-100">UseJsonbForHeaders</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">false</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">Store headers as JSONB</td>
                </tr>
                <tr>
                  <td className="px-4 py-3 text-sm font-mono text-gray-900 dark:text-gray-100">EnablePartitioning</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">false</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">Enable table partitioning</td>
                </tr>
                <tr>
                  <td className="px-4 py-3 text-sm font-mono text-gray-900 dark:text-gray-100">PartitionInterval</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">Monthly</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">Partition time interval</td>
                </tr>
                <tr>
                  <td className="px-4 py-3 text-sm font-mono text-gray-900 dark:text-gray-100">CreateIndexes</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">true</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">Auto-create indexes</td>
                </tr>
              </tbody>
            </table>
          </div>
        </section>

        {/* Best Practices */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4">Best Practices</h2>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="flex items-start gap-3 p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800">
              <CheckCircle className="h-5 w-5 text-green-500 mt-0.5" />
              <div>
                <h4 className="font-semibold text-gray-900 dark:text-white mb-1">Use JSONB for Headers</h4>
                <p className="text-sm text-gray-600 dark:text-gray-400">Flexible schema with fast queries</p>
              </div>
            </div>
            <div className="flex items-start gap-3 p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800">
              <CheckCircle className="h-5 w-5 text-green-500 mt-0.5" />
              <div>
                <h4 className="font-semibold text-gray-900 dark:text-white mb-1">Enable Partitioning</h4>
                <p className="text-sm text-gray-600 dark:text-gray-400">Better performance at scale</p>
              </div>
            </div>
            <div className="flex items-start gap-3 p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800">
              <CheckCircle className="h-5 w-5 text-green-500 mt-0.5" />
              <div>
                <h4 className="font-semibold text-gray-900 dark:text-white mb-1">Regular VACUUM</h4>
                <p className="text-sm text-gray-600 dark:text-gray-400">Maintain performance over time</p>
              </div>
            </div>
            <div className="flex items-start gap-3 p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800">
              <CheckCircle className="h-5 w-5 text-green-500 mt-0.5" />
              <div>
                <h4 className="font-semibold text-gray-900 dark:text-white mb-1">Connection Pooling</h4>
                <p className="text-sm text-gray-600 dark:text-gray-400">Use PgBouncer for high loads</p>
              </div>
            </div>
          </div>
        </section>

        {/* Next Steps */}
        <div className="mt-12 grid grid-cols-1 md:grid-cols-2 gap-4">
          <Link href="/docs/storage/mongodb" className="p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 hover:shadow-md transition-all group">
            <div className="flex items-center justify-between">
              <div>
                <h4 className="font-medium text-gray-900 dark:text-white group-hover:text-blue-600 dark:group-hover:text-blue-400">MongoDB Storage →</h4>
                <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">NoSQL with GridFS</p>
              </div>
              <Database className="h-5 w-5 text-gray-400" />
            </div>
          </Link>
          <Link href="/docs/storage/redis" className="p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 hover:shadow-md transition-all group">
            <div className="flex items-center justify-between">
              <div>
                <h4 className="font-medium text-gray-900 dark:text-white group-hover:text-blue-600 dark:group-hover:text-blue-400">Redis Cache →</h4>
                <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">High-performance caching</p>
              </div>
              <Zap className="h-5 w-5 text-gray-400" />
            </div>
          </Link>
        </div>
      </div>
    </div>
  );
}
