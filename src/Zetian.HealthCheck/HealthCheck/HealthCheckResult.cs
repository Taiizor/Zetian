using System;
using System.Collections.Generic;

namespace Zetian.HealthCheck
{
    /// <summary>
    /// Represents the result of a health check
    /// </summary>
    public class HealthCheckResult
    {
        /// <summary>
        /// Initializes a new instance of HealthCheckResult
        /// </summary>
        public HealthCheckResult(HealthStatus status, string? description = null, Exception? exception = null, Dictionary<string, object>? data = null)
        {
            Status = status;
            Description = description;
            Exception = exception;
            Data = data ?? new();
        }

        /// <summary>
        /// Gets the health status
        /// </summary>
        public HealthStatus Status { get; }

        /// <summary>
        /// Gets the description
        /// </summary>
        public string? Description { get; }

        /// <summary>
        /// Gets the exception if any
        /// </summary>
        public Exception? Exception { get; }

        /// <summary>
        /// Gets additional data
        /// </summary>
        public Dictionary<string, object> Data { get; }

        /// <summary>
        /// Creates a healthy result
        /// </summary>
        public static HealthCheckResult Healthy(string? description = null, Dictionary<string, object>? data = null)
        {
            return new HealthCheckResult(HealthStatus.Healthy, description, null, data);
        }

        /// <summary>
        /// Creates a degraded result
        /// </summary>
        public static HealthCheckResult Degraded(string? description = null, Exception? exception = null, Dictionary<string, object>? data = null)
        {
            return new HealthCheckResult(HealthStatus.Degraded, description, exception, data);
        }

        /// <summary>
        /// Creates an unhealthy result
        /// </summary>
        public static HealthCheckResult Unhealthy(string? description = null, Exception? exception = null, Dictionary<string, object>? data = null)
        {
            return new HealthCheckResult(HealthStatus.Unhealthy, description, exception, data);
        }
    }

    /// <summary>
    /// Health status
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