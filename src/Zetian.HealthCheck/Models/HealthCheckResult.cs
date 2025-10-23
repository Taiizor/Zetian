using System;
using System.Collections.Generic;
using Zetian.HealthCheck.Enums;

namespace Zetian.HealthCheck.Models
{
    /// <summary>
    /// Represents the result of a health check
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of HealthCheckResult
    /// </remarks>
    public class HealthCheckResult(HealthStatus status, string? description = null, Exception? exception = null, Dictionary<string, object>? data = null)
    {
        /// <summary>
        /// Gets the health status
        /// </summary>
        public HealthStatus Status { get; } = status;

        /// <summary>
        /// Gets the description
        /// </summary>
        public string? Description { get; } = description;

        /// <summary>
        /// Gets the exception if any
        /// </summary>
        public Exception? Exception { get; } = exception;

        /// <summary>
        /// Gets additional data
        /// </summary>
        public Dictionary<string, object> Data { get; } = data ?? new();

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
}