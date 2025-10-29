using System;

namespace Zetian.AntiSpam.Models
{
    /// <summary>
    /// Represents the result of a spam check
    /// </summary>
    /// <remarks>
    /// Creates a new spam check result
    /// </remarks>
    public class SpamCheckResult(bool isSpam, double score, string? reason = null, string? details = null)
    {
        /// <summary>
        /// Gets whether the message is spam
        /// </summary>
        public bool IsSpam { get; } = isSpam;

        /// <summary>
        /// Gets the spam score (0-100, higher is more spammy)
        /// </summary>
        public double Score { get; } = score;

        /// <summary>
        /// Gets the reason for the spam classification
        /// </summary>
        public string? Reason { get; } = reason;

        /// <summary>
        /// Gets additional details about the check
        /// </summary>
        public string? Details { get; } = details;

        /// <summary>
        /// Gets when the check was performed
        /// </summary>
        public DateTime CheckedAt { get; } = DateTime.UtcNow;

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