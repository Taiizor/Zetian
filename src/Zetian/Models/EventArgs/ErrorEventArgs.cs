using System;
using Zetian.Abstractions;

namespace Zetian.Models.EventArgs
{
    /// <summary>
    /// Event arguments for error events
    /// </summary>
    public class ErrorEventArgs(Exception exception, ISmtpSession? session = null) : System.EventArgs
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
}