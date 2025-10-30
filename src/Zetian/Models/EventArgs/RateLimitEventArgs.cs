using System;
using System.Net;
using Zetian.Abstractions;

namespace Zetian.Models.EventArgs
{
    /// <summary>
    /// Event arguments for rate limit events
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of RateLimitEventArgs
    /// </remarks>
    public class RateLimitEventArgs(IPAddress ipAddress, ISmtpSession? session = null) : System.EventArgs
    {
        /// <summary>
        /// Gets the IP address
        /// </summary>
        public IPAddress IpAddress { get; } = ipAddress ?? throw new ArgumentNullException(nameof(ipAddress));

        /// <summary>
        /// Gets the session (if available)
        /// </summary>
        public ISmtpSession? Session { get; } = session;

        /// <summary>
        /// Gets or sets the current request count
        /// </summary>
        public int CurrentCount { get; set; }

        /// <summary>
        /// Gets or sets the limit
        /// </summary>
        public int Limit { get; set; }

        /// <summary>
        /// Gets or sets the time window
        /// </summary>
        public TimeSpan TimeWindow { get; set; }

        /// <summary>
        /// Gets or sets when the limit will reset
        /// </summary>
        public DateTime ResetTime { get; set; }

        /// <summary>
        /// Gets or sets whether to block the request
        /// </summary>
        public bool Block { get; set; } = true;

        /// <summary>
        /// Gets or sets a custom response message
        /// </summary>
        public string? ResponseMessage { get; set; }
    }
}