'use client';

import Link from 'next/link';
import { Database, Grid, CheckCircle, Zap, Server, Clock, FileBox } from 'lucide-react';
import CodeBlock from '@/components/CodeBlock';

const installCommand = `dotnet add package Zetian
dotnet add package Zetian.Storage.MongoDB`;

const quickStartExample = `using Zetian.Server;
using Zetian.Storage.MongoDB.Extensions;

// Basic setup with MongoDB
var server = new SmtpServerBuilder()
    .Port(25)
    .WithMongoDbStorage(
        "mongodb://localhost:27017",
        "smtp_database")
    .Build();

await server.StartAsync();`;

const advancedExample = `var server = new SmtpServerBuilder()
    .Port(25)
    .WithMongoDbStorage(
        "mongodb://localhost:27017",
        "smtp_database",
        config =>
        {
            config.CollectionName = "messages";
            config.GridFsBucketName = "attachments";
            
            // GridFS for large attachments
            config.UseGridFsForLargeMessages = true;
            config.GridFsThresholdMB = 50;
            
            // TTL for auto-cleanup
            config.EnableTTL = true;
            config.TTLDays = 30;
            
            // Sharding support
            config.ShardKeyField = "received_date";
            
            // Performance
            config.CompressMessageBody = true;
        })
    .Build();`;

const gridFsExample = `// GridFS automatically handles large attachments
// Files over threshold are stored in chunks

// Retrieve attachment from GridFS
var gridFsBucket = new GridFSBucket(database, new GridFSBucketOptions
{
    BucketName = "attachments"
});

byte[] fileBytes;
using (var downloadStream = await gridFsBucket.OpenDownloadStreamByNameAsync("large-file.pdf"))
using (var memoryStream = new MemoryStream())
{
    await downloadStream.CopyToAsync(memoryStream);
    fileBytes = memoryStream.ToArray();
}

using var uploadStream = await gridFsBucket.OpenUploadStreamAsync("new-file.pdf");
await uploadStream.WriteAsync(fileBytes);
await uploadStream.CloseAsync();`;

const queryExample = `// MongoDB query examples
var collection = database.GetCollection<BsonDocument>("messages");

// Find by sender
var filter = Builders<BsonDocument>.Filter.Eq("from_address", "user@example.com");
var messages = await collection.Find(filter).ToListAsync();

// Find recent messages
var recentFilter = Builders<BsonDocument>.Filter.Gte("received_date", DateTime.UtcNow.AddDays(-7));
var recentMessages = await collection.Find(recentFilter).SortByDescending(x => x["received_date"]).ToListAsync();

// Aggregate statistics
var pipeline = new[]
{
    new BsonDocument("$group", new BsonDocument
    {
        { "_id", "$from_address" },
        { "count", new BsonDocument("$sum", 1) },
        { "total_size", new BsonDocument("$sum", "$message_size") }
    })
};
var stats = await collection.Aggregate<BsonDocument>(pipeline).ToListAsync();`;

const ttlExample = `// TTL index for automatic cleanup
db.messages.createIndex(
    { "created_at": 1 },
    { expireAfterSeconds: 2592000 } // 30 days
);

// Messages older than 30 days are automatically deleted
// No manual cleanup required!`;

export default function MongoDBStoragePage() {
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
            <span>MongoDB</span>
          </div>
          
          <h1 className="text-4xl font-bold text-gray-900 dark:text-white mb-4">MongoDB Storage Provider</h1>
          <p className="text-xl text-gray-600 dark:text-gray-400">
            NoSQL document storage with GridFS support for attachments and flexible schema design.
          </p>
        </div>

        {/* Features */}
        <div className="bg-purple-50 dark:bg-purple-900/20 border border-purple-200 dark:border-purple-800 rounded-lg p-6 mb-8">
          <div className="flex items-start gap-3">
            <Database className="h-5 w-5 text-purple-600 dark:text-purple-400 mt-0.5" />
            <div>
              <h3 className="font-semibold text-purple-900 dark:text-purple-100 mb-2">NoSQL Flexibility</h3>
              <div className="flex flex-wrap gap-2">
                <span className="text-xs px-2 py-1 bg-purple-100 dark:bg-purple-800 text-purple-700 dark:text-purple-300 rounded">
                  GridFS Support
                </span>
                <span className="text-xs px-2 py-1 bg-purple-100 dark:bg-purple-800 text-purple-700 dark:text-purple-300 rounded">
                  TTL Indexes
                </span>
                <span className="text-xs px-2 py-1 bg-purple-100 dark:bg-purple-800 text-purple-700 dark:text-purple-300 rounded">
                  Sharding Ready
                </span>
                <span className="text-xs px-2 py-1 bg-purple-100 dark:bg-purple-800 text-purple-700 dark:text-purple-300 rounded">
                  Flexible Schema
                </span>
                <span className="text-xs px-2 py-1 bg-purple-100 dark:bg-purple-800 text-purple-700 dark:text-purple-300 rounded">
                  Aggregation Pipeline
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

        {/* GridFS */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
            <FileBox className="h-6 w-6" />
            GridFS for Large Attachments
          </h2>
          <p className="text-gray-600 dark:text-gray-400 mb-4">
            Automatically handle large attachments with GridFS:
          </p>
          <CodeBlock code={gridFsExample} language="csharp" filename="GridFS.cs" />
          
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mt-6">
            <div className="p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800">
              <Grid className="h-5 w-5 text-blue-500 mb-2" />
              <h4 className="font-semibold text-gray-900 dark:text-white mb-1">16MB+ Files</h4>
              <p className="text-sm text-gray-600 dark:text-gray-400">Handles files of any size</p>
            </div>
            <div className="p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800">
              <Grid className="h-5 w-5 text-green-500 mb-2" />
              <h4 className="font-semibold text-gray-900 dark:text-white mb-1">Chunked Storage</h4>
              <p className="text-sm text-gray-600 dark:text-gray-400">255KB chunks by default</p>
            </div>
            <div className="p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800">
              <Grid className="h-5 w-5 text-purple-500 mb-2" />
              <h4 className="font-semibold text-gray-900 dark:text-white mb-1">Streaming API</h4>
              <p className="text-sm text-gray-600 dark:text-gray-400">Efficient memory usage</p>
            </div>
          </div>
        </section>

        {/* TTL */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
            <Clock className="h-6 w-6" />
            TTL Auto-Cleanup
          </h2>
          <p className="text-gray-600 dark:text-gray-400 mb-4">
            Automatic message expiration with TTL indexes:
          </p>
          <CodeBlock code={ttlExample} language="javascript" filename="TTL.js" />
        </section>

        {/* Queries */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4">Query Examples</h2>
          <CodeBlock code={queryExample} language="csharp" filename="Queries.cs" />
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
                  <td className="px-4 py-3 text-sm font-mono text-gray-900 dark:text-gray-100">CollectionName</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">"smtp_messages"</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">Collection name</td>
                </tr>
                <tr>
                  <td className="px-4 py-3 text-sm font-mono text-gray-900 dark:text-gray-100">UseGridFsForLargeMessages</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">true</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">Use GridFS for large data</td>
                </tr>
                <tr>
                  <td className="px-4 py-3 text-sm font-mono text-gray-900 dark:text-gray-100">GridFsThresholdMB</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">10</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">GridFS threshold size</td>
                </tr>
                <tr>
                  <td className="px-4 py-3 text-sm font-mono text-gray-900 dark:text-gray-100">EnableTTL</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">false</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">Enable TTL auto-cleanup</td>
                </tr>
                <tr>
                  <td className="px-4 py-3 text-sm font-mono text-gray-900 dark:text-gray-100">TTLDays</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">30</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">Days before expiration</td>
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
                <h4 className="font-semibold text-gray-900 dark:text-white mb-1">Index Key Fields</h4>
                <p className="text-sm text-gray-600 dark:text-gray-400">Create indexes for query fields</p>
              </div>
            </div>
            <div className="flex items-start gap-3 p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800">
              <CheckCircle className="h-5 w-5 text-green-500 mt-0.5" />
              <div>
                <h4 className="font-semibold text-gray-900 dark:text-white mb-1">Use TTL Indexes</h4>
                <p className="text-sm text-gray-600 dark:text-gray-400">Automatic cleanup saves space</p>
              </div>
            </div>
            <div className="flex items-start gap-3 p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800">
              <CheckCircle className="h-5 w-5 text-green-500 mt-0.5" />
              <div>
                <h4 className="font-semibold text-gray-900 dark:text-white mb-1">Enable Sharding</h4>
                <p className="text-sm text-gray-600 dark:text-gray-400">Scale horizontally as needed</p>
              </div>
            </div>
            <div className="flex items-start gap-3 p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800">
              <CheckCircle className="h-5 w-5 text-green-500 mt-0.5" />
              <div>
                <h4 className="font-semibold text-gray-900 dark:text-white mb-1">Replica Sets</h4>
                <p className="text-sm text-gray-600 dark:text-gray-400">High availability setup</p>
              </div>
            </div>
          </div>
        </section>

        {/* Next Steps */}
        <div className="mt-12 grid grid-cols-1 md:grid-cols-2 gap-4">
          <Link href="/docs/storage/redis" className="p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 hover:shadow-md transition-all group">
            <div className="flex items-center justify-between">
              <div>
                <h4 className="font-medium text-gray-900 dark:text-white group-hover:text-blue-600 dark:group-hover:text-blue-400">Redis Cache →</h4>
                <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">High-performance caching</p>
              </div>
              <Zap className="h-5 w-5 text-gray-400" />
            </div>
          </Link>
          <Link href="/docs/storage/azure-blob" className="p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 hover:shadow-md transition-all group">
            <div className="flex items-center justify-between">
              <div>
                <h4 className="font-medium text-gray-900 dark:text-white group-hover:text-blue-600 dark:group-hover:text-blue-400">Azure Blob →</h4>
                <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">Cloud object storage</p>
              </div>
              <Database className="h-5 w-5 text-gray-400" />
            </div>
          </Link>
        </div>
      </div>
    </div>
  );
}
