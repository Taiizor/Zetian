namespace Zetian.Clustering.Enums
{
    /// <summary>
    /// Cluster states
    /// </summary>
    public enum ClusterState
    {
        /// <summary>
        /// Cluster is forming
        /// </summary>
        Forming,

        /// <summary>
        /// Cluster is healthy
        /// </summary>
        Healthy,

        /// <summary>
        /// Cluster has degraded performance
        /// </summary>
        Degraded,

        /// <summary>
        /// Cluster has lost quorum
        /// </summary>
        NoQuorum,

        /// <summary>
        /// Cluster is partitioned
        /// </summary>
        Partitioned,

        /// <summary>
        /// Cluster is rebalancing
        /// </summary>
        Rebalancing,

        /// <summary>
        /// Cluster is shutting down
        /// </summary>
        ShuttingDown
    }
}