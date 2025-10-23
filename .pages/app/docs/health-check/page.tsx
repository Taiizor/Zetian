'use client';

import Link from 'next/link';
import { 
  Heart, 
  Activity, 
  CheckCircle,
  AlertTriangle,
  XCircle,
  Server,
  Code2,
  Gauge,
  Database,
  Globe,
  Shield
} from 'lucide-react';
import CodeBlock from '@/components/CodeBlock';

const basicExample = `using Zetian.Server;
using Zetian.HealthCheck.Extensions;

// Create SMTP server
var server = new SmtpServerBuilder()
    .Port(25)
    .Build();

// Enable health check on port 8080
var healthCheck = server.EnableHealthCheck(8080);

// Start server with health check
await server.StartWithHealthCheckAsync(8080);

// Access health endpoints:
// http://localhost:8080/health
// http://localhost:8080/health/livez
// http://localhost:8080/health/readyz`;

const customChecksExample = `using Zetian.Server;
using System.Diagnostics;
using StackExchange.Redis;
using Microsoft.Data.SqlClient;
using Zetian.HealthCheck.Models;
using Zetian.HealthCheck.Extensions;

// Create SMTP server
var server = new SmtpServerBuilder()
    .Port(25)
    .Build();

await server.StartAsync();

// Enable health check on port 8080
var healthCheck = server.EnableHealthCheck(8080);

// Add custom health checks
healthCheck.AddHealthCheck("database", async (ct) =>
{
    try
    {
        // Check database connection
        using var connection = new SqlConnection("Server=...;Database=...;User ID=...;Password=...;");
        await connection.OpenAsync(ct);

        // Check response time
        var sw = Stopwatch.StartNew();
        await connection.OpenAsync();
        sw.Stop();

        if (sw.ElapsedMilliseconds > 1000)
        {
            return HealthCheckResult.Degraded(
                $"Database slow: {sw.ElapsedMilliseconds}ms");
        }

        return HealthCheckResult.Healthy("Database is responsive");
    }
    catch (Exception ex)
    {
        return HealthCheckResult.Unhealthy(
            "Cannot connect to database", ex);
    }
});

// Add Redis health check
healthCheck.AddHealthCheck("redis", async (ct) =>
{
    try
    {
        var redis = ConnectionMultiplexer.Connect("localhost:6379");
        var db = redis.GetDatabase();
        await db.PingAsync();

        var info = new Dictionary<string, object>
        {
            ["connected_clients"] = redis.GetCounters().TotalOutstanding,
            ["memory_usage_mb"] = GC.GetTotalMemory(false) / (1024 * 1024)
        };

        return HealthCheckResult.Healthy("Redis connected", info);
    }
    catch (Exception ex)
    {
        // Redis is not critical, mark as degraded
        return HealthCheckResult.Degraded("Redis unavailable", ex);
    }
});

// Add disk space check
healthCheck.AddHealthCheck("disk_space", async (ct) =>
{
    var drive = new DriveInfo("C");
    var freeSpaceGB = drive.AvailableFreeSpace / (1024 * 1024 * 1024);

    if (freeSpaceGB < 1)
    {
        return HealthCheckResult.Unhealthy($"Low disk space: {freeSpaceGB:F2} GB");
    }
    else if (freeSpaceGB < 5)
    {
        return HealthCheckResult.Degraded($"Disk space warning: {freeSpaceGB:F2} GB");
    }

    return HealthCheckResult.Healthy($"Disk space OK: {freeSpaceGB:F2} GB");
});

// Start health check
await healthCheck.StartAsync();

// Access health endpoints:
// http://localhost:8080/health
// http://localhost:8080/health/livez
// http://localhost:8080/health/readyz`;

const bindingExample = `using System.Net;
using Zetian.Server;
using Zetian.HealthCheck.Extensions;
using Zetian.HealthCheck.Options;

// Bind to localhost (default)
var healthCheck = server.EnableHealthCheck(8080);

// Bind to specific IP
var healthCheck = server.EnableHealthCheck(
    IPAddress.Parse("192.168.1.100"), 8080);

// Bind to specific hostname
var healthCheck = server.EnableHealthCheck(
    "myserver.local", 8080);

// Bind to all interfaces (Docker/Kubernetes)
var healthCheck = server.EnableHealthCheck("0.0.0.0", 8080);

// IPv6 support
var healthCheck = server.EnableHealthCheck(
    IPAddress.IPv6Loopback, 8080);

// Custom service options
var serviceOptions = new HealthCheckServiceOptions
{
    // Define HTTP prefixes to listen on
    Prefixes = new() { "http://+:8080/health/" }, // Listen on all interfaces
    DegradedStatusCode = 200 // HTTP status code for degraded state
};

// SMTP health check options
var smtpOptions = new SmtpHealthCheckOptions
{
    CheckMemoryUsage = true,
    DegradedThresholdPercent = 60,   // 60% utilization = degraded
    UnhealthyThresholdPercent = 85   // 85% utilization = unhealthy
};

var healthCheck = server.EnableHealthCheck(serviceOptions, smtpOptions);`;

const kubernetesExample = `# Kubernetes deployment with health checks
apiVersion: apps/v1
kind: Deployment
metadata:
  name: smtp-server
spec:
  template:
    spec:
      containers:
      - name: smtp-server
        image: myregistry/smtp-server:latest
        ports:
        - containerPort: 25  # SMTP
        - containerPort: 8080 # Health check
        
        # Liveness probe - restart if unhealthy
        livenessProbe:
          httpGet:
            path: /livez
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
          timeoutSeconds: 5
          failureThreshold: 3
        
        # Readiness probe - remove from load balancer if not ready
        readinessProbe:
          httpGet:
            path: /readyz
            port: 8080
          initialDelaySeconds: 10
          periodSeconds: 5
          timeoutSeconds: 3
          failureThreshold: 2`;

const responseExample = `// Sample JSON response from /health endpoint
{
  "status": "Healthy",  // or "Degraded" or "Unhealthy"
  "timestamp": "2025-10-23T06:39:09.1509714+00:00",
  "checks": {
    "smtp_server": {
      "status": "Healthy",
      "description": "SMTP server is healthy",
      "data": {
        "status": "running",
        "uptime": "0d 0h 0m 11s",
        "endpoint": "0.0.0.0:25",
        "configuration": {
          "serverName": "Zetian SMTP Server",
          "maxConnections": 100,
          "maxMessageSize": 10485760,
          "requireAuthentication": false,
          "requireSecureConnection": false
        },
        "activeSessions": 0,
        "maxSessions": 100,
        "utilizationPercent": 0,
        "memory": {
          "workingSet": 49422336,
          "privateMemory": 16596992,
          "virtualMemory": 2237691518976,
          "gcTotalMemory": 2494752
        }
      }
    },
    "database": {
      "status": "Healthy",
      "description": "Database is responsive",
      "duration": "00:00:00.0234567"
    },
    "redis": {
      "status": "Degraded",
      "description": "Redis unavailable",
      "duration": "00:00:00.1000000",
      "exception": "Connection timeout"
    }
  }
}`;

export default function HealthCheckPage() {
  return (
    <div className="min-h-screen py-12 bg-gray-50 dark:bg-gray-950">
      <div className="container mx-auto px-4 max-w-5xl">
        {/* Header */}
        <div className="mb-12">
          <div className="flex items-center gap-2 text-sm text-gray-600 dark:text-gray-400 mb-4">
            <Link href="/docs" className="hover:text-primary-600 dark:hover:text-primary-400">
              Documentation
            </Link>
            <span>/</span>
            <span>Health Check</span>
          </div>
          
          <h1 className="text-4xl font-bold text-gray-900 dark:text-white mb-4">
            Health Check System
          </h1>
          <p className="text-xl text-gray-600 dark:text-gray-400">
            Monitor your SMTP server health with built-in HTTP endpoints and custom checks.
          </p>
        </div>

        {/* Health Status Overview */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-12">
          <div className="bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg p-4">
            <div className="flex items-center gap-2 mb-2">
              <CheckCircle className="h-5 w-5 text-green-600 dark:text-green-400" />
              <h3 className="font-semibold text-green-900 dark:text-green-100">Healthy</h3>
            </div>
            <p className="text-sm text-green-800 dark:text-green-200">
              All components working normally. System is fully operational.
            </p>
          </div>
          
          <div className="bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg p-4">
            <div className="flex items-center gap-2 mb-2">
              <AlertTriangle className="h-5 w-5 text-yellow-600 dark:text-yellow-400" />
              <h3 className="font-semibold text-yellow-900 dark:text-yellow-100">Degraded</h3>
            </div>
            <p className="text-sm text-yellow-800 dark:text-yellow-200">
              Some components affected but system is functional.
            </p>
          </div>
          
          <div className="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg p-4">
            <div className="flex items-center gap-2 mb-2">
              <XCircle className="h-5 w-5 text-red-600 dark:text-red-400" />
              <h3 className="font-semibold text-red-900 dark:text-red-100">Unhealthy</h3>
            </div>
            <p className="text-sm text-red-800 dark:text-red-200">
              Critical components failed. System needs attention.
            </p>
          </div>
        </div>

        {/* Basic Setup */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
            <Heart className="h-6 w-6" />
            Basic Setup
          </h2>
          
          <p className="text-gray-600 dark:text-gray-400 mb-4">
            Enable health monitoring with a single line of code:
          </p>
          
          <CodeBlock 
            code={basicExample}
            language="csharp"
            filename="BasicHealthCheck.cs"
          />

          <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mt-6">
            <div className="flex items-start gap-3">
              <Activity className="h-5 w-5 text-blue-500 mt-1" />
              <div>
                <h4 className="font-medium text-gray-900 dark:text-white">/health</h4>
                <p className="text-sm text-gray-600 dark:text-gray-400">
                  Complete health status with all checks
                </p>
              </div>
            </div>
            
            <div className="flex items-start gap-3">
              <Gauge className="h-5 w-5 text-green-500 mt-1" />
              <div>
                <h4 className="font-medium text-gray-900 dark:text-white">/health/livez</h4>
                <p className="text-sm text-gray-600 dark:text-gray-400">
                  Liveness probe for container orchestration
                </p>
              </div>
            </div>
            
            <div className="flex items-start gap-3">
              <Shield className="h-5 w-5 text-purple-500 mt-1" />
              <div>
                <h4 className="font-medium text-gray-900 dark:text-white">/health/readyz</h4>
                <p className="text-sm text-gray-600 dark:text-gray-400">
                  Readiness probe for load balancing
                </p>
              </div>
            </div>
          </div>
        </section>

        {/* Custom Health Checks */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
            <Code2 className="h-6 w-6" />
            Custom Health Checks
          </h2>
          
          <p className="text-gray-600 dark:text-gray-400 mb-4">
            Add custom checks for databases, external services, and system resources:
          </p>
          
          <CodeBlock 
            code={customChecksExample}
            language="csharp"
            filename="CustomHealthChecks.cs"
          />
        </section>

        {/* Binding Options */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
            <Globe className="h-6 w-6" />
            Binding Options
          </h2>
          
          <p className="text-gray-600 dark:text-gray-400 mb-4">
            Configure how the health check HTTP service binds to network interfaces:
          </p>
          
          <CodeBlock 
            code={bindingExample}
            language="csharp"
            filename="HealthCheckBinding.cs"
          />
        </section>

        {/* Response Format */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
            <Database className="h-6 w-6" />
            Response Format
          </h2>
          
          <p className="text-gray-600 dark:text-gray-400 mb-4">
            Health check endpoints return detailed JSON responses:
          </p>
          
          <CodeBlock 
            code={responseExample}
            language="json"
            filename="health-response.json"
          />

          <div className="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-6 mt-6">
            <div className="flex items-start gap-3">
              <Activity className="h-5 w-5 text-blue-600 dark:text-blue-400 mt-0.5" />
              <div>
                <h4 className="font-semibold text-blue-900 dark:text-blue-100 mb-2">HTTP Status Codes</h4>
                <ul className="space-y-1 text-sm text-blue-800 dark:text-blue-200">
                  <li>• <code>200 OK</code> - Healthy status</li>
                  <li>• <code>206 Partial Content</code> - Degraded status</li>
                  <li>• <code>503 Service Unavailable</code> - Unhealthy status</li>
                </ul>
              </div>
            </div>
          </div>
        </section>

        {/* Kubernetes Integration */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
            <Server className="h-6 w-6" />
            Kubernetes Integration
          </h2>
          
          <p className="text-gray-600 dark:text-gray-400 mb-4">
            Use health checks with Kubernetes liveness and readiness probes:
          </p>
          
          <CodeBlock 
            code={kubernetesExample}
            language="yaml"
            filename="kubernetes-deployment.yaml"
          />
        </section>

        {/* Best Practices */}
        <section className="mb-12">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-4">
            Best Practices
          </h2>
          
          <div className="space-y-4">
            <div className="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 p-6">
              <h3 className="font-semibold text-gray-900 dark:text-white mb-3">
                1. Separate Liveness and Readiness
              </h3>
              <p className="text-gray-600 dark:text-gray-400">
                Use <code>/livez</code> to check if the process is alive (restart if fails).
                Use <code>/readyz</code> to check if ready for traffic (remove from load balancer if fails).
              </p>
            </div>
            
            <div className="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 p-6">
              <h3 className="font-semibold text-gray-900 dark:text-white mb-3">
                2. Set Appropriate Timeouts
              </h3>
              <p className="text-gray-600 dark:text-gray-400">
                Configure reasonable timeouts for health checks. Too short may cause false positives,
                too long delays detection of real issues.
              </p>
            </div>
            
            <div className="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 p-6">
              <h3 className="font-semibold text-gray-900 dark:text-white mb-3">
                3. Use Degraded Status
              </h3>
              <p className="text-gray-600 dark:text-gray-400">
                Return degraded status for non-critical components. This signals issues without
                triggering restarts or removing from load balancer.
              </p>
            </div>
            
            <div className="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 p-6">
              <h3 className="font-semibold text-gray-900 dark:text-white mb-3">
                4. Security Considerations
              </h3>
              <p className="text-gray-600 dark:text-gray-400">
                Set <code>DetailedErrors = false</code> in production to avoid exposing sensitive
                information. Consider adding authentication for health endpoints if needed.
              </p>
            </div>
          </div>
        </section>

        {/* Next Steps */}
        <div className="mt-12 flex gap-4">
          <Link 
            href="/docs/configuration"
            className="flex-1 p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 hover:shadow-md transition-all group"
          >
            <div className="flex items-center justify-between">
              <div>
                <h4 className="font-medium text-gray-900 dark:text-white group-hover:text-blue-600 dark:group-hover:text-blue-400">
                  ← Configuration
                </h4>
                <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">
                  Server configuration options
                </p>
              </div>
              <Server className="h-5 w-5 text-gray-400" />
            </div>
          </Link>
          
          <Link 
            href="/examples"
            className="flex-1 p-4 bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 hover:shadow-md transition-all group"
          >
            <div className="flex items-center justify-between">
              <div>
                <h4 className="font-medium text-gray-900 dark:text-white group-hover:text-blue-600 dark:group-hover:text-blue-400">
                  Examples →
                </h4>
                <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">
                  See more code examples
                </p>
              </div>
              <Code2 className="h-5 w-5 text-gray-400" />
            </div>
          </Link>
        </div>
      </div>
    </div>
  );
}
