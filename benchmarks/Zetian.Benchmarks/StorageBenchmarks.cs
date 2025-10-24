using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Order;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Zetian.Abstractions;
using Zetian.Internal;
using Zetian.Storage;

namespace Zetian.Benchmarks
{
    /// <summary>
    /// Benchmarks for different message storage strategies
    /// </summary>
    [RankColumn]
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [SimpleJob(RunStrategy.ColdStart, launchCount: 1, warmupCount: 2, iterationCount: 3)]
    public class StorageBenchmarks
    {
        private IMessageStore _fileStore = null!;
        private IMessageStore _nullStore = null!;
        private ISmtpSession _mockSession = null!;
        private ISmtpMessage _mockMessage = null!;
        private string _tempDir = null!;

        [GlobalSetup]
        public void Setup()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"zetian_bench_{Guid.NewGuid()}");
            Directory.CreateDirectory(_tempDir);

            _fileStore = new FileMessageStore(_tempDir, createDirectoryStructure: true);
            _nullStore = NullMessageStore.Instance;

            _mockSession = new MockSession();

            // Create a mock message with some data
            byte[] messageData = GenerateMessageData(10 * 1024); // 10KB message
            _mockMessage = new SmtpMessage(
                "session123",
                "sender@example.com",
                new[] { "recipient1@example.com", "recipient2@example.com" },
                messageData
            );
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }

        [Benchmark(Description = "File Storage with Date Folders")]
        public async Task FileStorageWithDateFolders()
        {
            await _fileStore.SaveAsync(_mockSession, _mockMessage);
        }

        [Benchmark(Description = "Null Storage (No-Op)")]
        public async Task NullStorage()
        {
            await _nullStore.SaveAsync(_mockSession, _mockMessage);
        }

        [Benchmark(Description = "In-Memory Storage Simulation")]
        public async Task InMemoryStorage()
        {
            // Simulate in-memory storage
            Dictionary<string, byte[]> storage = new();
            await Task.Run(() =>
            {
                storage[_mockMessage.Id] = _mockMessage.GetRawData();
            });
        }

        [Benchmark(Description = "File Storage without Date Folders")]
        public async Task FileStorageFlat()
        {
            FileMessageStore flatStore = new(_tempDir, createDirectoryStructure: false);
            await flatStore.SaveAsync(_mockSession, _mockMessage);
        }

        private byte[] GenerateMessageData(int size)
        {
            byte[] data = new byte[size];
            Random random = new(42); // Fixed seed for reproducibility
            random.NextBytes(data);

            // Add some SMTP-like structure
            byte[] header = System.Text.Encoding.ASCII.GetBytes(
                "From: sender@example.com\r\n" +
                "To: recipient@example.com\r\n" +
                "Subject: Benchmark Test Message\r\n" +
                "Date: " + DateTime.UtcNow.ToString("R") + "\r\n" +
                "\r\n"
            );

            Array.Copy(header, data, Math.Min(header.Length, data.Length));
            return data;
        }

        private class MockSession : ISmtpSession
        {
            public string Id => "bench_session";
            public EndPoint RemoteEndPoint => new IPEndPoint(IPAddress.Loopback, 25);
            public EndPoint LocalEndPoint => new IPEndPoint(IPAddress.Any, 25);
            public bool IsSecure => false;
            public bool IsAuthenticated => false;
            public string? AuthenticatedIdentity => null;
            public string? ClientDomain => "benchmark.local";
            public DateTime StartTime => DateTime.UtcNow;
            public IDictionary<string, object> Properties => new Dictionary<string, object>();
            public X509Certificate2? ClientCertificate => null;
            public int MessageCount => 1;
            public bool PipeliningEnabled { get; set; }
            public bool EightBitMimeEnabled { get; set; }
            public bool BinaryMimeEnabled { get; set; }
            public long MaxMessageSize { get; set; } = 10485760;
        }
    }
}