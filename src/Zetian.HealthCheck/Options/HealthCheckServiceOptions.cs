using System;
using System.Collections.Generic;

namespace Zetian.HealthCheck.Options
{
    /// <summary>
    /// Options for health check service
    /// </summary>
    public class HealthCheckServiceOptions
    {
        /// <summary>
        /// Gets or sets the status code to return when health check times out
        /// Default is 503 (Service Unavailable)
        /// </summary>
        public int TimeoutStatusCode { get; set; } = 503;

        /// <summary>
        /// Gets or sets the status code for degraded health
        /// </summary>
        public int DegradedStatusCode { get; set; } = 200; // Some prefer 200, others 218

        /// <summary>
        /// Gets or sets whether to fail fast on timeout (return immediately) or wait for all checks
        /// Default is true (fail fast)
        /// </summary>
        public bool FailFastOnTimeout { get; set; } = true;

        /// <summary>
        /// Gets or sets the overall timeout for all health checks to complete
        /// Default is 30 seconds
        /// </summary>
        public TimeSpan TotalTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets the HTTP prefixes to listen on
        /// </summary>
        public List<string> Prefixes { get; set; } = new() { "http://localhost:8080/health/" };

        /// <summary>
        /// Gets or sets the timeout for individual health checks
        /// Default is 10 seconds
        /// </summary>
        public TimeSpan IndividualCheckTimeout { get; set; } = TimeSpan.FromSeconds(10);
    }
}