using System.Diagnostics;
using System.Net.Mail;
using Zetian.Server;

namespace Zetian.TestExamples
{
    /// <summary>
    /// Tests the race condition fixes in IP connection tracking
    /// </summary>
    public static class RaceConditionTestExample
    {
        private const int Port = 2525;
        private const int MaxConnectionsPerIp = 5;
        private const int ConcurrentAttempts = 20;

        public static async Task RunAsync()
        {
            Console.WriteLine("=== Race Condition Test for IP Connection Tracking ===");
            Console.WriteLine($"Max connections per IP: {MaxConnectionsPerIp}");
            Console.WriteLine($"Concurrent connection attempts: {ConcurrentAttempts}");
            Console.WriteLine();

            // Create and start server
            SmtpServer server = new SmtpServerBuilder()
                .Port(Port)
                .MaxConnectionsPerIP(MaxConnectionsPerIp)
                .MaxConnections(100)
                .Build();

            server.MessageReceived += (s, e) =>
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Message received from {e.Message.From?.Address}");
            };

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
            Console.WriteLine("Expected behavior: Only 5 connections should succeed, others should be rejected");
            Console.WriteLine();

            List<Task<bool>> tasks = [];
            List<SmtpClient> clients = [];
            int successCount = 0;
            int failureCount = 0;
            Stopwatch sw = Stopwatch.StartNew();

            // Use a barrier to ensure all connections start at the same time
            Barrier barrier = new(ConcurrentAttempts);

            // Create many concurrent connection attempts
            for (int i = 0; i < ConcurrentAttempts; i++)
            {
                int attemptNumber = i;
                tasks.Add(Task.Run(async () =>
                {
                    SmtpClient? client = null;
                    try
                    {
                        client = new SmtpClient("localhost", Port)
                        {
                            Timeout = 5000
                        };

                        // Wait for all tasks to be ready
                        barrier.SignalAndWait();

                        // Now all tasks try to connect at the same time!
                        // Try to send a message
                        MailMessage message = new(
                            $"test{attemptNumber}@example.com",
                            "recipient@example.com",
                            $"Test {attemptNumber}",
                            $"Concurrent test message {attemptNumber}");

                        await Task.Run(() => client.Send(message));

                        Interlocked.Increment(ref successCount);
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ✓ Connection {attemptNumber} succeeded");

                        lock (clients) { clients.Add(client); }

                        // Keep connection alive for a bit to prevent slot reuse
                        await Task.Delay(1000);

                        return true;
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref failureCount);
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ✗ Connection {attemptNumber} failed: {ex.Message}");
                        client?.Dispose();
                        return false;
                    }
                }));
            }

            // Wait for all attempts to complete
            bool[] results = await Task.WhenAll(tasks);
            sw.Stop();

            // Clean up all clients
            foreach (SmtpClient client in clients)
            {
                try { client.Dispose(); } catch { }
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
            if (successCount <= MaxConnectionsPerIp)
            {
                Console.WriteLine("✅ PASS: Connection limiting is working correctly!");
                Console.WriteLine($"   Only {successCount} connections succeeded (max allowed: {MaxConnectionsPerIp})");
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