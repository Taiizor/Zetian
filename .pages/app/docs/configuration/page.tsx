'use client';

import Link from 'next/link';
import { 
  Settings, 
  Server, 
  Shield, 
  Filter,
  Gauge,
  Copy,
  AlertCircle,
  CheckCircle,
  FileCode,
  Lock
} from 'lucide-react';
import CodeBlock from '@/components/CodeBlock';

const configExample = `using Zetian;

var server = new SmtpServerBuilder()
    // Basic Settings
    .Port(587)                           // Port number
    .ServerName("My SMTP Server")        // Server name
    .MaxMessageSizeMB(25)                // Max message size (MB)
    .MaxRecipients(100)                  // Max number of recipients
    .MaxConnections(50)                  // Max number of connections
    .MaxConnectionsPerIP(10)             // Max connections per IP
    
    // Security
    .RequireAuthentication()             // Authentication required
    .RequireSecureConnection()           // TLS/SSL required
    .Certificate("cert.pfx", "password") // SSL certificate
    
    // SMTP Features
    .EnableSmtpUtf8()                    // UTF-8 support
    .EnablePipelining()                  // Pipeline support
    
    // Timeout Settings
    .ConnectionTimeout(TimeSpan.FromMinutes(5))
    .CommandTimeout(TimeSpan.FromSeconds(30))
    
    .Build();`;

const authConfigExample = `// Simple authentication
.SimpleAuthentication("admin", "password123")

// Custom authentication
.AuthenticationHandler(async (username, password) =>
{
    // Database check
    var user = await GetUserAsync(username);
    if (user != null && VerifyPassword(password, user.PasswordHash))
    {
        return AuthenticationResult.Succeed(username);
    }
    return AuthenticationResult.Fail("Invalid credentials");
})

// Multiple authentication mechanisms
.AddAuthenticationMechanism("PLAIN")
.AddAuthenticationMechanism("LOGIN")`;

const filterConfigExample = `// Protocol-Level Filtering (at SMTP command level)
var server = new SmtpServerBuilder()
    .Port(25)
    // Domain filtering
    .WithSenderDomainWhitelist("trusted.com", "partner.org")
    .WithSenderDomainBlacklist("spam.com", "junk.org")
    .WithRecipientDomainWhitelist("mydomain.com")
    
    // Message size limit
    .MaxMessageSizeMB(10)
    
    // Message storage
    .WithFileMessageStore(@"C:\\smtp_messages", createDateFolders: true)
    .Build();

// Event-Based Filtering (after message is received)
server.AddSpamFilter(new[] { "spam.com", "junk.org" });
server.AddSizeFilter(10 * 1024 * 1024); // 10MB
server.AddAllowedDomains("example.com");`;

const rateLimitExample = `using Zetian.Extensions;

// Rate limiting configuration
var rateLimitConfig = new RateLimitConfiguration
{
    MessagesPerHour = 100,
    MessagesPerMinute = 10,
    MessagesPerSecond = 2,
    EnableIpBasedLimiting = true,
    ResetPeriod = TimeSpan.FromHours(1)
};

server.AddRateLimiting(rateLimitConfig);

// Alternative: Ready-made configurations
server.AddRateLimiting(RateLimitConfiguration.PerHour(100));
server.AddRateLimiting(RateLimitConfiguration.PerMinute(10));`;

export default function ConfigurationPage() {
  return (
    <div className="min-h-screen py-12 bg-gray-50 dark:bg-gray-950">
      <div className="container mx-auto px-4 max-w-5xl">
        {/* Header */}
        <div className="mb-12">
          <div className="flex items-center gap-2 text-sm text-gray-600 dark:text-gray-400 mb-4">
            <Link href="/docs" className="hover:text-primary-600 dark:hover:text-primary-400">
              Documentation
            </Link>
            <span>/</span>
            <span>Configuration</span>
          </div>
          
          <h1 className="text-4xl font-bold text-gray-900 dark:text-white mb-4">
            Configuration
          </h1>
          <p className="text-xl text-gray-600 dark:text-gray-400">
            Configure Zetian SMTP Server according to your needs.
          </p>
        </div>

        {/* Configuration Overview */}
        <div className="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-6 mb-8">
          <div className="flex items-start gap-3">
            <Settings className="h-5 w-5 text-blue-600 dark:text-blue-400 mt-0.5" />
            <div>
              <h3 className="font-semibold text-blue-900 dark:text-blue-100 mb-2">Fluent Builder Pattern</h3>
              <p className="text-sm text-blue-800 dark:text-blue-200">
                Zetian uses fluent builder pattern for readable and chainable configuration.
                You can configure all settings in a single expression.
              </p>
            </div>
          </div>
        </div>

        {/* Main Configuration */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
            <Server className="h-6 w-6" />
            Basic Configuration
          </h2>
          
          <p className="text-gray-600 dark:text-gray-400 mb-4">
            Configure your server with SmtpServerBuilder:
          </p>
          
          <CodeBlock 
            code={configExample}
            language="csharp"
            filename="ServerConfiguration.cs"
          />

          {/* Configuration Options */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mt-6">
            <div className="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 p-4">
              <h4 className="font-semibold text-gray-900 dark:text-white mb-2">Basic Settings</h4>
              <ul className="space-y-1 text-sm text-gray-600 dark:text-gray-400">
                <li>• <code className="text-blue-600 dark:text-blue-400">Port</code> - SMTP port number</li>
                <li>• <code className="text-blue-600 dark:text-blue-400">ServerName</code> - Server name</li>
                <li>• <code className="text-blue-600 dark:text-blue-400">MaxMessageSizeMB</code> - Max message size</li>
                <li>• <code className="text-blue-600 dark:text-blue-400">MaxRecipients</code> - Max number of recipients</li>
              </ul>
            </div>
            
            <div className="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 p-4">
              <h4 className="font-semibold text-gray-900 dark:text-white mb-2">Connection Limits</h4>
              <ul className="space-y-1 text-sm text-gray-600 dark:text-gray-400">
                <li>• <code className="text-blue-600 dark:text-blue-400">MaxConnections</code> - Total connection limit</li>
                <li>• <code className="text-blue-600 dark:text-blue-400">MaxConnectionsPerIP</code> - Limit per IP</li>
                <li>• <code className="text-blue-600 dark:text-blue-400">ConnectionTimeout</code> - Connection timeout</li>
                <li>• <code className="text-blue-600 dark:text-blue-400">CommandTimeout</code> - Command timeout</li>
              </ul>
            </div>
          </div>
        </section>

        {/* Authentication Configuration */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
            <Shield className="h-6 w-6" />
            Authentication Configuration
          </h2>
          
          <p className="text-gray-600 dark:text-gray-400 mb-4">
            Configure different authentication methods:
          </p>
          
          <CodeBlock 
            code={authConfigExample}
            language="csharp"
            filename="AuthConfiguration.cs"
          />

          <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mt-6">
            <div className="flex items-start gap-2">
              <CheckCircle className="h-4 w-4 text-green-500 mt-0.5" />
              <div>
                <p className="text-sm font-medium text-gray-900 dark:text-white">PLAIN Auth</p>
                <p className="text-xs text-gray-600 dark:text-gray-400">Simple username/password</p>
              </div>
            </div>
            <div className="flex items-start gap-2">
              <CheckCircle className="h-4 w-4 text-green-500 mt-0.5" />
              <div>
                <p className="text-sm font-medium text-gray-900 dark:text-white">LOGIN Auth</p>
                <p className="text-xs text-gray-600 dark:text-gray-400">Legacy auth</p>
              </div>
            </div>
            <div className="flex items-start gap-2">
              <CheckCircle className="h-4 w-4 text-green-500 mt-0.5" />
              <div>
                <p className="text-sm font-medium text-gray-900 dark:text-white">Custom Auth</p>
                <p className="text-xs text-gray-600 dark:text-gray-400">Custom handler</p>
              </div>
            </div>
          </div>
        </section>

        {/* Filtering Configuration */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
            <Filter className="h-6 w-6" />
            Filtering Configuration
          </h2>
          
          <p className="text-gray-600 dark:text-gray-400 mb-4">
            Two different filtering approaches:
          </p>
          
          <CodeBlock 
            code={filterConfigExample}
            language="csharp"
            filename="FilterConfiguration.cs"
          />

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mt-6">
            <div className="bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg p-4">
              <h4 className="font-semibold text-green-900 dark:text-green-100 mb-2">Protocol-Level Filtering</h4>
              <p className="text-sm text-green-800 dark:text-green-200 mb-2">
                Early rejection during SMTP commands. More efficient, saves bandwidth.
              </p>
              <ul className="space-y-1 text-xs text-green-700 dark:text-green-300">
                <li>• At MAIL FROM/RCPT TO level</li>
                <li>• Bandwidth savings</li>
                <li>• Fast rejection</li>
              </ul>
            </div>
            
            <div className="bg-purple-50 dark:bg-purple-900/20 border border-purple-200 dark:border-purple-800 rounded-lg p-4">
              <h4 className="font-semibold text-purple-900 dark:text-purple-100 mb-2">Event-Based Filtering</h4>
              <p className="text-sm text-purple-800 dark:text-purple-200 mb-2">
                Filtering after message is received. More flexible, can check content.
              </p>
              <ul className="space-y-1 text-xs text-purple-700 dark:text-purple-300">
                <li>• Full message content check</li>
                <li>• Complex logic support</li>
                <li>• Dynamic filters</li>
              </ul>
            </div>
          </div>
        </section>

        {/* Rate Limiting */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
            <Gauge className="h-6 w-6" />
            Rate Limiting
          </h2>
          
          <p className="text-gray-600 dark:text-gray-400 mb-4">
            Speed limiting for spam protection:
          </p>
          
          <CodeBlock 
            code={rateLimitExample}
            language="csharp"
            filename="RateLimiting.cs"
          />

          <div className="bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg p-6 mt-6">
            <div className="flex items-start gap-3">
              <AlertCircle className="h-5 w-5 text-yellow-600 dark:text-yellow-400 mt-0.5" />
              <div>
                <h4 className="font-semibold text-yellow-900 dark:text-yellow-100 mb-2">Important Note</h4>
                <p className="text-sm text-yellow-800 dark:text-yellow-200">
                  Rate limiting works based on IP. Be careful in localhost tests.
                  You can keep limits high in development environment.
                </p>
              </div>
            </div>
          </div>
        </section>

        {/* Next Steps */}
        <div className="mt-12 flex gap-4">
          <Link 
            href="/docs/authentication"
            className="flex-1 p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 hover:shadow-md transition-all group"
          >
            <div className="flex items-center justify-between">
              <div>
                <h4 className="font-medium text-gray-900 dark:text-white group-hover:text-blue-600 dark:group-hover:text-blue-400">
                  Authentication →
                </h4>
                <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">
                  Security and authentication details
                </p>
              </div>
              <Lock className="h-5 w-5 text-gray-400" />
            </div>
          </Link>
        </div>
      </div>
    </div>
  );
}