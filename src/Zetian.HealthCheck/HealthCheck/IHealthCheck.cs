using System.Threading;
using System.Threading.Tasks;

namespace Zetian.HealthCheck
{
    /// <summary>
    /// Represents a health check
    /// </summary>
    public interface IHealthCheck
    {
        /// <summary>
        /// Checks the health status
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Health check result</returns>
        Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default);
    }
}