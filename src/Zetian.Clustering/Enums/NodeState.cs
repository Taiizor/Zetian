namespace Zetian.Clustering.Enums
{
    /// <summary>
    /// Node states
    /// </summary>
    public enum NodeState
    {
        /// <summary>
        /// Node is initializing
        /// </summary>
        Initializing,

        /// <summary>
        /// Node is joining cluster
        /// </summary>
        Joining,

        /// <summary>
        /// Node is active and healthy
        /// </summary>
        Active,

        /// <summary>
        /// Node is suspected to be failing
        /// </summary>
        Suspected,

        /// <summary>
        /// Node has failed
        /// </summary>
        Failed,

        /// <summary>
        /// Node is leaving cluster
        /// </summary>
        Leaving,

        /// <summary>
        /// Node is in maintenance mode
        /// </summary>
        Maintenance,

        /// <summary>
        /// Node is shutdown
        /// </summary>
        Shutdown
    }
}