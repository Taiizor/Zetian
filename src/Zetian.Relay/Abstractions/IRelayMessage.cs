using System;
using System.Collections.Generic;
using System.Net.Mail;
using Zetian.Abstractions;
using Zetian.Relay.Enums;

namespace Zetian.Relay.Abstractions
{
    /// <summary>
    /// Represents a message in the relay queue
    /// </summary>
    public interface IRelayMessage
    {
        /// <summary>
        /// Gets the unique queue ID
        /// </summary>
        string QueueId { get; }

        /// <summary>
        /// Gets the original message
        /// </summary>
        ISmtpMessage OriginalMessage { get; }

        /// <summary>
        /// Gets the sender address
        /// </summary>
        MailAddress? From { get; }

        /// <summary>
        /// Gets the recipient addresses
        /// </summary>
        IReadOnlyList<MailAddress> Recipients { get; }

        /// <summary>
        /// Gets the list of pending recipients
        /// </summary>
        IReadOnlyList<MailAddress> PendingRecipients { get; }

        /// <summary>
        /// Gets the list of delivered recipients
        /// </summary>
        IReadOnlyList<MailAddress> DeliveredRecipients { get; }

        /// <summary>
        /// Gets the list of failed recipients
        /// </summary>
        IReadOnlyList<MailAddress> FailedRecipients { get; }

        /// <summary>
        /// Gets the current retry count
        /// </summary>
        int RetryCount { get; }

        /// <summary>
        /// Gets the time when the message was queued
        /// </summary>
        DateTime QueuedTime { get; }

        /// <summary>
        /// Gets the next delivery attempt time
        /// </summary>
        DateTime? NextDeliveryTime { get; }

        /// <summary>
        /// Gets the last delivery attempt time
        /// </summary>
        DateTime? LastAttemptTime { get; }

        /// <summary>
        /// Gets the priority of the message
        /// </summary>
        RelayPriority Priority { get; }

        /// <summary>
        /// Gets the current status of the message
        /// </summary>
        RelayStatus Status { get; }

        /// <summary>
        /// Gets the last error message
        /// </summary>
        string? LastError { get; }

        /// <summary>
        /// Gets or sets metadata associated with the message
        /// </summary>
        IDictionary<string, object> Metadata { get; }

        /// <summary>
        /// Gets the target smart host for this message
        /// </summary>
        string? SmartHost { get; }

        /// <summary>
        /// Gets whether the message has expired
        /// </summary>
        bool IsExpired { get; }
    }
}