using System.Collections.Generic;

namespace Zetian.Monitoring.Models
{
    /// <summary>
    /// Represents authentication metrics
    /// </summary>
    public class AuthenticationMetrics
    {
        /// <summary>
        /// Gets or sets total authentication attempts
        /// </summary>
        public long TotalAttempts { get; set; }

        /// <summary>
        /// Gets or sets successful authentications
        /// </summary>
        public long SuccessCount { get; set; }

        /// <summary>
        /// Gets or sets failed authentications
        /// </summary>
        public long FailureCount { get; set; }

        /// <summary>
        /// Gets the success rate
        /// </summary>
        public double SuccessRate => TotalAttempts > 0 ? (double)SuccessCount / TotalAttempts * 100 : 0;

        /// <summary>
        /// Gets metrics per authentication mechanism
        /// </summary>
        public Dictionary<string, MechanismMetrics> PerMechanism { get; } = [];

        /// <summary>
        /// Gets or sets the number of unique users authenticated
        /// </summary>
        public int UniqueUsers { get; set; }

        /// <summary>
        /// Gets or sets the number of brute force attempts detected
        /// </summary>
        public long BruteForceAttempts { get; set; }
    }

    /// <summary>
    /// Metrics for a specific authentication mechanism
    /// </summary>
    public class MechanismMetrics
    {
        /// <summary>
        /// Gets or sets the mechanism name
        /// </summary>
        public string Mechanism { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets total attempts
        /// </summary>
        public long Attempts { get; set; }

        /// <summary>
        /// Gets or sets successful attempts
        /// </summary>
        public long Successes { get; set; }

        /// <summary>
        /// Gets or sets failed attempts
        /// </summary>
        public long Failures { get; set; }

        /// <summary>
        /// Gets the success rate
        /// </summary>
        public double SuccessRate => Attempts > 0 ? (double)Successes / Attempts * 100 : 0;

        /// <summary>
        /// Gets or sets average duration in milliseconds
        /// </summary>
        public double AverageDurationMs { get; set; }
    }
}