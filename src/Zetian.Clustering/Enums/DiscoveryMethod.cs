namespace Zetian.Clustering.Enums
{
    /// <summary>
    /// Node discovery methods
    /// </summary>
    public enum DiscoveryMethod
    {
        /// <summary>
        /// Static list of seed nodes
        /// </summary>
        Static,

        /// <summary>
        /// DNS-based discovery
        /// </summary>
        Dns,

        /// <summary>
        /// Multicast discovery
        /// </summary>
        Multicast,

        /// <summary>
        /// Kubernetes service discovery
        /// </summary>
        Kubernetes,

        /// <summary>
        /// Consul service discovery
        /// </summary>
        Consul,

        /// <summary>
        /// Etcd service discovery
        /// </summary>
        Etcd,

        /// <summary>
        /// Custom discovery implementation
        /// </summary>
        Custom
    }
}