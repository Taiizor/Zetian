using System;
using Zetian.Protocol;

namespace Zetian.Core
{
    /// <summary>
    /// Event arguments for session events
    /// </summary>
    public class SessionEventArgs(ISmtpSession session) : EventArgs
    {
        /// <summary>
        /// Gets the SMTP session
        /// </summary>
        public ISmtpSession Session { get; } = session ?? throw new ArgumentNullException(nameof(session));
    }

    /// <summary>
    /// Event arguments for message events
    /// </summary>
    public class MessageEventArgs(ISmtpMessage message, ISmtpSession session) : EventArgs
    {
        /// <summary>
        /// Gets the received message
        /// </summary>
        public ISmtpMessage Message { get; } = message ?? throw new ArgumentNullException(nameof(message));

        /// <summary>
        /// Gets the session that received the message
        /// </summary>
        public ISmtpSession Session { get; } = session ?? throw new ArgumentNullException(nameof(session));

        /// <summary>
        /// Gets or sets whether to cancel/reject the message
        /// </summary>
        public bool Cancel { get; set; } = false;

        /// <summary>
        /// Gets or sets the response to send to the client
        /// </summary>
        public SmtpResponse Response { get; set; } = SmtpResponse.Ok;
    }

    /// <summary>
    /// Event arguments for error events
    /// </summary>
    public class ErrorEventArgs(Exception exception, ISmtpSession? session = null) : EventArgs
    {
        /// <summary>
        /// Gets the exception that occurred
        /// </summary>
        public Exception Exception { get; } = exception ?? throw new ArgumentNullException(nameof(exception));

        /// <summary>
        /// Gets the session where the error occurred (if any)
        /// </summary>
        public ISmtpSession? Session { get; } = session;
    }

    /// <summary>
    /// Event arguments for authentication events
    /// </summary>
    public class AuthenticationEventArgs(string mechanism, string? username, string? password, ISmtpSession session) : EventArgs
    {
        /// <summary>
        /// Gets the authentication mechanism
        /// </summary>
        public string Mechanism { get; } = mechanism ?? throw new ArgumentNullException(nameof(mechanism));

        /// <summary>
        /// Gets the username
        /// </summary>
        public string? Username { get; } = username;

        /// <summary>
        /// Gets the password
        /// </summary>
        public string? Password { get; } = password;

        /// <summary>
        /// Gets the session
        /// </summary>
        public ISmtpSession Session { get; } = session ?? throw new ArgumentNullException(nameof(session));

        /// <summary>
        /// Gets or sets whether the authentication succeeded
        /// </summary>
        public bool IsAuthenticated { get; set; } = false;

        /// <summary>
        /// Gets or sets the authenticated identity
        /// </summary>
        public string? AuthenticatedIdentity { get; set; }
    }
}