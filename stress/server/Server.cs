using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;
using YamlDotNet.Serialization;
using Zetian.Configuration;
using Zetian.Server;
using Zetian.Storage;
using Zetian.StressTestServer;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddHostedService<SmtpServerHost>();

        // Load configuration
        string configPath = Path.Combine(AppContext.BaseDirectory, "config", "server.yml");
        if (File.Exists(configPath))
        {
            string yaml = File.ReadAllText(configPath);
            IDeserializer deserializer = new DeserializerBuilder().Build();
            ServerConfig config = deserializer.Deserialize<ServerConfig>(yaml) ?? new ServerConfig();
            services.AddSingleton(config);
        }
        else
        {
            services.AddSingleton(new ServerConfig());
        }
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Information);
    })
    .Build();

// Start Prometheus metrics server
MetricServer metricsServer = new(hostname: "+", port: 9100);
metricsServer.Start();

Console.WriteLine("===========================================");
Console.WriteLine(" Zetian SMTP Stress Test Server");
Console.WriteLine("===========================================");
Console.WriteLine(" SMTP Port: 25");
Console.WriteLine(" Metrics Port: 9100");
Console.WriteLine(" Status: UNRESTRICTED MODE");
Console.WriteLine("===========================================");
Console.WriteLine();

await host.RunAsync();
metricsServer.Stop();

public class ServerConfig
{
    public SmtpConfig Smtp { get; set; } = new();
    public MetricsConfig Metrics { get; set; } = new();

    public class SmtpConfig
    {
        public int Port { get; set; } = 25;
        public bool EnableTls { get; set; } = false;
        public string? MaxConnections { get; set; } = "unlimited";
        public string? MaxMessageSize { get; set; } = "unlimited";
    }

    public class MetricsConfig
    {
        public bool Enabled { get; set; } = true;
        public int Port { get; set; } = 9100;
    }
}

public class SmtpServerHost(
    ILogger<SmtpServerHost> logger,
    ServerConfig config) : IHostedService
{
    private readonly ServerConfig _config = config;
    private SmtpServer? _server;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        SmtpServerConfiguration config = new()
        {
            Port = _config.Smtp.Port,
            ServerName = "stress-test-server",

            // NO LIMITS - Everything unlimited for stress testing
            MaxConnections = int.MaxValue / 2,  // Prevent overflow
            MaxConnectionsPerIp = int.MaxValue / 2,
            MaxMessageSize = long.MaxValue / 2,
            MaxRecipients = int.MaxValue / 2,

            // Timeouts - Very long for stress testing
            ConnectionTimeout = TimeSpan.FromHours(24),
            CommandTimeout = TimeSpan.FromMinutes(30),
            DataTimeout = TimeSpan.FromMinutes(30),

            // Performance optimizations
            EnablePipelining = true,
            Enable8BitMime = true,
            EnableChunking = true,
            EnableBinaryMime = true,
            EnableSmtpUtf8 = true,
            EnableSizeExtension = true,
            UseNagleAlgorithm = false,  // Disable for better latency
            ReadBufferSize = 65536,      // 64KB buffers
            WriteBufferSize = 65536,

            // No authentication required for stress testing
            RequireAuthentication = false,
            RequireSecureConnection = false,
            AllowPlainTextAuthentication = true,

            // Use NullMessageStore to ignore messages (pure performance test)
            MessageStore = NullMessageStore.Instance,

            // Accept all mailboxes
            MailboxFilter = new AcceptAllMailboxFilter()
        };

        _server = new SmtpServer(config);

        // Setup event handlers with metrics
        _server.SessionCreated += (sender, args) =>
        {
            ServerMetrics.ActiveConnections.Inc();
            ServerMetrics.TotalConnections.Inc();
            logger.LogDebug("Session created: {SessionId} from {RemoteEndPoint}",
                args.Session.Id, args.Session.RemoteEndPoint);
        };

        _server.SessionCompleted += (sender, args) =>
        {
            ServerMetrics.ActiveConnections.Dec();
            logger.LogDebug("Session completed: {SessionId}", args.Session.Id);
        };

        _server.MessageReceived += (sender, args) =>
        {
            using (ServerMetrics.MessageProcessingTime.NewTimer())
            {
                ServerMetrics.MessagesReceived.Inc();

                // Calculate approximate message size
                long messageSize = args.Message?.Size ?? 0;
                ServerMetrics.MessageSize.Observe(messageSize);
            }
        };

        _server.ErrorOccurred += (sender, args) =>
        {
            ServerMetrics.Errors.WithLabels(args.Exception.GetType().Name).Inc();
            logger.LogError(args.Exception, "Server error in session {SessionId}", args.Session?.Id ?? "unknown");
        };

        await _server.StartAsync(cancellationToken);

        logger.LogInformation(
            "SMTP server started on {Endpoint} (UNRESTRICTED MODE)",
            _server.Endpoint);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_server != null)
        {
            await _server.StopAsync(cancellationToken);
            _server.Dispose();
        }
        logger.LogInformation("SMTP server stopped");
    }
}