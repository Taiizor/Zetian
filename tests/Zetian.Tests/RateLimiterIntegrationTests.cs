using FluentAssertions;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using Xunit;
using Zetian.Models;
using Zetian.RateLimiting;

namespace Zetian.Tests
{
    /// <summary>
    /// Integration tests for rate limiter with more complex scenarios
    /// </summary>
    public class RateLimiterIntegrationTests : IDisposable
    {
        private readonly List<InMemoryRateLimiter> _limiters = new();

        public void Dispose()
        {
            foreach (InMemoryRateLimiter limiter in _limiters)
            {
                limiter?.Dispose();
            }
        }

        private InMemoryRateLimiter CreateLimiter(RateLimitConfiguration config)
        {
            InMemoryRateLimiter limiter = new(config);
            _limiters.Add(limiter);
            return limiter;
        }

        [Fact]
        public async Task ConcurrentRequests_ShouldBeThreadSafe()
        {
            // Arrange
            RateLimitConfiguration config = RateLimitConfiguration.PerMinute(100);
            InMemoryRateLimiter limiter = CreateLimiter(config);
            string key = "concurrent-key";
            int threadCount = 10;
            int requestsPerThread = 20;
            ConcurrentBag<bool> results = new();

            // Act
            Task[] tasks = Enumerable.Range(0, threadCount)
                .Select(async _ =>
                {
                    for (int i = 0; i < requestsPerThread; i++)
                    {
                        bool allowed = await limiter.IsAllowedAsync(key);
                        if (allowed)
                        {
                            await limiter.RecordRequestAsync(key);
                            results.Add(true);
                        }
                    }
                }).ToArray();

            await Task.WhenAll(tasks);

            // Assert
            results.Count.Should().BeLessThanOrEqualTo(100);
            int remaining = await limiter.GetRemainingAsync(key);
            remaining.Should().BeGreaterThanOrEqualTo(0);
        }

        [Fact]
        public async Task BurstRequests_ShouldHandleCorrectly()
        {
            // Arrange
            RateLimitConfiguration config = new()
            {
                MaxRequests = 10,
                Window = TimeSpan.FromSeconds(1),
                UseSlidingWindow = false
            };
            InMemoryRateLimiter limiter = CreateLimiter(config);
            string key = "burst-key";

            // Act - Send burst of requests
            List<bool> results = new();
            for (int i = 0; i < 15; i++)
            {
                bool allowed = await limiter.IsAllowedAsync(key);
                if (allowed)
                {
                    await limiter.RecordRequestAsync(key);
                }
                results.Add(allowed);
            }

            // Assert
            // First 10 requests should be allowed
            results.Take(10).All(r => r).Should().BeTrue();
            // Rest should be blocked
            results.Skip(10).All(r => !r).Should().BeTrue();
        }

        [Fact]
        public async Task MultipleIPAddresses_ShouldTrackSeparately()
        {
            // Arrange
            RateLimitConfiguration config = RateLimitConfiguration.PerMinute(5);
            InMemoryRateLimiter limiter = CreateLimiter(config);
            IPAddress[] ips = new[]
            {
                IPAddress.Parse("192.168.1.1"),
                IPAddress.Parse("192.168.1.2"),
                IPAddress.Parse("10.0.0.1"),
                IPAddress.Parse("::1") // IPv6
            };

            // Act
            foreach (IPAddress? ip in ips)
            {
                for (int i = 0; i < 3; i++)
                {
                    await limiter.RecordRequestAsync(ip);
                }
            }

            // Assert
            foreach (IPAddress? ip in ips)
            {
                int remaining = await limiter.GetRemainingAsync(ip.ToString());
                remaining.Should().Be(2, $"IP {ip} should have 2 requests remaining");
            }
        }

        [Fact]
        public async Task RateLimiter_WithCleanup_ShouldRemoveOldEntries()
        {
            // Arrange
            RateLimitConfiguration config = new()
            {
                MaxRequests = 5,
                Window = TimeSpan.FromSeconds(2)
            };
            InMemoryRateLimiter limiter = CreateLimiter(config);
            string key = "cleanup-key";

            // Act
            await limiter.RecordRequestAsync(key);
            await Task.Delay(3000); // Wait for window to expire and cleanup to run

            int remaining = await limiter.GetRemainingAsync(key);

            // Assert
            remaining.Should().Be(5, "Old entries should be cleaned up");
        }

        [Fact]
        public async Task DistributedRateLimiter_Simulation()
        {
            // Simulate multiple instances sharing rate limit
            RateLimitConfiguration config = RateLimitConfiguration.PerMinute(30);
            InMemoryRateLimiter limiter1 = CreateLimiter(config);
            InMemoryRateLimiter limiter2 = CreateLimiter(config);
            InMemoryRateLimiter limiter3 = CreateLimiter(config);
            string key = "shared-key";

            // Act - Each limiter allows its own limit (simulating no sharing)
            for (int i = 0; i < 10; i++)
            {
                await limiter1.RecordRequestAsync(key);
                await limiter2.RecordRequestAsync(key);
                await limiter3.RecordRequestAsync(key);
            }

            // Assert - In a real distributed scenario, total should be limited to 30
            // But with separate in-memory limiters, each tracks independently
            (await limiter1.GetRemainingAsync(key)).Should().Be(20);
            (await limiter2.GetRemainingAsync(key)).Should().Be(20);
            (await limiter3.GetRemainingAsync(key)).Should().Be(20);
        }

        [Fact]
        public async Task PerformanceTest_HighVolume()
        {
            // Arrange
            RateLimitConfiguration config = RateLimitConfiguration.PerMinute(10000);
            InMemoryRateLimiter limiter = CreateLimiter(config);
            string keyPrefix = "perf-key-";
            int iterations = 1000;

            // Act
            Stopwatch stopwatch = Stopwatch.StartNew();
            Task[] tasks = Enumerable.Range(0, iterations)
                .Select(async i =>
                {
                    string key = $"{keyPrefix}{i % 100}"; // 100 different keys
                    bool allowed = await limiter.IsAllowedAsync(key);
                    if (allowed)
                    {
                        await limiter.RecordRequestAsync(key);
                    }
                }).ToArray();

            await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Assert
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000,
                "High volume operations should complete quickly");
        }

        [Theory]
        [InlineData(60, 1)] // 60 per minute
        [InlineData(3600, 60)] // 3600 per hour
        [InlineData(86400, 1440)] // 86400 per day
        public async Task DifferentTimeWindows_ShouldCalculateCorrectly(int maxRequests, int windowMinutes)
        {
            // Arrange
            RateLimitConfiguration config = new()
            {
                MaxRequests = maxRequests,
                Window = TimeSpan.FromMinutes(windowMinutes)
            };
            InMemoryRateLimiter limiter = CreateLimiter(config);
            string key = $"window-{windowMinutes}-key";

            // Act
            await limiter.RecordRequestAsync(key);
            int remaining = await limiter.GetRemainingAsync(key);

            // Assert
            remaining.Should().Be(maxRequests - 1);
        }

        [Fact]
        public async Task RateLimiter_WithDifferentKeyFormats_ShouldWork()
        {
            // Arrange
            RateLimitConfiguration config = RateLimitConfiguration.PerHour(100);
            InMemoryRateLimiter limiter = CreateLimiter(config);
            string[] keys = new[]
            {
                "user:123",
                "api-key:abc-def-ghi",
                "session_xyz_123",
                "client.app.mobile",
                "192.168.1.1:8080"
            };

            // Act & Assert
            foreach (string? key in keys)
            {
                bool allowed = await limiter.IsAllowedAsync(key);
                allowed.Should().BeTrue($"Key '{key}' should be allowed");

                await limiter.RecordRequestAsync(key);
                int remaining = await limiter.GetRemainingAsync(key);
                remaining.Should().Be(99, $"Key '{key}' should have 99 requests remaining");
            }
        }

        [Fact]
        public async Task Reset_ShouldClearSpecificKey()
        {
            // Arrange
            RateLimitConfiguration config = RateLimitConfiguration.PerMinute(10);
            InMemoryRateLimiter limiter = CreateLimiter(config);
            string[] keys = new[] { "key1", "key2", "key3" };

            // Record requests for all keys
            foreach (string? key in keys)
            {
                for (int i = 0; i < 5; i++)
                {
                    await limiter.RecordRequestAsync(key);
                }
            }

            // Act - Reset only key2
            await limiter.ResetAsync("key2");

            // Assert
            (await limiter.GetRemainingAsync("key1")).Should().Be(5);
            (await limiter.GetRemainingAsync("key2")).Should().Be(10);
            (await limiter.GetRemainingAsync("key3")).Should().Be(5);
        }

        [Fact]
        public async Task TrackRequests_ShouldProvideAccurateCount()
        {
            // Arrange
            RateLimitConfiguration config = RateLimitConfiguration.PerMinute(50);
            InMemoryRateLimiter limiter = CreateLimiter(config);
            string key = "track-key";

            // Act
            await limiter.RecordRequestAsync(key);
            await limiter.RecordRequestAsync(key);
            await limiter.RecordRequestAsync(key);

            int remaining = await limiter.GetRemainingAsync(key);

            // Assert
            remaining.Should().Be(47);
        }

        [Fact]
        public async Task ExceedLimit_ShouldBlockRequests()
        {
            // Arrange
            RateLimitConfiguration config = RateLimitConfiguration.PerMinute(2);
            InMemoryRateLimiter limiter = CreateLimiter(config);
            string key = "limit-key";

            // Fill the limit
            await limiter.RecordRequestAsync(key);
            await limiter.RecordRequestAsync(key);

            // Act
            bool isAllowed = await limiter.IsAllowedAsync(key);
            int remaining = await limiter.GetRemainingAsync(key);

            // Assert
            isAllowed.Should().BeFalse();
            remaining.Should().Be(0);
        }
    }
}
