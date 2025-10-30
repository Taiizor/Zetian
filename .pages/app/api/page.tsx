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
        properties: ['ActiveSessionCount', 'Configuration', 'StartTime', 'IsRunning', 'Endpoint'],
        methods: ['StartAsync()', 'StopAsync()', 'Dispose()'],
        events: ['SessionCompleted', 'MessageReceived', 'SessionCreated', 'ErrorOccurred']
      },
      {
        name: 'SmtpServerBuilder',
        description: 'Fluent builder for configuring SMTP servers',
        properties: [],
        methods: [
          'Port(int)', 
          'BindTo(IPAddress)',
          'BindTo(string)',
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
          'Certificate(string path, string? password)',
          'CertificateFromPfx(string path, string? password, X509KeyStorageFlags flags)',
          'CertificateFromPem(string certPath, string? keyPath)',
          'CertificateFromCer(string path)',
          'SslProtocols(SslProtocols)',
          'RequireAuthentication(bool)',
          'RequireSecureConnection(bool)',
          'AllowPlainTextAuthentication(bool)',
          'AddAuthenticationMechanism(string)',
          'AuthenticationHandler(AuthenticationHandler)',
          'SimpleAuthentication(username, password)',
          'ConnectionTimeout(TimeSpan)',
          'CommandTimeout(TimeSpan)',
          'DataTimeout(TimeSpan)',
          'MaxRetryCount(int)',
          'LoggerFactory(ILoggerFactory)',
          'EnableVerboseLogging(bool)',
          'Banner(string)',
          'Greeting(string)',
          'BufferSize(readSize, writeSize)',
          'MessageStore(IMessageStore)',
          'WithFileMessageStore(directory, createDateFolders)',
          'MailboxFilter(IMailboxFilter)',
          'WithSenderDomainWhitelist(params string[])',
          'WithSenderDomainBlacklist(params string[])',
          'WithRecipientDomainWhitelist(params string[])',
          'WithRecipientDomainBlacklist(params string[])',
          'Build()',
          'CreateBasic()',
          'CreateSecure(X509Certificate2)',
          'CreateAuthenticated(int, AuthenticationHandler)',
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
          'MaxRetryCount',
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
    namespace: 'Zetian.Authentication & Zetian.Abstractions',
    items: [
      {
        name: 'IAuthenticator',
        description: 'Authentication mechanism interface (Zetian.Abstractions)',
        properties: ['Mechanism'],
        methods: ['AuthenticateAsync(session, initialResponse, reader, writer, ct)'],
        events: []
      },
      {
        name: 'AuthenticationResult',
        description: 'Authentication result (Zetian.Models)',
        properties: ['Success', 'Identity', 'ErrorMessage'],
        methods: ['Succeed(string identity)', 'Fail(string? errorMessage)'],
        events: []
      },
      {
        name: 'PlainAuthenticator',
        description: 'PLAIN mechanism authentication implementation',
        properties: ['Mechanism'],
        methods: ['AuthenticateAsync(session, initialResponse, reader, writer, ct)'],
        events: []
      },
      {
        name: 'LoginAuthenticator',
        description: 'LOGIN mechanism authentication implementation',
        properties: ['Mechanism'],
        methods: ['AuthenticateAsync(session, initialResponse, reader, writer, ct)'],
        events: []
      },
      {
        name: 'AuthenticatorFactory',
        description: 'Factory for creating authenticators',
        properties: [],
        methods: [
          'Create(mechanism)',
          'SetDefaultHandler(handler)',
          'GetDefaultHandler()',
          'ClearDefaultHandler()'
        ],
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
        methods: ['SaveAsync(ISmtpSession, ISmtpMessage, CancellationToken)'],
        events: []
      },
      {
        name: 'NullMessageStore',
        description: 'Null store that does not save messages',
        properties: [],
        methods: ['SaveAsync(ISmtpSession, ISmtpMessage, CancellationToken)'],
        events: []
      },
      {
        name: 'BaseStorageConfiguration',
        description: 'Base configuration for all storage providers',
        properties: [
          'MaxMessageSizeMB',
          'CompressMessageBody',
          'CompressionThresholdKB',
          'EnableRetry',
          'MaxRetryAttempts',
          'RetryDelayMs',
          'ConnectionTimeoutSeconds',
          'LogErrors',
          'Logger'
        ],
        methods: ['Validate()'],
        events: []
      }
    ]
  },
  {
    title: 'Storage Providers',
    icon: Database,
    namespace: 'Zetian.Storage.Providers',
    items: [
      {
        name: 'SqlServerMessageStore',
        description: 'SQL Server and Azure SQL Database storage provider',
        properties: ['ConnectionString', 'Configuration'],
        methods: [
          'SaveAsync(ISmtpSession, ISmtpMessage, CancellationToken)'
        ],
        events: []
      },
      {
        name: 'SqlServerStorageConfiguration',
        description: 'Configuration for SQL Server storage',
        properties: [
          'TableName',
          'SchemaName',
          'AutoCreateTable',
          'StoreAttachmentsSeparately',
          'AttachmentsTableName',
          'CommandTimeoutSeconds',
          'BulkCopyBatchSize'
        ],
        methods: ['Validate()', 'GetFullTableName()', 'GetAttachmentsTableName()'],
        events: []
      },
      {
        name: 'PostgreSqlMessageStore',
        description: 'PostgreSQL storage provider with JSONB support',
        properties: ['ConnectionString', 'Configuration'],
        methods: [
          'SaveAsync(ISmtpSession, ISmtpMessage, CancellationToken)'
        ],
        events: []
      },
      {
        name: 'PostgreSqlStorageConfiguration',
        description: 'Configuration for PostgreSQL storage',
        properties: [
          'TableName',
          'SchemaName',
          'AutoCreateTable',
          'UseJsonbForHeaders',
          'EnablePartitioning',
          'PartitionInterval',
          'CreateIndexes',
          'RetentionMonths'
        ],
        methods: ['Validate()', 'GetFullTableName()', 'GetPartitionName(DateTime)'],
        events: []
      },
      {
        name: 'MongoDbMessageStore',
        description: 'MongoDB NoSQL storage provider with GridFS support',
        properties: ['ConnectionString', 'DatabaseName', 'Configuration'],
        methods: [
          'SaveAsync(ISmtpSession, ISmtpMessage, CancellationToken)'
        ],
        events: []
      },
      {
        name: 'MongoDbStorageConfiguration',
        description: 'Configuration for MongoDB storage',
        properties: [
          'CollectionName',
          'GridFsBucketName',
          'UseGridFsForLargeMessages',
          'GridFsThresholdMB',
          'AutoCreateIndexes',
          'EnableTTL',
          'TTLDays',
          'ShardKeyField'
        ],
        methods: ['Validate()', 'ShouldUseGridFS(long sizeInBytes)'],
        events: []
      },
      {
        name: 'RedisMessageStore',
        description: 'Redis in-memory cache storage provider',
        properties: ['ConnectionString', 'Configuration', 'ConnectionMultiplexer'],
        methods: [
          'SaveAsync(ISmtpSession, ISmtpMessage, CancellationToken)'
        ],
        events: []
      },
      {
        name: 'RedisStorageConfiguration',
        description: 'Configuration for Redis storage',
        properties: [
          'DatabaseNumber',
          'KeyPrefix',
          'MessageTTLSeconds',
          'UseChunking',
          'ChunkSizeKB',
          'UseCompression',
          'EnablePubSub',
          'PubSubChannel',
          'UseStreams',
          'StreamName'
        ],
        methods: ['Validate()', 'GetMessageKey(string messageId)', 'GetChunkKey(string messageId, int chunkIndex)'],
        events: []
      },
      {
        name: 'S3MessageStore',
        description: 'Amazon S3 and S3-compatible storage provider',
        properties: ['AccessKeyId', 'SecretAccessKey', 'BucketName', 'Configuration', 'S3Client'],
        methods: [
          'SaveAsync(ISmtpSession, ISmtpMessage, CancellationToken)'
        ],
        events: []
      },
      {
        name: 'S3StorageConfiguration',
        description: 'Configuration for S3 storage',
        properties: [
          'Region',
          'ServiceUrl',
          'KeyPrefix',
          'StorageClass',
          'EnableServerSideEncryption',
          'KmsKeyId',
          'EnableVersioning',
          'UseTransferAcceleration',
          'TransitionToIADays',
          'TransitionToGlacierDays',
          'ForcePathStyle'
        ],
        methods: ['Validate()', 'GetObjectKey(string messageId)', 'GetS3Config()'],
        events: []
      },
      {
        name: 'AzureBlobMessageStore',
        description: 'Azure Blob Storage provider with Azure AD support',
        properties: ['ConnectionString', 'Configuration', 'BlobServiceClient', 'ContainerClient'],
        methods: [
          'SaveAsync(ISmtpSession, ISmtpMessage, CancellationToken)'
        ],
        events: []
      },
      {
        name: 'AzureBlobStorageConfiguration',
        description: 'Configuration for Azure Blob storage',
        properties: [
          'ContainerName',
          'StorageAccountName',
          'UseAzureAdAuthentication',
          'AccessTier',
          'EnableSoftDelete',
          'SoftDeleteRetentionDays',
          'EnableVersioning',
          'UseHierarchicalNamespace',
          'BlobPrefix'
        ],
        methods: ['Validate()', 'GetBlobName(string messageId)', 'GetServiceClient()'],
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
    namespace: 'Zetian.Extensions',
    items: [
      {
        name: 'SmtpServerExtensions',
        description: 'Extension methods for SMTP server',
        properties: [],
        methods: [
          'AddRateLimiting(IRateLimiter)',
          'AddRateLimiting(RateLimitConfiguration)',
          'AddMessageFilter(Func<ISmtpMessage, bool>)',
          'AddSpamFilter(string[] blacklistedDomains)',
          'AddSizeFilter(long maxSizeBytes)',
          'SaveMessagesToDirectory(string directory)',
          'LogMessages(ILogger logger)',
          'ForwardMessages(IMessageForwarder forwarder)',
          'AddRecipientValidation(Func<string, Task<bool>> validator)',
          'AddAllowedDomains(params string[] domains)',
          'AddStatistics(IStatisticsCollector collector)'
        ],
        events: []
      },
      {
        name: 'SmtpServerBuilderExtensions',
        description: 'Extension methods for SMTP server builder',
        properties: [],
        methods: [
          'WithRecipientDomainWhitelist(params string[] domains)',
          'WithRecipientDomainBlacklist(params string[] domains)'
        ],
        events: []
      },
      {
        name: 'StorageBuilderExtensions',
        description: 'Extension methods for storage providers',
        properties: [],
        methods: [
          'WithSqlServerStorage(string connectionString)',
          'WithSqlServerStorage(string connectionString, Action<SqlServerStorageConfiguration> configure)',
          'WithPostgreSqlStorage(string connectionString)',
          'WithPostgreSqlStorage(string connectionString, Action<PostgreSqlStorageConfiguration> configure)',
          'WithMongoDbStorage(string connectionString, string databaseName)',
          'WithMongoDbStorage(string connectionString, string databaseName, Action<MongoDbStorageConfiguration> configure)',
          'WithRedisStorage(string connectionString)',
          'WithRedisStorage(string connectionString, Action<RedisStorageConfiguration> configure)',
          'WithS3Storage(string accessKeyId, string secretAccessKey, string bucketName)',
          'WithS3Storage(string accessKeyId, string secretAccessKey, string bucketName, Action<S3StorageConfiguration> configure)',
          'WithS3CompatibleStorage(string serviceUrl, string accessKeyId, string secretAccessKey, string bucketName)',
          'WithS3CompatibleStorage(string serviceUrl, string accessKeyId, string secretAccessKey, string bucketName, Action<S3StorageConfiguration> configure)',
          'WithAzureBlobStorage(string connectionString)',
          'WithAzureBlobStorage(string connectionString, Action<AzureBlobStorageConfiguration> configure)',
          'WithAzureBlobStorageAD(string storageAccountName)',
          'WithAzureBlobStorageAD(string storageAccountName, Action<AzureBlobStorageConfiguration> configure)'
        ],
        events: []
      }
    ]
  },
  {
    title: 'Rate Limiting',
    icon: Gauge,
    namespace: 'Zetian.Models & Zetian.RateLimiting & Zetian.Abstractions',
    items: [
      {
        name: 'RateLimitConfiguration',
        description: 'Rate limiting configuration (Zetian.Models)',
        properties: ['MaxRequests', 'Window', 'UseSlidingWindow'],
        methods: [
          'PerMinute(int maxRequests)',
          'PerHour(int maxRequests)',
          'PerDay(int maxRequests)',
          'PerCustom(int maxRequests, TimeSpan window)'
        ],
        events: []
      },
      {
        name: 'IRateLimiter',
        description: 'Rate limiting interface (Zetian.Abstractions)',
        properties: [],
        methods: [
          'IsAllowedAsync(string key)',
          'IsAllowedAsync(IPAddress address)',
          'RecordRequestAsync(string key)',
          'RecordRequestAsync(IPAddress address)',
          'ResetAsync(string key)',
          'GetRemainingAsync(string key)'
        ],
        events: []
      },
      {
        name: 'InMemoryRateLimiter',
        description: 'In-memory implementation of rate limiter (Zetian.RateLimiting)',
        properties: ['Configuration'],
        methods: [
          'IsAllowedAsync(string key)',
          'IsAllowedAsync(IPAddress address)',
          'RecordRequestAsync(string key)',
          'RecordRequestAsync(IPAddress address)',
          'ResetAsync(string key)',
          'GetRemainingAsync(string key)',
          'CleanupExpiredWindows()',
          'Dispose()'
        ],
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
      },
      {
        name: 'PartitionInterval',
        description: 'PostgreSQL table partition interval for storage',
        properties: [],
        methods: [],
        events: [],
        values: ['Daily', 'Weekly', 'Monthly', 'Yearly']
      },
      {
        name: 'BlobNamingFormat',
        description: 'Azure Blob Storage blob naming format options',
        properties: [],
        methods: [],
        events: [],
        values: ['Flat', 'DateHierarchy', 'YearMonth', 'DomainBased']
      },
      {
        name: 'BlobAccessTier',
        description: 'Azure Blob Storage access tier for cost optimization',
        properties: [],
        methods: [],
        events: [],
        values: ['Hot', 'Cool', 'Archive']
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
          'StartWithHealthCheckAsync(port, configureHealthChecks, ct)',
          'StartWithHealthCheckAsync(hostname, port, ct)',
          'StartWithHealthCheckAsync(hostname, port, configureHealthChecks, ct)',
          'StartWithHealthCheckAsync(IPAddress, port, ct)',
          'StartWithHealthCheckAsync(IPAddress, port, configureHealthChecks, ct)',
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
  },
  {
    title: 'Relay Extension',
    icon: Mail,
    namespace: 'Zetian.Relay',
    items: [
      {
        name: 'RelayConfiguration',
        description: 'Configuration for relay service',
        properties: [
          'DefaultSmartHost',
          'SmartHosts',
          'UseMxRouting',
          'DomainRouting',
          'MaxRetryCount',
          'MessageLifetime',
          'ConnectionTimeout',
          'QueueProcessingInterval',
          'CleanupInterval',
          'MaxConcurrentDeliveries',
          'EnableBounceMessages',
          'BounceSender',
          'LocalDomains',
          'RelayDomains',
          'RelayNetworks',
          'RequireAuthentication',
          'EnableTls',
          'RequireTls',
          'SslProtocols',
          'DnsServers'
        ],
        methods: [],
        events: []
      },
      {
        name: 'SmartHostConfiguration',
        description: 'Smart host server configuration',
        properties: [
          'Host',
          'Port',
          'Priority',
          'Weight',
          'Credentials',
          'UseTls',
          'UseStartTls',
          'SslProtocols',
          'MaxConnections',
          'ConnectionTimeout'
        ],
        methods: [],
        events: []
      },
      {
        name: 'RelayBuilder',
        description: 'Fluent builder for relay configuration',
        properties: [],
        methods: [
          'WithSmartHost(host, port, username, password)',
          'AddSmartHost(SmartHostConfiguration)',
          'MaxConcurrentDeliveries(int)',
          'MaxRetries(int)',
          'MessageLifetime(TimeSpan)',
          'ConnectionTimeout(TimeSpan)',
          'EnableTls(enable, require)',
          'LocalDomain(string)',
          'AddLocalDomains(params string[])',
          'AddRelayDomains(params string[])',
          'RequireAuthentication(bool)',
          'EnableBounce(enable, senderAddress)',
          'Build()'
        ],
        events: []
      },
      {
        name: 'RelayService',
        description: 'Background service for message relay',
        properties: ['Queue', 'Configuration', 'IsRunning'],
        methods: [
          'StartAsync(CancellationToken)',
          'StopAsync(CancellationToken)',
          'QueueMessageAsync(RelayMessage)',
          'GetStatisticsAsync()'
        ],
        events: []
      },
      {
        name: 'IRelayQueue',
        description: 'Interface for relay queue implementation',
        properties: [],
        methods: [
          'EnqueueAsync(RelayMessage)',
          'DequeueAsync(CancellationToken)',
          'GetAllAsync()',
          'GetByStatusAsync(RelayStatus)',
          'UpdateAsync(RelayMessage)',
          'RemoveAsync(string)',
          'GetStatisticsAsync()',
          'ClearExpiredAsync()'
        ],
        events: []
      },
      {
        name: 'RelayMessage',
        description: 'Message in relay queue',
        properties: [
          'Id',
          'MessageId',
          'From',
          'Recipients',
          'Data',
          'Priority',
          'Status',
          'RetryCount',
          'NextRetryTime',
          'CreatedTime',
          'LastAttemptTime',
          'SmartHost',
          'Error'
        ],
        methods: [],
        events: []
      }
    ]
  },
  {
    title: 'Relay Enums',
    icon: Mail,
    namespace: 'Zetian.Relay.Enums',
    items: [
      {
        name: 'RelayPriority',
        description: 'Message priority levels',
        properties: [],
        methods: [],
        events: [],
        values: ['Urgent (0)', 'High (1)', 'Normal (2)', 'Low (3)']
      },
      {
        name: 'RelayStatus',
        description: 'Relay message status',
        properties: [],
        methods: [],
        events: [],
        values: [
          'Queued',
          'InProgress',
          'Delivered',
          'Failed',
          'Deferred',
          'Expired',
          'Cancelled',
          'PartiallyDelivered'
        ]
      }
    ]
  },
  {
    title: 'AntiSpam Extension',
    icon: Shield,
    namespace: 'Zetian.AntiSpam',
    items: [
      {
        name: 'AntiSpamBuilder',
        description: 'Fluent builder for anti-spam configuration',
        properties: [],
        methods: [
          'EnableSpf(failScore)',
          'EnableDkim(failScore, strictMode)',
          'EnableDmarc(failScore, quarantineScore, enforcePolicy)',
          'EnableRbl(params string[])',
          'EnableBayesian(spamThreshold)',
          'EnableGreylisting(initialDelay)',
          'EnableEmailAuthentication(strictMode, enforcePolicy)',
          'AddChecker(ISpamChecker)',
          'WithDnsClient(IDnsClient)',
          'WithOptions(AntiSpamOptions)',
          'UseAggressive()',
          'UseLenient()',
          'Build()'
        ],
        events: []
      },
      {
        name: 'AntiSpamService',
        description: 'Main anti-spam service',
        properties: ['Checkers', 'Options', 'Statistics'],
        methods: [
          'CheckMessageAsync(ISmtpMessage, ISmtpSession, CancellationToken)',
          'GetStatistics()',
          'ResetStatistics()',
          'EnableChecker(string)',
          'DisableChecker(string)'
        ],
        events: []
      },
      {
        name: 'ISpamChecker',
        description: 'Interface for spam checkers',
        properties: ['Name', 'IsEnabled'],
        methods: ['CheckAsync(ISmtpMessage, ISmtpSession, CancellationToken)'],
        events: []
      },
      {
        name: 'SpfChecker',
        description: 'SPF (Sender Policy Framework) checker',
        properties: ['FailScore', 'DnsClient'],
        methods: ['CheckAsync(ISmtpMessage, ISmtpSession, CancellationToken)'],
        events: []
      },
      {
        name: 'DkimChecker',
        description: 'DKIM signature verification',
        properties: ['FailScore', 'StrictMode', 'DnsClient'],
        methods: ['CheckAsync(ISmtpMessage, ISmtpSession, CancellationToken)'],
        events: []
      },
      {
        name: 'DmarcChecker',
        description: 'DMARC policy enforcement',
        properties: ['FailScore', 'QuarantineScore', 'EnforcePolicy', 'DnsClient'],
        methods: ['CheckAsync(ISmtpMessage, ISmtpSession, CancellationToken)'],
        events: []
      },
      {
        name: 'RblChecker',
        description: 'RBL/DNSBL blacklist checker',
        properties: ['Providers', 'DnsClient'],
        methods: ['CheckAsync(ISmtpMessage, ISmtpSession, CancellationToken)'],
        events: []
      },
      {
        name: 'BayesianSpamFilter',
        description: 'Machine learning-based spam filter',
        properties: ['SpamThreshold', 'MinTokenLength', 'MaxTokenLength'],
        methods: [
          'TrainSpamAsync(string)',
          'TrainHamAsync(string)',
          'ClassifyAsync(string)',
          'GetStatistics()',
          'SaveModelAsync(string)',
          'LoadModelAsync(string)',
          'CheckAsync(ISmtpMessage, ISmtpSession, CancellationToken)'
        ],
        events: []
      },
      {
        name: 'GreylistingChecker',
        description: 'Greylisting implementation',
        properties: ['InitialDelay', 'AutoWhitelistAfter', 'CleanupInterval'],
        methods: [
          'CheckAsync(ISmtpMessage, ISmtpSession, CancellationToken)',
          'Whitelist(string)',
          'RemoveWhitelist(string)',
          'ClearExpired()'
        ],
        events: []
      },
      {
        name: 'SpamCheckResult',
        description: 'Result of spam check',
        properties: ['Score', 'Reason', 'IsSpam', 'Action'],
        methods: [
          'Spam(score, reason)',
          'Clean(score)',
          'TempFail(reason)',
          'Reject(reason)'
        ],
        events: []
      }
    ]
  },
  {
    title: 'AntiSpam Models',
    icon: Shield,
    namespace: 'Zetian.AntiSpam.Models',
    items: [
      {
        name: 'AntiSpamOptions',
        description: 'Anti-spam configuration options',
        properties: [
          'RejectThreshold',
          'TempFailThreshold',
          'RunChecksInParallel',
          'CheckerTimeout',
          'ContinueOnSpamDetection',
          'EnableDetailedLogging'
        ],
        methods: [],
        events: []
      },
      {
        name: 'AntiSpamStatistics',
        description: 'Anti-spam statistics',
        properties: [
          'MessagesChecked',
          'MessagesBlocked',
          'MessagesPassed',
          'MessagesGreylisted',
          'SpfFails',
          'DkimFails',
          'DmarcFails',
          'RblHits',
          'BayesianSpamDetected',
          'AverageScore',
          'CheckerStatistics'
        ],
        methods: [],
        events: []
      },
      {
        name: 'RblProvider',
        description: 'RBL/DNSBL provider configuration',
        properties: ['Name', 'Zone', 'ExpectedResponses', 'Score', 'IsEnabled'],
        methods: [],
        events: []
      }
    ]
  },
  {
    title: 'AntiSpam Enums',
    icon: Shield,
    namespace: 'Zetian.AntiSpam.Enums',
    items: [
      {
        name: 'SpamAction',
        description: 'Action to take for spam',
        properties: [],
        methods: [],
        events: [],
        values: ['Accept', 'TempFail', 'Reject', 'Discard', 'Quarantine']
      },
      {
        name: 'SpfResult',
        description: 'SPF check results',
        properties: [],
        methods: [],
        events: [],
        values: ['Pass', 'Fail', 'SoftFail', 'Neutral', 'None', 'TempError', 'PermError']
      },
      {
        name: 'DkimResult',
        description: 'DKIM check results',
        properties: [],
        methods: [],
        events: [],
        values: ['Pass', 'Fail', 'None', 'Policy', 'Neutral', 'TempError', 'PermError']
      },
      {
        name: 'DmarcResult',
        description: 'DMARC check results',
        properties: [],
        methods: [],
        events: [],
        values: ['Pass', 'Fail', 'None', 'TempError', 'PermError']
      },
      {
        name: 'DmarcPolicy',
        description: 'DMARC policy actions',
        properties: [],
        methods: [],
        events: [],
        values: ['None', 'Quarantine', 'Reject']
      }
    ]
  },
  {
    title: 'Monitoring Extension',
    icon: Activity,
    namespace: 'Zetian.Monitoring',
    items: [
      {
        name: 'MonitoringConfiguration',
        description: 'Configuration for monitoring service',
        properties: [
          'EnablePrometheus',
          'PrometheusPort',
          'PrometheusHost',
          'EnableOpenTelemetry',
          'OpenTelemetryEndpoint',
          'UpdateInterval',
          'EnableDetailedMetrics',
          'EnableCommandMetrics',
          'EnableThroughputMetrics',
          'EnableHistograms',
          'ServiceName',
          'ServiceVersion',
          'CustomLabels',
          'CommandDurationBuckets',
          'MessageSizeBuckets'
        ],
        methods: ['Validate()'],
        events: []
      },
      {
        name: 'MonitoringBuilder',
        description: 'Fluent builder for monitoring configuration',
        properties: [],
        methods: [
          'EnablePrometheus(port)',
          'EnablePrometheus(host, port)',
          'EnableOpenTelemetry(endpoint)',
          'WithServiceName(name)',
          'WithServiceVersion(version)',
          'EnableDetailedMetrics()',
          'EnableCommandMetrics()',
          'EnableThroughputMetrics()',
          'EnableHistograms()',
          'WithUpdateInterval(TimeSpan)',
          'WithLabels(params (string, string)[])',
          'WithCommandDurationBuckets(params double[])',
          'WithMessageSizeBuckets(params double[])',
          'Build()'
        ],
        events: []
      },
      {
        name: 'MetricsCollector',
        description: 'Metrics collection service',
        properties: ['Configuration', 'Statistics'],
        methods: [
          'RecordSession(ISmtpSession)',
          'RecordMessage(ISmtpMessage, bool)',
          'RecordCommand(command, success, durationMs)',
          'RecordAuthentication(success, mechanism)',
          'RecordConnection(accepted)',
          'RecordTlsUpgrade(success)',
          'RecordRejection(reason)',
          'RecordError(type)',
          'RecordBytes(direction, count)',
          'GetStatistics()',
          'ResetStatistics()'
        ],
        events: []
      },
      {
        name: 'ServerStatistics',
        description: 'Comprehensive server statistics',
        properties: [
          'Uptime',
          'TotalSessions',
          'ActiveSessions',
          'TotalMessagesReceived',
          'TotalMessagesDelivered',
          'TotalMessagesRejected',
          'DeliveryRate',
          'RejectionRate',
          'TotalBytesReceived',
          'TotalBytesSent',
          'TotalErrors',
          'ConnectionMetrics',
          'AuthenticationMetrics',
          'CommandMetrics',
          'CurrentThroughput',
          'MemoryUsage',
          'PeakMemoryUsage',
          'LastReset'
        ],
        methods: [],
        events: []
      },
      {
        name: 'PrometheusExporter',
        description: 'Prometheus metrics exporter',
        properties: ['Port', 'Host', 'MetricsUrl', 'IsRunning'],
        methods: [
          'StartAsync(CancellationToken)',
          'StopAsync(CancellationToken)',
          'UpdateMetrics(ServerStatistics)',
          'RegisterCustomMetric(name, help, type)'
        ],
        events: []
      },
      {
        name: 'OpenTelemetryExporter',
        description: 'OpenTelemetry tracing and metrics exporter',
        properties: ['Endpoint', 'ServiceName', 'ServiceVersion'],
        methods: [
          'StartAsync(CancellationToken)',
          'StopAsync(CancellationToken)',
          'ExportMetrics(ServerStatistics)',
          'CreateSpan(operationName)',
          'RecordEvent(name, attributes)'
        ],
        events: []
      }
    ]
  },
  {
    title: 'Monitoring Models',
    icon: Activity,
    namespace: 'Zetian.Monitoring.Models',
    items: [
      {
        name: 'ConnectionMetrics',
        description: 'Connection-related metrics',
        properties: [
          'TotalConnectionsReceived',
          'AcceptedCount',
          'RejectedCount',
          'TlsUpgrades',
          'TlsUsageRate',
          'CurrentConnections',
          'PeakConcurrentConnections',
          'AverageConnectionDuration'
        ],
        methods: [],
        events: []
      },
      {
        name: 'AuthenticationMetrics',
        description: 'Authentication-related metrics',
        properties: [
          'TotalAttempts',
          'SuccessCount',
          'FailureCount',
          'SuccessRate',
          'MechanismBreakdown',
          'UniqueUsers',
          'FailureReasons'
        ],
        methods: [],
        events: []
      },
      {
        name: 'CommandMetrics',
        description: 'SMTP command metrics',
        properties: [
          'Command',
          'TotalCount',
          'SuccessCount',
          'FailureCount',
          'SuccessRate',
          'AverageDurationMs',
          'MinDurationMs',
          'MaxDurationMs',
          'P95DurationMs',
          'P99DurationMs'
        ],
        methods: [],
        events: []
      },
      {
        name: 'ThroughputMetrics',
        description: 'Real-time throughput metrics',
        properties: [
          'MessagesPerSecond',
          'BytesPerSecond',
          'CommandsPerSecond',
          'SessionsPerSecond',
          'AverageMessageSize',
          'PeakMessagesPerSecond',
          'PeakBytesPerSecond'
        ],
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