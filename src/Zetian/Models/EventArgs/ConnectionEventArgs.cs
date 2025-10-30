using System;
using System.Collections.Generic;
using System.Net;

namespace Zetian.Models.EventArgs
{
    /// <summary>
    /// Event arguments for connection events
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of ConnectionEventArgs
    /// </remarks>
    public class ConnectionEventArgs(IPEndPoint remoteEndPoint, IPEndPoint localEndPoint) : System.EventArgs
    {
        /// <summary>
        /// Gets the remote endpoint
        /// </summary>
        public IPEndPoint RemoteEndPoint { get; } = remoteEndPoint ?? throw new ArgumentNullException(nameof(remoteEndPoint));

        /// <summary>
        /// Gets the local endpoint
        /// </summary>
        public IPEndPoint LocalEndPoint { get; } = localEndPoint ?? throw new ArgumentNullException(nameof(localEndPoint));

        /// <summary>
        /// Gets or sets whether to accept the connection
        /// </summary>
        public bool Accept { get; set; } = true;

        /// <summary>
        /// Gets or sets the rejection reason
        /// </summary>
        public string? RejectionReason { get; set; }

        /// <summary>
        /// Gets or sets custom properties
        /// </summary>
        public Dictionary<string, object> Properties { get; } = [];
    }
}