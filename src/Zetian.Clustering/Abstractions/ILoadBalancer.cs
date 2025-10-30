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

    /// <summary>
    /// Session information for load balancing decisions
    /// </summary>
    public interface ISessionInfo
    {
        /// <summary>
        /// Session ID
        /// </summary>
        string SessionId { get; }

        /// <summary>
        /// Client IP address
        /// </summary>
        System.Net.IPAddress ClientIp { get; }

        /// <summary>
        /// Client port
        /// </summary>
        int ClientPort { get; }

        /// <summary>
        /// Estimated session size
        /// </summary>
        long EstimatedSize { get; }

        /// <summary>
        /// Session priority
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Session metadata
        /// </summary>
        IReadOnlyDictionary<string, string> Metadata { get; }
    }
}