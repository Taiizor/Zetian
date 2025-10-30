namespace Zetian.Monitoring.Models
{
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