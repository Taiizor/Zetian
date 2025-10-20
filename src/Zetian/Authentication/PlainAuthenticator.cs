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
            PipeReader reader,
            PipeWriter writer,
            CancellationToken cancellationToken)
        {
            string? response = initialResponse;

            if (string.IsNullOrWhiteSpace(response))
            {
                // Request credentials
                byte[] prompt = Encoding.ASCII.GetBytes("334 \r\n");
                await writer.WriteAsync(prompt.AsMemory(), cancellationToken).ConfigureAwait(false);
                await writer.FlushAsync(cancellationToken).ConfigureAwait(false);

                response = await ReadLineAsync(reader, cancellationToken).ConfigureAwait(false);
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