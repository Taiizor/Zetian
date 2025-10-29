using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Zetian.Abstractions;

namespace Zetian.Configuration
{
    /// <summary>
    /// SMTP server configuration
    /// </summary>
    public class SmtpServerConfiguration
    {
        /// <summary>
        /// Initializes a new instance of SmtpServerConfiguration
        /// </summary>
        public SmtpServerConfiguration()
        {
            Port = 25;
            IpAddress = IPAddress.Any;
            ServerName = "Zetian SMTP Server";
            MaxMessageSize = 10 * 1024 * 1024; // 10 MB
            MaxRecipients = 100;
            MaxConnections = 100;
            MaxConnectionsPerIp = 100;
            ConnectionTimeout = TimeSpan.FromMinutes(5);
            CommandTimeout = TimeSpan.FromMinutes(1);
            DataTimeout = TimeSpan.FromMinutes(3);
            MaxRetryCount = 3;
            EnablePipelining = true;
            Enable8BitMime = true;
            EnableBinaryMime = false;
            EnableChunking = false;
            EnableSmtpUtf8 = true;
            EnableSizeExtension = true;
            RequireAuthentication = false;
            RequireSecureConnection = false;
            AllowPlainTextAuthentication = false;
            SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
            AuthenticationMechanisms = ["PLAIN", "LOGIN"];
            Banner = null;
            Greeting = null;
        }

        /// <summary>
        /// Gets or sets the port to listen on
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Gets or sets the IP address to bind to
        /// </summary>
        public IPAddress IpAddress { get; set; }

        /// <summary>
        /// Gets or sets the server name
        /// </summary>
        public string ServerName { get; set; }

        /// <summary>
        /// Gets or sets the maximum message size in bytes
        /// </summary>
        public long MaxMessageSize { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of recipients per message
        /// </summary>
        public int MaxRecipients { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of concurrent connections
        /// </summary>
        public int MaxConnections { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of connections per IP
        /// </summary>
        public int MaxConnectionsPerIp { get; set; }

        /// <summary>
        /// Gets or sets the connection timeout
        /// </summary>
        public TimeSpan ConnectionTimeout { get; set; }

        /// <summary>
        /// Gets or sets the command timeout
        /// </summary>
        public TimeSpan CommandTimeout { get; set; }

        /// <summary>
        /// Gets or sets the data transfer timeout
        /// </summary>
        public TimeSpan DataTimeout { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of retries before quitting the session
        /// </summary>
        public int MaxRetryCount { get; set; }

        /// <summary>
        /// Gets or sets whether pipelining is enabled
        /// </summary>
        public bool EnablePipelining { get; set; }

        /// <summary>
        /// Gets or sets whether 8BITMIME is enabled
        /// </summary>
        public bool Enable8BitMime { get; set; }

        /// <summary>
        /// Gets or sets whether BINARYMIME is enabled
        /// </summary>
        public bool EnableBinaryMime { get; set; }

        /// <summary>
        /// Gets or sets whether CHUNKING is enabled
        /// </summary>
        public bool EnableChunking { get; set; }

        /// <summary>
        /// Gets or sets whether SIZE extension is enabled
        /// </summary>
        public bool EnableSizeExtension { get; set; }

        /// <summary>
        /// Gets or sets whether authentication is required
        /// </summary>
        public bool RequireAuthentication { get; set; }

        /// <summary>
        /// Gets or sets whether a secure connection is required
        /// </summary>
        public bool RequireSecureConnection { get; set; }

        /// <summary>
        /// Gets or sets whether plain text authentication is allowed over non-secure connections
        /// </summary>
        public bool AllowPlainTextAuthentication { get; set; }

        /// <summary>
        /// Gets or sets the SSL/TLS certificate
        /// </summary>
        public X509Certificate2? Certificate { get; set; }

        /// <summary>
        /// Gets or sets the SSL protocols to use
        /// </summary>
        public SslProtocols SslProtocols { get; set; }

        /// <summary>
        /// Gets or sets the authentication mechanisms to support
        /// </summary>
        public IList<string> AuthenticationMechanisms { get; set; }

        /// <summary>
        /// Gets or sets the custom banner message
        /// </summary>
        public string? Banner { get; set; }

        /// <summary>
        /// Gets or sets the custom greeting message
        /// </summary>
        public string? Greeting { get; set; }

        /// <summary>
        /// Gets or sets the logger factory
        /// </summary>
        public ILoggerFactory? LoggerFactory { get; set; }

        /// <summary>
        /// Gets or sets whether to enable verbose logging
        /// </summary>
        public bool EnableVerboseLogging { get; set; }

        /// <summary>
        /// Gets or sets the read buffer size
        /// </summary>
        public int ReadBufferSize { get; set; } = 4096;

        /// <summary>
        /// Gets or sets the write buffer size
        /// </summary>
        public int WriteBufferSize { get; set; } = 4096;

        /// <summary>
        /// Gets or sets whether to use Nagle's algorithm
        /// </summary>
        public bool UseNagleAlgorithm { get; set; } = false;

        /// <summary>
        /// Gets or sets whether SMTPUTF8 is enabled
        /// </summary>
        public bool EnableSmtpUtf8 { get; set; } = true;

        /// <summary>
        /// Gets or sets the message store for saving messages
        /// </summary>
        public IMessageStore? MessageStore { get; set; }

        /// <summary>
        /// Gets or sets the mailbox filter for accepting/rejecting senders and recipients
        /// </summary>
        public IMailboxFilter? MailboxFilter { get; set; }

        /// <summary>
        /// Gets or sets additional properties for extensions
        /// </summary>
        public Dictionary<string, object> Properties { get; } = [];

        /// <summary>
        /// Validates the configuration
        /// </summary>
        public void Validate()
        {
            if (Port is < 1 or > 65535)
            {
                throw new ArgumentException("Port must be between 1 and 65535");
            }

            if (MaxMessageSize < 1)
            {
                throw new ArgumentException("MaxMessageSize must be greater than 0");
            }

            if (MaxRecipients < 1)
            {
                throw new ArgumentException("MaxRecipients must be greater than 0");
            }

            if (MaxConnections < 1)
            {
                throw new ArgumentException("MaxConnections must be greater than 0");
            }

            if (MaxConnectionsPerIp < 1)
            {
                throw new ArgumentException("MaxConnectionsPerIp must be greater than 0");
            }

            if (ConnectionTimeout <= TimeSpan.Zero)
            {
                throw new ArgumentException("ConnectionTimeout must be greater than zero");
            }

            if (CommandTimeout <= TimeSpan.Zero)
            {
                throw new ArgumentException("CommandTimeout must be greater than zero");
            }

            if (DataTimeout <= TimeSpan.Zero)
            {
                throw new ArgumentException("DataTimeout must be greater than zero");
            }

            if (MaxRetryCount < 0)
            {
                throw new ArgumentException("MaxRetryCount cannot be negative");
            }

            if (RequireSecureConnection && Certificate == null)
            {
                throw new ArgumentException("Certificate is required when RequireSecureConnection is true");
            }

            if (!RequireSecureConnection && !AllowPlainTextAuthentication && RequireAuthentication)
            {
                throw new ArgumentException("Plain text authentication must be allowed when not requiring secure connection with authentication");
            }
        }
    }
}