using System;
using System.Collections.Generic;

namespace Zetian.AntiSpam.Models
{
    /// <summary>
    /// Represents the result of a spam check
    /// </summary>
    public class SpamCheckResult
    {
        /// <summary>
        /// Gets or sets whether the message is considered spam
        /// </summary>
        public bool IsSpam { get; set; }

        /// <summary>
        /// Gets or sets the spam score (0-100, higher is more spammy)
        /// </summary>
        public double Score { get; set; }

        /// <summary>
        /// Gets or sets the threshold score for spam classification
        /// </summary>
        public double Threshold { get; set; } = 50.0;

        /// <summary>
        /// Gets or sets the confidence level (0-1)
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// Gets or sets the checker that produced this result
        /// </summary>
        public string CheckerName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the reasons for the spam classification
        /// </summary>
        public List<string> Reasons { get; set; } = [];

        /// <summary>
        /// Gets or sets detailed check results
        /// </summary>
        public Dictionary<string, object> Details { get; set; } = [];

        /// <summary>
        /// Gets or sets the action to take
        /// </summary>
        public SpamAction Action { get; set; } = SpamAction.None;

        /// <summary>
        /// Gets or sets the SMTP response code
        /// </summary>
        public int? SmtpResponseCode { get; set; }

        /// <summary>
        /// Gets or sets the SMTP response message
        /// </summary>
        public string? SmtpResponseMessage { get; set; }

        /// <summary>
        /// Gets or sets when the check was performed
        /// </summary>
        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets how long the check took
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Creates a result indicating the message is not spam
        /// </summary>
        public static SpamCheckResult NotSpam(string checkerName, double score = 0)
        {
            return new SpamCheckResult
            {
                IsSpam = false,
                Score = score,
                CheckerName = checkerName,
                Action = SpamAction.None,
                Confidence = 1.0 - (score / 100.0)
            };
        }

        /// <summary>
        /// Creates a result indicating the message is spam
        /// </summary>
        public static SpamCheckResult Spam(string checkerName, double score, params string[] reasons)
        {
            return new SpamCheckResult
            {
                IsSpam = true,
                Score = score,
                CheckerName = checkerName,
                Action = SpamAction.Reject,
                Reasons = [.. reasons],
                Confidence = score / 100.0,
                SmtpResponseCode = 550,
                SmtpResponseMessage = "Message rejected as spam"
            };
        }

        /// <summary>
        /// Creates a result indicating the message should be greylisted
        /// </summary>
        public static SpamCheckResult Greylist(string checkerName, string reason)
        {
            return new SpamCheckResult
            {
                IsSpam = false,
                CheckerName = checkerName,
                Action = SpamAction.Greylist,
                Reasons = [reason],
                SmtpResponseCode = 451,
                SmtpResponseMessage = "Greylisted, please try again later"
            };
        }
    }

    /// <summary>
    /// Actions that can be taken based on spam check results
    /// </summary>
    public enum SpamAction
    {
        /// <summary>
        /// No action needed, message is clean
        /// </summary>
        None,

        /// <summary>
        /// Accept the message but mark it as spam
        /// </summary>
        Mark,

        /// <summary>
        /// Quarantine the message for review
        /// </summary>
        Quarantine,

        /// <summary>
        /// Reject the message immediately
        /// </summary>
        Reject,

        /// <summary>
        /// Temporarily defer the message (greylisting)
        /// </summary>
        Greylist,

        /// <summary>
        /// Silently discard the message
        /// </summary>
        Discard
    }
}