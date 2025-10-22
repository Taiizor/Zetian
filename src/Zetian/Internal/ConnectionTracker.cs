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
            if (ipAddress == null)
            {
                throw new ArgumentNullException(nameof(ipAddress));
            }

            // Get or create connection info atomically
            ConnectionInfo info = _connections.GetOrAdd(ipAddress, _ => new ConnectionInfo(_maxConnectionsPerIp));

            // Try to acquire the semaphore (this is the actual limit enforcement)
            bool acquired = await info.Semaphore.WaitAsync(0, cancellationToken).ConfigureAwait(false);

            if (acquired)
            {
                try
                {
                    // Only increment count after successfully acquiring semaphore
                    int currentCount = info.IncrementCount();

                    _logger.LogDebug("Connection acquired for {IPAddress}. Current count: {Count}/{Max}",
                        ipAddress, currentCount, _maxConnectionsPerIp);

                    return new ConnectionHandle(ipAddress, info, this);
                }
                catch
                {
                    // If something goes wrong, release the semaphore
                    info.Semaphore.Release();
                    throw;
                }
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

        private void ReleaseConnection(IPAddress ipAddress, ConnectionInfo info)
        {
            try
            {
                // First decrement the count
                int currentCount = info.DecrementCount();

                // Then release the semaphore
                info.Semaphore.Release();

                _logger.LogDebug("Connection released for {IPAddress}. Current count: {Count}",
                    ipAddress, currentCount);

                // Check if we can remove this entry (thread-safe check)
                if (currentCount == 0)
                {
                    // Double-check with proper locking
                    if (info.CanBeRemoved())
                    {
                        // Try to remove - but another thread might have added a connection
                        if (_connections.TryRemove(ipAddress, out ConnectionInfo? removed))
                        {
                            // Final check - make sure it's still removable
                            if (removed.CurrentCount == 0)
                            {
                                removed.Dispose();
                            }
                            else
                            {
                                // Race condition: connection was added, put it back
                                _connections.TryAdd(ipAddress, removed);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error releasing connection for {IPAddress}", ipAddress);
            }
        }

        private void CleanupExpired(object? state)
        {
            try
            {
                List<IPAddress> keysToRemove = new();

                // First pass: identify candidates for removal
                foreach (KeyValuePair<IPAddress, ConnectionInfo> kvp in _connections)
                {
                    if (kvp.Value.CanBeRemoved())
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }

                // Second pass: try to remove (might fail due to concurrent operations)
                foreach (IPAddress key in keysToRemove)
                {
                    if (_connections.TryRemove(key, out ConnectionInfo? removed))
                    {
                        // Final check before disposal
                        if (removed.CurrentCount == 0)
                        {
                            removed.Dispose();
                        }
                        else
                        {
                            // Race condition: connection was added, put it back
                            _connections.TryAdd(key, removed);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cleanup");
            }
        }

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
        /// Per-IP connection tracking information
        /// </summary>
        internal sealed class ConnectionInfo(int maxConnections) : IDisposable
        {
            private int _activeConnections = 0;
            private DateTime _lastAccess = DateTime.UtcNow;
            private readonly ReaderWriterLockSlim _lock = new();

            public SemaphoreSlim Semaphore { get; } = new SemaphoreSlim(maxConnections, maxConnections);

            public int CurrentCount
            {
                get
                {
                    _lock.EnterReadLock();
                    try
                    {
                        return _activeConnections;
                    }
                    finally
                    {
                        _lock.ExitReadLock();
                    }
                }
            }

            public int IncrementCount()
            {
                _lock.EnterWriteLock();
                try
                {
                    _activeConnections++;
                    _lastAccess = DateTime.UtcNow;
                    return _activeConnections;
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }

            public int DecrementCount()
            {
                _lock.EnterWriteLock();
                try
                {
                    _activeConnections--;
                    _lastAccess = DateTime.UtcNow;
                    return _activeConnections;
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }

            public void UpdateLastAccess()
            {
                _lock.EnterWriteLock();
                try
                {
                    _lastAccess = DateTime.UtcNow;
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }

            public bool CanBeRemoved()
            {
                _lock.EnterReadLock();
                try
                {
                    // Remove if not accessed for 10 minutes and no active connections
                    return _activeConnections == 0 &&
                           (DateTime.UtcNow - _lastAccess) > TimeSpan.FromMinutes(10);
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }

            public void Dispose()
            {
                Semaphore?.Dispose();
                _lock?.Dispose();
            }
        }
    }
}