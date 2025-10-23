using ConsoleTables;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;
using YamlDotNet.Serialization;
using Zetian.LoadGenerator;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddHostedService<LoadGeneratorHost>();

        // Load configuration
        string configPath = Path.Combine(AppContext.BaseDirectory, "config", "client.yml");
        if (File.Exists(configPath))
        {
            string yaml = File.ReadAllText(configPath);
            IDeserializer deserializer = new DeserializerBuilder().Build();
            ClientConfig config = deserializer.Deserialize<ClientConfig>(yaml) ?? new ClientConfig();
            services.AddSingleton(config);
        }
        else
        {
            services.AddSingleton(new ClientConfig());
        }
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Information);
    })
    .Build();

// Start metrics server for client
MetricServer metricsServer = new(hostname: "+", port: 9101);
metricsServer.Start();

await host.RunAsync();
metricsServer.Stop();

public class ClientConfig
{
    public TargetConfig Target { get; set; } = new();
    public ScenarioConfig Scenario { get; set; } = new();
    public MessageConfig Message { get; set; } = new();

    public class TargetConfig
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 25;
    }

    public class ScenarioConfig
    {
        public string Type { get; set; } = "throughput";
        public int Duration { get; set; } = 60;
        public int Connections { get; set; } = 10;
        public int Rate { get; set; } = 1000;
    }

    public class MessageConfig
    {
        public int Size { get; set; } = 1024;
        public int Recipients { get; set; } = 1;
    }
}

public class LoadGeneratorHost : BackgroundService
{
    private readonly ILogger<LoadGeneratorHost> _logger;
    private readonly ClientConfig _config;
    private readonly IHostApplicationLifetime _lifetime;

    public LoadGeneratorHost(
        ILogger<LoadGeneratorHost> logger,
        ClientConfig config,
        IHostApplicationLifetime lifetime)
    {
        _logger = logger;
        _config = config;
        _lifetime = lifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            PrintHeader();
            PrintConfiguration();

            _logger.LogInformation("Starting load test...");

            ITestScenario scenario = CreateScenario(_config.Scenario.Type);
            TestResult result = await scenario.ExecuteAsync(_config, stoppingToken);

            PrintResults(result);
            SaveResults(result);

            _logger.LogInformation("Load test completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Load test failed");
        }
        finally
        {
            _lifetime.StopApplication();
        }
    }

    private void PrintHeader()
    {
        Console.WriteLine();
        Console.WriteLine("╔═══════════════════════════════════════════╗");
        Console.WriteLine("║     Zetian SMTP Load Generator           ║");
        Console.WriteLine("╚═══════════════════════════════════════════╝");
        Console.WriteLine();
    }

    private void PrintConfiguration()
    {
        ConsoleTable table = new("Parameter", "Value");
        table.AddRow("Target Host", $"{_config.Target.Host}:{_config.Target.Port}")
             .AddRow("Scenario Type", _config.Scenario.Type)
             .AddRow("Duration", $"{_config.Scenario.Duration} seconds")
             .AddRow("Connections", _config.Scenario.Connections)
             .AddRow("Target Rate", $"{_config.Scenario.Rate} msg/s")
             .AddRow("Message Size", $"{_config.Message.Size} bytes")
             .AddRow("Recipients", _config.Message.Recipients);

        Console.WriteLine("Test Configuration:");
        table.Write(Format.Alternative);
        Console.WriteLine();
    }

    private void PrintResults(TestResult result)
    {
        Console.WriteLine();
        Console.WriteLine("╔═══════════════════════════════════════════╗");
        Console.WriteLine("║            TEST RESULTS                   ║");
        Console.WriteLine("╚═══════════════════════════════════════════╝");
        Console.WriteLine();

        ConsoleTable table = new("Metric", "Value");
        table.AddRow("Scenario", result.ScenarioName)
             .AddRow("Duration", $"{result.Duration.TotalSeconds:F2} seconds")
             .AddRow("Total Messages", result.TotalMessages)
             .AddRow("Successful", $"{result.SuccessfulMessages} ({100.0 * result.SuccessfulMessages / Math.Max(1, result.TotalMessages):F2}%)")
             .AddRow("Failed", $"{result.FailedMessages} ({100.0 * result.FailedMessages / Math.Max(1, result.TotalMessages):F2}%)")
             .AddRow("Throughput", $"{result.MessagesPerSecond:F2} msg/s");

        if (result.AverageLatency > 0)
        {
            table.AddRow("Avg Latency", $"{result.AverageLatency * 1000:F2} ms")
                 .AddRow("P95 Latency", $"{result.P95Latency * 1000:F2} ms")
                 .AddRow("P99 Latency", $"{result.P99Latency * 1000:F2} ms");
        }

        table.Write(Format.Alternative);

        if (result.Errors.Any())
        {
            Console.WriteLine();
            Console.WriteLine("Errors encountered:");
            foreach (string? error in result.Errors.Take(5))
            {
                Console.WriteLine($"  - {error}");
            }
            if (result.Errors.Count > 5)
            {
                Console.WriteLine($"  ... and {result.Errors.Count - 5} more");
            }
        }

        Console.WriteLine();
    }

    private void SaveResults(TestResult result)
    {
        try
        {
            string resultsDir = Path.Combine(AppContext.BaseDirectory, "results");
            Directory.CreateDirectory(resultsDir);

            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            string jsonFile = Path.Combine(resultsDir, $"result_{timestamp}.json");

            ISerializer serializer = new SerializerBuilder()
                .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
                .Build();

            string json = System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(jsonFile, json);

            _logger.LogInformation("Results saved to: {File}", jsonFile);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save results to file");
        }
    }

    private ITestScenario CreateScenario(string type)
    {
        return type.ToLowerInvariant() switch
        {
            "throughput" => new ThroughputScenario(),
            "concurrent" => new ConcurrentScenario(),
            "burst" => new BurstScenario(),
            "sustained" => new SustainedScenario(),
            _ => throw new ArgumentException($"Unknown scenario type: {type}")
        };
    }
}