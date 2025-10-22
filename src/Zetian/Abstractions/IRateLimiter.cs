using System.Net;
using System.Threading.Tasks;

namespace Zetian.Abstractions
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
}