namespace Zetian.Relay.Enums
{
    /// <summary>
    /// Relay message status
    /// </summary>
    public enum RelayStatus
    {
        /// <summary>
        /// Message is queued for delivery
        /// </summary>
        Queued = 0,

        /// <summary>
        /// Message is currently being delivered
        /// </summary>
        InProgress = 1,

        /// <summary>
        /// Message has been delivered successfully
        /// </summary>
        Delivered = 2,

        /// <summary>
        /// Message delivery failed permanently
        /// </summary>
        Failed = 3,

        /// <summary>
        /// Message has been deferred for later delivery
        /// </summary>
        Deferred = 4,

        /// <summary>
        /// Message has expired and won't be delivered
        /// </summary>
        Expired = 5,

        /// <summary>
        /// Message has been cancelled
        /// </summary>
        Cancelled = 6,

        /// <summary>
        /// Message has been partially delivered (some recipients succeeded)
        /// </summary>
        PartiallyDelivered = 7
    }
}