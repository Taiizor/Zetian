'use client';

import Link from 'next/link';
import { 
  ArrowLeft,
  GitBranch,
  CheckCircle2,
  Star,
  Zap,
  Shield,
  Package,
  Code,
  Sparkles,
  BookOpen,
  AlertCircle
} from 'lucide-react';

export default function ChangelogPage() {
  return (
    <div className="min-h-screen py-12 bg-gray-50 dark:bg-gray-950">
      <div className="container mx-auto px-4 max-w-4xl">
        {/* Header */}
        <div className="mb-12">
          <Link 
            href="/" 
            className="inline-flex items-center gap-2 text-sm text-gray-600 dark:text-gray-400 mb-4 hover:text-blue-600 dark:hover:text-blue-400 transition-colors"
          >
            <ArrowLeft className="h-4 w-4" />
            Back to Home
          </Link>
          
          <h1 className="text-4xl font-bold text-gray-900 dark:text-white mb-4">
            Changelog
          </h1>
          <p className="text-xl text-gray-600 dark:text-gray-400">
            All notable changes to Zetian SMTP Server will be documented here.
          </p>
        </div>

        {/* Version 1.0.0 - Initial Release (Upcoming) */}
        <div className="mb-16">
          <div className="flex items-center gap-3 mb-6">
            <div className="px-3 py-1 bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-300 rounded-full text-sm font-semibold">
              Current Release
            </div>
            <h2 className="text-2xl font-bold text-gray-900 dark:text-white">
              Version 1.0.20
            </h2>
            <span className="text-gray-500 dark:text-gray-400 text-sm">
              Full-Featured SMTP Server with Extensions
            </span>
          </div>

          <div className="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 p-6 space-y-8">
            {/* Core Features */}
            <div>
              <div className="flex items-center gap-2 mb-3">
                <Sparkles className="h-5 w-5 text-blue-500" />
                <h3 className="font-semibold text-lg text-gray-900 dark:text-white">Core Features</h3>
              </div>
              <ul className="space-y-2 text-gray-600 dark:text-gray-400">
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                  <span>High-performance async SMTP server implementation</span>
                </li>
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                  <span>Efficient async/await patterns and optimized I/O</span>
                </li>
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                  <span>Minimal dependencies - only essential packages</span>
                </li>
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                  <span>.NET 6, 7, 8, 9, and 10 support</span>
                </li>
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                  <span>Fluent API with SmtpServerBuilder for easy configuration</span>
                </li>
              </ul>
            </div>

            {/* Authentication */}
            <div>
              <div className="flex items-center gap-2 mb-3">
                <Shield className="h-5 w-5 text-purple-500" />
                <h3 className="font-semibold text-lg text-gray-900 dark:text-white">Authentication & Security</h3>
              </div>
              <ul className="space-y-2 text-gray-600 dark:text-gray-400">
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                  <span>PLAIN and LOGIN authentication mechanisms</span>
                </li>
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                  <span>TLS/SSL support with STARTTLS command</span>
                </li>
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                  <span>Custom authentication providers via IAuthenticator interface</span>
                </li>
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                  <span>Per-IP connection limiting for DDoS protection</span>
                </li>
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                  <span>Certificate validation and secure communication</span>
                </li>
              </ul>
            </div>

            {/* Message Filtering */}
            <div>
              <div className="flex items-center gap-2 mb-3">
                <Zap className="h-5 w-5 text-yellow-500" />
                <h3 className="font-semibold text-lg text-gray-900 dark:text-white">Message Filtering & Processing</h3>
              </div>
              <ul className="space-y-2 text-gray-600 dark:text-gray-400">
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                  <span>IMailboxFilter interface for custom filtering logic</span>
                </li>
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                  <span>DomainMailboxFilter with whitelist/blacklist support</span>
                </li>
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                  <span>CompositeMailboxFilter for combining multiple filters</span>
                </li>
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                  <span>AcceptAllMailboxFilter for development scenarios</span>
                </li>
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                  <span>Rate limiting with configurable thresholds</span>
                </li>
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                  <span>Message size limits and validation</span>
                </li>
              </ul>
            </div>

            {/* Storage */}
            <div>
              <div className="flex items-center gap-2 mb-3">
                <Package className="h-5 w-5 text-green-500" />
                <h3 className="font-semibold text-lg text-gray-900 dark:text-white">Storage & Persistence</h3>
              </div>
              <ul className="space-y-2 text-gray-600 dark:text-gray-400">
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                  <span>IMessageStore interface for custom storage backends</span>
                </li>
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                  <span>FileMessageStore for filesystem persistence</span>
                </li>
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                  <span>NullMessageStore for testing and forwarding scenarios</span>
                </li>
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                  <span>Automatic .eml file generation with headers</span>
                </li>
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                  <span>Directory-based organization by date</span>
                </li>
              </ul>
            </div>

            {/* SMTP Protocol */}
            <div>
              <div className="flex items-center gap-2 mb-3">
                <Code className="h-5 w-5 text-orange-500" />
                <h3 className="font-semibold text-lg text-gray-900 dark:text-white">SMTP Protocol Support</h3>
              </div>
              <ul className="space-y-2 text-gray-600 dark:text-gray-400">
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                  <span>Full SMTP command implementation (HELO, EHLO, MAIL FROM, RCPT TO, DATA, etc.)</span>
                </li>
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                  <span>ESMTP extensions support</span>
                </li>
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                  <span>8BITMIME for international character support</span>
                </li>
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                  <span>PIPELINING for improved performance</span>
                </li>
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                  <span>SMTPUTF8 for Unicode email addresses</span>
                </li>
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                  <span>SIZE extension with configurable limits</span>
                </li>
              </ul>
            </div>

            {/* Extensibility */}
            <div>
              <div className="flex items-center gap-2 mb-3">
                <Star className="h-5 w-5 text-indigo-500" />
                <h3 className="font-semibold text-lg text-gray-900 dark:text-white">Extensibility & Events</h3>
              </div>
              <ul className="space-y-2 text-gray-600 dark:text-gray-400">
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                  <span>Rich event system (SessionCreated, MessageReceived, SessionCompleted, etc.)</span>
                </li>
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                  <span>Extension methods for adding custom functionality</span>
                </li>
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                  <span>Plugin system support via ISmtpPlugin interface</span>
                </li>
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                  <span>Builder pattern extensions (SmtpServerBuilderExtensions)</span>
                </li>
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                  <span>CreateDevelopment() and CreateProduction() helper methods</span>
                </li>
              </ul>
            </div>

            {/* Extensions */}
            <div>
              <div className="flex items-center gap-2 mb-3">
                <Package className="h-5 w-5 text-red-500" />
                <h3 className="font-semibold text-lg text-gray-900 dark:text-white">Extensions & Packages</h3>
              </div>
              <ul className="space-y-2 text-gray-600 dark:text-gray-400">
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                  <span>Zetian.Relay - SMTP relay with smart host support, queue management, failover</span>
                </li>
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                  <span>Zetian.AntiSpam - SPF/DKIM/DMARC, RBL/DNSBL, Bayesian filtering, Greylisting</span>
                </li>
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                  <span>Zetian.Storage.SqlServer - Enterprise SQL Server storage provider</span>
                </li>
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                  <span>Zetian.Storage.PostgreSql - PostgreSQL with JSONB and partitioning</span>
                </li>
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                  <span>Zetian.Storage.MongoDB - NoSQL storage with GridFS</span>
                </li>
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                  <span>Zetian.Storage.Redis - High-performance in-memory caching</span>
                </li>
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                  <span>Zetian.Storage.S3 - Amazon S3 and compatible object storage</span>
                </li>
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                  <span>Zetian.Storage.AzureBlob - Azure Blob storage with tier management</span>
                </li>
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                  <span>Zetian.HealthCheck - Health monitoring endpoints for Kubernetes/Docker</span>
                </li>
              </ul>
            </div>

            {/* Developer Experience */}
            <div>
              <div className="flex items-center gap-2 mb-3">
                <BookOpen className="h-5 w-5 text-cyan-500" />
                <h3 className="font-semibold text-lg text-gray-900 dark:text-white">Developer Experience</h3>
              </div>
              <ul className="space-y-2 text-gray-600 dark:text-gray-400">
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                  <span>Comprehensive XML documentation</span>
                </li>
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                  <span>16+ ready-to-use examples including Relay and AntiSpam</span>
                </li>
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                  <span>Python test scripts for validation</span>
                </li>
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                  <span>Benchmark suite for performance testing</span>
                </li>
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                  <span>Comprehensive unit and integration tests</span>
                </li>
                <li className="flex items-start gap-2">
                  <CheckCircle2 className="h-4 w-4 text-green-500 mt-0.5 flex-shrink-0" />
                  <span>Modern documentation site with Next.js and interactive API reference</span>
                </li>
              </ul>
            </div>
          </div>
        </div>

        {/* Coming Soon Section */}
        <div className="mb-16">
          <div className="flex items-center gap-3 mb-6">
            <div className="px-3 py-1 bg-yellow-100 dark:bg-yellow-900/30 text-yellow-700 dark:text-yellow-300 rounded-full text-sm font-semibold">
              Planned
            </div>
            <h2 className="text-2xl font-bold text-gray-900 dark:text-white">
              Future Releases
            </h2>
          </div>

          <div className="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 p-6">
            <div className="space-y-4">
              <div className="flex items-start gap-3">
                <GitBranch className="h-5 w-5 text-blue-500 mt-0.5" />
                <div>
                  <h4 className="font-semibold text-gray-900 dark:text-white mb-1">Version 1.1.0</h4>
                  <ul className="space-y-1 text-sm text-gray-600 dark:text-gray-400">
                    <li>• WebSocket support for real-time monitoring</li>
                    <li>• Enhanced DMARC reporting</li>
                    <li>• Advanced queue management features</li>
                    <li>• Performance optimizations for high-volume servers</li>
                  </ul>
                </div>
              </div>

              <div className="flex items-start gap-3">
                <GitBranch className="h-5 w-5 text-purple-500 mt-0.5" />
                <div>
                  <h4 className="font-semibold text-gray-900 dark:text-white mb-1">Version 1.2.0</h4>
                  <ul className="space-y-1 text-sm text-gray-600 dark:text-gray-400">
                    <li>• Clustering support for high availability</li>
                    <li>• Distributed queue with Redis backend</li>
                    <li>• GraphQL API for management</li>
                    <li>• Advanced metrics with Prometheus export</li>
                  </ul>
                </div>
              </div>

              <div className="flex items-start gap-3">
                <GitBranch className="h-5 w-5 text-green-500 mt-0.5" />
                <div>
                  <h4 className="font-semibold text-gray-900 dark:text-white mb-1">Version 2.0.0</h4>
                  <ul className="space-y-1 text-sm text-gray-600 dark:text-gray-400">
                    <li>• Full IMAP server implementation</li>
                    <li>• POP3 server support</li>
                    <li>• Web-based administration panel</li>
                    <li>• Cloud-native deployment templates</li>
                  </ul>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Version Guidelines */}
        <div className="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-6">
          <div className="flex items-start gap-3">
            <AlertCircle className="h-5 w-5 text-blue-600 dark:text-blue-400 mt-0.5" />
            <div>
              <h3 className="font-semibold text-blue-900 dark:text-blue-100 mb-2">
                Versioning Guidelines
              </h3>
              <p className="text-sm text-blue-800 dark:text-blue-200 mb-3">
                Zetian follows Semantic Versioning 2.0.0 (SemVer)
              </p>
              <ul className="space-y-1 text-sm text-blue-800 dark:text-blue-200">
                <li>• <strong>Major (X.0.0):</strong> Breaking API changes</li>
                <li>• <strong>Minor (0.X.0):</strong> New features, backwards compatible</li>
                <li>• <strong>Patch (0.0.X):</strong> Bug fixes, backwards compatible</li>
              </ul>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}