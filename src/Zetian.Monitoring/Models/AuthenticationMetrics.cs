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
}