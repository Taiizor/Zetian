using System;
using System.Threading;
using System.Threading.Tasks;

namespace Zetian.Clustering.Abstractions
{
    /// <summary>
    /// Represents a distributed lock
    /// </summary>
    public interface IDistributedLock : IAsyncDisposable
    {
        /// <summary>
        /// Lock identifier
        /// </summary>
        string LockId { get; }

        /// <summary>
        /// Resource being locked
        /// </summary>
        string Resource { get; }

        /// <summary>
        /// Whether the lock is still held
        /// </summary>
        bool IsHeld { get; }

        /// <summary>
        /// Extends the lock TTL
        /// </summary>
        Task<bool> ExtendAsync(TimeSpan ttl, CancellationToken cancellationToken = default);

        /// <summary>
        /// Releases the lock
        /// </summary>
        Task ReleaseAsync(CancellationToken cancellationToken = default);
    }
}