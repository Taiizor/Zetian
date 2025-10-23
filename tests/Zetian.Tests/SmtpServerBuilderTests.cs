using FluentAssertions;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Xunit;
using Zetian.Server;
using Zetian.Storage;

namespace Zetian.Tests
{
    public class SmtpServerBuilderTests
    {
        [Fact]
        public void Build_DefaultConfiguration_ShouldCreateServer()
        {
            // Arrange
            SmtpServerBuilder builder = new();

            // Act
            SmtpServer server = builder.Build();

            // Assert
            server.Should().NotBeNull();
            server.Configuration.Should().NotBeNull();
            server.Configuration.Port.Should().Be(25);
            server.Configuration.IpAddress.Should().Be(IPAddress.Any);
        }

        [Fact]
        public void Port_ShouldSetPortCorrectly()
        {
            // Arrange
            SmtpServerBuilder builder = new();

            // Act
            SmtpServer server = builder.Port(587).Build();

            // Assert
            server.Configuration.Port.Should().Be(587);
        }

        [Fact]
        public void BindTo_IPAddress_ShouldSetCorrectly()
        {
            // Arrange
            SmtpServerBuilder builder = new();
            IPAddress ip = IPAddress.Loopback;

            // Act
            SmtpServer server = builder.BindTo(ip).Build();

            // Assert
            server.Configuration.IpAddress.Should().Be(ip);
        }

        [Fact]
        public void BindTo_StringIPAddress_ShouldParseAndSet()
        {
            // Arrange
            SmtpServerBuilder builder = new();

            // Act
            SmtpServer server = builder.BindTo("127.0.0.1").Build();

            // Assert
            server.Configuration.IpAddress.Should().Be(IPAddress.Loopback);
        }

        [Fact]
        public void ServerName_ShouldSetCorrectly()
        {
            // Arrange
            SmtpServerBuilder builder = new();
            string name = "Test SMTP Server";

            // Act
            SmtpServer server = builder.ServerName(name).Build();

            // Assert
            server.Configuration.ServerName.Should().Be(name);
        }

        [Fact]
        public void MaxMessageSize_ShouldSetCorrectly()
        {
            // Arrange
            SmtpServerBuilder builder = new();
            long size = 1024L * 1024L * 50L; // 50MB

            // Act
            SmtpServer server = builder.MaxMessageSize(size).Build();

            // Assert
            server.Configuration.MaxMessageSize.Should().Be(size);
        }

        [Fact]
        public void MaxMessageSizeMB_ShouldConvertToBytes()
        {
            // Arrange
            SmtpServerBuilder builder = new();

            // Act
            SmtpServer server = builder.MaxMessageSizeMB(10).Build();

            // Assert
            server.Configuration.MaxMessageSize.Should().Be(10 * 1024L * 1024L);
        }

        [Fact]
        public void MaxRecipients_ShouldSetCorrectly()
        {
            // Arrange
            SmtpServerBuilder builder = new();

            // Act
            SmtpServer server = builder.MaxRecipients(50).Build();

            // Assert
            server.Configuration.MaxRecipients.Should().Be(50);
        }

        [Fact]
        public void MaxConnections_ShouldSetCorrectly()
        {
            // Arrange
            SmtpServerBuilder builder = new();

            // Act
            SmtpServer server = builder.MaxConnections(200).Build();

            // Assert
            server.Configuration.MaxConnections.Should().Be(200);
        }

        [Fact]
        public void MaxConnectionsPerIP_ShouldSetCorrectly()
        {
            // Arrange
            SmtpServerBuilder builder = new();

            // Act
            SmtpServer server = builder.MaxConnectionsPerIP(5).Build();

            // Assert
            server.Configuration.MaxConnectionsPerIp.Should().Be(5);
        }

        [Fact]
        public void EnablePipelining_ShouldSetCorrectly()
        {
            // Arrange
            SmtpServerBuilder builder = new();

            // Act
            SmtpServer server = builder.EnablePipelining(false).Build();

            // Assert
            server.Configuration.EnablePipelining.Should().BeFalse();
        }

        [Fact]
        public void Enable8BitMime_ShouldSetCorrectly()
        {
            // Arrange
            SmtpServerBuilder builder = new();

            // Act
            SmtpServer server = builder.Enable8BitMime(false).Build();

            // Assert
            server.Configuration.Enable8BitMime.Should().BeFalse();
        }

        [Fact]
        public void RequireAuthentication_ShouldSetCorrectly()
        {
            // Arrange
            SmtpServerBuilder builder = new();

            // Act
            SmtpServer server = builder
                .RequireAuthentication()
                .AllowPlainTextAuthentication() // Need to allow plain text when not using TLS
                .Build();

            // Assert
            server.Configuration.RequireAuthentication.Should().BeTrue();
        }

        [Fact]
        public void RequireSecureConnection_ShouldSetCorrectly()
        {
            // Arrange
            SmtpServerBuilder builder = new();
            X509Certificate2 cert = CreateTestCertificate();

            // Act
            SmtpServer server = builder
                .RequireSecureConnection()
                .Certificate(cert) // Need certificate when requiring secure connection
                .Build();

            // Assert
            server.Configuration.RequireSecureConnection.Should().BeTrue();
        }

        [Fact]
        public void AllowPlainTextAuthentication_ShouldSetCorrectly()
        {
            // Arrange
            SmtpServerBuilder builder = new();

            // Act
            SmtpServer server = builder.AllowPlainTextAuthentication().Build();

            // Assert
            server.Configuration.AllowPlainTextAuthentication.Should().BeTrue();
        }

        [Fact]
        public void AddAuthenticationMechanism_ShouldAddToList()
        {
            // Arrange
            SmtpServerBuilder builder = new();

            // Act
            SmtpServer server = builder
                .AddAuthenticationMechanism("PLAIN")
                .AddAuthenticationMechanism("LOGIN")
                .Build();

            // Assert
            server.Configuration.AuthenticationMechanisms.Should().Contain("PLAIN");
            server.Configuration.AuthenticationMechanisms.Should().Contain("LOGIN");
        }

        [Fact]
        public void AddAuthenticationMechanism_Duplicate_ShouldNotAddTwice()
        {
            // Arrange
            SmtpServerBuilder builder = new();

            // Act
            SmtpServer server = builder
                .AddAuthenticationMechanism("PLAIN")
                .AddAuthenticationMechanism("PLAIN")
                .Build();

            // Assert
            server.Configuration.AuthenticationMechanisms
                .Count(m => m == "PLAIN").Should().Be(1);
        }

        [Fact]
        public void ConnectionTimeout_ShouldSetCorrectly()
        {
            // Arrange
            SmtpServerBuilder builder = new();
            TimeSpan timeout = TimeSpan.FromMinutes(10);

            // Act
            SmtpServer server = builder.ConnectionTimeout(timeout).Build();

            // Assert
            server.Configuration.ConnectionTimeout.Should().Be(timeout);
        }

        [Fact]
        public void CommandTimeout_ShouldSetCorrectly()
        {
            // Arrange
            SmtpServerBuilder builder = new();
            TimeSpan timeout = TimeSpan.FromSeconds(30);

            // Act
            SmtpServer server = builder.CommandTimeout(timeout).Build();

            // Assert
            server.Configuration.CommandTimeout.Should().Be(timeout);
        }

        [Fact]
        public void DataTimeout_ShouldSetCorrectly()
        {
            // Arrange
            SmtpServerBuilder builder = new();
            TimeSpan timeout = TimeSpan.FromMinutes(5);

            // Act
            SmtpServer server = builder.DataTimeout(timeout).Build();

            // Assert
            server.Configuration.DataTimeout.Should().Be(timeout);
        }

        [Fact]
        public void EnableVerboseLogging_ShouldSetCorrectly()
        {
            // Arrange
            SmtpServerBuilder builder = new();

            // Act
            SmtpServer server = builder.EnableVerboseLogging().Build();

            // Assert
            server.Configuration.EnableVerboseLogging.Should().BeTrue();
        }

        [Fact]
        public void Banner_ShouldSetCorrectly()
        {
            // Arrange
            SmtpServerBuilder builder = new();
            string banner = "Custom Banner";

            // Act
            SmtpServer server = builder.Banner(banner).Build();

            // Assert
            server.Configuration.Banner.Should().Be(banner);
        }

        [Fact]
        public void Greeting_ShouldSetCorrectly()
        {
            // Arrange
            SmtpServerBuilder builder = new();
            string greeting = "Welcome to SMTP";

            // Act
            SmtpServer server = builder.Greeting(greeting).Build();

            // Assert
            server.Configuration.Greeting.Should().Be(greeting);
        }

        [Fact]
        public void BufferSize_ShouldSetBothBuffers()
        {
            // Arrange
            SmtpServerBuilder builder = new();

            // Act
            SmtpServer server = builder.BufferSize(8192, 4096).Build();

            // Assert
            server.Configuration.ReadBufferSize.Should().Be(8192);
            server.Configuration.WriteBufferSize.Should().Be(4096);
        }

        [Fact]
        public void CreateBasic_ShouldCreateBasicServer()
        {
            // Act
            SmtpServer server = SmtpServerBuilder.CreateBasic();

            // Assert
            server.Should().NotBeNull();
            server.Configuration.Port.Should().Be(25);
            server.Configuration.RequireAuthentication.Should().BeFalse();
        }

        [Fact]
        public void FluentChaining_ShouldWorkCorrectly()
        {
            // Act
            SmtpServer server = new SmtpServerBuilder()
                .Port(587)
                .ServerName("Test Server")
                .MaxMessageSizeMB(25)
                .MaxRecipients(100)
                .RequireAuthentication()
                .AllowPlainTextAuthentication() // Need to allow plain text
                .EnablePipelining()
                .Enable8BitMime()
                .Build();

            // Assert
            server.Configuration.Port.Should().Be(587);
            server.Configuration.ServerName.Should().Be("Test Server");
            server.Configuration.MaxMessageSize.Should().Be(25 * 1024 * 1024);
            server.Configuration.MaxRecipients.Should().Be(100);
            server.Configuration.RequireAuthentication.Should().BeTrue();
            server.Configuration.EnablePipelining.Should().BeTrue();
            server.Configuration.Enable8BitMime.Should().BeTrue();
        }

        [Fact]
        public void EnableSmtpUtf8_ShouldSetCorrectly()
        {
            // Arrange
            SmtpServerBuilder builder = new();

            // Act
            SmtpServer server = builder.EnableSmtpUtf8().Build();

            // Assert
            server.Configuration.EnableSmtpUtf8.Should().BeTrue();
        }

        [Fact]
        public void WithFileMessageStore_ShouldCreateFileStore()
        {
            // Arrange
            SmtpServerBuilder builder = new();
            string directory = Path.Combine(Path.GetTempPath(), "test_store");

            // Act
            SmtpServer server = builder.WithFileMessageStore(directory, true).Build();

            // Assert
            server.Configuration.MessageStore.Should().NotBeNull();
            server.Configuration.MessageStore.Should().BeOfType<FileMessageStore>();
        }

        [Fact]
        public void MessageStore_ShouldSetCustomStore()
        {
            // Arrange
            SmtpServerBuilder builder = new();
            NullMessageStore customStore = NullMessageStore.Instance;

            // Act
            SmtpServer server = builder.MessageStore(customStore).Build();

            // Assert
            server.Configuration.MessageStore.Should().Be(customStore);
        }

        [Fact]
        public void WithSenderDomainWhitelist_ShouldCreateFilter()
        {
            // Arrange
            SmtpServerBuilder builder = new();

            // Act
            SmtpServer server = builder
                .WithSenderDomainWhitelist("trusted.com", "example.com")
                .Build();

            // Assert
            server.Configuration.MailboxFilter.Should().NotBeNull();
            server.Configuration.MailboxFilter.Should().BeOfType<DomainMailboxFilter>();
        }

        [Fact]
        public void WithSenderDomainBlacklist_ShouldCreateFilter()
        {
            // Arrange
            SmtpServerBuilder builder = new();

            // Act
            SmtpServer server = builder
                .WithSenderDomainBlacklist("spam.com", "junk.org")
                .Build();

            // Assert
            server.Configuration.MailboxFilter.Should().NotBeNull();
            server.Configuration.MailboxFilter.Should().BeOfType<DomainMailboxFilter>();
        }

        [Fact]
        public void WithRecipientDomainWhitelist_ShouldCreateFilter()
        {
            // Arrange
            SmtpServerBuilder builder = new();

            // Act
            SmtpServer server = builder
                .WithRecipientDomainWhitelist("mydomain.com", "example.com")
                .Build();

            // Assert
            server.Configuration.MailboxFilter.Should().NotBeNull();
            server.Configuration.MailboxFilter.Should().BeOfType<DomainMailboxFilter>();
        }

        [Fact]
        public void WithRecipientDomainBlacklist_ShouldCreateFilter()
        {
            // Arrange
            SmtpServerBuilder builder = new();

            // Act
            SmtpServer server = builder
                .WithRecipientDomainBlacklist("bad.com", "evil.org")
                .Build();

            // Assert
            server.Configuration.MailboxFilter.Should().NotBeNull();
            server.Configuration.MailboxFilter.Should().BeOfType<DomainMailboxFilter>();
        }

        [Fact]
        public void MailboxFilter_ShouldSetCustomFilter()
        {
            // Arrange
            SmtpServerBuilder builder = new();
            AcceptAllMailboxFilter customFilter = AcceptAllMailboxFilter.Instance;

            // Act
            SmtpServer server = builder.MailboxFilter(customFilter).Build();

            // Assert
            server.Configuration.MailboxFilter.Should().Be(customFilter);
        }

        [Fact]
        public void CombinedFiltering_ShouldWork()
        {
            // Arrange & Act
            SmtpServer server = new SmtpServerBuilder()
                .Port(587)
                .EnableSmtpUtf8()
                .WithFileMessageStore(@"C:\temp\mail", true)
                .WithSenderDomainWhitelist("trusted.com")
                .WithSenderDomainBlacklist("spam.com")
                .WithRecipientDomainWhitelist("mydomain.com")
                .Build();

            // Assert
            server.Configuration.Port.Should().Be(587);
            server.Configuration.EnableSmtpUtf8.Should().BeTrue();
            server.Configuration.MessageStore.Should().NotBeNull();
            server.Configuration.MailboxFilter.Should().NotBeNull();
        }

        private static X509Certificate2 CreateTestCertificate()
        {
            using RSA rsa = RSA.Create(2048);
            CertificateRequest req = new(
                "CN=Test",
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            X509Certificate2 cert = req.CreateSelfSigned(
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddYears(1));

#if NET9_0_OR_GREATER
            byte[] pfxData = cert.Export(X509ContentType.Pfx);
            return X509CertificateLoader.LoadPkcs12(pfxData, null);
#else
            return new X509Certificate2(cert.Export(X509ContentType.Pfx));
#endif
        }
    }
}