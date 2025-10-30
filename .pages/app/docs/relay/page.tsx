'use client';

import { 
  Send, 
  RefreshCw, 
  Shield, 
  Database,
  Settings,
  CheckCircle,
  Network,
  Mail,
  BarChart,
  Zap,
  Globe,
  Lock,
  Users
} from 'lucide-react';
import CodeBlock from '@/components/CodeBlock';
import Link from 'next/link';

export default function RelayPage() {
  return (
    <div className="min-h-screen bg-gradient-to-br from-gray-900 via-slate-800 to-gray-900">
      <div className="relative">
        <div className="absolute inset-0">
          <div className="absolute inset-0 bg-grid-white/[0.02]" />
        </div>
        
        <div className="relative max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
          {/* Header */}
          <div className="text-center mb-12">
            <div className="inline-flex items-center justify-center w-20 h-20 bg-gradient-to-br from-blue-500 to-purple-600 rounded-2xl mb-6">
              <Send className="w-10 h-10 text-white" />
            </div>
            <h1 className="text-5xl font-bold text-white mb-4">Zetian.Relay</h1>
            <p className="text-xl text-gray-300 mb-6">
              Advanced SMTP Relay and Proxy Extension for Zetian
            </p>
            <div className="flex justify-center gap-4">
              <a href="https://www.nuget.org/packages/Zetian.Relay" className="inline-flex items-center px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700">
                <span className="mr-2">📦</span> NuGet Package
              </a>
              <a href="https://github.com/Taiizor/Zetian" className="inline-flex items-center px-4 py-2 bg-gray-700 text-white rounded-lg hover:bg-gray-600">
                <span className="mr-2">⭐</span> Star on GitHub
              </a>
            </div>
          </div>

          {/* Features */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-12">
            {[
              { icon: Network, title: 'Smart Host Support', desc: 'Route through multiple relay servers with failover' },
              { icon: RefreshCw, title: 'Queue Management', desc: 'Persistent queue with retry mechanisms' },
              { icon: BarChart, title: 'Load Balancing', desc: 'Distribute load across multiple servers' },
              { icon: Shield, title: 'Authentication', desc: 'AUTH PLAIN and LOGIN support' },
              { icon: Lock, title: 'TLS/SSL', desc: 'Secure connections with STARTTLS' },
              { icon: Globe, title: 'MX Routing', desc: 'DNS MX record-based routing' },
            ].map((feature, i) => (
              <div key={i} className="bg-gray-800/50 backdrop-blur-sm rounded-xl p-6 border border-gray-700/50">
                <feature.icon className="w-10 h-10 text-blue-400 mb-3" />
                <h3 className="text-lg font-semibold text-white mb-2">{feature.title}</h3>
                <p className="text-gray-400 text-sm">{feature.desc}</p>
              </div>
            ))}
          </div>

          {/* Quick Start */}
          <div className="bg-gray-800/30 backdrop-blur-sm rounded-2xl p-8 mb-12">
            <h2 className="text-3xl font-bold text-white mb-6 flex items-center">
              <Zap className="w-8 h-8 mr-3 text-yellow-400" />
              Quick Start
            </h2>

            <div className="space-y-6">
              <div>
                <h3 className="text-xl font-semibold text-white mb-3">Basic Relay Setup</h3>
                <CodeBlock language="csharp" code={`using Zetian.Server;
using Zetian.Relay.Extensions;

// Create SMTP server with relay enabled
var server = SmtpServerBuilder
    .CreateBasic()
    .EnableRelay();

// Start server with relay service
var relayService = await server.StartWithRelayAsync();`} />
              </div>

              <div>
                <h3 className="text-xl font-semibold text-white mb-3">With Smart Host</h3>
                <CodeBlock language="csharp" code={`using System.Net;
using Zetian.Server;
using Zetian.Relay.Extensions;
using Zetian.Relay.Configuration;

var server = SmtpServerBuilder
    .CreateBasic()
    .EnableRelay(config =>
    {
        config.DefaultSmartHost = new SmartHostConfiguration
        {
            Host = "smtp.example.com",
            Port = 587,
            Credentials = new NetworkCredential("user", "password"),
            UseTls = true
        };
    });

await server.StartAsync();`} />
              </div>
            </div>
          </div>

          {/* Advanced Configuration */}
          <div className="bg-gray-800/30 backdrop-blur-sm rounded-2xl p-8 mb-12">
            <h2 className="text-3xl font-bold text-white mb-6 flex items-center">
              <Settings className="w-8 h-8 mr-3 text-purple-400" />
              Advanced Configuration
            </h2>

            <div className="space-y-6">
              {/* Multiple Smart Hosts with Failover */}
              <div>
                <h3 className="text-xl font-semibold text-white mb-3">Multiple Smart Hosts with Failover</h3>
                <p className="text-gray-300 mb-3">
                  Configure primary and backup smart hosts with automatic failover:
                </p>
                <CodeBlock language="csharp" code={`var server = SmtpServerBuilder
    .CreateBasic()
    .EnableRelay(config =>
{
    // Primary smart host
    config.DefaultSmartHost = new SmartHostConfiguration
    {
        Host = "primary.smtp.com",
        Port = 587,
        Priority = 10, // Lower priority = higher preference
        Credentials = new NetworkCredential("user", "pass")
    };
    
    // Backup smart hosts
    config.SmartHosts.Add(new SmartHostConfiguration
    {
        Host = "backup1.smtp.com",
        Port = 587,
        Priority = 20,
        Credentials = new NetworkCredential("user", "pass")
    });
    
    config.SmartHosts.Add(new SmartHostConfiguration
    {
        Host = "backup2.smtp.com",
        Port = 587,
        Priority = 30
    });
});`} />
              </div>

              {/* Domain-Specific Routing */}
              <div>
                <h3 className="text-xl font-semibold text-white mb-3">Domain-Specific Routing</h3>
                <p className="text-gray-300 mb-3">
                  Route specific domains through different smart hosts:
                </p>
                <CodeBlock language="csharp" code={`var server = SmtpServerBuilder
    .CreateBasic()
    .EnableRelay(config =>
{
    // Route specific domains through different smart hosts
    config.DomainRouting["gmail.com"] = new SmartHostConfiguration
    {
        Host = "smtp.gmail.com",
        Port = 587,
        Credentials = new NetworkCredential("user@gmail.com", "app_password")
    };
    
    config.DomainRouting["outlook.com"] = new SmartHostConfiguration
    {
        Host = "smtp-mail.outlook.com",
        Port = 587,
        Credentials = new NetworkCredential("user@outlook.com", "password")
    };
    
    // Default for all other domains
    config.DefaultSmartHost = new SmartHostConfiguration
    {
        Host = "smtp.sendgrid.net",
        Port = 587,
        Credentials = new NetworkCredential("apikey", "SG.xxxxx")
    };
});`} />
              </div>

              {/* MX-Based Routing */}
              <div>
                <h3 className="text-xl font-semibold text-white mb-3">MX-Based Routing</h3>
                <p className="text-gray-300 mb-3">
                  Use DNS MX records for automatic routing:
                </p>
                <CodeBlock language="csharp" code={`var server = SmtpServerBuilder
    .CreateBasic()
    .EnableRelay(config =>
{
    // Use DNS MX records for routing
    config.UseMxRouting = true;
    
    // Optional: Custom DNS servers
    config.DnsServers.Add(IPAddress.Parse("8.8.8.8"));
    config.DnsServers.Add(IPAddress.Parse("1.1.1.1"));
    
    // Fallback smart host if MX lookup fails
    config.DefaultSmartHost = new SmartHostConfiguration
    {
        Host = "fallback.smtp.com",
        Port = 25
    };
});`} />
              </div>

              {/* Using Relay Builder */}
              <div>
                <h3 className="text-xl font-semibold text-white mb-3">Using Relay Builder</h3>
                <p className="text-gray-300 mb-3">
                  Fluent API for comprehensive relay configuration:
                </p>
                <CodeBlock language="csharp" code={`using Zetian.Relay.Builder;

var relayConfig = new RelayBuilder()
    .WithSmartHost("smtp.office365.com", 587, "user@domain.com", "password")
    .MaxConcurrentDeliveries(20)
    .MaxRetries(5)
    .MessageLifetime(TimeSpan.FromDays(3))
    .ConnectionTimeout(TimeSpan.FromMinutes(10))
    .EnableTls(true, require: true)
    .LocalDomain("mail.mydomain.com")
    .AddLocalDomains("mydomain.com", "internal.local")
    .AddRelayDomains("partner.com", "customer.com")
    .RequireAuthentication(true)
    .EnableBounce(true, "postmaster@mydomain.com")
    .Build();

var server = SmtpServerBuilder
    .CreateBasic()
    .EnableRelay(relayConfig);`} />
              </div>
            </div>
          </div>

          {/* Queue Management */}
          <div className="bg-gray-800/30 backdrop-blur-sm rounded-2xl p-8 mb-12">
            <h2 className="text-3xl font-bold text-white mb-6 flex items-center">
              <Database className="w-8 h-8 mr-3 text-green-400" />
              Queue Management
            </h2>

            <div className="space-y-6">
              <div>
                <h3 className="text-xl font-semibold text-white mb-3">Manual Queue Operations</h3>
                <CodeBlock language="csharp" code={`// Get relay service
var relayService = server.GetRelayService();

// Queue a message manually
var relayMessage = await server.QueueForRelayAsync(
    message,
    session,
    RelayPriority.High);

// Get queue statistics
var stats = await server.GetRelayStatisticsAsync();
Console.WriteLine($"Queued: {stats.QueuedMessages}");
Console.WriteLine($"In Progress: {stats.InProgressMessages}");
Console.WriteLine($"Delivered: {stats.DeliveredMessages}");
Console.WriteLine($"Failed: {stats.FailedMessages}");

// Get all messages in queue
var messages = await relayService.Queue.GetAllAsync();

// Get messages by status
var deferredMessages = await relayService.Queue.GetByStatusAsync(RelayStatus.Deferred);

// Remove a message
await relayService.Queue.RemoveAsync(queueId);

// Clear expired messages
var cleared = await relayService.Queue.ClearExpiredAsync();`} />
              </div>

              <div>
                <h3 className="text-xl font-semibold text-white mb-3">Message Priority</h3>
                <CodeBlock language="csharp" code={`server.MessageReceived += async (sender, e) =>
{
    // Determine priority based on sender or content
    var priority = e.Message.From?.Address?.EndsWith("@vip.com") == true
        ? RelayPriority.Urgent
        : RelayPriority.Normal;
    
    // Queue with priority
    await server.QueueForRelayAsync(e.Message, e.Session, priority);
};`} />
              </div>
            </div>
          </div>

          {/* Retry Configuration */}
          <div className="bg-gray-800/30 backdrop-blur-sm rounded-2xl p-8 mb-12">
            <h2 className="text-3xl font-bold text-white mb-6 flex items-center">
              <RefreshCw className="w-8 h-8 mr-3 text-orange-400" />
              Retry Configuration
            </h2>

            <div className="space-y-6">
              <div>
                <h3 className="text-xl font-semibold text-white mb-3">Retry Settings</h3>
                <CodeBlock language="csharp" code={`var server = SmtpServerBuilder
    .CreateBasic()
    .EnableRelay(config =>
{
    // Maximum retry attempts
    config.MaxRetryCount = 10;
    
    // Message lifetime before expiration
    config.MessageLifetime = TimeSpan.FromDays(4);
    
    // Connection timeout per attempt
    config.ConnectionTimeout = TimeSpan.FromMinutes(5);
    
    // Queue processing interval
    config.QueueProcessingInterval = TimeSpan.FromSeconds(30);
    
    // Cleanup expired messages interval
    config.CleanupInterval = TimeSpan.FromHours(1);
});`} />
              </div>

              <div>
                <h3 className="text-xl font-semibold text-white mb-3">Retry Schedule</h3>
                <p className="text-gray-300 mb-3">The relay service uses exponential backoff for retries:</p>
                <div className="bg-gray-900/50 rounded-lg p-4">
                  <table className="w-full text-left">
                    <thead>
                      <tr className="border-b border-gray-700">
                        <th className="py-2 text-gray-300">Retry</th>
                        <th className="py-2 text-gray-300">Wait Time</th>
                      </tr>
                    </thead>
                    <tbody className="text-gray-400">
                      <tr><td className="py-1">1st</td><td>1 minute</td></tr>
                      <tr><td className="py-1">2nd</td><td>2 minutes</td></tr>
                      <tr><td className="py-1">3rd</td><td>4 minutes</td></tr>
                      <tr><td className="py-1">4th</td><td>8 minutes</td></tr>
                      <tr><td className="py-1">5th</td><td>16 minutes</td></tr>
                      <tr><td className="py-1">6th</td><td>32 minutes</td></tr>
                      <tr><td className="py-1">7th+</td><td>1-4 hours (capped)</td></tr>
                    </tbody>
                  </table>
                </div>
              </div>
            </div>
          </div>

          {/* Load Balancing */}
          <div className="bg-gray-800/30 backdrop-blur-sm rounded-2xl p-8 mb-12">
            <h2 className="text-3xl font-bold text-white mb-6 flex items-center">
              <Users className="w-8 h-8 mr-3 text-indigo-400" />
              Load Balancing
            </h2>

            <CodeBlock language="csharp" code={`var server = SmtpServerBuilder
    .CreateBasic()
    .EnableRelay(config =>
{
    // Configure multiple smart hosts with weights
    config.SmartHosts.AddRange(new[]
    {
        new SmartHostConfiguration
        {
            Host = "smtp1.example.com",
            Port = 25,
            Priority = 10,
            Weight = 50  // 50% of traffic
        },
        new SmartHostConfiguration
        {
            Host = "smtp2.example.com",
            Port = 25,
            Priority = 10,
            Weight = 30  // 30% of traffic
        },
        new SmartHostConfiguration
        {
            Host = "smtp3.example.com",
            Port = 25,
            Priority = 10,
            Weight = 20  // 20% of traffic
        }
    });
});`} />
          </div>

          {/* Message Status */}
          <div className="bg-gray-800/30 backdrop-blur-sm rounded-2xl p-8 mb-12">
            <h2 className="text-3xl font-bold text-white mb-6 flex items-center">
              <Mail className="w-8 h-8 mr-3 text-blue-400" />
              Message Status
            </h2>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {[
                { status: 'Queued', desc: 'Message is waiting for delivery', color: 'text-yellow-400' },
                { status: 'InProgress', desc: 'Message is currently being delivered', color: 'text-blue-400' },
                { status: 'Delivered', desc: 'Message successfully delivered', color: 'text-green-400' },
                { status: 'Failed', desc: 'Permanent delivery failure', color: 'text-red-400' },
                { status: 'Deferred', desc: 'Temporary failure, will retry', color: 'text-orange-400' },
                { status: 'Expired', desc: 'Message exceeded lifetime', color: 'text-gray-400' },
                { status: 'Cancelled', desc: 'Message was cancelled', color: 'text-gray-500' },
                { status: 'PartiallyDelivered', desc: 'Some recipients succeeded', color: 'text-purple-400' },
              ].map((item, i) => (
                <div key={i} className="bg-gray-900/50 rounded-lg p-4">
                  <h3 className={`font-semibold ${item.color}`}>{item.status}</h3>
                  <p className="text-gray-400 text-sm mt-1">{item.desc}</p>
                </div>
              ))}
            </div>
          </div>

          {/* Monitoring */}
          <div className="bg-gray-800/30 backdrop-blur-sm rounded-2xl p-8 mb-12">
            <h2 className="text-3xl font-bold text-white mb-6 flex items-center">
              <BarChart className="w-8 h-8 mr-3 text-cyan-400" />
              Monitoring & Statistics
            </h2>

            <CodeBlock language="csharp" code={`// Get comprehensive statistics
var stats = await relayService.Queue.GetStatisticsAsync();

Console.WriteLine($"Total Messages: {stats.TotalMessages}");
Console.WriteLine($"Queued: {stats.QueuedMessages}");
Console.WriteLine($"In Progress: {stats.InProgressMessages}");
Console.WriteLine($"Deferred: {stats.DeferredMessages}");
Console.WriteLine($"Delivered: {stats.DeliveredMessages}");
Console.WriteLine($"Failed: {stats.FailedMessages}");
Console.WriteLine($"Expired: {stats.ExpiredMessages}");
Console.WriteLine($"Total Size: {stats.TotalSize} bytes");
Console.WriteLine($"Oldest Message: {stats.OldestMessageTime}");
Console.WriteLine($"Average Queue Time: {stats.AverageQueueTime}");
Console.WriteLine($"Average Retry Count: {stats.AverageRetryCount}");

// Messages by priority
foreach (var kvp in stats.MessagesByPriority)
{
    Console.WriteLine($"Priority {kvp.Key}: {kvp.Value} messages");
}

// Messages by smart host
foreach (var kvp in stats.MessagesBySmartHost)
{
    Console.WriteLine($"Host {kvp.Key}: {kvp.Value} messages");
}`} />
          </div>

          {/* Best Practices */}
          <div className="bg-gradient-to-br from-blue-900/20 to-purple-900/20 rounded-2xl p-8 mb-12">
            <h2 className="text-3xl font-bold text-white mb-6">Best Practices</h2>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div className="space-y-4">
                <h3 className="text-xl font-semibold text-blue-300">Performance Tips</h3>
                <ul className="space-y-2 text-gray-300">
                  <li className="flex items-start">
                    <CheckCircle className="w-5 h-5 text-green-400 mr-2 mt-0.5 flex-shrink-0" />
                    <span>Cache DNS MX record lookups for improved performance</span>
                  </li>
                  <li className="flex items-start">
                    <CheckCircle className="w-5 h-5 text-green-400 mr-2 mt-0.5 flex-shrink-0" />
                    <span>Use connection pooling to reuse SMTP client connections</span>
                  </li>
                  <li className="flex items-start">
                    <CheckCircle className="w-5 h-5 text-green-400 mr-2 mt-0.5 flex-shrink-0" />
                    <span>Group messages to same destination for batch delivery</span>
                  </li>
                  <li className="flex items-start">
                    <CheckCircle className="w-5 h-5 text-green-400 mr-2 mt-0.5 flex-shrink-0" />
                    <span>Adjust MaxConcurrentDeliveries based on resources</span>
                  </li>
                </ul>
              </div>
              <div className="space-y-4">
                <h3 className="text-xl font-semibold text-purple-300">Security Considerations</h3>
                <ul className="space-y-2 text-gray-300">
                  <li className="flex items-start">
                    <Shield className="w-5 h-5 text-blue-400 mr-2 mt-0.5 flex-shrink-0" />
                    <span>Always require authentication for relay access</span>
                  </li>
                  <li className="flex items-start">
                    <Shield className="w-5 h-5 text-blue-400 mr-2 mt-0.5 flex-shrink-0" />
                    <span>Use TLS/SSL for all outbound connections</span>
                  </li>
                  <li className="flex items-start">
                    <Shield className="w-5 h-5 text-blue-400 mr-2 mt-0.5 flex-shrink-0" />
                    <span>Limit relay networks to trusted IPs only</span>
                  </li>
                  <li className="flex items-start">
                    <Shield className="w-5 h-5 text-blue-400 mr-2 mt-0.5 flex-shrink-0" />
                    <span>Monitor for relay abuse and implement rate limiting</span>
                  </li>
                </ul>
              </div>
            </div>
          </div>

          {/* Navigation */}
          <div className="flex justify-between items-center">
            <Link href="/docs/extensions" className="text-blue-400 hover:text-blue-300 flex items-center">
              ← Back to Extensions
            </Link>
            <Link href="/docs/anti-spam" className="text-blue-400 hover:text-blue-300 flex items-center">
              AntiSpam Extension →
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
}