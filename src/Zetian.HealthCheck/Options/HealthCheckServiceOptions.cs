using System.Collections.Generic;

namespace Zetian.HealthCheck.Options
{
    /// <summary>
    /// Options for health check service
    /// </summary>
    public class HealthCheckServiceOptions
    {
        /// <summary>
        /// Gets or sets the status code for degraded health
        /// </summary>
        public int DegradedStatusCode { get; set; } = 200; // Some prefer 200, others 218

        /// <summary>
        /// Gets or sets the HTTP prefixes to listen on
        /// </summary>
        public List<string> Prefixes { get; set; } = new() { "http://localhost:8080/health/" };
    }
}