import { Metadata } from 'next';
import Link from 'next/link';
import { 
  Code2, 
  FileCode, 
  Shield,
  Database,
  Filter,
  ChevronRight,
  Hash,
  Box,
  Zap,
  Lock,
  Mail,
  Settings,
  Gauge,
  Heart,
  Activity
} from 'lucide-react';

export const metadata: Metadata = {
  title: 'API Reference',
  description: 'Zetian SMTP Server API documentation - Classes, methods and interfaces',
};

const apiCategories = [
  {
    title: 'Core Classes',
    icon: Box,
    namespace: 'Zetian.Server',
    items: [
      {
        name: 'SmtpServer',
        description: 'Main SMTP server class that handles connections and messages',
        properties: ['Configuration', 'StartTime', 'IsRunning', 'Endpoint'],
        methods: ['StartAsync()', 'StopAsync()', 'Dispose()'],
        events: ['MessageReceived', 'SessionCreated', 'SessionCompleted', 'ErrorOccurred']
      },
      {
        name: 'SmtpServerBuilder',
        description: 'Fluent builder for configuring SMTP servers',
        properties: [],
        methods: [
          'Port(int)', 
          'BindTo(IPAddress)',
          'ServerName(string)', 
          'MaxMessageSize(long)',
          'MaxMessageSizeMB(int)',
          'MaxRecipients(int)',
          'MaxConnections(int)',
          'MaxConnectionsPerIP(int)',
          'EnablePipelining(bool)',
          'Enable8BitMime(bool)',
          'EnableSmtpUtf8(bool)',
          'Certificate(X509Certificate2)',
          'Certificate(string, string)',
          'SslProtocols(SslProtocols)',
          'RequireAuthentication(bool)',
          'RequireSecureConnection(bool)',
          'AllowPlainTextAuthentication(bool)',
          'AddAuthenticationMechanism(string)',
          'AuthenticationHandler(handler)',
          'SimpleAuthentication(user, pass)',
          'ConnectionTimeout(TimeSpan)',
          'CommandTimeout(TimeSpan)',
          'DataTimeout(TimeSpan)',
          'LoggerFactory(ILoggerFactory)',
          'EnableVerboseLogging(bool)',
          'Banner(string)',
          'Greeting(string)',
          'BufferSize(read, write)',
          'MessageStore(IMessageStore)',
          'WithFileMessageStore(dir, createFolders)',
          'MailboxFilter(IMailboxFilter)',
          'WithSenderDomainWhitelist(domains)',
          'WithSenderDomainBlacklist(domains)',
          'WithRecipientDomainWhitelist(domains)',
          'WithRecipientDomainBlacklist(domains)',
          'Build()'
        ],
        events: []
      }
    ]
  },
  {
    title: 'Configuration',
    icon: Settings,
    namespace: 'Zetian.Configuration',
    items: [
      {
        name: 'SmtpServerConfiguration',
        description: 'Configuration settings for SMTP server',
        properties: [
          'Port',
          'IpAddress',
          'ServerName',
          'MaxMessageSize',
          'MaxRecipients',
          'MaxConnections',
          'MaxConnectionsPerIp',
          'EnablePipelining',
          'Enable8BitMime',
          'EnableSmtpUtf8',
          'Certificate',
          'SslProtocols',
          'RequireAuthentication',
          'RequireSecureConnection',
          'AllowPlainTextAuthentication',
          'AuthenticationMechanisms',
          'ConnectionTimeout',
          'CommandTimeout',
          'DataTimeout',
          'ReadBufferSize',
          'WriteBufferSize',
          'Banner',
          'Greeting',
          'LoggerFactory',
          'EnableVerboseLogging',
          'MessageStore',
          'MailboxFilter'
        ],
        methods: ['Validate()'],
        events: []
      }
    ]
  },
  {
    title: 'Core Interfaces',
    icon: FileCode,
    namespace: 'Zetian.Abstractions',
    items: [
      {
        name: 'ISmtpMessage',
        description: 'Represents an SMTP message',
        properties: [
          'Id',
          'From (MailAddress?)',
          'Recipients (IReadOnlyList<MailAddress>)',
          'Subject',
          'TextBody',
          'HtmlBody',
          'Headers',
          'Size',
          'Date',
          'Priority',
          'HasAttachments',
          'AttachmentCount'
        ],
        methods: [
          'GetRawData()',
          'GetRawDataAsync()',
          'GetRawDataStream()',
          'GetHeader(string)',
          'GetHeaders(string)',
          'SaveToFile(string)',
          'SaveToFileAsync(string)',
          'SaveToStream(Stream)',
          'SaveToStreamAsync(Stream)'
        ],
        events: []
      },
      {
        name: 'ISmtpSession',
        description: 'Represents an SMTP session',
        properties: [
          'Id',
          'RemoteEndPoint',
          'LocalEndPoint',
          'IsSecure',
          'IsAuthenticated',
          'AuthenticatedIdentity',
          'ClientDomain',
          'StartTime',
          'Properties',
          'ClientCertificate',
          'MessageCount',
          'PipeliningEnabled',
          'EightBitMimeEnabled',
          'BinaryMimeEnabled',
          'MaxMessageSize'
        ],
        methods: [],
        events: []
      },
      {
        name: 'IMessageStore',
        description: 'Message storage interface',
        properties: [],
        methods: ['SaveAsync(ISmtpSession, ISmtpMessage, CancellationToken)'],
        events: []
      },
      {
        name: 'IMailboxFilter',
        description: 'Mailbox filtering interface',
        properties: [],
        methods: [
          'CanAcceptFromAsync(ISmtpSession, string, long, CancellationToken)',
          'CanDeliverToAsync(ISmtpSession, string, string, CancellationToken)'
        ],
        events: []
      },
      {
        name: 'IStatisticsCollector',
        description: 'Interface for statistics collection',
        properties: ['TotalSessions', 'TotalMessages', 'TotalErrors', 'TotalBytes'],
        methods: [
          'RecordSession()',
          'RecordMessage(ISmtpMessage)',
          'RecordError(Exception)'
        ],
        events: []
      }
    ]
  },
  {
    title: 'Authentication',
    icon: Shield,
    namespace: 'Zetian.Authentication & Zetian.Models',
    items: [
      {
        name: 'IAuthenticator',
        description: 'Authentication mechanism interface',
        properties: ['Mechanism'],
        methods: ['AuthenticateAsync(session, initialResponse, reader, writer, ct)'],
        events: []
      },
      {
        name: 'AuthenticationResult',
        description: 'Authentication result',
        properties: ['Success', 'Identity', 'ErrorMessage'],
        methods: ['Succeed(string)', 'Fail(string?)'],
        events: []
      },
      {
        name: 'PlainAuthenticator',
        description: 'Authentication with PLAIN mechanism',
        properties: [],
        methods: [],
        events: []
      },
      {
        name: 'LoginAuthenticator',
        description: 'Authentication with LOGIN mechanism',
        properties: [],
        methods: [],
        events: []
      }
    ]
  },
  {
    title: 'Storage',
    icon: Database,
    namespace: 'Zetian.Storage',
    items: [
      {
        name: 'FileMessageStore',
        description: 'Saving messages to file system',
        properties: ['Directory', 'CreateDateFolders'],
        methods: ['SaveAsync()'],
        events: []
      },
      {
        name: 'NullMessageStore',
        description: 'Null store that does not save messages',
        properties: [],
        methods: ['SaveAsync()'],
        events: []
      }
    ]
  },
  {
    title: 'Filtering',
    icon: Filter,
    namespace: 'Zetian.Storage',
    items: [
      {
        name: 'DomainMailboxFilter',
        description: 'Domain-based filtering',
        properties: ['AllowedFromDomains', 'BlockedFromDomains', 'AllowedToDomains', 'BlockedToDomains'],
        methods: ['AllowFromDomains()', 'BlockFromDomains()', 'AllowToDomains()', 'BlockToDomains()'],
        events: []
      },
      {
        name: 'CompositeMailboxFilter',
        description: 'Combining multiple filters',
        properties: ['Mode', 'Filters'],
        methods: ['AddFilter()', 'RemoveFilter()'],
        events: []
      },
      {
        name: 'AcceptAllMailboxFilter',
        description: 'Filter that accepts all messages',
        properties: [],
        methods: [],
        events: []
      }
    ]
  },
  {
    title: 'Event Arguments',
    icon: Zap,
    namespace: 'Zetian.Models.EventArgs',
    items: [
      {
        name: 'MessageEventArgs',
        description: 'Event args for message events',
        properties: ['Message', 'Session', 'Cancel', 'Response'],
        methods: [],
        events: []
      },
      {
        name: 'SessionEventArgs',
        description: 'Event args for session events',
        properties: ['Session'],
        methods: [],
        events: []
      },
      {
        name: 'AuthenticationEventArgs',
        description: 'Event args for authentication events',
        properties: ['Mechanism', 'Username', 'Password', 'Session', 'IsAuthenticated', 'AuthenticatedIdentity'],
        methods: [],
        events: []
      },
      {
        name: 'ErrorEventArgs',
        description: 'Event args for error events',
        properties: ['Exception', 'Session'],
        methods: [],
        events: []
      }
    ]
  },
  {
    title: 'Protocol',
    icon: Mail,
    namespace: 'Zetian.Protocol',
    items: [
      {
        name: 'SmtpResponse',
        description: 'SMTP protocol response',
        properties: ['Code', 'Lines', 'Message', 'IsPositive', 'IsError', 'IsSuccess'],
        methods: ['ToString()'],
        events: [],
        staticMembers: [
          'Ok (250)',
          'ServiceReady (220)',
          'ServiceClosing (221)',
          'StartMailInput (354)',
          'AuthenticationRequired (530)',
          'AuthenticationSuccessful (235)',
          'AuthenticationFailed (535)',
          'ServiceNotAvailable (421)',
          'SyntaxError (500)',
          'BadSequence (503)',
          'TransactionFailed (554)'
        ]
      },
      {
        name: 'SmtpCommand',
        description: 'SMTP protocol command',
        properties: ['Name', 'Parameters'],
        methods: ['Parse(string)', 'IsValid()'],
        events: []
      }
    ]
  },
  {
    title: 'Extensions',
    icon: Zap,
    namespace: 'Zetian.Extensions & Zetian.Models',
    items: [
      {
        name: 'SmtpServerExtensions',
        description: 'Extension methods for SMTP server',
        properties: [],
        methods: [
          'AddRateLimiting(IRateLimiter)',
          'AddRateLimiting(RateLimitConfiguration)',
          'AddMessageFilter(Func<ISmtpMessage, bool>)',
          'AddSpamFilter(blacklistedDomains)',
          'AddSizeFilter(maxSizeBytes)',
          'SaveMessagesToDirectory(directory)',
          'LogMessages(logger)',
          'ForwardMessages(forwarder)',
          'AddRecipientValidation(validator)',
          'AddAllowedDomains(domains)',
          'AddStatistics(collector)'
        ],
        events: []
      },
      {
        name: 'RateLimitConfiguration',
        description: 'Rate limiting configuration',
        properties: ['MaxRequests', 'Window', 'UseSlidingWindow'],
        methods: [
          'PerMinute(maxRequests)',
          'PerHour(maxRequests)',
          'PerDay(maxRequests)'
        ],
        events: []
      },
      {
        name: 'IRateLimiter',
        description: 'Rate limiting interface',
        properties: [],
        methods: [
          'IsAllowedAsync(key)',
          'IsAllowedAsync(IPAddress)',
          'RecordRequestAsync(key)',
          'RecordRequestAsync(IPAddress)',
          'ResetAsync(key)',
          'GetRemainingAsync(key)'
        ],
        events: []
      }
    ]
  },
  {
    title: 'Rate Limiting',
    icon: Gauge,
    namespace: 'Zetian.RateLimiting & Zetian.Abstractions',
    items: [
      {
        name: 'InMemoryRateLimiter',
        description: 'In-memory rate limiter implementation',
        properties: ['Configuration'],
        methods: ['IsAllowedAsync(key)', 'IsAllowedAsync(IPAddress)', 'RecordRequestAsync(key)', 'RecordRequestAsync(IPAddress)', 'ResetAsync(key)', 'GetRemainingAsync(key)'],
        events: []
      }
    ]
  },
  {
    title: 'Delegates',
    icon: Code2,
    namespace: 'Zetian.Delegates',
    items: [
      {
        name: 'AuthenticationHandler',
        description: 'Delegate for handling authentication',
        properties: [],
        methods: [],
        events: [],
        signature: 'Task<AuthenticationResult> AuthenticationHandler(string? username, string? password)'
      }
    ]
  },
  {
    title: 'Enums',
    icon: Hash,
    namespace: 'Zetian.Enums',
    items: [
      {
        name: 'CompositeMode',
        description: 'Composite filter mode for combining multiple filters',
        properties: [],
        methods: [],
        events: [],
        values: ['All (AND logic)', 'Any (OR logic)']
      },
      {
        name: 'SmtpSessionState',
        description: 'SMTP session state enumeration',
        properties: [],
        methods: [],
        events: [],
        values: ['Connected', 'AwaitingCommand', 'ReceivingData', 'Closing']
      }
    ]
  },
  {
    title: 'Health Check',
    icon: Heart,
    namespace: 'Zetian.HealthCheck',
    items: [
      {
        name: 'IHealthCheck',
        description: 'Interface for implementing health checks',
        properties: [],
        methods: ['CheckHealthAsync(CancellationToken)'],
        events: []
      },
      {
        name: 'HealthCheckResult',
        description: 'Represents the result of a health check',
        properties: ['Status', 'Description', 'Exception', 'Data'],
        methods: [
          'Healthy(description?, data?)',
          'Degraded(description?, exception?, data?)',
          'Unhealthy(description?, exception?, data?)'
        ],
        events: []
      },
      {
        name: 'HealthCheckService',
        description: 'HTTP service for health check endpoints',
        properties: ['Options', 'IsRunning', 'HttpListener'],
        methods: [
          'StartAsync(CancellationToken)',
          'StopAsync(CancellationToken)',
          'AddHealthCheck(name, check)',
          'AddHealthCheck(name, checkFunc)'
        ],
        events: []
      },
      {
        name: 'SmtpServerHealthCheck',
        description: 'Health check implementation for SMTP server',
        properties: ['Server', 'Options'],
        methods: ['CheckHealthAsync(CancellationToken)'],
        events: []
      },
      {
        name: 'HealthCheckServiceOptions',
        description: 'Options for health check service',
        properties: ['Host', 'Port', 'Endpoints', 'Timeout', 'DetailedErrors'],
        methods: [],
        events: []
      },
      {
        name: 'SmtpHealthCheckOptions',
        description: 'Options for SMTP server health check',
        properties: [
          'DegradedThreshold',
          'UnhealthyThreshold',
          'MemoryThresholdMB',
          'CheckInterval'
        ],
        methods: [],
        events: []
      }
    ]
  },
  {
    title: 'Health Check Extensions',
    icon: Activity,
    namespace: 'Zetian.HealthCheck.Extensions',
    items: [
      {
        name: 'HealthCheckExtensions',
        description: 'Extension methods for adding health checks to SMTP server',
        properties: [],
        methods: [
          'EnableHealthCheck(port)',
          'EnableHealthCheck(hostname, port)',
          'EnableHealthCheck(IPAddress, port)',
          'EnableHealthCheck(options)',
          'StartWithHealthCheckAsync(port, ct)',
          'StartWithHealthCheckAsync(hostname, port, ct)',
          'StartWithHealthCheckAsync(IPAddress, port, ct)',
          'StartWithHealthCheckAsync(options, ct)',
          'AddHealthCheck(healthCheckService, name, check)',
          'AddHealthCheck(healthCheckService, name, checkFunc)'
        ],
        events: []
      }
    ]
  },
  {
    title: 'Health Check Enums',
    icon: Heart,
    namespace: 'Zetian.HealthCheck.Enums',
    items: [
      {
        name: 'HealthStatus',
        description: 'Health status enumeration',
        properties: [],
        methods: [],
        events: [],
        values: ['Healthy (0)', 'Degraded (1)', 'Unhealthy (2)']
      }
    ]
  }
];

export default function ApiReferencePage() {
  return (
    <div className="min-h-screen py-12 bg-gray-50 dark:bg-gray-950">
      <div className="container mx-auto px-4">
        {/* Header */}
        <div className="text-center mb-12">
          <h1 className="text-4xl font-bold text-gray-900 dark:text-white mb-4">
            API Reference
          </h1>
          <p className="text-xl text-gray-600 dark:text-gray-400 max-w-3xl mx-auto">
            Detailed documentation of all classes, interfaces and methods of Zetian SMTP Server.
          </p>
        </div>

        {/* Quick Navigation */}
        <div className="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 p-6 mb-12 max-w-4xl mx-auto">
          <h2 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">Quick Navigation</h2>
          <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
            {apiCategories.map((category) => (
              <a
                key={category.title}
                href={`#${category.title.toLowerCase().replace(/\s+/g, '-')}`}
                className="flex items-center gap-2 text-gray-600 dark:text-gray-400 hover:text-primary-600 dark:hover:text-primary-400 transition-colors"
              >
                <category.icon className="h-4 w-4" />
                <span>{category.title}</span>
              </a>
            ))}
          </div>
        </div>

        {/* API Categories */}
        <div className="space-y-12 max-w-6xl mx-auto">
          {apiCategories.map((category) => {
            const Icon = category.icon;
            return (
              <div 
                key={category.title}
                id={category.title.toLowerCase().replace(/\s+/g, '-')}
                className="scroll-mt-20"
              >
                {/* Category Header */}
                <div className="mb-6">
                  <div className="flex items-center gap-3 mb-2">
                    <div className="p-2 bg-primary-100 dark:bg-primary-900/30 rounded-lg">
                      <Icon className="h-5 w-5 text-primary-600 dark:text-primary-400" />
                    </div>
                    <h2 className="text-2xl font-bold text-gray-900 dark:text-white">
                      {category.title}
                    </h2>
                  </div>
                  {'namespace' in category && category.namespace && (
                    <p className="text-sm text-gray-600 dark:text-gray-400 ml-12">
                      Namespace: <code className="bg-gray-100 dark:bg-gray-800 px-2 py-1 rounded text-primary-600 dark:text-primary-400">{category.namespace}</code>
                    </p>
                  )}
                </div>

                {/* Items */}
                <div className="grid gap-6">
                  {category.items.map((item) => (
                    <div 
                      key={item.name}
                      className="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 p-6 hover:shadow-lg transition-shadow"
                    >
                      {/* Item Header */}
                      <div className="mb-4">
                        <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-2 font-mono">
                          {item.name}
                        </h3>
                        <p className="text-gray-600 dark:text-gray-400">
                          {item.description}
                        </p>
                      </div>

                      {/* Properties */}
                      {item.properties.length > 0 && (
                        <div className="mb-4">
                          <h4 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2 uppercase tracking-wider">
                            Properties
                          </h4>
                          <div className="space-y-2">
                            {item.properties.map((prop) => (
                              <div 
                                key={prop}
                                className="flex items-center gap-2 text-sm"
                              >
                                <Hash className="h-3 w-3 text-gray-400" />
                                <code className="bg-gray-100 dark:bg-gray-800 px-2 py-1 rounded text-primary-600 dark:text-primary-400">
                                  {prop}
                                </code>
                              </div>
                            ))}
                          </div>
                        </div>
                      )}

                      {/* Methods */}
                      {item.methods.length > 0 && (
                        <div className="mb-4">
                          <h4 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2 uppercase tracking-wider">
                            Methods
                          </h4>
                          <div className="space-y-2">
                            {item.methods.map((method) => (
                              <div 
                                key={method}
                                className="flex items-center gap-2 text-sm"
                              >
                                <ChevronRight className="h-3 w-3 text-gray-400" />
                                <code className="bg-gray-100 dark:bg-gray-800 px-2 py-1 rounded text-green-600 dark:text-green-400">
                                  {method}
                                </code>
                              </div>
                            ))}
                          </div>
                        </div>
                      )}

                      {/* Static Members */}
                      {'staticMembers' in item && item.staticMembers && item.staticMembers.length > 0 && (
                        <div>
                          <h4 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2 uppercase tracking-wider">
                            Static Members
                          </h4>
                          <div className="space-y-2">
                            {item.staticMembers.map((member: string) => (
                              <div 
                                key={member}
                                className="flex items-center gap-2 text-sm"
                              >
                                <Hash className="h-3 w-3 text-gray-400" />
                                <code className="bg-gray-100 dark:bg-gray-800 px-2 py-1 rounded text-indigo-600 dark:text-indigo-400">
                                  {member}
                                </code>
                              </div>
                            ))}
                          </div>
                        </div>
                      )}

                      {/* Events */}
                      {item.events.length > 0 && (
                        <div>
                          <h4 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2 uppercase tracking-wider">
                            Events
                          </h4>
                          <div className="space-y-2">
                            {item.events.map((event) => (
                              <div 
                                key={event}
                                className="flex items-center gap-2 text-sm"
                              >
                                <Zap className="h-3 w-3 text-gray-400" />
                                <code className="bg-gray-100 dark:bg-gray-800 px-2 py-1 rounded text-purple-600 dark:text-purple-400">
                                  {event}
                                </code>
                              </div>
                            ))}
                          </div>
                        </div>
                      )}

                      {/* Delegate Signature */}
                      {'signature' in item && item.signature && (
                        <div>
                          <h4 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2 uppercase tracking-wider">
                            Signature
                          </h4>
                          <div className="bg-gray-100 dark:bg-gray-800 p-3 rounded">
                            <code className="text-sm text-blue-600 dark:text-blue-400">
                              {item.signature}
                            </code>
                          </div>
                        </div>
                      )}

                      {/* Enum Values */}
                      {'values' in item && item.values && item.values.length > 0 && (
                        <div>
                          <h4 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2 uppercase tracking-wider">
                            Values
                          </h4>
                          <div className="space-y-2">
                            {item.values.map((value: string) => (
                              <div 
                                key={value}
                                className="flex items-center gap-2 text-sm"
                              >
                                <Hash className="h-3 w-3 text-gray-400" />
                                <code className="bg-gray-100 dark:bg-gray-800 px-2 py-1 rounded text-orange-600 dark:text-orange-400">
                                  {value}
                                </code>
                              </div>
                            ))}
                          </div>
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              </div>
            );
          })}
        </div>

        {/* Help Section */}
        <div className="mt-16 text-center">
          <div className="inline-flex items-center gap-2 px-4 py-2 bg-blue-100 dark:bg-blue-900/30 rounded-full text-sm">
            <Code2 className="h-4 w-4 text-blue-600 dark:text-blue-400" />
            <span className="text-blue-700 dark:text-blue-300">
              For detailed examples and usage
            </span>
            <Link 
              href="/examples"
              className="text-blue-600 dark:text-blue-400 hover:underline font-medium"
            >
              Examples page
            </Link>
            <span className="text-blue-700 dark:text-blue-300">visit</span>
          </div>
        </div>
      </div>
    </div>
  );
}