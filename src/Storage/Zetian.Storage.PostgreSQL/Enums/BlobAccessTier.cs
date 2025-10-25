namespace Zetian.Storage.PostgreSQL.Enums
{
    /// <summary>
    /// Partition interval options for PostgreSQL
    /// </summary>
    public enum PartitionInterval
    {
        /// <summary>
        /// Represents a value or setting that is applied or occurs on a daily basis.
        /// </summary>
        Daily,
        /// <summary>
        /// Represents a value or option that occurs on a weekly basis.
        /// </summary>
        Weekly,
        /// <summary>
        /// Represents a value or option that occurs on a monthly basis.
        /// </summary>
        Monthly,
        /// <summary>
        /// Represents a value or option that occurs on a yearly basis.
        /// </summary>
        Yearly
    }
}