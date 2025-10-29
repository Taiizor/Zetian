using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using Zetian.Abstractions;
using Zetian.Relay.Abstractions;
using Zetian.Relay.Enums;

namespace Zetian.Relay.Models
{
    /// <summary>
    /// Implementation of a relay queue message
    /// </summary>
    public class RelayMessage : IRelayMessage
    {
        private readonly List<MailAddress> _pendingRecipients;
        private readonly List<MailAddress> _deliveredRecipients;
        private readonly List<MailAddress> _failedRecipients;

        public RelayMessage(
            ISmtpMessage originalMessage,
            string? smartHost = null,
            RelayPriority priority = RelayPriority.Normal,
            TimeSpan? messageLifetime = null)
        {
            OriginalMessage = originalMessage ?? throw new ArgumentNullException(nameof(originalMessage));
            QueueId = Guid.NewGuid().ToString("N");
            QueuedTime = DateTime.UtcNow;
            Priority = priority;
            SmartHost = smartHost;
            Status = RelayStatus.Queued;
            Metadata = new Dictionary<string, object>();

            From = originalMessage.From;
            Recipients = originalMessage.Recipients.ToList();

            _pendingRecipients = [.. Recipients];
            _deliveredRecipients = [];
            _failedRecipients = [];

            MessageLifetime = messageLifetime ?? TimeSpan.FromDays(4); // Default 4 days
            ExpirationTime = QueuedTime.Add(MessageLifetime);
        }

        public string QueueId { get; }
        public ISmtpMessage OriginalMessage { get; }
        public MailAddress? From { get; }
        public IReadOnlyList<MailAddress> Recipients { get; }
        public IReadOnlyList<MailAddress> PendingRecipients => _pendingRecipients;
        public IReadOnlyList<MailAddress> DeliveredRecipients => _deliveredRecipients;
        public IReadOnlyList<MailAddress> FailedRecipients => _failedRecipients;
        public int RetryCount { get; private set; }
        public DateTime QueuedTime { get; }
        public DateTime? NextDeliveryTime { get; private set; }
        public DateTime? LastAttemptTime { get; private set; }
        public RelayPriority Priority { get; set; }
        public RelayStatus Status { get; private set; }
        public string? LastError { get; private set; }
        public IDictionary<string, object> Metadata { get; }
        public string? SmartHost { get; set; }
        public TimeSpan MessageLifetime { get; }
        public DateTime ExpirationTime { get; }
        public bool IsExpired => DateTime.UtcNow > ExpirationTime;

        /// <summary>
        /// Marks the message as being processed
        /// </summary>
        public void MarkInProgress()
        {
            Status = RelayStatus.InProgress;
            LastAttemptTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Marks recipients as successfully delivered
        /// </summary>
        public void MarkDelivered(IEnumerable<string> recipients)
        {
            foreach (string recipient in recipients)
            {
                MailAddress? address = _pendingRecipients.FirstOrDefault(r =>
                    r.Address.Equals(recipient, StringComparison.OrdinalIgnoreCase));

                if (address != null)
                {
                    _pendingRecipients.Remove(address);
                    _deliveredRecipients.Add(address);
                }
            }

            UpdateStatus();
        }

        /// <summary>
        /// Marks recipients as failed
        /// </summary>
        public void MarkFailed(IEnumerable<string> recipients, string error)
        {
            LastError = error;

            foreach (string recipient in recipients)
            {
                MailAddress? address = _pendingRecipients.FirstOrDefault(r =>
                    r.Address.Equals(recipient, StringComparison.OrdinalIgnoreCase));

                if (address != null)
                {
                    _pendingRecipients.Remove(address);
                    _failedRecipients.Add(address);
                }
            }

            UpdateStatus();
        }

        /// <summary>
        /// Marks all pending recipients as failed
        /// </summary>
        public void MarkAllFailed(string error)
        {
            LastError = error;
            _failedRecipients.AddRange(_pendingRecipients);
            _pendingRecipients.Clear();
            Status = RelayStatus.Failed;
        }

        /// <summary>
        /// Schedules the next delivery attempt
        /// </summary>
        public void ScheduleRetry(TimeSpan delay)
        {
            RetryCount++;
            NextDeliveryTime = DateTime.UtcNow.Add(delay);
            Status = RelayStatus.Deferred;
        }

        /// <summary>
        /// Calculates the retry delay using exponential backoff
        /// </summary>
        public TimeSpan CalculateRetryDelay()
        {
            // Exponential backoff: 1min, 2min, 4min, 8min, 16min, 32min, 1hr, 2hr, 4hr...
            var baseDelay = TimeSpan.FromMinutes(1);
            var maxDelay = TimeSpan.FromHours(4);

            var delay = TimeSpan.FromMilliseconds(
                baseDelay.TotalMilliseconds * Math.Pow(2, Math.Min(RetryCount, 10)));

            return delay > maxDelay ? maxDelay : delay;
        }

        /// <summary>
        /// Marks the message as expired
        /// </summary>
        public void MarkExpired()
        {
            Status = RelayStatus.Expired;
            LastError = "Message expired";
        }

        /// <summary>
        /// Marks the message as cancelled
        /// </summary>
        public void Cancel(string reason = "Cancelled by user")
        {
            Status = RelayStatus.Cancelled;
            LastError = reason;
        }

        private void UpdateStatus()
        {
            if (_pendingRecipients.Count == 0)
            {
                if (_failedRecipients.Count == 0)
                {
                    Status = RelayStatus.Delivered;
                }
                else if (_deliveredRecipients.Count > 0)
                {
                    Status = RelayStatus.PartiallyDelivered;
                }
                else
                {
                    Status = RelayStatus.Failed;
                }
            }
            else if (_deliveredRecipients.Count > 0 || _failedRecipients.Count > 0)
            {
                Status = RelayStatus.Deferred; // Still has pending recipients
            }
        }
    }
}