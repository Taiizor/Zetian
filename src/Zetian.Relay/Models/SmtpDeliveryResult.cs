using System;
using System.Collections.Generic;

namespace Zetian.Relay.Models
{
    /// <summary>
    /// Represents the result of an SMTP delivery attempt
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of SmtpDeliveryResult
    /// </remarks>
    public class SmtpDeliveryResult(bool success, string? message = null, int responseCode = 0)
    {
        /// <summary>
        /// Gets whether the delivery was successful
        /// </summary>
        public bool Success { get; } = success;

        /// <summary>
        /// Gets the SMTP response message
        /// </summary>
        public string? Message { get; } = message;

        /// <summary>
        /// Gets the SMTP response code
        /// </summary>
        public int ResponseCode { get; } = responseCode;

        /// <summary>
        /// Gets the list of successfully delivered recipients
        /// </summary>
        public IList<string> DeliveredRecipients { get; } = [];

        /// <summary>
        /// Gets the dictionary of failed recipients with error messages
        /// </summary>
        public IDictionary<string, string> FailedRecipients { get; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets the timestamp of the delivery attempt
        /// </summary>
        public DateTime Timestamp { get; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the remote server endpoint
        /// </summary>
        public string? RemoteEndpoint { get; set; }

        /// <summary>
        /// Gets or sets the transaction ID from the remote server
        /// </summary>
        public string? TransactionId { get; set; }

        /// <summary>
        /// Gets or sets additional metadata
        /// </summary>
        public IDictionary<string, object>? Metadata { get; set; }

        /// <summary>
        /// Gets whether this is a temporary failure that can be retried
        /// </summary>
        public bool IsTemporaryFailure => ResponseCode is >= 400 and < 500;

        /// <summary>
        /// Gets whether this is a permanent failure
        /// </summary>
        public bool IsPermanentFailure => ResponseCode is >= 500 and < 600;

        /// <summary>
        /// Creates a successful delivery result
        /// </summary>
        public static SmtpDeliveryResult CreateSuccess(
            IEnumerable<string> recipients,
            string? transactionId = null)
        {
            SmtpDeliveryResult result = new(true, "Message delivered successfully", 250);
            foreach (string recipient in recipients)
            {
                result.DeliveredRecipients.Add(recipient);
            }
            result.TransactionId = transactionId;
            return result;
        }

        /// <summary>
        /// Creates a failed delivery result
        /// </summary>
        public static SmtpDeliveryResult CreateFailure(
            string error,
            int code = 550,
            bool isTemporary = false)
        {
            return new SmtpDeliveryResult(false, error, isTemporary ? 451 : code);
        }

        /// <summary>
        /// Creates a partial delivery result
        /// </summary>
        public static SmtpDeliveryResult CreatePartial(
            IEnumerable<string> delivered,
            IDictionary<string, string> failed)
        {
            SmtpDeliveryResult result = new(false, "Partial delivery", 250);
            foreach (string recipient in delivered)
            {
                result.DeliveredRecipients.Add(recipient);
            }
            foreach (KeyValuePair<string, string> failure in failed)
            {
                result.FailedRecipients[failure.Key] = failure.Value;
            }
            return result;
        }
    }
}