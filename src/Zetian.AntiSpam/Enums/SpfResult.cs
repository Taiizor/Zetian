namespace Zetian.AntiSpam.Enums
{
    /// <summary>
    /// SPF check results as defined in RFC 7208
    /// </summary>
    public enum SpfResult
    {
        /// <summary>
        /// No SPF record found
        /// </summary>
        None,

        /// <summary>
        /// SPF record syntax error
        /// </summary>
        PermError,

        /// <summary>
        /// Temporary error during SPF check
        /// </summary>
        TempError,

        /// <summary>
        /// Authorized sender (pass)
        /// </summary>
        Pass,

        /// <summary>
        /// Not authorized but not forbidden
        /// </summary>
        Neutral,

        /// <summary>
        /// Weak statement that sender is not authorized
        /// </summary>
        SoftFail,

        /// <summary>
        /// Sender is not authorized (fail)
        /// </summary>
        Fail
    }
}