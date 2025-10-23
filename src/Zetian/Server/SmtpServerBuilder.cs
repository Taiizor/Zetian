using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Zetian.Abstractions;
using Zetian.Authentication;
using Zetian.Configuration;
using Zetian.Delegates;
using Zetian.Models;
using Zetian.Storage;

namespace Zetian.Server
{
    /// <summary>
    /// Builder for creating SMTP server instances
    /// </summary>
    public class SmtpServerBuilder
    {
        private readonly SmtpServerConfiguration _configuration;

        /// <summary>
        /// Initializes a new SmtpServerBuilder
        /// </summary>
        public SmtpServerBuilder()
        {
            _configuration = new SmtpServerConfiguration();
        }

        /// <summary>
        /// Sets the port
        /// </summary>
        public SmtpServerBuilder Port(int port)
        {
            _configuration.Port = port;
            return this;
        }

        /// <summary>
        /// Sets the IP address to bind to
        /// </summary>
        public SmtpServerBuilder BindTo(IPAddress ipAddress)
        {
            _configuration.IpAddress = ipAddress ?? throw new ArgumentNullException(nameof(ipAddress));
            return this;
        }

        /// <summary>
        /// Sets the IP address to bind to
        /// </summary>
        public SmtpServerBuilder BindTo(string ipAddress)
        {
            _configuration.IpAddress = IPAddress.Parse(ipAddress);
            return this;
        }

        /// <summary>
        /// Sets the server name
        /// </summary>
        public SmtpServerBuilder ServerName(string serverName)
        {
            _configuration.ServerName = serverName ?? throw new ArgumentNullException(nameof(serverName));
            return this;
        }

        /// <summary>
        /// Sets the maximum message size
        /// </summary>
        public SmtpServerBuilder MaxMessageSize(long sizeInBytes)
        {
            _configuration.MaxMessageSize = sizeInBytes;
            return this;
        }

        /// <summary>
        /// Sets the maximum message size in MB
        /// </summary>
        public SmtpServerBuilder MaxMessageSizeMB(int sizeInMB)
        {
            _configuration.MaxMessageSize = sizeInMB * 1024L * 1024L;
            return this;
        }

        /// <summary>
        /// Sets the maximum number of recipients
        /// </summary>
        public SmtpServerBuilder MaxRecipients(int maxRecipients)
        {
            _configuration.MaxRecipients = maxRecipients;
            return this;
        }

        /// <summary>
        /// Sets the maximum number of connections
        /// </summary>
        public SmtpServerBuilder MaxConnections(int maxConnections)
        {
            _configuration.MaxConnections = maxConnections;
            return this;
        }

        /// <summary>
        /// Sets the maximum connections per IP
        /// </summary>
        public SmtpServerBuilder MaxConnectionsPerIP(int maxConnectionsPerIp)
        {
            _configuration.MaxConnectionsPerIp = maxConnectionsPerIp;
            return this;
        }

        /// <summary>
        /// Enables/disables pipelining
        /// </summary>
        public SmtpServerBuilder EnablePipelining(bool enable = true)
        {
            _configuration.EnablePipelining = enable;
            return this;
        }

        /// <summary>
        /// Enables/disables 8BITMIME
        /// </summary>
        public SmtpServerBuilder Enable8BitMime(bool enable = true)
        {
            _configuration.Enable8BitMime = enable;
            return this;
        }

        /// <summary>
        /// Sets the certificate for TLS/SSL
        /// </summary>
        public SmtpServerBuilder Certificate(X509Certificate2 certificate)
        {
            _configuration.Certificate = certificate;
            return this;
        }

        /// <summary>
        /// Sets SSL certificate from file
        /// </summary>
        public SmtpServerBuilder Certificate(string path, string? password = null)
        {
#if NET9_0_OR_GREATER
            _configuration.Certificate = string.IsNullOrEmpty(password)
                ? X509CertificateLoader.LoadCertificateFromFile(path)
                : X509CertificateLoader.LoadPkcs12FromFile(path, password);
#else
            _configuration.Certificate = string.IsNullOrEmpty(password)
                ? new X509Certificate2(path)
                : new X509Certificate2(path, password);
#endif
            return this;
        }

        /// <summary>
        /// Sets SSL protocols
        /// </summary>
        public SmtpServerBuilder SslProtocols(SslProtocols protocols)
        {
            _configuration.SslProtocols = protocols;
            return this;
        }

        /// <summary>
        /// Requires authentication
        /// </summary>
        public SmtpServerBuilder RequireAuthentication(bool require = true)
        {
            _configuration.RequireAuthentication = require;
            return this;
        }

        /// <summary>
        /// Requires secure connection
        /// </summary>
        public SmtpServerBuilder RequireSecureConnection(bool require = true)
        {
            _configuration.RequireSecureConnection = require;
            return this;
        }

        /// <summary>
        /// Allows plain text authentication
        /// </summary>
        public SmtpServerBuilder AllowPlainTextAuthentication(bool allow = true)
        {
            _configuration.AllowPlainTextAuthentication = allow;
            return this;
        }

        /// <summary>
        /// Adds an authentication mechanism
        /// </summary>
        public SmtpServerBuilder AddAuthenticationMechanism(string mechanism)
        {
            if (!_configuration.AuthenticationMechanisms.Contains(mechanism))
            {
                _configuration.AuthenticationMechanisms.Add(mechanism);
            }
            return this;
        }

        /// <summary>
        /// Sets the authentication handler
        /// </summary>
        public SmtpServerBuilder AuthenticationHandler(AuthenticationHandler handler)
        {
            AuthenticatorFactory.SetDefaultHandler(handler);
            return this;
        }

        /// <summary>
        /// Sets the authentication handler with simple username/password validation
        /// </summary>
        public SmtpServerBuilder SimpleAuthentication(string username, string password)
        {
            AuthenticatorFactory.SetDefaultHandler(async (user, pass) =>
            {
                if (user == username && pass == password)
                {
                    return AuthenticationResult.Succeed(user);
                }

                return AuthenticationResult.Fail("Invalid credentials");
            });
            return this;
        }

        /// <summary>
        /// Sets the connection timeout
        /// </summary>
        public SmtpServerBuilder ConnectionTimeout(TimeSpan timeout)
        {
            _configuration.ConnectionTimeout = timeout;
            return this;
        }

        /// <summary>
        /// Sets the command timeout
        /// </summary>
        public SmtpServerBuilder CommandTimeout(TimeSpan timeout)
        {
            _configuration.CommandTimeout = timeout;
            return this;
        }

        /// <summary>
        /// Sets the data timeout
        /// </summary>
        public SmtpServerBuilder DataTimeout(TimeSpan timeout)
        {
            _configuration.DataTimeout = timeout;
            return this;
        }

        /// <summary>
        /// Sets the logger factory
        /// </summary>
        public SmtpServerBuilder LoggerFactory(ILoggerFactory loggerFactory)
        {
            _configuration.LoggerFactory = loggerFactory;
            return this;
        }

        /// <summary>
        /// Enables verbose logging
        /// </summary>
        public SmtpServerBuilder EnableVerboseLogging(bool enable = true)
        {
            _configuration.EnableVerboseLogging = enable;
            return this;
        }

        /// <summary>
        /// Sets the banner message
        /// </summary>
        public SmtpServerBuilder Banner(string banner)
        {
            _configuration.Banner = banner;
            return this;
        }

        /// <summary>
        /// Sets the greeting message
        /// </summary>
        public SmtpServerBuilder Greeting(string greeting)
        {
            _configuration.Greeting = greeting;
            return this;
        }

        /// <summary>
        /// Sets buffer sizes
        /// </summary>
        public SmtpServerBuilder BufferSize(int readBufferSize, int writeBufferSize)
        {
            _configuration.ReadBufferSize = readBufferSize;
            _configuration.WriteBufferSize = writeBufferSize;
            return this;
        }

        /// <summary>
        /// Enables SMTP UTF8 support
        /// </summary>
        public SmtpServerBuilder EnableSmtpUtf8(bool enable = true)
        {
            _configuration.EnableSmtpUtf8 = enable;
            return this;
        }

        /// <summary>
        /// Sets the message store for saving messages
        /// </summary>
        public SmtpServerBuilder MessageStore(IMessageStore messageStore)
        {
            _configuration.MessageStore = messageStore ?? throw new ArgumentNullException(nameof(messageStore));
            return this;
        }

        /// <summary>
        /// Sets a file-based message store with directory structure
        /// </summary>
        public SmtpServerBuilder WithFileMessageStore(string directory, bool createDateFolders = true)
        {
            _configuration.MessageStore = new FileMessageStore(directory, createDateFolders);
            return this;
        }

        /// <summary>
        /// Sets the mailbox filter
        /// </summary>
        public SmtpServerBuilder MailboxFilter(IMailboxFilter mailboxFilter)
        {
            _configuration.MailboxFilter = mailboxFilter ?? throw new ArgumentNullException(nameof(mailboxFilter));
            return this;
        }

        /// <summary>
        /// Filters senders by allowing only specific domains (protocol-level filtering)
        /// </summary>
        public SmtpServerBuilder WithSenderDomainWhitelist(params string[] domains)
        {
            _configuration.MailboxFilter ??= new DomainMailboxFilter(true);

            if (_configuration.MailboxFilter is DomainMailboxFilter filter)
            {
                filter.AllowFromDomains(domains);
            }

            return this;
        }

        /// <summary>
        /// Blocks senders from specific domains (protocol-level filtering)
        /// </summary>
        public SmtpServerBuilder WithSenderDomainBlacklist(params string[] domains)
        {
            _configuration.MailboxFilter ??= new DomainMailboxFilter(true);

            if (_configuration.MailboxFilter is DomainMailboxFilter filter)
            {
                filter.BlockFromDomains(domains);
            }

            return this;
        }

        /// <summary>
        /// Filters recipients by allowing only specific domains (protocol-level filtering)
        /// </summary>
        public SmtpServerBuilder WithRecipientDomainWhitelist(params string[] domains)
        {
            _configuration.MailboxFilter ??= new DomainMailboxFilter(true);

            if (_configuration.MailboxFilter is DomainMailboxFilter filter)
            {
                filter.AllowToDomains(domains);
            }

            return this;
        }

        /// <summary>
        /// Blocks recipients to specific domains (protocol-level filtering)
        /// </summary>
        public SmtpServerBuilder WithRecipientDomainBlacklist(params string[] domains)
        {
            _configuration.MailboxFilter ??= new DomainMailboxFilter(true);

            if (_configuration.MailboxFilter is DomainMailboxFilter filter)
            {
                filter.BlockToDomains(domains);
            }

            return this;
        }

        /// <summary>
        /// Builds the SMTP server
        /// </summary>
        public SmtpServer Build()
        {
            _configuration.Validate();
            return new SmtpServer(_configuration);
        }

        /// <summary>
        /// Creates a basic SMTP server on port 25
        /// </summary>
        public static SmtpServer CreateBasic()
        {
            return new SmtpServerBuilder()
                .Port(25)
                .Build();
        }

        /// <summary>
        /// Creates a secure SMTP server on port 465
        /// </summary>
        public static SmtpServer CreateSecure(X509Certificate2 certificate)
        {
            return new SmtpServerBuilder()
                .Port(465)
                .Certificate(certificate)
                .RequireSecureConnection()
                .Build();
        }

        /// <summary>
        /// Creates an authenticated SMTP server
        /// </summary>
        public static SmtpServer CreateAuthenticated(int port, AuthenticationHandler authHandler)
        {
            return new SmtpServerBuilder()
                .Port(port)
                .RequireAuthentication()
                .AuthenticationHandler(authHandler)
                .AddAuthenticationMechanism("PLAIN")
                .AddAuthenticationMechanism("LOGIN")
                .Build();
        }
    }
}