using System;

namespace Zetian.AntiSpam.Models
{
    /// <summary>
    /// Represents the result of a spam check
    /// </summary>
    public class SpamCheckResult
    {
        /// <summary>
        /// Creates a new spam check result
        /// </summary>
        public SpamCheckResult(bool isSpam, double score, string? reason = null, string? details = null)
        {
            IsSpam = isSpam;
            Score = score;
            Reason = reason;
            Details = details;
            CheckedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets whether the message is spam
        /// </summary>
        public bool IsSpam { get; }

        /// <summary>
        /// Gets the spam score (0-100, higher is more spammy)
        /// </summary>
        public double Score { get; }

        /// <summary>
        /// Gets the reason for the spam classification
        /// </summary>
        public string? Reason { get; }

        /// <summary>
        /// Gets additional details about the check
        /// </summary>
        public string? Details { get; }

        /// <summary>
        /// Gets when the check was performed
        /// </summary>
        public DateTime CheckedAt { get; }

        /// <summary>
        /// Creates a result for a clean message
        /// </summary>
        public static SpamCheckResult Clean(double score = 0, string? details = null)
        {
            return new SpamCheckResult(false, score, null, details);
        }

        /// <summary>
        /// Creates a result for a spam message
        /// </summary>
        public static SpamCheckResult Spam(double score, string reason, string? details = null)
        {
            return new SpamCheckResult(true, score, reason, details);
        }
    }
}
