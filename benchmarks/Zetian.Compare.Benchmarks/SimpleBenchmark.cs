using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using MailKit.Net.Smtp;
using MimeKit;
using SmtpServer;
using SmtpServer.ComponentModel;
using Zetian.Configuration;

namespace Zetian.Compare.Benchmarks
{
    [RankColumn]
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [SimpleJob(launchCount: 1, warmupCount: 2, iterationCount: 3)]
    public class SimpleBenchmark
    {
        private Zetian.Server.SmtpServer? _zetianServer;
        private SmtpServer.SmtpServer? _smtpServer;

        private readonly int _zetianPort = 10025;
        private readonly int _smtpServerPort = 10026;

        [GlobalSetup]
        public void Setup()
        {
            SmtpServerConfiguration zetianConfig = new()
            {
                Port = _zetianPort,
                MaxMessageSize = 1024 * 1024, // 1MB
                MaxConnections = 10,
                ConnectionTimeout = TimeSpan.FromSeconds(10),
                MessageStore = Zetian.Storage.NullMessageStore.Instance,
                MailboxFilter = new Zetian.Storage.AcceptAllMailboxFilter()
            };

            _zetianServer = new Zetian.Server.SmtpServer(zetianConfig);
            _ = _zetianServer.StartAsync();

            ISmtpServerOptions smtpOptions = new SmtpServer.SmtpServerOptionsBuilder()
                .ServerName("Test")
                .Port(_smtpServerPort)
                .Build();

            _smtpServer = new SmtpServer.SmtpServer(smtpOptions, ServiceProvider.Default);
            _ = _smtpServer.StartAsync(CancellationToken.None);

            Thread.Sleep(500);
        }

        [GlobalCleanup]
        public async Task Cleanup()
        {
            if (_zetianServer != null)
            {
                await _zetianServer.StopAsync();
                _zetianServer.Dispose();
            }

            _smtpServer?.Shutdown();
        }

        [Benchmark(Baseline = true)]
        public async Task Zetian_SendSingleMessage()
        {
            await SendMessage(_zetianPort);
        }

        [Benchmark]
        public async Task SmtpServer_SendSingleMessage()
        {
            await SendMessage(_smtpServerPort);
        }

        private async Task SendMessage(int port)
        {
            using SmtpClient client = new();
            await client.ConnectAsync("localhost", port, false);

            MimeMessage message = new();
            message.From.Add(new MailboxAddress("Test", "test@test.com"));
            message.To.Add(new MailboxAddress("User", "user@test.com"));
            message.Subject = "Test";
            message.Body = new TextPart("plain") { Text = "Test message" };

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}