using System;
using Zetian.Abstractions;

namespace Zetian.Models.EventArgs
{
    /// <summary>
    /// Event arguments for session events
    /// </summary>
    public class SessionEventArgs(ISmtpSession session) : System.EventArgs
    {
        /// <summary>
        /// Gets the SMTP session
        /// </summary>
        public ISmtpSession Session { get; } = session ?? throw new ArgumentNullException(nameof(session));
    }
}