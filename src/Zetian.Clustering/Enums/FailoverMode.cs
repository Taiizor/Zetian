namespace Zetian.Clustering.Enums
{
    /// <summary>
    /// Failover modes
    /// </summary>
    public enum FailoverMode
    {
        /// <summary>
        /// Automatic failover
        /// </summary>
        Automatic,

        /// <summary>
        /// Manual failover
        /// </summary>
        Manual,

        /// <summary>
        /// No failover (drop connection)
        /// </summary>
        None
    }
}