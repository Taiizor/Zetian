namespace Zetian.AntiSpam.Enums
{
    /// <summary>
    /// DMARC alignment modes
    /// </summary>
    public enum DmarcAlignment
    {
        /// <summary>
        /// Relaxed alignment (subdomains allowed)
        /// </summary>
        Relaxed,

        /// <summary>
        /// Strict alignment (exact domain match required)
        /// </summary>
        Strict
    }
}