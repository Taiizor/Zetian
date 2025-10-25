using Microsoft.Extensions.Logging;
using Zetian.Server;

namespace Zetian.Storage.Extensions
{
    /// <summary>
    /// Base extension methods for storage configuration
    /// </summary>
    public static class StorageExtensions
    {
        /// <summary>
        /// Helper method to get logger from builder (would need to be implemented in SmtpServerBuilder)
        /// </summary>
        public static ILogger<T>? GetLogger<T>(this SmtpServerBuilder builder)
        {
            // This would need to be implemented in the main Zetian library
            // For now, return null (no logging)
            return null;
        }
    }
}