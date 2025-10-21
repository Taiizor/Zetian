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
  Settings
} from 'lucide-react';

export const metadata: Metadata = {
  title: 'API Reference',
  description: 'Zetian SMTP Server API documentation - Classes, methods and interfaces',
};

const apiCategories = [
  {
    title: 'Core Classes',
    icon: Box,
    items: [
      {
        name: 'SmtpServer',
        description: 'Main SMTP server class that handles connections and messages',
        properties: ['Configuration', 'IsRunning', 'Endpoint'],
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
      },
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
      }
    ]
  },
  {
    title: 'Authentication',
    icon: Shield,
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
      },
      {
        name: 'InMemoryRateLimiter',
        description: 'In-memory rate limiter implementation',
        properties: ['Configuration'],
        methods: [],
        events: []
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
                <div className="flex items-center gap-3 mb-6">
                  <div className="p-2 bg-primary-100 dark:bg-primary-900/30 rounded-lg">
                    <Icon className="h-5 w-5 text-primary-600 dark:text-primary-400" />
                  </div>
                  <h2 className="text-2xl font-bold text-gray-900 dark:text-white">
                    {category.title}
                  </h2>
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