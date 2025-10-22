# Zetian.HealthCheck - Health Monitoring for SMTP

[![NuGet-Version](https://img.shields.io/nuget/v/Zetian.HealthCheck.svg?label=NuGet)](https://www.nuget.org/packages/Zetian.HealthCheck)
[![NuGet-Download](https://img.shields.io/nuget/dt/Zetian.HealthCheck?label=Download)](https://www.nuget.org/api/v2/package/Zetian.HealthCheck)
[![License](https://img.shields.io/github/license/Taiizor/Zetian.svg?label=License)](https://github.com/Taiizor/Zetian/blob/develop/LICENSE)

A lightweight health monitoring extension for Zetian SMTP Server, providing HTTP endpoints for health checks, liveness probes, readiness checks, and custom health metrics. Perfect for Kubernetes, Docker Swarm, load balancers, and monitoring systems.

## ⚡ Features

- 🔄 **Real-time Status** - Live server health monitoring
- 📈 **JSON Response** - Structured health data for monitoring tools
- ☸️ **Kubernetes Ready** - Built for K8s liveness and readiness probes
- 🔍 **Multiple Endpoints** - `/health/`, `/health/livez`, `/health/readyz`
- 🌐 **Flexible Binding** - Support for localhost, specific IPs, hostnames, IPv6
- 🚀 **Zero Dependencies** - Lightweight, uses only .NET built-in HttpListener
- 🎯 **Custom Checks** - Add your own health checks (database, disk, services)
- 📊 **Detailed Metrics** - Server uptime, active sessions, memory usage, utilization

## 📦 Installation

```bash
# Install Zetian SMTP Server (required)
dotnet add package Zetian

# Install Health Check Extension
dotnet add package Zetian.HealthCheck
```

## 🚀 Quick Start

### Basic Health Check

```csharp
using Zetian;
using Zetian.HealthCheck.Extensions;

// Create and start SMTP server
var server = SmtpServerBuilder.CreateBasic();
await server.StartAsync();

// Enable health check on port 8080
var healthCheck = server.EnableHealthCheck(8080);
await healthCheck.StartAsync();

// Health check now available at:
// http://localhost:8080/health/
// http://localhost:8080/health/livez
// http://localhost:8080/health/readyz
```

### Start Server with Health Check

```csharp
// Start both SMTP and health check together
var healthCheck = await server.StartWithHealthCheckAsync(8080);
```

## 🛠️ Advanced Configuration

### Custom Port and Path

```csharp
// Custom port
var healthCheck = server.EnableHealthCheck(9090);

// Custom path (must end with /)
var healthCheck = server.EnableHealthCheck(8080, "/status/");
```

### IP/Hostname Binding

```csharp
// Bind to all interfaces (accessible externally)
var healthCheck = server.EnableHealthCheck("0.0.0.0", 8080);

// Bind to hostname
var healthCheck = server.EnableHealthCheck("myserver.local", 8080);

// IPv6 support
var healthCheck = server.EnableHealthCheck(IPAddress.IPv6Loopback, 8080);

// Bind to specific IP
var healthCheck = server.EnableHealthCheck(IPAddress.Parse("192.168.1.100"), 8080);
```

### Custom Health Checks

```csharp
var healthCheck = server.EnableHealthCheck(8080);

// Add database check
healthCheck.AddHealthCheck("database", async (ct) =>
{
    try
    {
        var isConnected = await CheckDatabaseAsync();
        return isConnected 
            ? HealthCheckResult.Healthy("Database connected")
            : HealthCheckResult.Unhealthy("Database connection failed");
    }
    catch (Exception ex)
    {
        return HealthCheckResult.Unhealthy("Database error", ex);
    }
});

// Add disk space check
healthCheck.AddHealthCheck("disk_space", async (ct) =>
{
    var drive = new DriveInfo("C:\\");
    var freePercent = (double)drive.AvailableFreeSpace / drive.TotalSize * 100;
    
    var data = new Dictionary<string, object>
    {
        ["freePercent"] = Math.Round(freePercent, 2),
        ["totalSpaceGB"] = drive.TotalSize / (1024 * 1024 * 1024),
        ["freeSpaceGB"] = drive.AvailableFreeSpace / (1024 * 1024 * 1024)
    };
    
    if (freePercent < 10)
        return HealthCheckResult.Unhealthy($"Critical: {freePercent:F2}% free", data: data);
    if (freePercent < 20)
        return HealthCheckResult.Degraded($"Low space: {freePercent:F2}% free", data: data);
    
    return HealthCheckResult.Healthy($"Disk space OK: {freePercent:F2}% free", data: data);
});

await healthCheck.StartAsync();
```

### Advanced Options

```csharp
// Custom thresholds for SMTP health
var smtpOptions = new SmtpHealthCheckOptions
{
    CheckMemoryUsage = true,       // Include memory metrics
    DegradedThresholdPercent = 60, // Degraded at 60% utilization
    UnhealthyThresholdPercent = 85 // Unhealthy at 85% utilization
};

// Custom service options
var serviceOptions = new HealthCheckServiceOptions
{
    Prefixes = new() { "http://+:8080/health/" },  // Listen on all interfaces
    DegradedStatusCode = 218  // Custom HTTP status for degraded (default: 200)
};

var healthCheck = server.EnableHealthCheck(serviceOptions, smtpOptions);
await healthCheck.StartAsync();
```

## 📊 Response Format

### Healthy Response (HTTP 200)
```json
{
  "status": "Healthy",
  "timestamp": "2024-10-22T16:30:00Z",
  "checks": {
    "smtp_server": {
      "status": "Healthy",
      "description": "SMTP server is healthy",
      "data": {
        "status": "running",
        "uptime": "2d 3h 15m 42s",
        "activeSessions": 5,
        "maxSessions": 50,
        "utilizationPercent": 10.0,
        "memoryUsageMB": 45.2
      }
    },
    "database": {
      "status": "Healthy",
      "description": "Database connected"
    },
    "disk_space": {
      "status": "Healthy",
      "description": "Disk space OK: 65.3% free",
      "data": {
        "freeSpaceGB": 250,
        "totalSpaceGB": 500,
        "freePercent": 50.0
      }
    }
  }
}
```

### Degraded Response (HTTP 200 or custom)
```json
{
  "status": "Degraded",
  "timestamp": "2024-10-22T16:30:00Z",
  "checks": {
    "smtp_server": {
      "status": "Degraded",
      "description": "SMTP server utilization is high",
      "data": {
        "activeSessions": 35,
        "maxSessions": 50,
        "utilizationPercent": 70.0
      }
    }
  }
}
```

### Unhealthy Response (HTTP 503)
```json
{
  "status": "Unhealthy",
  "timestamp": "2024-10-22T16:30:00Z",
  "checks": {
    "smtp_server": {
      "status": "Unhealthy",
      "description": "SMTP server is not running"
    }
  }
}
```

## ☸️ Kubernetes Integration

### Deployment Configuration

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: smtp-server
spec:
  replicas: 3
  selector:
    matchLabels:
      app: smtp-server
  template:
    metadata:
      labels:
        app: smtp-server
    spec:
      containers:
      - name: smtp-server
        image: myregistry/smtp-server:latest
        ports:
        - containerPort: 25   # SMTP port
        - containerPort: 8080 # Health check port
        livenessProbe:
          httpGet:
            path: /health/livez
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
          timeoutSeconds: 5
          failureThreshold: 3
        readinessProbe:
          httpGet:
            path: /health/readyz
            port: 8080
          initialDelaySeconds: 10
          periodSeconds: 5
          timeoutSeconds: 3
          failureThreshold: 2
```

### Service Configuration

```yaml
apiVersion: v1
kind: Service
metadata:
  name: smtp-service
spec:
  selector:
    app: smtp-server
  ports:
  - name: smtp
    port: 25
    targetPort: 25
  - name: health
    port: 8080
    targetPort: 8080
```

## 🐳 Docker Integration

### Dockerfile Example

```dockerfile
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY . .
EXPOSE 25 8080
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health/ || exit 1
ENTRYPOINT ["dotnet", "MySmtpServer.dll"]
```

### Docker Compose

```yaml
version: '3.8'
services:
  smtp:
    image: mysmtp:latest
    ports:
      - "25:25"
      - "8080:8080"
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health/"]
      interval: 30s
      timeout: 3s
      retries: 3
      start_period: 40s
```

## 🎯 Monitoring Integration

### Prometheus Metrics

```csharp
// Add custom metrics check for Prometheus
healthCheck.AddHealthCheck("metrics", async (ct) =>
{
    var data = new Dictionary<string, object>
    {
        ["smtp_uptime_seconds"] = server.UptimeSeconds,
        ["smtp_connections_active"] = server.SessionCount,
        ["smtp_messages_received"] = server.MessagesReceivedCount,
        ["smtp_connections_total"] = server.TotalConnectionsCount
    };
    
    return HealthCheckResult.Healthy("Metrics collected", data: data);
});
```

### Grafana/Datadog

Use the JSON endpoints to collect metrics:

```bash
# Collect health data
curl http://localhost:8080/health/ | jq '.checks.smtp_server.data'

# Check liveness
curl -f http://localhost:8080/health/livez

# Check readiness  
curl -f http://localhost:8080/health/readyz
```

## 📝 API Reference

### Extension Methods

| Method | Description |
|--------|-------------|
| `EnableHealthCheck(port)` | Enable health check on localhost |
| `AddHealthCheck(name, checkFunc)` | Add custom health check |
| `StartWithHealthCheckAsync(port)` | Start server and health check |
| `EnableHealthCheck(hostname, port)` | Enable on specific hostname |
| `EnableHealthCheck(IPAddress, port)` | Enable on specific IP |
| `EnableHealthCheck(options, healthOptions)` | Advanced configuration |

### Health Check Results

| Status | HTTP Code | Description |
|--------|-----------|-------------|
| Healthy | 200 | Everything is working |
| Degraded | 200/218 | Working but with issues |
| Unhealthy | 503 | Critical problems detected |

## 🔧 Troubleshooting

### Common Issues

**Port Already in Use**
```csharp
// Check if port is available before starting
var listener = new HttpListener();
try
{
    listener.Prefixes.Add("http://localhost:8080/");
    listener.Start();
    listener.Stop();
    // Port is available
}
catch (HttpListenerException)
{
    // Port is in use
}
```

**Access Denied on Non-Localhost**
```bash
# Windows: Run as administrator or add URL ACL
netsh http add urlacl url=http://+:8080/health/ user=Everyone

# Linux: Use authbind or run as root
sudo setcap cap_net_bind_service=+ep /usr/bin/dotnet
```

**IPv6 Issues**
```csharp
// Check IPv6 support
if (Socket.OSSupportsIPv6)
{
    var healthCheck = server.EnableHealthCheck(IPAddress.IPv6Any, 8080);
}
else
{
    var healthCheck = server.EnableHealthCheck("0.0.0.0", 8080);
}
```

## 📋 Requirements

- Windows, Linux, or macOS
- Zetian SMTP Server package
- .NET 6.0, 7.0, 8.0, 9.0, or 10.0
- Administrator privileges for non-localhost binding (Windows)

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