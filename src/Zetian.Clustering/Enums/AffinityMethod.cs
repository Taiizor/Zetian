namespace Zetian.Clustering.Enums
{
    /// <summary>
    /// Session affinity methods
    /// </summary>
    public enum AffinityMethod
    {
        /// <summary>
        /// No affinity
        /// </summary>
        None,

        /// <summary>
        /// Source IP-based affinity
        /// </summary>
        SourceIp,

        /// <summary>
        /// Session ID-based affinity
        /// </summary>
        SessionId,

        /// <summary>
        /// Cookie-based affinity
        /// </summary>
        Cookie,

        /// <summary>
        /// Custom affinity logic
        /// </summary>
        Custom
    }
}