using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Zetian.WebUI.Services
{
    /// <summary>
    /// Service for authentication
    /// </summary>
    public interface IAuthenticationService
    {
        /// <summary>
        /// Authenticates a user
        /// </summary>
        Task<AuthenticationResult> AuthenticateAsync(string username, string password);

        /// <summary>
        /// Authenticates with API key
        /// </summary>
        Task<AuthenticationResult> AuthenticateWithApiKeyAsync(string apiKey);

        /// <summary>
        /// Refreshes a token
        /// </summary>
        Task<AuthenticationResult> RefreshTokenAsync(string refreshToken);

        /// <summary>
        /// Validates a token
        /// </summary>
        Task<TokenValidationResult> ValidateTokenAsync(string token);

        /// <summary>
        /// Revokes a token
        /// </summary>
        Task RevokeTokenAsync(string token);

        /// <summary>
        /// Changes user password
        /// </summary>
        Task<bool> ChangePasswordAsync(string username, string currentPassword, string newPassword);

        /// <summary>
        /// Gets active sessions
        /// </summary>
        Task<IEnumerable<UserSession>> GetActiveSessionsAsync();

        /// <summary>
        /// Terminates a session
        /// </summary>
        Task<bool> TerminateSessionAsync(string sessionId);
    }

    /// <summary>
    /// Authentication result
    /// </summary>
    public class AuthenticationResult
    {
        public bool Success { get; set; }
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public UserInfo? User { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// User information
    /// </summary>
    public class UserInfo
    {
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public string[] Roles { get; set; } = Array.Empty<string>();
        public Dictionary<string, object> Claims { get; set; } = [];
    }

    /// <summary>
    /// Token validation result
    /// </summary>
    public class TokenValidationResult
    {
        public bool IsValid { get; set; }
        public string? Username { get; set; }
        public string[] Roles { get; set; } = Array.Empty<string>();
        public DateTime? ExpiresAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// User session
    /// </summary>
    public class UserSession
    {
        public string SessionId { get; set; } = "";
        public string Username { get; set; } = "";
        public string IpAddress { get; set; } = "";
        public string UserAgent { get; set; } = "";
        public DateTime LoginTime { get; set; }
        public DateTime LastActivity { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}