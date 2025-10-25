'use client';

import Link from 'next/link';
import { Zap, Radio, MessageSquare, Layers, Server, Clock, Database } from 'lucide-react';
import CodeBlock from '@/components/CodeBlock';

const installCommand = `dotnet add package Zetian
dotnet add package Zetian.Storage.Redis`;

const quickStartExample = `using Zetian.Server;
using Zetian.Storage.Redis.Extensions;

// Basic Redis cache setup
var server = new SmtpServerBuilder()
    .Port(25)
    .WithRedisStorage("localhost:6379")
    .Build();

await server.StartAsync();`;

const advancedExample = `var server = new SmtpServerBuilder()
    .Port(25)
    .WithRedisStorage(
        "localhost:6379,password=secret,ssl=false",
        config =>
        {
            config.DatabaseNumber = 1;
            config.KeyPrefix = "smtp:msg:";
            
            // TTL for auto-expiration
            config.MessageTTLSeconds = 3600; // 1 hour
            
            // Chunking for large messages
            config.EnableChunking = true;
            config.ChunkSizeKB = 64;
            
            // Real-time features
            config.UseRedisStreams = true;
            config.StreamKey = "smtp:events";
            config.EnablePubSub = true;
            config.PubSubChannel = "smtp:notifications";
            
            // Indexing
            config.MaintainIndex = true;
            config.IndexKey = "smtp:index";
            
            // Compression
            config.CompressMessageBody = true;
        })
    .Build();`;

const chunkingExample = `// Large messages are automatically split into chunks
// This prevents Redis memory fragmentation

// Message stored as:
// smtp:msg:123456 -> metadata
// smtp:msg:123456:chunk:0 -> first 64KB
// smtp:msg:123456:chunk:1 -> second 64KB
// smtp:msg:123456:chunk:2 -> third 64KB

// Retrieval automatically reassembles chunks
var fullMessage = await store.GetMessageAsync(messageId);`;

const pubsubExample = `// Real-time notifications with Pub/Sub
var subscriber = redis.GetSubscriber();

// Subscribe to message events
await subscriber.SubscribeAsync("smtp:notifications", (channel, value) =>
{
    var notification = JsonSerializer.Deserialize<MessageNotification>(value);
    Console.WriteLine($"New message: {notification.MessageId} from {notification.From}");
});

// Publish notification (done automatically by storage)
await subscriber.PublishAsync("smtp:notifications", JsonSerializer.Serialize(new
{
    Event = "message_received",
    MessageId = "ABC123",
    From = "sender@example.com",
    Timestamp = DateTime.UtcNow
}));`;

const streamsExample = `// Redis Streams for event sourcing
// Automatically creates event stream

XADD smtp:events * event message_received id ABC123 from sender@example.com
XADD smtp:events * event message_stored id ABC123 size 45678
XADD smtp:events * event message_deleted id ABC123

// Read stream events
XREAD STREAMS smtp:events 0

// Consumer groups for processing
XGROUP CREATE smtp:events processors $
XREADGROUP GROUP processors consumer1 STREAMS smtp:events >`;

export default function RedisStoragePage() {
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
            <span>Redis</span>
          </div>
          
          <h1 className="text-4xl font-bold text-gray-900 dark:text-white mb-4">Redis Storage Provider</h1>
          <p className="text-xl text-gray-600 dark:text-gray-400">
            Ultra-fast in-memory caching with real-time notifications and streaming capabilities.
          </p>
        </div>

        {/* Features */}
        <div className="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg p-6 mb-8">
          <div className="flex items-start gap-3">
            <Zap className="h-5 w-5 text-red-600 dark:text-red-400 mt-0.5" />
            <div>
              <h3 className="font-semibold text-red-900 dark:text-red-100 mb-2">Lightning Fast Performance</h3>
              <div className="flex flex-wrap gap-2">
                <span className="text-xs px-2 py-1 bg-red-100 dark:bg-red-800 text-red-700 dark:text-red-300 rounded">
                  Sub-ms Latency
                </span>
                <span className="text-xs px-2 py-1 bg-red-100 dark:bg-red-800 text-red-700 dark:text-red-300 rounded">
                  Auto Chunking
                </span>
                <span className="text-xs px-2 py-1 bg-red-100 dark:bg-red-800 text-red-700 dark:text-red-300 rounded">
                  Pub/Sub Events
                </span>
                <span className="text-xs px-2 py-1 bg-red-100 dark:bg-red-800 text-red-700 dark:text-red-300 rounded">
                  Redis Streams
                </span>
                <span className="text-xs px-2 py-1 bg-red-100 dark:bg-red-800 text-red-700 dark:text-red-300 rounded">
                  Cluster Support
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

        {/* Chunking */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
            <Layers className="h-6 w-6" />
            Automatic Chunking
          </h2>
          <p className="text-gray-600 dark:text-gray-400 mb-4">
            Large messages are automatically split into manageable chunks:
          </p>
          <CodeBlock code={chunkingExample} language="csharp" filename="Chunking.cs" />
          
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mt-6">
            <div className="p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800">
              <Layers className="h-5 w-5 text-blue-500 mb-2" />
              <h4 className="font-semibold text-gray-900 dark:text-white mb-1">Smart Splitting</h4>
              <p className="text-sm text-gray-600 dark:text-gray-400">Configurable chunk size</p>
            </div>
            <div className="p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800">
              <Layers className="h-5 w-5 text-green-500 mb-2" />
              <h4 className="font-semibold text-gray-900 dark:text-white mb-1">Auto Assembly</h4>
              <p className="text-sm text-gray-600 dark:text-gray-400">Transparent to client</p>
            </div>
            <div className="p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800">
              <Layers className="h-5 w-5 text-purple-500 mb-2" />
              <h4 className="font-semibold text-gray-900 dark:text-white mb-1">Memory Efficient</h4>
              <p className="text-sm text-gray-600 dark:text-gray-400">Prevents fragmentation</p>
            </div>
          </div>
        </section>

        {/* Pub/Sub */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
            <Radio className="h-6 w-6" />
            Real-time Pub/Sub
          </h2>
          <p className="text-gray-600 dark:text-gray-400 mb-4">
            Get instant notifications when messages arrive:
          </p>
          <CodeBlock code={pubsubExample} language="csharp" filename="PubSub.cs" />
        </section>

        {/* Redis Streams */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
            <MessageSquare className="h-6 w-6" />
            Redis Streams
          </h2>
          <p className="text-gray-600 dark:text-gray-400 mb-4">
            Event sourcing with Redis Streams:
          </p>
          <CodeBlock code={streamsExample} language="bash" filename="Streams.sh" />
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
                  <td className="px-4 py-3 text-sm font-mono text-gray-900 dark:text-gray-100">DatabaseNumber</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">0</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">Redis database (0-15)</td>
                </tr>
                <tr>
                  <td className="px-4 py-3 text-sm font-mono text-gray-900 dark:text-gray-100">KeyPrefix</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">"smtp:"</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">Key namespace prefix</td>
                </tr>
                <tr>
                  <td className="px-4 py-3 text-sm font-mono text-gray-900 dark:text-gray-100">MessageTTLSeconds</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">3600</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">Message expiration time</td>
                </tr>
                <tr>
                  <td className="px-4 py-3 text-sm font-mono text-gray-900 dark:text-gray-100">EnableChunking</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">true</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">Split large messages</td>
                </tr>
                <tr>
                  <td className="px-4 py-3 text-sm font-mono text-gray-900 dark:text-gray-100">ChunkSizeKB</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">64</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">Chunk size in KB</td>
                </tr>
                <tr>
                  <td className="px-4 py-3 text-sm font-mono text-gray-900 dark:text-gray-100">UseRedisStreams</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">false</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">Enable Redis Streams</td>
                </tr>
                <tr>
                  <td className="px-4 py-3 text-sm font-mono text-gray-900 dark:text-gray-100">EnablePubSub</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">false</td>
                  <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">Enable Pub/Sub events</td>
                </tr>
              </tbody>
            </table>
          </div>
        </section>

        {/* Use Cases */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4">When to Use Redis</h2>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="p-4 bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg">
              <h4 className="font-semibold text-green-900 dark:text-green-100 mb-2">✅ Perfect For</h4>
              <ul className="text-sm text-green-800 dark:text-green-200 space-y-1">
                <li>• Recent message cache</li>
                <li>• Real-time notifications</li>
                <li>• Temporary message queue</li>
                <li>• High-frequency access patterns</li>
                <li>• Session storage</li>
              </ul>
            </div>
            <div className="p-4 bg-orange-50 dark:bg-orange-900/20 border border-orange-200 dark:border-orange-800 rounded-lg">
              <h4 className="font-semibold text-orange-900 dark:text-orange-100 mb-2">⚠️ Consider Alternatives</h4>
              <ul className="text-sm text-orange-800 dark:text-orange-200 space-y-1">
                <li>• Long-term archival</li>
                <li>• Very large attachments</li>
                <li>• Complex queries needed</li>
                <li>• Persistent storage required</li>
                <li>• Limited memory available</li>
              </ul>
            </div>
          </div>
        </section>

        {/* Performance */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
            <Clock className="h-6 w-6" />
            Performance Tips
          </h2>
          <div className="space-y-3">
            <div className="flex items-start gap-3">
              <span className="text-green-500 mt-1">•</span>
              <div>
                <p className="font-semibold text-gray-900 dark:text-white">Use TTL for automatic cleanup</p>
                <p className="text-sm text-gray-600 dark:text-gray-400">Prevents memory bloat</p>
              </div>
            </div>
            <div className="flex items-start gap-3">
              <span className="text-green-500 mt-1">•</span>
              <div>
                <p className="font-semibold text-gray-900 dark:text-white">Enable connection pooling</p>
                <p className="text-sm text-gray-600 dark:text-gray-400">Reuse connections for better performance</p>
              </div>
            </div>
            <div className="flex items-start gap-3">
              <span className="text-green-500 mt-1">•</span>
              <div>
                <p className="font-semibold text-gray-900 dark:text-white">Consider Redis Cluster for scaling</p>
                <p className="text-sm text-gray-600 dark:text-gray-400">Horizontal scaling for high loads</p>
              </div>
            </div>
            <div className="flex items-start gap-3">
              <span className="text-green-500 mt-1">•</span>
              <div>
                <p className="font-semibold text-gray-900 dark:text-white">Use pipelining for batch operations</p>
                <p className="text-sm text-gray-600 dark:text-gray-400">Reduce network round-trips</p>
              </div>
            </div>
          </div>
        </section>

        {/* Next Steps */}
        <div className="mt-12 grid grid-cols-1 md:grid-cols-2 gap-4">
          <Link href="/docs/storage/azure-blob" className="p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 hover:shadow-md transition-all group">
            <div className="flex items-center justify-between">
              <div>
                <h4 className="font-medium text-gray-900 dark:text-white group-hover:text-blue-600 dark:group-hover:text-blue-400">Azure Blob →</h4>
                <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">Cloud object storage</p>
              </div>
              <Database className="h-5 w-5 text-gray-400" />
            </div>
          </Link>
          <Link href="/docs/storage/amazon-s3" className="p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 hover:shadow-md transition-all group">
            <div className="flex items-center justify-between">
              <div>
                <h4 className="font-medium text-gray-900 dark:text-white group-hover:text-blue-600 dark:group-hover:text-blue-400">Amazon S3 →</h4>
                <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">S3-compatible storage</p>
              </div>
              <Database className="h-5 w-5 text-gray-400" />
            </div>
          </Link>
        </div>
      </div>
    </div>
  );
}
