import { Metadata } from 'next';
import { 
  BarChart, 
  Activity, 
  Gauge,
  Shield,
  CheckCircle,
  Server,
  Zap,
  LineChart,
  PieChart,
  Target,
  Globe,
  Hash,
  Settings
} from 'lucide-react';
import CodeBlock from '@/components/CodeBlock';

export const metadata: Metadata = {
  title: 'Monitoring',
  description: 'Real-time metrics and observability for Zetian SMTP Server with Prometheus and OpenTelemetry',
};

const prometheusExample = `// Enable Prometheus exporter on port 9090
server.EnablePrometheus(9090);

// With custom host (use "+" for all interfaces - requires admin)
server.EnablePrometheus("0.0.0.0", 9090);  // Listen on all IPv4
server.EnablePrometheus("localhost", 9090); // Listen on localhost only
server.EnablePrometheus("+", 9090);        // All interfaces (requires admin)

// Metrics available at http://localhost:9090/metrics`;

const basicExample = `using Zetian.Server;
using Zetian.Monitoring.Extensions;

// Create and start SMTP server
var server = SmtpServerBuilder.CreateBasic();

// Enable monitoring with default settings
server.EnableMonitoring();

await server.StartAsync();

// Get statistics
var stats = server.GetStatistics();
Console.WriteLine($"Active Sessions: {stats?.ActiveSessions}");
Console.WriteLine($"Messages/sec: {stats?.CurrentThroughput?.MessagesPerSecond}");`;

const advancedExample = `server.EnableMonitoring(builder => builder
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
    .WithMessageSizeBuckets(1024, 10240, 102400, 1048576, 10485760));`;

const statisticsExample = `// Get comprehensive statistics
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
Console.WriteLine($"Commands/sec: {throughput.CommandsPerSecond}");`;

const openTelemetryExample = `using OpenTelemetry;
using Zetian.Server;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Zetian.Monitoring.Extensions;

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
    .WithServiceVersion("1.0.0"));`;

const customMetricsExample = `// Record custom command metrics
server.RecordMetric("CUSTOM_CMD", success: true, durationMs: 42.5);

// Access metrics collector directly
var collector = server.GetMetricsCollector();
collector.RecordCommand("XCUSTOM", true, 15.3);
collector.RecordAuthentication(true, "CUSTOM_AUTH");
collector.RecordRejection("Custom rejection reason");`;

const dockerComposeExample = `services:
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
      - "3000:3000"`;

const kubernetesExample = `apiVersion: v1
kind: Service
metadata:
  name: smtp-server
  annotations:
    prometheus.io/scrape: "true"
    prometheus.io/port: "9090"
    prometheus.io/path: "/metrics"`;

export default function MonitoringPage() {
  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-950">
      <div className="relative">
        
        <div className="relative max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
          {/* Header */}
          <div className="text-center mb-12">
            <div className="inline-flex items-center justify-center w-20 h-20 bg-gradient-to-br from-purple-500 to-pink-600 rounded-2xl mb-6">
              <Activity className="w-10 h-10 text-white" />
            </div>
            <h1 className="text-5xl font-bold text-gray-900 dark:text-white mb-4">Zetian.Monitoring</h1>
            <p className="text-xl text-gray-600 dark:text-gray-400 mb-6">
              Real-time Metrics & Observability for SMTP Server
            </p>
            <div className="flex justify-center gap-4">
              <a href="https://www.nuget.org/packages/Zetian.Monitoring" className="inline-flex items-center px-4 py-2 bg-purple-600 text-white rounded-lg hover:bg-purple-700">
                <span className="mr-2">📦</span> NuGet Package
              </a>
              <a href="https://github.com/Taiizor/Zetian" className="inline-flex items-center px-4 py-2 bg-gray-700 text-white rounded-lg hover:bg-gray-600">
                <span className="mr-2">⭐</span> Star on GitHub
              </a>
            </div>
          </div>

          {/* Features */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-12">
            {[
              { icon: BarChart, title: 'Prometheus Ready', desc: 'Native Prometheus exporter with /metrics endpoint' },
              { icon: Globe, title: 'OpenTelemetry', desc: 'Full observability with distributed tracing' },
              { icon: Gauge, title: 'Real-time Metrics', desc: 'Live server performance monitoring' },
              { icon: LineChart, title: 'Histograms & Percentiles', desc: 'P95, P99 latency tracking' },
              { icon: Zap, title: 'Low Overhead', desc: 'Minimal performance impact < 1% CPU' },
              { icon: Target, title: 'Command Metrics', desc: 'Detailed SMTP command statistics' },
            ].map((feature, i) => (
              <div key={i} className="bg-white dark:bg-gray-800 rounded-xl p-6 border border-gray-200 dark:border-gray-700 shadow-sm">
                <feature.icon className="w-10 h-10 text-purple-500 dark:text-purple-400 mb-3" />
                <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-2">{feature.title}</h3>
                <p className="text-gray-600 dark:text-gray-400 text-sm">{feature.desc}</p>
              </div>
            ))}
          </div>

          {/* Quick Start */}
          <div className="bg-white dark:bg-gray-900 rounded-2xl p-8 mb-12 shadow-sm border border-gray-200 dark:border-gray-700">
            <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-6 flex items-center">
              <Zap className="w-8 h-8 mr-3 text-yellow-400" />
              Quick Start
            </h2>

            <div className="space-y-6">
              <div>
                <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-3">Installation</h3>
                <CodeBlock language="bash" code={`# Install Zetian SMTP Server (required)
dotnet add package Zetian

# Install Monitoring Extension
dotnet add package Zetian.Monitoring`} />
              </div>

              <div>
                <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-3">Basic Monitoring</h3>
                <CodeBlock language="csharp" code={basicExample} />
              </div>

              <div>
                <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-3">Prometheus Integration</h3>
                <CodeBlock language="csharp" code={prometheusExample} />
              </div>
            </div>
          </div>

          {/* Available Metrics */}
          <div className="bg-white dark:bg-gray-900 rounded-2xl p-8 mb-12 shadow-sm border border-gray-200 dark:border-gray-700">
            <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-6 flex items-center">
              <PieChart className="w-8 h-8 mr-3 text-blue-400" />
              Available Metrics
            </h2>

            <div className="space-y-6">
              <div>
                <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-3">Prometheus Metrics</h3>
                <div className="bg-gray-100 dark:bg-gray-950 rounded-lg overflow-hidden">
                  <table className="w-full">
                    <thead className="bg-gray-200 dark:bg-gray-800">
                      <tr>
                        <th className="px-4 py-3 text-left text-gray-700 dark:text-gray-300">Metric</th>
                        <th className="px-4 py-3 text-left text-gray-700 dark:text-gray-300">Type</th>
                        <th className="px-4 py-3 text-left text-gray-700 dark:text-gray-300">Description</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-gray-300 dark:divide-gray-700">
                      <tr><td className="px-4 py-3 text-gray-700 dark:text-gray-300 font-mono text-sm">zetian_sessions_total</td><td className="px-4 py-3 text-gray-600 dark:text-gray-400">Counter</td><td className="px-4 py-3 text-gray-600 dark:text-gray-400">Total SMTP sessions</td></tr>
                      <tr><td className="px-4 py-3 text-gray-700 dark:text-gray-300 font-mono text-sm">zetian_messages_total{"{status}"}</td><td className="px-4 py-3 text-gray-600 dark:text-gray-400">Counter</td><td className="px-4 py-3 text-gray-600 dark:text-gray-400">Total messages (delivered/rejected)</td></tr>
                      <tr><td className="px-4 py-3 text-gray-700 dark:text-gray-300 font-mono text-sm">zetian_bytes_total{"{direction}"}</td><td className="px-4 py-3 text-gray-600 dark:text-gray-400">Counter</td><td className="px-4 py-3 text-gray-600 dark:text-gray-400">Total bytes (in/out)</td></tr>
                      <tr><td className="px-4 py-3 text-gray-700 dark:text-gray-300 font-mono text-sm">zetian_errors_total{"{type}"}</td><td className="px-4 py-3 text-gray-600 dark:text-gray-400">Counter</td><td className="px-4 py-3 text-gray-600 dark:text-gray-400">Total errors by type</td></tr>
                      <tr><td className="px-4 py-3 text-gray-700 dark:text-gray-300 font-mono text-sm">zetian_commands_total{"{command,status}"}</td><td className="px-4 py-3 text-gray-600 dark:text-gray-400">Counter</td><td className="px-4 py-3 text-gray-600 dark:text-gray-400">SMTP commands executed</td></tr>
                      <tr><td className="px-4 py-3 text-gray-700 dark:text-gray-300 font-mono text-sm">zetian_authentications_total{"{mechanism,status}"}</td><td className="px-4 py-3 text-gray-600 dark:text-gray-400">Counter</td><td className="px-4 py-3 text-gray-600 dark:text-gray-400">Authentication attempts</td></tr>
                      <tr><td className="px-4 py-3 text-gray-700 dark:text-gray-300 font-mono text-sm">zetian_active_sessions</td><td className="px-4 py-3 text-gray-600 dark:text-gray-400">Gauge</td><td className="px-4 py-3 text-gray-600 dark:text-gray-400">Current active sessions</td></tr>
                      <tr><td className="px-4 py-3 text-gray-700 dark:text-gray-300 font-mono text-sm">zetian_uptime_seconds</td><td className="px-4 py-3 text-gray-600 dark:text-gray-400">Gauge</td><td className="px-4 py-3 text-gray-600 dark:text-gray-400">Server uptime</td></tr>
                      <tr><td className="px-4 py-3 text-gray-700 dark:text-gray-300 font-mono text-sm">zetian_memory_bytes</td><td className="px-4 py-3 text-gray-600 dark:text-gray-400">Gauge</td><td className="px-4 py-3 text-gray-600 dark:text-gray-400">Memory usage</td></tr>
                      <tr><td className="px-4 py-3 text-gray-700 dark:text-gray-300 font-mono text-sm">zetian_throughput_messages_per_second</td><td className="px-4 py-3 text-gray-600 dark:text-gray-400">Gauge</td><td className="px-4 py-3 text-gray-600 dark:text-gray-400">Current message throughput</td></tr>
                      <tr><td className="px-4 py-3 text-gray-700 dark:text-gray-300 font-mono text-sm">zetian_command_duration_milliseconds{"{command}"}</td><td className="px-4 py-3 text-gray-600 dark:text-gray-400">Histogram</td><td className="px-4 py-3 text-gray-600 dark:text-gray-400">Command execution time</td></tr>
                      <tr><td className="px-4 py-3 text-gray-700 dark:text-gray-300 font-mono text-sm">zetian_message_size_bytes</td><td className="px-4 py-3 text-gray-600 dark:text-gray-400">Histogram</td><td className="px-4 py-3 text-gray-600 dark:text-gray-400">Message size distribution</td></tr>
                      <tr><td className="px-4 py-3 text-gray-700 dark:text-gray-300 font-mono text-sm">zetian_session_duration_seconds</td><td className="px-4 py-3 text-gray-600 dark:text-gray-400">Summary</td><td className="px-4 py-3 text-gray-600 dark:text-gray-400">Session duration statistics</td></tr>
                    </tbody>
                  </table>
                </div>
              </div>

              <div>
                <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-3">Server Statistics</h3>
                <CodeBlock language="csharp" code={statisticsExample} />
              </div>
            </div>
          </div>

          {/* Advanced Configuration */}
          <div className="bg-white dark:bg-gray-900 rounded-2xl p-8 mb-12 shadow-sm border border-gray-200 dark:border-gray-700">
            <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-6 flex items-center">
              <Settings className="w-8 h-8 mr-3 text-orange-400" />
              Advanced Configuration
            </h2>

            <div className="space-y-6">
              <div>
                <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-3">Full Configuration</h3>
                <CodeBlock language="csharp" code={advancedExample} />
              </div>

              <div>
                <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-3">Custom Metrics</h3>
                <CodeBlock language="csharp" code={customMetricsExample} />
              </div>
            </div>
          </div>

          {/* OpenTelemetry */}
          <div className="bg-white dark:bg-gray-900 rounded-2xl p-8 mb-12 shadow-sm border border-gray-200 dark:border-gray-700">
            <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-6 flex items-center">
              <Globe className="w-8 h-8 mr-3 text-green-400" />
              OpenTelemetry Integration
            </h2>
            <CodeBlock language="csharp" code={openTelemetryExample} />
          </div>

          {/* Deployment */}
          <div className="bg-white dark:bg-gray-900 rounded-2xl p-8 mb-12 shadow-sm border border-gray-200 dark:border-gray-700">
            <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-6 flex items-center">
              <Server className="w-8 h-8 mr-3 text-cyan-400" />
              Deployment Examples
            </h2>

            <div className="space-y-6">
              <div>
                <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-3">Docker Compose</h3>
                <CodeBlock language="yaml" code={dockerComposeExample} />
              </div>

              <div>
                <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-3">Kubernetes</h3>
                <CodeBlock language="yaml" code={kubernetesExample} />
              </div>
            </div>
          </div>

          {/* Performance Impact */}
          <div className="bg-white dark:bg-gray-900 rounded-2xl p-8 mb-12 shadow-sm border border-gray-200 dark:border-gray-700">
            <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-6 flex items-center">
              <Gauge className="w-8 h-8 mr-3 text-red-400" />
              Performance Impact
            </h2>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div>
                <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-3">Resource Usage</h3>
                <ul className="space-y-2 text-gray-700 dark:text-gray-300">
                  <li className="flex items-start">
                    <CheckCircle className="w-5 h-5 text-green-400 mr-2 mt-0.5 flex-shrink-0" />
                    <span><strong>CPU:</strong> Less than 1% overhead</span>
                  </li>
                  <li className="flex items-start">
                    <CheckCircle className="w-5 h-5 text-green-400 mr-2 mt-0.5 flex-shrink-0" />
                    <span><strong>Memory:</strong> 10-50MB depending on traffic</span>
                  </li>
                  <li className="flex items-start">
                    <CheckCircle className="w-5 h-5 text-green-400 mr-2 mt-0.5 flex-shrink-0" />
                    <span><strong>Network:</strong> Minimal (metrics endpoint only)</span>
                  </li>
                  <li className="flex items-start">
                    <CheckCircle className="w-5 h-5 text-green-400 mr-2 mt-0.5 flex-shrink-0" />
                    <span><strong>Storage:</strong> No persistent storage required</span>
                  </li>
                </ul>
              </div>

              <div>
                <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-3">Best Practices</h3>
                <ul className="space-y-2 text-gray-700 dark:text-gray-300">
                  <li className="flex items-start">
                    <Shield className="w-5 h-5 text-blue-400 mr-2 mt-0.5 flex-shrink-0" />
                    <span>Protect metrics endpoint in production</span>
                  </li>
                  <li className="flex items-start">
                    <Shield className="w-5 h-5 text-blue-400 mr-2 mt-0.5 flex-shrink-0" />
                    <span>Use firewall rules for Prometheus port</span>
                  </li>
                  <li className="flex items-start">
                    <Shield className="w-5 h-5 text-blue-400 mr-2 mt-0.5 flex-shrink-0" />
                    <span>Monitor for high cardinality metrics</span>
                  </li>
                  <li className="flex items-start">
                    <Shield className="w-5 h-5 text-blue-400 mr-2 mt-0.5 flex-shrink-0" />
                    <span>Sanitize custom labels to prevent explosion</span>
                  </li>
                </ul>
              </div>
            </div>
          </div>

          {/* Grafana Dashboard */}
          <div className="bg-gradient-to-br from-purple-50 dark:from-purple-900/20 to-pink-50 dark:to-pink-900/20 rounded-2xl p-8 mb-12 border border-purple-200 dark:border-purple-800">
            <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-6 flex items-center">
              <LineChart className="w-8 h-8 mr-3 text-purple-500" />
              Grafana Dashboard
            </h2>

            <div className="space-y-4">
              <div>
                <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-3">Import Dashboard</h3>
                <ol className="space-y-2 text-gray-700 dark:text-gray-300">
                  <li>1. Import the <a href="https://grafana.com/dashboards/zetian-smtp" className="text-purple-600 dark:text-purple-400 hover:underline">Zetian SMTP Dashboard</a></li>
                  <li>2. Configure Prometheus data source</li>
                  <li>3. Set server variables</li>
                </ol>
              </div>

              <div>
                <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-3">Key Panels</h3>
                <div className="grid grid-cols-2 gap-4">
                  {[
                    'Overview - Sessions, messages, errors, uptime',
                    'Connections - Active/peak connections, TLS usage',
                    'Performance - Latency percentiles, throughput graphs',
                    'Commands - Execution counts and durations',
                    'Authentication - Success rates by mechanism',
                    'Rejections - Reasons and trends',
                  ].map((panel, i) => (
                    <div key={i} className="flex items-center gap-2 text-sm text-gray-700 dark:text-gray-300">
                      <Hash className="h-4 w-4 text-purple-400" />
                      <span>{panel}</span>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
