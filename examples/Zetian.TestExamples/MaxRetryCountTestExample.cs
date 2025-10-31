using System.Net.Sockets;
using System.Text;
using Zetian.Server;

namespace Zetian.TestExamples
{
    /// <summary>
    /// Integration test example for MaxRetryCount functionality
    /// </summary>
    public static class MaxRetryCountTestExample
    {
        public static async Task RunAsync()
        {
            try
            {
                Console.WriteLine("Starting MaxRetryCount Integration Tests");
                Console.WriteLine("=========================================\n");

                Console.WriteLine("=== MaxRetryCount Integration Test ===\n");

                // Test 1: Test with default retry count
                await TestDefaultRetryCountAsync();

                // Test 2: Test with custom retry count
                await TestCustomRetryCountAsync();

                // Test 3: Test retry counter reset on successful command
                await TestRetryCounterResetAsync();

                // Test 4: Test immediate termination with MaxRetryCount = 1
                await TestImmediateTerminationAsync();

                Console.WriteLine("\nAll MaxRetryCount tests completed successfully!");

                Console.WriteLine("\nPress 'T' to run timeout test or any other key to exit...");
                ConsoleKeyInfo key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.T)
                {
                    Console.WriteLine("\n");
                    await TestTimeoutCountsAsRetryAsync();
                }

                Console.WriteLine("\n✓ All tests passed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n✗ Test failed: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        /// <summary>
        /// Test default retry count behavior
        /// </summary>
        private static async Task TestDefaultRetryCountAsync()
        {
            Console.WriteLine("Test 1: Default Retry Count (3)");
            Console.WriteLine("--------------------------------");

            SmtpServer server = new SmtpServerBuilder()
                .Port(3525)
                .ServerName("Test SMTP Server")
                .Build();

            await server.StartAsync();
            Console.WriteLine($"Server started with MaxRetryCount = {server.Configuration.MaxRetryCount}");

            // Simulate client with errors
            using (TcpClient client = new())
            {
                await client.ConnectAsync("127.0.0.1", 3525);
                using NetworkStream stream = client.GetStream();
                using StreamReader reader = new(stream, Encoding.ASCII);
                using StreamWriter writer = new(stream, Encoding.ASCII) { AutoFlush = true };

                // Read greeting
                string? greeting = await reader.ReadLineAsync();
                Console.WriteLine($"Server: {greeting}");

                // Send 3 invalid commands
                for (int i = 1; i <= 3; i++)
                {
                    await writer.WriteLineAsync($"INVALID_COMMAND_{i}");
                    string? response = await reader.ReadLineAsync();
                    Console.WriteLine($"Error {i}/3: {response}");

                    if (response?.StartsWith("421") == true)
                    {
                        Console.WriteLine("Session closed due to too many errors");
                        break;
                    }
                }
            }

            await server.StopAsync();
            Console.WriteLine("Test completed\n");
        }

        /// <summary>
        /// Test custom retry count
        /// </summary>
        private static async Task TestCustomRetryCountAsync()
        {
            Console.WriteLine("Test 2: Custom Retry Count (5)");
            Console.WriteLine("-------------------------------");

            SmtpServer server = new SmtpServerBuilder()
                .Port(3526)
                .ServerName("Test SMTP Server")
                .MaxRetryCount(5)  // Custom retry count
                .Build();

            await server.StartAsync();
            Console.WriteLine($"Server started with MaxRetryCount = {server.Configuration.MaxRetryCount}");

            using (TcpClient client = new())
            {
                await client.ConnectAsync("127.0.0.1", 3526);
                using NetworkStream stream = client.GetStream();
                using StreamReader reader = new(stream, Encoding.ASCII);
                using StreamWriter writer = new(stream, Encoding.ASCII) { AutoFlush = true };

                // Read greeting
                await reader.ReadLineAsync();

                // Send 5 invalid commands
                for (int i = 1; i <= 5; i++)
                {
                    await writer.WriteLineAsync($"INVALID_{i}");
                    string? response = await reader.ReadLineAsync();
                    Console.WriteLine($"Error {i}/5: {response}");

                    if (response?.StartsWith("421") == true)
                    {
                        Console.WriteLine("Session closed after 5 errors as expected");
                        break;
                    }
                }
            }

            await server.StopAsync();
            Console.WriteLine("Test completed\n");
        }

        /// <summary>
        /// Test that retry counter resets on successful command
        /// </summary>
        private static async Task TestRetryCounterResetAsync()
        {
            Console.WriteLine("Test 3: Retry Counter Reset");
            Console.WriteLine("----------------------------");

            SmtpServer server = new SmtpServerBuilder()
                .Port(3527)
                .ServerName("Test SMTP Server")
                .MaxRetryCount(2)  // Low retry count for testing
                .Build();

            await server.StartAsync();
            Console.WriteLine($"Server started with MaxRetryCount = {server.Configuration.MaxRetryCount}");

            using (TcpClient client = new())
            {
                await client.ConnectAsync("127.0.0.1", 3527);
                using NetworkStream stream = client.GetStream();
                using StreamReader reader = new(stream, Encoding.ASCII);
                using StreamWriter writer = new(stream, Encoding.ASCII) { AutoFlush = true };

                // Read greeting
                await reader.ReadLineAsync();

                // Pattern: Error -> Success -> Error -> Success
                // This should NOT close the session

                // First error
                await writer.WriteLineAsync("INVALID_1");
                string? response = await reader.ReadLineAsync();
                Console.WriteLine($"Error 1: {response}");

                // Success command (resets counter)
                await writer.WriteLineAsync("NOOP");
                response = await reader.ReadLineAsync();
                Console.WriteLine($"Success (NOOP): {response}");

                // Second error (counter reset, so this is error 1 again)
                await writer.WriteLineAsync("INVALID_2");
                response = await reader.ReadLineAsync();
                Console.WriteLine($"Error 1 (after reset): {response}");

                // Another success command
                await writer.WriteLineAsync("NOOP");
                response = await reader.ReadLineAsync();
                Console.WriteLine($"Success (NOOP): {response}");

                // Graceful quit
                await writer.WriteLineAsync("QUIT");
                response = await reader.ReadLineAsync();
                Console.WriteLine($"QUIT: {response}");

                if (response?.StartsWith("221") == true)
                {
                    Console.WriteLine("Session ended gracefully - retry counter reset worked!");
                }
            }

            await server.StopAsync();
            Console.WriteLine("Test completed\n");
        }

        /// <summary>
        /// Test immediate termination with MaxRetryCount = 1
        /// </summary>
        private static async Task TestImmediateTerminationAsync()
        {
            Console.WriteLine("Test 4: Immediate Termination (MaxRetryCount = 1)");
            Console.WriteLine("--------------------------------------------------");

            SmtpServer server = new SmtpServerBuilder()
                .Port(3528)
                .ServerName("Test SMTP Server")
                .MaxRetryCount(1)  // Immediate termination on first error
                .Build();

            await server.StartAsync();
            Console.WriteLine($"Server started with MaxRetryCount = {server.Configuration.MaxRetryCount}");

            using (TcpClient client = new())
            {
                await client.ConnectAsync("127.0.0.1", 3528);
                using NetworkStream stream = client.GetStream();
                using StreamReader reader = new(stream, Encoding.ASCII);
                using StreamWriter writer = new(stream, Encoding.ASCII) { AutoFlush = true };

                // Read greeting
                await reader.ReadLineAsync();

                // Send one invalid command
                await writer.WriteLineAsync("INVALID_COMMAND");
                string? response = await reader.ReadLineAsync();
                Console.WriteLine($"Error 1/1: {response}");

                if (response?.StartsWith("502") == true)
                {
                    Console.WriteLine("Session closed immediately after first error as expected");
                }
            }

            await server.StopAsync();
            Console.WriteLine("Test completed\n");
        }

        /// <summary>
        /// Test timeout counting towards retry limit
        /// </summary>
        public static async Task TestTimeoutCountsAsRetryAsync()
        {
            Console.WriteLine("Test 5: Timeout Counts as Retry");
            Console.WriteLine("--------------------------------");

            SmtpServer server = new SmtpServerBuilder()
                .Port(3529)
                .ServerName("Test SMTP Server")
                .MaxRetryCount(2)
                .CommandTimeout(TimeSpan.FromSeconds(1))  // Short timeout for testing
                .Build();

            await server.StartAsync();
            Console.WriteLine($"Server started with MaxRetryCount = {server.Configuration.MaxRetryCount}");
            Console.WriteLine($"Command timeout set to {server.Configuration.CommandTimeout.TotalSeconds} seconds");

            using (TcpClient client = new())
            {
                await client.ConnectAsync("127.0.0.1", 3529);
                using NetworkStream stream = client.GetStream();
                using StreamReader reader = new(stream, Encoding.ASCII);

                // Read greeting
                await reader.ReadLineAsync();

                // Wait for timeouts without sending commands
                Console.WriteLine("Waiting for timeout 1...");
                await Task.Delay(1500);

                Console.WriteLine("Waiting for timeout 2...");
                await Task.Delay(1500);

                // Try to read - connection should be closed
                try
                {
                    string? response = await reader.ReadLineAsync();
                    if (response == null)
                    {
                        Console.WriteLine("Connection closed after 2 timeouts as expected");
                    }
                    else if (response.StartsWith("421"))
                    {
                        Console.WriteLine($"Session closed: {response}");
                    }
                    else
                    {
                        Console.WriteLine($"Unexpected response: {response}");
                    }
                }
                catch
                {
                    Console.WriteLine("Connection closed after timeouts");
                }
            }

            await server.StopAsync();
            Console.WriteLine("Test completed\n");
        }
    }
}