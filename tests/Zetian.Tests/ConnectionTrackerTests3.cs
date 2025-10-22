using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using Xunit;
using Zetian.Internal;

namespace Zetian.Tests
{
    /// <summary>
    /// Tests for ConnectionTracker to verify thread-safety and race condition prevention
    /// </summary>
    public class ConnectionTrackerTests3
    {
        private readonly ILogger _logger = NullLogger.Instance;

        [Fact]
        public async Task ConcurrentAcquireRelease_ShouldBeThreadSafe()
        {
            // Arrange
            const int maxConnectionsPerIp = 10;
            const int threadCount = 100;
            const int operationsPerThread = 50;

            using ConnectionTracker tracker = new(maxConnectionsPerIp, _logger);
            IPAddress ipAddress = IPAddress.Parse("192.168.1.1");
            List<Exception> errors = new();
            int successCount = 0;

            // Act
            Task[] tasks = new Task[threadCount];
            for (int i = 0; i < threadCount; i++)
            {
                tasks[i] = Task.Run(async () =>
                {
                    for (int j = 0; j < operationsPerThread; j++)
                    {
                        try
                        {
                            ConnectionTracker.ConnectionHandle? handle = await tracker.TryAcquireAsync(ipAddress);
                            if (handle != null)
                            {
                                Interlocked.Increment(ref successCount);

                                // Simulate some work
                                await Task.Delay(Random.Shared.Next(1, 10));

                                // Get current count (this should not cause race conditions)
                                int count = await tracker.GetConnectionCountAsync(ipAddress);
                                Assert.InRange(count, 0, maxConnectionsPerIp);

                                handle.Dispose();
                            }
                        }
                        catch (Exception ex)
                        {
                            lock (errors)
                            {
                                errors.Add(ex);
                            }
                        }
                    }
                });
            }

            await Task.WhenAll(tasks);

            // Assert
            Assert.Empty(errors);

            // After all operations, connection count should be zero
            int finalCount = await tracker.GetConnectionCountAsync(ipAddress);
            Assert.Equal(0, finalCount);
        }

        [Fact]
        public async Task MaxConnectionsPerIp_ShouldBeRespected()
        {
            // Arrange
            const int maxConnectionsPerIp = 5;
            using ConnectionTracker tracker = new(maxConnectionsPerIp, _logger);
            IPAddress ipAddress = IPAddress.Parse("10.0.0.1");
            List<ConnectionTracker.ConnectionHandle?> handles = new();

            // Act - Acquire up to the maximum
            for (int i = 0; i < maxConnectionsPerIp; i++)
            {
                ConnectionTracker.ConnectionHandle? handle = await tracker.TryAcquireAsync(ipAddress);
                Assert.NotNull(handle);
                handles.Add(handle);
            }

            // Try to acquire one more (should fail)
            ConnectionTracker.ConnectionHandle? extraHandle = await tracker.TryAcquireAsync(ipAddress);
            Assert.Null(extraHandle);

            // Release one connection
            handles[0]?.Dispose();
            handles.RemoveAt(0);

            // Now we should be able to acquire again
            ConnectionTracker.ConnectionHandle? newHandle = await tracker.TryAcquireAsync(ipAddress);
            Assert.NotNull(newHandle);
            handles.Add(newHandle);

            // Cleanup
            foreach (ConnectionTracker.ConnectionHandle? handle in handles)
            {
                handle?.Dispose();
            }

            // Final count should be zero
            int finalCount = await tracker.GetConnectionCountAsync(ipAddress);
            Assert.Equal(0, finalCount);
        }

        [Fact]
        public async Task MultipleIpAddresses_ShouldBeTrackedIndependently()
        {
            // Arrange
            const int maxConnectionsPerIp = 3;
            using ConnectionTracker tracker = new(maxConnectionsPerIp, _logger);

            IPAddress ip1 = IPAddress.Parse("192.168.1.1");
            IPAddress ip2 = IPAddress.Parse("192.168.1.2");
            IPAddress ip3 = IPAddress.Parse("192.168.1.3");

            // Act
            ConnectionTracker.ConnectionHandle? handle1_1 = await tracker.TryAcquireAsync(ip1);
            ConnectionTracker.ConnectionHandle? handle1_2 = await tracker.TryAcquireAsync(ip1);
            ConnectionTracker.ConnectionHandle? handle2_1 = await tracker.TryAcquireAsync(ip2);
            ConnectionTracker.ConnectionHandle? handle3_1 = await tracker.TryAcquireAsync(ip3);

            // Assert
            Assert.Equal(2, await tracker.GetConnectionCountAsync(ip1));
            Assert.Equal(1, await tracker.GetConnectionCountAsync(ip2));
            Assert.Equal(1, await tracker.GetConnectionCountAsync(ip3));

            // Cleanup
            handle1_1?.Dispose();
            handle1_2?.Dispose();
            handle2_1?.Dispose();
            handle3_1?.Dispose();

            // All counts should be zero
            Assert.Equal(0, await tracker.GetConnectionCountAsync(ip1));
            Assert.Equal(0, await tracker.GetConnectionCountAsync(ip2));
            Assert.Equal(0, await tracker.GetConnectionCountAsync(ip3));
        }

        [Fact]
        public async Task GetConnectionCount_ShouldBeThreadSafe()
        {
            // Arrange
            const int maxConnectionsPerIp = 50;
            using ConnectionTracker tracker = new(maxConnectionsPerIp, _logger);
            IPAddress ipAddress = IPAddress.Parse("172.16.0.1");
            const int readerThreads = 20;
            const int writerThreads = 10;
            List<Exception> errors = new();
            CancellationTokenSource cancellationTokenSource = new();

            // Act - Start writer threads
            Task[] writerTasks = new Task[writerThreads];
            for (int i = 0; i < writerThreads; i++)
            {
                writerTasks[i] = Task.Run(async () =>
                {
                    while (!cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        try
                        {
                            ConnectionTracker.ConnectionHandle? handle = await tracker.TryAcquireAsync(ipAddress, cancellationTokenSource.Token);
                            if (handle != null)
                            {
                                await Task.Delay(Random.Shared.Next(10, 50), cancellationTokenSource.Token);
                                handle.Dispose();
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            // Expected
                        }
                        catch (Exception ex)
                        {
                            lock (errors)
                            {
                                errors.Add(ex);
                            }
                        }
                    }
                });
            }

            // Start reader threads
            Task[] readerTasks = new Task[readerThreads];
            for (int i = 0; i < readerThreads; i++)
            {
                readerTasks[i] = Task.Run(async () =>
                {
                    while (!cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        try
                        {
                            int count = await tracker.GetConnectionCountAsync(ipAddress);
                            Assert.InRange(count, 0, maxConnectionsPerIp);
                            await Task.Delay(Random.Shared.Next(5, 20), cancellationTokenSource.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            // Expected
                        }
                        catch (Exception ex)
                        {
                            lock (errors)
                            {
                                errors.Add(ex);
                            }
                        }
                    }
                });
            }

            // Let it run for a while
            await Task.Delay(5000);
            cancellationTokenSource.Cancel();

            await Task.WhenAll(writerTasks.Concat(readerTasks));

            // Assert
            Assert.Empty(errors);
        }
    }
}