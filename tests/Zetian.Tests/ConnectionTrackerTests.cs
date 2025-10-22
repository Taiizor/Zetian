using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Concurrent;
using System.Net;
using Xunit;
using Zetian.Internal;

namespace Zetian.Tests
{
    /// <summary>
    /// Tests for ConnectionTracker to ensure thread-safety and proper connection limiting
    /// </summary>
    public class ConnectionTrackerTests : IDisposable
    {
        private readonly ConnectionTracker _tracker;
        private readonly IPAddress _testIp = IPAddress.Parse("127.0.0.1");
        private readonly IPAddress _testIp2 = IPAddress.Parse("192.168.1.1");

        public ConnectionTrackerTests()
        {
            _tracker = new ConnectionTracker(5, NullLogger.Instance);
        }

        public void Dispose()
        {
            _tracker?.Dispose();
        }

        [Fact]
        public async Task TryAcquireAsync_ShouldAllowUpToMaxConnections()
        {
            // Arrange
            const int maxConnections = 5;
            List<ConnectionTracker.ConnectionHandle> handles = new();

            // Act - Acquire max connections
            for (int i = 0; i < maxConnections; i++)
            {
                ConnectionTracker.ConnectionHandle? handle = await _tracker.TryAcquireAsync(_testIp);
                Assert.NotNull(handle);
                handles.Add(handle);
            }

            // Assert - Next connection should fail
            ConnectionTracker.ConnectionHandle? extraHandle = await _tracker.TryAcquireAsync(_testIp);
            Assert.Null(extraHandle);

            // Cleanup
            foreach (ConnectionTracker.ConnectionHandle handle in handles)
            {
                handle.Dispose();
            }
        }

        [Fact]
        public async Task TryAcquireAsync_ShouldReleaseConnectionOnDispose()
        {
            // Arrange & Act
            ConnectionTracker.ConnectionHandle? handle1 = await _tracker.TryAcquireAsync(_testIp);
            Assert.NotNull(handle1);

            // Dispose the first connection
            handle1.Dispose();

            // Should be able to acquire again
            ConnectionTracker.ConnectionHandle? handle2 = await _tracker.TryAcquireAsync(_testIp);
            Assert.NotNull(handle2);

            // Cleanup
            handle2.Dispose();
        }

        [Fact]
        public async Task TryAcquireAsync_ShouldHandleMultipleIPs()
        {
            // Arrange
            const int maxConnections = 5;
            List<ConnectionTracker.ConnectionHandle> handles1 = new();
            List<ConnectionTracker.ConnectionHandle> handles2 = new();

            // Act - Fill up connections for first IP
            for (int i = 0; i < maxConnections; i++)
            {
                ConnectionTracker.ConnectionHandle? handle = await _tracker.TryAcquireAsync(_testIp);
                Assert.NotNull(handle);
                handles1.Add(handle);
            }

            // Second IP should still have available slots
            for (int i = 0; i < maxConnections; i++)
            {
                ConnectionTracker.ConnectionHandle? handle = await _tracker.TryAcquireAsync(_testIp2);
                Assert.NotNull(handle);
                handles2.Add(handle);
            }

            // Both IPs should be at limit
            Assert.Null(await _tracker.TryAcquireAsync(_testIp));
            Assert.Null(await _tracker.TryAcquireAsync(_testIp2));

            // Cleanup
            handles1.ForEach(h => h.Dispose());
            handles2.ForEach(h => h.Dispose());
        }

        [Fact]
        public async Task TryAcquireAsync_ShouldBeThreadSafe()
        {
            // Arrange
            const int maxConnections = 5;
            const int totalAttempts = 100;
            int successCount = 0;
            ConcurrentBag<ConnectionTracker.ConnectionHandle> handles = new();
            Barrier barrier = new(totalAttempts);

            // Act - Many concurrent attempts
            Task[] tasks = Enumerable.Range(0, totalAttempts).Select(_ => Task.Run(async () =>
            {
                barrier.SignalAndWait(); // Synchronize all tasks to start at the same time

                ConnectionTracker.ConnectionHandle? handle = await _tracker.TryAcquireAsync(_testIp);
                if (handle != null)
                {
                    Interlocked.Increment(ref successCount);
                    handles.Add(handle);
                    await Task.Delay(100); // Hold the connection briefly
                }
            })).ToArray();

            await Task.WhenAll(tasks);

            // Assert - Exactly max connections should succeed
            Assert.Equal(maxConnections, successCount);
            Assert.Equal(maxConnections, handles.Count);

            // Cleanup
            foreach (ConnectionTracker.ConnectionHandle handle in handles)
            {
                handle.Dispose();
            }
        }

        [Fact]
        public async Task ConnectionHandle_ShouldPreventDoubleDispose()
        {
            // Arrange
            ConnectionTracker.ConnectionHandle? handle = await _tracker.TryAcquireAsync(_testIp);
            Assert.NotNull(handle);

            // Act - Dispose twice
            handle.Dispose();
            handle.Dispose(); // Should not throw

            // Assert - Should be able to acquire a new connection
            ConnectionTracker.ConnectionHandle? newHandle = await _tracker.TryAcquireAsync(_testIp);
            Assert.NotNull(newHandle);

            // Cleanup
            newHandle.Dispose();
        }

        [Fact]
        public void GetConnectionCount_ShouldReturnCorrectCount()
        {
            // Arrange
            List<ConnectionTracker.ConnectionHandle> handles = new();

            // Initially should be 0
            Assert.Equal(0, _tracker.GetConnectionCount(_testIp));

            // Acquire some connections
            for (int i = 1; i <= 3; i++)
            {
                ConnectionTracker.ConnectionHandle? handle = _tracker.TryAcquireAsync(_testIp).Result;
                Assert.NotNull(handle);
                handles.Add(handle);

                // Count should increase
                Assert.Equal(i, _tracker.GetConnectionCount(_testIp));
            }

            // Release one connection
            handles[0].Dispose();
            handles.RemoveAt(0);

            // Count should decrease
            Assert.Equal(2, _tracker.GetConnectionCount(_testIp));

            // Cleanup
            handles.ForEach(h => h.Dispose());
        }

        [Fact]
        public async Task TryAcquireAsync_ShouldRespectCancellationToken()
        {
            // Arrange
            using CancellationTokenSource cts = new();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await _tracker.TryAcquireAsync(_testIp, cts.Token);
            });
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(10)]
        public async Task TryAcquireAsync_ShouldRespectDifferentMaxLimits(int maxConnections)
        {
            // Arrange
            using ConnectionTracker customTracker = new(maxConnections, NullLogger.Instance);
            List<ConnectionTracker.ConnectionHandle> handles = new();

            // Act - Acquire max connections
            for (int i = 0; i < maxConnections; i++)
            {
                ConnectionTracker.ConnectionHandle? handle = await customTracker.TryAcquireAsync(_testIp);
                Assert.NotNull(handle);
                handles.Add(handle);
            }

            // Assert - Next connection should fail
            ConnectionTracker.ConnectionHandle? extraHandle = await customTracker.TryAcquireAsync(_testIp);
            Assert.Null(extraHandle);

            // Cleanup
            handles.ForEach(h => h.Dispose());
        }
    }
}