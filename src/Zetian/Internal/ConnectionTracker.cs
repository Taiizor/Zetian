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

            ConnectionInfo info = _connections.GetOrAdd(ipAddress, _ => new ConnectionInfo(_maxConnectionsPerIp));

            // Try to acquire the semaphore
            bool acquired = await info.Semaphore.WaitAsync(0, cancellationToken).ConfigureAwait(false);

            if (acquired)
            {
                info.UpdateLastAccess();
                int currentCount = info.IncrementCount();

                _logger.LogDebug("Connection acquired for {IPAddress}. Current count: {Count}/{Max}",
                    ipAddress, currentCount, _maxConnectionsPerIp);

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

        private void ReleaseConnection(IPAddress ipAddress, ConnectionInfo info)
        {
            try
            {
                int currentCount = info.DecrementCount();
                info.Semaphore.Release();

                _logger.LogDebug("Connection released for {IPAddress}. Current count: {Count}",
                    ipAddress, currentCount);

                // If no more connections, consider removing the entry
                if (currentCount == 0 && info.CanBeRemoved())
                {
                    if (_connections.TryRemove(ipAddress, out ConnectionInfo? removed))
                    {
                        removed.Dispose();
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
                foreach (KeyValuePair<IPAddress, ConnectionInfo> kvp in _connections)
                {
                    if (kvp.Value.CanBeRemoved() && kvp.Value.CurrentCount == 0)
                    {
                        if (_connections.TryRemove(kvp.Key, out ConnectionInfo? removed))
                        {
                            removed.Dispose();
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
        internal sealed class ConnectionHandle : IDisposable
        {
            private readonly IPAddress _ipAddress;
            private readonly ConnectionInfo _info;
            private readonly ConnectionTracker _tracker;
            private int _disposed;

            internal ConnectionHandle(IPAddress ipAddress, ConnectionInfo info, ConnectionTracker tracker)
            {
                _ipAddress = ipAddress;
                _info = info;
                _tracker = tracker;
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) == 0)
                {
                    _tracker.ReleaseConnection(_ipAddress, _info);
                }
            }
        }

        /// <summary>
        /// Per-IP connection tracking information
        /// </summary>
        internal sealed class ConnectionInfo : IDisposable
        {
            private DateTime _lastAccess;
            private readonly object _lock = new();

            public ConnectionInfo(int maxConnections)
            {
                CurrentCount = maxConnections;
                Semaphore = new SemaphoreSlim(maxConnections, maxConnections);
                _lastAccess = DateTime.UtcNow;
            }

            public SemaphoreSlim Semaphore { get; }

            public int CurrentCount =>
                    // SemaphoreSlim.CurrentCount shows available slots
                    // So active connections = max - available
                    field - Semaphore.CurrentCount;

            public int IncrementCount()
            {
                // Just return the current count after semaphore acquire
                // No need for separate tracking
                return CurrentCount;
            }

            public int DecrementCount()
            {
                // Just return the current count after semaphore release
                // No need for separate tracking
                return CurrentCount;
            }

            public void UpdateLastAccess()
            {
                lock (_lock)
                {
                    _lastAccess = DateTime.UtcNow;
                }
            }

            public bool CanBeRemoved()
            {
                lock (_lock)
                {
                    // Remove if not accessed for 10 minutes and no active connections
                    return CurrentCount == 0 &&
                           (DateTime.UtcNow - _lastAccess) > TimeSpan.FromMinutes(10);
                }
            }

            public void Dispose()
            {
                Semaphore?.Dispose();
            }
        }
    }
}