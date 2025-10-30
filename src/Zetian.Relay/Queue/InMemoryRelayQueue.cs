using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Zetian.Abstractions;
using Zetian.Relay.Abstractions;
using Zetian.Relay.Enums;
using Zetian.Relay.Models;

namespace Zetian.Relay.Queue
{
    /// <summary>
    /// In-memory implementation of relay queue
    /// </summary>
    public class InMemoryRelayQueue : IRelayQueue
    {
        private readonly ILogger<InMemoryRelayQueue> _logger;
        private readonly ConcurrentDictionary<string, RelayMessage> _messages;
        private readonly Channel<string> _readyQueue;
        private readonly SemaphoreSlim _queueLock;
        private int _activeDeliveries;

        public InMemoryRelayQueue(ILogger<InMemoryRelayQueue>? logger = null)
        {
            _logger = logger ?? NullLogger<InMemoryRelayQueue>.Instance;
            _messages = new ConcurrentDictionary<string, RelayMessage>();
            _queueLock = new SemaphoreSlim(1, 1);

            // Create unbounded channel for ready messages
            _readyQueue = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
            {
                SingleReader = false,
                SingleWriter = false
            });
        }

        public int Count => _messages.Count;
        public int ActiveDeliveries => _activeDeliveries;

        public async Task<IRelayMessage> EnqueueAsync(
            ISmtpMessage message,
            string? smartHost = null,
            RelayPriority priority = RelayPriority.Normal,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(message);

            RelayMessage relayMessage = new(message, smartHost, priority);

            if (!_messages.TryAdd(relayMessage.QueueId, relayMessage))
            {
                throw new InvalidOperationException($"Failed to enqueue message {relayMessage.QueueId}");
            }

            _logger.LogInformation("Message {QueueId} enqueued with priority {Priority}",
                relayMessage.QueueId, priority);

            // Add to ready queue for immediate processing
            await _readyQueue.Writer.WriteAsync(relayMessage.QueueId, cancellationToken).ConfigureAwait(false);

            return relayMessage;
        }

        public async Task<IRelayMessage?> DequeueAsync(CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // First, check for deferred messages that are ready
                await ProcessDeferredMessagesAsync(cancellationToken).ConfigureAwait(false);

                // Try to get a message from the ready queue
                if (_readyQueue.Reader.TryRead(out string? queueId))
                {
                    if (_messages.TryGetValue(queueId, out RelayMessage? message))
                    {
                        // Check if message is ready for delivery
                        if (message.Status == RelayStatus.Queued ||
                            (message.Status == RelayStatus.Deferred &&
                             message.NextDeliveryTime <= DateTime.UtcNow))
                        {
                            message.MarkInProgress();
                            Interlocked.Increment(ref _activeDeliveries);

                            _logger.LogDebug("Dequeued message {QueueId} for delivery", queueId);
                            return message;
                        }
                    }
                }

                // Wait for new messages with timeout
                try
                {
                    using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(TimeSpan.FromSeconds(30));

                    queueId = await _readyQueue.Reader.ReadAsync(cts.Token).ConfigureAwait(false);

                    if (_messages.TryGetValue(queueId, out RelayMessage? message))
                    {
                        if (message.Status == RelayStatus.Queued ||
                            (message.Status == RelayStatus.Deferred &&
                             message.NextDeliveryTime <= DateTime.UtcNow))
                        {
                            message.MarkInProgress();
                            Interlocked.Increment(ref _activeDeliveries);

                            _logger.LogDebug("Dequeued message {QueueId} for delivery", queueId);
                            return message;
                        }
                    }
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    // Timeout - continue to check for deferred messages
                    continue;
                }
            }

            return null;
        }

        public Task<IRelayMessage?> GetMessageAsync(string queueId, CancellationToken cancellationToken = default)
        {
            _messages.TryGetValue(queueId, out RelayMessage? message);
            return Task.FromResult<IRelayMessage?>(message);
        }

        public async Task UpdateStatusAsync(
            string queueId,
            RelayStatus status,
            string? error = null,
            CancellationToken cancellationToken = default)
        {
            if (!_messages.TryGetValue(queueId, out RelayMessage? message))
            {
                _logger.LogWarning("Message {QueueId} not found for status update", queueId);
                return;
            }

            bool wasInProgress = message.Status == RelayStatus.InProgress;

            // Update status based on the new status
            switch (status)
            {
                case RelayStatus.Delivered:
                    message.MarkDelivered(message.PendingRecipients.Select(r => r.Address));
                    break;

                case RelayStatus.Failed:
                    message.MarkAllFailed(error ?? "Delivery failed");
                    break;

                case RelayStatus.Deferred:
                    message.ScheduleRetry(message.CalculateRetryDelay());
                    // Re-queue for later delivery
                    await _readyQueue.Writer.WriteAsync(queueId, cancellationToken).ConfigureAwait(false);
                    break;

                case RelayStatus.Expired:
                    message.MarkExpired();
                    break;

                case RelayStatus.Cancelled:
                    message.Cancel(error ?? "Cancelled");
                    break;
            }

            if (wasInProgress)
            {
                Interlocked.Decrement(ref _activeDeliveries);
            }

            _logger.LogInformation("Message {QueueId} status updated to {Status}", queueId, status);
        }

        public async Task MarkDeliveredAsync(
            string queueId,
            IEnumerable<string> recipients,
            CancellationToken cancellationToken = default)
        {
            if (!_messages.TryGetValue(queueId, out RelayMessage? message))
            {
                _logger.LogWarning("Message {QueueId} not found for delivery update", queueId);
                return;
            }

            bool wasInProgress = message.Status == RelayStatus.InProgress;
            message.MarkDelivered(recipients);

            if (wasInProgress && message.Status != RelayStatus.InProgress)
            {
                Interlocked.Decrement(ref _activeDeliveries);
            }

            // If still has pending recipients, re-queue
            if (message.PendingRecipients.Count > 0 && message.Status == RelayStatus.Deferred)
            {
                message.ScheduleRetry(message.CalculateRetryDelay());
                await _readyQueue.Writer.WriteAsync(queueId, cancellationToken).ConfigureAwait(false);
            }

            _logger.LogInformation("Message {QueueId} delivered to {Count} recipients",
                queueId, recipients.Count());
        }

        public async Task MarkFailedAsync(
            string queueId,
            IEnumerable<string> recipients,
            string error,
            CancellationToken cancellationToken = default)
        {
            if (!_messages.TryGetValue(queueId, out RelayMessage? message))
            {
                _logger.LogWarning("Message {QueueId} not found for failure update", queueId);
                return;
            }

            bool wasInProgress = message.Status == RelayStatus.InProgress;
            message.MarkFailed(recipients, error);

            if (wasInProgress && message.Status != RelayStatus.InProgress)
            {
                Interlocked.Decrement(ref _activeDeliveries);
            }

            // If still has pending recipients, re-queue for retry
            if (message.PendingRecipients.Count > 0 && message.Status == RelayStatus.Deferred)
            {
                message.ScheduleRetry(message.CalculateRetryDelay());
                await _readyQueue.Writer.WriteAsync(queueId, cancellationToken).ConfigureAwait(false);
            }

            _logger.LogWarning("Message {QueueId} failed for {Count} recipients: {Error}",
                queueId, recipients.Count(), error);
        }

        public async Task RescheduleAsync(
            string queueId,
            TimeSpan delay,
            CancellationToken cancellationToken = default)
        {
            if (!_messages.TryGetValue(queueId, out RelayMessage? message))
            {
                _logger.LogWarning("Message {QueueId} not found for rescheduling", queueId);
                return;
            }

            if (message.Status == RelayStatus.InProgress)
            {
                Interlocked.Decrement(ref _activeDeliveries);
            }

            message.ScheduleRetry(delay);

            // Add back to ready queue for future processing
            await _readyQueue.Writer.WriteAsync(queueId, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Message {QueueId} rescheduled for {Time}",
                queueId, message.NextDeliveryTime);
        }

        public Task<bool> RemoveAsync(string queueId, CancellationToken cancellationToken = default)
        {
            if (_messages.TryRemove(queueId, out RelayMessage? message))
            {
                if (message.Status == RelayStatus.InProgress)
                {
                    Interlocked.Decrement(ref _activeDeliveries);
                }

                _logger.LogInformation("Message {QueueId} removed from queue", queueId);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public Task<IReadOnlyList<IRelayMessage>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            List<IRelayMessage> messages = _messages.Values
                .Cast<IRelayMessage>()
                .OrderBy(m => m.Priority == RelayPriority.Urgent ? 0 :
                             m.Priority == RelayPriority.High ? 1 :
                             m.Priority == RelayPriority.Normal ? 2 : 3)
                .ThenBy(m => m.QueuedTime)
                .ToList();

            return Task.FromResult<IReadOnlyList<IRelayMessage>>(messages);
        }

        public Task<IReadOnlyList<IRelayMessage>> GetByStatusAsync(
            RelayStatus status,
            CancellationToken cancellationToken = default)
        {
            List<IRelayMessage> messages = _messages.Values
                .Where(m => m.Status == status)
                .Cast<IRelayMessage>()
                .OrderBy(m => m.QueuedTime)
                .ToList();

            return Task.FromResult<IReadOnlyList<IRelayMessage>>(messages);
        }

        public async Task<int> ClearExpiredAsync(CancellationToken cancellationToken = default)
        {
            await _queueLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                List<RelayMessage> expired = _messages.Values
                    .Where(m => m.IsExpired)
                    .ToList();

                foreach (RelayMessage message in expired)
                {
                    if (_messages.TryRemove(message.QueueId, out _))
                    {
                        if (message.Status == RelayStatus.InProgress)
                        {
                            Interlocked.Decrement(ref _activeDeliveries);
                        }
                        message.MarkExpired();
                    }
                }

                if (expired.Count > 0)
                {
                    _logger.LogInformation("Cleared {Count} expired messages from queue", expired.Count);
                }

                return expired.Count;
            }
            finally
            {
                _queueLock.Release();
            }
        }

        public Task<RelayQueueStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            List<RelayMessage> messages = _messages.Values.ToList();

            RelayQueueStatistics stats = new()
            {
                TotalMessages = messages.Count,
                QueuedMessages = messages.Count(m => m.Status == RelayStatus.Queued),
                InProgressMessages = _activeDeliveries,
                DeferredMessages = messages.Count(m => m.Status == RelayStatus.Deferred),
                DeliveredMessages = messages.Count(m => m.Status == RelayStatus.Delivered),
                FailedMessages = messages.Count(m => m.Status == RelayStatus.Failed),
                ExpiredMessages = messages.Count(m => m.Status == RelayStatus.Expired),
                TotalSize = messages.Sum(m => m.OriginalMessage.Size)
            };

            if (messages.Count > 0)
            {
                stats.OldestMessageTime = messages.Min(m => m.QueuedTime);

                List<RelayMessage> queuedMessages = messages.Where(m => m.LastAttemptTime.HasValue).ToList();
                if (queuedMessages.Count > 0)
                {
                    double totalQueueTime = queuedMessages.Sum(m =>
                        (m.LastAttemptTime!.Value - m.QueuedTime).TotalSeconds);
                    stats.AverageQueueTime = TimeSpan.FromSeconds(totalQueueTime / queuedMessages.Count);
                }

                stats.AverageRetryCount = messages.Average(m => m.RetryCount);
            }

            // Group by priority
            foreach (IGrouping<RelayPriority, RelayMessage> group in messages.GroupBy(m => m.Priority))
            {
                stats.MessagesByPriority[group.Key] = group.Count();
            }

            // Group by smart host
            foreach (IGrouping<string, RelayMessage> group in messages.Where(m => !string.IsNullOrEmpty(m.SmartHost))
                                         .GroupBy(m => m.SmartHost!))
            {
                stats.MessagesBySmartHost[group.Key] = group.Count();
            }

            return Task.FromResult(stats);
        }

        private async Task ProcessDeferredMessagesAsync(CancellationToken cancellationToken)
        {
            DateTime now = DateTime.UtcNow;
            List<RelayMessage> deferredMessages = _messages.Values
                .Where(m => m.Status == RelayStatus.Deferred &&
                           m.NextDeliveryTime.HasValue &&
                           m.NextDeliveryTime.Value <= now)
                .ToList();

            foreach (RelayMessage message in deferredMessages)
            {
                await _readyQueue.Writer.WriteAsync(message.QueueId, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}