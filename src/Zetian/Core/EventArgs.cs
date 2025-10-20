using System;
using Zetian.Protocol;

namespace Zetian.Core
{
    /// <summary>
    /// Event arguments for session events
    /// </summary>
    public class SessionEventArgs : EventArgs
    {
        public SessionEventArgs(ISmtpSession session)
        {
            Session = session ?? throw new ArgumentNullException(nameof(session));
        }

        /// <summary>
        /// Gets the SMTP session
        /// </summary>
        public ISmtpSession Session { get; }
    }

    /// <summary>
    /// Event arguments for message events
    /// </summary>
    public class MessageEventArgs : EventArgs
    {
        public MessageEventArgs(ISmtpMessage message, ISmtpSession session)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Session = session ?? throw new ArgumentNullException(nameof(session));
            Cancel = false;
            Response = SmtpResponse.Ok;
        }

        /// <summary>
        /// Gets the received message
        /// </summary>
        public ISmtpMessage Message { get; }

        /// <summary>
        /// Gets the session that received the message
        /// </summary>
        public ISmtpSession Session { get; }

        /// <summary>
        /// Gets or sets whether to cancel/reject the message
        /// </summary>
        public bool Cancel { get; set; }

        /// <summary>
        /// Gets or sets the response to send to the client
        /// </summary>
        public SmtpResponse Response { get; set; }
    }

    /// <summary>
    /// Event arguments for error events
    /// </summary>
    public class ErrorEventArgs : EventArgs
    {
        public ErrorEventArgs(Exception exception, ISmtpSession? session = null)
        {
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
            Session = session;
        }

        /// <summary>
        /// Gets the exception that occurred
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Gets the session where the error occurred (if any)
        /// </summary>
        public ISmtpSession? Session { get; }
    }

    /// <summary>
    /// Event arguments for authentication events
    /// </summary>
    public class AuthenticationEventArgs : EventArgs
    {
        public AuthenticationEventArgs(string mechanism, string? username, string? password, ISmtpSession session)
        {
            Mechanism = mechanism ?? throw new ArgumentNullException(nameof(mechanism));
            Username = username;
            Password = password;
            Session = session ?? throw new ArgumentNullException(nameof(session));
            IsAuthenticated = false;
        }

        /// <summary>
        /// Gets the authentication mechanism
        /// </summary>
        public string Mechanism { get; }

        /// <summary>
        /// Gets the username
        /// </summary>
        public string? Username { get; }

        /// <summary>
        /// Gets the password
        /// </summary>
        public string? Password { get; }

        /// <summary>
        /// Gets the session
        /// </summary>
        public ISmtpSession Session { get; }

        /// <summary>
        /// Gets or sets whether the authentication succeeded
        /// </summary>
        public bool IsAuthenticated { get; set; }

        /// <summary>
        /// Gets or sets the authenticated identity
        /// </summary>
        public string? AuthenticatedIdentity { get; set; }
    }
}