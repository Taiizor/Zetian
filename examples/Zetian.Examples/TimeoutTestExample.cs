using System.Net.Sockets;
using Zetian.Server;

namespace Zetian.Examples
{
    /// <summary>
    /// Example demonstrating timeout functionality in SMTP server
    /// </summary>
    public class TimeoutTestExample
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("=== SMTP Server Timeout Test Example ===\n");

            // Create server with short timeouts for testing
            SmtpServer server = new SmtpServerBuilder()
                .Port(2525)
                .ServerName("TimeoutTest SMTP Server")
                .ConnectionTimeout(TimeSpan.FromSeconds(10))  // Connection will timeout after 10 seconds of inactivity
                .CommandTimeout(TimeSpan.FromSeconds(5))      // Each command must complete within 5 seconds
                .DataTimeout(TimeSpan.FromSeconds(8))         // Data transfer must complete within 8 seconds
                .EnableVerboseLogging(true)
                .Build();

            // Start server
            await server.StartAsync();
            Console.WriteLine($"SMTP server started on port 2525");
            Console.WriteLine($"ConnectionTimeout: {server.Configuration.ConnectionTimeout}");
            Console.WriteLine($"CommandTimeout: {server.Configuration.CommandTimeout}");
            Console.WriteLine($"DataTimeout: {server.Configuration.DataTimeout}\n");

            // Test scenarios
            Console.WriteLine("Running test scenarios...\n");

            // Test 1: Connection Timeout
            Console.WriteLine("Test 1: Testing ConnectionTimeout (will timeout after 10 seconds)...");
            await Task.Run(TestConnectionTimeout);

            await Task.Delay(2000);

            // Test 2: Command Timeout
            Console.WriteLine("\nTest 2: Testing CommandTimeout (will timeout after 5 seconds)...");
            await Task.Run(TestCommandTimeout);

            await Task.Delay(2000);

            // Test 3: Data Timeout
            Console.WriteLine("\nTest 3: Testing DataTimeout (will timeout after 8 seconds)...");
            await Task.Run(TestDataTimeout);

            await Task.Delay(2000);

            // Test 4: Normal operation (no timeout)
            Console.WriteLine("\nTest 4: Testing normal operation (no timeout)...");
            await Task.Run(TestNormalOperation);

            Console.WriteLine("\nPress any key to stop the server...");
            Console.ReadKey();

            // Stop server
            await server.StopAsync();
            Console.WriteLine("Server stopped.");
        }

        private static async Task TestConnectionTimeout()
        {
            try
            {
                using TcpClient client = new();
                await client.ConnectAsync("localhost", 2525);

                NetworkStream stream = client.GetStream();
                StreamReader reader = new(stream);
                StreamWriter writer = new(stream) { AutoFlush = true };

                // Read greeting
                string? greeting = await reader.ReadLineAsync();
                Console.WriteLine($"  Server: {greeting}");

                // Send EHLO
                await writer.WriteLineAsync("EHLO test.com");
                string? line;
                while ((line = await reader.ReadLineAsync()) != null && !line.StartsWith("250 "))
                {
                    Console.WriteLine($"  Server: {line}");
                }

                // Now wait for connection timeout (10 seconds)
                Console.WriteLine("  Waiting for connection timeout...");
                await Task.Delay(11000); // Wait 11 seconds to trigger timeout

                // Try to send another command (should fail)
                await writer.WriteLineAsync("NOOP");
                string? response = await reader.ReadLineAsync();
                Console.WriteLine($"  Server response after timeout: {response}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Connection closed as expected: {ex.Message}");
            }
        }

        private static async Task TestCommandTimeout()
        {
            try
            {
                using TcpClient client = new();
                await client.ConnectAsync("localhost", 2525);

                NetworkStream stream = client.GetStream();
                StreamReader reader = new(stream);
                StreamWriter writer = new(stream) { AutoFlush = true };

                // Read greeting
                string? greeting = await reader.ReadLineAsync();
                Console.WriteLine($"  Server: {greeting}");

                // Send incomplete command and wait
                await writer.WriteAsync("EHLO "); // Don't send complete line
                await writer.FlushAsync();

                Console.WriteLine("  Sent incomplete command, waiting for command timeout...");

                // Server should timeout waiting for complete line
                await Task.Delay(6000); // Wait 6 seconds to trigger command timeout

                string? response = await reader.ReadLineAsync();
                Console.WriteLine($"  Server response after timeout: {response}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Command timeout occurred as expected: {ex.Message}");
            }
        }

        private static async Task TestDataTimeout()
        {
            try
            {
                using TcpClient client = new();
                await client.ConnectAsync("localhost", 2525);

                NetworkStream stream = client.GetStream();
                StreamReader reader = new(stream);
                StreamWriter writer = new(stream) { AutoFlush = true };

                // Read greeting
                string? greeting = await reader.ReadLineAsync();
                Console.WriteLine($"  Server: {greeting}");

                // Send EHLO
                await writer.WriteLineAsync("EHLO test.com");
                string? line;
                while ((line = await reader.ReadLineAsync()) != null && !line.StartsWith("250 "))
                {
                    Console.WriteLine($"  Server: {line}");
                }

                // Send MAIL FROM
                await writer.WriteLineAsync("MAIL FROM:<sender@test.com>");
                string? mailResponse = await reader.ReadLineAsync();
                Console.WriteLine($"  Server: {mailResponse}");

                // Send RCPT TO
                await writer.WriteLineAsync("RCPT TO:<recipient@test.com>");
                string? rcptResponse = await reader.ReadLineAsync();
                Console.WriteLine($"  Server: {rcptResponse}");

                // Send DATA command
                await writer.WriteLineAsync("DATA");
                string? dataResponse = await reader.ReadLineAsync();
                Console.WriteLine($"  Server: {dataResponse}");

                // Start sending data but don't complete it
                await writer.WriteLineAsync("Subject: Test");
                await writer.WriteLineAsync("");
                await writer.WriteLineAsync("This is a test message");
                // Don't send the terminating "."

                Console.WriteLine("  Started data transfer but not completing it, waiting for data timeout...");
                await Task.Delay(9000); // Wait 9 seconds to trigger data timeout

                string? timeoutResponse = await reader.ReadLineAsync();
                Console.WriteLine($"  Server response after timeout: {timeoutResponse}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Data timeout occurred as expected: {ex.Message}");
            }
        }

        private static async Task TestNormalOperation()
        {
            try
            {
                using TcpClient client = new();
                await client.ConnectAsync("localhost", 2525);

                NetworkStream stream = client.GetStream();
                StreamReader reader = new(stream);
                StreamWriter writer = new(stream) { AutoFlush = true };

                // Read greeting
                string? greeting = await reader.ReadLineAsync();
                Console.WriteLine($"  Server: {greeting}");

                // Send EHLO
                await writer.WriteLineAsync("EHLO test.com");
                string? line;
                while ((line = await reader.ReadLineAsync()) != null && !line.StartsWith("250 "))
                {
                    Console.WriteLine($"  Server: {line}");
                }

                // Send MAIL FROM
                await writer.WriteLineAsync("MAIL FROM:<sender@test.com>");
                string? mailResponse = await reader.ReadLineAsync();
                Console.WriteLine($"  Server: {mailResponse}");

                // Send RCPT TO
                await writer.WriteLineAsync("RCPT TO:<recipient@test.com>");
                string? rcptResponse = await reader.ReadLineAsync();
                Console.WriteLine($"  Server: {rcptResponse}");

                // Send DATA command
                await writer.WriteLineAsync("DATA");
                string? dataResponse = await reader.ReadLineAsync();
                Console.WriteLine($"  Server: {dataResponse}");

                // Send complete message
                await writer.WriteLineAsync("Subject: Test");
                await writer.WriteLineAsync("");
                await writer.WriteLineAsync("This is a test message");
                await writer.WriteLineAsync(".");

                string? acceptResponse = await reader.ReadLineAsync();
                Console.WriteLine($"  Server: {acceptResponse}");

                // Send QUIT
                await writer.WriteLineAsync("QUIT");
                string? quitResponse = await reader.ReadLineAsync();
                Console.WriteLine($"  Server: {quitResponse}");

                Console.WriteLine("  Normal operation completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Unexpected error: {ex.Message}");
            }
        }
    }
}