using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Zetian.Abstractions;
using Zetian.Models;

namespace Zetian.Authentication
{
    /// <summary>
    /// Implements LOGIN authentication mechanism
    /// </summary>
    public class LoginAuthenticator(AuthenticationHandler? handler = null) : IAuthenticator
    {
        public string Mechanism => "LOGIN";

        public async Task<AuthenticationResult> AuthenticateAsync(
            ISmtpSession session,
            string? initialResponse,
            StreamReader reader,
            StreamWriter writer,
            CancellationToken cancellationToken)
        {
            try
            {
                // Send username prompt
                string usernamePrompt = Convert.ToBase64String(Encoding.ASCII.GetBytes("Username:"));
                await writer.WriteLineAsync($"334 {usernamePrompt}").ConfigureAwait(false);
                await writer.FlushAsync().ConfigureAwait(false);

                // Read username
                string? usernameResponse = await reader.ReadLineAsync().ConfigureAwait(false);
                if (usernameResponse == "*")
                {
                    return AuthenticationResult.Fail("Authentication cancelled");
                }

                if (string.IsNullOrWhiteSpace(usernameResponse))
                {
                    return AuthenticationResult.Fail("Invalid username");
                }

                string username;
                try
                {
                    username = Encoding.ASCII.GetString(Convert.FromBase64String(usernameResponse));
                }
                catch
                {
                    return AuthenticationResult.Fail("Invalid username encoding");
                }

                // Send password prompt
                string passwordPrompt = Convert.ToBase64String(Encoding.ASCII.GetBytes("Password:"));
                await writer.WriteLineAsync($"334 {passwordPrompt}").ConfigureAwait(false);
                await writer.FlushAsync().ConfigureAwait(false);

                // Read password
                string? passwordResponse = await reader.ReadLineAsync().ConfigureAwait(false);
                if (passwordResponse == "*")
                {
                    return AuthenticationResult.Fail("Authentication cancelled");
                }

                if (string.IsNullOrWhiteSpace(passwordResponse))
                {
                    return AuthenticationResult.Fail("Invalid password");
                }

                string password;
                try
                {
                    password = Encoding.ASCII.GetString(Convert.FromBase64String(passwordResponse));
                }
                catch
                {
                    return AuthenticationResult.Fail("Invalid password encoding");
                }

                if (handler != null)
                {
                    return await handler(username, password).ConfigureAwait(false);
                }

                // Default: accept any non-empty credentials
                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                {
                    return AuthenticationResult.Succeed(username);
                }

                return AuthenticationResult.Fail("Invalid credentials");
            }
            catch (Exception ex)
            {
                return AuthenticationResult.Fail($"Authentication error: {ex.Message}");
            }
        }
    }
}