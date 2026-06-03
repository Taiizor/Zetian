using System;
using System.Net.Mail;
using Zetian.Relay.Abstractions;
using Zetian.Relay.Enums;

namespace Zetian.Relay.Models.EventArgs
{
    /// <summary>
    /// Event arguments raised by the relay service for delivery lifecycle
    /// events (delivered, bounced, deferred and expired). Useful for logging
    /// delivery outcomes to an external store such as a database.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of <see cref="RelayDeliveryEventArgs"/>.
    /// </remarks>
    public class RelayDeliveryEventArgs(IRelayMessage message) : System.EventArgs
    {
        /// <summary>
        /// Gets the relay message the event relates to.
        /// </summary>
        public IRelayMessage Message { get; } = message ?? throw new ArgumentNullException(nameof(message));

        /// <summary>
        /// Gets the unique queue ID of the message.
        /// </summary>
        public string QueueId => Message.QueueId;

        /// <summary>
        /// Gets the sender address of the message, if any.
        /// </summary>
        public MailAddress? From => Message.From;

        /// <summary>
        /// Gets the current status of the message at the time the event was raised.
        /// </summary>
        public RelayStatus Status => Message.Status;

        /// <summary>
        /// Gets the target smart host the message was routed to, when known.
        /// </summary>
        public string? SmartHost => Message.SmartHost;

        /// <summary>
        /// Gets the number of delivery attempts made so far.
        /// </summary>
        public int RetryCount => Message.RetryCount;

        /// <summary>
        /// Gets the result of the SMTP delivery attempt, when available.
        /// Populated for the delivered, bounced and deferred events that follow
        /// an actual delivery attempt; <c>null</c> when no attempt produced a result
        /// (for example the expired event, or a transport-level exception).
        /// </summary>
        public SmtpDeliveryResult? Result { get; init; }

        /// <summary>
        /// Gets the error or diagnostic message, when available.
        /// Populated for the bounced, deferred and expired events.
        /// </summary>
        public string? Error { get; init; }

        /// <summary>
        /// Gets the scheduled time of the next delivery attempt.
        /// Populated only for the deferred event.
        /// </summary>
        public DateTime? NextRetryTime { get; init; }

        /// <summary>
        /// Gets the UTC timestamp at which the event was raised.
        /// </summary>
        public DateTime Timestamp { get; } = DateTime.UtcNow;
    }
}