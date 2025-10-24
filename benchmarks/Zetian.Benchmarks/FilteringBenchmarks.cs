using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Order;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Zetian.Abstractions;
using Zetian.Enums;
using Zetian.Extensions;
using Zetian.Server;
using Zetian.Storage;

namespace Zetian.Benchmarks
{
    /// <summary>
    /// Benchmarks for comparing Protocol-Level vs Event-Based filtering performance
    /// </summary>
    [RankColumn]
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [SimpleJob(RunStrategy.ColdStart, launchCount: 1, warmupCount: 2, iterationCount: 3)]
    public class FilteringBenchmarks
    {
        private SmtpServer _protocolFilterServer = null!;
        private SmtpServer _eventFilterServer = null!;
        private readonly List<string> _testDomains = ["spam.com", "junk.org", "phishing.net", "malware.org", "badsite.com"];
        private readonly List<string> _goodDomains = ["example.com", "trusted.com", "company.com"];

        [GlobalSetup]
        public void Setup()
        {
            // Server with protocol-level filtering
            _protocolFilterServer = new SmtpServerBuilder()
                .Port(9999)
                .WithSenderDomainBlacklist(_testDomains.ToArray())
                .WithRecipientDomainWhitelist(_goodDomains.ToArray())
                .Build();

            // Server with event-based filtering
            _eventFilterServer = new SmtpServerBuilder()
                .Port(9998)
                .Build();

            _eventFilterServer.AddSpamFilter(_testDomains);
            _eventFilterServer.AddAllowedDomains(_goodDomains.ToArray());
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _protocolFilterServer?.Dispose();
            _eventFilterServer?.Dispose();
        }

        [Benchmark(Description = "Protocol-Level Domain Filter (Early Rejection)")]
        public async Task ProtocolLevelFiltering()
        {
            DomainMailboxFilter filter = new(true);
            filter.BlockFromDomains(_testDomains.ToArray());
            filter.AllowToDomains(_goodDomains.ToArray());

            // Simulate checking 100 email addresses
            for (int i = 0; i < 100; i++)
            {
                string from = i % 2 == 0 ? $"user{i}@spam.com" : $"user{i}@trusted.com";
                string to = i % 3 == 0 ? $"recipient{i}@badsite.com" : $"recipient{i}@example.com";

                _ = await filter.CanAcceptFromAsync(new MockSession(), from, 1024);
                _ = await filter.CanDeliverToAsync(new MockSession(), to, from);
            }
        }

        [Benchmark(Description = "Event-Based Domain Filter (Late Rejection)")]
        public void EventBasedFiltering()
        {
            HashSet<string> blacklist = new(_testDomains, StringComparer.OrdinalIgnoreCase);
            HashSet<string> whitelist = new(_goodDomains, StringComparer.OrdinalIgnoreCase);

            // Simulate checking 100 email addresses
            for (int i = 0; i < 100; i++)
            {
                string fromDomain = i % 2 == 0 ? "spam.com" : "trusted.com";
                string toDomain = i % 3 == 0 ? "badsite.com" : "example.com";

                _ = !blacklist.Contains(fromDomain);
                _ = whitelist.Contains(toDomain);
            }
        }

        [Benchmark(Description = "Composite Filter (Multiple Filters)")]
        public async Task CompositeFiltering()
        {
            CompositeMailboxFilter compositeFilter = new(CompositeMode.All);
            compositeFilter.AddFilter(new DomainMailboxFilter(true).BlockFromDomains(_testDomains.ToArray()));
            compositeFilter.AddFilter(new AcceptAllMailboxFilter());

            // Simulate checking 100 email addresses
            for (int i = 0; i < 100; i++)
            {
                string from = i % 2 == 0 ? $"user{i}@spam.com" : $"user{i}@trusted.com";
                _ = await compositeFilter.CanAcceptFromAsync(new MockSession(), from, 1024);
            }
        }

        private class MockSession : ISmtpSession
        {
            public string Id => "mock";
            public EndPoint RemoteEndPoint => new IPEndPoint(IPAddress.Loopback, 25);
            public EndPoint LocalEndPoint => new IPEndPoint(IPAddress.Any, 25);
            public bool IsSecure => false;
            public bool IsAuthenticated => false;
            public string? AuthenticatedIdentity => null;
            public string? ClientDomain => "mock.local";
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