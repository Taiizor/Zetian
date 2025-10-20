using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Zetian.Core;

namespace Zetian.Authentication
{
    /// <summary>
    /// Implements LOGIN authentication mechanism
    /// </summary>
    public class LoginAuthenticator : IAuthenticator
    {
        private readonly AuthenticationHandler? _handler;

        public LoginAuthenticator(AuthenticationHandler? handler = null)
        {
            _handler = handler;
        }

        public string Mechanism => "LOGIN";

        public async Task<AuthenticationResult> AuthenticateAsync(
            ISmtpSession session,
            string? initialResponse,
            PipeReader reader,
            PipeWriter writer,
            CancellationToken cancellationToken)
        {
            try
            {
                // Send username prompt
                string usernamePrompt = Convert.ToBase64String(Encoding.ASCII.GetBytes("Username:"));
                byte[] prompt = Encoding.ASCII.GetBytes($"334 {usernamePrompt}\r\n");
                await writer.WriteAsync(prompt.AsMemory(), cancellationToken).ConfigureAwait(false);
                await writer.FlushAsync(cancellationToken).ConfigureAwait(false);

                // Read username
                string? usernameResponse = await ReadLineAsync(reader, cancellationToken).ConfigureAwait(false);
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
                byte[] passwordPromptBytes = Encoding.ASCII.GetBytes($"334 {passwordPrompt}\r\n");
                await writer.WriteAsync(passwordPromptBytes.AsMemory(), cancellationToken).ConfigureAwait(false);
                await writer.FlushAsync(cancellationToken).ConfigureAwait(false);

                // Read password
                string? passwordResponse = await ReadLineAsync(reader, cancellationToken).ConfigureAwait(false);
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

        private async Task<string?> ReadLineAsync(PipeReader reader, CancellationToken cancellationToken)
        {
            StringBuilder lineBuilder = new();

            while (true)
            {
                ReadResult result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                ReadOnlySequence<byte> buffer = result.Buffer;

                SequencePosition? position = buffer.PositionOf((byte)'\n');

                if (position != null)
                {
                    // Process the line
                    ReadOnlySequence<byte> line = buffer.Slice(0, position.Value);

                    // Remove \r if present
                    if (!line.IsEmpty)
                    {
                        byte[] lineBytes = ArrayPool<byte>.Shared.Rent((int)line.Length);
                        try
                        {
                            line.CopyTo(lineBytes);
                            int length = (int)line.Length;
                            if (length > 0 && lineBytes[length - 1] == '\r')
                            {
                                length--;
                            }
                            lineBuilder.Append(Encoding.ASCII.GetString(lineBytes, 0, length));
                        }
                        finally
                        {
                            ArrayPool<byte>.Shared.Return(lineBytes);
                        }
                    }

                    string completeLine = lineBuilder.ToString();

                    // Skip the line ending
                    buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
                    reader.AdvanceTo(buffer.Start);

                    return completeLine;
                }

                // Tell the PipeReader how much of the buffer we've consumed
                reader.AdvanceTo(buffer.Start, buffer.End);

                // If we've completed reading, return null
                if (result.IsCompleted)
                {
                    return lineBuilder.Length > 0 ? lineBuilder.ToString() : null;
                }
            }
        }
    }
}