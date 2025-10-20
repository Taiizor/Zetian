using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Zetian.Authentication;
using Zetian.Configuration;
using Zetian.Core;
using Zetian.Protocol;

namespace Zetian.Internal
{
    internal class SmtpSession : ISmtpSession
    {
        private readonly SmtpServer _server;
        private readonly TcpClient _client;
        private readonly SmtpServerConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly Dictionary<string, object> _properties;

        private Stream _stream;
        private PipeReader _reader;
        private PipeWriter _writer;
        private SmtpMessage? _currentMessage;
        private readonly Pipe _pipe;
        private string? _mailFrom;
        private readonly List<string> _recipients;
        private SmtpSessionState _state;
        private bool _disposed;

        public SmtpSession(SmtpServer server, TcpClient client, SmtpServerConfiguration configuration, ILogger logger)
        {
            _server = server ?? throw new ArgumentNullException(nameof(server));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            Id = Guid.NewGuid().ToString("N");
            StartTime = DateTime.UtcNow;
            RemoteEndPoint = _client.Client.RemoteEndPoint ?? new IPEndPoint(IPAddress.None, 0);
            LocalEndPoint = _client.Client.LocalEndPoint ?? new IPEndPoint(IPAddress.None, 0);

            _properties = new Dictionary<string, object>();
            _recipients = new List<string>();
            _state = SmtpSessionState.Connected;

            MaxMessageSize = _configuration.MaxMessageSize;
            PipeliningEnabled = _configuration.EnablePipelining;
            EightBitMimeEnabled = _configuration.Enable8BitMime;
            BinaryMimeEnabled = _configuration.EnableBinaryMime;

            _stream = _client.GetStream();

            // Create a pipe for bidirectional communication
            PipeOptions options = new(pauseWriterThreshold: _configuration.WriteBufferSize,
                                     resumeWriterThreshold: _configuration.ReadBufferSize,
                                     pool: MemoryPool<byte>.Shared);
            _pipe = new Pipe(options);

            _reader = PipeReader.Create(_stream, new StreamPipeReaderOptions(bufferSize: _configuration.ReadBufferSize));
            _writer = PipeWriter.Create(_stream, new StreamPipeWriterOptions());
        }

        public string Id { get; }
        public EndPoint RemoteEndPoint { get; }
        public EndPoint LocalEndPoint { get; }
        public bool IsSecure { get; private set; }
        public bool IsAuthenticated { get; private set; }
        public string? AuthenticatedIdentity { get; private set; }
        public string? ClientDomain { get; private set; }
        public DateTime StartTime { get; }
        public IDictionary<string, object> Properties => _properties;
        public X509Certificate2? ClientCertificate { get; private set; }
        public int MessageCount { get; private set; }
        public bool PipeliningEnabled { get; set; }
        public bool EightBitMimeEnabled { get; set; }
        public bool BinaryMimeEnabled { get; set; }
        public long MaxMessageSize { get; set; }

        public async Task HandleAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Send greeting
                await SendGreetingAsync().ConfigureAwait(false);

                // Process commands
                while (!cancellationToken.IsCancellationRequested && _client.Connected && _state != SmtpSessionState.Quit)
                {
                    try
                    {
                        string? line = await ReadLineAsync(cancellationToken).ConfigureAwait(false);
                        if (line == null)
                        {
                            break;
                        }

                        if (_configuration.EnableVerboseLogging)
                        {
                            _logger.LogDebug("C: {Command}", line);
                        }

                        await ProcessCommandAsync(line, cancellationToken).ConfigureAwait(false);
                    }
                    catch (IOException)
                    {
                        // Connection lost
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing command");
                        await SendResponseAsync(SmtpResponse.LocalError).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Session error");
            }
            finally
            {
                await CloseAsync().ConfigureAwait(false);
            }
        }

        private async Task SendGreetingAsync()
        {
            string greeting = _configuration.Greeting ?? $"{_configuration.ServerName} ESMTP Zetian";
            SmtpResponse response = new(220, greeting);
            await SendResponseAsync(response).ConfigureAwait(false);
        }

        private async Task ProcessCommandAsync(string commandLine, CancellationToken cancellationToken)
        {
            if (!SmtpCommand.TryParse(commandLine, out SmtpCommand? command) || command == null)
            {
                await SendResponseAsync(SmtpResponse.SyntaxError).ConfigureAwait(false);
                return;
            }

            switch (command.Verb)
            {
                case SmtpCommand.Commands.HELO:
                    await ProcessHeloAsync(command).ConfigureAwait(false);
                    break;

                case SmtpCommand.Commands.EHLO:
                    await ProcessEhloAsync(command).ConfigureAwait(false);
                    break;

                case SmtpCommand.Commands.MAIL:
                    await ProcessMailFromAsync(command).ConfigureAwait(false);
                    break;

                case SmtpCommand.Commands.RCPT:
                    await ProcessRcptToAsync(command).ConfigureAwait(false);
                    break;

                case SmtpCommand.Commands.DATA:
                    await ProcessDataAsync(cancellationToken).ConfigureAwait(false);
                    break;

                case SmtpCommand.Commands.RSET:
                    await ProcessRsetAsync().ConfigureAwait(false);
                    break;

                case SmtpCommand.Commands.QUIT:
                    await ProcessQuitAsync().ConfigureAwait(false);
                    break;

                case SmtpCommand.Commands.NOOP:
                    await SendResponseAsync(SmtpResponse.Ok).ConfigureAwait(false);
                    break;

                case SmtpCommand.Commands.VRFY:
                    await SendResponseAsync(SmtpResponse.CannotVerifyUser).ConfigureAwait(false);
                    break;

                case SmtpCommand.Commands.STARTTLS:
                    await ProcessStartTlsAsync(cancellationToken).ConfigureAwait(false);
                    break;

                case SmtpCommand.Commands.AUTH:
                    await ProcessAuthAsync(command, cancellationToken).ConfigureAwait(false);
                    break;

                default:
                    await SendResponseAsync(SmtpResponse.CommandNotImplemented).ConfigureAwait(false);
                    break;
            }
        }

        private async Task ProcessHeloAsync(SmtpCommand command)
        {
            if (string.IsNullOrWhiteSpace(command.Argument))
            {
                await SendResponseAsync(SmtpResponse.SyntaxErrorInParameters).ConfigureAwait(false);
                return;
            }

            ClientDomain = command.Argument;
            _state = SmtpSessionState.Hello;
            ResetMessage();

            SmtpResponse response = new(250, $"{_configuration.ServerName} Hello {ClientDomain}");
            await SendResponseAsync(response).ConfigureAwait(false);
        }

        private async Task ProcessEhloAsync(SmtpCommand command)
        {
            if (string.IsNullOrWhiteSpace(command.Argument))
            {
                await SendResponseAsync(SmtpResponse.SyntaxErrorInParameters).ConfigureAwait(false);
                return;
            }

            ClientDomain = command.Argument;
            _state = SmtpSessionState.Hello;
            ResetMessage();

            List<string> lines = new()
            {
                $"{_configuration.ServerName} Hello {ClientDomain}"
            };

            // Add supported extensions
            if (_configuration.EnableSizeExtension)
            {
                lines.Add($"SIZE {_configuration.MaxMessageSize}");
            }

            if (_configuration.EnablePipelining)
            {
                lines.Add("PIPELINING");
            }

            if (_configuration.Enable8BitMime)
            {
                lines.Add("8BITMIME");
            }

            if (_configuration.EnableBinaryMime)
            {
                lines.Add("BINARYMIME");
            }

            if (_configuration.EnableChunking)
            {
                lines.Add("CHUNKING");
            }

            if (_configuration.Certificate != null && !IsSecure)
            {
                lines.Add("STARTTLS");
            }

            if (_configuration.AuthenticationMechanisms.Count > 0)
            {
                string authMechanisms = string.Join(" ", _configuration.AuthenticationMechanisms);
                lines.Add($"AUTH {authMechanisms}");
            }

            lines.Add("ENHANCEDSTATUSCODES");
            lines.Add("HELP");

            SmtpResponse response = new(250, lines.ToArray());
            await SendResponseAsync(response).ConfigureAwait(false);
        }

        private async Task ProcessMailFromAsync(SmtpCommand command)
        {
            if (_state < SmtpSessionState.Hello)
            {
                await SendResponseAsync(SmtpResponse.BadSequence).ConfigureAwait(false);
                return;
            }

            if (_configuration.RequireAuthentication && !IsAuthenticated)
            {
                await SendResponseAsync(SmtpResponse.AuthenticationRequired).ConfigureAwait(false);
                return;
            }

            if (string.IsNullOrWhiteSpace(command.Argument) || !command.Argument.StartsWith("FROM:", StringComparison.OrdinalIgnoreCase))
            {
                await SendResponseAsync(SmtpResponse.SyntaxErrorInParameters).ConfigureAwait(false);
                return;
            }

            string fromPart = command.Argument[5..].Trim();
            string? mailFrom = ExtractEmailAddress(fromPart);

            if (mailFrom == null)
            {
                await SendResponseAsync(SmtpResponse.SyntaxErrorInParameters).ConfigureAwait(false);
                return;
            }

            ResetMessage();
            _mailFrom = mailFrom;
            _state = SmtpSessionState.Mail;

            await SendResponseAsync(SmtpResponse.Ok).ConfigureAwait(false);
        }

        private async Task ProcessRcptToAsync(SmtpCommand command)
        {
            if (_state < SmtpSessionState.Mail)
            {
                await SendResponseAsync(SmtpResponse.BadSequence).ConfigureAwait(false);
                return;
            }

            if (string.IsNullOrWhiteSpace(command.Argument) || !command.Argument.StartsWith("TO:", StringComparison.OrdinalIgnoreCase))
            {
                await SendResponseAsync(SmtpResponse.SyntaxErrorInParameters).ConfigureAwait(false);
                return;
            }

            string toPart = command.Argument[3..].Trim();
            string? rcptTo = ExtractEmailAddress(toPart);

            if (rcptTo == null)
            {
                await SendResponseAsync(SmtpResponse.SyntaxErrorInParameters).ConfigureAwait(false);
                return;
            }

            if (_recipients.Count >= _configuration.MaxRecipients)
            {
                await SendResponseAsync(new SmtpResponse(452, "Too many recipients")).ConfigureAwait(false);
                return;
            }

            _recipients.Add(rcptTo);
            _state = SmtpSessionState.Recipient;

            await SendResponseAsync(SmtpResponse.Ok).ConfigureAwait(false);
        }

        private async Task ProcessDataAsync(CancellationToken cancellationToken)
        {
            if (_state < SmtpSessionState.Recipient)
            {
                await SendResponseAsync(SmtpResponse.BadSequence).ConfigureAwait(false);
                return;
            }

            await SendResponseAsync(SmtpResponse.StartMailInput).ConfigureAwait(false);

            byte[]? messageData = await ReadMessageDataAsync(cancellationToken).ConfigureAwait(false);

            if (messageData == null || messageData.Length == 0)
            {
                await SendResponseAsync(SmtpResponse.LocalError).ConfigureAwait(false);
                return;
            }

            try
            {
                _currentMessage = new SmtpMessage(Id, _mailFrom, _recipients, messageData);
                MessageCount++;

                MessageEventArgs eventArgs = new(_currentMessage, this);
                _server.OnMessageReceived(eventArgs);

                if (eventArgs.Cancel)
                {
                    await SendResponseAsync(eventArgs.Response).ConfigureAwait(false);
                }
                else
                {
                    await SendResponseAsync(SmtpResponse.MessageAccepted).ConfigureAwait(false);
                }

                ResetMessage();
                _state = SmtpSessionState.Hello;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");
                await SendResponseAsync(SmtpResponse.LocalError).ConfigureAwait(false);
            }
        }

        private async Task<byte[]?> ReadMessageDataAsync(CancellationToken cancellationToken)
        {
            ArrayBufferWriter<byte> messageBuffer = new();
            StringBuilder lineBuilder = new();
            bool previousWasCr = false;
            long totalSize = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                ReadResult result = await _reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                ReadOnlySequence<byte> buffer = result.Buffer;

                SequencePosition consumed = buffer.Start;
                SequencePosition examined = buffer.End;

                // Process the buffer
                foreach (ReadOnlyMemory<byte> segment in buffer)
                {
                    ReadOnlySpan<byte> span = segment.Span;

                    for (int i = 0; i < span.Length; i++)
                    {
                        byte b = span[i];

                        if (previousWasCr && b == '\n')
                        {
                            string line = lineBuilder.ToString();
                            lineBuilder.Clear();

                            if (line == ".")
                            {
                                // End of message found
                                consumed = buffer.GetPosition(i + 1, consumed);
                                _reader.AdvanceTo(consumed);

                                byte[] messageData = messageBuffer.WrittenMemory.ToArray();
                                return messageData;
                            }

                            // Remove dot-stuffing
                            if (line.StartsWith(".."))
                            {
                                line = line[1..];
                            }

                            // Write line to buffer
                            byte[] lineBytes = Encoding.ASCII.GetBytes(line + "\r\n");
                            messageBuffer.Write(lineBytes);
                            totalSize += lineBytes.Length;

                            previousWasCr = false;
                        }
                        else if (b == '\r')
                        {
                            previousWasCr = true;
                        }
                        else
                        {
                            if (previousWasCr)
                            {
                                lineBuilder.Append('\r');
                                previousWasCr = false;
                            }
                            lineBuilder.Append((char)b);
                        }

                        if (totalSize > _configuration.MaxMessageSize)
                        {
                            throw new InvalidOperationException("Message size exceeds maximum");
                        }
                    }

                    consumed = buffer.GetPosition(segment.Length, consumed);
                }

                // Tell the PipeReader how much of the buffer we've consumed and examined
                _reader.AdvanceTo(consumed, examined);

                if (result.IsCompleted)
                {
                    return null;
                }
            }

            return null;
        }

        private async Task ProcessRsetAsync()
        {
            ResetMessage();
            _state = _state >= SmtpSessionState.Hello ? SmtpSessionState.Hello : _state;
            await SendResponseAsync(SmtpResponse.Ok).ConfigureAwait(false);
        }

        private async Task ProcessQuitAsync()
        {
            _state = SmtpSessionState.Quit;
            await SendResponseAsync(SmtpResponse.ServiceClosing).ConfigureAwait(false);
        }

        private async Task ProcessStartTlsAsync(CancellationToken cancellationToken)
        {
            if (IsSecure)
            {
                await SendResponseAsync(SmtpResponse.BadSequence).ConfigureAwait(false);
                return;
            }

            if (_configuration.Certificate == null)
            {
                await SendResponseAsync(SmtpResponse.CommandNotImplemented).ConfigureAwait(false);
                return;
            }

            await SendResponseAsync(new SmtpResponse(220, "Ready to start TLS")).ConfigureAwait(false);

            try
            {
                SslStream sslStream = new(_stream, false);
                await sslStream.AuthenticateAsServerAsync(
                    _configuration.Certificate,
                    false,
                    _configuration.SslProtocols,
                    false
                ).ConfigureAwait(false);

                _stream = sslStream;
                _reader = PipeReader.Create(_stream, new StreamPipeReaderOptions(bufferSize: _configuration.ReadBufferSize));
                _writer = PipeWriter.Create(_stream, new StreamPipeWriterOptions());

                IsSecure = true;

                // Reset state after STARTTLS
                _state = SmtpSessionState.Connected;
                ClientDomain = null;
                ResetMessage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to establish TLS");
                throw;
            }
        }

        private async Task ProcessAuthAsync(SmtpCommand command, CancellationToken cancellationToken)
        {
            if (IsAuthenticated)
            {
                await SendResponseAsync(SmtpResponse.BadSequence).ConfigureAwait(false);
                return;
            }

            if (!IsSecure && !_configuration.AllowPlainTextAuthentication)
            {
                await SendResponseAsync(new SmtpResponse(538, "Encryption required for authentication")).ConfigureAwait(false);
                return;
            }

            if (string.IsNullOrWhiteSpace(command.Argument))
            {
                await SendResponseAsync(SmtpResponse.SyntaxErrorInParameters).ConfigureAwait(false);
                return;
            }

            string[] parts = command.Argument.Split(' ', 2);
            string mechanism = parts[0].ToUpperInvariant();

            if (!_configuration.AuthenticationMechanisms.Contains(mechanism))
            {
                await SendResponseAsync(new SmtpResponse(504, "Authentication mechanism not supported")).ConfigureAwait(false);
                return;
            }

            IAuthenticator? authenticator = AuthenticatorFactory.Create(mechanism);
            if (authenticator == null)
            {
                await SendResponseAsync(SmtpResponse.ParameterNotImplemented).ConfigureAwait(false);
                return;
            }

            string? initialResponse = parts.Length > 1 ? parts[1] : null;
            AuthenticationResult result = await authenticator.AuthenticateAsync(this, initialResponse, _reader, _writer, cancellationToken).ConfigureAwait(false);

            if (result.Success)
            {
                IsAuthenticated = true;
                AuthenticatedIdentity = result.Identity;
                await SendResponseAsync(new SmtpResponse(235, "Authentication successful")).ConfigureAwait(false);
            }
            else
            {
                await SendResponseAsync(SmtpResponse.AuthenticationFailed).ConfigureAwait(false);
            }
        }

        private void ResetMessage()
        {
            _mailFrom = null;
            _recipients.Clear();
            _currentMessage = null;
        }

        private string? ExtractEmailAddress(string input)
        {
            input = input.Trim();

            if (input.StartsWith("<") && input.EndsWith(">"))
            {
                return input[1..^1];
            }

            int angleStart = input.IndexOf('<');
            int angleEnd = input.IndexOf('>');

            if (angleStart >= 0 && angleEnd > angleStart)
            {
                return input.Substring(angleStart + 1, angleEnd - angleStart - 1);
            }

            return null;
        }

        private async Task<string?> ReadLineAsync(CancellationToken cancellationToken)
        {
            try
            {
                using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(_configuration.CommandTimeout);

                StringBuilder lineBuilder = new();

                while (true)
                {
                    ReadResult result = await _reader.ReadAsync(cts.Token).ConfigureAwait(false);
                    ReadOnlySequence<byte> buffer = result.Buffer;

                    SequencePosition? position = null;

                    do
                    {
                        // Look for a line ending (\r\n)
                        position = buffer.PositionOf((byte)'\n');

                        if (position != null)
                        {
                            // Process the line
                            ReadOnlySequence<byte> line = buffer.Slice(0, position.Value);

                            // Remove \r if present
                            if (!line.IsEmpty && line.IsSingleSegment)
                            {
                                ReadOnlySpan<byte> span = line.FirstSpan;
                                if (span.Length > 0 && span[^1] == '\r')
                                {
                                    span = span[..^1];
                                }
                                lineBuilder.Append(Encoding.ASCII.GetString(span));
                            }
                            else if (!line.IsEmpty)
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
                            lineBuilder.Clear();

                            // Skip the line ending
                            buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
                            _reader.AdvanceTo(buffer.Start);

                            return completeLine;
                        }
                        else
                        {
                            // No line ending found, accumulate the data
                            if (!buffer.IsEmpty)
                            {
                                byte[] tempBytes = ArrayPool<byte>.Shared.Rent((int)buffer.Length);
                                try
                                {
                                    buffer.CopyTo(tempBytes);
                                    lineBuilder.Append(Encoding.ASCII.GetString(tempBytes, 0, (int)buffer.Length));
                                }
                                finally
                                {
                                    ArrayPool<byte>.Shared.Return(tempBytes);
                                }
                            }
                        }
                    }
                    while (position != null);

                    // Tell the PipeReader how much of the buffer we've consumed
                    _reader.AdvanceTo(buffer.End);

                    // If we've completed reading, return null
                    if (result.IsCompleted)
                    {
                        return lineBuilder.Length > 0 ? lineBuilder.ToString() : null;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading line");
                throw;
            }
        }

        private async Task SendResponseAsync(SmtpResponse response)
        {
            try
            {
                string responseText = response.ToString();

                if (_configuration.EnableVerboseLogging)
                {
                    _logger.LogDebug("S: {Response}", responseText.TrimEnd());
                }

                byte[] responseBytes = Encoding.ASCII.GetBytes(responseText);
                await _writer.WriteAsync(responseBytes.AsMemory()).ConfigureAwait(false);

                FlushResult result = await _writer.FlushAsync().ConfigureAwait(false);
                if (result.IsCompleted)
                {
                    throw new IOException("Connection closed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending response");
                throw;
            }
        }

        public async Task CloseAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            try
            {
                await _reader.CompleteAsync().ConfigureAwait(false);
                await _writer.CompleteAsync().ConfigureAwait(false);
                _stream?.Dispose();
                _client?.Close();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing session");
            }
        }
    }

    internal enum SmtpSessionState
    {
        Connected,
        Hello,
        Mail,
        Recipient,
        Data,
        Quit
    }
}