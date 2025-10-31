using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Zetian.Clustering.Abstractions
{
    /// <summary>
    /// Interface for load balancing strategy
    /// </summary>
    public interface ILoadBalancer
    {
        /// <summary>
        /// Selects a node for handling a session
        /// </summary>
        Task<IClusterNode?> SelectNodeAsync(
            ISessionInfo sessionInfo,
            IReadOnlyCollection<IClusterNode> availableNodes,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates node statistics after selection
        /// </summary>
        Task UpdateStatisticsAsync(
            IClusterNode node,
            bool success,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Resets statistics
        /// </summary>
        Task ResetStatisticsAsync(CancellationToken cancellationToken = default);
    }
}