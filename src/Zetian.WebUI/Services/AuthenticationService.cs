using Microsoft.IdentityModel.Tokens;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Zetian.WebUI.Options;

namespace Zetian.WebUI.Services
{
    /// <summary>
    /// Implementation of authentication service
    /// </summary>
    public class AuthenticationService : IAuthenticationService
    {
        private readonly WebUIOptions _options;
        private readonly ConcurrentDictionary<string, UserSession> _sessions = new();
        private readonly ConcurrentDictionary<string, string> _refreshTokens = new();
        private readonly JwtSecurityTokenHandler _tokenHandler = new();

        public AuthenticationService(WebUIOptions options)
        {
            _options = options;
        }

        public Task<AuthenticationResult> AuthenticateAsync(string username, string password)
        {
            // Check admin credentials
            if (username == _options.AdminUsername && password == _options.AdminPassword)
            {
                UserInfo user = new()
                {
                    Username = username,
                    Email = $"{username}@localhost",
                    Roles = new[] { "Admin" }
                };

                (string? token, string? refreshToken, DateTime expiresAt) = GenerateTokens(user);

                // Create session
                UserSession session = new()
                {
                    SessionId = Guid.NewGuid().ToString(),
                    Username = username,
                    LoginTime = DateTime.UtcNow,
                    LastActivity = DateTime.UtcNow,
                    ExpiresAt = expiresAt
                };

                _sessions[session.SessionId] = session;

                return Task.FromResult(new AuthenticationResult
                {
                    Success = true,
                    Token = token,
                    RefreshToken = refreshToken,
                    ExpiresAt = expiresAt,
                    User = user
                });
            }

            return Task.FromResult(new AuthenticationResult
            {
                Success = false,
                ErrorMessage = "Invalid username or password"
            });
        }

        public Task<AuthenticationResult> AuthenticateWithApiKeyAsync(string apiKey)
        {
            if (_options.EnableApiKey && apiKey == _options.ApiKey)
            {
                UserInfo user = new()
                {
                    Username = "api_user",
                    Email = "api@localhost",
                    Roles = new[] { "Api" }
                };

                (string? token, string _, DateTime expiresAt) = GenerateTokens(user);

                return Task.FromResult(new AuthenticationResult
                {
                    Success = true,
                    Token = token,
                    ExpiresAt = expiresAt,
                    User = user
                });
            }

            return Task.FromResult(new AuthenticationResult
            {
                Success = false,
                ErrorMessage = "Invalid API key"
            });
        }

        public Task<AuthenticationResult> RefreshTokenAsync(string refreshToken)
        {
            if (_refreshTokens.TryGetValue(refreshToken, out var username))
            {
                UserInfo user = new()
                {
                    Username = username,
                    Email = $"{username}@localhost",
                    Roles = username == _options.AdminUsername ? new[] { "Admin" } : new[] { "User" }
                };

                (string? token, string? newRefreshToken, DateTime expiresAt) = GenerateTokens(user);

                // Remove old refresh token
                _refreshTokens.TryRemove(refreshToken, out _);

                return Task.FromResult(new AuthenticationResult
                {
                    Success = true,
                    Token = token,
                    RefreshToken = newRefreshToken,
                    ExpiresAt = expiresAt,
                    User = user
                });
            }

            return Task.FromResult(new AuthenticationResult
            {
                Success = false,
                ErrorMessage = "Invalid refresh token"
            });
        }

        public Task<TokenValidationResult> ValidateTokenAsync(string token)
        {
            try
            {
                SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(_options.JwtSecretKey));
                TokenValidationParameters validationParameters = new()
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidIssuer = _options.JwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = _options.JwtAudience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                ClaimsPrincipal principal = _tokenHandler.ValidateToken(token, validationParameters, out SecurityToken? validatedToken);
                JwtSecurityToken? jwtToken = validatedToken as JwtSecurityToken;

                return Task.FromResult(new TokenValidationResult
                {
                    IsValid = true,
                    Username = principal.Identity?.Name,
                    Roles = principal.Claims
                        .Where(c => c.Type == ClaimTypes.Role)
                        .Select(c => c.Value)
                        .ToArray(),
                    ExpiresAt = jwtToken?.ValidTo
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new TokenValidationResult
                {
                    IsValid = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        public Task RevokeTokenAsync(string token)
        {
            // In production, you would maintain a blacklist of revoked tokens
            // For now, we'll just return completed task
            return Task.CompletedTask;
        }

        public Task<bool> ChangePasswordAsync(string username, string currentPassword, string newPassword)
        {
            // Check if it's the admin user
            if (username == _options.AdminUsername && currentPassword == _options.AdminPassword)
            {
                // In a real implementation, you would persist this change
                _options.AdminPassword = newPassword;
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public Task<IEnumerable<UserSession>> GetActiveSessionsAsync()
        {
            // Clean up expired sessions
            DateTime now = DateTime.UtcNow;
            List<string> expiredSessions = _sessions
                .Where(s => s.Value.ExpiresAt < now)
                .Select(s => s.Key)
                .ToList();

            foreach (var sessionId in expiredSessions)
            {
                _sessions.TryRemove(sessionId, out _);
            }

            return Task.FromResult(_sessions.Values.AsEnumerable());
        }

        public Task<bool> TerminateSessionAsync(string sessionId)
        {
            return Task.FromResult(_sessions.TryRemove(sessionId, out _));
        }

        private (string token, string refreshToken, DateTime expiresAt) GenerateTokens(UserInfo user)
        {
            SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(_options.JwtSecretKey));
            SigningCredentials credentials = new(key, SecurityAlgorithms.HmacSha256);
            DateTime expiresAt = DateTime.UtcNow.Add(_options.SessionTimeout);

            List<Claim> claims =
            [
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Email, user.Email)
            ];

            foreach (var role in user.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            JwtSecurityToken token = new(
                issuer: _options.JwtIssuer,
                audience: _options.JwtAudience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials
            );

            var tokenString = _tokenHandler.WriteToken(token);

            // Generate refresh token
            var refreshToken = GenerateRefreshToken();
            _refreshTokens[refreshToken] = user.Username;

            return (tokenString, refreshToken, expiresAt);
        }

        private string GenerateRefreshToken()
        {
            var randomBytes = new byte[32];
            using RandomNumberGenerator rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }
    }
}