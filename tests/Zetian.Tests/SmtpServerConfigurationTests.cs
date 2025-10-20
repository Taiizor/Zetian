using FluentAssertions;
using System.Net;
using System.Security.Authentication;
using Xunit;
using Zetian.Configuration;

namespace Zetian.Tests
{
    public class SmtpServerConfigurationTests
    {
        [Fact]
        public void Constructor_ShouldSetDefaultValues()
        {
            // Act
            SmtpServerConfiguration config = new();

            // Assert
            config.Port.Should().Be(25);
            config.IpAddress.Should().Be(IPAddress.Any);
            config.ServerName.Should().Be("Zetian SMTP Server");
            config.MaxMessageSize.Should().Be(10 * 1024 * 1024);
            config.MaxRecipients.Should().Be(100);
            config.MaxConnections.Should().Be(100);
            config.MaxConnectionsPerIp.Should().Be(10);
            config.ConnectionTimeout.Should().Be(TimeSpan.FromMinutes(5));
            config.CommandTimeout.Should().Be(TimeSpan.FromMinutes(1));
            config.DataTimeout.Should().Be(TimeSpan.FromMinutes(3));
            config.EnablePipelining.Should().BeTrue();
            config.Enable8BitMime.Should().BeTrue();
            config.EnableBinaryMime.Should().BeFalse();
            config.EnableChunking.Should().BeFalse();
            config.EnableSizeExtension.Should().BeTrue();
            config.RequireAuthentication.Should().BeFalse();
            config.RequireSecureConnection.Should().BeFalse();
            config.AllowPlainTextAuthentication.Should().BeFalse();
            config.SslProtocols.Should().Be(SslProtocols.Tls12 | SslProtocols.Tls13);
        }

        [Fact]
        public void Validate_ValidConfiguration_ShouldNotThrow()
        {
            // Arrange
            SmtpServerConfiguration config = new()
            {
                Port = 587,
                MaxMessageSize = 1024,
                MaxRecipients = 10,
                MaxConnections = 50,
                MaxConnectionsPerIp = 5,
                ConnectionTimeout = TimeSpan.FromSeconds(30),
                CommandTimeout = TimeSpan.FromSeconds(10),
                DataTimeout = TimeSpan.FromSeconds(60)
            };

            // Act & Assert
            config.Invoking(c => c.Validate()).Should().NotThrow();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(65536)]
        [InlineData(100000)]
        public void Validate_InvalidPort_ShouldThrow(int port)
        {
            // Arrange
            SmtpServerConfiguration config = new() { Port = port };

            // Act & Assert
            config.Invoking(c => c.Validate())
                .Should().Throw<ArgumentException>()
                .WithMessage("*Port*");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public void Validate_InvalidMaxMessageSize_ShouldThrow(long size)
        {
            // Arrange
            SmtpServerConfiguration config = new() { MaxMessageSize = size };

            // Act & Assert
            config.Invoking(c => c.Validate())
                .Should().Throw<ArgumentException>()
                .WithMessage("*MaxMessageSize*");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Validate_InvalidMaxRecipients_ShouldThrow(int maxRecipients)
        {
            // Arrange
            SmtpServerConfiguration config = new() { MaxRecipients = maxRecipients };

            // Act & Assert
            config.Invoking(c => c.Validate())
                .Should().Throw<ArgumentException>()
                .WithMessage("*MaxRecipients*");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Validate_InvalidMaxConnections_ShouldThrow(int maxConnections)
        {
            // Arrange
            SmtpServerConfiguration config = new() { MaxConnections = maxConnections };

            // Act & Assert
            config.Invoking(c => c.Validate())
                .Should().Throw<ArgumentException>()
                .WithMessage("*MaxConnections*");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Validate_InvalidMaxConnectionsPerIp_ShouldThrow(int maxConnectionsPerIp)
        {
            // Arrange
            SmtpServerConfiguration config = new() { MaxConnectionsPerIp = maxConnectionsPerIp };

            // Act & Assert
            config.Invoking(c => c.Validate())
                .Should().Throw<ArgumentException>()
                .WithMessage("*MaxConnectionsPerIp*");
        }

        [Fact]
        public void Validate_ZeroConnectionTimeout_ShouldThrow()
        {
            // Arrange
            SmtpServerConfiguration config = new()
            {
                ConnectionTimeout = TimeSpan.Zero
            };

            // Act & Assert
            config.Invoking(c => c.Validate())
                .Should().Throw<ArgumentException>()
                .WithMessage("*ConnectionTimeout*");
        }

        [Fact]
        public void Validate_NegativeCommandTimeout_ShouldThrow()
        {
            // Arrange
            SmtpServerConfiguration config = new()
            {
                CommandTimeout = TimeSpan.FromSeconds(-1)
            };

            // Act & Assert
            config.Invoking(c => c.Validate())
                .Should().Throw<ArgumentException>()
                .WithMessage("*CommandTimeout*");
        }

        [Fact]
        public void Validate_ZeroDataTimeout_ShouldThrow()
        {
            // Arrange
            SmtpServerConfiguration config = new()
            {
                DataTimeout = TimeSpan.Zero
            };

            // Act & Assert
            config.Invoking(c => c.Validate())
                .Should().Throw<ArgumentException>()
                .WithMessage("*DataTimeout*");
        }

        [Fact]
        public void Validate_RequireSecureConnectionWithoutCertificate_ShouldThrow()
        {
            // Arrange
            SmtpServerConfiguration config = new()
            {
                RequireSecureConnection = true,
                Certificate = null
            };

            // Act & Assert
            config.Invoking(c => c.Validate())
                .Should().Throw<ArgumentException>()
                .WithMessage("*Certificate*");
        }

        [Fact]
        public void Validate_RequireAuthWithoutSecureAndNoPlainText_ShouldThrow()
        {
            // Arrange
            SmtpServerConfiguration config = new()
            {
                RequireAuthentication = true,
                RequireSecureConnection = false,
                AllowPlainTextAuthentication = false
            };

            // Act & Assert
            config.Invoking(c => c.Validate())
                .Should().Throw<ArgumentException>()
                .WithMessage("*Plain text authentication*");
        }

        [Fact]
        public void Validate_ValidSecureConfiguration_ShouldNotThrow()
        {
            // Arrange
            SmtpServerConfiguration config = new()
            {
                RequireAuthentication = true,
                RequireSecureConnection = false,
                AllowPlainTextAuthentication = true
            };

            // Act & Assert
            config.Invoking(c => c.Validate()).Should().NotThrow();
        }

        [Fact]
        public void AuthenticationMechanisms_ShouldBeModifiable()
        {
            // Arrange
            SmtpServerConfiguration config = new();

            // Act
            config.AuthenticationMechanisms.Add("CRAM-MD5");
            config.AuthenticationMechanisms.Add("DIGEST-MD5");

            // Assert
            config.AuthenticationMechanisms.Should().Contain("PLAIN");
            config.AuthenticationMechanisms.Should().Contain("LOGIN");
            config.AuthenticationMechanisms.Should().Contain("CRAM-MD5");
            config.AuthenticationMechanisms.Should().Contain("DIGEST-MD5");
        }

        [Fact]
        public void BufferSizes_ShouldHaveDefaultValues()
        {
            // Arrange
            SmtpServerConfiguration config = new();

            // Assert
            config.ReadBufferSize.Should().Be(4096);
            config.WriteBufferSize.Should().Be(4096);
            config.UseNagleAlgorithm.Should().BeFalse();
        }

        [Fact]
        public void CustomMessages_CanBeSet()
        {
            // Arrange
            SmtpServerConfiguration config = new();

            // Act
            config.Banner = "Custom Banner";
            config.Greeting = "Custom Greeting";

            // Assert
            config.Banner.Should().Be("Custom Banner");
            config.Greeting.Should().Be("Custom Greeting");
        }
    }
}