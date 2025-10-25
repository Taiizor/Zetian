namespace Zetian.Storage.AzureBlob.Enums
{
    /// <summary>
    /// Azure Blob access tier options
    /// </summary>
    public enum BlobAccessTier
    {
        /// <summary>
        /// Hot tier for frequently accessed data
        /// </summary>
        Hot,

        /// <summary>
        /// Cool tier for infrequently accessed data
        /// </summary>
        Cool,

        /// <summary>
        /// Archive tier for rarely accessed data
        /// </summary>
        Archive
    }
}