using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace Zetian.Core
{
    /// <summary>
    /// Represents an SMTP session
    /// </summary>
    public interface ISmtpSession
    {
        /// <summary>
        /// Gets the unique session ID
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Gets the remote endpoint
        /// </summary>
        EndPoint RemoteEndPoint { get; }

        /// <summary>
        /// Gets the local endpoint
        /// </summary>
        EndPoint LocalEndPoint { get; }

        /// <summary>
        /// Gets whether the connection is secure (TLS/SSL)
        /// </summary>
        bool IsSecure { get; }

        /// <summary>
        /// Gets whether the session is authenticated
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// Gets the authenticated identity
        /// </summary>
        string? AuthenticatedIdentity { get; }

        /// <summary>
        /// Gets the client's HELO/EHLO domain
        /// </summary>
        string? ClientDomain { get; }

        /// <summary>
        /// Gets the session start time
        /// </summary>
        DateTime StartTime { get; }

        /// <summary>
        /// Gets the session properties/metadata
        /// </summary>
        IDictionary<string, object> Properties { get; }

        /// <summary>
        /// Gets the SSL/TLS certificate if secure
        /// </summary>
        X509Certificate2? ClientCertificate { get; }

        /// <summary>
        /// Gets the number of messages received in this session
        /// </summary>
        int MessageCount { get; }

        /// <summary>
        /// Gets or sets whether pipelining is enabled
        /// </summary>
        bool PipeliningEnabled { get; set; }

        /// <summary>
        /// Gets or sets whether 8BITMIME is enabled
        /// </summary>
        bool EightBitMimeEnabled { get; set; }

        /// <summary>
        /// Gets or sets whether binary MIME is enabled
        /// </summary>
        bool BinaryMimeEnabled { get; set; }

        /// <summary>
        /// Gets or sets the maximum message size for this session
        /// </summary>
        long MaxMessageSize { get; set; }
    }
}