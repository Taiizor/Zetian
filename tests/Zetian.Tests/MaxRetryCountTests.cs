using System.Net.Sockets;
using Xunit;
using Zetian.Server;
using Zetian.Tests.Helpers;

namespace Zetian.Tests
{
    /// <summary>
    /// Unit tests for MaxRetryCount configuration
    /// </summary>
    public class MaxRetryCountTests : IDisposable
    {
        private SmtpServer? _server;

        public void Dispose()
        {
            _server?.Dispose();
        }

        [Fact]
        public void MaxRetryCount_DefaultValue_ShouldBe3()
        {
            // Arrange & Act
            SmtpServer server = new SmtpServerBuilder()
                .Port(TestHelper.GetAvailablePort())
                .Build();

            // Assert
            Assert.Equal(3, server.Configuration.MaxRetryCount);
        }

        [Fact]
        public void MaxRetryCount_SetCustomValue_ShouldBeApplied()
        {
            // Arrange & Act
            SmtpServer server = new SmtpServerBuilder()
                .Port(TestHelper.GetAvailablePort())
                .MaxRetryCount(5)
                .Build();

            // Assert
            Assert.Equal(5, server.Configuration.MaxRetryCount);
        }

        [Fact]
        public void MaxRetryCount_SetToZero_ShouldBeAllowed()
        {
            // Arrange & Act
            SmtpServer server = new SmtpServerBuilder()
                .Port(TestHelper.GetAvailablePort())
                .MaxRetryCount(0)
                .Build();

            // Assert
            Assert.Equal(0, server.Configuration.MaxRetryCount);
        }

        [Fact]
        public void MaxRetryCount_SetNegativeValue_ShouldThrowException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentException>(() =>
            {
                SmtpServer server = new SmtpServerBuilder()
                    .Port(TestHelper.GetAvailablePort())
                    .MaxRetryCount(-1)
                    .Build();
            });
        }

        [Fact]
        public async Task MaxRetryCount_ExceedsLimit_ShouldCloseSession()
        {
            // Arrange
            int port = TestHelper.GetAvailablePort();
            bool sessionClosed = false;
            int messagesProcessed = 0;

            _server = new SmtpServerBuilder()
                .Port(port)
                .MaxRetryCount(2)  // Allow only 2 errors
                .Build();

            _server.SessionCompleted += (sender, e) =>
            {
                sessionClosed = true;
                messagesProcessed = e.Session.MessageCount;
            };

            await _server.StartAsync();

            // Act - Send multiple invalid commands to trigger retry limit
            using TcpClient client = new();
            await client.ConnectAsync("127.0.0.1", port);

            using NetworkStream stream = client.GetStream();
            using StreamReader reader = new(stream);
            using StreamWriter writer = new(stream) { AutoFlush = true };

            // Read greeting
            await reader.ReadLineAsync();

            // Send invalid commands to trigger errors
            bool sessionClosedByServer = false;
            for (int i = 1; i <= 3; i++)
            {
                await writer.WriteLineAsync("INVALID_COMMAND");
                await writer.FlushAsync();

                string? response = await reader.ReadLineAsync();
                if (response?.StartsWith("421") == true)
                {
                    // Session closed due to too many errors
                    sessionClosedByServer = true;
                    break;
                }

                await Task.Delay(100);
            }

            // Close the client connection to trigger SessionCompleted
            client.Close();

            // Wait for session to complete
            await Task.Delay(1000);

            // If server didn't close the session, something went wrong
            Assert.True(sessionClosedByServer, "Server should have sent 421 error after exceeding retry limit");

            // Assert
            Assert.True(sessionClosed, "Session should have been closed after exceeding retry limit");

            await _server.StopAsync();
        }

        [Fact]
        public async Task MaxRetryCount_ErrorsWithRecovery_ShouldResetCounter()
        {
            // Arrange
            int port = TestHelper.GetAvailablePort();

            _server = new SmtpServerBuilder()
                .Port(port)
                .MaxRetryCount(2)  // Allow only 2 consecutive errors
                .Build();

            await _server.StartAsync();

            // Act
            using TcpClient client = new();
            await client.ConnectAsync("127.0.0.1", port);

            using NetworkStream stream = client.GetStream();
            using StreamReader reader = new(stream);
            using StreamWriter writer = new(stream) { AutoFlush = true };

            // Read greeting
            await reader.ReadLineAsync();

            // Send EHLO (valid command)
            await writer.WriteLineAsync("EHLO localhost");
            await writer.FlushAsync();

            // Read EHLO response (multiple lines)
            string? response;
            do
            {
                response = await reader.ReadLineAsync();
            } while (response != null && response.StartsWith("250-"));

            // Send invalid command (error 1)
            await writer.WriteLineAsync("INVALID_COMMAND1");
            await writer.FlushAsync();
            response = await reader.ReadLineAsync();
            Assert.NotNull(response);
            Assert.StartsWith("502", response); // Command not implemented

            // Send valid command to reset counter
            await writer.WriteLineAsync("NOOP");
            await writer.FlushAsync();
            response = await reader.ReadLineAsync();
            Assert.NotNull(response);
            Assert.StartsWith("250", response);

            // Send another invalid command (error should be reset to 1)
            await writer.WriteLineAsync("INVALID_COMMAND2");
            await writer.FlushAsync();
            response = await reader.ReadLineAsync();
            Assert.NotNull(response);
            Assert.StartsWith("502", response);

            // Send QUIT to gracefully close
            await writer.WriteLineAsync("QUIT");
            await writer.FlushAsync();
            response = await reader.ReadLineAsync();

            // Assert - Session should still be active (not closed due to errors)
            Assert.NotNull(response);
            Assert.StartsWith("221", response); // Service closing

            await _server.StopAsync();
        }

        [Fact]
        public async Task MaxRetryCount_TimeoutErrors_ShouldCount()
        {
            // Arrange
            int port = TestHelper.GetAvailablePort();

            _server = new SmtpServerBuilder()
                .Port(port)
                .MaxRetryCount(1)  // Close on first timeout
                .CommandTimeout(TimeSpan.FromMilliseconds(500))  // Short timeout
                .Build();

            await _server.StartAsync();

            // Act
            using TcpClient client = new();
            await client.ConnectAsync("127.0.0.1", port);

            using NetworkStream stream = client.GetStream();
            using StreamReader reader = new(stream);

            // Read greeting
            await reader.ReadLineAsync();

            // Don't send any command and wait for timeout
            await Task.Delay(1000);

            // Try to read response (should be timeout/disconnect message)
            string? response = await reader.ReadLineAsync();

            // Assert
            if (response != null)
            {
                Assert.StartsWith("421", response); // Service not available / Too many errors
            }

            await _server.StopAsync();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(5)]
        [InlineData(10)]
        public void MaxRetryCount_VariousValues_ShouldBeConfigurable(int retryCount)
        {
            // Arrange & Act
            SmtpServer server = new SmtpServerBuilder()
                .Port(TestHelper.GetAvailablePort())
                .MaxRetryCount(retryCount)
                .Build();

            // Assert
            Assert.Equal(retryCount, server.Configuration.MaxRetryCount);
        }
    }
}