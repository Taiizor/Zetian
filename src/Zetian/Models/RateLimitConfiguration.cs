using System;

namespace Zetian.Models
{
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
        /// Gets or sets whether to use sliding window
        /// </summary>
        public bool UseSlidingWindow { get; set; } = false;

        /// <summary>
        /// Gets or sets the time window
        /// </summary>
        public TimeSpan Window { get; set; } = TimeSpan.FromHours(1);

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