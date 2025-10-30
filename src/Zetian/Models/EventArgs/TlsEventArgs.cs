using System;
using System.Security.Cryptography.X509Certificates;
using Zetian.Abstractions;

namespace Zetian.Models.EventArgs
{
    /// <summary>
    /// Event arguments for TLS events
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of TlsEventArgs
    /// </remarks>
    public class TlsEventArgs(ISmtpSession session) : System.EventArgs
    {
        /// <summary>
        /// Gets the session
        /// </summary>
        public ISmtpSession Session { get; } = session ?? throw new ArgumentNullException(nameof(session));

        /// <summary>
        /// Gets or sets the server certificate
        /// </summary>
        public X509Certificate2? Certificate { get; set; }

        /// <summary>
        /// Gets or sets whether the TLS negotiation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the error message if failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the TLS protocol version
        /// </summary>
        public string? ProtocolVersion { get; set; }

        /// <summary>
        /// Gets or sets the cipher suite
        /// </summary>
        public string? CipherSuite { get; set; }

        /// <summary>
        /// Gets or sets whether to cancel the TLS upgrade
        /// </summary>
        public bool Cancel { get; set; }
    }
}