'use client';

import { useState, useEffect, useRef, useCallback } from 'react';
import { useRouter } from 'next/navigation';
import Fuse from 'fuse.js';
import { 
  Search as SearchIcon, 
  X, 
  FileText, 
  Code2, 
  Hash, 
  Clock, 
  Star,
  Settings,
  Package,
  ChevronRight
} from 'lucide-react';

interface SearchItem {
  title: string;
  description: string;
  path: string;
  category: string;
  tags?: string[];
  content?: string;
  code?: string;
  popular?: boolean;
}

const searchData: SearchItem[] = [
  // Main Pages
  { title: 'Home', description: 'Modern and scalable SMTP server library for .NET', path: '/', category: 'Main', tags: ['home', 'start', 'overview'] },
  { title: 'Documentation', description: 'Complete documentation and guides', path: '/docs', category: 'Main', tags: ['docs', 'guides', 'manual'] },
  { title: 'API Reference', description: 'Complete API documentation', path: '/api', category: 'Main', tags: ['api', 'reference', 'classes'] },
  { title: 'Examples', description: 'Code examples and use cases', path: '/examples', category: 'Main', tags: ['examples', 'code', 'samples'] },
  { title: 'Changelog', description: 'Version history and release notes', path: '/changelog', category: 'Main', tags: ['changelog', 'versions', 'releases', 'updates', 'history'] },
  
  // Documentation
  { 
    title: 'Getting Started', 
    description: 'Quick setup guide - install NuGet package, create basic server, start receiving emails', 
    path: '/docs/getting-started', 
    category: 'Documentation',
    tags: ['install', 'setup', 'quickstart', 'nuget'],
    popular: true,
    code: 'dotnet add package Zetian'
  },
  { 
    title: 'Configuration', 
    description: 'Server settings - ports, certificates, timeouts, buffer sizes, connection limits', 
    path: '/docs/configuration', 
    category: 'Documentation',
    tags: ['config', 'settings', 'ports', 'ssl', 'tls', 'certificate']
  },
  { 
    title: 'Authentication', 
    description: 'PLAIN, LOGIN, custom authentication handlers, RequireAuthentication, AllowPlainTextAuthentication', 
    path: '/docs/authentication', 
    category: 'Documentation',
    tags: ['auth', 'security', 'login', 'plain', 'password'],
    popular: true
  },
  { 
    title: 'Message Processing', 
    description: 'MessageReceived event, filtering, spam detection, forwarding, storage', 
    path: '/docs/message-processing', 
    category: 'Documentation',
    tags: ['message', 'email', 'processing', 'filter', 'spam']
  },
  { 
    title: 'Extensions', 
    description: 'Rate limiting, spam filters, custom storage, statistics, domain validation', 
    path: '/docs/extensions', 
    category: 'Documentation',
    tags: ['extensions', 'plugins', 'rate-limit', 'spam-filter']
  },
  { 
    title: 'Monitoring Extension', 
    description: 'Real-time metrics - Prometheus exporter, OpenTelemetry, server statistics, Grafana dashboards', 
    path: '/docs/monitoring', 
    category: 'Documentation',
    tags: ['monitoring', 'metrics', 'prometheus', 'opentelemetry', 'grafana', 'observability', 'statistics'],
    popular: true,
    code: 'server.EnableMonitoring().EnablePrometheus(9090)'
  },
  { 
    title: 'Clustering Extension', 
    description: 'High availability, load balancing, state replication, leader election, multi-region support', 
    path: '/docs/clustering', 
    category: 'Documentation',
    tags: ['clustering', 'high-availability', 'load-balancing', 'distributed', 'failover', 'replication', 'leader-election'],
    popular: true,
    code: 'await server.EnableClusteringAsync(options => options.NodeId = "node-1")'
  },
  { 
    title: 'Relay Extension', 
    description: 'SMTP relay and proxy - Smart host support, queue management, load balancing, failover, MX routing', 
    path: '/docs/relay', 
    category: 'Documentation',
    tags: ['relay', 'proxy', 'smart-host', 'queue', 'mx', 'failover', 'load-balance'],
    popular: true,
    code: '.EnableRelay(config => config.DefaultSmartHost = ...)'
  },
  { 
    title: 'AntiSpam Extension', 
    description: 'Advanced spam protection - SPF/DKIM/DMARC, RBL/DNSBL, Bayesian filtering, Greylisting', 
    path: '/docs/anti-spam', 
    category: 'Documentation',
    tags: ['antispam', 'spam', 'spf', 'dkim', 'dmarc', 'rbl', 'dnsbl', 'bayesian', 'greylist'],
    popular: true,
    code: 'server.AddAntiSpam(builder => builder.EnableSpf())'
  },
  
  // Storage Providers Documentation
  { 
    title: 'Storage Providers', 
    description: 'Multiple storage backends - SQL Server, MongoDB, Redis, S3, Azure Blob, PostgreSQL', 
    path: '/docs/storage', 
    category: 'Documentation',
    tags: ['storage', 'database', 'providers', 'backend'],
    popular: true
  },
  { 
    title: 'SQL Server Storage', 
    description: 'Enterprise SQL Server and Azure SQL Database - ACID compliance, auto table creation, compression, full-text search', 
    path: '/docs/storage/sql-server', 
    category: 'Documentation',
    tags: ['storage', 'sql', 'sqlserver', 'database', 'azure-sql'],
    code: '.WithSqlServerStorage("Server=localhost;Database=SmtpDb;...")'
  },
  { 
    title: 'PostgreSQL Storage', 
    description: 'PostgreSQL with JSONB - table partitioning, GIN indexing, advanced queries, time-based retention', 
    path: '/docs/storage/postgresql', 
    category: 'Documentation',
    tags: ['storage', 'postgresql', 'postgres', 'jsonb', 'partitioning'],
    code: '.WithPostgreSqlStorage("Host=localhost;Database=smtp_db;...")'
  },
  { 
    title: 'MongoDB Storage', 
    description: 'NoSQL MongoDB with GridFS - large attachments, TTL indexes, sharding support, flexible schema', 
    path: '/docs/storage/mongodb', 
    category: 'Documentation',
    tags: ['storage', 'mongodb', 'nosql', 'gridfs', 'ttl'],
    code: '.WithMongoDbStorage("mongodb://localhost:27017", "smtp_server")'
  },
  { 
    title: 'Redis Storage', 
    description: 'High-performance Redis cache - in-memory storage, auto-chunking, Pub/Sub, Redis Streams', 
    path: '/docs/storage/redis', 
    category: 'Documentation',
    tags: ['storage', 'redis', 'cache', 'memory', 'pub-sub'],
    code: '.WithRedisStorage("localhost:6379")'
  },
  { 
    title: 'Amazon S3 Storage', 
    description: 'S3 and S3-compatible services - MinIO, Wasabi, KMS encryption, lifecycle rules, cost optimization', 
    path: '/docs/storage/amazon-s3', 
    category: 'Documentation',
    tags: ['storage', 's3', 'aws', 'amazon', 'cloud', 'minio'],
    code: '.WithS3Storage(accessKeyId, secretAccessKey, bucketName)'
  },
  { 
    title: 'Azure Blob Storage', 
    description: 'Azure cloud storage - Azure AD auth, access tiers (Hot/Cool/Archive), soft delete, lifecycle policies', 
    path: '/docs/storage/azure-blob', 
    category: 'Documentation',
    tags: ['storage', 'azure', 'blob', 'cloud', 'microsoft'],
    code: '.WithAzureBlobStorage("DefaultEndpointsProtocol=https;...")'
  },
  
  // Core Classes
  { 
    title: 'SmtpServer', 
    description: 'Main server class - StartAsync(), StopAsync(), MessageReceived, SessionCreated, SessionCompleted, ErrorOccurred events', 
    path: '/api#core-classes', 
    category: 'API',
    tags: ['server', 'main', 'start', 'stop', 'events'],
    popular: true,
    code: 'var server = new SmtpServerBuilder().Port(25).Build();'
  },
  { 
    title: 'SmtpServerBuilder', 
    description: 'Fluent builder - Port(), ServerName(), Certificate(), CertificateFromPfx(), CertificateFromPem(), CertificateFromCer(), RequireAuthentication(), WithFileMessageStore(), Build()', 
    path: '/api#core-classes', 
    category: 'API',
    tags: ['builder', 'fluent', 'configuration', 'certificate'],
    code: '.Port(587).RequireAuthentication().CertificateFromPfx("cert.pfx", "password")'
  },
  { 
    title: 'SmtpServerConfiguration', 
    description: 'All server configuration properties - Port, MaxMessageSize, MaxConnections, Timeouts, Buffers', 
    path: '/api#core-classes', 
    category: 'API',
    tags: ['configuration', 'settings']
  },
  
  // Interfaces
  { 
    title: 'ISmtpMessage', 
    description: 'Message interface - Id, From, Recipients, Subject, TextBody, HtmlBody, GetRawData(), SaveToFileAsync()', 
    path: '/api#core-interfaces', 
    category: 'API',
    tags: ['message', 'interface', 'email'],
    code: 'e.Message.From?.Address; e.Message.SaveToFileAsync(path);'
  },
  { 
    title: 'ISmtpSession', 
    description: 'Session interface - Id, RemoteEndPoint, IsAuthenticated, AuthenticatedIdentity, MessageCount', 
    path: '/api#core-interfaces', 
    category: 'API',
    tags: ['session', 'interface', 'connection'],
    code: 'if (e.Session.IsAuthenticated) { ... }'
  },
  { 
    title: 'IMessageStore', 
    description: 'Storage interface - SaveAsync(session, message, ct) for custom message storage implementations', 
    path: '/api#core-interfaces', 
    category: 'API',
    tags: ['storage', 'interface', 'save']
  },
  { 
    title: 'IMailboxFilter', 
    description: 'Filtering interface - CanAcceptFromAsync(), CanDeliverToAsync() for domain/recipient validation', 
    path: '/api#core-interfaces', 
    category: 'API',
    tags: ['filter', 'interface', 'validation']
  },
  { 
    title: 'IAuthenticator', 
    description: 'Custom authentication mechanism interface - AuthenticateAsync() method (Zetian.Abstractions)', 
    path: '/api#authentication', 
    category: 'API',
    tags: ['authenticator', 'interface', 'custom', 'abstractions']
  },
  { 
    title: 'IRateLimiter', 
    description: 'Rate limiting interface - IsAllowedAsync(), RecordRequestAsync(), GetRemainingAsync() (Zetian.Abstractions)', 
    path: '/api#rate-limiting', 
    category: 'API',
    tags: ['ratelimit', 'interface', 'throttle', 'abstractions']
  },
  { 
    title: 'IStatisticsCollector', 
    description: 'Statistics collection interface - RecordSession(), RecordMessage(), RecordError() (Zetian.Abstractions)', 
    path: '/api#core-interfaces', 
    category: 'API',
    tags: ['statistics', 'interface', 'metrics', 'abstractions'],
    code: 'server.AddStatistics(new MyStatisticsCollector())'
  },
  
  // Storage Classes
  { 
    title: 'FileMessageStore', 
    description: 'Built-in file storage - saves messages to disk with optional date folders', 
    path: '/api#storage', 
    category: 'API',
    tags: ['storage', 'file', 'disk'],
    code: '.WithFileMessageStore(@"C:\\smtp_messages", createDateFolders: true)'
  },
  { 
    title: 'DomainMailboxFilter', 
    description: 'Domain-based filtering - whitelist/blacklist for sender and recipient domains', 
    path: '/api#filtering', 
    category: 'API',
    tags: ['filter', 'domain', 'whitelist', 'blacklist'],
    code: '.WithSenderDomainWhitelist("trusted.com")'
  },
  { 
    title: 'CompositeMailboxFilter', 
    description: 'Combine multiple filters - Mode (All/Any), AddFilter(), RemoveFilter()', 
    path: '/api#filtering', 
    category: 'API',
    tags: ['filter', 'composite', 'combine', 'multiple'],
    code: 'new CompositeMailboxFilter(CompositeMode.All)'
  },
  { 
    title: 'AcceptAllMailboxFilter', 
    description: 'Filter that accepts all messages - no validation', 
    path: '/api#filtering', 
    category: 'API',
    tags: ['filter', 'accept', 'all'],
    code: 'AcceptAllMailboxFilter.Instance'
  },
  { 
    title: 'NullMessageStore', 
    description: 'Null storage - discards messages without saving', 
    path: '/api#storage', 
    category: 'API',
    tags: ['storage', 'null', 'discard'],
    code: 'NullMessageStore.Instance'
  },
  { 
    title: 'SqlServerMessageStore', 
    description: 'SQL Server storage implementation - AutoCreateTable, CompressMessageBody, StoreAttachmentsSeparately', 
    path: '/api#storage', 
    category: 'API',
    tags: ['storage', 'sql', 'sqlserver', 'implementation'],
    code: 'new SqlServerMessageStore(connectionString, configuration)'
  },
  { 
    title: 'PostgreSqlMessageStore', 
    description: 'PostgreSQL storage - UseJsonbForHeaders, PartitionInterval, CreateIndexes, table partitioning', 
    path: '/api#storage', 
    category: 'API',
    tags: ['storage', 'postgresql', 'postgres', 'implementation'],
    code: 'new PostgreSqlMessageStore(connectionString, configuration)'
  },
  { 
    title: 'MongoDbMessageStore', 
    description: 'MongoDB storage - UseGridFsForLargeMessages, TTLDays, AutoCreateIndexes, sharding support', 
    path: '/api#storage', 
    category: 'API',
    tags: ['storage', 'mongodb', 'nosql', 'implementation'],
    code: 'new MongoDbMessageStore(connectionString, databaseName, configuration)'
  },
  { 
    title: 'RedisMessageStore', 
    description: 'Redis cache storage - MessageTTLSeconds, UseChunking, KeyPrefix, DatabaseNumber', 
    path: '/api#storage', 
    category: 'API',
    tags: ['storage', 'redis', 'cache', 'implementation'],
    code: 'new RedisMessageStore(connectionString, configuration)'
  },
  { 
    title: 'S3MessageStore', 
    description: 'Amazon S3 storage - Region, KeyPrefix, EnableServerSideEncryption, StorageClass', 
    path: '/api#storage', 
    category: 'API',
    tags: ['storage', 's3', 'aws', 'implementation'],
    code: 'new S3MessageStore(accessKeyId, secretAccessKey, bucketName, configuration)'
  },
  { 
    title: 'AzureBlobMessageStore', 
    description: 'Azure Blob storage - ContainerName, UseAzureAdAuthentication, EnableSoftDelete, AccessTier', 
    path: '/api#storage', 
    category: 'API',
    tags: ['storage', 'azure', 'blob', 'implementation'],
    code: 'new AzureBlobMessageStore(connectionString, configuration)'
  },
  
  // Relay Classes
  { 
    title: 'RelayConfiguration', 
    description: 'Relay configuration - DefaultSmartHost, SmartHosts, UseMxRouting, MaxRetryCount, MessageLifetime', 
    path: '/api#relay', 
    category: 'API',
    tags: ['relay', 'configuration', 'smarthost', 'mx'],
    code: 'new RelayConfiguration { DefaultSmartHost = ... }'
  },
  { 
    title: 'SmartHostConfiguration', 
    description: 'Smart host settings - Host, Port, Priority, Credentials, UseTls, Weight for load balancing', 
    path: '/api#relay', 
    category: 'API',
    tags: ['relay', 'smarthost', 'configuration'],
    code: 'new SmartHostConfiguration { Host = "smtp.office365.com", Port = 587 }'
  },
  { 
    title: 'RelayBuilder', 
    description: 'Fluent relay builder - WithSmartHost(), MaxRetries(), MessageLifetime(), EnableTls()', 
    path: '/api#relay', 
    category: 'API',
    tags: ['relay', 'builder', 'fluent'],
    code: 'new RelayBuilder().WithSmartHost(...).Build()'
  },
  { 
    title: 'IRelayQueue', 
    description: 'Relay queue interface - EnqueueAsync(), DequeueAsync(), GetStatisticsAsync()', 
    path: '/api#relay', 
    category: 'API',
    tags: ['relay', 'queue', 'interface'],
    code: 'await queue.EnqueueAsync(relayMessage)'
  },
  
  // Clustering Classes
  { 
    title: 'IClusterManager', 
    description: 'Cluster manager interface - EnableClusteringAsync(), GetHealthAsync(), GetMetrics(), ReplicateStateAsync()', 
    path: '/api#clustering', 
    category: 'API',
    tags: ['clustering', 'interface', 'manager', 'distributed'],
    code: 'await cluster.ReplicateStateAsync("key", data)'
  },
  { 
    title: 'ClusterOptions', 
    description: 'Clustering configuration - NodeId, ClusterPort, ReplicationFactor, ConsistencyLevel, DiscoveryMethod', 
    path: '/api#clustering', 
    category: 'API',
    tags: ['clustering', 'options', 'configuration'],
    code: 'options.NodeId = "node-1"; options.ReplicationFactor = 3;'
  },
  { 
    title: 'LoadBalancingStrategy', 
    description: 'Load balancing strategies enum - RoundRobin, LeastConnections, WeightedRoundRobin, IpHash', 
    path: '/api#clustering', 
    category: 'API',
    tags: ['clustering', 'load-balancing', 'enum'],
    code: 'LoadBalancingStrategy.LeastConnections'
  },
  
  // AntiSpam Classes
  { 
    title: 'AntiSpamBuilder', 
    description: 'AntiSpam builder - EnableSpf(), EnableRbl(), EnableBayesian(), EnableGreylisting()', 
    path: '/api#antispam', 
    category: 'API',
    tags: ['antispam', 'spam', 'builder'],
    code: 'new AntiSpamBuilder().EnableSpf().EnableBayesian()'
  },
  { 
    title: 'ISpamChecker', 
    description: 'Spam checker interface - CheckAsync() returns SpamCheckResult with score and reason', 
    path: '/api#antispam', 
    category: 'API',
    tags: ['antispam', 'spam', 'interface', 'checker'],
    code: 'public class CustomChecker : ISpamChecker'
  },
  { 
    title: 'SpfChecker', 
    description: 'SPF validation - CheckAsync() verifies sender policy framework records', 
    path: '/api#antispam', 
    category: 'API',
    tags: ['antispam', 'spf', 'validation'],
    code: 'new SpfChecker(failScore: 50)'
  },
  { 
    title: 'BayesianSpamFilter', 
    description: 'Machine learning filter - TrainSpamAsync(), TrainHamAsync(), ClassifyAsync()', 
    path: '/api#antispam', 
    category: 'API',
    tags: ['antispam', 'bayesian', 'machine-learning', 'ml'],
    code: 'await filter.TrainSpamAsync(spamContent)'
  },
  { 
    title: 'GreylistingChecker', 
    description: 'Greylisting - initialDelay, Whitelist(), temporary rejection for unknown senders', 
    path: '/api#antispam', 
    category: 'API',
    tags: ['antispam', 'greylist', 'delay'],
    code: 'new GreylistingChecker(TimeSpan.FromMinutes(5))'
  },
  
  // Extension Methods
  { 
    title: 'AddRateLimiting', 
    description: 'Extension method - adds rate limiting per IP address with configurable limits', 
    path: '/api#extensions', 
    category: 'API',
    tags: ['extension', 'ratelimit', 'throttle'],
    code: 'server.AddRateLimiting(RateLimitConfiguration.PerHour(100))'
  },
  { 
    title: 'AddMessageFilter', 
    description: 'Extension method - adds custom message filtering logic', 
    path: '/api#extensions', 
    category: 'API',
    tags: ['extension', 'filter', 'custom'],
    code: 'server.AddMessageFilter(msg => msg.Size < 10_000_000)'
  },
  { 
    title: 'SaveMessagesToDirectory', 
    description: 'Extension method - automatically saves all received messages to a directory', 
    path: '/api#extensions', 
    category: 'API',
    tags: ['extension', 'save', 'directory'],
    code: 'server.SaveMessagesToDirectory(@"C:\\emails")'
  },
  
  // Examples
  { 
    title: 'Basic Example', 
    description: 'Simple server on port 25 - no auth, accepts all messages, logs to console', 
    path: '/examples#basic', 
    category: 'Examples',
    tags: ['example', 'basic', 'simple'],
    popular: true,
    code: 'var server = new SmtpServerBuilder().Port(25).Build();'
  },
  { 
    title: 'Authenticated Example', 
    description: 'Port 587 with PLAIN/LOGIN auth - AuthenticationHandler, RequireAuthentication', 
    path: '/examples#authenticated', 
    category: 'Examples',
    tags: ['example', 'auth', 'secure', 'password'],
    code: '.RequireAuthentication().AuthenticationHandler(handler)'
  },
  { 
    title: 'Secure Example', 
    description: 'TLS/SSL with STARTTLS - Certificate(), RequireSecureConnection(), port 587', 
    path: '/examples#secure', 
    category: 'Examples',
    tags: ['example', 'secure', 'tls', 'ssl', 'certificate'],
    code: '.Certificate("cert.pfx", "password").RequireSecureConnection()'
  },
  { 
    title: 'Certificate Formats', 
    description: 'PFX, PEM, CER/CRT certificate loading - CertificateFromPfx(), CertificateFromPem(), CertificateFromCer()', 
    path: '/examples#certificate-formats', 
    category: 'Examples',
    tags: ['certificate', 'pfx', 'pem', 'cer', 'crt', 'x509'],
    popular: true,
    code: '.CertificateFromPem("cert.pem", "key.pem")'
  },
  { 
    title: 'Rate Limited Example', 
    description: 'Spam protection - PerMinute/PerHour limits, connection limits per IP', 
    path: '/examples#rate-limited', 
    category: 'Examples',
    tags: ['example', 'ratelimit', 'spam', 'protection'],
    code: '.AddRateLimiting(RateLimitConfiguration.PerMinute(10))'
  },
  { 
    title: 'Custom Processing', 
    description: 'Domain filtering, content validation, spam word detection, size limits', 
    path: '/examples#filtered', 
    category: 'Examples',
    tags: ['example', 'filter', 'custom', 'validation'],
    code: 'if (spamWords.Any(w => message.Subject?.Contains(w)))'
  },
  { 
    title: 'Message Storage', 
    description: 'FileMessageStore, custom IMessageStore implementations, JSON metadata', 
    path: '/examples#storage', 
    category: 'Examples',
    tags: ['example', 'storage', 'save', 'database'],
    code: '.WithFileMessageStore(directory, createDateFolders: true)'
  },
  { 
    title: 'Redis Storage Example', 
    description: 'High-performance Redis caching - compression, expiration, chunking for large messages', 
    path: '/examples#redis-storage', 
    category: 'Examples',
    tags: ['example', 'redis', 'cache', 'storage'],
    code: '.WithRedisStorage("localhost:6379", config => config.MessageTTLSeconds = 1440)'
  },
  { 
    title: 'MongoDB Storage Example', 
    description: 'NoSQL MongoDB with GridFS - large attachments, TTL auto-cleanup, sharding', 
    path: '/examples#mongodb-storage', 
    category: 'Examples',
    tags: ['example', 'mongodb', 'nosql', 'gridfs', 'storage'],
    code: '.WithMongoDbStorage("mongodb://localhost:27017", "smtp_server")'
  },
  { 
    title: 'SQL Server Storage Example', 
    description: 'Enterprise SQL Server - auto table creation, compression, full-text search', 
    path: '/examples#sqlserver-storage', 
    category: 'Examples',
    tags: ['example', 'sql', 'sqlserver', 'database', 'storage'],
    code: '.WithSqlServerStorage("Server=localhost;Database=SmtpDb;...")'
  },
  { 
    title: 'PostgreSQL Storage Example', 
    description: 'Advanced PostgreSQL with JSONB - GIN indexing, table partitioning, flexible queries', 
    path: '/examples#postgresql-storage', 
    category: 'Examples',
    tags: ['example', 'postgresql', 'jsonb', 'storage', 'partitioning'],
    code: '.WithPostgreSqlStorage("Host=localhost;Database=smtp_db;...")'
  },
  { 
    title: 'S3 Storage Example', 
    description: 'Amazon S3 and compatible services - lifecycle management, KMS encryption', 
    path: '/examples#s3-storage', 
    category: 'Examples',
    tags: ['example', 's3', 'aws', 'cloud', 'storage'],
    code: '.WithS3Storage(accessKeyId, secretAccessKey, bucketName)'
  },
  { 
    title: 'Azure Blob Storage Example', 
    description: 'Azure cloud storage - access tiers, soft delete, Azure AD authentication', 
    path: '/examples#azure-blob-storage', 
    category: 'Examples',
    tags: ['example', 'azure', 'blob', 'cloud', 'storage'],
    code: '.WithAzureBlobStorage("DefaultEndpointsProtocol=https;...")'
  },
  { 
    title: 'Multi-Provider Storage Example', 
    description: 'Multiple storage backends with failover - Redis cache + SQL Server + Azure Blob', 
    path: '/examples#multi-storage', 
    category: 'Examples',
    tags: ['example', 'multi', 'failover', 'storage', 'tiered'],
    popular: true,
    code: '.WithRedisStorage().WithSqlServerStorage().WithAzureBlobStorage()'
  },
  { 
    title: 'Relay Example', 
    description: 'SMTP relay with smart host - failover, load balancing, queue management, domain routing', 
    path: '/examples#relay', 
    category: 'Examples',
    tags: ['example', 'relay', 'smarthost', 'failover', 'queue'],
    popular: true,
    code: '.EnableRelay(config => config.DefaultSmartHost = ...)'
  },
  { 
    title: 'AntiSpam Example', 
    description: 'Comprehensive spam protection - SPF/DKIM/DMARC, RBL checking, Bayesian filter, greylisting', 
    path: '/examples#antispam', 
    category: 'Examples',
    tags: ['example', 'antispam', 'spam', 'spf', 'dkim', 'bayesian'],
    popular: true,
    code: 'server.AddAntiSpam(builder => builder.EnableSpf().EnableBayesian())'
  },
  
  // Protocol Classes
  { 
    title: 'SmtpResponse', 
    description: 'SMTP response - Ok, ServiceReady, AuthenticationRequired, AuthenticationSuccessful, etc.', 
    path: '/api#protocol', 
    category: 'API',
    tags: ['protocol', 'response', 'smtp', 'codes'],
    code: 'SmtpResponse.Ok'
  },
  { 
    title: 'SmtpCommand', 
    description: 'SMTP command parser - Parse(), Name, Parameters, IsValid()', 
    path: '/api#protocol', 
    category: 'API',
    tags: ['protocol', 'command', 'smtp', 'parser'],
    code: 'SmtpCommand.Parse("MAIL FROM:<user@example.com>")'
  },
  
  // Event Arguments
  { 
    title: 'MessageEventArgs', 
    description: 'Event args for MessageReceived - Message, Session, Cancel, Response', 
    path: '/api#event-arguments', 
    category: 'API',
    tags: ['event', 'args', 'message'],
    code: 'e.Cancel = true; e.Response = SmtpResponse.TransactionFailed;'
  },
  { 
    title: 'SessionEventArgs', 
    description: 'Event args for session events - Session property', 
    path: '/api#event-arguments', 
    category: 'API',
    tags: ['event', 'args', 'session']
  },
  { 
    title: 'AuthenticationEventArgs', 
    description: 'Event args for authentication - Username, Password, Session, IsAuthenticated', 
    path: '/api#event-arguments', 
    category: 'API',
    tags: ['event', 'args', 'authentication']
  },
  { 
    title: 'ErrorEventArgs', 
    description: 'Event args for errors - Exception, Session', 
    path: '/api#event-arguments', 
    category: 'API',
    tags: ['event', 'args', 'error', 'exception']
  },
  
  // Common Issues & Solutions
  { 
    title: 'Port 25 Access Denied', 
    description: 'Solution: Run as administrator or use port > 1024 for testing', 
    path: '/docs/getting-started#troubleshooting', 
    category: 'Troubleshooting',
    tags: ['error', 'port', 'permission', 'admin']
  },
  { 
    title: 'Authentication Error 538', 
    description: 'Encryption required - use AllowPlainTextAuthentication() for testing without TLS', 
    path: '/docs/authentication#common-errors', 
    category: 'Troubleshooting',
    tags: ['error', 'auth', '538', 'encryption'],
    code: '.AllowPlainTextAuthentication()'
  },
  { 
    title: 'Connection Limit Exceeded', 
    description: 'MaxConnectionsPerIP default is 10 - increase with .MaxConnectionsPerIP(100)', 
    path: '/docs/configuration#connection-limits', 
    category: 'Troubleshooting',
    tags: ['error', 'connection', 'limit'],
    code: '.MaxConnectionsPerIP(100)'
  },
  
  // Health Check
  { 
    title: 'Health Check', 
    description: 'Health monitoring endpoints - /health, /livez, /readyz - server metrics, uptime, memory usage', 
    path: '/docs/health-check', 
    category: 'Documentation',
    tags: ['health', 'monitoring', 'metrics', 'liveness', 'readiness'],
    popular: true,
    code: 'server.EnableHealthCheck(8080)'
  },
  { 
    title: 'HealthCheckService', 
    description: 'HTTP service for health checks - StartAsync(), AddHealthCheck(), AddReadinessCheck()', 
    path: '/api#health-check-core', 
    category: 'API',
    tags: ['health', 'service', 'http', 'monitoring'],
    code: 'healthCheck.AddHealthCheck("database", async (ct) => { ... })'
  },
  { 
    title: 'IHealthCheck', 
    description: 'Health check interface - CheckHealthAsync() for custom health checks', 
    path: '/api#health-check-core', 
    category: 'API',
    tags: ['health', 'interface', 'custom']
  },
  { 
    title: 'HealthCheckResult', 
    description: 'Health check result - Healthy(), Degraded(), Unhealthy() static factory methods', 
    path: '/api#health-check-core', 
    category: 'API',
    tags: ['health', 'result', 'status'],
    code: 'HealthCheckResult.Healthy("Service is running")'
  },
  { 
    title: 'SmtpServerHealthCheck', 
    description: 'Built-in SMTP server health check - monitors uptime, active sessions, memory usage', 
    path: '/api#health-check-core', 
    category: 'API',
    tags: ['health', 'smtp', 'monitoring']
  },
  { 
    title: 'EnableHealthCheck', 
    description: 'Extension method - enables health check endpoint on SMTP server', 
    path: '/api#health-check-extensions', 
    category: 'API',
    tags: ['health', 'extension', 'enable'],
    code: 'server.EnableHealthCheck("0.0.0.0", 8080)'
  },
  { 
    title: 'StartWithHealthCheckAsync', 
    description: 'Extension - starts SMTP server with health check endpoint simultaneously', 
    path: '/api#health-check-extensions', 
    category: 'API',
    tags: ['health', 'extension', 'start'],
    code: 'await server.StartWithHealthCheckAsync(8080, healthService => { ... }, ct)'
  },
  
  // Authentication Updates
  { 
    title: 'AuthenticatorFactory', 
    description: 'Factory for creating authenticators - Create(), SetDefaultHandler(), GetDefaultHandler()', 
    path: '/api#authentication', 
    category: 'API',
    tags: ['authentication', 'factory', 'handler'],
    code: 'AuthenticatorFactory.SetDefaultHandler(myHandler)'
  },
  { 
    title: 'AuthenticationHandler', 
    description: 'Delegate for custom authentication - Task<AuthenticationResult>(username, password)', 
    path: '/api#delegates', 
    category: 'API',
    tags: ['authentication', 'delegate', 'handler'],
    code: 'async (username, password) => AuthenticationResult.Succeed(username)'
  },
  
  // Rate Limiting Updates
  { 
    title: 'InMemoryRateLimiter', 
    description: 'In-memory rate limiter - IsAllowedAsync(), RecordRequestAsync(), CleanupExpiredWindows()', 
    path: '/api#rate-limiting', 
    category: 'API',
    tags: ['ratelimit', 'memory', 'implementation'],
    code: 'new InMemoryRateLimiter(RateLimitConfiguration.PerHour(100))'
  },
  { 
    title: 'RateLimitConfiguration', 
    description: 'Rate limit config - PerMinute(), PerHour(), PerDay(), PerCustom() factory methods', 
    path: '/api#rate-limiting', 
    category: 'API',
    tags: ['ratelimit', 'configuration', 'factory'],
    code: 'RateLimitConfiguration.PerCustom(50, TimeSpan.FromMinutes(30))'
  },
  
  // Extension Method Updates
  { 
    title: 'SmtpServerExtensions', 
    description: 'Extension methods - AddRateLimiting(), AddMessageFilter(), AddSpamFilter(), SaveMessagesToDirectory(), AddStatistics()', 
    path: '/api#extensions', 
    category: 'API',
    tags: ['extension', 'methods', 'server'],
    popular: true
  },
  { 
    title: 'SmtpServerBuilderExtensions', 
    description: 'Builder extensions - WithRecipientDomainWhitelist(), WithRecipientDomainBlacklist()', 
    path: '/api#extensions', 
    category: 'API',
    tags: ['extension', 'builder', 'domain'],
    code: '.WithRecipientDomainWhitelist("mydomain.com")'
  },
  { 
    title: 'Storage Builder Extensions', 
    description: 'Storage provider extensions - WithSqlServerStorage(), WithPostgreSqlStorage(), WithMongoDbStorage(), WithRedisStorage(), WithS3Storage(), WithAzureBlobStorage()', 
    path: '/api#extensions', 
    category: 'API',
    tags: ['extension', 'builder', 'storage', 'providers'],
    popular: true,
    code: '.WithRedisStorage("localhost:6379").WithSqlServerStorage(connectionString)'
  },
  
  // Enum Updates
  { 
    title: 'HealthStatus', 
    description: 'Health status enum - Healthy = 0, Degraded = 1, Unhealthy = 2', 
    path: '/api#health-check-enums', 
    category: 'API',
    tags: ['health', 'enum', 'status'],
    code: 'HealthStatus.Healthy'
  },
  { 
    title: 'CompositeMode', 
    description: 'Composite filter mode - All (AND logic), Any (OR logic)', 
    path: '/api#enums', 
    category: 'API',
    tags: ['enum', 'filter', 'composite'],
    code: 'CompositeMode.All'
  },
  { 
    title: 'SmtpSessionState', 
    description: 'Session states - Connected, AwaitingCommand, ReceivingData, Closing', 
    path: '/api#enums', 
    category: 'API',
    tags: ['enum', 'session', 'state'],
    code: 'SmtpSessionState.AwaitingCommand'
  },
];

// Enhanced Fuse configuration for better search
const fuse = new Fuse(searchData, {
  keys: [
    { name: 'title', weight: 0.4 },
    { name: 'description', weight: 0.3 },
    { name: 'tags', weight: 0.2 },
    { name: 'content', weight: 0.05 },
    { name: 'code', weight: 0.05 }
  ],
  threshold: 0.2,
  includeScore: true,
  includeMatches: true,
  minMatchCharLength: 1,
  shouldSort: true,
  location: 0,
  distance: 100,
});

export function Search() {
  const [isOpen, setIsOpen] = useState(false);
  const [query, setQuery] = useState('');
  const [results, setResults] = useState<SearchItem[]>([]);
  const [selectedIndex, setSelectedIndex] = useState(0);
  const [selectedCategory, setSelectedCategory] = useState<string>('All');
  const [recentSearches, setRecentSearches] = useState<string[]>([]);
  const router = useRouter();
  const inputRef = useRef<HTMLInputElement>(null);
  const modalRef = useRef<HTMLDivElement>(null);
  
  // Load recent searches from localStorage
  useEffect(() => {
    const saved = localStorage.getItem('recentSearches');
    if (saved) {
      setRecentSearches(JSON.parse(saved).slice(0, 5));
    }
  }, []);
  
  // Save search to recent
  const saveToRecent = useCallback((searchTerm: string) => {
    if (!searchTerm.trim()) return;
    const updated = [searchTerm, ...recentSearches.filter(s => s !== searchTerm)].slice(0, 5);
    setRecentSearches(updated);
    localStorage.setItem('recentSearches', JSON.stringify(updated));
  }, [recentSearches]);
  
  // Get popular searches
  const popularSearches = searchData.filter(item => item.popular).slice(0, 6);
  
  // Get categories for filtering
  const categories = ['All', ...Array.from(new Set(searchData.map(item => item.category)))];

  const handleSearch = useCallback((searchQuery: string) => {
    if (searchQuery.trim() === '') {
      setResults([]);
      setSelectedIndex(0);
      return;
    }
    
    let searchResults = fuse.search(searchQuery);
    
    // Filter by category if selected
    if (selectedCategory !== 'All') {
      searchResults = searchResults.filter(r => r.item.category === selectedCategory);
    }
    
    // Sort by score and popularity
    searchResults.sort((a, b) => {
      const scoreA = a.score || 0;
      const scoreB = b.score || 0;
      const popularA = a.item.popular ? -0.1 : 0;
      const popularB = b.item.popular ? -0.1 : 0;
      return (scoreA + popularA) - (scoreB + popularB);
    });
    
    const newResults = searchResults.map(r => r.item).slice(0, 50);
    setResults(newResults);
    setSelectedIndex(0);
  }, [selectedCategory]);

  useEffect(() => {
    handleSearch(query);
  }, [query, handleSearch]);

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      // Cmd/Ctrl + K to open search
      if ((e.metaKey || e.ctrlKey) && e.key === 'k') {
        e.preventDefault();
        setIsOpen(true);
      }
      
      // Escape to close
      if (e.key === 'Escape' && isOpen) {
        setIsOpen(false);
      }
      
      // Arrow navigation
      if (isOpen && results.length > 0) {
        if (e.key === 'ArrowDown') {
          e.preventDefault();
          setSelectedIndex((prev) => (prev + 1) % results.length);
        } else if (e.key === 'ArrowUp') {
          e.preventDefault();
          setSelectedIndex((prev) => (prev - 1 + results.length) % results.length);
        } else if (e.key === 'Enter') {
          e.preventDefault();
          const selected = results[selectedIndex];
          if (selected) {
            router.push(selected.path);
            setIsOpen(false);
            setQuery('');
          }
        }
      }
    };

    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [isOpen, results, selectedIndex, router]);

  useEffect(() => {
    if (isOpen && inputRef.current) {
      inputRef.current.focus();
    } else {
      // Dialog kapandığında state'leri temizle
      setQuery('');
      setResults([]);
      setSelectedIndex(0);
    }
  }, [isOpen]);

  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (modalRef.current && !modalRef.current.contains(e.target as Node)) {
        setIsOpen(false);
      }
    };

    if (isOpen) {
      document.addEventListener('mousedown', handleClickOutside);
    }
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [isOpen]);

  const getCategoryIcon = (category: string) => {
    switch (category) {
      case 'Documentation':
        return <FileText className="h-4 w-4" />;
      case 'API':
        return <Hash className="h-4 w-4" />;
      case 'Examples':
        return <Code2 className="h-4 w-4" />;
      case 'Troubleshooting':
        return <Settings className="h-4 w-4" />;
      case 'Main':
        return <Package className="h-4 w-4" />;
      default:
        return <FileText className="h-4 w-4" />;
    }
  };
  
  const getCategoryColor = (category: string) => {
    switch (category) {
      case 'Documentation':
        return 'text-blue-600 dark:text-blue-400';
      case 'API':
        return 'text-purple-600 dark:text-purple-400';
      case 'Examples':
        return 'text-green-600 dark:text-green-400';
      case 'Troubleshooting':
        return 'text-red-600 dark:text-red-400';
      case 'Main':
        return 'text-gray-600 dark:text-gray-400';
      default:
        return 'text-gray-600 dark:text-gray-400';
    }
  };

  return (
    <>
      {/* Search Button */}
      <button
        onClick={() => setIsOpen(true)}
        className="flex items-center gap-2 px-3 py-1.5 text-sm text-gray-600 dark:text-gray-400 bg-gray-100 dark:bg-gray-800 hover:bg-gray-200 dark:hover:bg-gray-700 rounded-lg transition-colors"
        aria-label="Search (Cmd+K)"
      >
        <SearchIcon className="h-4 w-4" />
        <span className="hidden sm:inline">Search...</span>
        <kbd className="hidden sm:inline-flex items-center gap-1 px-1.5 py-0.5 text-xs text-gray-500 dark:text-gray-400 bg-white dark:bg-gray-900 border border-gray-300 dark:border-gray-700 rounded">
          <span className="text-xs">⌘</span>K
        </kbd>
      </button>

      {/* Search Modal */}
      {isOpen && (
        <div className="fixed inset-0 z-50 flex items-start justify-center pt-16 bg-black/50 backdrop-blur-sm">
          <div
            ref={modalRef}
            className="w-full max-w-3xl bg-white dark:bg-gray-900 rounded-xl shadow-2xl overflow-hidden animate-slide-up"
          >
            {/* Search Input */}
            <div className="flex flex-col gap-3 p-4 border-b border-gray-200 dark:border-gray-800">
              <div className="flex items-center gap-3">
                <SearchIcon className="h-5 w-5 text-gray-400" />
                <input
                  ref={inputRef}
                  type="text"
                  value={query}
                  onChange={(e) => setQuery(e.target.value)}
                  placeholder="Search documentation, API, examples..."
                  className="flex-1 bg-transparent text-gray-900 dark:text-white placeholder-gray-500 dark:placeholder-gray-400 focus:outline-none"
                />
                <button
                  onClick={() => {
                    setIsOpen(false);
                    setQuery('');
                    setSelectedCategory('All');
                  }}
                  className="p-1 hover:bg-gray-100 dark:hover:bg-gray-800 rounded transition-colors"
                >
                  <X className="h-4 w-4 text-gray-400" />
                </button>
              </div>
              
              {/* Category Filter */}
              <div className="flex items-center gap-2 overflow-x-auto pb-1">
                <span className="text-xs text-gray-500 dark:text-gray-400 whitespace-nowrap">Filter:</span>
                {categories.map((cat) => (
                  <button
                    key={cat}
                    onClick={() => {
                      setSelectedCategory(cat);
                      handleSearch(query);
                    }}
                    className={`px-2 py-1 text-xs rounded-md transition-colors whitespace-nowrap ${
                      selectedCategory === cat
                        ? 'bg-blue-100 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400'
                        : 'bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-400 hover:bg-gray-200 dark:hover:bg-gray-700'
                    }`}
                  >
                    {cat}
                  </button>
                ))}
              </div>
            </div>

            {/* Search Results */}
            {results.length > 0 ? (
              <div className="max-h-[60vh] overflow-y-auto p-2">
                {results.map((result, index) => (
                  <button
                    key={`${result.path}-${index}`}
                    onClick={() => {
                      saveToRecent(result.title);
                      router.push(result.path);
                      setIsOpen(false);
                    }}
                    onMouseEnter={() => setSelectedIndex(index)}
                    className={`w-full flex items-start gap-3 p-3 rounded-lg text-left transition-colors ${
                      index === selectedIndex
                        ? 'bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800'
                        : 'hover:bg-gray-50 dark:hover:bg-gray-800/50'
                    }`}
                  >
                    <div className={`mt-0.5 ${getCategoryColor(result.category)}`}>
                      {getCategoryIcon(result.category)}
                    </div>
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2">
                        <div className="font-medium text-gray-900 dark:text-white">
                          {result.title}
                        </div>
                        {result.popular && (
                          <Star className="h-3 w-3 text-yellow-500 fill-yellow-500" />
                        )}
                      </div>
                      <div className="text-sm text-gray-600 dark:text-gray-400 line-clamp-2">
                        {result.description}
                      </div>
                      {result.code && (
                        <div className="mt-1.5 p-1.5 bg-gray-100 dark:bg-gray-800 rounded text-xs font-mono text-gray-700 dark:text-gray-300 truncate">
                          {result.code}
                        </div>
                      )}
                      <div className="flex items-center gap-2 mt-1.5">
                        <div className={`text-xs ${getCategoryColor(result.category)}`}>
                          {result.category}
                        </div>
                        {result.tags && result.tags.length > 0 && (
                          <>
                            <span className="text-gray-300 dark:text-gray-700">•</span>
                            <div className="flex gap-1">
                              {result.tags.slice(0, 3).map((tag) => (
                                <span
                                  key={tag}
                                  className="px-1.5 py-0.5 bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-400 text-xs rounded"
                                >
                                  {tag}
                                </span>
                              ))}
                            </div>
                          </>
                        )}
                      </div>
                    </div>
                    <ChevronRight className="h-4 w-4 text-gray-400 mt-3 flex-shrink-0" />
                  </button>
                ))}
              </div>
            ) : query.trim() !== '' ? (
              <div className="p-8 text-center text-gray-500 dark:text-gray-400">
                <SearchIcon className="h-8 w-8 mx-auto mb-3 opacity-50" />
                <p>No results found for "{query}"</p>
                {selectedCategory !== 'All' && (
                  <p className="text-sm mt-2">
                    in {selectedCategory} category.
                    <button
                      onClick={() => setSelectedCategory('All')}
                      className="text-blue-600 dark:text-blue-400 hover:underline ml-1"
                    >
                      Search all categories
                    </button>
                  </p>
                )}
              </div>
            ) : (
              <div className="p-6">
                {/* Popular Searches */}
                {popularSearches.length > 0 && (
                  <div className="mb-6">
                    <div className="flex items-center gap-2 text-xs text-gray-500 dark:text-gray-400 mb-2">
                      <Star className="h-3 w-3" />
                      <span>Popular</span>
                    </div>
                    <div className="grid grid-cols-2 gap-2">
                      {popularSearches.map((item) => (
                        <button
                          key={item.path}
                          onClick={() => {
                            router.push(item.path);
                            setIsOpen(false);
                          }}
                          className="flex items-center gap-2 p-2 bg-gray-50 dark:bg-gray-800/50 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors text-left"
                        >
                          <div className={getCategoryColor(item.category)}>
                            {getCategoryIcon(item.category)}
                          </div>
                          <div className="min-w-0">
                            <div className="text-sm font-medium text-gray-900 dark:text-white truncate">
                              {item.title}
                            </div>
                            <div className="text-xs text-gray-500 dark:text-gray-400">
                              {item.category}
                            </div>
                          </div>
                        </button>
                      ))}
                    </div>
                  </div>
                )}
                
                {/* Recent Searches */}
                {recentSearches.length > 0 && (
                  <div className="mb-6">
                    <div className="flex items-center gap-2 text-xs text-gray-500 dark:text-gray-400 mb-2">
                      <Clock className="h-3 w-3" />
                      <span>Recent</span>
                    </div>
                    <div className="flex flex-wrap gap-2">
                      {recentSearches.map((term) => (
                        <button
                          key={term}
                          onClick={() => setQuery(term)}
                          className="px-3 py-1 bg-gray-100 dark:bg-gray-800 text-sm text-gray-700 dark:text-gray-300 rounded-full hover:bg-gray-200 dark:hover:bg-gray-700 transition-colors"
                        >
                          {term}
                        </button>
                      ))}
                    </div>
                  </div>
                )}
                
                {/* Keyboard Shortcuts */}
                <div className="text-center pt-4 border-t border-gray-200 dark:border-gray-800">
                  <p className="text-sm text-gray-500 dark:text-gray-400 mb-3">Keyboard shortcuts</p>
                  <div className="flex items-center justify-center gap-6 text-xs">
                    <span className="flex items-center gap-1">
                      <kbd className="px-2 py-1 bg-gray-100 dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded">↑</kbd>
                      <kbd className="px-2 py-1 bg-gray-100 dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded">↓</kbd>
                      Navigate
                    </span>
                    <span className="flex items-center gap-1">
                      <kbd className="px-2 py-1 bg-gray-100 dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded">Enter</kbd>
                      Select
                    </span>
                    <span className="flex items-center gap-1">
                      <kbd className="px-2 py-1 bg-gray-100 dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded">Esc</kbd>
                      Close
                    </span>
                  </div>
                </div>
              </div>
            )}
          </div>
        </div>
      )}
    </>
  );
}