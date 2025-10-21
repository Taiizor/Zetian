'use client';

import Link from 'next/link';
import { 
  Code2, 
  Shield, 
  Gauge, 
  Filter, 
  Database,
  Lock,
  Zap,
  FileCode,
  Copy,
  ExternalLink,
  Check
} from 'lucide-react';

const examples = [
  {
    id: 'basic',
    title: 'Basic SMTP Server',
    description: 'A simple SMTP server in its most basic form',
    icon: Zap,
    color: 'from-blue-500 to-indigo-600',
    difficulty: 'Beginner',
    code: `using Zetian;

// Basic SMTP server - accepts all messages
using var server = new SmtpServerBuilder()
    .Port(25)
    .ServerName("My SMTP Server")
    .MaxMessageSizeMB(10)
    .Build();

server.MessageReceived += (sender, e) => {
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
    title: 'Authenticated Server',
    description: 'Secure server with username and password',
    icon: Shield,
    color: 'from-green-500 to-emerald-600',
    difficulty: 'Intermediate',
    code: `using Zetian;

// Authenticated SMTP server
using var server = new SmtpServerBuilder()
    .Port(587)
    .RequireAuthentication()
    .AuthenticationHandler(async (username, password) =>
    {
        // Check user from database
        if (await ValidateUserAsync(username, password))
        {
            return AuthenticationResult.Succeed(username);
        }
        
        return AuthenticationResult.Fail();
    })
    .Build();

server.Authentication += (sender, e) => {
    Console.WriteLine($"User {e.Username} authenticated successfully");
};

server.MessageReceived += (sender, e) => {
    Console.WriteLine($"Authenticated user {e.Session.AuthenticatedUser} sent message");
};

await server.StartAsync();`
  },
  {
    id: 'secure',
    title: 'TLS/SSL Secured Server',
    description: 'Encrypted connections with STARTTLS',
    icon: Lock,
    color: 'from-purple-500 to-pink-600',
    difficulty: 'Intermediate',
    code: `using Zetian;

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
};

server.TlsStarted += (sender, e) => {
    Console.WriteLine("TLS handshake completed");
};

await server.StartAsync();
Console.WriteLine("Secure SMTP Server running with STARTTLS support");`
  },
  {
    id: 'rate-limited',
    title: 'Rate Limiting',
    description: 'Speed limiting for spam protection',
    icon: Gauge,
    color: 'from-yellow-500 to-orange-600',
    difficulty: 'Intermediate',
    code: `using Zetian;
using Zetian.Extensions;

// SMTP server protected with rate limiting
using var server = new SmtpServerBuilder()
    .Port(25)
    .MaxConnections(100)
    .MaxConnectionsPerIP(5)
    .Build();

// Add rate limiting - 100 messages per hour per IP
server.AddRateLimiting(new RateLimitConfiguration
{
    MessagesPerHour = 100,
    MessagesPerMinute = 10,
    EnableIpBasedLimiting = true
});

server.MessageReceived += (sender, e) => {
    var remainingQuota = GetRemainingQuota(e.Session.RemoteEndPoint);
    Console.WriteLine($"Message received. Remaining quota: {remainingQuota}");
};

server.RateLimitExceeded += (sender, e) => {
    Console.WriteLine($"Rate limit exceeded for {e.RemoteEndPoint}");
};

await server.StartAsync();`
  },
  {
    id: 'filtered',
    title: 'Message Filtering',
    description: 'Domain and content-based filtering',
    icon: Filter,
    color: 'from-red-500 to-rose-600',
    difficulty: 'Advanced',
    code: `using Zetian;

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
    code: `using Zetian;
using Zetian.Storage;

// Custom message store implementation
public class MongoMessageStore : IMessageStore
{
    private readonly IMongoCollection<EmailDocument> _collection;
    
    public async Task<bool> SaveAsync(ISmtpSession session, ISmtpMessage message, CancellationToken ct)
    {
        var document = new EmailDocument
        {
            Id = message.Id,
            From = message.From?.Address,
            Recipients = message.Recipients.ToList(),
            Subject = message.Subject,
            TextBody = message.TextBody,
            HtmlBody = message.HtmlBody,
            ReceivedDate = DateTime.UtcNow,
            RemoteIp = session.RemoteEndPoint?.Address.ToString()
        };
        
        await _collection.InsertOneAsync(document, cancellationToken: ct);
        return true;
    }
}

// SMTP server with message store
using var server = new SmtpServerBuilder()
    .Port(25)
    // Save to file system
    .WithFileMessageStore(@"C:\\smtp_messages", createDateFolders: true)
    // OR use custom store
    //.MessageStore(new MongoMessageStore(mongoDatabase))
    .Build();

server.MessageStored += (sender, e) => {
    Console.WriteLine($"Message {e.Message.Id} stored successfully");
};

await server.StartAsync();`
  }
];

export default function ExamplesPage() {
  const copyToClipboard = (text: string) => {
    navigator.clipboard.writeText(text);
  };

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
            <div className="text-2xl font-bold text-gray-900 dark:text-white">6</div>
            <div className="text-sm text-gray-600 dark:text-gray-400">Code Examples</div>
          </div>
          <div className="bg-white dark:bg-gray-900 rounded-lg p-4 text-center border border-gray-200 dark:border-gray-800">
            <div className="text-2xl font-bold text-gray-900 dark:text-white">3</div>
            <div className="text-sm text-gray-600 dark:text-gray-400">Difficulty Levels</div>
          </div>
          <div className="bg-white dark:bg-gray-900 rounded-lg p-4 text-center border border-gray-200 dark:border-gray-800">
            <div className="text-2xl font-bold text-gray-900 dark:text-white">15+</div>
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
                  <div className="absolute top-4 right-4 z-10 flex gap-2">
                    <button
                      onClick={() => copyToClipboard(example.code)}
                      className="p-2 bg-gray-800 hover:bg-gray-700 rounded-lg transition-colors group"
                      aria-label="Copy Code"
                    >
                      <Copy className="h-4 w-4 text-gray-400 group-hover:text-white" />
                    </button>
                    <a
                      href={`https://github.com/Taiizor/Zetian/tree/develop/examples/Zetian.Examples/${example.title.replace(/\s+/g, '')}Example.cs`}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="p-2 bg-gray-800 hover:bg-gray-700 rounded-lg transition-colors group"
                      aria-label="View on GitHub"
                    >
                      <ExternalLink className="h-4 w-4 text-gray-400 group-hover:text-white" />
                    </a>
                  </div>
                  <pre className="p-6 bg-gray-900 dark:bg-gray-950 overflow-x-auto">
                    <code className="text-sm text-gray-100">{example.code}</code>
                  </pre>
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