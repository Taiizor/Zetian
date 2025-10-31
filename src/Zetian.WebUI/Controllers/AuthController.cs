using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Zetian.WebUI.Services;

namespace Zetian.WebUI.Controllers
{
    /// <summary>
    /// API controller for authentication
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class AuthController : ControllerBase
    {
        private readonly IAuthenticationService _authService;

        public AuthController(IAuthenticationService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Login with username and password
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            AuthenticationResult result = await _authService.AuthenticateAsync(request.Username, request.Password);

            if (result.Success)
            {
                return Ok(new
                {
                    token = result.Token,
                    refreshToken = result.RefreshToken,
                    expiresAt = result.ExpiresAt,
                    user = result.User
                });
            }

            return Unauthorized(new { error = result.ErrorMessage ?? "Invalid credentials" });
        }

        /// <summary>
        /// Login with API key
        /// </summary>
        [HttpPost("api-key")]
        public async Task<IActionResult> LoginWithApiKey([FromHeader(Name = "X-API-Key")] string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return BadRequest(new { error = "API key is required" });
            }

            AuthenticationResult result = await _authService.AuthenticateWithApiKeyAsync(apiKey);

            if (result.Success)
            {
                return Ok(new
                {
                    token = result.Token,
                    expiresAt = result.ExpiresAt,
                    user = result.User
                });
            }

            return Unauthorized(new { error = result.ErrorMessage ?? "Invalid API key" });
        }

        /// <summary>
        /// Refresh authentication token
        /// </summary>
        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            AuthenticationResult result = await _authService.RefreshTokenAsync(request.RefreshToken);

            if (result.Success)
            {
                return Ok(new
                {
                    token = result.Token,
                    refreshToken = result.RefreshToken,
                    expiresAt = result.ExpiresAt
                });
            }

            return Unauthorized(new { error = result.ErrorMessage ?? "Invalid refresh token" });
        }

        /// <summary>
        /// Logout
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            string token = Request.Headers["Authorization"]
                .ToString()
                .Replace("Bearer ", "");

            if (!string.IsNullOrWhiteSpace(token))
            {
                await _authService.RevokeTokenAsync(token);
            }

            return Ok(new { message = "Logged out successfully" });
        }

        /// <summary>
        /// Validate token
        /// </summary>
        [HttpGet("validate")]
        public async Task<IActionResult> ValidateToken([FromHeader(Name = "Authorization")] string authorization)
        {
            if (string.IsNullOrWhiteSpace(authorization))
            {
                return BadRequest(new { error = "Authorization header is required" });
            }

            string token = authorization.Replace("Bearer ", "");
            TokenValidationResult result = await _authService.ValidateTokenAsync(token);

            if (result.IsValid)
            {
                return Ok(new
                {
                    valid = true,
                    username = result.Username,
                    roles = result.Roles,
                    expiresAt = result.ExpiresAt
                });
            }

            return Ok(new
            {
                valid = false,
                error = result.ErrorMessage
            });
        }

        /// <summary>
        /// Change password
        /// </summary>
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            string? username = User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(username))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            bool result = await _authService.ChangePasswordAsync(
                username,
                request.CurrentPassword,
                request.NewPassword);

            if (result)
            {
                return Ok(new { message = "Password changed successfully" });
            }

            return BadRequest(new { error = "Failed to change password" });
        }

        /// <summary>
        /// Get active sessions
        /// </summary>
        [HttpGet("sessions")]
        [Authorize]
        public async Task<IActionResult> GetSessions()
        {
            IEnumerable<UserSession> sessions = await _authService.GetActiveSessionsAsync();
            return Ok(sessions);
        }

        /// <summary>
        /// Terminate a session
        /// </summary>
        [HttpDelete("sessions/{sessionId}")]
        [Authorize]
        public async Task<IActionResult> TerminateSession(string sessionId)
        {
            bool result = await _authService.TerminateSessionAsync(sessionId);

            if (result)
            {
                return Ok(new { message = "Session terminated successfully" });
            }

            return NotFound(new { error = "Session not found" });
        }
    }

    /// <summary>
    /// Login request
    /// </summary>
    public class LoginRequest
    {
        [Required]
        public string Username { get; set; } = "";

        [Required]
        public string Password { get; set; } = "";
    }

    /// <summary>
    /// Refresh token request
    /// </summary>
    public class RefreshTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; } = "";
    }

    /// <summary>
    /// Change password request
    /// </summary>
    public class ChangePasswordRequest
    {
        [Required]
        public string CurrentPassword { get; set; } = "";

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = "";

        [Required]
        [Compare("NewPassword")]
        public string ConfirmPassword { get; set; } = "";
    }
}