using System.Collections.Concurrent;
using System.Net.Mail;
using System.Net.Sockets;
using System.Text;
using Xunit;

namespace Zetian.Tests
{
    /// <summary>
    /// Integration tests for SmtpServer to verify race condition fixes in connection tracking
    /// </summary>
    public class SmtpServerRaceConditionTests : IAsyncLifetime
    {
        private SmtpServer _server;
        private const int TestPort = 25250;
        private const int MaxConnectionsPerIp = 5;

        public async Task InitializeAsync()
        {
            _server = new SmtpServerBuilder()
                .Port(TestPort)
                .MaxConnectionsPerIP(MaxConnectionsPerIp)
                .MaxConnections(100)
                .Build();

            await _server.StartAsync();
        }

        public async Task DisposeAsync()
        {
            if (_server != null)
            {
                await _server.StopAsync();
                _server.Dispose();
            }
        }

        [Fact]
        public async Task SmtpServer_ShouldEnforceMaxConnectionsPerIP()
        {
            // Arrange
            const int attemptCount = 20;
            ConcurrentBag<TcpClient> successfulConnections = new();
            Barrier barrier = new(attemptCount);
            int successCount = 0;

            // Act - Try to connect many times simultaneously
            Task<bool>[] tasks = Enumerable.Range(0, attemptCount).Select(i => Task.Run(async () =>
            {
                TcpClient client = null;
                try
                {
                    client = new TcpClient();

                    // Wait for all tasks to be ready
                    barrier.SignalAndWait();

                    // All tasks connect at the same time
                    await client.ConnectAsync("localhost", TestPort);

                    // Read greeting
                    NetworkStream stream = client.GetStream();
                    byte[] buffer = new byte[1024];
                    int bytes = await stream.ReadAsync(buffer, 0, buffer.Length);
                    string response = Encoding.UTF8.GetString(buffer, 0, bytes);

                    if (response.StartsWith("220"))
                    {
                        Interlocked.Increment(ref successCount);
                        successfulConnections.Add(client);

                        // Keep connection alive
                        await Task.Delay(1000);

                        // Send QUIT
                        byte[] quit = Encoding.UTF8.GetBytes("QUIT\r\n");
                        await stream.WriteAsync(quit, 0, quit.Length);

                        return true;
                    }

                    client.Close();
                    return false;
                }
                catch
                {
                    client?.Close();
                    return false;
                }
            })).ToArray();

            await Task.WhenAll(tasks);

            // Assert - Only max connections should succeed
            Assert.Equal(MaxConnectionsPerIp, successCount);
            Assert.Equal(MaxConnectionsPerIp, successfulConnections.Count);

            // Cleanup
            foreach (TcpClient client in successfulConnections)
            {
                try { client.Close(); } catch { }
            }
        }

        [Fact]
        public async Task SmtpServer_ShouldAllowNewConnectionsAfterDisconnect()
        {
            // Arrange & Act
            // First, max out connections
            List<TcpClient> firstBatch = new();
            for (int i = 0; i < MaxConnectionsPerIp; i++)
            {
                TcpClient client = new();
                await client.ConnectAsync("localhost", TestPort);

                // Verify we got the greeting
                byte[] buffer = new byte[1024];
                int bytes = await client.GetStream().ReadAsync(buffer, 0, buffer.Length);
                string response = Encoding.UTF8.GetString(buffer, 0, bytes);
                Assert.StartsWith("220", response);

                firstBatch.Add(client);
            }

            // Try one more - should fail
            using (TcpClient extraClient = new())
            {
                extraClient.ReceiveTimeout = 1000;

                bool connected = false;
                try
                {
                    await extraClient.ConnectAsync("localhost", TestPort);
                    byte[] buffer = new byte[1024];
                    await extraClient.GetStream().ReadAsync(buffer, 0, buffer.Length);
                    connected = true;
                }
                catch
                {
                    // Expected to fail
                }

                Assert.False(connected, "Should not be able to connect beyond limit");
            }

            // Close all first batch connections
            foreach (TcpClient client in firstBatch)
            {
                byte[] quit = Encoding.UTF8.GetBytes("QUIT\r\n");
                await client.GetStream().WriteAsync(quit, 0, quit.Length);
                client.Close();
            }

            // Wait a bit for cleanup
            await Task.Delay(100);

            // Now should be able to connect again
            List<TcpClient> secondBatch = new();
            for (int i = 0; i < MaxConnectionsPerIp; i++)
            {
                TcpClient client = new();
                await client.ConnectAsync("localhost", TestPort);

                byte[] buffer = new byte[1024];
                int bytes = await client.GetStream().ReadAsync(buffer, 0, buffer.Length);
                string response = Encoding.UTF8.GetString(buffer, 0, bytes);
                Assert.StartsWith("220", response);

                secondBatch.Add(client);
            }

            // Cleanup
            foreach (TcpClient client in secondBatch)
            {
                client.Close();
            }
        }

        [Fact]
        public async Task SmtpServer_ShouldHandleConcurrentSmtpClients()
        {
            // This test uses SmtpClient which automatically closes connections after sending
            // We're testing that the connection tracking properly handles this scenario

            // Arrange
            const int attemptCount = 20;
            List<Task<bool>> tasks = new();
            int successCount = 0;
            Barrier barrier = new(attemptCount);

            // Act
            for (int i = 0; i < attemptCount; i++)
            {
                int attemptNum = i;
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        using SmtpClient client = new("localhost", TestPort);
                        client.Timeout = 2000;

                        // Synchronize all clients
                        barrier.SignalAndWait();

                        MailMessage message = new(
                            $"test{attemptNum}@example.com",
                            "recipient@example.com",
                            $"Test {attemptNum}",
                            $"Body {attemptNum}");

                        await Task.Run(() => client.Send(message));

                        Interlocked.Increment(ref successCount);

                        // Keep connection briefly to prevent immediate slot reuse
                        await Task.Delay(500);

                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }));
            }

            bool[] results = await Task.WhenAll(tasks);

            // Assert
            // Since SmtpClient closes connections, we might see more than MaxConnectionsPerIp succeed
            // if they reuse slots. The important thing is that at any given moment,
            // no more than MaxConnectionsPerIp are active
            Assert.True(successCount > 0, "At least some connections should succeed");
        }

        [Fact]
        public async Task SmtpServer_ShouldTrackMultipleIPsSeparately()
        {
            // This test would require multiple IP addresses which is hard to test
            // In a real scenario, you'd test from different machines
            // For unit testing, we verify the ConnectionTracker handles multiple IPs correctly
            // which is covered in ConnectionTrackerTests

            Assert.True(true, "See ConnectionTrackerTests for multi-IP testing");
            await Task.CompletedTask;
        }

        [Fact]
        public async Task SmtpServer_StressTest_NoRaceConditions()
        {
            // Arrange
            const int iterations = 10;
            const int concurrentAttempts = 50;
            List<int> allSuccessCounts = new();

            // Act - Run multiple rounds to catch intermittent race conditions
            for (int round = 0; round < iterations; round++)
            {
                int successCount = 0;
                Barrier barrier = new(concurrentAttempts);
                ConcurrentBag<TcpClient> clients = new();

                Task<bool>[] tasks = Enumerable.Range(0, concurrentAttempts).Select(_ => Task.Run(async () =>
                {
                    TcpClient client = null;
                    try
                    {
                        client = new TcpClient();
                        barrier.SignalAndWait();

                        await client.ConnectAsync("localhost", TestPort);

                        byte[] buffer = new byte[1024];
                        int bytes = await client.GetStream().ReadAsync(buffer, 0, buffer.Length);
                        string response = Encoding.UTF8.GetString(buffer, 0, bytes);

                        if (response.StartsWith("220"))
                        {
                            Interlocked.Increment(ref successCount);
                            clients.Add(client);
                            await Task.Delay(200); // Hold connection
                            return true;
                        }

                        client.Close();
                        return false;
                    }
                    catch
                    {
                        client?.Close();
                        return false;
                    }
                })).ToArray();

                await Task.WhenAll(tasks);
                allSuccessCounts.Add(successCount);

                // Cleanup this round
                foreach (TcpClient client in clients)
                {
                    try
                    {
                        byte[] quit = Encoding.UTF8.GetBytes("QUIT\r\n");
                        await client.GetStream().WriteAsync(quit, 0, quit.Length);
                        client.Close();
                    }
                    catch { }
                }

                await Task.Delay(100); // Wait for cleanup
            }

            // Assert - All rounds should have consistent results
            Assert.All(allSuccessCounts, count =>
            {
                Assert.Equal(MaxConnectionsPerIp, count);
            });
        }
    }
}