namespace Zetian.Clustering.Enums
{
    /// <summary>
    /// Consistency levels for distributed operations
    /// </summary>
    public enum ConsistencyLevel
    {
        /// <summary>
        /// Operation succeeds after writing to one node
        /// </summary>
        One,

        /// <summary>
        /// Operation succeeds after writing to a quorum of nodes
        /// </summary>
        Quorum,

        /// <summary>
        /// Operation succeeds after writing to all nodes
        /// </summary>
        All,

        /// <summary>
        /// Operation succeeds after writing to local node
        /// </summary>
        Local,

        /// <summary>
        /// Operation succeeds after writing to local quorum
        /// </summary>
        LocalQuorum,

        /// <summary>
        /// Operation succeeds after writing to each data center quorum
        /// </summary>
        EachQuorum
    }
}