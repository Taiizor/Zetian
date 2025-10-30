namespace Zetian.Clustering.Enums
{
    /// <summary>
    /// Rate limiting algorithms
    /// </summary>
    public enum RateLimitAlgorithm
    {
        /// <summary>
        /// Token bucket algorithm
        /// </summary>
        TokenBucket,

        /// <summary>
        /// Sliding window algorithm
        /// </summary>
        SlidingWindow,

        /// <summary>
        /// Fixed window algorithm
        /// </summary>
        FixedWindow,

        /// <summary>
        /// Leaky bucket algorithm
        /// </summary>
        LeakyBucket
    }
}