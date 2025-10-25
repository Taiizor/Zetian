namespace Zetian.Storage.AmazonS3.Enums
{
    /// <summary>
    /// S3 object naming format options
    /// </summary>
    public enum S3NamingFormat
    {
        /// <summary>
        /// Flat structure (all objects in prefix)
        /// </summary>
        Flat,

        /// <summary>
        /// Hierarchical by date (yyyy/MM/dd/)
        /// </summary>
        DateHierarchy,

        /// <summary>
        /// Year and month folders (yyyy-MM/)
        /// </summary>
        YearMonth,

        /// <summary>
        /// Hourly partitions for high volume (yyyy/MM/dd/HH/)
        /// </summary>
        HourlyPartition
    }
}