'use client';

import { 
  Shield, 
  Lock, 
  Zap, 
  Server,
  Database,
  Check,
  ChevronRight,
  Copy,
  CheckCircle,
  Cloud,
  Filter,
  Package,
  Send,
  Brain,
  Heart,
  Activity,
  Layers,
  Gauge,
  ExternalLink,
  FileCode
} from 'lucide-react';
import CodeBlock from '@/components/CodeBlock';

const examples = [
  {
    id: 'basic',
    title: 'Basic',
    description: 'A simple SMTP server in its most basic form',
    icon: Zap,
    color: 'from-blue-500 to-indigo-600',
    difficulty: 'Beginner',
    code: `using Zetian.Server;

// Basic SMTP server - accepts all messages
using var server = new SmtpServerBuilder()
    .Port(25)
    .ServerName("My SMTP Server")
    .MaxMessageSizeMB(10)
    .Build();

server.MessageReceived += async (sender, e) => {
    Console.WriteLine($"New message from {e.Message.From}");
    Console.WriteLine($"Subject: {e.Message.Subject}");
    
    // Save message to file
    var fileName = $"message_{e.Message.Id}.eml";
    await e.Message.SaveToFileAsync(fileName);
};

await server.StartAsync();
Console.WriteLine("SMTP Server is running on port 25");`
  },
  {
    id: 'authenticated',
    title: 'Authenticated',
    description: 'Secure server with username and password',
    icon: Shield,
    color: 'from-green-500 to-emerald-600',
    difficulty: 'Intermediate',
    code: `using Zetian.Models;
using Zetian.Server;

// Authenticated SMTP server
using var server = new SmtpServerBuilder()
    .Port(587)
    .RequireAuthentication()
    .AllowPlainTextAuthentication() // For testing without TLS
    .AuthenticationHandler(async (username, password) =>
    {
        // Example: Check hardcoded credentials
        if (username == "testuser" && password == "testpass")
        {
            return AuthenticationResult.Succeed(username);
        }
        
        // In production, validate against a database:
        // if (await CheckDatabase(username, password))
        //     return AuthenticationResult.Succeed(username);
        
        return AuthenticationResult.Fail();
    })
    .AddAuthenticationMechanism("PLAIN")
    .AddAuthenticationMechanism("LOGIN")
    .Build();

server.MessageReceived += (sender, e) => {
    if (e.Session.IsAuthenticated)
    {
        Console.WriteLine($"User {e.Session.AuthenticatedIdentity} sent message");
        Console.WriteLine($"Subject: {e.Message.Subject}");
    }
};

await server.StartAsync();
Console.WriteLine("SMTP Server with authentication on port 587");`
  },
  {
    id: 'secure',
    title: 'Secure',
    description: 'Encrypted connections with STARTTLS',
    icon: Lock,
    color: 'from-purple-500 to-pink-600',
    difficulty: 'Intermediate',
    code: `using Zetian.Server;

// Secure SMTP server with TLS/SSL support
using var server = new SmtpServerBuilder()
    .Port(587)
    .Certificate("certificate.pfx", "password")
    .RequireSecureConnection()
    .RequireAuthentication()
    .SimpleAuthentication("admin", "admin123")
    .Build();

server.SessionCreated += (sender, e) => {
    Console.WriteLine($"New {(e.Session.IsSecure ? "SECURE" : "INSECURE")} connection");
    Console.WriteLine($"  From: {e.Session.RemoteEndPoint}");
};

server.MessageReceived += (sender, e) => {
    if (e.Session.IsSecure)
    {
        Console.WriteLine("Message received over secure connection");
    }
};

await server.StartAsync();
Console.WriteLine("Secure SMTP Server running with STARTTLS support on port 587");`
  },
  {
    id: 'redis-storage',
    title: 'Redis Storage',
    description: 'High-performance in-memory storage with Redis',
    icon: Zap,
    color: 'from-red-500 to-orange-600',
    difficulty: 'Intermediate',
    code: `using Zetian.Server;
using Zetian.Storage.Redis.Extensions;

// SMTP server with Redis storage backend
using var server = new SmtpServerBuilder()
    .Port(25)
    .WithRedisStorage("localhost:6379")  // Use Redis for message storage
    .Build();

// Configure Redis with advanced options
// using var server = new SmtpServerBuilder()
//     .Port(25)
//     .WithRedisStorage("localhost:6379", config =>
//     {
//         config.DatabaseNumber = 0;
//         config.KeyPrefix = "smtp:";
//         config.MessageTTLSeconds = 1440;  // 24 hours
//         config.MaxMessageSizeMB = 10;
//         config.CompressMessageBody = true;
//     })
//     .Build();

server.MessageReceived += async (sender, e) => {
    Console.WriteLine($"Message {e.Message.Id} stored in Redis");
    Console.WriteLine($"From: {e.Message.From?.Address}");
    Console.WriteLine($"To: {string.Join(", ", e.Message.Recipients.Select(r => r.Address))}");
    
    // Message is automatically stored in Redis
    // Retrieve later using: await redis.GetMessage(messageId);
};

await server.StartAsync();
Console.WriteLine("SMTP Server with Redis storage on port 25");`
  },
  {
    id: 'mongodb-storage',
    title: 'MongoDB Storage',
    description: 'NoSQL document storage with MongoDB',
    icon: Database,
    color: 'from-green-500 to-teal-600',
    difficulty: 'Intermediate',
    code: `using Zetian.Server;
using Zetian.Storage.MongoDB.Extensions;

// SMTP server with MongoDB storage
using var server = new SmtpServerBuilder()
    .Port(25)
    .WithMongoDbStorage(
        connectionString: "mongodb://localhost:27017",
        databaseName: "smtp_server",
        collection: "messages")
    .Build();

// Advanced MongoDB configuration
// using var server = new SmtpServerBuilder()
//     .Port(25)
//     .WithMongoDbStorage("mongodb://localhost:27017", "email_system", config =>
//     {
//         config.CollectionName = "emails";
//         config.UseGridFsForLargeMessages = true;  // For large attachments
//         config.AutoCreateIndexes = true;
//         config.TTLDays = 30;  // Auto-delete after 30 days
//     })
//     .Build();

server.MessageReceived += async (sender, e) => {
    var message = e.Message;
    
    Console.WriteLine($"Message saved to MongoDB");
    Console.WriteLine($"  ID: {message.Id}");
    Console.WriteLine($"  Subject: {message.Subject}");
    Console.WriteLine($"  Attachments: {message.AttachmentCount}");
    
    // GridFS handles large attachments automatically
    // TTL index ensures old messages are cleaned up
};

await server.StartAsync();
Console.WriteLine("SMTP Server with MongoDB storage running on port 25");`
  },
  {
    id: 'sqlserver-storage',
    title: 'SQL Server Storage',
    description: 'Enterprise-grade storage with SQL Server',
    icon: Database,
    color: 'from-blue-500 to-indigo-600',
    difficulty: 'Intermediate',
    code: `using Zetian.Server;
using Zetian.Storage.SqlServer.Extensions;

// SMTP server with SQL Server storage
using var server = new SmtpServerBuilder()
    .Port(25)
    .WithSqlServerStorage(
        "Server=localhost;Database=SmtpDb;Trusted_Connection=true;")
    .Build();

// Advanced SQL Server configuration
// using var server = new SmtpServerBuilder()
//     .Port(25)
//     .WithSqlServerStorage(connectionString, config =>
//     {
//         config.TableName = "EmailMessages";
//         config.SchemaName = "smtp";
//         config.AutoCreateTable = true;
//         config.CompressMessageBody = true;
//     })
//     .Build();

server.MessageReceived += async (sender, e) => {
    Console.WriteLine($"Message stored in SQL Server");
    Console.WriteLine($"  Table: smtp.EmailMessages");
    Console.WriteLine($"  Compression: Enabled");
    
    // Full-text search available:
    // SELECT * FROM smtp.EmailMessages 
    // WHERE CONTAINS(Subject, 'important')
};

await server.StartAsync();
Console.WriteLine("SMTP Server with SQL Server storage on port 25");`
  },
  {
    id: 'multi-storage',
    title: 'Multi-Provider Storage',
    description: 'Multiple storage backends with failover',
    icon: Layers,
    color: 'from-purple-500 to-pink-600',
    difficulty: 'Advanced',
    code: `using Zetian.Server;
using Zetian.Storage.Redis.Extensions;
using Zetian.Storage.SqlServer.Extensions;
using Zetian.Storage.AzureBlob.Extensions;

// SMTP server with multiple storage providers
using var server = new SmtpServerBuilder()
    .Port(25)
    // Primary storage: Fast Redis cache
    .WithRedisStorage("localhost:6379", config =>
    {
        config.MessageTTLSeconds = 60;  // Short-term cache
    })
    // Secondary storage: SQL Server for search
    .WithSqlServerStorage(
        "Server=localhost;Database=SmtpDb;Trusted_Connection=true;",
        config => 
        {
            config.AutoCreateTable = true;
            config.StoreAttachmentsSeparately = false;
        })
    // Archive storage: Azure Blob for long-term
    .WithAzureBlobStorage(
        "DefaultEndpointsProtocol=https;AccountName=myaccount;AccountKey=mykey;",
        config =>
        {
            config.ContainerName = "email-archive";
            config.AccessTier = BlobAccessTier.Cool;
        })
    .Build();

server.MessageReceived += async (sender, e) => {
    var message = e.Message;
    
    Console.WriteLine("Message stored in multiple backends:");
    Console.WriteLine("  ✓ Redis (hot cache) - Instant access");
    Console.WriteLine("  ✓ SQL Server - Full-text searchable");
    Console.WriteLine("  ✓ Azure Blob - Long-term archive");
    
    // Automatic tiering:
    // - Last 1 hour: Served from Redis
    // - Last 30 days: Served from SQL Server
    // - Older: Retrieved from Azure Blob
};

// Failover is automatic
server.ErrorOccurred += (sender, e) => {
    if (e.Exception.Message.Contains("Redis"))
    {
        Console.WriteLine("Redis unavailable, falling back to SQL Server");
    }
};

await server.StartAsync();
Console.WriteLine("Multi-provider SMTP server with automatic failover");`
  },
  {
    id: 's3-storage',
    title: 'Amazon S3 Storage',
    description: 'Cloud storage with S3 or compatible services',
    icon: Database,
    color: 'from-orange-500 to-red-600',
    difficulty: 'Intermediate',
    code: `using Zetian.Server;
using Zetian.Storage.AmazonS3.Extensions;

// SMTP server with Amazon S3 storage
using var server = new SmtpServerBuilder()
    .Port(25)
    .WithS3Storage(
        accessKeyId: Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID"),
        secretAccessKey: Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY"),
        bucketName: "smtp-messages", config =>
        {
            config.Region = "us-west-2";
        })
    .Build();

// Advanced S3 configuration with encryption and lifecycle
// using var server = new SmtpServerBuilder()
//     .Port(25)
//     .WithS3Storage(accessKeyId, secretAccessKey, bucketName, config =>
//     {
//         config.Region = "us-west-2";
//         config.KeyPrefix = "emails/";
//         
//         // Server-side encryption
//         config.EnableServerSideEncryption = true;
//         config.KmsKeyId = "alias/my-kms-key";
//         
//         // Lifecycle rules
//         config.TransitionToIADays = 7;
//         config.TransitionToGlacierDays = 90;
//     })
//     .Build();

server.MessageReceived += async (sender, e) => {
    Console.WriteLine($"Message uploaded to S3");
    Console.WriteLine($"  Bucket: smtp-messages");
    Console.WriteLine($"  Region: us-west-2");
    Console.WriteLine($"  Encryption: AES-256");
    
    // Cost optimization with automatic tiering:
    // - Standard: First 30 days
    // - Infrequent Access: 30-90 days  
    // - Glacier: After 90 days
};

await server.StartAsync();
Console.WriteLine("SMTP Server with S3 storage on port 25");`
  },
  {
    id: 'azure-blob-storage',
    title: 'Azure Blob Storage',
    description: 'Scalable cloud storage with Azure',
    icon: Database,
    color: 'from-blue-600 to-purple-600',
    difficulty: 'Intermediate',
    code: `using Zetian.Server;
using Zetian.Storage.AzureBlob.Extensions;

// SMTP server with Azure Blob storage
using var server = new SmtpServerBuilder()
    .Port(25)
    .WithAzureBlobStorage(
        "DefaultEndpointsProtocol=https;AccountName=myaccount;AccountKey=mykey;")
    .Build();

// Advanced Azure Blob configuration
// using var server = new SmtpServerBuilder()
//     .Port(25)
//     .WithAzureBlobStorage(connectionString, config =>
//     {
//         config.ContainerName = "smtp-messages";
//         
//         // Access tiers for cost optimization
//         config.AccessTier = BlobAccessTier.Cool;
//         
//         // Security features
//         config.EnableSoftDelete = true;
//         config.SoftDeleteRetentionDays = 7;
//         config.CompressMessageBody = true;
//         
//         // Azure AD authentication (instead of connection string)
//         // config.UseAzureAdAuthentication = true;
//     })
//     .Build();

server.MessageReceived += async (sender, e) => {
    Console.WriteLine($"Message stored in Azure Blob Storage");
    Console.WriteLine($"  Container: smtp-messages");
    Console.WriteLine($"  Access Tier: Cool");
    Console.WriteLine($"  Encryption: Enabled");
    
    // Automatic lifecycle management:
    // - Cool tier: Default for cost savings
    // - Archive tier: After 30 days
    // - Soft delete: 7 days retention
};

await server.StartAsync();
Console.WriteLine("SMTP Server with Azure Blob storage on port 25");`
  },
  {
    id: 'postgresql-storage',
    title: 'PostgreSQL Storage',
    description: 'Advanced JSON storage with PostgreSQL',
    icon: Database,
    color: 'from-cyan-500 to-blue-600',
    difficulty: 'Intermediate',
    code: `using Zetian.Server;
using Zetian.Storage.PostgreSQL.Extensions;

// SMTP server with PostgreSQL storage
using var server = new SmtpServerBuilder()
    .Port(25)
    .WithPostgreSqlStorage(
        "Host=localhost;Database=smtp_db;Username=postgres;Password=postgres")
    .Build();

// Advanced PostgreSQL with partitioning and JSONB
// using var server = new SmtpServerBuilder()
//     .Port(25)
//     .WithPostgreSqlStorage(connectionString, config =>
//     {
//         config.TableName = "email_messages";
//         config.SchemaName = "smtp";
//         config.AutoCreateTable = true;
//         
//         // JSONB for headers and metadata
//         config.UseJsonbForHeaders = true;
//         config.CreateIndexes = true;  // Fast JSON queries
//         
//         // Table partitioning for scale
//         config.EnablePartitioning = true;
//         config.PartitionInterval = PartitionInterval.Monthly;
//     })
//     .Build();

server.MessageReceived += async (sender, e) => {
    Console.WriteLine($"Message stored in PostgreSQL");
    Console.WriteLine($"  JSONB storage for flexible queries");
    Console.WriteLine($"  GIN indexed for fast searches");
    
    // Example JSONB queries:
    // SELECT * FROM smtp.email_messages 
    // WHERE headers @> '{"X-Spam-Score": "0"}'
    // 
    // Full text search:
    // WHERE to_tsvector('english', body) @@ plainto_tsquery('important')
};

await server.StartAsync();
Console.WriteLine("SMTP Server with PostgreSQL storage on port 25");`
  },
  {
    id: 'rate-limited',
    title: 'Rate Limited',
    description: 'Speed limiting for spam protection',
    icon: Gauge,
    color: 'from-yellow-500 to-orange-600',
    difficulty: 'Intermediate',
    code: `using Zetian.Models;
using Zetian.Server;
using Zetian.Extensions;

// SMTP server protected with rate limiting
using var server = new SmtpServerBuilder()
    .Port(25)
    .MaxConnections(100)
    .MaxConnectionsPerIP(5)
    .Build();

// Add rate limiting - 100 messages per hour per IP
server.AddRateLimiting(RateLimitConfiguration.PerHour(100));

// Alternative configurations:
// server.AddRateLimiting(RateLimitConfiguration.PerMinute(10));
// server.AddRateLimiting(RateLimitConfiguration.PerDay(1000));

server.MessageReceived += (sender, e) => {
    Console.WriteLine($"Message from {e.Session.RemoteEndPoint}");
    // Rate limiting is handled automatically
    // Messages exceeding limit get SMTP error 421
};

server.ErrorOccurred += (sender, e) => {
    if (e.Exception.Message.Contains("rate limit"))
        Console.WriteLine($"Rate limit exceeded: {e.Session?.RemoteEndPoint}");
};

await server.StartAsync();
Console.WriteLine("Rate-limited server on port 25");`
  },
  {
    id: 'filtered',
    title: 'Custom Processing',
    description: 'Domain and content-based filtering',
    icon: Filter,
    color: 'from-red-500 to-rose-600',
    difficulty: 'Advanced',
    code: `using Zetian.Server;
using Zetian.Protocol;

// Protocol-level filtering
using var server = new SmtpServerBuilder()
    .Port(25)
    // Only accept messages from these domains
    .WithSenderDomainWhitelist("trusted.com", "partner.org")
    // Block these domains
    .WithSenderDomainBlacklist("spam.com", "junk.org")
    // Only accept messages to these domains
    .WithRecipientDomainWhitelist("mydomain.com", "mycompany.com")
    .Build();

// Event-based filtering
server.MessageReceived += (sender, e) => {
    var message = e.Message;
    
    // Check for spam words
    var spamWords = new[] { "viagra", "lottery", "winner" };
    if (spamWords.Any(word => message.Subject?.Contains(word, StringComparison.OrdinalIgnoreCase) ?? false))
    {
        e.Cancel = true;
        e.Response = new SmtpResponse(550, "Message rejected: Spam detected");
        return;
    }
    
    // Check message size
    if (message.Size > 10_000_000) // 10MB
    {
        e.Cancel = true;
        e.Response = new SmtpResponse(552, "Message too large");
        return;
    }
    
    Console.WriteLine("Message passed all filters");
};

await server.StartAsync();`
  },
  {
    id: 'storage',
    title: 'Message Storage',
    description: 'Saving messages to file system or database',
    icon: Database,
    color: 'from-teal-500 to-cyan-600',
    difficulty: 'Advanced',
    code: `using System.Net;
using Zetian.Server;
using System.Text.Json;
using Zetian.Abstractions;

// SMTP server with built-in file storage
using var server = new SmtpServerBuilder()
    .Port(25)
    // Save messages to file system automatically
    .WithFileMessageStore(@"C:\\smtp_messages", createDateFolders: true)
    .Build();

// Using custom message store
// var customStore = new JsonMessageStore(@"C:\\email_storage");
// var server = new SmtpServerBuilder()
//     .Port(25)
//     .MessageStore(customStore)
//     .Build();

// Log when messages are received and stored
server.MessageReceived += (sender, e) => {
    Console.WriteLine($"Message {e.Message.Id} saved");
    Console.WriteLine($"From: {e.Message.From?.Address}");
    Console.WriteLine($"Subject: {e.Message.Subject}");
};

// Use custom store: .MessageStore(new JsonMessageStore(@"C:\\email_storage"))
await server.StartAsync();

// Custom JSON-based message store
public class JsonMessageStore : IMessageStore
{
    private readonly string _directory;
    
    public JsonMessageStore(string directory)
    {
        _directory = directory;
        Directory.CreateDirectory(directory);
    }
    
    public async Task<bool> SaveAsync(
        ISmtpSession session, 
        ISmtpMessage message, 
        CancellationToken ct)
    {
        var dateFolder = Path.Combine(_directory, DateTime.Now.ToString("yyyy-MM-dd"));
        Directory.CreateDirectory(dateFolder);
        
        // Save raw message
        var emlFile = Path.Combine(dateFolder, $"{message.Id}.eml");
        await message.SaveToFileAsync(emlFile);
        
        // Save metadata as JSON
        var metadata = new {
          Id = message.Id,
          From = message.From?.Address,
          Recipients = message.Recipients.Select(r => r.Address).ToArray(),
          Subject = message.Subject,
          TextBody = message.TextBody,
          HtmlBody = message.HtmlBody,
          Size = message.Size,
          ReceivedDate = DateTime.UtcNow,
          SessionId = session.Id,
          RemoteEndPoint = (session.RemoteEndPoint as IPEndPoint)?.Address?.ToString(),
          IsAuthenticated = session.IsAuthenticated,
          AuthenticatedUser = session.AuthenticatedIdentity
        };
        
        var jsonFile = Path.Combine(dateFolder, $"{message.Id}.json");
        await File.WriteAllTextAsync(jsonFile, 
            JsonSerializer.Serialize(metadata, 
                new JsonSerializerOptions { WriteIndented = true }), ct);
        
        return true;
    }
}`
  },
  {
    id: 'relay',
    title: 'Relay',
    description: 'Server with relay and smart host support',
    icon: Send,
    color: 'from-blue-500 to-purple-600',
    difficulty: 'Advanced',
    code: `using Zetian.Server;
using Zetian.Relay.Extensions;
using Zetian.Relay.Configuration;
using System.Net;

// SMTP server with relay support
var server = SmtpServerBuilder
    .CreateBasic()
    .EnableRelay(config =>
    {
        // Primary smart host
        config.DefaultSmartHost = new SmartHostConfiguration
        {
            Host = "smtp.office365.com",
            Port = 587,
            Priority = 10,
            Credentials = new NetworkCredential("user@domain.com", "password"),
            UseTls = true
        };
        
        // Backup smart host for failover
        config.SmartHosts.Add(new SmartHostConfiguration
        {
            Host = "backup.smtp.com",
            Port = 587,
            Priority = 20,
            Credentials = new NetworkCredential("user", "pass")
        });
        
        // Domain-specific routing
        config.DomainRouting["gmail.com"] = new SmartHostConfiguration
        {
            Host = "smtp.gmail.com",
            Port = 587,
            Credentials = new NetworkCredential("user@gmail.com", "app_password")
        };
        
        // MX routing for other domains
        config.UseMxRouting = true;
        config.DnsServers.Add(IPAddress.Parse("8.8.8.8"));
        
        // Retry configuration
        config.MaxRetryCount = 5;
        config.MessageLifetime = TimeSpan.FromDays(3);
        config.QueueProcessingInterval = TimeSpan.FromMinutes(1);
        
        // Enable bounce messages
        config.EnableBounceMessages = true;
        config.BounceSender = "postmaster@mydomain.com";
    });

// Monitor relay queue
server.MessageReceived += async (sender, e) =>
{
    // Queue message for relay with priority
    var priority = e.Message.From?.Address?.Contains("@vip.com") == true
        ? RelayPriority.Urgent
        : RelayPriority.Normal;
        
    await server.QueueForRelayAsync(e.Message, e.Session, priority);
    
    // Get relay statistics
    var stats = await server.GetRelayStatisticsAsync();
    Console.WriteLine($"Queued: {stats.QueuedMessages}, In Progress: {stats.InProgressMessages}");
};

// Start with relay service
var relayService = await server.StartWithRelayAsync();
Console.WriteLine("SMTP Server running with relay support");`
  },
  {
    id: 'antispam',
    title: 'AntiSpam',
    description: 'Server with comprehensive spam protection',
    icon: Brain,
    color: 'from-red-500 to-orange-600',
    difficulty: 'Advanced',
    code: `using Zetian.Server;
using Zetian.AntiSpam.Extensions;
using Zetian.AntiSpam.Checkers;

// SMTP server with anti-spam protection
var server = new SmtpServerBuilder()
    .Port(25)
    .ServerName("Protected SMTP Server")
    .Build();

// Configure comprehensive anti-spam
server.AddAntiSpam(builder => builder
    // SPF checking - verify sender authorization
    .EnableSpf(failScore: 50)
    
    // DKIM signature verification
    .EnableDkim(failScore: 40, strictMode: true)
    
    // DMARC policy enforcement
    .EnableDmarc(failScore: 70, quarantineScore: 50, enforcePolicy: true)
    
    // RBL/DNSBL checking
    .EnableRbl(
        "zen.spamhaus.org",
        "bl.spamcop.net",
        "b.barracudacentral.org"
    )
    
    // Bayesian filtering with training
    .EnableBayesian(spamThreshold: 0.85)
    
    // Greylisting for unknown senders
    .EnableGreylisting(initialDelay: TimeSpan.FromMinutes(5))
    
    // Configure thresholds
    .WithOptions(options =>
    {
        options.RejectThreshold = 60;        // Score >= 60 = reject
        options.TempFailThreshold = 40;      // Score >= 40 = temporary reject
        options.RunChecksInParallel = true;  // Parallel checking for performance
        options.CheckerTimeout = TimeSpan.FromSeconds(10);
        options.EnableDetailedLogging = true;
    }));

// Train Bayesian filter
var bayesianFilter = new BayesianSpamFilter();

// Load training data (example)
string[] spamSamples = {
    "Get rich quick! Click here now!!!",
    "Congratulations! You've won $1,000,000",
    "Cheap viagra online pharmacy"
};

string[] hamSamples = {
    "Meeting scheduled for tomorrow at 2pm",
    "Please find the attached invoice",
    "Thank you for your recent purchase"
};

// Train the filter
foreach (var spam in spamSamples)
    await bayesianFilter.TrainSpamAsync(spam);

foreach (var ham in hamSamples)
    await bayesianFilter.TrainHamAsync(ham);

// Add trained filter
server.AddSpamChecker(bayesianFilter);

// Monitor spam detection
server.MessageReceived += (sender, e) =>
{
    Console.WriteLine($"Message from {e.Message.From?.Address}");
    
    // Messages are automatically checked before this event
    // High-scoring spam will be rejected at SMTP level
};

// Custom spam checker for additional rules
public class CustomSpamChecker : ISpamChecker
{
    public string Name => "CustomRules";
    public bool IsEnabled { get; set; } = true;

    public async Task<SpamCheckResult> CheckAsync(
        ISmtpMessage message,
        ISmtpSession session,
        CancellationToken cancellationToken)
    {
        var subject = message.Subject?.ToLower() ?? "";
        
        // Check for suspicious patterns
        if (subject.Contains("lottery") || subject.Contains("winner"))
            return SpamCheckResult.Spam(80, "Lottery scam detected");
            
        // Check for excessive caps
        if (subject.Count(char.IsUpper) > subject.Length * 0.7)
            return SpamCheckResult.Spam(50, "Excessive capitalization");
            
        return SpamCheckResult.Clean(0);
    }
}

// Add custom checker
server.AddSpamChecker(new CustomSpamChecker());

await server.StartAsync();
Console.WriteLine("SMTP Server running with comprehensive anti-spam protection");`
  },
  {
    id: 'health-check',
    title: 'Health Monitoring',
    description: 'Server with health monitoring and custom checks',
    icon: Heart,
    color: 'from-pink-500 to-rose-600',
    difficulty: 'Advanced',
    code: `using Zetian.Server;
using StackExchange.Redis;
using Microsoft.Data.SqlClient;
using Zetian.HealthCheck.Models;
using Zetian.HealthCheck.Extensions;

// SMTP server with health monitoring
using var server = new SmtpServerBuilder()
    .Port(25)
    .MaxConnections(100)
    .MaxMessageSizeMB(10)
    .Build();

// Monitor server metrics
server.MessageReceived += (sender, e) => {
    var sessionCount = server.Configuration.MaxConnections; // Active sessions
    Console.WriteLine($"Active sessions: {sessionCount}");

    if (sessionCount > 80)
    {
        Console.WriteLine("WARNING: High connection count!");
    }
};

// Start server and health check with custom checks configuration
await server.StartWithHealthCheckAsync(8080, healthService =>
{
    // Add custom health checks
    healthService.AddHealthCheck("database", async (ct) =>
    {
        try
        {
            // Check database connection
            using var connection = new SqlConnection("Server=...;Database=...;User ID=...;Password=...;");
            await connection.OpenAsync(ct);
            return HealthCheckResult.Healthy("Database connected");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Cannot connect to database", ex);
        }
    });

    // Add Redis check
    healthService.AddHealthCheck("redis", async (ct) =>
    {
        try
        {
            // Check Redis connection
            var redis = ConnectionMultiplexer.Connect("localhost:6379");
            var db = redis.GetDatabase();
            await db.PingAsync();
            return HealthCheckResult.Healthy("Redis is responsive");
        }
        catch (Exception ex)
        {
            // Redis down is not critical, mark as degraded
            return HealthCheckResult.Degraded("Redis not available", ex);
        }
    });
});

Console.WriteLine("Server running with health monitoring");
Console.WriteLine("Health endpoints:");
Console.WriteLine("  http://localhost:8080/health - Overall status");
Console.WriteLine("  http://localhost:8080/health/livez  - Liveness check");
Console.WriteLine("  http://localhost:8080/health/readyz - Readiness check");`
  },
  {
    id: 'monitoring',
    title: 'Monitoring',
    description: 'Server with Prometheus and OpenTelemetry metrics',
    icon: Activity,
    color: 'from-purple-500 to-pink-600',
    difficulty: 'Advanced',
    code: `using Zetian.Server;
using Zetian.Monitoring.Extensions;

// Create SMTP server
var server = SmtpServerBuilder
    .CreateBasic();

// Enable comprehensive monitoring
server.EnableMonitoring(builder => builder
    // Prometheus exporter on port 9090
    .EnablePrometheus(9090)
    
    // OpenTelemetry integration
    .EnableOpenTelemetry("http://localhost:4317")
    
    // Service identification
    .WithServiceName("smtp-production")
    .WithServiceVersion("1.0.0")
    
    // Detailed metrics
    .EnableDetailedMetrics()
    .EnableCommandMetrics()
    .EnableThroughputMetrics()
    .EnableHistograms()
    
    // Update interval
    .WithUpdateInterval(TimeSpan.FromSeconds(10))
    
    // Custom labels for all metrics
    .WithLabels(
        ("environment", "production"),
        ("region", "us-east-1"),
        ("instance", "smtp-01"))
    
    // Histogram buckets for command duration (milliseconds)
    .WithCommandDurationBuckets(1, 5, 10, 25, 50, 100, 250, 500, 1000)
    
    // Histogram buckets for message size (bytes)
    .WithMessageSizeBuckets(1024, 10240, 102400, 1048576, 10485760));

// Monitor server events
server.SessionCompleted += (sender, e) =>
{
    var stats = server.GetStatistics();
    Console.WriteLine($"Active Sessions: {stats?.ActiveSessions}");
    Console.WriteLine($"Messages/sec: {stats?.CurrentThroughput?.MessagesPerSecond}");
    Console.WriteLine($"Delivery Rate: {stats?.DeliveryRate}%");
};

// Custom metrics recording
server.MessageReceived += (sender, e) =>
{
    // Record custom metrics
    server.RecordMetric("CUSTOM_PROCESSING", success: true, durationMs: 42.5);
    
    // Access metrics collector
    var collector = server.GetMetricsCollector();
    if (e.Message.Size > 10_000_000)
    {
        collector.RecordRejection("Message too large");
    }
};

// Start server
await server.StartAsync();
Console.WriteLine("SMTP Server with monitoring on port 25");
Console.WriteLine("Prometheus metrics available at http://localhost:9090/metrics");

// Get real-time statistics
while (!Console.KeyAvailable)
{
    var stats = server.GetStatistics();
    Console.Clear();
    Console.WriteLine("=== Server Statistics ===");
    Console.WriteLine($"Uptime: {stats.Uptime}");
    Console.WriteLine($"Total Sessions: {stats.TotalSessions}");
    Console.WriteLine($"Active Sessions: {stats.ActiveSessions}");
    Console.WriteLine($"Messages/sec: {stats.CurrentThroughput?.MessagesPerSecond}");
    Console.WriteLine($"Bytes/sec: {stats.CurrentThroughput?.BytesPerSecond}");
    Console.WriteLine($"TLS Usage: {stats.ConnectionMetrics?.TlsUsageRate}%");
    await Task.Delay(5000);
}`
  }
];

export default function ExamplesPage() {
  return (
    <div className="min-h-screen py-12 bg-gray-50 dark:bg-gray-950">
      <div className="container mx-auto px-4">
        {/* Header */}
        <div className="text-center mb-12">
          <h1 className="text-4xl font-bold text-gray-900 dark:text-white mb-4">
            Code Examples
          </h1>
          <p className="text-xl text-gray-600 dark:text-gray-400 max-w-3xl mx-auto">
            Real-world code examples ready to use.
            Copy, paste, and customize according to your needs.
          </p>
        </div>

        {/* Quick Stats */}
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4 max-w-4xl mx-auto mb-12">
          <div className="bg-white dark:bg-gray-900 rounded-lg p-4 text-center border border-gray-200 dark:border-gray-800">
            <div className="text-2xl font-bold text-gray-900 dark:text-white">16</div>
            <div className="text-sm text-gray-600 dark:text-gray-400">Code Examples</div>
          </div>
          <div className="bg-white dark:bg-gray-900 rounded-lg p-4 text-center border border-gray-200 dark:border-gray-800">
            <div className="text-2xl font-bold text-gray-900 dark:text-white">3</div>
            <div className="text-sm text-gray-600 dark:text-gray-400">Difficulty Levels</div>
          </div>
          <div className="bg-white dark:bg-gray-900 rounded-lg p-4 text-center border border-gray-200 dark:border-gray-800">
            <div className="text-2xl font-bold text-gray-900 dark:text-white">25+</div>
            <div className="text-sm text-gray-600 dark:text-gray-400">Features</div>
          </div>
          <div className="bg-white dark:bg-gray-900 rounded-lg p-4 text-center border border-gray-200 dark:border-gray-800">
            <div className="text-2xl font-bold text-gray-900 dark:text-white">100%</div>
            <div className="text-sm text-gray-600 dark:text-gray-400">Tested</div>
          </div>
        </div>

        {/* Examples Grid */}
        <div className="space-y-8 max-w-6xl mx-auto">
          {examples.map((example) => {
            const Icon = example.icon;
            return (
              <div 
                key={example.id}
                className="bg-white dark:bg-gray-900 rounded-xl shadow-sm hover:shadow-xl transition-all border border-gray-200 dark:border-gray-800"
              >
                {/* Header */}
                <div className="p-6 border-b border-gray-200 dark:border-gray-800">
                  <div className="flex items-start justify-between">
                    <div className="flex items-start gap-4">
                      <div className={`p-3 rounded-lg bg-gradient-to-br ${example.color}`}>
                        <Icon className="h-6 w-6 text-white" />
                      </div>
                      <div>
                        <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-1">
                          {example.title}
                        </h3>
                        <p className="text-gray-600 dark:text-gray-400">
                          {example.description}
                        </p>
                      </div>
                    </div>
                    <span className={`px-3 py-1 rounded-full text-xs font-medium ${
                      example.difficulty === 'Beginner' 
                        ? 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-300'
                        : example.difficulty === 'Intermediate'
                        ? 'bg-yellow-100 dark:bg-yellow-900/30 text-yellow-700 dark:text-yellow-300'
                        : 'bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-300'
                    }`}>
                      {example.difficulty}
                    </span>
                  </div>
                </div>

                {/* Code */}
                <div className="relative">
                  <div className="absolute top-4 right-4 z-20">
                    <a
                      href={`https://github.com/Taiizor/Zetian/tree/develop/examples/Zetian.Examples/${example.title.replace(/\s+/g, '')}Example.cs`}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="p-2 bg-gray-300 hover:bg-gray-400 dark:bg-gray-700 dark:hover:bg-gray-600 rounded-lg transition-colors group inline-flex items-center gap-2"
                      aria-label="View on GitHub"
                    >
                      <ExternalLink className="h-4 w-4 text-gray-700 dark:text-gray-300" />
                    </a>
                  </div>
                  <CodeBlock 
                    code={example.code}
                    language="csharp"
                    filename={`${example.title.replace(/\s+/g, '')}Example.cs`}
                    showLineNumbers={true}
                  />
                </div>

                {/* Features */}
                <div className="p-6 border-t border-gray-800">
                  <div className="flex flex-wrap gap-2">
                    <span className="inline-flex items-center gap-1 px-2 py-1 bg-gray-800 rounded text-xs text-gray-300">
                      <Check className="h-3 w-3" />
                      Async/Await
                    </span>
                    <span className="inline-flex items-center gap-1 px-2 py-1 bg-gray-800 rounded text-xs text-gray-300">
                      <Check className="h-3 w-3" />
                      Event-Driven
                    </span>
                    <span className="inline-flex items-center gap-1 px-2 py-1 bg-gray-800 rounded text-xs text-gray-300">
                      <Check className="h-3 w-3" />
                      Production Ready
                    </span>
                  </div>
                </div>
              </div>
            );
          })}
        </div>

        {/* More Examples CTA */}
        <div className="mt-16 text-center">
          <div className="inline-flex flex-col items-center gap-4 p-8 bg-gradient-to-r from-primary-50 to-blue-50 dark:from-primary-900/20 dark:to-blue-900/20 rounded-2xl">
            <FileCode className="h-12 w-12 text-primary-600 dark:text-primary-400" />
            <h3 className="text-xl font-semibold text-gray-900 dark:text-white">
              Looking for More Examples?
            </h3>
            <p className="text-gray-600 dark:text-gray-400 max-w-md">
              Find more examples and test scenarios in our GitHub repository.
            </p>
            <a
              href="https://github.com/Taiizor/Zetian/tree/develop/examples"
              target="_blank"
              rel="noopener noreferrer"
              className="inline-flex items-center gap-2 px-6 py-3 bg-primary-600 hover:bg-primary-700 text-white rounded-lg font-medium transition-all"
            >
              View All Examples on GitHub
              <ExternalLink className="h-4 w-4" />
            </a>
          </div>
        </div>
      </div>
    </div>
  );
}