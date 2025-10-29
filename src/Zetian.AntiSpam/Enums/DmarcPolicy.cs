namespace Zetian.AntiSpam.Enums
{
    /// <summary>
    /// DMARC policy actions
    /// </summary>
    public enum DmarcPolicy
    {
        /// <summary>
        /// No action, monitoring only
        /// </summary>
        None,

        /// <summary>
        /// Quarantine the message
        /// </summary>
        Quarantine,

        /// <summary>
        /// Reject the message
        /// </summary>
        Reject
    }
}