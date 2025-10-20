using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Zetian.Core;

namespace Zetian.Authentication
{
    /// <summary>
    /// Implements PLAIN authentication mechanism
    /// </summary>
    public class PlainAuthenticator : IAuthenticator
    {
        private readonly AuthenticationHandler? _handler;

        public PlainAuthenticator(AuthenticationHandler? handler = null)
        {
            _handler = handler;
        }

        public string Mechanism => "PLAIN";

        public async Task<AuthenticationResult> AuthenticateAsync(
            ISmtpSession session,
            string? initialResponse,
            StreamReader reader,
            StreamWriter writer,
            CancellationToken cancellationToken)
        {
            string? response = initialResponse;

            if (string.IsNullOrWhiteSpace(response))
            {
                // Request credentials
                await writer.WriteLineAsync("334 ").ConfigureAwait(false);
                await writer.FlushAsync().ConfigureAwait(false);

                response = await reader.ReadLineAsync().ConfigureAwait(false);
                if (response == "*")
                {
                    // Authentication cancelled
                    return AuthenticationResult.Fail("Authentication cancelled");
                }
            }

            if (string.IsNullOrWhiteSpace(response))
            {
                return AuthenticationResult.Fail("Invalid response");
            }

            try
            {
                // Decode base64 response
                byte[] bytes = Convert.FromBase64String(response);
                string decoded = Encoding.ASCII.GetString(bytes);

                // PLAIN format: [authzid]\0authcid\0password
                string[] parts = decoded.Split('\0');

                if (parts.Length < 2)
                {
                    return AuthenticationResult.Fail("Invalid PLAIN authentication format");
                }

                string? username;
                string? password;

                if (parts.Length == 2)
                {
                    // No authorization identity
                    username = parts[0];
                    password = parts[1];
                }
                else
                {
                    // Has authorization identity (we'll ignore it for simplicity)
                    username = parts[1];
                    password = parts[2];
                }

                if (_handler != null)
                {
                    return await _handler(username, password).ConfigureAwait(false);
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