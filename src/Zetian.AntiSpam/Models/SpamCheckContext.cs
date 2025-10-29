using System.Collections.Generic;
using System.Net;

namespace Zetian.AntiSpam.Models
{
    /// <summary>
    /// Context for spam checking operations
    /// </summary>
    public class SpamCheckContext
    {
        /// <summary>
        /// Gets or sets the sender's email address
        /// </summary>
        public string FromAddress { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the sender's domain
        /// </summary>
        public string FromDomain { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the recipient email addresses
        /// </summary>
        public List<string> Recipients { get; set; } = [];

        /// <summary>
        /// Gets or sets the client IP address
        /// </summary>
        public IPAddress? ClientIpAddress { get; set; }

        /// <summary>
        /// Gets or sets the client hostname
        /// </summary>
        public string? ClientHostname { get; set; }

        /// <summary>
        /// Gets or sets the HELO/EHLO domain
        /// </summary>
        public string? HeloDomain { get; set; }

        /// <summary>
        /// Gets or sets the email message
        /// </summary>
        public SmtpMessage? Message { get; set; }

        /// <summary>
        /// Gets or sets the raw email headers
        /// </summary>
        public Dictionary<string, List<string>> Headers { get; set; } = [];

        /// <summary>
        /// Gets or sets the message body
        /// </summary>
        public string? MessageBody { get; set; }

        /// <summary>
        /// Gets or sets the message subject
        /// </summary>
        public string? Subject { get; set; }

        /// <summary>
        /// Gets or sets additional metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = [];

        /// <summary>
        /// Gets or sets whether the connection is authenticated
        /// </summary>
        public bool IsAuthenticated { get; set; }

        /// <summary>
        /// Gets or sets the authenticated username
        /// </summary>
        public string? AuthenticatedUser { get; set; }

        /// <summary>
        /// Creates a SpamCheckContext from an SMTP session
        /// </summary>
        public static SpamCheckContext FromSession(Abstractions.ISmtpSession session, SmtpMessage? message = null)
        {
            return new SpamCheckContext
            {
                FromAddress = session.From ?? string.Empty,
                ClientIpAddress = session.RemoteEndPoint?.Address,
                ClientHostname = session.RemoteEndPoint?.ToString(),
                IsAuthenticated = session.IsAuthenticated,
                AuthenticatedUser = session.AuthenticatedUser,
                Message = message,
                Subject = message?.Subject,
                MessageBody = message?.TextBody ?? message?.HtmlBody
            };
        }
    }
}