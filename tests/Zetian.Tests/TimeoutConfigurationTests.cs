using System.Net.Sockets;
using Xunit;
using Zetian.Server;
using Zetian.Tests.Helpers;

namespace Zetian.Tests
{
    /// <summary>
    /// Unit tests for timeout configurations in SMTP server
    /// </summary>
    public class TimeoutConfigurationTests : IDisposable
    {
        private SmtpServer? _server;

        public void Dispose()
        {
            _server?.Dispose();
        }

        #region Configuration Tests

        [Fact]
        public void ConnectionTimeout_DefaultValue_ShouldBe5Minutes()
        {
            // Arrange & Act
            SmtpServer server = new SmtpServerBuilder()
                .Port(TestHelper.GetAvailablePort())
                .Build();

            // Assert
            Assert.Equal(TimeSpan.FromMinutes(5), server.Configuration.ConnectionTimeout);
        }

        [Fact]
        public void CommandTimeout_DefaultValue_ShouldBe1Minute()
        {
            // Arrange & Act
            SmtpServer server = new SmtpServerBuilder()
                .Port(TestHelper.GetAvailablePort())
                .Build();

            // Assert
            Assert.Equal(TimeSpan.FromMinutes(1), server.Configuration.CommandTimeout);
        }

        [Fact]
        public void DataTimeout_DefaultValue_ShouldBe3Minutes()
        {
            // Arrange & Act
            SmtpServer server = new SmtpServerBuilder()
                .Port(TestHelper.GetAvailablePort())
                .Build();

            // Assert
            Assert.Equal(TimeSpan.FromMinutes(3), server.Configuration.DataTimeout);
        }

        [Fact]
        public void ConnectionTimeout_SetCustomValue_ShouldBeApplied()
        {
            // Arrange & Act
            TimeSpan customTimeout = TimeSpan.FromSeconds(30);
            SmtpServer server = new SmtpServerBuilder()
                .Port(TestHelper.GetAvailablePort())
                .ConnectionTimeout(customTimeout)
                .Build();

            // Assert
            Assert.Equal(customTimeout, server.Configuration.ConnectionTimeout);
        }

        [Fact]
        public void CommandTimeout_SetCustomValue_ShouldBeApplied()
        {
            // Arrange & Act
            TimeSpan customTimeout = TimeSpan.FromSeconds(15);
            SmtpServer server = new SmtpServerBuilder()
                .Port(TestHelper.GetAvailablePort())
                .CommandTimeout(customTimeout)
                .Build();

            // Assert
            Assert.Equal(customTimeout, server.Configuration.CommandTimeout);
        }

        [Fact]
        public void DataTimeout_SetCustomValue_ShouldBeApplied()
        {
            // Arrange & Act
            TimeSpan customTimeout = TimeSpan.FromSeconds(20);
            SmtpServer server = new SmtpServerBuilder()
                .Port(TestHelper.GetAvailablePort())
                .DataTimeout(customTimeout)
                .Build();

            // Assert
            Assert.Equal(customTimeout, server.Configuration.DataTimeout);
        }

        [Fact]
        public void ConnectionTimeout_SetNegativeValue_ShouldThrowException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentException>(() =>
            {
                SmtpServer server = new SmtpServerBuilder()
                    .Port(TestHelper.GetAvailablePort())
                    .ConnectionTimeout(TimeSpan.FromSeconds(-1))
                    .Build();
            });
        }

        [Fact]
        public void CommandTimeout_SetNegativeValue_ShouldThrowException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentException>(() =>
            {
                SmtpServer server = new SmtpServerBuilder()
                    .Port(TestHelper.GetAvailablePort())
                    .CommandTimeout(TimeSpan.FromSeconds(-1))
                    .Build();
            });
        }

        [Fact]
        public void DataTimeout_SetNegativeValue_ShouldThrowException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentException>(() =>
            {
                SmtpServer server = new SmtpServerBuilder()
                    .Port(TestHelper.GetAvailablePort())
                    .DataTimeout(TimeSpan.FromSeconds(-1))
                    .Build();
            });
        }

        #endregion

        #region Connection Timeout Tests

        [Fact]
        public async Task ConnectionTimeout_WhenExceeded_ShouldCloseConnection()
        {
            // Arrange
            int port = TestHelper.GetAvailablePort();
            TimeSpan connectionTimeout = TimeSpan.FromSeconds(2); // Short timeout for testing
            bool sessionCompleted = false;

            _server = new SmtpServerBuilder()
                .Port(port)
                .ConnectionTimeout(connectionTimeout)
                .Build();

            _server.SessionCompleted += (sender, e) =>
            {
                sessionCompleted = true;
            };

            await _server.StartAsync();

            // Act
            using TcpClient client = new();
            await client.ConnectAsync("127.0.0.1", port);

            using NetworkStream stream = client.GetStream();
            using StreamReader reader = new(stream);
            using StreamWriter writer = new(stream) { AutoFlush = true };

            // Read greeting
            string? greeting = await reader.ReadLineAsync();
            Assert.NotNull(greeting);
            Assert.StartsWith("220", greeting);

            // Send EHLO
            await writer.WriteLineAsync("EHLO test.com");

            // Read EHLO response
            string? line;
            do
            {
                line = await reader.ReadLineAsync();
            } while (line != null && !line.StartsWith("250 "));

            // Wait for timeout to exceed
            await Task.Delay(connectionTimeout.Add(TimeSpan.FromSeconds(1)));

            // Try to read - should fail as connection should be closed
            bool connectionClosed = false;
            try
            {
                await writer.WriteLineAsync("NOOP");
                await reader.ReadLineAsync();
            }
            catch
            {
                connectionClosed = true;
            }

            // Assert
            Assert.True(connectionClosed, "Connection should have been closed after timeout");

            // Wait a bit for session completed event
            await Task.Delay(500);
            Assert.True(sessionCompleted, "Session should have been completed");
        }

        #endregion

        #region Command Timeout Tests

        [Fact]
        public async Task CommandTimeout_WhenExceeded_ShouldReturnTimeoutError()
        {
            // Arrange
            int port = TestHelper.GetAvailablePort();
            TimeSpan commandTimeout = TimeSpan.FromSeconds(2); // Short timeout for testing

            _server = new SmtpServerBuilder()
                .Port(port)
                .CommandTimeout(commandTimeout)
                .Build();

            await _server.StartAsync();

            // Act
            using TcpClient client = new();
            await client.ConnectAsync("127.0.0.1", port);

            using NetworkStream stream = client.GetStream();
            using StreamReader reader = new(stream);
            using StreamWriter writer = new(stream) { AutoFlush = true };

            // Read greeting
            string? greeting = await reader.ReadLineAsync();
            Assert.NotNull(greeting);

            // Send incomplete command (no line ending)
            await writer.WriteAsync("EHLO "); // Don't send complete line
            await writer.FlushAsync();

            // Wait for timeout
            await Task.Delay(commandTimeout.Add(TimeSpan.FromSeconds(1)));

            // Try to read response - should get timeout error
            string? response = await reader.ReadLineAsync();

            // Assert
            Assert.NotNull(response);
            Assert.StartsWith("421", response); // Should be 421 Command timeout
            Assert.Contains("Command timeout", response, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task CommandTimeout_MultipleTimeouts_ShouldRespectMaxRetryCount()
        {
            // Arrange
            int port = TestHelper.GetAvailablePort();
            TimeSpan commandTimeout = TimeSpan.FromSeconds(1); // Very short timeout
            int maxRetryCount = 2;

            _server = new SmtpServerBuilder()
                .Port(port)
                .CommandTimeout(commandTimeout)
                .MaxRetryCount(maxRetryCount)
                .Build();

            await _server.StartAsync();

            // Act
            using TcpClient client = new();
            await client.ConnectAsync("127.0.0.1", port);

            using NetworkStream stream = client.GetStream();
            using StreamReader reader = new(stream);
            using StreamWriter writer = new(stream) { AutoFlush = true };

            // Read greeting
            string? greeting = await reader.ReadLineAsync();
            Assert.NotNull(greeting);

            // Cause first timeout
            await writer.WriteAsync("EHLO ");
            await writer.FlushAsync();
            await Task.Delay(commandTimeout.Add(TimeSpan.FromMilliseconds(500)));
            string? response1 = await reader.ReadLineAsync();
            Assert.NotNull(response1);
            Assert.StartsWith("421", response1);

            // Cause second timeout (should close connection)
            await writer.WriteAsync("MAIL ");
            await writer.FlushAsync();
            await Task.Delay(commandTimeout.Add(TimeSpan.FromMilliseconds(500)));

            string? response2 = await reader.ReadLineAsync();

            // Assert
            Assert.NotNull(response2);
            Assert.StartsWith("421", response2);
            Assert.Contains("Too many errors", response2, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region Data Timeout Tests

        [Fact]
        public async Task DataTimeout_WhenExceeded_ShouldReturnTimeoutError()
        {
            // Arrange
            int port = TestHelper.GetAvailablePort();
            TimeSpan dataTimeout = TimeSpan.FromSeconds(2); // Short timeout for testing

            _server = new SmtpServerBuilder()
                .Port(port)
                .DataTimeout(dataTimeout)
                .Build();

            await _server.StartAsync();

            // Act
            using TcpClient client = new();
            await client.ConnectAsync("127.0.0.1", port);

            using NetworkStream stream = client.GetStream();
            using StreamReader reader = new(stream);
            using StreamWriter writer = new(stream) { AutoFlush = true };

            // Complete handshake
            string? greeting = await reader.ReadLineAsync();
            Assert.NotNull(greeting);

            await writer.WriteLineAsync("EHLO test.com");
            string? line;
            do
            {
                line = await reader.ReadLineAsync();
            } while (line != null && !line.StartsWith("250 "));

            // Send MAIL FROM
            await writer.WriteLineAsync("MAIL FROM:<sender@test.com>");
            string? mailResponse = await reader.ReadLineAsync();
            Assert.NotNull(mailResponse);
            Assert.StartsWith("250", mailResponse);

            // Send RCPT TO
            await writer.WriteLineAsync("RCPT TO:<recipient@test.com>");
            string? rcptResponse = await reader.ReadLineAsync();
            Assert.NotNull(rcptResponse);
            Assert.StartsWith("250", rcptResponse);

            // Send DATA command
            await writer.WriteLineAsync("DATA");
            string? dataResponse = await reader.ReadLineAsync();
            Assert.NotNull(dataResponse);
            Assert.StartsWith("354", dataResponse);

            // Start sending data but don't complete it
            await writer.WriteLineAsync("Subject: Test");
            await writer.WriteLineAsync("");
            await writer.WriteLineAsync("This is a test message");
            // Don't send the terminating "."

            // Wait for timeout
            await Task.Delay(dataTimeout.Add(TimeSpan.FromSeconds(1)));

            // Read timeout response
            string? timeoutResponse = await reader.ReadLineAsync();

            // Assert
            Assert.NotNull(timeoutResponse);
            Assert.StartsWith("421", timeoutResponse);
            Assert.Contains("Data transfer timeout", timeoutResponse, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region Normal Operation Tests

        [Fact]
        public async Task NormalOperation_WithinTimeouts_ShouldCompleteSuccessfully()
        {
            // Arrange
            int port = TestHelper.GetAvailablePort();
            bool messageReceived = false;

            _server = new SmtpServerBuilder()
                .Port(port)
                .ConnectionTimeout(TimeSpan.FromSeconds(10))
                .CommandTimeout(TimeSpan.FromSeconds(5))
                .DataTimeout(TimeSpan.FromSeconds(8))
                .Build();

            _server.MessageReceived += (sender, e) =>
            {
                messageReceived = true;
            };

            await _server.StartAsync();

            // Act
            using TcpClient client = new();
            await client.ConnectAsync("127.0.0.1", port);

            using NetworkStream stream = client.GetStream();
            using StreamReader reader = new(stream);
            using StreamWriter writer = new(stream) { AutoFlush = true };

            // Complete full SMTP transaction
            string? greeting = await reader.ReadLineAsync();
            Assert.NotNull(greeting);

            await writer.WriteLineAsync("EHLO test.com");
            string? line;
            do
            {
                line = await reader.ReadLineAsync();
            } while (line != null && !line.StartsWith("250 "));

            await writer.WriteLineAsync("MAIL FROM:<sender@test.com>");
            string? mailResponse = await reader.ReadLineAsync();
            Assert.NotNull(mailResponse);
            Assert.StartsWith("250", mailResponse);

            await writer.WriteLineAsync("RCPT TO:<recipient@test.com>");
            string? rcptResponse = await reader.ReadLineAsync();
            Assert.NotNull(rcptResponse);
            Assert.StartsWith("250", rcptResponse);

            await writer.WriteLineAsync("DATA");
            string? dataResponse = await reader.ReadLineAsync();
            Assert.NotNull(dataResponse);
            Assert.StartsWith("354", dataResponse);

            // Send complete message
            await writer.WriteLineAsync("Subject: Test");
            await writer.WriteLineAsync("");
            await writer.WriteLineAsync("This is a test message");
            await writer.WriteLineAsync(".");

            string? acceptResponse = await reader.ReadLineAsync();
            Assert.NotNull(acceptResponse);
            Assert.StartsWith("250", acceptResponse);

            await writer.WriteLineAsync("QUIT");
            string? quitResponse = await reader.ReadLineAsync();
            Assert.NotNull(quitResponse);
            Assert.StartsWith("221", quitResponse);

            // Assert
            await Task.Delay(100); // Give time for event to fire
            Assert.True(messageReceived, "Message should have been received successfully");
        }

        [Fact]
        public async Task ResetErrorCount_AfterSuccessfulCommand_ShouldAllowMoreErrors()
        {
            // Arrange
            int port = TestHelper.GetAvailablePort();

            _server = new SmtpServerBuilder()
                .Port(port)
                .MaxRetryCount(3)  // Allow 3 errors
                .Build();

            await _server.StartAsync();

            // Act
            using TcpClient client = new();
            await client.ConnectAsync("127.0.0.1", port);

            using NetworkStream stream = client.GetStream();
            using StreamReader reader = new(stream);
            using StreamWriter writer = new(stream) { AutoFlush = true };

            // Read greeting
            string? greeting = await reader.ReadLineAsync();
            Assert.NotNull(greeting);

            // Cause first error with invalid command
            await writer.WriteLineAsync("INVALID_COMMAND");
            string? response1 = await reader.ReadLineAsync();
            Assert.NotNull(response1);
            Assert.StartsWith("502", response1); // Command not implemented

            // Cause second error
            await writer.WriteLineAsync("ANOTHER_INVALID");
            string? response2 = await reader.ReadLineAsync();
            Assert.NotNull(response2);
            Assert.StartsWith("502", response2); // Still not closing

            // Send successful command to reset error count
            await writer.WriteLineAsync("NOOP");
            string? noopResponse = await reader.ReadLineAsync();
            Assert.NotNull(noopResponse);
            Assert.StartsWith("250", noopResponse);

            // Now error count should be reset, we can have more errors
            await writer.WriteLineAsync("INVALID_AGAIN");
            string? response3 = await reader.ReadLineAsync();
            Assert.NotNull(response3);
            Assert.StartsWith("502", response3);

            await writer.WriteLineAsync("YET_ANOTHER_INVALID");
            string? response4 = await reader.ReadLineAsync();
            Assert.NotNull(response4);
            Assert.StartsWith("502", response4);

            // Third error after reset should close
            await writer.WriteLineAsync("FINAL_INVALID");
            string? response5 = await reader.ReadLineAsync();

            // Assert
            Assert.NotNull(response5);
            Assert.StartsWith("421", response5);
            Assert.Contains("Too many errors", response5, StringComparison.OrdinalIgnoreCase);
        }

        #endregion
    }
}