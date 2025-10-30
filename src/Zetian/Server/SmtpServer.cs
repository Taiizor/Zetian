using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Zetian.Abstractions;
using Zetian.Configuration;
using Zetian.Internal;
using Zetian.Models.EventArgs;

namespace Zetian.Server
{
    /// <summary>
    /// The main SMTP server implementation
    /// </summary>
    public class SmtpServer : ISmtpServer
    {
        private readonly ILogger<SmtpServer> _logger;
        private readonly ConcurrentDictionary<string, SmtpSession> _sessions;
        private readonly SemaphoreSlim _connectionSemaphore;
        private readonly ConnectionTracker _connectionTracker;

        private TcpListener? _listener;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _acceptTask;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of SmtpServer
        /// </summary>
        public SmtpServer(SmtpServerConfiguration configuration)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            Configuration.Validate();

            ILoggerFactory loggerFactory = Configuration.LoggerFactory ?? NullLoggerFactory.Instance;
            _logger = loggerFactory.CreateLogger<SmtpServer>();

            _sessions = new ConcurrentDictionary<string, SmtpSession>();
            _connectionTracker = new ConnectionTracker(Configuration.MaxConnectionsPerIp, _logger);
            _connectionSemaphore = new SemaphoreSlim(Configuration.MaxConnections, Configuration.MaxConnections);
        }

        /// <summary>
        /// Initializes a new instance of SmtpServer with default configuration
        /// </summary>
        public SmtpServer() : this(new SmtpServerConfiguration())
        {
        }

        /// <inheritdoc />
        public SmtpServerConfiguration Configuration { get; }

        /// <inheritdoc />
        public bool IsRunning { get; private set; }

        /// <inheritdoc />
        public IPEndPoint? Endpoint => _listener?.LocalEndpoint as IPEndPoint;

        /// <summary>
        /// Gets the server start time
        /// </summary>
        public DateTime? StartTime { get; private set; }

        /// <summary>
        /// Gets the number of active sessions
        /// </summary>
        public int ActiveSessionCount => _sessions?.Count ?? 0;

        /// <inheritdoc />
        public event EventHandler<SessionEventArgs>? SessionCreated;

        /// <inheritdoc />
        public event EventHandler<SessionEventArgs>? SessionCompleted;

        /// <inheritdoc />
        public event EventHandler<MessageEventArgs>? MessageReceived;

        /// <inheritdoc />
        public event EventHandler<ErrorEventArgs>? ErrorOccurred;

        /// <inheritdoc />
        public event EventHandler<ConnectionEventArgs>? ConnectionAccepted;

        /// <inheritdoc />
        public event EventHandler<ConnectionEventArgs>? ConnectionRejected;

        /// <inheritdoc />
        public event EventHandler<CommandEventArgs>? CommandReceived;

        /// <inheritdoc />
        public event EventHandler<CommandEventArgs>? CommandExecuted;

        /// <inheritdoc />
        public event EventHandler<AuthenticationEventArgs>? AuthenticationAttempted;

        /// <inheritdoc />
        public event EventHandler<AuthenticationEventArgs>? AuthenticationSucceeded;

        /// <inheritdoc />
        public event EventHandler<AuthenticationEventArgs>? AuthenticationFailed;

        /// <inheritdoc />
        public event EventHandler<TlsEventArgs>? TlsNegotiationStarted;

        /// <inheritdoc />
        public event EventHandler<TlsEventArgs>? TlsNegotiationCompleted;

        /// <inheritdoc />
        public event EventHandler<TlsEventArgs>? TlsNegotiationFailed;

        /// <inheritdoc />
        public event EventHandler<DataTransferEventArgs>? DataTransferStarted;

        /// <inheritdoc />
        public event EventHandler<DataTransferEventArgs>? DataTransferCompleted;

        /// <inheritdoc />
        public event EventHandler<RateLimitEventArgs>? RateLimitExceeded;

        /// <inheritdoc />
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (IsRunning)
            {
                _logger.LogWarning("SMTP server is already running");
                return;
            }

            try
            {
                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                IPEndPoint endpoint = new(Configuration.IpAddress, Configuration.Port);
                _listener = new TcpListener(endpoint);

                // Configure socket options
                if (_listener.Server != null)
                {
                    _listener.Server.NoDelay = !Configuration.UseNagleAlgorithm;
                    _listener.Server.ReceiveBufferSize = Configuration.ReadBufferSize;
                    _listener.Server.SendBufferSize = Configuration.WriteBufferSize;
                }

                _listener.Start();
                IsRunning = true;
                StartTime = DateTime.UtcNow;

                _logger.LogInformation("SMTP server started on {Endpoint}", Endpoint);

                // Start accepting connections
                _acceptTask = AcceptConnectionsAsync(_cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start SMTP server");
                IsRunning = false;
                throw;
            }
        }

        /// <inheritdoc />
        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (!IsRunning)
            {
                _logger.LogWarning("SMTP server is not running");
                return;
            }

            try
            {
                _logger.LogInformation("Stopping SMTP server");

                // Stop accepting new connections
                _cancellationTokenSource?.Cancel();
                _listener?.Stop();

                // Wait for accept task to complete
                if (_acceptTask != null)
                {
                    try
                    {
                        await _acceptTask.ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected
                    }
                }

                // Close all active sessions
                Task[] closeTasks = new Task[_sessions.Count];
                int index = 0;
                foreach (SmtpSession session in _sessions.Values)
                {
                    closeTasks[index++] = Task.Run(async () =>
                    {
                        try
                        {
                            await session.CloseAsync().ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error closing session {SessionId}", session.Id);
                        }
                    });
                }

                // Wait for all sessions to close (with timeout)
                if (closeTasks.Length > 0)
                {
                    using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(TimeSpan.FromSeconds(30));

                    try
                    {
                        await Task.WhenAll(closeTasks).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogWarning("Timeout waiting for sessions to close");
                    }
                }

                _sessions.Clear();

                IsRunning = false;
                StartTime = null;
                _logger.LogInformation("SMTP server stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping SMTP server");
                throw;
            }
            finally
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                _listener = null;
                _acceptTask = null;
                StartTime = null;
            }
        }

        private async Task AcceptConnectionsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _listener != null)
            {
                try
                {
                    TcpClient? tcpClient = await AcceptClientAsync(cancellationToken).ConfigureAwait(false);
                    if (tcpClient == null)
                    {
                        continue;
                    }

                    // Handle connection in background
                    _ = Task.Run(async () =>
                    {
                        await HandleConnectionAsync(tcpClient, cancellationToken).ConfigureAwait(false);
                    }, cancellationToken);
                }
                catch (ObjectDisposedException)
                {
                    // Listener was stopped
                    break;
                }
                catch (OperationCanceledException)
                {
                    // Cancellation requested
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error accepting connection");
                    OnErrorOccurred(new ErrorEventArgs(ex));

                    // Brief delay before continuing
                    await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private async Task<TcpClient?> AcceptClientAsync(CancellationToken cancellationToken)
        {
            if (_listener == null)
            {
                return null;
            }

            try
            {
                using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                Task<TcpClient> acceptTask = _listener.AcceptTcpClientAsync();
                TaskCompletionSource<bool> tcs = new();

                using (cts.Token.Register(() => tcs.TrySetCanceled()))
                {
                    Task completedTask = await Task.WhenAny(acceptTask, tcs.Task).ConfigureAwait(false);

                    if (completedTask == acceptTask)
                    {
                        return await acceptTask.ConfigureAwait(false);
                    }

                    throw new OperationCanceledException();
                }
            }
            catch (ObjectDisposedException)
            {
                throw;
            }
        }

        private async Task HandleConnectionAsync(TcpClient client, CancellationToken cancellationToken)
        {
            SmtpSession? session = null;
            bool acquired = false;
            ConnectionTracker.ConnectionHandle? connectionHandle = null;

            try
            {
                // Check connection limits
                if (client.Client.RemoteEndPoint is not IPEndPoint remoteEndPoint)
                {
                    client.Close();
                    return;
                }

                IPEndPoint localEndPoint = (IPEndPoint)client.Client.LocalEndPoint!;
                ConnectionEventArgs connectionArgs = new(remoteEndPoint, localEndPoint);

                // Try to acquire a connection slot for this IP
                IPAddress ipAddress = remoteEndPoint.Address;
                connectionHandle = await _connectionTracker.TryAcquireAsync(ipAddress, cancellationToken).ConfigureAwait(false);

                if (connectionHandle == null)
                {
                    _logger.LogWarning("Connection limit exceeded for IP {IPAddress}", ipAddress);
                    connectionArgs.Accept = false;
                    connectionArgs.RejectionReason = "Connection limit exceeded for IP";
                    OnConnectionRejected(connectionArgs);

                    // Fire rate limit event
                    OnRateLimitExceeded(new RateLimitEventArgs(ipAddress)
                    {
                        CurrentCount = Configuration.MaxConnectionsPerIp,
                        Limit = Configuration.MaxConnectionsPerIp,
                        TimeWindow = TimeSpan.FromMinutes(1)
                    });

                    client.Close();
                    return;
                }

                // Acquire global connection semaphore
                acquired = await _connectionSemaphore.WaitAsync(0, cancellationToken).ConfigureAwait(false);
                if (!acquired)
                {
                    _logger.LogWarning("Maximum connection limit reached");
                    connectionArgs.Accept = false;
                    connectionArgs.RejectionReason = "Maximum connection limit reached";
                    OnConnectionRejected(connectionArgs);
                    client.Close();
                    return;
                }

                // Connection accepted
                OnConnectionAccepted(connectionArgs);

                // Create session
                session = new SmtpSession(this, client, Configuration, _logger);

                if (_sessions.TryAdd(session.Id, session))
                {
                    _logger.LogInformation("Session {SessionId} created from {RemoteEndPoint}",
                        session.Id, remoteEndPoint);

                    OnSessionCreated(new SessionEventArgs(session));

                    // Handle session
                    await session.HandleAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling connection");
                OnErrorOccurred(new ErrorEventArgs(ex, session));
            }
            finally
            {
                if (session != null)
                {
                    _sessions.TryRemove(session.Id, out _);
                    OnSessionCompleted(new SessionEventArgs(session));
                    _logger.LogInformation("Session {SessionId} completed", session.Id);
                }

                // Release connection handle (this handles IP-specific connection tracking)
                connectionHandle?.Dispose();

                if (acquired)
                {
                    _connectionSemaphore.Release();
                }

                try
                {
                    client?.Close();
                }
                catch
                {
                    // Ignore
                }
            }
        }

        internal void OnMessageReceived(MessageEventArgs args)
        {
            try
            {
                MessageReceived?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MessageReceived event handler");
            }
        }

        private void OnSessionCreated(SessionEventArgs args)
        {
            try
            {
                SessionCreated?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SessionCreated event handler");
            }
        }

        private void OnSessionCompleted(SessionEventArgs args)
        {
            try
            {
                SessionCompleted?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SessionCompleted event handler");
            }
        }

        private void OnErrorOccurred(ErrorEventArgs args)
        {
            try
            {
                ErrorOccurred?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ErrorOccurred event handler");
            }
        }

        internal void OnConnectionAccepted(ConnectionEventArgs args)
        {
            try
            {
                ConnectionAccepted?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ConnectionAccepted event handler");
            }
        }

        internal void OnConnectionRejected(ConnectionEventArgs args)
        {
            try
            {
                ConnectionRejected?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ConnectionRejected event handler");
            }
        }

        internal void OnCommandReceived(CommandEventArgs args)
        {
            try
            {
                CommandReceived?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CommandReceived event handler");
            }
        }

        internal void OnCommandExecuted(CommandEventArgs args)
        {
            try
            {
                CommandExecuted?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CommandExecuted event handler");
            }
        }

        internal void OnAuthenticationAttempted(AuthenticationEventArgs args)
        {
            try
            {
                AuthenticationAttempted?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AuthenticationAttempted event handler");
            }
        }

        internal void OnAuthenticationSucceeded(AuthenticationEventArgs args)
        {
            try
            {
                AuthenticationSucceeded?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AuthenticationSucceeded event handler");
            }
        }

        internal void OnAuthenticationFailed(AuthenticationEventArgs args)
        {
            try
            {
                AuthenticationFailed?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AuthenticationFailed event handler");
            }
        }

        internal void OnTlsNegotiationStarted(TlsEventArgs args)
        {
            try
            {
                TlsNegotiationStarted?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TlsNegotiationStarted event handler");
            }
        }

        internal void OnTlsNegotiationCompleted(TlsEventArgs args)
        {
            try
            {
                TlsNegotiationCompleted?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TlsNegotiationCompleted event handler");
            }
        }

        internal void OnTlsNegotiationFailed(TlsEventArgs args)
        {
            try
            {
                TlsNegotiationFailed?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TlsNegotiationFailed event handler");
            }
        }

        internal void OnDataTransferStarted(DataTransferEventArgs args)
        {
            try
            {
                DataTransferStarted?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DataTransferStarted event handler");
            }
        }

        internal void OnDataTransferCompleted(DataTransferEventArgs args)
        {
            try
            {
                DataTransferCompleted?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DataTransferCompleted event handler");
            }
        }

        internal void OnRateLimitExceeded(RateLimitEventArgs args)
        {
            try
            {
                RateLimitExceeded?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RateLimitExceeded event handler");
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

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases resources
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                try
                {
                    if (IsRunning)
                    {
                        StopAsync().GetAwaiter().GetResult();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disposing SMTP server");
                }

                _connectionSemaphore?.Dispose();
                _connectionTracker?.Dispose();
                _cancellationTokenSource?.Dispose();
            }

            _disposed = true;
        }
    }
}