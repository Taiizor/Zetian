using System;
using System.Net;
using System.Threading.Tasks;

namespace Zetian.Extensions.RateLimiting
{
    /// <summary>
    /// Interface for rate limiting
    /// </summary>
    public interface IRateLimiter
    {
        /// <summary>
        /// Checks if a request is allowed
        /// </summary>
        Task<bool> IsAllowedAsync(string key);

        /// <summary>
        /// Checks if a request from an IP is allowed
        /// </summary>
        Task<bool> IsAllowedAsync(IPAddress ipAddress);

        /// <summary>
        /// Records a request
        /// </summary>
        Task RecordRequestAsync(string key);

        /// <summary>
        /// Records a request from an IP
        /// </summary>
        Task RecordRequestAsync(IPAddress ipAddress);

        /// <summary>
        /// Resets the rate limit for a key
        /// </summary>
        Task ResetAsync(string key);

        /// <summary>
        /// Gets the remaining requests for a key
        /// </summary>
        Task<int> GetRemainingAsync(string key);
    }

    /// <summary>
    /// Rate limit configuration
    /// </summary>
    public class RateLimitConfiguration
    {
        /// <summary>
        /// Gets or sets the maximum requests per window
        /// </summary>
        public int MaxRequests { get; set; } = 100;

        /// <summary>
        /// Gets or sets the time window
        /// </summary>
        public TimeSpan Window { get; set; } = TimeSpan.FromHours(1);

        /// <summary>
        /// Gets or sets whether to use sliding window
        /// </summary>
        public bool UseSlidingWindow { get; set; } = false;

        /// <summary>
        /// Creates a per-minute rate limit
        /// </summary>
        public static RateLimitConfiguration PerMinute(int maxRequests)
        {
            return new RateLimitConfiguration
            {
                MaxRequests = maxRequests,
                Window = TimeSpan.FromMinutes(1)
            };
        }

        /// <summary>
        /// Creates a per-hour rate limit
        /// </summary>
        public static RateLimitConfiguration PerHour(int maxRequests)
        {
            return new RateLimitConfiguration
            {
                MaxRequests = maxRequests,
                Window = TimeSpan.FromHours(1)
            };
        }

        /// <summary>
        /// Creates a per-day rate limit
        /// </summary>
        public static RateLimitConfiguration PerDay(int maxRequests)
        {
            return new RateLimitConfiguration
            {
                MaxRequests = maxRequests,
                Window = TimeSpan.FromDays(1)
            };
        }
    }
}