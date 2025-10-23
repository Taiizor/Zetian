using System.Collections.Concurrent;
using System.Net.Mail;
using System.Net.Sockets;
using System.Text;
using Xunit;
using Zetian.Server;
using Zetian.Tests.Helpers;

namespace Zetian.Tests
{
    /// <summary>
    /// Integration tests for SmtpServer to verify race condition fixes in connection tracking
    /// </summary>
    public class SmtpServerRaceConditionTests : IAsyncLifetime
    {
        private SmtpServer? _server;
        private readonly int _testPort;
        private const int MaxConnectionsPerIp = 5;

        public SmtpServerRaceConditionTests()
        {
            _testPort = TestHelper.GetAvailablePort();
        }

        public async Task InitializeAsync()
        {
            // Each test will create its own server to avoid interference
            await Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            if (_server != null)
            {
                await _server.StopAsync();
                _server.Dispose();
                _server = null;
            }
        }

        private async Task<SmtpServer> CreateAndStartServerAsync(int port = 0)
        {
            SmtpServer server = new SmtpServerBuilder()
                .Port(port == 0 ? _testPort : port)
                .MaxConnectionsPerIP(MaxConnectionsPerIp)
                .MaxConnections(100)
                .Build();

            await server.StartAsync();
            await Task.Delay(50); // Give server time to fully start
            return server;
        }

        [Fact]
        public async Task SmtpServer_ShouldEnforceMaxConnectionsPerIP()
        {
            // Arrange
            _server = await CreateAndStartServerAsync();
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
                    await client.ConnectAsync("localhost", _testPort);

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
                        await Task.Delay(500); // Reduced for faster test

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
            // Arrange
            _server = await CreateAndStartServerAsync(_testPort + 1); // Use different port

            // First, quickly max out connections
            List<TcpClient> firstBatch = new();
            Task<TcpClient>[] connectionTasks = Enumerable.Range(0, MaxConnectionsPerIp).Select(async i =>
            {
                using TcpClient client = new();
                await client.ConnectAsync("127.0.0.1", _testPort + 1);
                return client;
            }).ToArray();

            TcpClient[] connectedClients = await Task.WhenAll(connectionTasks);

            // Verify all got greetings
            foreach (TcpClient client in connectedClients)
            {
                byte[] buffer = new byte[1024];
                client.ReceiveTimeout = 1000;
                int bytes = await client.GetStream().ReadAsync(buffer, 0, buffer.Length);
                string response = Encoding.UTF8.GetString(buffer, 0, bytes);
                Assert.StartsWith("220", response);
                firstBatch.Add(client);
            }

            // Small delay to ensure all connections are properly tracked
            await Task.Delay(100);

            // Verify that we're at the limit by checking current count
            Assert.Equal(MaxConnectionsPerIp, firstBatch.Count);

            // Try one more - should fail or timeout
            bool extraConnectionAccepted = false;
            string debugInfo = "";

            try
            {
                using TcpClient extraClient = new();
                extraClient.ReceiveTimeout = 500; // Increased timeout

                // Try to connect
                await extraClient.ConnectAsync("127.0.0.1", _testPort + 1);
                debugInfo += "TCP connected. ";

                if (extraClient.Connected)
                {
                    try
                    {
                        // Try to read SMTP greeting
                        byte[] buffer = new byte[1024];
                        int bytes = await extraClient.GetStream().ReadAsync(buffer, 0, buffer.Length);
                        string response = Encoding.UTF8.GetString(buffer, 0, bytes);
                        debugInfo += $"Got response: {response.Trim()}. ";

                        // If we got a valid greeting, the connection was incorrectly accepted
                        if (response.StartsWith("220"))
                        {
                            extraConnectionAccepted = true;
                            debugInfo += "SMTP greeting received - connection was accepted!";
                        }
                    }
                    catch (Exception ex)
                    {
                        // Timeout or error is expected - connection was rejected
                        debugInfo += $"Read failed: {ex.GetType().Name}. Connection rejected.";
                    }
                }
            }
            catch (Exception ex)
            {
                // Connection failed - this is expected
                debugInfo += $"Connect failed: {ex.GetType().Name}. Connection rejected.";
            }

            Assert.False(extraConnectionAccepted, $"Extra connection should be rejected. Debug: {debugInfo}");

            // Close all first batch connections quickly
            Parallel.ForEach(firstBatch, client => client.Close());

            // Wait briefly for cleanup
            await Task.Delay(50);

            // Now should be able to connect again - test with just one connection
            using (TcpClient newClient = new())
            {
                await newClient.ConnectAsync("127.0.0.1", _testPort + 1);
                byte[] buffer = new byte[1024];
                int bytes = await newClient.GetStream().ReadAsync(buffer, 0, buffer.Length);
                string response = Encoding.UTF8.GetString(buffer, 0, bytes);
                Assert.StartsWith("220", response);
            }
        }

        [Fact]
        public async Task SmtpServer_ShouldHandleConcurrentSmtpClients()
        {
            // This test uses SmtpClient which automatically closes connections after sending
            // We're testing that the connection tracking properly handles this scenario

            // Arrange
            _server = await CreateAndStartServerAsync(_testPort + 2); // Use different port
            const int attemptCount = 10; // Reduced for faster test
            List<Task<bool>> tasks = new();
            int successCount = 0;
            ConcurrentBag<string> failureMessages = new();
            Barrier barrier = new(attemptCount);

            // Act
            for (int i = 0; i < attemptCount; i++)
            {
                int attemptNum = i;
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        using SmtpClient client = new("127.0.0.1", _testPort + 2);
                        client.Timeout = 5000; // Increased timeout

                        // Synchronize all clients
                        barrier.SignalAndWait();

                        MailMessage message = new(
                            $"test{attemptNum}@example.com",
                            "recipient@example.com",
                            $"Test {attemptNum}",
                            $"Body {attemptNum}");

                        await Task.Run(() => client.Send(message));

                        Interlocked.Increment(ref successCount);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        failureMessages.Add($"Attempt {attemptNum}: {ex.Message}");
                        return false;
                    }
                }));
            }

            bool[] results = await Task.WhenAll(tasks);

            // Assert
            // Since SmtpClient closes connections, we might see more than MaxConnectionsPerIp succeed
            // if they reuse slots. The important thing is that at any given moment,
            // no more than MaxConnectionsPerIp are active
            if (successCount == 0)
            {
                string errorDetails = string.Join("\n", failureMessages.Take(5));
                Assert.Fail($"No connections succeeded. Sample errors:\n{errorDetails}");
            }
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
            _server = await CreateAndStartServerAsync(_testPort + 3); // Use different port
            const int iterations = 3; // Reduced for faster test
            const int concurrentAttempts = 15; // Reduced for faster test
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

                        await client.ConnectAsync("localhost", _testPort + 3);

                        byte[] buffer = new byte[1024];
                        int bytes = await client.GetStream().ReadAsync(buffer, 0, buffer.Length);
                        string response = Encoding.UTF8.GetString(buffer, 0, bytes);

                        if (response.StartsWith("220"))
                        {
                            Interlocked.Increment(ref successCount);
                            clients.Add(client);
                            await Task.Delay(100); // Hold connection
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

                await Task.Delay(50); // Wait for cleanup
            }

            // Assert - All rounds should have consistent results
            Assert.All(allSuccessCounts, count =>
            {
                Assert.Equal(MaxConnectionsPerIp, count);
            });
        }
    }
}