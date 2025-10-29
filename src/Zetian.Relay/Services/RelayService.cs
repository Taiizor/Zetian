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
                SmtpRelayClient client = new(_logger as ILogger<SmtpRelayClient>)
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
            TimeSpan baseDelay = TimeSpan.FromMinutes(1);
            TimeSpan maxDelay = TimeSpan.FromHours(4);

            TimeSpan delay = TimeSpan.FromMilliseconds(
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
                // Don't send bounce for bounce messages to avoid loops
                if (message.From == null ||
                    message.From.Address.Equals(_configuration.BounceSender, StringComparison.OrdinalIgnoreCase) ||
                    message.From.Address.StartsWith("<>", StringComparison.OrdinalIgnoreCase) ||
                    message.From.Address.Equals("postmaster", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug("Skipping bounce message for {QueueId}: sender is bounce address", message.QueueId);
                    return;
                }

                _logger.LogInformation("Generating bounce message for {QueueId} to {Sender}",
                    message.QueueId, message.From.Address);

                // Create bounce message
                string bounceSubject = $"Undelivered Mail Returned to Sender: {message.OriginalMessage.Subject ?? "(no subject)"}";
                string bounceBody = GenerateBounceBody(message, error);

                // Create a new SMTP message for the bounce
                MailMessage bounceMessage = new()
                {
                    From = new MailAddress(
                        string.IsNullOrEmpty(_configuration.BounceSender) ? "<>" : _configuration.BounceSender,
                        "Mail Delivery System"),
                    Subject = bounceSubject,
                    Body = bounceBody,
                    IsBodyHtml = false,
                    Priority = MailPriority.High
                };

                // Add recipient (original sender)
                bounceMessage.To.Add(message.From);

                // Add headers
                bounceMessage.Headers.Add("X-Bounce-Message-Id", message.QueueId);
                bounceMessage.Headers.Add("X-Original-Message-Id", message.OriginalMessage.Id ?? "unknown");
                bounceMessage.Headers.Add("Auto-Submitted", "auto-replied");
                bounceMessage.Headers.Add("Content-Type", "multipart/report; report-type=delivery-status");

                // Send bounce message
                if (_configuration.DefaultSmartHost != null)
                {
                    ISmtpClient client = GetOrCreateClient(_configuration.DefaultSmartHost);

                    // Connect if not connected
                    if (!client.IsConnected)
                    {
                        // Set connection parameters
                        client.Host = _configuration.DefaultSmartHost.Host;
                        client.Port = _configuration.DefaultSmartHost.Port;
                        client.EnableSsl = _configuration.DefaultSmartHost.UseTls || _configuration.DefaultSmartHost.UseStartTls;
                        client.Credentials = _configuration.DefaultSmartHost.Credentials;

                        await client.ConnectAsync().ConfigureAwait(false);

                        // Authenticate if credentials provided
                        if (_configuration.DefaultSmartHost.Credentials != null)
                        {
                            await client.AuthenticateAsync().ConfigureAwait(false);
                        }
                    }

                    // Convert bounce message to raw data
                    string fromAddress = string.IsNullOrEmpty(_configuration.BounceSender) ? "" : _configuration.BounceSender;
                    string[] recipients = new[] { message.From.Address };

                    // Build the raw message
                    System.Text.StringBuilder rawMessage = new();
                    rawMessage.AppendLine($"From: {bounceMessage.From.Address}");
                    rawMessage.AppendLine($"To: {message.From.Address}");
                    rawMessage.AppendLine($"Subject: {bounceMessage.Subject}");
                    rawMessage.AppendLine($"Date: {DateTime.UtcNow:R}");
                    rawMessage.AppendLine("X-Bounce-Message-Id: " + message.QueueId);
                    rawMessage.AppendLine("X-Original-Message-Id: " + (message.OriginalMessage.Id ?? "unknown"));
                    rawMessage.AppendLine("Auto-Submitted: auto-replied");
                    rawMessage.AppendLine("Content-Type: text/plain; charset=UTF-8");
                    rawMessage.AppendLine("MIME-Version: 1.0");
                    rawMessage.AppendLine();
                    rawMessage.Append(bounceBody);

                    byte[] messageData = System.Text.Encoding.UTF8.GetBytes(rawMessage.ToString());

                    // Send the bounce message
                    await client.SendRawAsync(fromAddress, recipients, messageData).ConfigureAwait(false);

                    _logger.LogInformation("Bounce message sent for {QueueId} to {Recipient}",
                        message.QueueId, message.From.Address);
                }
                else
                {
                    _logger.LogWarning("Cannot send bounce message for {QueueId}: no smart host configured",
                        message.QueueId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending bounce message for {QueueId}", message.QueueId);
            }
        }

        private string GenerateBounceBody(IRelayMessage message, string error)
        {
            System.Text.StringBuilder body = new();

            // Header
            body.AppendLine("***** THIS IS AN AUTOMATED MESSAGE - DO NOT REPLY *****");
            body.AppendLine();
            body.AppendLine("Your message could not be delivered to one or more recipients.");
            body.AppendLine("This is a permanent error. The message will not be retried.");
            body.AppendLine();

            // Error details
            body.AppendLine("---------- ERROR DETAILS ----------");
            body.AppendLine($"Queue ID: {message.QueueId}");
            body.AppendLine($"Error Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            body.AppendLine($"Error Message: {error}");
            body.AppendLine($"Retry Count: {message.RetryCount}");
            body.AppendLine($"Message Age: {DateTime.UtcNow - message.QueuedTime:d\\d\\ hh\\:mm\\:ss}");
            body.AppendLine();

            // Failed recipients
            body.AppendLine("---------- FAILED RECIPIENTS ----------");
            if (message.FailedRecipients.Count > 0)
            {
                foreach (MailAddress recipient in message.FailedRecipients)
                {
                    body.AppendLine($"  - {recipient.Address}");
                }
            }
            else if (message.PendingRecipients.Count > 0)
            {
                foreach (MailAddress recipient in message.PendingRecipients)
                {
                    body.AppendLine($"  - {recipient.Address}");
                }
            }
            else
            {
                foreach (MailAddress recipient in message.Recipients)
                {
                    body.AppendLine($"  - {recipient.Address}");
                }
            }
            body.AppendLine();

            // Delivery attempts
            if (message.LastAttemptTime.HasValue)
            {
                body.AppendLine("---------- DELIVERY ATTEMPTS ----------");
                body.AppendLine($"First Attempt: {message.QueuedTime:yyyy-MM-dd HH:mm:ss} UTC");
                body.AppendLine($"Last Attempt: {message.LastAttemptTime.Value:yyyy-MM-dd HH:mm:ss} UTC");
                body.AppendLine($"Total Attempts: {message.RetryCount}");
                body.AppendLine();
            }

            // Original message info
            body.AppendLine("---------- ORIGINAL MESSAGE ----------");
            body.AppendLine($"From: {message.From?.Address ?? "<unknown>"}");
            body.AppendLine($"Subject: {message.OriginalMessage.Subject ?? "(no subject)"}");
            body.AppendLine($"Date: {message.QueuedTime:yyyy-MM-dd HH:mm:ss} UTC");
            body.AppendLine($"Message-ID: {message.OriginalMessage.Id ?? "<unknown>"}");

            // Recipients
            body.Append("To: ");
            body.AppendLine(string.Join(", ", message.Recipients.Select(r => r.Address)));

            // Original headers if available
            if (message.OriginalMessage.Headers.Count > 0)
            {
                body.AppendLine();
                body.AppendLine("---------- ORIGINAL HEADERS ----------");
                foreach (KeyValuePair<string, string> header in message.OriginalMessage.Headers)
                {
                    body.AppendLine($"{header.Key}: {header.Value}");
                }
            }

            // Original body preview (first 500 characters)
            string? messageBody = message.OriginalMessage.TextBody ?? message.OriginalMessage.HtmlBody;
            if (!string.IsNullOrEmpty(messageBody))
            {
                body.AppendLine();
                body.AppendLine("---------- MESSAGE PREVIEW (first 500 chars) ----------");
                string preview = messageBody.Length > 500
                    ? messageBody[..500] + "..."
                    : messageBody;
                body.AppendLine(preview);
            }

            body.AppendLine();
            body.AppendLine("---------- END OF BOUNCE MESSAGE ----------");

            return body.ToString();
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