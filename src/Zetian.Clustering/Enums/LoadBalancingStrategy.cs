namespace Zetian.Clustering.Enums
{
    /// <summary>
    /// Load balancing strategies
    /// </summary>
    public enum LoadBalancingStrategy
    {
        /// <summary>
        /// Round-robin distribution
        /// </summary>
        RoundRobin,

        /// <summary>
        /// Least connections
        /// </summary>
        LeastConnections,

        /// <summary>
        /// Weighted round-robin
        /// </summary>
        WeightedRoundRobin,

        /// <summary>
        /// Random selection
        /// </summary>
        Random,

        /// <summary>
        /// IP hash-based routing
        /// </summary>
        IpHash,

        /// <summary>
        /// Resource-based routing
        /// </summary>
        ResourceBased,

        /// <summary>
        /// Custom strategy
        /// </summary>
        Custom
    }
}