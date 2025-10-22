using System;
using Zetian.Abstractions;

namespace Zetian.Models.EventArgs
{
    /// <summary>
    /// Event arguments for authentication events
    /// </summary>
    public class AuthenticationEventArgs(string mechanism, string? username, string? password, ISmtpSession session) : System.EventArgs
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