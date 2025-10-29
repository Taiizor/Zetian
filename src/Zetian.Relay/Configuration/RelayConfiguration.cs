using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Authentication;

namespace Zetian.Relay.Configuration
{
    /// <summary>
    /// Configuration for SMTP relay service
    /// </summary>
    public class RelayConfiguration
    {
        /// <summary>
        /// Gets or sets whether relay is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the default smart host configuration
        /// </summary>
        public SmartHostConfiguration? DefaultSmartHost { get; set; }

        /// <summary>
        /// Gets or sets additional smart hosts for failover/load balancing
        /// </summary>
        public List<SmartHostConfiguration> SmartHosts { get; set; } = [];

        /// <summary>
        /// Gets or sets the maximum number of concurrent deliveries
        /// </summary>
        public int MaxConcurrentDeliveries { get; set; } = 10;

        /// <summary>
        /// Gets or sets the maximum retry count for failed deliveries
        /// </summary>
        public int MaxRetryCount { get; set; } = 10;

        /// <summary>
        /// Gets or sets the message lifetime before expiration
        /// </summary>
        public TimeSpan MessageLifetime { get; set; } = TimeSpan.FromDays(4);

        /// <summary>
        /// Gets or sets the connection timeout for SMTP clients
        /// </summary>
        public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets the interval for processing the queue
        /// </summary>
        public TimeSpan QueueProcessingInterval { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets the interval for cleaning expired messages
        /// </summary>
        public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);

        /// <summary>
        /// Gets or sets whether to use DNS MX records for routing
        /// </summary>
        public bool UseMxRouting { get; set; } = true;

        /// <summary>
        /// Gets or sets the DNS servers to use for MX lookups
        /// </summary>
        public List<IPAddress> DnsServers { get; set; } = [];

        /// <summary>
        /// Gets or sets whether to enable TLS for outbound connections
        /// </summary>
        public bool EnableTls { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to require TLS for outbound connections
        /// </summary>
        public bool RequireTls { get; set; } = false;

        /// <summary>
        /// Gets or sets the SSL protocols to use
        /// </summary>
        public SslProtocols SslProtocols { get; set; } = SslProtocols.Tls12 | SslProtocols.Tls13;

        /// <summary>
        /// Gets or sets whether to validate server certificates
        /// </summary>
        public bool ValidateServerCertificate { get; set; } = true;

        /// <summary>
        /// Gets or sets the local domain name for HELO/EHLO
        /// </summary>
        public string? LocalDomain { get; set; }

        /// <summary>
        /// Gets or sets domains that should be delivered locally (not relayed)
        /// </summary>
        public HashSet<string> LocalDomains { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets or sets domains that should always be relayed
        /// </summary>
        public HashSet<string> RelayDomains { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets or sets IP addresses allowed to relay without authentication
        /// </summary>
        public HashSet<IPAddress> RelayNetworks { get; set; } = [];

        /// <summary>
        /// Gets or sets whether to require authentication for relay.
        /// When true (default), only authenticated sessions can relay mail.
        /// Set to false for open relay (NOT RECOMMENDED for production).
        /// </summary>
        public bool RequireAuthentication { get; set; } = true;

        /// <summary>
        /// Gets or sets routing rules for specific domains
        /// </summary>
        public Dictionary<string, SmartHostConfiguration> DomainRouting { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets or sets whether to enable bounce messages
        /// </summary>
        public bool EnableBounceMessages { get; set; } = true;

        /// <summary>
        /// Gets or sets the bounce message sender address
        /// </summary>
        public string BounceSender { get; set; } = "<>";

        /// <summary>
        /// Gets or sets whether to enable delivery status notifications
        /// </summary>
        public bool EnableDsn { get; set; } = true;

        /// <summary>
        /// Validates the configuration
        /// </summary>
        public void Validate()
        {
            if (MaxConcurrentDeliveries < 1)
            {
                throw new ArgumentException("MaxConcurrentDeliveries must be at least 1");
            }

            if (MaxRetryCount < 0)
            {
                throw new ArgumentException("MaxRetryCount cannot be negative");
            }

            if (MessageLifetime <= TimeSpan.Zero)
            {
                throw new ArgumentException("MessageLifetime must be positive");
            }

            if (ConnectionTimeout <= TimeSpan.Zero)
            {
                throw new ArgumentException("ConnectionTimeout must be positive");
            }

            if (QueueProcessingInterval <= TimeSpan.Zero)
            {
                throw new ArgumentException("QueueProcessingInterval must be positive");
            }

            if (CleanupInterval <= TimeSpan.Zero)
            {
                throw new ArgumentException("CleanupInterval must be positive");
            }

            // Validate smart hosts
            DefaultSmartHost?.Validate();
            foreach (SmartHostConfiguration smartHost in SmartHosts)
            {
                smartHost.Validate();
            }

            foreach (SmartHostConfiguration routing in DomainRouting.Values)
            {
                routing.Validate();
            }
        }
    }

    /// <summary>
    /// Configuration for a smart host
    /// </summary>
    public class SmartHostConfiguration
    {
        /// <summary>
        /// Gets or sets the hostname
        /// </summary>
        public string Host { get; set; } = "localhost";

        /// <summary>
        /// Gets or sets the port
        /// </summary>
        public int Port { get; set; } = 25;

        /// <summary>
        /// Gets or sets whether to use SSL/TLS
        /// </summary>
        public bool UseTls { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to use STARTTLS
        /// </summary>
        public bool UseStartTls { get; set; } = true;

        /// <summary>
        /// Gets or sets the authentication credentials
        /// </summary>
        public NetworkCredential? Credentials { get; set; }

        /// <summary>
        /// Gets or sets the priority (lower values = higher priority)
        /// </summary>
        public int Priority { get; set; } = 10;

        /// <summary>
        /// Gets or sets the weight for load balancing
        /// </summary>
        public int Weight { get; set; } = 100;

        /// <summary>
        /// Gets or sets whether this smart host is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum messages per connection
        /// </summary>
        public int MaxMessagesPerConnection { get; set; } = 100;

        /// <summary>
        /// Gets or sets the maximum connections to this host
        /// </summary>
        public int MaxConnections { get; set; } = 5;

        /// <summary>
        /// Gets or sets the connection timeout
        /// </summary>
        public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Validates the configuration
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Host))
            {
                throw new ArgumentException("Host cannot be empty");
            }

            if (Port is < 1 or > 65535)
            {
                throw new ArgumentException("Port must be between 1 and 65535");
            }

            if (Priority < 0)
            {
                throw new ArgumentException("Priority cannot be negative");
            }

            if (Weight < 1)
            {
                throw new ArgumentException("Weight must be at least 1");
            }

            if (MaxMessagesPerConnection < 1)
            {
                throw new ArgumentException("MaxMessagesPerConnection must be at least 1");
            }

            if (MaxConnections < 1)
            {
                throw new ArgumentException("MaxConnections must be at least 1");
            }

            if (ConnectionTimeout <= TimeSpan.Zero)
            {
                throw new ArgumentException("ConnectionTimeout must be positive");
            }
        }
    }
}