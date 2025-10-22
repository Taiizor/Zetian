namespace Zetian.HealthCheck.Options
{
    /// <summary>
    /// Options for SMTP server health check
    /// </summary>
    public class SmtpHealthCheckOptions
    {
        /// <summary>
        /// Gets or sets whether to check memory usage
        /// </summary>
        public bool CheckMemoryUsage { get; set; } = true;

        /// <summary>
        /// Gets or sets the threshold percentage for degraded status
        /// </summary>
        public double DegradedThresholdPercent { get; set; } = 70;

        /// <summary>
        /// Gets or sets the threshold percentage for unhealthy status
        /// </summary>
        public double UnhealthyThresholdPercent { get; set; } = 90;
    }
}