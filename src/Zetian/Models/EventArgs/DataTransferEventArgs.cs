using System;
using System.Collections.Generic;
using Zetian.Abstractions;

namespace Zetian.Models.EventArgs
{
    /// <summary>
    /// Event arguments for data transfer events
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of DataTransferEventArgs
    /// </remarks>
    public class DataTransferEventArgs(ISmtpSession session, string? from = null, List<string>? recipients = null) : System.EventArgs
    {
        /// <summary>
        /// Gets the session
        /// </summary>
        public ISmtpSession Session { get; } = session ?? throw new ArgumentNullException(nameof(session));

        /// <summary>
        /// Gets the sender address
        /// </summary>
        public string? From { get; } = from;

        /// <summary>
        /// Gets the recipient addresses
        /// </summary>
        public List<string> Recipients { get; } = recipients ?? [];

        /// <summary>
        /// Gets or sets the transferred bytes
        /// </summary>
        public long BytesTransferred { get; set; }

        /// <summary>
        /// Gets or sets the total message size
        /// </summary>
        public long TotalSize { get; set; }

        /// <summary>
        /// Gets or sets whether the transfer was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the error message if failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the transfer duration in milliseconds
        /// </summary>
        public double DurationMs { get; set; }

        /// <summary>
        /// Gets or sets whether to cancel the transfer
        /// </summary>
        public bool Cancel { get; set; }
    }
}