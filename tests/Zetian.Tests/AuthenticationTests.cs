using FluentAssertions;
using Moq;
using System.IO.Pipelines;
using System.Text;
using Xunit;
using Zetian.Authentication;
using Zetian.Core;

namespace Zetian.Tests
{
    public class AuthenticationTests
    {
        [Fact]
        public async Task PlainAuthenticator_ValidCredentials_ShouldSucceed()
        {
            // Arrange
            Mock<AuthenticationHandler> handler = new();
            handler.Setup(h => h(It.IsAny<string>(), It.IsAny<string>()))
                   .ReturnsAsync(AuthenticationResult.Succeed("testuser"));

            PlainAuthenticator authenticator = new(handler.Object);
            ISmtpSession session = Mock.Of<ISmtpSession>();

            // PLAIN format: [authzid]\0authcid\0password
            string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes("\0testuser\0password"));

            MemoryStream stream = new();
            PipeReader reader = PipeReader.Create(stream);
            PipeWriter writer = PipeWriter.Create(stream);

            // Act
            AuthenticationResult result = await authenticator.AuthenticateAsync(
                session, credentials, reader, writer, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Identity.Should().Be("testuser");
        }

        [Fact]
        public async Task PlainAuthenticator_InvalidBase64_ShouldFail()
        {
            // Arrange
            PlainAuthenticator authenticator = new();
            ISmtpSession session = Mock.Of<ISmtpSession>();
            string invalidBase64 = "not-valid-base64!@#";

            MemoryStream stream = new();
            PipeReader reader = PipeReader.Create(stream);
            PipeWriter writer = PipeWriter.Create(stream);

            // Act
            AuthenticationResult result = await authenticator.AuthenticateAsync(
                session, invalidBase64, reader, writer, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Authentication error");
        }

        [Fact]
        public async Task PlainAuthenticator_EmptyResponse_ShouldRequestCredentials()
        {
            // Arrange
            PlainAuthenticator authenticator = new();
            ISmtpSession session = Mock.Of<ISmtpSession>();

            string responseData = Convert.ToBase64String(Encoding.ASCII.GetBytes("\0user\0pass"));
            MemoryStream inputStream = new(Encoding.ASCII.GetBytes(responseData + "\r\n"));
            MemoryStream outputStream = new();

            PipeReader reader = PipeReader.Create(inputStream);
            PipeWriter writer = PipeWriter.Create(outputStream);

            // Act
            AuthenticationResult result = await authenticator.AuthenticateAsync(
                session, null, reader, writer, CancellationToken.None);

            // Assert
            outputStream.Position = 0;
            using StreamReader outputReader = new(outputStream);
            string output = outputReader.ReadToEnd();
            output.Should().Contain("334");
        }

        [Fact]
        public async Task PlainAuthenticator_CancelledAuthentication_ShouldFail()
        {
            // Arrange
            PlainAuthenticator authenticator = new();
            ISmtpSession session = Mock.Of<ISmtpSession>();

            MemoryStream inputStream = new(Encoding.ASCII.GetBytes("*\r\n"));
            MemoryStream outputStream = new();

            PipeReader reader = PipeReader.Create(inputStream);
            PipeWriter writer = PipeWriter.Create(outputStream);

            // Act
            AuthenticationResult result = await authenticator.AuthenticateAsync(
                session, null, reader, writer, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Authentication cancelled");
        }

        [Fact]
        public async Task LoginAuthenticator_ValidCredentials_ShouldSucceed()
        {
            // Arrange
            Mock<AuthenticationHandler> handler = new();
            handler.Setup(h => h("testuser", "password"))
                   .ReturnsAsync(AuthenticationResult.Succeed("testuser"));

            LoginAuthenticator authenticator = new(handler.Object);
            ISmtpSession session = Mock.Of<ISmtpSession>();

            string username = Convert.ToBase64String(Encoding.ASCII.GetBytes("testuser"));
            string password = Convert.ToBase64String(Encoding.ASCII.GetBytes("password"));

            MemoryStream inputStream = new(
                Encoding.ASCII.GetBytes($"{username}\r\n{password}\r\n"));
            MemoryStream outputStream = new();

            PipeReader reader = PipeReader.Create(inputStream);
            PipeWriter writer = PipeWriter.Create(outputStream);

            // Act
            AuthenticationResult result = await authenticator.AuthenticateAsync(
                session, null, reader, writer, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Identity.Should().Be("testuser");
        }

        [Fact]
        public async Task LoginAuthenticator_InvalidBase64Username_ShouldFail()
        {
            // Arrange
            LoginAuthenticator authenticator = new();
            ISmtpSession session = Mock.Of<ISmtpSession>();

            MemoryStream inputStream = new(
                Encoding.ASCII.GetBytes("invalid!@#\r\n"));
            MemoryStream outputStream = new();

            PipeReader reader = PipeReader.Create(inputStream);
            PipeWriter writer = PipeWriter.Create(outputStream);

            // Act
            AuthenticationResult result = await authenticator.AuthenticateAsync(
                session, null, reader, writer, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Invalid username encoding");
        }

        [Fact]
        public void AuthenticationResult_Succeed_ShouldCreateSuccessResult()
        {
            // Act
            AuthenticationResult result = AuthenticationResult.Succeed("user123");

            // Assert
            result.Success.Should().BeTrue();
            result.Identity.Should().Be("user123");
            result.ErrorMessage.Should().BeNull();
        }

        [Fact]
        public void AuthenticationResult_Fail_ShouldCreateFailureResult()
        {
            // Act
            AuthenticationResult result = AuthenticationResult.Fail("Invalid credentials");

            // Assert
            result.Success.Should().BeFalse();
            result.Identity.Should().BeNull();
            result.ErrorMessage.Should().Be("Invalid credentials");
        }

        [Fact]
        public void AuthenticatorFactory_RegisterAndCreate_ShouldWork()
        {
            // Arrange
            Mock<IAuthenticator> customAuthenticator = new();
            customAuthenticator.Setup(a => a.Mechanism).Returns("CUSTOM");

            // Act
            AuthenticatorFactory.Register("CUSTOM", () => customAuthenticator.Object);
            IAuthenticator? created = AuthenticatorFactory.Create("CUSTOM");

            // Assert
            created.Should().NotBeNull();
            created!.Mechanism.Should().Be("CUSTOM");

            // Cleanup
            AuthenticatorFactory.Reset();
        }

        [Fact]
        public void AuthenticatorFactory_CreateUnregistered_ShouldReturnNull()
        {
            // Act
            IAuthenticator? created = AuthenticatorFactory.Create("NONEXISTENT");

            // Assert
            created.Should().BeNull();
        }

        [Fact]
        public void AuthenticatorFactory_GetMechanisms_ShouldReturnRegistered()
        {
            // Arrange
            AuthenticatorFactory.Reset();

            // Act
            System.Collections.Generic.IEnumerable<string> mechanisms = AuthenticatorFactory.GetMechanisms();

            // Assert
            mechanisms.Should().Contain("PLAIN");
            mechanisms.Should().Contain("LOGIN");
        }

        [Fact]
        public void AuthenticatorFactory_SetDefaultHandler_ShouldWork()
        {
            // Arrange
            static async Task<AuthenticationResult> handler(string? u, string? p)
            {
                return AuthenticationResult.Succeed(u ?? "default");
            }

            // Act & Assert (should not throw)
            AuthenticatorFactory.SetDefaultHandler(handler);

            // Cleanup
            AuthenticatorFactory.Reset();
        }

        [Fact]
        public void AuthenticatorFactory_Clear_ShouldRemoveAll()
        {
            // Arrange
            AuthenticatorFactory.Reset();
            int initialCount = AuthenticatorFactory.GetMechanisms().Count();

            // Act
            AuthenticatorFactory.Clear();
            int afterClearCount = AuthenticatorFactory.GetMechanisms().Count();

            // Assert
            initialCount.Should().BeGreaterThan(0);
            afterClearCount.Should().Be(0);

            // Cleanup
            AuthenticatorFactory.Reset();
        }
    }
}