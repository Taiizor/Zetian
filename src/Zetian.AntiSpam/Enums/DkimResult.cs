namespace Zetian.AntiSpam.Enums
{
    /// <summary>
    /// DKIM verification results
    /// </summary>
    public enum DkimResult
    {
        /// <summary>
        /// No DKIM signature found
        /// </summary>
        None,

        /// <summary>
        /// DKIM signature is valid
        /// </summary>
        Pass,

        /// <summary>
        /// DKIM signature verification failed
        /// </summary>
        Fail,

        /// <summary>
        /// Temporary failure (DNS timeout, etc.)
        /// </summary>
        TempError,

        /// <summary>
        /// Permanent error (invalid signature format, missing required fields)
        /// </summary>
        PermError,

        /// <summary>
        /// Policy violation (e.g., expired signature)
        /// </summary>
        Policy,

        /// <summary>
        /// Neutral result (signature exists but not verified)
        /// </summary>
        Neutral
    }
}