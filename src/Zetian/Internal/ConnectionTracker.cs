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

            // All operations on the connection info must be atomic
            bool acquired = await info.TryAcquireAsync(cancellationToken).ConfigureAwait(false);

            if (acquired)
            {
                _logger.LogDebug("Connection acquired for {IPAddress}. Current count: {Count}/{Max}",
                    ipAddress, info.CurrentCount, _maxConnectionsPerIp);

                return new ConnectionHandle(ipAddress, info, this);
            }

            _logger.LogWarning("Connection limit exceeded for {IPAddress}. Max: {Max}",
                ipAddress, _maxConnectionsPerIp);

            return null;
        }

        /// <summary>
        /// Gets the current connection count for an IP
        /// </summary>
        public int GetConnectionCount(IPAddress ipAddress)
        {
            if (_connections.TryGetValue(ipAddress, out ConnectionInfo? info))
            {
                return info.CurrentCount;
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
        private async Task ReleaseConnectionAsync(IPAddress ipAddress, ConnectionInfo info)
        {
            try
            {
                // Release connection atomically
                int currentCount = await info.ReleaseAsync().ConfigureAwait(false);

                _logger.LogDebug("Connection released for {IPAddress}. Current count: {Count}",
                    ipAddress, currentCount);

                // Try to remove if expired and no connections
                // The removal logic is handled in the cleanup timer to avoid race conditions
                // We don't remove immediately to prevent the scenario where:
                // 1. Thread A releases last connection
                // 2. Thread B tries to acquire and creates new ConnectionInfo
                // 3. Thread A removes the ConnectionInfo
                // Instead, we let the cleanup timer handle removal after inactivity
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
                    // Use TryMarkForRemoval to atomically check and mark
                    if (kvp.Value.TryMarkForRemoval())
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }

                // Remove marked entries
                foreach (IPAddress key in keysToRemove)
                {
                    // Only remove if it's still marked for removal
                    if (_connections.TryRemove(key, out ConnectionInfo? removed))
                    {
                        // The ConnectionInfo was already marked for removal,
                        // so it's safe to dispose
                        removed.Dispose();
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
                    tracker.ReleaseConnectionAsync(ipAddress, info).GetAwaiter().GetResult();
                }
            }
        }

        /// <summary>
        /// Per-IP connection tracking information with proper synchronization
        /// </summary>
        internal sealed class ConnectionInfo : IDisposable
        {
            private readonly SemaphoreSlim _syncLock = new(1, 1);
            private readonly int _maxConnections;
            private int _activeConnections = 0;
            private DateTime _lastAccess = DateTime.UtcNow;
            private bool _markedForRemoval = false;
            private bool _disposed = false;

            public ConnectionInfo(int maxConnections)
            {
                _maxConnections = maxConnections;
            }

            public int CurrentCount
            {
                get
                {
                    lock (_syncLock)
                    {
                        return _activeConnections;
                    }
                }
            }

            /// <summary>
            /// Atomically tries to acquire a connection
            /// </summary>
            public async Task<bool> TryAcquireAsync(CancellationToken cancellationToken)
            {
                // Use semaphore for async-safe locking
                await _syncLock.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    // Check if we can accept more connections
                    if (_disposed || _markedForRemoval || _activeConnections >= _maxConnections)
                    {
                        return false;
                    }

                    _activeConnections++;
                    _lastAccess = DateTime.UtcNow;
                    return true;
                }
                finally
                {
                    _syncLock.Release();
                }
            }

            /// <summary>
            /// Atomically releases a connection
            /// </summary>
            public async Task<int> ReleaseAsync()
            {
                await _syncLock.WaitAsync().ConfigureAwait(false);
                try
                {
                    if (_activeConnections > 0)
                    {
                        _activeConnections--;
                        _lastAccess = DateTime.UtcNow;
                    }
                    return _activeConnections;
                }
                finally
                {
                    _syncLock.Release();
                }
            }

            /// <summary>
            /// Synchronous release for backward compatibility
            /// </summary>
            public int Release()
            {
                return ReleaseAsync().GetAwaiter().GetResult();
            }

            /// <summary>
            /// Tries to mark this info for removal
            /// </summary>
            public async Task<bool> TryMarkForRemovalAsync()
            {
                await _syncLock.WaitAsync().ConfigureAwait(false);
                try
                {
                    // Only mark for removal if:
                    // 1. No active connections
                    // 2. Not accessed for 10 minutes
                    // 3. Not already marked
                    if (_activeConnections == 0 &&
                        !_markedForRemoval &&
                        (DateTime.UtcNow - _lastAccess) > TimeSpan.FromMinutes(10))
                    {
                        _markedForRemoval = true;
                        return true;
                    }
                    return false;
                }
                finally
                {
                    _syncLock.Release();
                }
            }

            /// <summary>
            /// Synchronous version for timer callback
            /// </summary>
            public bool TryMarkForRemoval()
            {
                return TryMarkForRemovalAsync().GetAwaiter().GetResult();
            }

            /// <summary>
            /// Releases all resources used by the current instance.
            /// </summary>
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