using FluentAssertions;
using Moq;
using System.Net;
using System.Net.Mail;
using Xunit;
using Zetian.Abstractions;
using Zetian.Extensions;
using Zetian.Models;
using Zetian.Models.EventArgs;
using Zetian.Tests.Helpers;
using ErrorEventArgs = Zetian.Models.EventArgs.ErrorEventArgs;

namespace Zetian.Tests
{
    public class SmtpServerExtensionsTests
    {
        private readonly Mock<ISmtpServer> _serverMock;
        private readonly Mock<ISmtpSession> _sessionMock;
        private readonly Mock<ISmtpMessage> _messageMock;

        public SmtpServerExtensionsTests()
        {
            _serverMock = new();
            _sessionMock = new();
            _messageMock = new();

            _sessionMock.Setup(s => s.Properties).Returns(new Dictionary<string, object>());
            _sessionMock.Setup(s => s.RemoteEndPoint).Returns(new IPEndPoint(IPAddress.Loopback, TestHelper.GetAvailablePort()));
        }

        [Fact]
        public void AddMessageFilter_ShouldAttachToMessageReceivedEvent()
        {
            // Arrange
            bool filterCalled = false;
            bool filter(ISmtpMessage msg)
            {
                filterCalled = true;
                return true;
            }

            // Act
            _serverMock.Object.AddMessageFilter(filter);
            _serverMock.Raise(s => s.MessageReceived += null,
                new MessageEventArgs(_messageMock.Object, _sessionMock.Object));

            // Assert
            filterCalled.Should().BeTrue();
        }

        [Fact]
        public void AddMessageFilter_RejectingMessage_ShouldSetCancelAndResponse()
        {
            // Arrange
            static bool filter(ISmtpMessage msg)
            {
                return false;
            }

            MessageEventArgs? capturedArgs = null;
            EventHandler<MessageEventArgs>? messageHandler = null;

            _serverMock.SetupAdd(s => s.MessageReceived += It.IsAny<EventHandler<MessageEventArgs>>())
                .Callback<EventHandler<MessageEventArgs>>(handler => messageHandler = handler);

            // Act
            _serverMock.Object.AddMessageFilter(filter);

            if (messageHandler != null)
            {
                capturedArgs = new MessageEventArgs(_messageMock.Object, _sessionMock.Object);
                messageHandler(null!, capturedArgs);
            }

            // Assert
            capturedArgs.Should().NotBeNull();
            capturedArgs!.Cancel.Should().BeTrue();
            capturedArgs.Response.Code.Should().Be(550);
        }

        [Fact]
        public void AddSpamFilter_ShouldBlockBlacklistedDomains()
        {
            // Arrange
            string[] blacklistedDomains = new[] { "spam.com", "junk.org" };
            MessageEventArgs? capturedArgs = null;
            EventHandler<MessageEventArgs>? messageHandler = null;

            _messageMock.Setup(m => m.From).Returns(new MailAddress("sender@spam.com"));

            _serverMock.SetupAdd(s => s.MessageReceived += It.IsAny<EventHandler<MessageEventArgs>>())
                .Callback<EventHandler<MessageEventArgs>>(handler => messageHandler = handler);

            // Act
            _serverMock.Object.AddSpamFilter(blacklistedDomains);

            if (messageHandler != null)
            {
                capturedArgs = new MessageEventArgs(_messageMock.Object, _sessionMock.Object);
                messageHandler(null!, capturedArgs);
            }

            // Assert
            capturedArgs.Should().NotBeNull();
            capturedArgs!.Cancel.Should().BeTrue();
        }

        [Fact]
        public void AddSpamFilter_ShouldAllowNonBlacklistedDomains()
        {
            // Arrange
            string[] blacklistedDomains = new[] { "spam.com", "junk.org" };
            MessageEventArgs? capturedArgs = null;
            EventHandler<MessageEventArgs>? messageHandler = null;

            _messageMock.Setup(m => m.From).Returns(new MailAddress("sender@legitimate.com"));

            _serverMock.SetupAdd(s => s.MessageReceived += It.IsAny<EventHandler<MessageEventArgs>>())
                .Callback<EventHandler<MessageEventArgs>>(handler => messageHandler = handler);

            // Act
            _serverMock.Object.AddSpamFilter(blacklistedDomains);

            if (messageHandler != null)
            {
                capturedArgs = new MessageEventArgs(_messageMock.Object, _sessionMock.Object);
                messageHandler(null!, capturedArgs);
            }

            // Assert
            capturedArgs.Should().NotBeNull();
            capturedArgs!.Cancel.Should().BeFalse();
        }

        [Fact]
        public void AddSizeFilter_ShouldBlockLargeMessages()
        {
            // Arrange
            long maxSize = 1024L * 1024L; // 1MB
            MessageEventArgs? capturedArgs = null;
            EventHandler<MessageEventArgs>? messageHandler = null;

            _messageMock.Setup(m => m.Size).Returns(2 * 1024 * 1024); // 2MB

            _serverMock.SetupAdd(s => s.MessageReceived += It.IsAny<EventHandler<MessageEventArgs>>())
                .Callback<EventHandler<MessageEventArgs>>(handler => messageHandler = handler);

            // Act
            _serverMock.Object.AddSizeFilter(maxSize);

            if (messageHandler != null)
            {
                capturedArgs = new MessageEventArgs(_messageMock.Object, _sessionMock.Object);
                messageHandler(null!, capturedArgs);
            }

            // Assert
            capturedArgs.Should().NotBeNull();
            capturedArgs!.Cancel.Should().BeTrue();
        }

        [Fact]
        public void AddSizeFilter_ShouldAllowSmallMessages()
        {
            // Arrange
            long maxSize = 10L * 1024L * 1024L; // 10MB
            MessageEventArgs? capturedArgs = null;
            EventHandler<MessageEventArgs>? messageHandler = null;

            _messageMock.Setup(m => m.Size).Returns(1024 * 1024); // 1MB

            _serverMock.SetupAdd(s => s.MessageReceived += It.IsAny<EventHandler<MessageEventArgs>>())
                .Callback<EventHandler<MessageEventArgs>>(handler => messageHandler = handler);

            // Act
            _serverMock.Object.AddSizeFilter(maxSize);

            if (messageHandler != null)
            {
                capturedArgs = new MessageEventArgs(_messageMock.Object, _sessionMock.Object);
                messageHandler(null!, capturedArgs);
            }

            // Assert
            capturedArgs.Should().NotBeNull();
            capturedArgs!.Cancel.Should().BeFalse();
        }

        [Fact]
        public void AddAllowedDomains_ShouldOnlyAcceptSpecifiedDomains()
        {
            // Arrange
            string[] allowedDomains = new[] { "example.com", "test.com" };
            MessageEventArgs? capturedArgs = null;
            EventHandler<MessageEventArgs>? messageHandler = null;

            List<MailAddress> recipients =
            [
                new MailAddress("user@example.com"),
                new MailAddress("admin@invalid.org")
            ];

            _messageMock.Setup(m => m.Recipients).Returns(recipients);

            _serverMock.SetupAdd(s => s.MessageReceived += It.IsAny<EventHandler<MessageEventArgs>>())
                .Callback<EventHandler<MessageEventArgs>>(handler => messageHandler = handler);

            // Act
            _serverMock.Object.AddAllowedDomains(allowedDomains);

            if (messageHandler != null)
            {
                capturedArgs = new MessageEventArgs(_messageMock.Object, _sessionMock.Object);
                messageHandler(null!, capturedArgs);
            }

            // Assert
            capturedArgs.Should().NotBeNull();
            capturedArgs!.Cancel.Should().BeTrue();
            capturedArgs.Response.Message.Should().Contain("Invalid recipients");
        }

        [Fact]
        public void LogMessages_ShouldCallLoggerForEachMessage()
        {
            // Arrange
            bool logged = false;
            void logger(ISmtpMessage msg)
            {
                logged = true;
            }

            // Act
            _serverMock.Object.LogMessages(logger);
            _serverMock.Raise(s => s.MessageReceived += null,
                new MessageEventArgs(_messageMock.Object, _sessionMock.Object));

            // Assert
            logged.Should().BeTrue();
        }

        [Fact]
        public async Task SaveMessagesToDirectory_ShouldSaveMessageToFile()
        {
            // Arrange
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            string messageId = "test-message-id";
            byte[] messageData = new byte[] { 0x01, 0x02, 0x03 };

            _messageMock.Setup(m => m.Id).Returns(messageId);
            _messageMock.Setup(m => m.SaveToFileAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask)
                .Callback<string>(path =>
                {
                    // Create the file to simulate saving
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    File.WriteAllBytes(path, messageData);
                });

            // Act
            _serverMock.Object.SaveMessagesToDirectory(tempDir);
            await Task.Run(() =>
            {
                _serverMock.Raise(s => s.MessageReceived += null,
                    new MessageEventArgs(_messageMock.Object, _sessionMock.Object));
            });

            // Wait a bit for async operation
            await Task.Delay(100);

            // Assert
            Directory.Exists(tempDir).Should().BeTrue();
            string[] files = Directory.GetFiles(tempDir, "*.eml");
            files.Should().NotBeEmpty();

            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void AddRecipientValidation_ShouldValidateRecipients()
        {
            // Arrange
            static bool validator(MailAddress addr)
            {
                return addr.User.StartsWith("valid");
            }

            MessageEventArgs? capturedArgs = null;
            EventHandler<MessageEventArgs>? messageHandler = null;

            List<MailAddress> recipients =
            [
                new MailAddress("valid.user@example.com"),
                new MailAddress("invalid@example.com")
            ];

            _messageMock.Setup(m => m.Recipients).Returns(recipients);

            _serverMock.SetupAdd(s => s.MessageReceived += It.IsAny<EventHandler<MessageEventArgs>>())
                .Callback<EventHandler<MessageEventArgs>>(handler => messageHandler = handler);

            // Act
            _serverMock.Object.AddRecipientValidation(validator);

            if (messageHandler != null)
            {
                capturedArgs = new MessageEventArgs(_messageMock.Object, _sessionMock.Object);
                messageHandler(null!, capturedArgs);
            }

            // Assert
            capturedArgs.Should().NotBeNull();
            capturedArgs!.Cancel.Should().BeTrue();
            capturedArgs.Response.Message.Should().Contain("invalid@example.com");
        }

        [Fact]
        public void AddRateLimiting_WithConfiguration_ShouldCreateAndAttachLimiter()
        {
            // Arrange
            RateLimitConfiguration config = RateLimitConfiguration.PerMinute(10);

            // Act & Assert (should not throw)
            _serverMock.Object.Invoking(s => s.AddRateLimiting(config))
                .Should().NotThrow();
        }

        [Fact]
        public void AddStatistics_ShouldTrackEvents()
        {
            // Arrange
            TestStatisticsCollector collector = new();
            _messageMock.Setup(m => m.Size).Returns(1024);

            // Act
            _serverMock.Object.AddStatistics(collector);

            _serverMock.Raise(s => s.SessionCreated += null,
                new SessionEventArgs(_sessionMock.Object));
            _serverMock.Raise(s => s.MessageReceived += null,
                new MessageEventArgs(_messageMock.Object, _sessionMock.Object));
            _serverMock.Raise(s => s.ErrorOccurred += null,
                new ErrorEventArgs(new Exception("Test"), _sessionMock.Object));

            // Assert
            collector.TotalSessions.Should().Be(1);
            collector.TotalMessages.Should().Be(1);
            collector.TotalErrors.Should().Be(1);
            collector.TotalBytes.Should().Be(1024);
        }

        [Fact]
        public void ExtensionMethods_NullServer_ShouldThrow()
        {
            // Arrange
            ISmtpServer? nullServer = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                nullServer!.AddMessageFilter(msg => true));

            Assert.Throws<ArgumentNullException>(() =>
                nullServer!.AddSpamFilter(new[] { "spam.com" }));

            Assert.Throws<ArgumentNullException>(() =>
                nullServer!.AddSizeFilter(1024));
        }

        private class TestStatisticsCollector : IStatisticsCollector
        {
            public long TotalSessions { get; private set; }
            public long TotalMessages { get; private set; }
            public long TotalErrors { get; private set; }
            public long TotalBytes { get; private set; }

            public void RecordSession()
            {
                TotalSessions++;
            }

            public void RecordMessage(ISmtpMessage message)
            {
                TotalMessages++;
                TotalBytes += message.Size;
            }
            public void RecordError(Exception exception)
            {
                TotalErrors++;
            }
        }
    }
}