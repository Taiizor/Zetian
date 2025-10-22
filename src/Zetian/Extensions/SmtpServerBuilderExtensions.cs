using System;
using System.Security.Cryptography.X509Certificates;
using Zetian.Authentication;
using Zetian.Storage;

namespace Zetian.Extensions
{
    /// <summary>
    /// Extension methods for SmtpServerBuilder
    /// </summary>
    public static class SmtpServerBuilderExtensions
    {
        /// <summary>
        /// Adds a spam filter that blocks known spam domains
        /// </summary>
        public static SmtpServerBuilder AddSpamFilter(this SmtpServerBuilder builder, params string[] spamDomains)
        {
            ArgumentNullException.ThrowIfNull(builder);

            return builder.WithSenderDomainBlacklist(spamDomains);
        }

        /// <summary>
        /// Configures the server to only accept emails for specific domains
        /// </summary>
        public static SmtpServerBuilder AddAllowedDomains(this SmtpServerBuilder builder, params string[] domains)
        {
            ArgumentNullException.ThrowIfNull(builder);

            return builder.WithRecipientDomainWhitelist(domains);
        }

        /// <summary>
        /// Adds a size filter that rejects messages over a certain size
        /// </summary>
        public static SmtpServerBuilder AddSizeFilter(this SmtpServerBuilder builder, long maxSizeInBytes)
        {
            ArgumentNullException.ThrowIfNull(builder);

            return builder.MaxMessageSize(maxSizeInBytes);
        }

        /// <summary>
        /// Creates a development SMTP server with verbose logging and no authentication
        /// </summary>
        public static SmtpServer CreateDevelopment(this SmtpServerBuilder _, int port = 2525, string? messageDirectory = null)
        {
            SmtpServerBuilder builder = new SmtpServerBuilder()
                .Port(port)
                .EnableVerboseLogging()
                .EnableSmtpUtf8()
                .EnablePipelining()
                .Enable8BitMime()
                .MaxMessageSizeMB(50);

            if (!string.IsNullOrEmpty(messageDirectory))
            {
                builder.WithFileMessageStore(messageDirectory);
            }

            return builder.Build();
        }

        /// <summary>
        /// Creates a production SMTP server with authentication and TLS
        /// </summary>
        public static SmtpServer CreateProduction(
            this SmtpServerBuilder _,
            int port,
            X509Certificate2 certificate,
            AuthenticationHandler authHandler,
            IMessageStore? messageStore = null,
            IMailboxFilter? mailboxFilter = null)
        {
            SmtpServerBuilder builder = new SmtpServerBuilder()
                .Port(port)
                .Certificate(certificate)
                .RequireAuthentication()
                .RequireSecureConnection()
                .AuthenticationHandler(authHandler)
                .EnableSmtpUtf8()
                .EnablePipelining()
                .Enable8BitMime()
                .MaxMessageSizeMB(25)
                .MaxConnections(100)
                .MaxConnectionsPerIP(10);

            if (messageStore != null)
            {
                builder.MessageStore(messageStore);
            }

            if (mailboxFilter != null)
            {
                builder.MailboxFilter(mailboxFilter);
            }

            return builder.Build();
        }
    }
}