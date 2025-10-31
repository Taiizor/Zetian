using System.Collections.Generic;
using System.Net;

namespace Zetian.Clustering.Abstractions
{
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
        IPAddress ClientIp { get; }

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