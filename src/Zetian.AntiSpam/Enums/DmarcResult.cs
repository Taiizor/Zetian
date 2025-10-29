namespace Zetian.AntiSpam.Enums
{
    /// <summary>
    /// DMARC policy evaluation results
    /// </summary>
    public enum DmarcResult
    {
        /// <summary>
        /// No DMARC record found
        /// </summary>
        None,

        /// <summary>
        /// DMARC check passed
        /// </summary>
        Pass,

        /// <summary>
        /// DMARC check failed
        /// </summary>
        Fail,

        /// <summary>
        /// Temporary error during DMARC check
        /// </summary>
        TempError,

        /// <summary>
        /// Permanent error in DMARC record
        /// </summary>
        PermError
    }
}