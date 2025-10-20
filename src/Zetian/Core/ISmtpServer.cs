using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Zetian.Configuration;

namespace Zetian.Core
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
    }
}