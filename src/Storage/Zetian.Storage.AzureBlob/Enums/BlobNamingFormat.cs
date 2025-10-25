namespace Zetian.Storage.AzureBlob.Enums
{
    /// <summary>
    /// Blob naming format options
    /// </summary>
    public enum BlobNamingFormat
    {
        /// <summary>
        /// Flat structure (all blobs in root)
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
        /// Domain-based folders
        /// </summary>
        DomainBased
    }
}