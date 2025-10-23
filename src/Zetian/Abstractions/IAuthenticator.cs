using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Zetian.Models;

namespace Zetian.Abstractions
{
    /// <summary>
    /// Represents an authentication mechanism
    /// </summary>
    public interface IAuthenticator
    {
        /// <summary>
        /// Gets the mechanism name
        /// </summary>
        string Mechanism { get; }

        /// <summary>
        /// Authenticates a session
        /// </summary>
        Task<AuthenticationResult> AuthenticateAsync(
            ISmtpSession session,
            string? initialResponse,
            StreamReader reader,
            StreamWriter writer,
            CancellationToken cancellationToken);
    }
}