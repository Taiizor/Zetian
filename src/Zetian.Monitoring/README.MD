# Zetian.Monitoring - Real-time Metrics & Observability

[![NuGet-Version](https://img.shields.io/nuget/v/Zetian.Monitoring.svg?label=NuGet)](https://www.nuget.org/packages/Zetian.Monitoring)
[![NuGet-Download](https://img.shields.io/nuget/dt/Zetian.Monitoring?label=Download)](https://www.nuget.org/api/v2/package/Zetian.Monitoring)
[![License](https://img.shields.io/github/license/Taiizor/Zetian.svg?label=License)](https://github.com/Taiizor/Zetian/blob/develop/LICENSE)

Comprehensive monitoring and observability solution for Zetian SMTP Server with Prometheus, OpenTelemetry, and real-time metrics collection.

## ⚡ Features

- 💾 **Low Overhead** - Minimal performance impact
- 🔄 **Zero-downtime** - Hot-reload metrics without restart
- 📉 **Histograms & Percentiles** - P95, P99 latency tracking
- 📈 **Command Metrics** - Detailed SMTP command statistics
- 📊 **Real-time Metrics** - Live server performance monitoring
- 🔭 **OpenTelemetry** - Full observability with distributed tracing
- 🚀 **Performance Tracking** - Throughput, latency, and resource usage
- 🎯 **Prometheus Ready** - Native Prometheus exporter with /metrics endpoint

## 📦 Installation

```bash
# Install Zetian SMTP Server (required)
dotnet add package Zetian

# Install Monitoring Extension
dotnet add package Zetian.Monitoring
```

## 🚀 Quick Start

### Basic Monitoring

```csharp
using Zetian.Server;
using Zetian.Monitoring.Extensions;

// Create and start SMTP server
var server = SmtpServerBuilder.CreateBasic();

// Enable monitoring with default settings
server.EnableMonitoring();

await server.StartAsync();

// Get statistics
var stats = server.GetStatistics();
Console.WriteLine($"Active Sessions: {stats.ActiveSessions}");
Console.WriteLine($"Messages/sec: {stats.CurrentThroughput.MessagesPerSecond}");
```

### Prometheus Integration

```csharp
// Enable Prometheus exporter on port 9090 (default host: localhost)
server.EnablePrometheus(9090);

// With custom host (use "+" for all interfaces - requires admin rights)
server.EnablePrometheus("0.0.0.0", 9090);  // Listen on all IPv4
server.EnablePrometheus("localhost", 9090);  // Listen on localhost only (default)
server.EnablePrometheus("+", 9090);  // Listen on all interfaces (requires admin)

// Or with builder pattern
server.EnableMonitoring(builder => builder
    .EnablePrometheus(9090)  // Uses default host: localhost
    .WithUpdateInterval(TimeSpan.FromSeconds(5)));

// With custom host in builder
server.EnableMonitoring(builder => builder
    .EnablePrometheus("0.0.0.0", 9090)
    .WithPrometheusHost("0.0.0.0")  // Alternative way
    .WithUpdateInterval(TimeSpan.FromSeconds(5)));

// Metrics available at http://localhost:9090/metrics (or your configured host)
```

### Advanced Configuration

```csharp
server.EnableMonitoring(builder => builder
    .EnablePrometheus(9090)
    .EnableOpenTelemetry("http://localhost:4317")
    .WithServiceName("smtp-production")
    .WithServiceVersion("1.0.0")
    .EnableDetailedMetrics()
    .EnableCommandMetrics()
    .EnableThroughputMetrics()
    .EnableHistograms()
    .WithUpdateInterval(TimeSpan.FromSeconds(10))
    .WithLabels(
        ("environment", "production"),
        ("region", "us-east-1"),
        ("instance", "smtp-01"))
    .WithCommandDurationBuckets(1, 5, 10, 25, 50, 100, 250, 500, 1000)
    .WithMessageSizeBuckets(1024, 10240, 102400, 1048576, 10485760));
```

## 📊 Available Metrics

### Prometheus Metrics

| Metric | Type | Description |
|--------|------|-------------|
| `zetian_sessions_total` | Counter | Total SMTP sessions |
| `zetian_messages_total{status}` | Counter | Total messages (delivered/rejected) |
| `zetian_bytes_total{direction}` | Counter | Total bytes (in/out) |
| `zetian_errors_total{type}` | Counter | Total errors by type |
| `zetian_commands_total{command,status}` | Counter | SMTP commands executed |
| `zetian_authentications_total{mechanism,status}` | Counter | Authentication attempts |
| `zetian_connections_total{status}` | Counter | Connection attempts |
| `zetian_tls_upgrades_total{status}` | Counter | TLS upgrades |
| `zetian_rejections_total{reason}` | Counter | Message rejections |
| `zetian_active_sessions` | Gauge | Current active sessions |
| `zetian_uptime_seconds` | Gauge | Server uptime |
| `zetian_memory_bytes` | Gauge | Memory usage |
| `zetian_throughput_messages_per_second` | Gauge | Current message throughput |
| `zetian_throughput_bytes_per_second` | Gauge | Current bytes throughput |
| `zetian_command_duration_milliseconds{command}` | Histogram | Command execution time |
| `zetian_message_size_bytes` | Histogram | Message size distribution |
| `zetian_session_duration_seconds` | Summary | Session duration statistics |

### Server Statistics

```csharp
// Get comprehensive statistics
var stats = server.GetStatistics();

// Basic metrics
Console.WriteLine($"Uptime: {stats.Uptime}");
Console.WriteLine($"Total Sessions: {stats.TotalSessions}");
Console.WriteLine($"Active Sessions: {stats.ActiveSessions}");
Console.WriteLine($"Total Messages: {stats.TotalMessagesReceived}");
Console.WriteLine($"Delivery Rate: {stats.DeliveryRate}%");
Console.WriteLine($"Rejection Rate: {stats.RejectionRate}%");

// Connection metrics
Console.WriteLine($"Connections Accepted: {stats.ConnectionMetrics.AcceptedCount}");
Console.WriteLine($"TLS Usage: {stats.ConnectionMetrics.TlsUsageRate}%");
Console.WriteLine($"Peak Concurrent: {stats.ConnectionMetrics.PeakConcurrentConnections}");

// Authentication metrics
Console.WriteLine($"Auth Success Rate: {stats.AuthenticationMetrics.SuccessRate}%");
Console.WriteLine($"Unique Users: {stats.AuthenticationMetrics.UniqueUsers}");

// Command metrics
foreach (var cmd in stats.CommandMetrics)
{
    Console.WriteLine($"{cmd.Key}: {cmd.Value.AverageDurationMs}ms avg, " +
                      $"{cmd.Value.SuccessRate}% success");
}

// Throughput
var throughput = stats.CurrentThroughput;
Console.WriteLine($"Messages/sec: {throughput.MessagesPerSecond}");
Console.WriteLine($"Bytes/sec: {throughput.BytesPerSecond}");
Console.WriteLine($"Commands/sec: {throughput.CommandsPerSecond}");
```

## 🔧 Custom Metrics

```csharp
// Record custom command metrics
server.RecordMetric("CUSTOM_CMD", success: true, durationMs: 42.5);

// Access metrics collector directly
var collector = server.GetMetricsCollector();
collector.RecordCommand("XCUSTOM", true, 15.3);
collector.RecordAuthentication(true, "CUSTOM_AUTH");
collector.RecordRejection("Custom rejection reason");
```

## 📈 Grafana Dashboard

### Import Dashboard

1. Import the [Zetian SMTP Dashboard](https://grafana.com/dashboards/zetian-smtp)
2. Configure Prometheus data source
3. Set server variables

### Key Panels

- **Rejections** - Reasons and trends
- **Commands** - Execution counts and durations
- **Authentication** - Success rates by mechanism
- **Overview** - Sessions, messages, errors, uptime
- **Connections** - Active/peak connections, TLS usage
- **Performance** - Latency percentiles, throughput graphs

## 🔭 OpenTelemetry Integration

```csharp
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

// Configure OpenTelemetry
var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSource("Zetian.SMTP")
    .AddOtlpExporter(options =>
    {
        options.Endpoint = new Uri("http://localhost:4317");
    })
    .Build();

var meterProvider = Sdk.CreateMeterProviderBuilder()
    .AddMeter("Zetian.SMTP")
    .AddOtlpExporter(options =>
    {
        options.Endpoint = new Uri("http://localhost:4317");
    })
    .Build();

// Enable monitoring with OpenTelemetry
server.EnableMonitoring(builder => builder
    .EnableOpenTelemetry("http://localhost:4317")
    .WithServiceName("smtp-server")
    .WithServiceVersion("1.0.0"));
```

## ⚙️ Configuration Options

```csharp
public class MonitoringConfiguration
{
    // Prometheus settings
    public bool EnablePrometheus { get; set; }
    public int? PrometheusPort { get; set; }
    
    // OpenTelemetry settings  
    public bool EnableOpenTelemetry { get; set; }
    public string? OpenTelemetryEndpoint { get; set; }
    
    // Update settings
    public TimeSpan UpdateInterval { get; set; } = TimeSpan.FromSeconds(10);
    
    // Feature toggles
    public bool EnableDetailedMetrics { get; set; } = true;
    public bool EnableCommandMetrics { get; set; } = true;
    public bool EnableThroughputMetrics { get; set; } = true;
    public bool EnableHistograms { get; set; } = true;
    
    // Service identification
    public string ServiceName { get; set; } = "Zetian.SMTP";
    public string ServiceVersion { get; set; } = "1.0.0";
    
    // Custom labels for all metrics
    public Dictionary<string, string> CustomLabels { get; }
}
```

## 📊 Monitoring Best Practices

1. **Monitor trends** - Use Grafana for long-term trend analysis
2. **Configure alerting** - Set up Prometheus alerts for critical metrics
3. **Enable only needed metrics** - Disable unused features to reduce overhead
4. **Optimize histogram buckets** - Adjust buckets based on your traffic patterns
5. **Use custom labels** - Add environment, region, instance for multi-server setups
6. **Set appropriate update intervals** - Balance between real-time data and performance

## 🎯 Performance Impact

- **Storage**: No persistent storage required
- **CPU Overhead**: < 1% with default settings
- **Network**: Minimal (metrics endpoint only)
- **Memory Usage**: ~10-50MB depending on traffic

## 🔒 Security

- Monitor for high cardinality metrics
- Metrics endpoint should be protected in production
- Sanitize custom labels to prevent metric explosion
- Use authentication/firewall rules for Prometheus endpoint

## 📚 Integration Examples

### With Docker

```dockerfile
FROM mcr.microsoft.com/dotnet/runtime:8.0
EXPOSE 25 9090
# SMTP on 25, Metrics on 9090
```

### With Kubernetes

```yaml
apiVersion: v1
kind: Service
metadata:
  name: smtp-server
  annotations:
    prometheus.io/scrape: "true"
    prometheus.io/port: "9090"
    prometheus.io/path: "/metrics"
```

### With docker-compose

```yaml
services:
  smtp:
    image: zetian/smtp:latest
    ports:
      - "25:25"
      - "9090:9090"
  
  prometheus:
    image: prom/prometheus
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml
    
  grafana:
    image: grafana/grafana
    ports:
      - "3000:3000"
```

## 📋 Requirements

- .NET 8.0, 9.0, or 10.0
- OpenTelemetry packages
- Zetian SMTP Server package
- Prometheus.NET for metrics export

## 📚 Documentation & Support

- **Issues**: [GitHub Issues](https://github.com/Taiizor/Zetian/issues)
- **Examples**: [GitHub Examples](https://github.com/Taiizor/Zetian/tree/develop/examples)
- **Discussions**: [GitHub Discussions](https://github.com/Taiizor/Zetian/discussions)
- **Documentation**: [Zetian Documentation](https://zetian.soferity.com)

## 🙏 Acknowledgments

- Built on top of [Zetian SMTP Server](https://www.nuget.org/packages/Zetian)
- Community feedback and contributions
- Inspired by ASP.NET Core Health Checks

## 🔒 Security

**Best Practices:**
- Configure rate limiting
- Keep the library updated
- Implement proper authentication
- Always use TLS/SSL in production

**Report Security Issues:** taiizor@vegalya.com

## 📄 License

MIT License - see [LICENSE](https://github.com/Taiizor/Zetian/blob/develop/LICENSE)

---

**Built with ❤️ for the .NET community**