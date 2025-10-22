using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Zetian.Internal
{
    /// <summary>
    /// Thread-safe connection tracker for IP-based connection limiting
    /// </summary>
    internal sealed class ConnectionTracker : IDisposable
    {
        private readonly ConcurrentDictionary<IPAddress, ConnectionInfo> _connections;
        private readonly ILogger _logger;
        private readonly int _maxConnectionsPerIp;
        private readonly Timer _cleanupTimer;
        private bool _disposed;

        public ConnectionTracker(int maxConnectionsPerIp, ILogger logger)
        {
            _maxConnectionsPerIp = maxConnectionsPerIp;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connections = new ConcurrentDictionary<IPAddress, ConnectionInfo>();

            // Cleanup expired connection infos every 5 minutes
            _cleanupTimer = new Timer(CleanupExpired, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        /// <summary>
        /// Tries to acquire a connection slot for the given IP address
        /// </summary>
        /// <returns>A connection handle if successful, null if limit exceeded</returns>
        public async Task<ConnectionHandle?> TryAcquireAsync(IPAddress ipAddress, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(ipAddress);

            // Get or create connection info atomically
            ConnectionInfo info = _connections.GetOrAdd(ipAddress, _ => new ConnectionInfo(_maxConnectionsPerIp));

            // Acquire the lock and perform the operation atomically
            await info.LockAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // Check if we can acquire a connection
                if (info.CanAcquire())
                {
                    info.IncrementUnsafe();

                    _logger.LogDebug("Connection acquired for {IPAddress}. Current count: {Count}/{Max}",
                        ipAddress, info.GetCountUnsafe(), _maxConnectionsPerIp);

                    return new ConnectionHandle(ipAddress, info, this);
                }

                _logger.LogWarning("Connection limit exceeded for {IPAddress}. Max: {Max}",
                    ipAddress, _maxConnectionsPerIp);

                return null;
            }
            finally
            {
                info.ReleaseLock();
            }
        }

        /// <summary>
        /// Gets the current connection count for an IP
        /// </summary>
        public async Task<int> GetConnectionCountAsync(IPAddress ipAddress)
        {
            if (_connections.TryGetValue(ipAddress, out ConnectionInfo? info))
            {
                await info.LockAsync().ConfigureAwait(false);
                try
                {
                    return info.GetCountUnsafe();
                }
                finally
                {
                    info.ReleaseLock();
                }
            }
            return 0;
        }

        /// <summary>
        /// Gets the current connection count for an IP (synchronous version)
        /// </summary>
        public int GetConnectionCount(IPAddress ipAddress)
        {
            if (_connections.TryGetValue(ipAddress, out ConnectionInfo? info))
            {
                info.Lock();
                try
                {
                    return info.GetCountUnsafe();
                }
                finally
                {
                    info.ReleaseLock();
                }
            }
            return 0;
        }

        /// <summary>
        /// Releases a connection associated with the specified IP address and updates the connection count.
        /// </summary>
        /// <remarks>Connection removal is deferred and handled by a cleanup timer to avoid race
        /// conditions when multiple threads interact with connection state. Immediate removal is not performed to
        /// ensure thread safety and prevent premature deletion of connection information.</remarks>
        /// <param name="ipAddress">The IP address for which the connection is being released.</param>
        /// <param name="info">The connection information object that manages the state and count for the specified IP address.</param>
        internal async Task ReleaseConnectionAsync(IPAddress ipAddress, ConnectionInfo info)
        {
            try
            {
                await info.LockAsync().ConfigureAwait(false);
                try
                {
                    info.DecrementUnsafe();
                    int currentCount = info.GetCountUnsafe();

                    _logger.LogDebug("Connection released for {IPAddress}. Current count: {Count}",
                        ipAddress, currentCount);
                }
                finally
                {
                    info.ReleaseLock();
                }

                // The removal logic is handled in the cleanup timer to avoid race conditions
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error releasing connection for {IPAddress}", ipAddress);
            }
        }

        /// <summary>
        /// Releases a connection synchronously
        /// </summary>
        internal void ReleaseConnection(IPAddress ipAddress, ConnectionInfo info)
        {
            try
            {
                info.Lock();
                try
                {
                    info.DecrementUnsafe();
                    int currentCount = info.GetCountUnsafe();

                    _logger.LogDebug("Connection released for {IPAddress}. Current count: {Count}",
                        ipAddress, currentCount);
                }
                finally
                {
                    info.ReleaseLock();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error releasing connection for {IPAddress}", ipAddress);
            }
        }

        /// <summary>
        /// Removes expired connection entries from the internal collection.
        /// </summary>
        /// <remarks>This method is intended to be used as a callback for timer-based cleanup operations.
        /// It safely disposes of expired connections and logs any errors encountered during the cleanup process. The
        /// method is thread-safe and can be invoked concurrently.</remarks>
        /// <param name="state">An optional state object provided by the timer or scheduling mechanism. This parameter is not used.</param>
        private void CleanupExpired(object? state)
        {
            try
            {
                List<IPAddress> keysToRemove = new();

                // Identify candidates for removal
                foreach (KeyValuePair<IPAddress, ConnectionInfo> kvp in _connections)
                {
                    kvp.Value.Lock();
                    try
                    {
                        // Only remove if no active connections and not accessed recently
                        if (kvp.Value.CanRemoveUnsafe())
                        {
                            kvp.Value.MarkForRemovalUnsafe();
                            keysToRemove.Add(kvp.Key);
                        }
                    }
                    finally
                    {
                        kvp.Value.ReleaseLock();
                    }
                }

                // Remove marked entries
                foreach (IPAddress key in keysToRemove)
                {
                    if (_connections.TryRemove(key, out ConnectionInfo? removed))
                    {
                        // Only dispose if still marked for removal
                        removed.Lock();
                        try
                        {
                            if (removed.IsMarkedForRemovalUnsafe())
                            {
                                removed.Dispose();
                            }
                        }
                        finally
                        {
                            removed.ReleaseLock();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cleanup");
            }
        }

        /// <summary>
        /// Releases all resources used by the instance and disposes of managed connections and timers.
        /// </summary>
        /// <remarks>Call this method when the instance is no longer needed to free associated resources
        /// promptly. After calling <see cref="Dispose"/>, the instance should not be used. This method is safe to call
        /// multiple times; subsequent calls have no effect.</remarks>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _cleanupTimer?.Dispose();

            foreach (KeyValuePair<IPAddress, ConnectionInfo> kvp in _connections)
            {
                kvp.Value.Dispose();
            }
            _connections.Clear();
        }

        /// <summary>
        /// Connection handle that ensures proper cleanup
        /// </summary>
        internal sealed class ConnectionHandle(IPAddress ipAddress, ConnectionInfo info, ConnectionTracker tracker) : IDisposable
        {
            private int _disposed;

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) == 0)
                {
                    tracker.ReleaseConnection(ipAddress, info);
                }
            }
        }

        /// <summary>
        /// Per-IP connection tracking information with proper synchronization
        /// </summary>
        internal sealed class ConnectionInfo(int maxConnections) : IDisposable
        {
            private readonly SemaphoreSlim _syncLock = new(1, 1);
            private int _activeConnections = 0;
            private DateTime _lastAccess = DateTime.UtcNow;
            private bool _markedForRemoval = false;
            private bool _disposed = false;
            /// <summary>
            /// Acquires the async lock for exclusive access
            /// </summary>
            public Task LockAsync(CancellationToken cancellationToken = default)
            {
                return _syncLock.WaitAsync(cancellationToken);
            }

            /// <summary>
            /// Acquires the sync lock for exclusive access
            /// </summary>
            public void Lock()
            {
                _syncLock.Wait();
            }

            /// <summary>
            /// Releases the lock
            /// </summary>
            public void ReleaseLock()
            {
                _syncLock.Release();
            }

            /// <summary>
            /// Checks if a connection can be acquired (must be called under lock)
            /// </summary>
            public bool CanAcquire()
            {
                return !_disposed && !_markedForRemoval && _activeConnections < maxConnections;
            }

            /// <summary>
            /// Increments the connection count (must be called under lock)
            /// </summary>
            public void IncrementUnsafe()
            {
                _activeConnections++;
                _lastAccess = DateTime.UtcNow;
            }

            /// <summary>
            /// Decrements the connection count (must be called under lock)
            /// </summary>
            public void DecrementUnsafe()
            {
                if (_activeConnections > 0)
                {
                    _activeConnections--;
                    _lastAccess = DateTime.UtcNow;
                }
            }

            /// <summary>
            /// Gets the current connection count (must be called under lock)
            /// </summary>
            public int GetCountUnsafe()
            {
                return _activeConnections;
            }

            /// <summary>
            /// Checks if this can be removed (must be called under lock)
            /// </summary>
            public bool CanRemoveUnsafe()
            {
                // Remove if no active connections and not accessed for 5 minutes
                return _activeConnections == 0 &&
                       (DateTime.UtcNow - _lastAccess) > TimeSpan.FromMinutes(5);
            }

            /// <summary>
            /// Marks for removal (must be called under lock)
            /// </summary>
            public void MarkForRemovalUnsafe()
            {
                _markedForRemoval = true;
            }

            /// <summary>
            /// Checks if marked for removal (must be called under lock)
            /// </summary>
            public bool IsMarkedForRemovalUnsafe()
            {
                return _markedForRemoval;
            }

            /// <summary>
            /// Releases all resources used by the current instance.
            /// </summary>
            /// <remarks>Call this method when you are finished using the object to free unmanaged
            /// resources and perform other cleanup operations. After calling <see cref="Dispose"/>, the object should
            /// not be used further.</remarks>
            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _syncLock?.Dispose();
            }
        }
    }
}