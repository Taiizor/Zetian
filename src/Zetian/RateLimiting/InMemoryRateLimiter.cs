using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Zetian.Abstractions;

namespace Zetian.Extensions.RateLimiting
{
    /// <summary>
    /// In-memory rate limiter implementation
    /// </summary>
    public class InMemoryRateLimiter : IRateLimiter, IDisposable
    {
        private readonly RateLimitConfiguration _configuration;
        private readonly ConcurrentDictionary<string, RequestWindow> _windows;
        private readonly Timer _cleanupTimer;
        private bool _disposed;

        public InMemoryRateLimiter(RateLimitConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _windows = new ConcurrentDictionary<string, RequestWindow>();

            // Cleanup old entries every minute
            _cleanupTimer = new Timer(_ => CleanupExpiredWindows(), null,
                TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        public Task<bool> IsAllowedAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be empty", nameof(key));
            }

            RequestWindow window = _windows.GetOrAdd(key, _ => new RequestWindow(_configuration));
            return Task.FromResult(window.IsAllowed());
        }

        public Task<bool> IsAllowedAsync(IPAddress ipAddress)
        {
            ArgumentNullException.ThrowIfNull(ipAddress);

            return IsAllowedAsync(ipAddress.ToString());
        }

        public Task RecordRequestAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be empty", nameof(key));
            }

            RequestWindow window = _windows.GetOrAdd(key, _ => new RequestWindow(_configuration));
            window.RecordRequest();
            return Task.CompletedTask;
        }

        public Task RecordRequestAsync(IPAddress ipAddress)
        {
            ArgumentNullException.ThrowIfNull(ipAddress);

            return RecordRequestAsync(ipAddress.ToString());
        }

        public Task ResetAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be empty", nameof(key));
            }

            _windows.TryRemove(key, out _);
            return Task.CompletedTask;
        }

        public Task<int> GetRemainingAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be empty", nameof(key));
            }

            RequestWindow window = _windows.GetOrAdd(key, _ => new RequestWindow(_configuration));
            return Task.FromResult(window.GetRemaining());
        }

        private void CleanupExpiredWindows()
        {
            List<string> expiredKeys = _windows
                .Where(kvp => kvp.Value.IsExpired())
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (string? key in expiredKeys)
            {
                _windows.TryRemove(key, out _);
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
            _windows.Clear();
        }

        private class RequestWindow(RateLimitConfiguration configuration)
        {
            private readonly object _lock = new();
            private readonly Queue<DateTime> _requests = new();
            private DateTime _windowStart = DateTime.UtcNow;
            private int _requestCount = 0;

            public bool IsAllowed()
            {
                lock (_lock)
                {
                    CleanupOldRequests();

                    if (configuration.UseSlidingWindow)
                    {
                        return _requests.Count < configuration.MaxRequests;
                    }
                    else
                    {
                        // Fixed window
                        DateTime now = DateTime.UtcNow;
                        if (now - _windowStart > configuration.Window)
                        {
                            // New window
                            _windowStart = now;
                            _requestCount = 0;
                        }

                        return _requestCount < configuration.MaxRequests;
                    }
                }
            }

            public void RecordRequest()
            {
                lock (_lock)
                {
                    DateTime now = DateTime.UtcNow;

                    if (configuration.UseSlidingWindow)
                    {
                        CleanupOldRequests();
                        _requests.Enqueue(now);
                    }
                    else
                    {
                        // Fixed window
                        if (now - _windowStart > configuration.Window)
                        {
                            // New window
                            _windowStart = now;
                            _requestCount = 1;
                        }
                        else
                        {
                            _requestCount++;
                        }
                    }
                }
            }

            public int GetRemaining()
            {
                lock (_lock)
                {
                    CleanupOldRequests();

                    if (configuration.UseSlidingWindow)
                    {
                        return Math.Max(0, configuration.MaxRequests - _requests.Count);
                    }
                    else
                    {
                        DateTime now = DateTime.UtcNow;
                        if (now - _windowStart > configuration.Window)
                        {
                            return configuration.MaxRequests;
                        }

                        return Math.Max(0, configuration.MaxRequests - _requestCount);
                    }
                }
            }

            public bool IsExpired()
            {
                lock (_lock)
                {
                    DateTime now = DateTime.UtcNow;

                    if (configuration.UseSlidingWindow)
                    {
                        CleanupOldRequests();
                        return _requests.Count == 0 &&
                               (_requests.Count == 0 || now - _windowStart > configuration.Window * 2);
                    }
                    else
                    {
                        return now - _windowStart > configuration.Window * 2;
                    }
                }
            }

            private void CleanupOldRequests()
            {
                if (!configuration.UseSlidingWindow)
                {
                    return;
                }

                DateTime cutoff = DateTime.UtcNow - configuration.Window;

                while (_requests.Count > 0 && _requests.Peek() < cutoff)
                {
                    _requests.Dequeue();
                }
            }
        }
    }
}