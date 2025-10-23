using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using Zetian.Server;

namespace Zetian.TestExamples
{
    /// <summary>
    /// Improved test that keeps connections open simultaneously
    /// </summary>
    public static class ImprovedRaceConditionTestExample
    {
        private const int Port = 2525;
        private const int MaxConnectionsPerIp = 5;
        private const int ConcurrentAttempts = 20;

        public static async Task RunAsync()
        {
            Console.WriteLine("=== Improved Race Condition Test ===");
            Console.WriteLine($"Max connections per IP: {MaxConnectionsPerIp}");
            Console.WriteLine($"Concurrent connection attempts: {ConcurrentAttempts}");
            Console.WriteLine("This test keeps all connections open simultaneously");
            Console.WriteLine();

            // Create and start server
            SmtpServer server = new SmtpServerBuilder()
                .Port(Port)
                .MaxConnectionsPerIP(MaxConnectionsPerIp)
                .MaxConnections(100)
                .Build();

            server.SessionCreated += (s, e) =>
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Session created: {e.Session.Id}");
            };

            server.SessionCompleted += (s, e) =>
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Session completed: {e.Session.Id}");
            };

            await server.StartAsync();
            Console.WriteLine($"SMTP server started on port {Port}");
            Console.WriteLine();

            // Run test
            await RunConcurrentConnectionTest();

            Console.WriteLine("\nPress any key to stop the server...");
            Console.ReadKey();

            await server.StopAsync();
            server.Dispose();
        }

        private static async Task RunConcurrentConnectionTest()
        {
            Console.WriteLine("Starting concurrent connection test...");
            Console.WriteLine($"Opening {ConcurrentAttempts} connections and keeping them open...");
            Console.WriteLine();

            ConcurrentBag<TcpClient> successfulClients = new();
            Barrier barrier = new(ConcurrentAttempts);
            int successCount = 0;
            int failureCount = 0;
            Stopwatch sw = Stopwatch.StartNew();

            // Create tasks that will all try to connect at the same time
            Task<bool>[] tasks = Enumerable.Range(0, ConcurrentAttempts).Select(i => Task.Run(async () =>
            {
                TcpClient? client = null;
                try
                {
                    // Create TCP connection
                    client = new TcpClient();

                    // Wait for all tasks to be ready
                    barrier.SignalAndWait();

                    // Now all tasks try to connect at exactly the same time!
                    await client.ConnectAsync("localhost", Port);

                    // Read the server greeting (220 response)
                    NetworkStream stream = client.GetStream();
                    byte[] buffer = new byte[1024];
                    int bytes = await stream.ReadAsync(buffer, 0, buffer.Length);
                    string response = Encoding.UTF8.GetString(buffer, 0, bytes);

                    if (response.StartsWith("220"))
                    {
                        Interlocked.Increment(ref successCount);
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ✓ Connection {i} succeeded: {response.Trim()}");

                        // Keep the connection open!
                        successfulClients.Add(client);

                        // Keep connection alive for a bit
                        await Task.Delay(2000);

                        // Send QUIT command
                        byte[] quitCmd = Encoding.UTF8.GetBytes("QUIT\r\n");
                        await stream.WriteAsync(quitCmd, 0, quitCmd.Length);

                        return true;
                    }
                    else
                    {
                        Interlocked.Increment(ref failureCount);
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ✗ Connection {i} got unexpected response: {response.Trim()}");
                        client.Close();
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref failureCount);
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ✗ Connection {i} failed: {ex.Message}");
                    client?.Close();
                    return false;
                }
            })).ToArray();

            // Wait for all connection attempts to complete
            await Task.WhenAll(tasks);
            sw.Stop();

            // Close all successful connections
            foreach (TcpClient client in successfulClients)
            {
                try
                {
                    client.Close();
                }
                catch { }
            }

            // Display results
            Console.WriteLine();
            Console.WriteLine("=== Test Results ===");
            Console.WriteLine($"Total attempts: {ConcurrentAttempts}");
            Console.WriteLine($"Successful connections: {successCount}");
            Console.WriteLine($"Failed connections: {failureCount}");
            Console.WriteLine($"Time taken: {sw.ElapsedMilliseconds}ms");
            Console.WriteLine();

            // Verify correctness
            if (successCount == MaxConnectionsPerIp)
            {
                Console.WriteLine("✅ PERFECT: Exactly the right number of connections!");
                Console.WriteLine($"   Exactly {successCount} connections succeeded (max allowed: {MaxConnectionsPerIp})");
            }
            else if (successCount <= MaxConnectionsPerIp)
            {
                Console.WriteLine("✅ PASS: Connection limiting is working!");
                Console.WriteLine($"   {successCount} connections succeeded (max allowed: {MaxConnectionsPerIp})");
            }
            else
            {
                Console.WriteLine("❌ FAIL: Too many connections succeeded!");
                Console.WriteLine($"   {successCount} connections succeeded but max should be {MaxConnectionsPerIp}");
                Console.WriteLine("   This indicates a race condition in connection tracking!");
            }
        }
    }
}