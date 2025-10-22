using System;
using Zetian.Abstractions;
using Zetian.Protocol;

namespace Zetian.Models.EventArgs
{
    /// <summary>
    /// Event arguments for message events
    /// </summary>
    public class MessageEventArgs(ISmtpMessage message, ISmtpSession session) : System.EventArgs
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
}