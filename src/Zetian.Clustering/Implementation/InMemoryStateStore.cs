using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Zetian.Clustering.Abstractions;

namespace Zetian.Clustering.Implementation
{
    /// <summary>
    /// In-memory implementation of state store
    /// </summary>
    public class InMemoryStateStore : IStateStore
    {
        private readonly ConcurrentDictionary<string, StateEntry> _store;
        private readonly ConcurrentDictionary<string, DistributedLock> _locks;
        private readonly Timer _cleanupTimer;
        private bool _disposed;

        public InMemoryStateStore()
        {
            _store = new ConcurrentDictionary<string, StateEntry>();
            _locks = new ConcurrentDictionary<string, DistributedLock>();

            // Cleanup expired entries every minute
            _cleanupTimer = new Timer(CleanupExpired, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        public Task<byte[]?> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            if (_store.TryGetValue(key, out StateEntry? entry))
            {
                if (entry.IsExpired())
                {
                    _store.TryRemove(key, out _);
                    return Task.FromResult<byte[]?>(null);
                }

                return Task.FromResult<byte[]?>(entry.Value);
            }

            return Task.FromResult<byte[]?>(null);
        }

        public Task<bool> SetAsync(string key, byte[] value, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
        {
            StateEntry entry = new()
            {
                Value = value,
                ExpiresAt = ttl.HasValue ? DateTime.UtcNow.Add(ttl.Value) : null
            };

            _store.AddOrUpdate(key, entry, (k, v) => entry);
            return Task.FromResult(true);
        }

        public Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_store.TryRemove(key, out _));
        }

        public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            if (_store.TryGetValue(key, out StateEntry? entry))
            {
                if (entry.IsExpired())
                {
                    _store.TryRemove(key, out _);
                    return Task.FromResult(false);
                }

                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public Task<IDictionary<string, byte[]>> GetMultipleAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
        {
            Dictionary<string, byte[]> result = [];

            foreach (string key in keys)
            {
                if (_store.TryGetValue(key, out StateEntry? entry) && !entry.IsExpired())
                {
                    result[key] = entry.Value;
                }
            }

            return Task.FromResult<IDictionary<string, byte[]>>(result);
        }

        public Task<bool> SetMultipleAsync(IDictionary<string, byte[]> values, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
        {
            DateTime? expiresAt = ttl.HasValue ? DateTime.UtcNow.Add(ttl.Value) : (DateTime?)null;

            foreach (KeyValuePair<string, byte[]> kvp in values)
            {
                StateEntry entry = new()
                {
                    Value = kvp.Value,
                    ExpiresAt = expiresAt
                };

                _store.AddOrUpdate(kvp.Key, entry, (k, v) => entry);
            }

            return Task.FromResult(true);
        }

        public Task<bool> CompareAndSwapAsync(string key, byte[] expectedValue, byte[] newValue, CancellationToken cancellationToken = default)
        {
            if (_store.TryGetValue(key, out StateEntry? entry))
            {
                if (entry.IsExpired())
                {
                    _store.TryRemove(key, out _);
                    return Task.FromResult(false);
                }

                if (ByteArrayEquals(entry.Value, expectedValue))
                {
                    StateEntry newEntry = new()
                    {
                        Value = newValue,
                        ExpiresAt = entry.ExpiresAt
                    };

                    return Task.FromResult(_store.TryUpdate(key, newEntry, entry));
                }
            }

            return Task.FromResult(false);
        }

        public Task<long> IncrementAsync(string key, long delta = 1, CancellationToken cancellationToken = default)
        {
            StateEntry entry = _store.AddOrUpdate(key,
                k => new StateEntry { Value = BitConverter.GetBytes(delta) },
                (k, v) =>
                {
                    long currentValue = v.Value.Length >= 8 ? BitConverter.ToInt64(v.Value, 0) : 0;
                    return new StateEntry
                    {
                        Value = BitConverter.GetBytes(currentValue + delta),
                        ExpiresAt = v.ExpiresAt
                    };
                });

            return Task.FromResult(BitConverter.ToInt64(entry.Value, 0));
        }

        public Task<IEnumerable<string>> GetKeysAsync(string pattern = "*", CancellationToken cancellationToken = default)
        {
            IEnumerable<string> keys = _store.Keys.AsEnumerable();

            if (pattern != "*")
            {
                // Simple pattern matching (supports * wildcard)
                Regex regex = new(
                    "^" + System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*") + "$");
                keys = keys.Where(k => regex.IsMatch(k));
            }

            return Task.FromResult(keys);
        }

        public async Task<IDistributedLock?> AcquireLockAsync(string resource, TimeSpan ttl, CancellationToken cancellationToken = default)
        {
            string lockId = Guid.NewGuid().ToString("N");
            DistributedLock @lock = new(this, resource, lockId, ttl);

            // Try to acquire lock
            if (_locks.TryAdd(resource, @lock))
            {
                // Schedule automatic release
                _ = Task.Delay(ttl, cancellationToken).ContinueWith(_ => ReleaseLockInternal(resource, lockId), cancellationToken);
                return @lock;
            }

            // Check if existing lock is expired
            if (_locks.TryGetValue(resource, out DistributedLock? existingLock) && existingLock.IsExpired())
            {
                if (_locks.TryUpdate(resource, @lock, existingLock))
                {
                    _ = Task.Delay(ttl, cancellationToken).ContinueWith(_ => ReleaseLockInternal(resource, lockId), cancellationToken);
                    return @lock;
                }
            }

            return null;
        }

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            _store.Clear();
            _locks.Clear();
            return Task.CompletedTask;
        }

        public Task<long> GetSizeAsync(CancellationToken cancellationToken = default)
        {
            long size = 0;
            foreach (StateEntry entry in _store.Values)
            {
                size += entry.Value?.Length ?? 0;
            }
            return Task.FromResult(size);
        }

        private void CleanupExpired(object? state)
        {
            // Remove expired entries
            List<string> expiredKeys = _store
                .Where(kvp => kvp.Value.IsExpired())
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (string key in expiredKeys)
            {
                _store.TryRemove(key, out _);
            }

            // Remove expired locks
            List<string> expiredLocks = _locks
                .Where(kvp => kvp.Value.IsExpired())
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (string resource in expiredLocks)
            {
                _locks.TryRemove(resource, out _);
            }
        }

        private void ReleaseLockInternal(string resource, string lockId)
        {
            if (_locks.TryGetValue(resource, out DistributedLock? @lock) && @lock.LockId == lockId)
            {
                _locks.TryRemove(resource, out _);
            }
        }

        private static bool ByteArrayEquals(byte[]? a, byte[]? b)
        {
            if (a == null && b == null)
            {
                return true;
            }

            if (a == null || b == null)
            {
                return false;
            }

            if (a.Length != b.Length)
            {
                return false;
            }

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }

            return true;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _cleanupTimer?.Dispose();
            _store.Clear();
            _locks.Clear();
        }

        private class StateEntry
        {
            public byte[] Value { get; set; } = [];
            public DateTime? ExpiresAt { get; set; }

            public bool IsExpired()
            {
                return ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;
            }
        }

        private class DistributedLock(InMemoryStateStore store, string resource, string lockId, TimeSpan ttl) : IDistributedLock
        {
            private readonly DateTime _expiresAt = DateTime.UtcNow.Add(ttl);
            private bool _isReleased;

            public string LockId { get; } = lockId;
            public string Resource { get; } = resource;
            public bool IsHeld => !_isReleased && !IsExpired();

            public bool IsExpired()
            {
                return DateTime.UtcNow > _expiresAt;
            }

            public Task<bool> ExtendAsync(TimeSpan ttl, CancellationToken cancellationToken = default)
            {
                if (_isReleased || IsExpired())
                {
                    return Task.FromResult(false);
                }

                // In-memory implementation doesn't support extension
                // Would need to track expiration separately
                return Task.FromResult(false);
            }

            public Task ReleaseAsync(CancellationToken cancellationToken = default)
            {
                if (!_isReleased)
                {
                    _isReleased = true;
                    store.ReleaseLockInternal(Resource, LockId);
                }

                return Task.CompletedTask;
            }

            public async ValueTask DisposeAsync()
            {
                await ReleaseAsync();
            }
        }
    }
}