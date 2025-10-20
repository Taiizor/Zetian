using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Zetian.Core;

namespace Zetian.Authentication
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
            PipeReader reader,
            PipeWriter writer,
            CancellationToken cancellationToken);
    }

    /// <summary>
    /// Authentication result
    /// </summary>
    public class AuthenticationResult
    {
        /// <summary>
        /// Initializes a new authentication result
        /// </summary>
        public AuthenticationResult(bool success, string? identity = null, string? errorMessage = null)
        {
            Success = success;
            Identity = identity;
            ErrorMessage = errorMessage;
        }

        /// <summary>
        /// Gets whether authentication succeeded
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Gets the authenticated identity
        /// </summary>
        public string? Identity { get; }

        /// <summary>
        /// Gets the error message if failed
        /// </summary>
        public string? ErrorMessage { get; }

        /// <summary>
        /// Creates a successful result
        /// </summary>
        public static AuthenticationResult Succeed(string identity)
        {
            return new AuthenticationResult(true, identity);
        }

        /// <summary>
        /// Creates a failed result
        /// </summary>
        public static AuthenticationResult Fail(string? errorMessage = null)
        {
            return new AuthenticationResult(false, null, errorMessage);
        }
    }

    /// <summary>
    /// Authentication handler delegate
    /// </summary>
    public delegate Task<AuthenticationResult> AuthenticationHandler(string? username, string? password);
}