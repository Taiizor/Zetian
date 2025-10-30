using System;
using Zetian.Abstractions;
using Zetian.Protocol;

namespace Zetian.Models.EventArgs
{
    /// <summary>
    /// Event arguments for command events
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of CommandEventArgs
    /// </remarks>
    public class CommandEventArgs(ISmtpSession session, SmtpCommand command, string rawCommand) : System.EventArgs
    {
        /// <summary>
        /// Gets the session
        /// </summary>
        public ISmtpSession Session { get; } = session ?? throw new ArgumentNullException(nameof(session));

        /// <summary>
        /// Gets the command
        /// </summary>
        public SmtpCommand Command { get; } = command ?? throw new ArgumentNullException(nameof(command));

        /// <summary>
        /// Gets the raw command string
        /// </summary>
        public string RawCommand { get; } = rawCommand ?? throw new ArgumentNullException(nameof(rawCommand));

        /// <summary>
        /// Gets or sets whether to cancel the command
        /// </summary>
        public bool Cancel { get; set; }

        /// <summary>
        /// Gets or sets the custom response (if Cancel is true)
        /// </summary>
        public SmtpResponse? Response { get; set; }

        /// <summary>
        /// Gets or sets the command execution duration in milliseconds
        /// </summary>
        public double? DurationMs { get; set; }

        /// <summary>
        /// Gets or sets whether the command was successful
        /// </summary>
        public bool Success { get; set; } = true;
    }
}