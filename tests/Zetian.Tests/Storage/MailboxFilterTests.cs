using System.Net;
using System.Security.Cryptography.X509Certificates;
using Xunit;
using Zetian.Abstractions;
using Zetian.Enums;
using Zetian.Storage;

namespace Zetian.Tests.Storage
{
    public class MailboxFilterTests
    {
        [Fact]
        public async Task AcceptAllMailboxFilter_AcceptsAllAddresses()
        {
            // Arrange
            AcceptAllMailboxFilter filter = AcceptAllMailboxFilter.Instance;
            MockSession session = new();

            // Act & Assert
            Assert.True(await filter.CanAcceptFromAsync(session, "any@domain.com", 1024));
            Assert.True(await filter.CanAcceptFromAsync(session, "spam@spam.com", 1024));
            Assert.True(await filter.CanDeliverToAsync(session, "any@domain.com", "sender@test.com"));
            Assert.True(await filter.CanDeliverToAsync(session, "spam@spam.com", "sender@test.com"));
        }

        [Theory]
        [InlineData("user@allowed.com", true)]
        [InlineData("user@blocked.com", false)]
        [InlineData("user@other.com", true)] // Default allow
        public async Task DomainMailboxFilter_FiltersFromDomains(string from, bool expected)
        {
            // Arrange
            DomainMailboxFilter filter = new(allowByDefault: true);
            filter.AllowFromDomains("allowed.com");
            filter.BlockFromDomains("blocked.com");
            MockSession session = new();

            // Act
            bool result = await filter.CanAcceptFromAsync(session, from, 1024);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("user@allowed.com", true)]
        [InlineData("user@blocked.com", false)]
        [InlineData("user@other.com", true)]
        public async Task DomainMailboxFilter_FiltersToDomains(string to, bool expected)
        {
            // Arrange
            DomainMailboxFilter filter = new(allowByDefault: true);
            filter.AllowToDomains("allowed.com");
            filter.BlockToDomains("blocked.com");
            MockSession session = new();

            // Act
            bool result = await filter.CanDeliverToAsync(session, to, "sender@test.com");

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task DomainMailboxFilter_AllowByDefault_False_BlocksUnknownDomains()
        {
            // Arrange
            DomainMailboxFilter filter = new(allowByDefault: false);
            filter.AllowFromDomains("trusted.com");
            MockSession session = new();

            // Act & Assert
            Assert.True(await filter.CanAcceptFromAsync(session, "user@trusted.com", 1024));
            Assert.False(await filter.CanAcceptFromAsync(session, "user@unknown.com", 1024));
        }

        [Fact]
        public async Task CompositeMailboxFilter_All_RequiresAllFiltersToPass()
        {
            // Arrange
            DomainMailboxFilter filter1 = new DomainMailboxFilter(true).AllowFromDomains("allowed.com");
            DomainMailboxFilter filter2 = new DomainMailboxFilter(true).BlockFromDomains("spam.com");
            CompositeMailboxFilter composite = new(CompositeMode.All, filter1, filter2);
            MockSession session = new();

            // Act & Assert
            Assert.True(await composite.CanAcceptFromAsync(session, "user@allowed.com", 1024));
            Assert.False(await composite.CanAcceptFromAsync(session, "user@spam.com", 1024));
            Assert.False(await composite.CanAcceptFromAsync(session, "user@other.com", 1024)); // filter1 has whitelist
        }

        [Fact]
        public async Task CompositeMailboxFilter_Any_RequiresOneFilterToPass()
        {
            // Arrange
            DomainMailboxFilter filter1 = new DomainMailboxFilter(false).AllowFromDomains("domain1.com");
            DomainMailboxFilter filter2 = new DomainMailboxFilter(false).AllowFromDomains("domain2.com");
            CompositeMailboxFilter composite = new(CompositeMode.Any, filter1, filter2);
            MockSession session = new();

            // Act & Assert
            Assert.True(await composite.CanAcceptFromAsync(session, "user@domain1.com", 1024));
            Assert.True(await composite.CanAcceptFromAsync(session, "user@domain2.com", 1024));
            Assert.False(await composite.CanAcceptFromAsync(session, "user@other.com", 1024));
        }

        [Fact]
        public async Task CompositeMailboxFilter_AddRemoveFilters()
        {
            // Arrange
            CompositeMailboxFilter composite = new(CompositeMode.All);
            AcceptAllMailboxFilter filter1 = AcceptAllMailboxFilter.Instance;
            DomainMailboxFilter filter2 = new(false);
            MockSession session = new();

            // Act & Assert - Empty composite accepts all
            Assert.True(await composite.CanAcceptFromAsync(session, "any@domain.com", 1024));

            // Add filter1 (accepts all)
            composite.AddFilter(filter1);
            Assert.True(await composite.CanAcceptFromAsync(session, "any@domain.com", 1024));

            // Add filter2 (rejects all by default)
            composite.AddFilter(filter2);
            Assert.False(await composite.CanAcceptFromAsync(session, "any@domain.com", 1024));

            // Remove filter2
            composite.RemoveFilter(filter2);
            Assert.True(await composite.CanAcceptFromAsync(session, "any@domain.com", 1024));
        }

        [Fact]
        public async Task DomainMailboxFilter_HandlesNullSender()
        {
            // Arrange
            DomainMailboxFilter filter = new(allowByDefault: false);
            filter.BlockFromDomains("blocked.com");
            MockSession session = new();

            // Act - null sender (bounce messages)
            bool result = await filter.CanAcceptFromAsync(session, string.Empty, 1024);

            // Assert - should accept null sender
            Assert.True(result);
        }

        [Fact]
        public async Task DomainMailboxFilter_CaseInsensitive()
        {
            // Arrange
            DomainMailboxFilter filter = new(allowByDefault: false);
            filter.AllowFromDomains("ALLOWED.COM");
            MockSession session = new();

            // Act & Assert
            Assert.True(await filter.CanAcceptFromAsync(session, "user@allowed.com", 1024));
            Assert.True(await filter.CanAcceptFromAsync(session, "user@ALLOWED.COM", 1024));
            Assert.True(await filter.CanAcceptFromAsync(session, "user@Allowed.Com", 1024));
        }

        private class MockSession : ISmtpSession
        {
            public string Id => "test_session";
            public EndPoint RemoteEndPoint => new IPEndPoint(IPAddress.Loopback, 25);
            public EndPoint LocalEndPoint => new IPEndPoint(IPAddress.Any, 25);
            public bool IsSecure => false;
            public bool IsAuthenticated => false;
            public string? AuthenticatedIdentity => null;
            public string? ClientDomain => "test.local";
            public DateTime StartTime => DateTime.UtcNow;
            public IDictionary<string, object> Properties => new Dictionary<string, object>();
            public X509Certificate2? ClientCertificate => null;
            public int MessageCount => 0;
            public bool PipeliningEnabled { get; set; }
            public bool EightBitMimeEnabled { get; set; }
            public bool BinaryMimeEnabled { get; set; }
            public long MaxMessageSize { get; set; } = 10485760;
        }
    }
}