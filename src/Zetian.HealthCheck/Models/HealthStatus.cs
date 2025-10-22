namespace Zetian.HealthCheck.Models
{
    /// <summary>
    /// Health status enumeration
    /// </summary>
    public enum HealthStatus
    {
        /// <summary>
        /// The component is healthy
        /// </summary>
        Healthy = 0,

        /// <summary>
        /// The component is degraded but still functional
        /// </summary>
        Degraded = 1,

        /// <summary>
        /// The component is unhealthy
        /// </summary>
        Unhealthy = 2
    }
}