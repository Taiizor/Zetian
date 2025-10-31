using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Zetian.Clustering.Abstractions
{
    /// <summary>
    /// Interface for distributed state storage
    /// </summary>
    public interface IStateStore
    {
        /// <summary>
        /// Gets a value from the state store
        /// </summary>
        Task<byte[]?> GetAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets a value in the state store
        /// </summary>
        Task<bool> SetAsync(string key, byte[] value, TimeSpan? ttl = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a value from the state store
        /// </summary>
        Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a key exists
        /// </summary>
        Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets multiple values
        /// </summary>
        Task<IDictionary<string, byte[]>> GetMultipleAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets multiple values atomically
        /// </summary>
        Task<bool> SetMultipleAsync(IDictionary<string, byte[]> values, TimeSpan? ttl = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Compare and swap operation
        /// </summary>
        Task<bool> CompareAndSwapAsync(string key, byte[] expectedValue, byte[] newValue, CancellationToken cancellationToken = default);

        /// <summary>
        /// Increments a counter
        /// </summary>
        Task<long> IncrementAsync(string key, long delta = 1, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all keys matching a pattern
        /// </summary>
        Task<IEnumerable<string>> GetKeysAsync(string pattern = "*", CancellationToken cancellationToken = default);

        /// <summary>
        /// Acquires a distributed lock
        /// </summary>
        Task<IDistributedLock?> AcquireLockAsync(string resource, TimeSpan ttl, CancellationToken cancellationToken = default);

        /// <summary>
        /// Clears all state
        /// </summary>
        Task ClearAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the size of stored data
        /// </summary>
        Task<long> GetSizeAsync(CancellationToken cancellationToken = default);
    }
}