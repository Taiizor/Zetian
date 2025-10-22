namespace Zetian.Models
{
    /// <summary>
    /// Authentication result
    /// </summary>
    /// <remarks>
    /// Initializes a new authentication result
    /// </remarks>
    public class AuthenticationResult(bool success, string? identity = null, string? errorMessage = null)
    {
        /// <summary>
        /// Gets whether authentication succeeded
        /// </summary>
        public bool Success { get; } = success;

        /// <summary>
        /// Gets the authenticated identity
        /// </summary>
        public string? Identity { get; } = identity;

        /// <summary>
        /// Gets the error message if failed
        /// </summary>
        public string? ErrorMessage { get; } = errorMessage;

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
}