# Zetian SMTP Stress Testing Framework

A professional SMTP server stress testing framework with real-time monitoring via Prometheus and Grafana.

## 📋 Features

- **Docker-based**: Easy deployment with Docker Compose
- **User-friendly Configuration**: Simple YAML configuration files
- **Real-time Monitoring**: Prometheus metrics and Grafana dashboards
- **Configurable Load Generator**: Flexible client with multiple test scenarios
- **Unrestricted SMTP Server**: High-performance test server with no artificial limits

## 🚀 Quick Start

### 1. Start all services:
```bash
docker-compose up -d
```

### 2. Run a test:
```bash
docker-compose run client
```

### 3. View metrics:
- Prometheus: http://localhost:9090
- Grafana: http://localhost:3000 (admin/admin)

## 📁 Project Structure

```
stress/
├── server/                 # SMTP test server
│   ├── Server.cs          # Main server implementation
│   ├── Metrics.cs         # Prometheus metrics
│   ├── Server.csproj      # Project file
│   └── Dockerfile         # Docker image
│
├── client/                 # Load generator
│   ├── Client.cs          # Load generator implementation
│   ├── Scenarios.cs       # Test scenarios
│   ├── Client.csproj      # Project file
│   └── Dockerfile         # Docker image
│
├── monitoring/            # Monitoring stack
│   ├── prometheus/
│   │   └── prometheus.yml # Prometheus configuration
│   └── grafana/
│       ├── datasources/   # Data source configs
│       └── dashboards/    # Dashboard definitions
│
├── config/                # User configurations
│   ├── server.yml         # Server settings
│   └── client.yml         # Client test scenarios
│
├── docker-compose.yml     # Docker orchestration
└── README.md             # This file
```

## ⚙️ Configuration

### Server Configuration (config/server.yml)
```yaml
smtp:
  Port: 25
  EnableTls: false
  MaxConnections: unlimited
  MaxMessageSize: unlimited
  
metrics:
  Enabled: true
  Port: 9100
```

### Client Configuration (config/client.yml)
```yaml
target:
  Host: server
  Port: 25
  
scenario:
  Type: throughput        # throughput|concurrent|burst|sustained
  Duration: 60            # seconds
  Connections: 10         # parallel connections
  Rate: 1000             # messages per second
  
message:
  Size: 1024             # bytes
  Recipients: 1          # per message
```

## 📊 Test Scenarios

1. **Burst Test**: Sudden load spikes
2. **Sustained Test**: Long-duration constant load
3. **Concurrent Test**: Multiple parallel connections
4. **Throughput Test**: Maximum messages per second

## 📈 Metrics

### Server Metrics
- Error rate
- Queue size
- Processing time
- Connection count
- Messages per second
- Total messages received

### Client Metrics
- Throughput
- Success rate
- Messages sent
- Response time
- Connection errors

## 🛠️ Development

### Build locally:
```bash
# Server
cd server && dotnet build

# Client
cd client && dotnet build
```

### Run without Docker:
```bash
# Server
cd server && dotnet run

# Client
cd client && dotnet run
```