using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Zetian.Configuration;
using Zetian.Models.EventArgs;

namespace Zetian.Abstractions
{
    /// <summary>
    /// Represents the main SMTP server interface
    /// </summary>
    public interface ISmtpServer : IDisposable
    {
        /// <summary>
        /// Gets the server configuration
        /// </summary>
        SmtpServerConfiguration Configuration { get; }

        /// <summary>
        /// Gets whether the server is currently running
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Gets the server's endpoint
        /// </summary>
        IPEndPoint? Endpoint { get; }

        /// <summary>
        /// Starts the SMTP server
        /// </summary>
        Task StartAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops the SMTP server
        /// </summary>
        Task StopAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Occurs when a new session is created
        /// </summary>
        event EventHandler<SessionEventArgs>? SessionCreated;

        /// <summary>
        /// Occurs when a session is completed
        /// </summary>
        event EventHandler<SessionEventArgs>? SessionCompleted;

        /// <summary>
        /// Occurs when a message is received
        /// </summary>
        event EventHandler<MessageEventArgs>? MessageReceived;

        /// <summary>
        /// Occurs when an error happens
        /// </summary>
        event EventHandler<ErrorEventArgs>? ErrorOccurred;

        /// <summary>
        /// Occurs when a connection is accepted
        /// </summary>
        event EventHandler<ConnectionEventArgs>? ConnectionAccepted;

        /// <summary>
        /// Occurs when a connection is rejected
        /// </summary>
        event EventHandler<ConnectionEventArgs>? ConnectionRejected;

        /// <summary>
        /// Occurs when a command is received
        /// </summary>
        event EventHandler<CommandEventArgs>? CommandReceived;

        /// <summary>
        /// Occurs when a command is executed
        /// </summary>
        event EventHandler<CommandEventArgs>? CommandExecuted;

        /// <summary>
        /// Occurs when authentication is attempted
        /// </summary>
        event EventHandler<AuthenticationEventArgs>? AuthenticationAttempted;

        /// <summary>
        /// Occurs when authentication succeeds
        /// </summary>
        event EventHandler<AuthenticationEventArgs>? AuthenticationSucceeded;

        /// <summary>
        /// Occurs when authentication fails
        /// </summary>
        event EventHandler<AuthenticationEventArgs>? AuthenticationFailed;

        /// <summary>
        /// Occurs when TLS negotiation starts
        /// </summary>
        event EventHandler<TlsEventArgs>? TlsNegotiationStarted;

        /// <summary>
        /// Occurs when TLS negotiation completes
        /// </summary>
        event EventHandler<TlsEventArgs>? TlsNegotiationCompleted;

        /// <summary>
        /// Occurs when TLS negotiation fails
        /// </summary>
        event EventHandler<TlsEventArgs>? TlsNegotiationFailed;

        /// <summary>
        /// Occurs when data transfer starts
        /// </summary>
        event EventHandler<DataTransferEventArgs>? DataTransferStarted;

        /// <summary>
        /// Occurs when data transfer completes
        /// </summary>
        event EventHandler<DataTransferEventArgs>? DataTransferCompleted;

        /// <summary>
        /// Occurs when rate limit is exceeded
        /// </summary>
        event EventHandler<RateLimitEventArgs>? RateLimitExceeded;
    }
}