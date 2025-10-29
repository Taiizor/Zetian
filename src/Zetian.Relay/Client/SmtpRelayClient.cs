using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Zetian.Abstractions;
using Zetian.Protocol;
using Zetian.Relay.Abstractions;
using Zetian.Relay.Models;

namespace Zetian.Relay.Client
{
    /// <summary>
    /// SMTP client for relaying messages to remote servers
    /// </summary>
    public class SmtpRelayClient(ILogger<SmtpRelayClient>? logger = null) : ISmtpClient
    {
        private readonly ILogger<SmtpRelayClient> _logger = logger ?? NullLogger<SmtpRelayClient>.Instance;
        private TcpClient? _tcpClient;
        private Stream? _stream;
        private StreamReader? _reader;
        private StreamWriter? _writer;
        private bool _disposed;
        private Dictionary<string, string>? _serverCapabilities;

        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 25;
        public bool EnableSsl { get; set; }
        public SslProtocols SslProtocols { get; set; } = SslProtocols.Tls12 | SslProtocols.Tls13;
        public X509Certificate2? ClientCertificate { get; set; }
        public NetworkCredential? Credentials { get; set; }
        public string? LocalDomain { get; set; }
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);
        public bool IsConnected => _tcpClient?.Connected ?? false;
        public IReadOnlyDictionary<string, string>? ServerCapabilities => _serverCapabilities;

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (IsConnected)
            {
                _logger.LogWarning("Already connected to {Host}:{Port}", Host, Port);
                return;
            }

            try
            {
                _logger.LogInformation("Connecting to {Host}:{Port}", Host, Port);

                _tcpClient = new TcpClient();

                using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(Timeout);

                await _tcpClient.ConnectAsync(Host, Port).ConfigureAwait(false);

                _stream = _tcpClient.GetStream();

                if (EnableSsl)
                {
                    await UpgradeToSslAsync(cts.Token).ConfigureAwait(false);
                }

                _reader = new StreamReader(_stream, Encoding.ASCII);
                _writer = new StreamWriter(_stream, Encoding.ASCII) { AutoFlush = true };

                // Read greeting
                SmtpResponse greeting = await ReadResponseAsync(cts.Token).ConfigureAwait(false);
                if (!greeting.IsSuccess)
                {
                    throw new InvalidOperationException($"Server greeting failed: {greeting.Message}");
                }

                // Send EHLO
                await SendEhloAsync(cts.Token).ConfigureAwait(false);

                _logger.LogInformation("Connected to {Host}:{Port}", Host, Port);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to {Host}:{Port}", Host, Port);
                Cleanup();
                throw;
            }
        }

        public async Task AuthenticateAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            EnsureConnected();

            if (Credentials == null)
            {
                _logger.LogDebug("No credentials provided, skipping authentication");
                return;
            }

            try
            {
                using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(Timeout);

                // Try AUTH PLAIN first
                if (_serverCapabilities?.ContainsKey("AUTH") == true &&
                    _serverCapabilities["AUTH"].Contains("PLAIN", StringComparison.OrdinalIgnoreCase))
                {
                    await AuthPlainAsync(cts.Token).ConfigureAwait(false);
                }
                // Try AUTH LOGIN
                else if (_serverCapabilities?.ContainsKey("AUTH") == true &&
                         _serverCapabilities["AUTH"].Contains("LOGIN", StringComparison.OrdinalIgnoreCase))
                {
                    await AuthLoginAsync(cts.Token).ConfigureAwait(false);
                }
                else
                {
                    throw new InvalidOperationException("Server does not support authentication");
                }

                _logger.LogInformation("Authenticated as {Username}", Credentials.UserName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Authentication failed");
                throw;
            }
        }

        public async Task<SmtpDeliveryResult> SendAsync(
            ISmtpMessage message,
            CancellationToken cancellationToken = default)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            List<string> recipients = message.Recipients.Select(r => r.Address).ToList();
            return await SendAsync(message, recipients, cancellationToken).ConfigureAwait(false);
        }

        public async Task<SmtpDeliveryResult> SendAsync(
            ISmtpMessage message,
            IEnumerable<string> recipients,
            CancellationToken cancellationToken = default)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (recipients == null)
            {
                throw new ArgumentNullException(nameof(recipients));
            }

            string from = message.From?.Address ?? "<>";
            byte[] rawData = await message.GetRawDataAsync().ConfigureAwait(false);

            return await SendRawAsync(from, recipients, rawData, cancellationToken).ConfigureAwait(false);
        }

        public async Task<SmtpDeliveryResult> SendRawAsync(
            string from,
            IEnumerable<string> recipients,
            byte[] messageData,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            EnsureConnected();

            if (string.IsNullOrWhiteSpace(from))
            {
                from = "<>";
            }

            List<string> recipientList = recipients?.ToList() ?? [];
            if (recipientList.Count == 0)
            {
                return SmtpDeliveryResult.CreateFailure("No recipients specified", 550);
            }

            try
            {
                using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(Timeout);

                // MAIL FROM
                await SendCommandAsync($"MAIL FROM:<{from}>", cts.Token).ConfigureAwait(false);
                SmtpResponse mailResponse = await ReadResponseAsync(cts.Token).ConfigureAwait(false);
                if (!mailResponse.IsSuccess)
                {
                    return SmtpDeliveryResult.CreateFailure(
                        mailResponse.Message ?? "MAIL FROM rejected",
                        mailResponse.Code);
                }

                // RCPT TO for each recipient
                List<string> acceptedRecipients = [];
                Dictionary<string, string> rejectedRecipients = [];

                foreach (string recipient in recipientList)
                {
                    await SendCommandAsync($"RCPT TO:<{recipient}>", cts.Token).ConfigureAwait(false);
                    SmtpResponse rcptResponse = await ReadResponseAsync(cts.Token).ConfigureAwait(false);

                    if (rcptResponse.IsSuccess)
                    {
                        acceptedRecipients.Add(recipient);
                    }
                    else
                    {
                        rejectedRecipients[recipient] = rcptResponse.Message ?? "Recipient rejected";
                        _logger.LogWarning("Recipient {Recipient} rejected: {Message}",
                            recipient, rcptResponse.Message);
                    }
                }

                if (acceptedRecipients.Count == 0)
                {
                    // All recipients rejected, reset and return failure
                    await ResetAsync(cts.Token).ConfigureAwait(false);
                    return SmtpDeliveryResult.CreateFailure("All recipients rejected", 550);
                }

                // DATA
                await SendCommandAsync("DATA", cts.Token).ConfigureAwait(false);
                SmtpResponse dataResponse = await ReadResponseAsync(cts.Token).ConfigureAwait(false);
                if (!dataResponse.IsPositiveIntermediate)
                {
                    await ResetAsync(cts.Token).ConfigureAwait(false);
                    return SmtpDeliveryResult.CreateFailure(
                        dataResponse.Message ?? "DATA command rejected",
                        dataResponse.Code);
                }

                // Send message data
                await SendDataAsync(messageData, cts.Token).ConfigureAwait(false);

                // Read final response
                SmtpResponse finalResponse = await ReadResponseAsync(cts.Token).ConfigureAwait(false);

                if (finalResponse.IsSuccess)
                {
                    // Extract transaction ID if available
                    string? transactionId = null;
                    if (!string.IsNullOrEmpty(finalResponse.Message))
                    {
                        string[] parts = finalResponse.Message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 1)
                        {
                            transactionId = parts[^1];
                        }
                    }

                    if (rejectedRecipients.Count > 0)
                    {
                        // Partial success
                        return SmtpDeliveryResult.CreatePartial(acceptedRecipients, rejectedRecipients);
                    }
                    else
                    {
                        // Full success
                        return SmtpDeliveryResult.CreateSuccess(acceptedRecipients, transactionId);
                    }
                }
                else
                {
                    return SmtpDeliveryResult.CreateFailure(
                        finalResponse.Message ?? "Message rejected",
                        finalResponse.Code,
                        finalResponse.IsTransientNegative);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
                return SmtpDeliveryResult.CreateFailure(ex.Message, 451, true);
            }
        }

        public async Task<bool> VerifyAsync(string address, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            EnsureConnected();

            try
            {
                using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(Timeout);

                await SendCommandAsync($"VRFY {address}", cts.Token).ConfigureAwait(false);
                SmtpResponse response = await ReadResponseAsync(cts.Token).ConfigureAwait(false);

                return response.IsSuccess;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying address {Address}", address);
                return false;
            }
        }

        public async Task NoOpAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            EnsureConnected();

            using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(30));

            await SendCommandAsync("NOOP", cts.Token).ConfigureAwait(false);
            await ReadResponseAsync(cts.Token).ConfigureAwait(false);
        }

        public async Task ResetAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            EnsureConnected();

            using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(30));

            await SendCommandAsync("RSET", cts.Token).ConfigureAwait(false);
            await ReadResponseAsync(cts.Token).ConfigureAwait(false);
        }

        public async Task DisconnectAsync(bool quit = true, CancellationToken cancellationToken = default)
        {
            if (!IsConnected)
            {
                return;
            }

            try
            {
                if (quit && _writer != null)
                {
                    using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(TimeSpan.FromSeconds(10));

                    await SendCommandAsync("QUIT", cts.Token).ConfigureAwait(false);
                    await ReadResponseAsync(cts.Token).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error during disconnect");
            }
            finally
            {
                Cleanup();
            }
        }

        private async Task SendEhloAsync(CancellationToken cancellationToken)
        {
            string domain = LocalDomain ?? Dns.GetHostName();
            await SendCommandAsync($"EHLO {domain}", cancellationToken).ConfigureAwait(false);

            SmtpResponse response = await ReadMultilineResponseAsync(cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccess)
            {
                // Try HELO if EHLO fails
                _logger.LogWarning("EHLO failed, trying HELO");
                await SendCommandAsync($"HELO {domain}", cancellationToken).ConfigureAwait(false);
                response = await ReadResponseAsync(cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccess)
                {
                    throw new InvalidOperationException($"HELO/EHLO failed: {response.Message}");
                }
            }
            else
            {
                // Parse capabilities from EHLO response
                _serverCapabilities = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (string? line in response.Lines.Skip(1))
                {
                    string[] parts = line.Split(' ', 2);
                    _serverCapabilities[parts[0]] = parts.Length > 1 ? parts[1] : string.Empty;
                }
            }
        }

        private async Task UpgradeToSslAsync(CancellationToken cancellationToken)
        {
            SslStream sslStream = new(_stream, false, ValidateServerCertificate);

            await sslStream.AuthenticateAsClientAsync(
                Host,
                ClientCertificate != null ? [ClientCertificate] : null,
                SslProtocols,
                true).ConfigureAwait(false);

            _stream = sslStream;
            _logger.LogDebug("SSL/TLS connection established");
        }

        private async Task AuthPlainAsync(CancellationToken cancellationToken)
        {
            if (Credentials == null)
            {
                throw new InvalidOperationException("Credentials not set");
            }

            string authString = $"\0{Credentials.UserName}\0{Credentials.Password}";
            byte[] authBytes = Encoding.ASCII.GetBytes(authString);
            string authBase64 = Convert.ToBase64String(authBytes);

            await SendCommandAsync($"AUTH PLAIN {authBase64}", cancellationToken).ConfigureAwait(false);
            SmtpResponse response = await ReadResponseAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccess)
            {
                throw new InvalidOperationException($"Authentication failed: {response.Message}");
            }
        }

        private async Task AuthLoginAsync(CancellationToken cancellationToken)
        {
            if (Credentials == null)
            {
                throw new InvalidOperationException("Credentials not set");
            }

            await SendCommandAsync("AUTH LOGIN", cancellationToken).ConfigureAwait(false);
            SmtpResponse response = await ReadResponseAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsPositiveIntermediate)
            {
                throw new InvalidOperationException($"AUTH LOGIN failed: {response.Message}");
            }

            // Send username
            string usernameBase64 = Convert.ToBase64String(Encoding.ASCII.GetBytes(Credentials.UserName));
            await SendCommandAsync(usernameBase64, cancellationToken).ConfigureAwait(false);
            response = await ReadResponseAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsPositiveIntermediate)
            {
                throw new InvalidOperationException($"Username rejected: {response.Message}");
            }

            // Send password
            string passwordBase64 = Convert.ToBase64String(Encoding.ASCII.GetBytes(Credentials.Password));
            await SendCommandAsync(passwordBase64, cancellationToken).ConfigureAwait(false);
            response = await ReadResponseAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccess)
            {
                throw new InvalidOperationException($"Authentication failed: {response.Message}");
            }
        }

        private async Task SendDataAsync(byte[] data, CancellationToken cancellationToken)
        {
            using MemoryStream ms = new(data);
            using StreamReader reader = new(ms);

            string? line;
            while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
            {
                // Apply dot-stuffing
                if (line.StartsWith('.'))
                {
                    line = "." + line;
                }

                await _writer!.WriteLineAsync(line).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
            }

            // Send terminating sequence
            await _writer!.WriteLineAsync(".").ConfigureAwait(false);
            await _writer.FlushAsync().ConfigureAwait(false);
        }

        private async Task SendCommandAsync(string command, CancellationToken cancellationToken)
        {
            _logger.LogDebug("C: {Command}", command.Contains("AUTH") ? "AUTH ***" : command);

            await _writer!.WriteLineAsync(command).ConfigureAwait(false);
            await _writer.FlushAsync().ConfigureAwait(false);
        }

        private async Task<SmtpResponse> ReadResponseAsync(CancellationToken cancellationToken)
        {
            string? line = await _reader!.ReadLineAsync().ConfigureAwait(false);

            if (string.IsNullOrEmpty(line))
            {
                throw new InvalidOperationException("Empty response from server");
            }

            _logger.LogDebug("S: {Response}", line);

            if (line.Length < 3 || !int.TryParse(line[..3], out int code))
            {
                throw new InvalidOperationException($"Invalid response format: {line}");
            }

            string message = line.Length > 4 ? line[4..] : string.Empty;
            return new SmtpResponse(code, message);
        }

        private async Task<SmtpResponse> ReadMultilineResponseAsync(CancellationToken cancellationToken)
        {
            List<string> lines = [];
            int code = 0;

            while (true)
            {
                string? line = await _reader!.ReadLineAsync().ConfigureAwait(false);

                if (string.IsNullOrEmpty(line))
                {
                    throw new InvalidOperationException("Empty response from server");
                }

                _logger.LogDebug("S: {Response}", line);

                if (line.Length < 3 || !int.TryParse(line[..3], out int currentCode))
                {
                    throw new InvalidOperationException($"Invalid response format: {line}");
                }

                if (code == 0)
                {
                    code = currentCode;
                }
                else if (code != currentCode)
                {
                    throw new InvalidOperationException($"Inconsistent response code: {line}");
                }

                bool hasMore = line.Length > 3 && line[3] == '-';
                string message = line.Length > 4 ? line[4..] : string.Empty;
                lines.Add(message);

                if (!hasMore)
                {
                    break;
                }
            }

            return new SmtpResponse(code, [.. lines]);
        }

        private bool ValidateServerCertificate(
            object sender,
            X509Certificate? certificate,
            X509Chain? chain,
            SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }

            _logger.LogWarning("SSL certificate validation error: {Errors}", sslPolicyErrors);

            // You might want to make this configurable
            return false;
        }

        private void EnsureConnected()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Not connected to SMTP server");
            }
        }

        private void ThrowIfDisposed()
        {
#if NET6_0
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
#else
            ObjectDisposedException.ThrowIf(_disposed, this);
#endif
        }

        private void Cleanup()
        {
            _reader?.Dispose();
            _writer?.Dispose();
            _stream?.Dispose();
            _tcpClient?.Dispose();

            _reader = null;
            _writer = null;
            _stream = null;
            _tcpClient = null;
            _serverCapabilities = null;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            try
            {
                DisconnectAsync(false).GetAwaiter().GetResult();
            }
            catch
            {
                // Ignore errors during dispose
            }

            Cleanup();
        }
    }
}