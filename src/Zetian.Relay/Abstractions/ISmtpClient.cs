using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Zetian.Abstractions;

namespace Zetian.Relay.Abstractions
{
    /// <summary>
    /// Interface for SMTP client operations
    /// </summary>
    public interface ISmtpClient : IDisposable
    {
        /// <summary>
        /// Gets or sets the remote server hostname
        /// </summary>
        string Host { get; set; }

        /// <summary>
        /// Gets or sets the remote server port
        /// </summary>
        int Port { get; set; }

        /// <summary>
        /// Gets or sets whether to use SSL/TLS
        /// </summary>
        bool EnableSsl { get; set; }

        /// <summary>
        /// Gets or sets the SSL protocols to use
        /// </summary>
        SslProtocols SslProtocols { get; set; }

        /// <summary>
        /// Gets or sets the client certificate
        /// </summary>
        X509Certificate2? ClientCertificate { get; set; }

        /// <summary>
        /// Gets or sets the credentials for authentication
        /// </summary>
        NetworkCredential? Credentials { get; set; }

        /// <summary>
        /// Gets or sets the local hostname for HELO/EHLO
        /// </summary>
        string? LocalDomain { get; set; }

        /// <summary>
        /// Gets or sets the connection timeout
        /// </summary>
        TimeSpan Timeout { get; set; }

        /// <summary>
        /// Gets whether the client is connected
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Gets the server capabilities after EHLO
        /// </summary>
        IReadOnlyDictionary<string, string>? ServerCapabilities { get; }

        /// <summary>
        /// Connects to the SMTP server
        /// </summary>
        Task ConnectAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Authenticates with the SMTP server
        /// </summary>
        Task AuthenticateAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a message
        /// </summary>
        Task<SmtpDeliveryResult> SendAsync(
            ISmtpMessage message,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a message to specific recipients
        /// </summary>
        Task<SmtpDeliveryResult> SendAsync(
            ISmtpMessage message,
            IEnumerable<string> recipients,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends raw message data
        /// </summary>
        Task<SmtpDeliveryResult> SendRawAsync(
            string from,
            IEnumerable<string> recipients,
            byte[] messageData,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifies a recipient address
        /// </summary>
        Task<bool> VerifyAsync(string address, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a NOOP command
        /// </summary>
        Task NoOpAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Resets the current transaction
        /// </summary>
        Task ResetAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Disconnects from the SMTP server
        /// </summary>
        Task DisconnectAsync(bool quit = true, CancellationToken cancellationToken = default);
    }
}