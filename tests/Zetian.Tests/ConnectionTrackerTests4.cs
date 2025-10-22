using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Concurrent;
using System.Net;
using Xunit;
using Zetian.Internal;

namespace Zetian.Tests
{
    public class ConnectionTrackerTests4
    {
        private readonly ConnectionTracker _tracker;
        private readonly IPAddress _testIp = IPAddress.Parse("192.168.1.1");
        private readonly IPAddress _testIp2 = IPAddress.Parse("192.168.1.2");

        public ConnectionTrackerTests4()
        {
            _tracker = new ConnectionTracker(5, NullLogger.Instance);
        }

        [Fact]
        public async Task TryAcquireAsync_ShouldBeThreadSafe()
        {
            // Test concurrent acquisition from same IP
            List<Task<ConnectionTracker.ConnectionHandle?>> tasks = new();
            int successCount = 0;

            // Try to acquire 10 connections concurrently (limit is 5)
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var handle = await _tracker.TryAcquireAsync(_testIp);
                    if (handle != null)
                    {
                        Interlocked.Increment(ref successCount);
                        // Hold the connection for a bit
                        await Task.Delay(100);
                        handle.Dispose();
                    }
                    return handle;
                }));
            }

            await Task.WhenAll(tasks);

            // Only 5 should succeed due to limit
            Assert.Equal(5, successCount);
        }

        [Fact]
        public async Task ReleaseConnection_ShouldBeThreadSafe()
        {
            // Acquire all 5 connections
            List<ConnectionTracker.ConnectionHandle?> handles = new();
            for (int i = 0; i < 5; i++)
            {
                var handle = await _tracker.TryAcquireAsync(_testIp);
                Assert.NotNull(handle);
                handles.Add(handle);
            }

            // Verify we can't acquire more
            var extraHandle = await _tracker.TryAcquireAsync(_testIp);
            Assert.Null(extraHandle);

            // Release all connections concurrently
            var releaseTasks = handles.Select(h => Task.Run(() => h?.Dispose())).ToArray();
            await Task.WhenAll(releaseTasks);

            // Now we should be able to acquire again
            var newHandle = await _tracker.TryAcquireAsync(_testIp);
            Assert.NotNull(newHandle);
            newHandle?.Dispose();
        }

        [Fact]
        public async Task GetConnectionCount_ShouldReturnCorrectCount()
        {
            // Initial count should be 0
            int count = await _tracker.GetConnectionCountAsync(_testIp);
            Assert.Equal(0, count);

            // Acquire 3 connections
            List<ConnectionTracker.ConnectionHandle?> handles = new();
            for (int i = 0; i < 3; i++)
            {
                handles.Add(await _tracker.TryAcquireAsync(_testIp));
            }

            // Count should be 3
            count = await _tracker.GetConnectionCountAsync(_testIp);
            Assert.Equal(3, count);

            // Release one
            handles[0]?.Dispose();
            handles.RemoveAt(0);

            // Count should be 2
            count = await _tracker.GetConnectionCountAsync(_testIp);
            Assert.Equal(2, count);

            // Cleanup
            handles.ForEach(h => h?.Dispose());
        }

        [Fact]
        public async Task MultipleIPs_ShouldTrackIndependently()
        {
            // Acquire max connections for first IP
            for (int i = 0; i < 5; i++)
            {
                var handle = await _tracker.TryAcquireAsync(_testIp);
                Assert.NotNull(handle);
            }

            // Should fail for first IP
            var extraHandle = await _tracker.TryAcquireAsync(_testIp);
            Assert.Null(extraHandle);

            // But should work for second IP
            var handle2 = await _tracker.TryAcquireAsync(_testIp2);
            Assert.NotNull(handle2);
            handle2?.Dispose();
        }

        [Fact]
        public async Task ConcurrentAcquireRelease_ShouldMaintainConsistency()
        {
            CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
            ConcurrentBag<Exception> errors = new();
            int operations = 0;

            // Multiple threads continuously acquiring and releasing
            var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(async () =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var handle = await _tracker.TryAcquireAsync(_testIp, cts.Token);
                        if (handle != null)
                        {
                            Interlocked.Increment(ref operations);
                            await Task.Delay(Random.Shared.Next(1, 10), cts.Token);
                            handle.Dispose();
                        }
                        else
                        {
                            await Task.Delay(Random.Shared.Next(1, 5), cts.Token);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected
                    }
                    catch (Exception ex)
                    {
                        errors.Add(ex);
                    }
                }
            })).ToArray();

            await Task.WhenAll(tasks);

            // Should have no errors
            Assert.Empty(errors);

            // Should have performed many operations
            Assert.True(operations > 0);

            // Final count should be 0 or low (depending on timing)
            int finalCount = await _tracker.GetConnectionCountAsync(_testIp);
            Assert.InRange(finalCount, 0, 5);
        }

        [Fact]
        public void Dispose_ShouldBeIdempotent()
        {
            ConnectionTracker tracker = new(5, NullLogger.Instance);

            // Multiple dispose calls should not throw
            tracker.Dispose();
            tracker.Dispose();
            tracker.Dispose();
        }

        [Fact]
        public async Task ConnectionHandle_DisposeShouldBeIdempotent()
        {
            var handle = await _tracker.TryAcquireAsync(_testIp);
            Assert.NotNull(handle);

            // Multiple dispose calls should not throw or cause issues
            handle?.Dispose();
            handle?.Dispose();
            handle?.Dispose();

            // Count should still be correct (0)
            int count = await _tracker.GetConnectionCountAsync(_testIp);
            Assert.Equal(0, count);
        }
    }
}
