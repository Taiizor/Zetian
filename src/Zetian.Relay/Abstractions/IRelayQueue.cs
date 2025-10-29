using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Zetian.Abstractions;
using Zetian.Relay.Enums;

namespace Zetian.Relay.Abstractions
{
    /// <summary>
    /// Interface for managing the relay message queue
    /// </summary>
    public interface IRelayQueue
    {
        /// <summary>
        /// Gets the number of messages in the queue
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets the number of active deliveries
        /// </summary>
        int ActiveDeliveries { get; }

        /// <summary>
        /// Enqueues a message for relay
        /// </summary>
        Task<IRelayMessage> EnqueueAsync(
            ISmtpMessage message,
            string? smartHost = null,
            RelayPriority priority = RelayPriority.Normal,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Dequeues the next message for delivery
        /// </summary>
        Task<IRelayMessage?> DequeueAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a message by queue ID
        /// </summary>
        Task<IRelayMessage?> GetMessageAsync(string queueId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the status of a message
        /// </summary>
        Task UpdateStatusAsync(
            string queueId,
            RelayStatus status,
            string? error = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks recipients as delivered
        /// </summary>
        Task MarkDeliveredAsync(
            string queueId,
            IEnumerable<string> recipients,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks recipients as failed
        /// </summary>
        Task MarkFailedAsync(
            string queueId,
            IEnumerable<string> recipients,
            string error,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Reschedules a message for retry
        /// </summary>
        Task RescheduleAsync(
            string queueId,
            TimeSpan delay,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a message from the queue
        /// </summary>
        Task<bool> RemoveAsync(string queueId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all messages in the queue
        /// </summary>
        Task<IReadOnlyList<IRelayMessage>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets messages by status
        /// </summary>
        Task<IReadOnlyList<IRelayMessage>> GetByStatusAsync(
            RelayStatus status,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Clears expired messages from the queue
        /// </summary>
        Task<int> ClearExpiredAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets queue statistics
        /// </summary>
        Task<RelayQueueStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
    }
}