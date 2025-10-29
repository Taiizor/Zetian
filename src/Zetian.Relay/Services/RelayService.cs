using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using Zetian.Abstractions;
using Zetian.Relay.Abstractions;
using Zetian.Relay.Client;
using Zetian.Relay.Configuration;
using Zetian.Relay.Enums;
using Zetian.Relay.Queue;

namespace Zetian.Relay.Services
{
    /// <summary>
    /// Main relay service for processing and delivering queued messages
    /// </summary>
    public class RelayService : IDisposable
    {
        private readonly ILogger<RelayService> _logger;
        private readonly RelayConfiguration _configuration;
        private readonly ConcurrentDictionary<string, ISmtpClient> _clientPool;
        private readonly SemaphoreSlim _deliverySemaphore;

        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _processingTask;
        private Task? _cleanupTask;
        private bool _disposed;

        public RelayService(
            RelayConfiguration configuration,
            IRelayQueue? queue = null,
            ILogger<RelayService>? logger = null)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _configuration.Validate();

            Queue = queue ?? new InMemoryRelayQueue();
            _logger = logger ?? NullLogger<RelayService>.Instance;
            _clientPool = new ConcurrentDictionary<string, ISmtpClient>();
            _deliverySemaphore = new SemaphoreSlim(_configuration.MaxConcurrentDeliveries);
        }

        /// <summary>
        /// Gets whether the service is running
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Gets the relay queue
        /// </summary>
        public IRelayQueue Queue { get; }

        /// <summary>
        /// Starts the relay service
        /// </summary>
        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (IsRunning)
            {
                _logger.LogWarning("Relay service is already running");
                return Task.CompletedTask;
            }

            _logger.LogInformation("Starting relay service");

            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Start queue processing
            _processingTask = ProcessQueueAsync(_cancellationTokenSource.Token);

            // Start cleanup task
            _cleanupTask = CleanupExpiredMessagesAsync(_cancellationTokenSource.Token);

            IsRunning = true;

            _logger.LogInformation("Relay service started");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Stops the relay service
        /// </summary>
        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            if (!IsRunning)
            {
                _logger.LogWarning("Relay service is not running");
                return;
            }

            _logger.LogInformation("Stopping relay service");

            _cancellationTokenSource?.Cancel();

            // Wait for tasks to complete
            IEnumerable<Task?> tasks = new[] { _processingTask, _cleanupTask }.Where(t => t != null);

            try
            {
                await Task.WhenAll(tasks!).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected
            }

            // Dispose all clients
            foreach (ISmtpClient client in _clientPool.Values)
            {
                client?.Dispose();
            }
            _clientPool.Clear();

            IsRunning = false;

            _logger.LogInformation("Relay service stopped");
        }

        /// <summary>
        /// Queues a message for relay
        /// </summary>
        public async Task<IRelayMessage> QueueMessageAsync(
            ISmtpMessage message,
            ISmtpSession session,
            RelayPriority priority = RelayPriority.Normal,
            CancellationToken cancellationToken = default)
        {
            if (!_configuration.Enabled)
            {
                throw new InvalidOperationException("Relay is not enabled");
            }

            // Check if sender is allowed to relay
            if (!await CanRelayAsync(session, message).ConfigureAwait(false))
            {
                throw new UnauthorizedAccessException("Relay access denied");
            }

            // Determine smart host for the message
            string? smartHost = DetermineSmartHost(message);

            // Queue the message
            IRelayMessage relayMessage = await Queue.EnqueueAsync(
                message,
                smartHost,
                priority,
                cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Message {QueueId} queued for relay from {From} to {Recipients}",
                relayMessage.QueueId,
                message.From?.Address,
                string.Join(", ", message.Recipients.Select(r => r.Address)));

            return relayMessage;
        }

        private async Task ProcessQueueAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Queue processing started");

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Get next message from queue
                    IRelayMessage? message = await Queue.DequeueAsync(cancellationToken).ConfigureAwait(false);

                    if (message != null)
                    {
                        // Process message asynchronously with semaphore
                        _ = Task.Run(async () =>
                        {
                            await _deliverySemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                            try
                            {
                                await DeliverMessageAsync(message, cancellationToken).ConfigureAwait(false);
                            }
                            finally
                            {
                                _deliverySemaphore.Release();
                            }
                        }, cancellationToken);
                    }
                    else
                    {
                        // No messages, wait before checking again
                        await Task.Delay(_configuration.QueueProcessingInterval, cancellationToken)
                            .ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing queue");
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
                }
            }

            _logger.LogInformation("Queue processing stopped");
        }

        private async Task DeliverMessageAsync(IRelayMessage message, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Delivering message {QueueId} (attempt {Retry})",
                    message.QueueId, message.RetryCount + 1);

                // Check if message has expired
                if (message.IsExpired)
                {
                    await Queue.UpdateStatusAsync(message.QueueId, RelayStatus.Expired,
                        "Message expired", cancellationToken).ConfigureAwait(false);
                    return;
                }

                // Determine target server
                SmartHostConfiguration? targetConfig = GetTargetConfiguration(message);
                if (targetConfig == null)
                {
                    await Queue.UpdateStatusAsync(message.QueueId, RelayStatus.Failed,
                        "No target server configured", cancellationToken).ConfigureAwait(false);
                    return;
                }

                // Get or create client
                ISmtpClient client = GetOrCreateClient(targetConfig);

                // Connect if needed
                if (!client.IsConnected)
                {
                    await client.ConnectAsync(cancellationToken).ConfigureAwait(false);

                    if (targetConfig.Credentials != null)
                    {
                        await client.AuthenticateAsync(cancellationToken).ConfigureAwait(false);
                    }
                }

                // Send message
                SmtpDeliveryResult result = await client.SendAsync(
                    message.OriginalMessage,
                    cancellationToken).ConfigureAwait(false);

                if (result.Success)
                {
                    // Mark delivered recipients
                    await Queue.MarkDeliveredAsync(
                        message.QueueId,
                        result.DeliveredRecipients,
                        cancellationToken).ConfigureAwait(false);

                    _logger.LogInformation("Message {QueueId} delivered successfully to {Count} recipients",
                        message.QueueId, result.DeliveredRecipients.Count);
                }
                else if (result.IsTemporaryFailure && message.RetryCount < _configuration.MaxRetryCount)
                {
                    // Schedule retry
                    TimeSpan delay = CalculateRetryDelay(message.RetryCount);
                    await Queue.RescheduleAsync(message.QueueId, delay, cancellationToken)
                        .ConfigureAwait(false);

                    _logger.LogWarning("Message {QueueId} delivery deferred: {Error}",
                        message.QueueId, result.Message);
                }
                else
                {
                    // Permanent failure or max retries reached
                    await Queue.UpdateStatusAsync(
                        message.QueueId,
                        RelayStatus.Failed,
                        result.Message,
                        cancellationToken).ConfigureAwait(false);

                    _logger.LogError("Message {QueueId} delivery failed: {Error}",
                        message.QueueId, result.Message);

                    // Send bounce if enabled
                    if (_configuration.EnableBounceMessages && message.From != null)
                    {
                        await SendBounceMessageAsync(message, result.Message ?? "Delivery failed")
                            .ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error delivering message {QueueId}", message.QueueId);

                if (message.RetryCount < _configuration.MaxRetryCount)
                {
                    // Schedule retry
                    TimeSpan delay = CalculateRetryDelay(message.RetryCount);
                    await Queue.RescheduleAsync(message.QueueId, delay, cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                {
                    // Max retries reached
                    await Queue.UpdateStatusAsync(
                        message.QueueId,
                        RelayStatus.Failed,
                        ex.Message,
                        cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private async Task CleanupExpiredMessagesAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Cleanup task started");

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_configuration.CleanupInterval, cancellationToken)
                        .ConfigureAwait(false);

                    int count = await Queue.ClearExpiredAsync(cancellationToken)
                        .ConfigureAwait(false);

                    if (count > 0)
                    {
                        _logger.LogInformation("Cleaned up {Count} expired messages", count);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during cleanup");
                }
            }

            _logger.LogInformation("Cleanup task stopped");
        }

        private async Task<bool> CanRelayAsync(ISmtpSession session, ISmtpMessage message)
        {
            // Allow relay if authenticated
            if (session.IsAuthenticated)
            {
                return true;
            }

            // Allow relay from specific networks
            if (session.RemoteEndPoint is System.Net.IPEndPoint ipEndPoint)
            {
                if (_configuration.RelayNetworks.Contains(ipEndPoint.Address))
                {
                    return true;
                }
            }

            // Require authentication if configured
            if (_configuration.RequireAuthentication)
            {
                return false;
            }

            return await Task.FromResult(true);
        }

        private string? DetermineSmartHost(ISmtpMessage message)
        {
            // Check domain-specific routing
            foreach (MailAddress recipient in message.Recipients)
            {
                string domain = recipient.Host;
                if (_configuration.DomainRouting.ContainsKey(domain))
                {
                    SmartHostConfiguration config = _configuration.DomainRouting[domain];
                    return $"{config.Host}:{config.Port}";
                }
            }

            // Use default smart host if configured
            if (_configuration.DefaultSmartHost != null)
            {
                return $"{_configuration.DefaultSmartHost.Host}:{_configuration.DefaultSmartHost.Port}";
            }

            // Use MX routing if enabled
            if (_configuration.UseMxRouting)
            {
                // This would involve DNS MX lookups
                // For now, return null to indicate direct delivery
                return null;
            }

            return null;
        }

        private SmartHostConfiguration? GetTargetConfiguration(IRelayMessage message)
        {
            if (!string.IsNullOrEmpty(message.SmartHost))
            {
                // Parse smart host string
                string[] parts = message.SmartHost.Split(':');
                string host = parts[0];
                int port = parts.Length > 1 && int.TryParse(parts[1], out int p) ? p : 25;

                // Find matching configuration
                SmartHostConfiguration? config = _configuration.SmartHosts
                    .FirstOrDefault(sh => sh.Host == host && sh.Port == port && sh.Enabled);

                if (config != null)
                {
                    return config;
                }

                // Create default configuration
                return new SmartHostConfiguration
                {
                    Host = host,
                    Port = port,
                    UseTls = _configuration.EnableTls,
                    UseStartTls = _configuration.EnableTls,
                    ConnectionTimeout = _configuration.ConnectionTimeout
                };
            }

            return _configuration.DefaultSmartHost;
        }

        private ISmtpClient GetOrCreateClient(SmartHostConfiguration config)
        {
            string key = $"{config.Host}:{config.Port}";

            return _clientPool.GetOrAdd(key, _ =>
            {
                var client = new SmtpRelayClient(_logger as ILogger<SmtpRelayClient>)
                {
                    Host = config.Host,
                    Port = config.Port,
                    EnableSsl = config.UseTls,
                    Credentials = config.Credentials,
                    LocalDomain = _configuration.LocalDomain,
                    Timeout = config.ConnectionTimeout
                };

                return client;
            });
        }

        private TimeSpan CalculateRetryDelay(int retryCount)
        {
            // Exponential backoff with jitter
            var baseDelay = TimeSpan.FromMinutes(1);
            var maxDelay = TimeSpan.FromHours(4);

            var delay = TimeSpan.FromMilliseconds(
                baseDelay.TotalMilliseconds * Math.Pow(2, Math.Min(retryCount, 10)));

            // Add jitter (±10%)
            double jitter = (new Random().NextDouble() * 0.2) - 0.1;
            delay = delay.Add(TimeSpan.FromMilliseconds(delay.TotalMilliseconds * jitter));

            return delay > maxDelay ? maxDelay : delay;
        }

        private async Task SendBounceMessageAsync(IRelayMessage message, string error)
        {
            try
            {
                // TODO: Implement bounce message generation and sending
                _logger.LogDebug("Bounce message would be sent for {QueueId}: {Error}",
                    message.QueueId, error);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending bounce message for {QueueId}", message.QueueId);
            }
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
                if (IsRunning)
                {
                    StopAsync().GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during dispose");
            }

            foreach (ISmtpClient client in _clientPool.Values)
            {
                client?.Dispose();
            }

            _deliverySemaphore?.Dispose();
            _cancellationTokenSource?.Dispose();
        }
    }
}