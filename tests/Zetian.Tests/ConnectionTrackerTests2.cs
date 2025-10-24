using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Concurrent;
using System.Net;
using Xunit;
using Zetian.Internal;

namespace Zetian.Tests
{
    public class ConnectionTrackerTests2
    {
        private readonly ILogger _logger = NullLogger.Instance;

        [Fact]
        public async Task TryAcquireAsync_ShouldEnforceMaxConnectionsPerIp()
        {
            // Arrange
            const int maxConnectionsPerIp = 3;
            ConnectionTracker tracker = new(maxConnectionsPerIp, _logger);
            IPAddress ipAddress = IPAddress.Parse("192.168.1.1");
            List<ConnectionTracker.ConnectionHandle?> handles = [];

            try
            {
                // Act - Acquire up to the limit
                for (int i = 0; i < maxConnectionsPerIp; i++)
                {
                    ConnectionTracker.ConnectionHandle? handle = await tracker.TryAcquireAsync(ipAddress);
                    Assert.NotNull(handle);
                    handles.Add(handle);
                }

                // Try to acquire one more (should fail)
                ConnectionTracker.ConnectionHandle? extraHandle = await tracker.TryAcquireAsync(ipAddress);

                // Assert
                Assert.Null(extraHandle);
                Assert.Equal(maxConnectionsPerIp, tracker.GetConnectionCount(ipAddress));
            }
            finally
            {
                // Cleanup
                foreach (ConnectionTracker.ConnectionHandle? handle in handles)
                {
                    handle?.Dispose();
                }
                tracker.Dispose();
            }
        }

        [Fact]
        public async Task TryAcquireAsync_ShouldBeThreadSafe_NoConcurrentOverLimit()
        {
            // Arrange
            const int maxConnectionsPerIp = 5;
            const int numThreads = 20;
            ConnectionTracker tracker = new(maxConnectionsPerIp, _logger);
            IPAddress ipAddress = IPAddress.Parse("10.0.0.1");
            ConcurrentBag<ConnectionTracker.ConnectionHandle> successfulHandles = [];
            Barrier barrier = new(numThreads);

            try
            {
                // Act - Multiple threads try to acquire connections simultaneously
                Task[] tasks = Enumerable.Range(0, numThreads).Select(_ => Task.Run(async () =>
                {
                    // Synchronize all threads to start at the same time
                    barrier.SignalAndWait();

                    ConnectionTracker.ConnectionHandle? handle = await tracker.TryAcquireAsync(ipAddress);
                    if (handle != null)
                    {
                        successfulHandles.Add(handle);
                        // Hold the connection briefly
                        await Task.Delay(100);
                    }
                })).ToArray();

                await Task.WhenAll(tasks);

                // Assert - Exactly maxConnectionsPerIp connections should succeed
                Assert.Equal(maxConnectionsPerIp, successfulHandles.Count);
                Assert.Equal(maxConnectionsPerIp, tracker.GetConnectionCount(ipAddress));
            }
            finally
            {
                // Cleanup
                foreach (ConnectionTracker.ConnectionHandle handle in successfulHandles)
                {
                    handle?.Dispose();
                }
                tracker.Dispose();
                barrier.Dispose();
            }
        }

        [Fact]
        public async Task ReleaseConnection_ShouldAllowNewConnectionsAfterRelease()
        {
            // Arrange
            const int maxConnectionsPerIp = 2;
            ConnectionTracker tracker = new(maxConnectionsPerIp, _logger);
            IPAddress ipAddress = IPAddress.Parse("172.16.0.1");

            try
            {
                // Act - Acquire max connections
                ConnectionTracker.ConnectionHandle? handle1 = await tracker.TryAcquireAsync(ipAddress);
                ConnectionTracker.ConnectionHandle? handle2 = await tracker.TryAcquireAsync(ipAddress);
                Assert.NotNull(handle1);
                Assert.NotNull(handle2);

                // Try to acquire another (should fail)
                ConnectionTracker.ConnectionHandle? handle3 = await tracker.TryAcquireAsync(ipAddress);
                Assert.Null(handle3);

                // Release one connection
                handle1!.Dispose();

                // Small delay to ensure dispose completes
                await Task.Delay(50);

                // Try to acquire again (should succeed)
                ConnectionTracker.ConnectionHandle? handle4 = await tracker.TryAcquireAsync(ipAddress);

                // Assert
                Assert.NotNull(handle4);
                Assert.Equal(2, tracker.GetConnectionCount(ipAddress));

                // Cleanup
                handle2?.Dispose();
                handle4?.Dispose();
            }
            finally
            {
                tracker.Dispose();
            }
        }

        [Fact]
        public async Task ConcurrentAcquireAndRelease_ShouldMaintainCorrectCount()
        {
            // Arrange
            const int maxConnectionsPerIp = 10;
            const int numOperations = 100;
            ConnectionTracker tracker = new(maxConnectionsPerIp, _logger);
            IPAddress ipAddress = IPAddress.Parse("203.0.113.0");
            Random random = new();

            try
            {
                // Act - Concurrent acquire and release operations
                Task[] tasks = Enumerable.Range(0, numOperations).Select(_ => Task.Run(async () =>
                {
                    ConnectionTracker.ConnectionHandle? handle = await tracker.TryAcquireAsync(ipAddress);
                    if (handle != null)
                    {
                        // Hold connection for random time
                        await Task.Delay(random.Next(10, 50));
                        handle.Dispose();
                    }
                })).ToArray();

                await Task.WhenAll(tasks);

                // Small delay to ensure all releases complete
                await Task.Delay(100);

                // Assert - All connections should be released
                Assert.Equal(0, tracker.GetConnectionCount(ipAddress));

                // Verify we can acquire max connections again
                List<ConnectionTracker.ConnectionHandle?> handles = [];
                for (int i = 0; i < maxConnectionsPerIp; i++)
                {
                    ConnectionTracker.ConnectionHandle? handle = await tracker.TryAcquireAsync(ipAddress);
                    Assert.NotNull(handle);
                    handles.Add(handle);
                }

                // Cleanup
                foreach (ConnectionTracker.ConnectionHandle? handle in handles)
                {
                    handle?.Dispose();
                }
            }
            finally
            {
                tracker.Dispose();
            }
        }

        [Fact]
        public async Task MultipleIpAddresses_ShouldTrackIndependently()
        {
            // Arrange
            const int maxConnectionsPerIp = 2;
            ConnectionTracker tracker = new(maxConnectionsPerIp, _logger);
            IPAddress ip1 = IPAddress.Parse("192.168.1.1");
            IPAddress ip2 = IPAddress.Parse("192.168.1.2");
            IPAddress ip3 = IPAddress.Parse("192.168.1.3");

            try
            {
                // Act - Acquire connections for different IPs
                List<ConnectionTracker.ConnectionHandle?> handles = [];

                // Fill up IP1
                for (int i = 0; i < maxConnectionsPerIp; i++)
                {
                    ConnectionTracker.ConnectionHandle? handle = await tracker.TryAcquireAsync(ip1);
                    Assert.NotNull(handle);
                    handles.Add(handle);
                }

                // IP1 should be full
                ConnectionTracker.ConnectionHandle? extraHandle1 = await tracker.TryAcquireAsync(ip1);
                Assert.Null(extraHandle1);

                // IP2 should still have capacity
                for (int i = 0; i < maxConnectionsPerIp; i++)
                {
                    ConnectionTracker.ConnectionHandle? handle = await tracker.TryAcquireAsync(ip2);
                    Assert.NotNull(handle);
                    handles.Add(handle);
                }

                // IP3 should still have capacity
                ConnectionTracker.ConnectionHandle? handle3 = await tracker.TryAcquireAsync(ip3);
                Assert.NotNull(handle3);
                handles.Add(handle3);

                // Assert
                Assert.Equal(maxConnectionsPerIp, tracker.GetConnectionCount(ip1));
                Assert.Equal(maxConnectionsPerIp, tracker.GetConnectionCount(ip2));
                Assert.Equal(1, tracker.GetConnectionCount(ip3));

                // Cleanup
                foreach (ConnectionTracker.ConnectionHandle? handle in handles)
                {
                    handle?.Dispose();
                }
            }
            finally
            {
                tracker.Dispose();
            }
        }

        [Fact]
        public async Task DisposedHandle_ShouldNotBeDisposedMultipleTimes()
        {
            // Arrange
            ConnectionTracker tracker = new(5, _logger);
            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");

            try
            {
                // Act
                ConnectionTracker.ConnectionHandle? handle = await tracker.TryAcquireAsync(ipAddress);
                Assert.NotNull(handle);

                int initialCount = tracker.GetConnectionCount(ipAddress);
                Assert.Equal(1, initialCount);

                // Dispose multiple times
                handle!.Dispose();
                await Task.Delay(50);

                int countAfterFirstDispose = tracker.GetConnectionCount(ipAddress);
                Assert.Equal(0, countAfterFirstDispose);

                // Dispose again (should be no-op)
                handle.Dispose();
                handle.Dispose();
                await Task.Delay(50);

                int countAfterMultipleDispose = tracker.GetConnectionCount(ipAddress);

                // Assert - Count should still be 0, not negative
                Assert.Equal(0, countAfterMultipleDispose);
            }
            finally
            {
                tracker.Dispose();
            }
        }
    }
}