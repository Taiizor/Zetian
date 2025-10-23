'use client';

import Link from 'next/link';
import { 
  Package, 
  Puzzle, 
  Code2, 
  Filter,
  Database,
  Shield,
  Settings,
  AlertCircle,
  CheckCircle,
  Layers,
  Github,
  Zap
} from 'lucide-react';
import CodeBlock from '@/components/CodeBlock';

const customFilterExample = `using System.Net;
using Zetian.Server;
using Zetian.Abstractions;

// Custom mailbox filter
public class CustomSpamFilter : IMailboxFilter
{
    private readonly ISpamDatabase _spamDb;

    public CustomSpamFilter(ISpamDatabase spamDb)
    {
        _spamDb = spamDb;
    }

    public async Task<bool> CanAcceptFromAsync(
        ISmtpSession session,
        string from,
        long size,
        CancellationToken cancellationToken)
    {
        // Spam database check
        if (await _spamDb.IsSpammerAsync(from))
        {
            return false;
        }

        // IP reputation check
        var ipScore = await GetIpReputationAsync(session.RemoteEndPoint);
        if (ipScore < 0.5)
        {
            return false;
        }

        // Size check
        if (size > 50_000_000) // 50MB
        {
            return false;
        }

        return true;
    }

    public async Task<bool> CanDeliverToAsync(
        ISmtpSession session,
        string to,
        string from,
        CancellationToken cancellationToken)
    {
        // Check if user exists
        if (!await UserExistsAsync(to))
        {
            return false;
        }

        // User's blacklist
        var userBlacklist = await GetUserBlacklistAsync(to);
        if (userBlacklist.Contains(from))
        {
            return false;
        }

        // Quota check
        if (await IsQuotaExceededAsync(to))
        {
            return false;
        }

        return true;
    }

    // IP reputation check helper
    private async Task<double> GetIpReputationAsync(EndPoint remoteEndPoint)
    {
        // Simulate IP reputation check
        var ipAddress = (remoteEndPoint as IPEndPoint)?.Address?.ToString();
        
        // In real implementation, check against IP reputation services
        // For demo, allow localhost and private IPs
        if (ipAddress == "127.0.0.1" || ipAddress?.StartsWith("192.168.") == true)
        {
            return 1.0; // Perfect reputation
        }

        // Simulate async operation
        await Task.Delay(10);
        
        // Return random reputation score for demo
        return 0.7; // Default good reputation
    }

    // User existence check helper
    private async Task<bool> UserExistsAsync(string email)
    {
        // Simulate user database check
        await Task.Delay(10);
        
        // For demo, accept common email patterns
        var validUsers = new[] { "admin", "user", "test", "info", "support" };
        var localPart = email.Split('@')[0].ToLower();
        
        return validUsers.Contains(localPart);
    }

    // User blacklist check helper
    private async Task<HashSet<string>> GetUserBlacklistAsync(string userEmail)
    {
        // Simulate fetching user's personal blacklist
        await Task.Delay(10);
        
        // For demo, return a sample blacklist
        return new HashSet<string>
        {
            "spammer@spam.com",
            "blocked@example.com",
            "noreply@marketing.com"
        };
    }

    // Quota check helper
    private async Task<bool> IsQuotaExceededAsync(string userEmail)
    {
        // Simulate quota check
        await Task.Delay(10);
        
        // For demo, quota is never exceeded
        return false;
    }
}

// Spam database interface
public interface ISpamDatabase
{
    Task<bool> IsSpammerAsync(string email);
    Task AddSpammerAsync(string email);
    Task RemoveSpammerAsync(string email);
}

// Simple in-memory spam database implementation
public class SimpleSpamDatabase : ISpamDatabase
{
    private readonly HashSet<string> _spammerEmails;
    private readonly HashSet<string> _spammerDomains;

    public SimpleSpamDatabase()
    {
        // Initialize with known spam emails and domains
        _spammerEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "spammer@spam.com",
            "badactor@malicious.com",
            "phisher@fake-bank.com"
        };

        _spammerDomains = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "spam.com",
            "malicious.com",
            "spammy-domain.net",
            "10minutemail.com",
            "tempmail.com"
        };
    }

    public Task<bool> IsSpammerAsync(string email)
    {
        if (string.IsNullOrEmpty(email))
            return Task.FromResult(true); // No email = spam

        // Check if exact email is in spam list
        if (_spammerEmails.Contains(email))
            return Task.FromResult(true);

        // Check if domain is in spam list
        var atIndex = email.IndexOf('@');
        if (atIndex > 0 && atIndex < email.Length - 1)
        {
            var domain = email.Substring(atIndex + 1);
            if (_spammerDomains.Contains(domain))
                return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    public Task AddSpammerAsync(string email)
    {
        _spammerEmails.Add(email);
        
        // Also add domain to spam list
        var atIndex = email.IndexOf('@');
        if (atIndex > 0 && atIndex < email.Length - 1)
        {
            var domain = email.Substring(atIndex + 1);
            _spammerDomains.Add(domain);
        }

        return Task.CompletedTask;
    }

    public Task RemoveSpammerAsync(string email)
    {
        _spammerEmails.Remove(email);
        return Task.CompletedTask;
    }
}

// Create spam database instance
var spamDatabase = new SimpleSpamDatabase();

// Create custom spam filter
var spamFilter = new CustomSpamFilter(spamDatabase);

// Build server with custom filter
using Zetian.Server;

var server = new SmtpServerBuilder()
    .Port(25)
    .MailboxFilter(spamFilter)
    .Build();

// Start server
await server.StartAsync();`;

const customStoreExample = `using System.Net;
using Zetian.Server;
using Azure.Storage.Blobs;
using Zetian.Abstractions;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;

// Azure Blob Storage message store
public class AzureBlobMessageStore : IMessageStore
{
    private readonly BlobContainerClient _containerClient;
    
    public AzureBlobMessageStore(string connectionString, string containerName)
    {
        var blobServiceClient = new BlobServiceClient(connectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        _containerClient.CreateIfNotExists();
    }
    
    public async Task<bool> SaveAsync(
        ISmtpSession session, 
        ISmtpMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            // Blob name: year/month/day/messageId.eml
            var blobName = $"{DateTime.UtcNow:yyyy/MM/dd}/{message.Id}.eml";
            var blobClient = _containerClient.GetBlobClient(blobName);
            
            // Metadata
            var metadata = new Dictionary<string, string>
            {
                ["From"] = message.From?.Address ?? "unknown",
                ["To"] = string.Join(";", message.Recipients),
                ["Subject"] = message.Subject ?? "",
                ["RemoteIp"] = (session.RemoteEndPoint as IPEndPoint)?.Address.ToString() ?? "",
                ["ReceivedAt"] = DateTime.UtcNow.ToString("O")
            };
            
            // Upload raw message to blob
            using var stream = new MemoryStream(message.GetRawMessage());
            await blobClient.UploadAsync(
                stream, 
                new BlobUploadOptions 
                { 
                    Metadata = metadata 
                },
                cancellationToken);
            
            // Add to Azure Search for indexing
            await IndexMessageAsync(message, blobName);
            
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to save message to Azure Blob Storage");
            return false;
        }
    }
    
    private async Task IndexMessageAsync(ISmtpMessage message, string blobPath)
    {
        // Azure Cognitive Search integration
        var document = new
        {
            Id = message.Id,
            From = message.From?.Address,
            Recipients = message.Recipients,
            Subject = message.Subject,
            TextBody = message.TextBody,
            Date = DateTime.UtcNow,
            BlobPath = blobPath
        };
        
        await _searchClient.IndexDocumentAsync(document);
    }
}

// Usage
var azureStore = new AzureBlobMessageStore(
    "DefaultEndpointsProtocol=https;AccountName=...", 
    "smtp-messages");
    
var server = new SmtpServerBuilder()
    .Port(25)
    .MessageStore(azureStore)
    .Build();`;

const compositeFilterExample = `using Zetian.Enums;
using Zetian.Server;
using Zetian.Storage;

// Combining multiple filters
var compositeFilter = new CompositeMailboxFilter(CompositeMode.All) // All must pass
    .AddFilter(new DomainMailboxFilter()
        .AllowFromDomains("trusted.com", "partner.org")
        .BlockFromDomains("spam.com"))
    .AddFilter(new RateLimitFilter(100, TimeSpan.FromHours(1)))
    .AddFilter(new GeoLocationFilter(allowedCountries: new[] { "TR", "US", "GB" }))
    .AddFilter(new CustomSpamFilter(spamDatabase));

var server = new SmtpServerBuilder()
    .Port(25)
    .MailboxFilter(compositeFilter)
    .Build();

// Combining with OR logic
var orFilter = new CompositeMailboxFilter(CompositeMode.Any) // At least one must pass
    .AddFilter(new WhitelistFilter(trustedSenders))
    .AddFilter(new AuthenticatedUserFilter()) // Authenticated users always pass
    .AddFilter(new InternalNetworkFilter("192.168.0.0/16"));

// Nested composite filters
var mainFilter = new CompositeMailboxFilter(CompositeMode.All)
    .AddFilter(orFilter) // Whitelist OR authenticated OR internal
    .AddFilter(new AntiVirusFilter()) // AND must pass virus check
    .AddFilter(new SizeFilter(25_000_000)); // AND must be smaller than 25MB`;

const extensionMethodsExample = `using Nest;
using System.Net;
using Zetian.Server;
using Zetian.Abstractions;
using System.Net.Http.Json;
using Metrics = Prometheus.Metrics;

// Extending server with extension methods
public static class SmtpServerExtensions
{
    // Add webhook support
    public static ISmtpServer AddWebhook(
        this ISmtpServer server, 
        string webhookUrl)
    {
        server.MessageReceived += async (sender, e) =>
        {
            var payload = new
            {
                messageId = e.Message.Id,
                from = e.Message.From?.Address,
                to = e.Message.Recipients,
                subject = e.Message.Subject,
                size = e.Message.Size,
                timestamp = DateTime.UtcNow
            };
            
            using var client = new HttpClient();
            await client.PostAsJsonAsync(webhookUrl, payload);
        };
        
        return server;
    }
    
    // Add Prometheus metrics
    public static ISmtpServer AddPrometheusMetrics(
        this ISmtpServer server)
    {
        var messagesReceived = Metrics.CreateCounter(
            "smtp_messages_received_total", 
            "Total number of messages received");
            
        var messageSize = Metrics.CreateHistogram(
            "smtp_message_size_bytes",
            "Message size in bytes");
            
        var sessionDuration = Metrics.CreateHistogram(
            "smtp_session_duration_seconds",
            "Session duration in seconds");
        
        server.MessageReceived += (sender, e) =>
        {
            messagesReceived.Inc();
            messageSize.Observe(e.Message.Size);
        };
        
        server.SessionCompleted += (sender, e) =>
        {
            var duration = DateTime.UtcNow - e.Session.StartTime;
            sessionDuration.Observe(duration.TotalSeconds);
        };
        
        return server;
    }
    
    // Add Elasticsearch logging
    public static ISmtpServer AddElasticsearchLogging(
        this ISmtpServer server, 
        string elasticUrl)
    {
        var elasticClient = new ElasticClient(new Uri(elasticUrl));
        
        server.MessageReceived += async (sender, e) =>
        {
            var logEntry = new
            {
                Timestamp = DateTime.UtcNow,
                MessageId = e.Message.Id,
                From = e.Message.From?.Address,
                Recipients = e.Message.Recipients,
                Subject = e.Message.Subject,
                Size = e.Message.Size,
                RemoteIp = (e.Session.RemoteEndPoint as IPEndPoint)?.Address.ToString() ?? ""
            };
            
            await elasticClient.IndexDocumentAsync(logEntry);
        };
        
        return server;
    }
}

// Usage
var server = new SmtpServerBuilder()
    .Port(25)
    .Build()
    .AddWebhook("https://api.example.com/smtp-webhook")
    .AddPrometheusMetrics()
    .AddElasticsearchLogging("http://localhost:9200");

// Start server
await server.StartAsync();`;

const pluginSystemExample = `using Zetian.Server;
using Zetian.Protocol;
using Zetian.Abstractions;

// Spam checker interface
public interface ISpamChecker
{
    Task<double> CheckAsync(ISmtpMessage message);
}

// Simple spam checker implementation
public class SimpleSpamChecker : ISpamChecker
{
    private readonly string[] _spamKeywords = new[] 
    { 
        "viagra", "casino", "lottery", "winner", "prize", 
        "free money", "click here", "limited offer", "act now" 
    };

    public Task<double> CheckAsync(ISmtpMessage message)
    {
        double score = 0.0;

        // Check subject
        if (!string.IsNullOrEmpty(message.Subject))
        {
            var subjectLower = message.Subject.ToLowerInvariant();
            foreach (var keyword in _spamKeywords)
            {
                if (subjectLower.Contains(keyword))
                {
                    score += 0.2; // Each keyword adds to spam score
                }
            }
        }

        // Check for missing headers
        if (string.IsNullOrEmpty(message.Subject))
            score += 0.3;

        if (message.From == null || string.IsNullOrEmpty(message.From.Address))
            score += 0.4;

        // Normalize score between 0 and 1
        score = Math.Min(1.0, score);
        return Task.FromResult(score);
    }
}

// Plugin system
public interface ISmtpPlugin
{
    string Name { get; }
    string Version { get; }
    Task InitializeAsync(ISmtpServer server);
}

// Anti-spam plugin
public class AntiSpamPlugin : ISmtpPlugin
{
    public string Name => "Anti-Spam Plugin";
    public string Version => "1.0.0";
    
    private readonly ISpamChecker _spamChecker;
    
    public AntiSpamPlugin(ISpamChecker spamChecker)
    {
        _spamChecker = spamChecker;
    }
    
    public async Task InitializeAsync(ISmtpServer server)
    {
        server.MessageReceived += async (sender, e) =>
        {
            var spamScore = await _spamChecker.CheckAsync(e.Message);
            
            if (spamScore > 0.8)
            {
                e.Cancel = true;
                e.Response = new SmtpResponse(550, $"Spam detected (score: {spamScore:F2})");
            }
            else if (spamScore > 0.5)
            {
                // Redirect to spam folder
                e.Message.Headers["X-Spam-Score"] = spamScore.ToString("F2");
                e.Message.Headers["X-Spam-Flag"] = "YES";
            }
        };
    }
}

// Plugin manager
public class PluginManager
{
    private readonly List<ISmtpPlugin> _plugins = new();
    
    public void RegisterPlugin(ISmtpPlugin plugin)
    {
        _plugins.Add(plugin);
        Console.WriteLine($"Plugin registered: {plugin.Name} v{plugin.Version}");
    }
    
    public async Task InitializePluginsAsync(ISmtpServer server)
    {
        foreach (var plugin in _plugins)
        {
            await plugin.InitializeAsync(server);
            Console.WriteLine($"Plugin initialized: {plugin.Name}");
        }
    }
}

// Usage
var spamChecker = new SimpleSpamChecker();
var pluginManager = new PluginManager();
pluginManager.RegisterPlugin(new AntiSpamPlugin(spamChecker));
// You can add more plugins:
// pluginManager.RegisterPlugin(new AntiVirusPlugin(virusScanner));
// pluginManager.RegisterPlugin(new GreylistingPlugin(database));
// pluginManager.RegisterPlugin(new DkimValidationPlugin());

var server = new SmtpServerBuilder()
    .Port(25)
    .Build();

// Initialize plugins
await pluginManager.InitializePluginsAsync(server);

// Start server
await server.StartAsync();`;

export default function ExtensionsPage() {
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
            <span>Extensions</span>
          </div>
          
          <h1 className="text-4xl font-bold text-gray-900 dark:text-white mb-4">
            Extensions and Plugin Development
          </h1>
          <p className="text-xl text-gray-600 dark:text-gray-400">
            Develop custom filters, storage providers and plugins.
          </p>
        </div>

        {/* Extension Overview */}
        <div className="bg-purple-50 dark:bg-purple-900/20 border border-purple-200 dark:border-purple-800 rounded-lg p-6 mb-8">
          <div className="flex items-start gap-3">
            <Puzzle className="h-5 w-5 text-purple-600 dark:text-purple-400 mt-0.5" />
            <div>
              <h3 className="font-semibold text-purple-900 dark:text-purple-100 mb-2">Extensible Architecture</h3>
              <p className="text-sm text-purple-800 dark:text-purple-200">
                Zetian is easily extensible thanks to its interface-based architecture.
                You can develop your own filters, storage providers and plugins.
              </p>
            </div>
          </div>
        </div>

        {/* Custom Filters */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
            <Filter className="h-6 w-6" />
            Custom Filters
          </h2>
          
          <p className="text-gray-600 dark:text-gray-400 mb-4">
            Create custom filters by implementing the IMailboxFilter interface:
          </p>
          
          <CodeBlock 
            code={customFilterExample}
            language="csharp"
            filename="CustomSpamFilter.cs"
          />

          <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mt-6">
            <div className="flex items-start gap-2">
              <CheckCircle className="h-4 w-4 text-green-500 mt-0.5" />
              <div>
                <p className="text-sm font-medium text-gray-900 dark:text-white">Spam Control</p>
                <p className="text-xs text-gray-600 dark:text-gray-400">
                  Database and AI-based spam detection
                </p>
              </div>
            </div>
            <div className="flex items-start gap-2">
              <CheckCircle className="h-4 w-4 text-green-500 mt-0.5" />
              <div>
                <p className="text-sm font-medium text-gray-900 dark:text-white">IP Reputation</p>
                <p className="text-xs text-gray-600 dark:text-gray-400">
                  IP address reputation check
                </p>
              </div>
            </div>
            <div className="flex items-start gap-2">
              <CheckCircle className="h-4 w-4 text-green-500 mt-0.5" />
              <div>
                <p className="text-sm font-medium text-gray-900 dark:text-white">Quota Management</p>
                <p className="text-xs text-gray-600 dark:text-gray-400">
                  Per-user quota control
                </p>
              </div>
            </div>
          </div>
        </section>

        {/* Custom Storage */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
            <Database className="h-6 w-6" />
            Custom Storage Providers
          </h2>
          
          <p className="text-gray-600 dark:text-gray-400 mb-4">
            Create custom storage solutions by implementing the IMessageStore interface:
          </p>
          
          <CodeBlock 
            code={customStoreExample}
            language="csharp"
            filename="CustomMessageStore.cs"
          />

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mt-6">
            <div className="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-4">
              <h4 className="font-semibold text-blue-900 dark:text-blue-100 mb-2">Cloud Storage</h4>
              <ul className="space-y-1 text-sm text-blue-800 dark:text-blue-200">
                <li>• Azure Blob Storage</li>
                <li>• AWS S3</li>
                <li>• Google Cloud Storage</li>
                <li>• MinIO</li>
              </ul>
            </div>
            
            <div className="bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg p-4">
              <h4 className="font-semibold text-green-900 dark:text-green-100 mb-2">Database Storage</h4>
              <ul className="space-y-1 text-sm text-green-800 dark:text-green-200">
                <li>• SQL Server / PostgreSQL</li>
                <li>• MongoDB / CosmosDB</li>
                <li>• Elasticsearch</li>
                <li>• Redis</li>
              </ul>
            </div>
          </div>
        </section>

        {/* Composite Filters */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
            <Layers className="h-6 w-6" />
            Composite Filters
          </h2>
          
          <p className="text-gray-600 dark:text-gray-400 mb-4">
            Combine multiple filters with AND/OR logic:
          </p>
          
          <CodeBlock 
            code={compositeFilterExample}
            language="csharp"
            filename="CompositeFilter.cs"
          />
        </section>

        {/* Extension Methods */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
            <Code2 className="h-6 w-6" />
            Extension Methods
          </h2>
          
          <p className="text-gray-600 dark:text-gray-400 mb-4">
            Extension methods that add new features to SmtpServer:
          </p>
          
          <CodeBlock 
            code={extensionMethodsExample}
            language="csharp"
            filename="ExtensionMethods.cs"
          />

          <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mt-6">
            <div className="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 p-4">
              <Zap className="h-5 w-5 text-yellow-500 mb-2" />
              <h4 className="font-semibold text-gray-900 dark:text-white mb-1">Webhook</h4>
              <p className="text-sm text-gray-600 dark:text-gray-400">
                HTTP webhook integration
              </p>
            </div>
            
            <div className="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 p-4">
              <Settings className="h-5 w-5 text-blue-500 mb-2" />
              <h4 className="font-semibold text-gray-900 dark:text-white mb-1">Metrics</h4>
              <p className="text-sm text-gray-600 dark:text-gray-400">
                Prometheus metrics
              </p>
            </div>
            
            <div className="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 p-4">
              <Database className="h-5 w-5 text-green-500 mb-2" />
              <h4 className="font-semibold text-gray-900 dark:text-white mb-1">Logging</h4>
              <p className="text-sm text-gray-600 dark:text-gray-400">
                Elasticsearch logging
              </p>
            </div>
          </div>
        </section>

        {/* Plugin System */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
            <Package className="h-6 w-6" />
            Plugin System
          </h2>
          
          <p className="text-gray-600 dark:text-gray-400 mb-4">
            Create a modular plugin system:
          </p>
          
          <CodeBlock 
            code={pluginSystemExample}
            language="csharp"
            filename="PluginSystem.cs"
          />

          <div className="bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg p-6 mt-6">
            <div className="flex items-start gap-3">
              <Shield className="h-5 w-5 text-green-600 dark:text-green-400 mt-0.5" />
              <div>
                <h4 className="font-semibold text-green-900 dark:text-green-100 mb-2">Plugin Examples</h4>
                <ul className="space-y-1 text-sm text-green-800 dark:text-green-200">
                  <li>• <strong>Anti-Spam:</strong> SpamAssassin integration</li>
                  <li>• <strong>Anti-Virus:</strong> ClamAV scanning</li>
                  <li>• <strong>Greylisting:</strong> Temporary rejection</li>
                  <li>• <strong>DKIM/SPF:</strong> Email validation</li>
                  <li>• <strong>Rate Limiting:</strong> Dynamic rate limiting</li>
                </ul>
              </div>
            </div>
          </div>
        </section>

        {/* Best Practices */}
        <div className="mt-12 bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg p-6">
          <div className="flex items-start gap-3">
            <AlertCircle className="h-5 w-5 text-yellow-600 dark:text-yellow-400 mt-0.5" />
            <div>
              <h4 className="font-semibold text-yellow-900 dark:text-yellow-100 mb-2">Best Practices</h4>
              <ul className="space-y-1 text-sm text-yellow-800 dark:text-yellow-200">
                <li>• Write extensions with async/await</li>
                <li>• Properly handle and log errors</li>
                <li>• Use caching for performance</li>
                <li>• Write thread-safe code</li>
                <li>• Use dependency injection</li>
                <li>• Write unit tests</li>
              </ul>
            </div>
          </div>
        </div>

        {/* Next Steps */}
        <div className="mt-12 grid grid-cols-1 md:grid-cols-2 gap-4">
          <Link 
            href="/examples"
            className="p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 hover:shadow-md transition-all group"
          >
            <div className="flex items-center justify-between">
              <div>
                <h4 className="font-medium text-gray-900 dark:text-white group-hover:text-blue-600 dark:group-hover:text-blue-400">
                  Code Examples →
                </h4>
                <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">
                  Browse ready-to-use code examples
                </p>
              </div>
              <Code2 className="h-5 w-5 text-gray-400" />
            </div>
          </Link>
          
          <a 
            href="https://github.com/Taiizor/Zetian"
            target="_blank"
            rel="noopener noreferrer"
            className="p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 hover:shadow-md transition-all group"
          >
            <div className="flex items-center justify-between">
              <div>
                <h4 className="font-medium text-gray-900 dark:text-white group-hover:text-blue-600 dark:group-hover:text-blue-400">
                  GitHub →
                </h4>
                <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">
                  Source code and examples
                </p>
              </div>
              <Github className="h-5 w-5 text-gray-400" />
            </div>
          </a>
        </div>
      </div>
    </div>
  );
}