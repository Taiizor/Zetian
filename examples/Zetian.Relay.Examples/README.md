# Zetian.Relay Examples

This directory contains comprehensive examples demonstrating various features and capabilities of the Zetian.Relay extension for SMTP relay and proxy functionality.

## 📚 Available Examples

### 1. **BasicRelayExample**
- Basic relay setup and configuration
- Sending messages through relay
- Queue monitoring and statistics

### 2. **SmartHostExample**
- Configuring smart hosts for relay
- Authentication with smart hosts
- TLS/SSL configuration
- Real-time monitoring

### 3. **FailoverExample**
- Multiple smart hosts configuration
- Automatic failover to backup servers
- Priority-based server selection
- Retry mechanisms

### 4. **DomainRoutingExample**
- Domain-specific routing rules
- Different smart hosts per domain
- Local vs relay domain configuration
- Custom routing tables

### 5. **QueueManagementExample**
- Queue operations (list, remove, reschedule)
- Queue statistics and monitoring
- Message status tracking
- Expired message cleanup

### 6. **PriorityQueueExample**
- Priority-based message queuing
- Different priority levels (Urgent, High, Normal, Low)
- Priority-based delivery order
- Visual monitoring

### 7. **MxRoutingExample**
- DNS MX record-based routing
- Direct delivery to recipient domains
- Custom DNS server configuration
- Fallback smart host on MX failure

### 8. **LoadBalancingExample**
- Weight-based load distribution
- Multiple smart hosts with same priority
- Connection pooling and limits
- Traffic distribution monitoring

### 9. **AuthenticationExample**
- Authentication requirements for relay
- Relay networks (IPs allowed without auth)
- AUTH PLAIN and LOGIN mechanisms
- Access control based on authentication

### 10. **CustomConfigurationExample**
- Complete relay configuration
- RelayBuilder fluent API
- All configuration options
- Production-ready setup

## 🚀 Running the Examples

### Prerequisites
- .NET 6.0 or later
- Administrator privileges may be required for some port bindings

### Build and Run

```bash
# Build the project
dotnet build

# Run the examples
dotnet run
```

This will display an interactive menu where you can select which example to run.

### Running Individual Examples

You can also run specific examples directly:

```csharp
// In your code
await BasicRelayExample.RunAsync(loggerFactory);
await SmartHostExample.RunAsync(loggerFactory);
// ... etc
```

## 📋 Example Features

Each example demonstrates specific aspects of the relay functionality:

| Example | Key Features |
|---------|-------------|
| **Basic** | Simple relay setup, queue monitoring |
| **Smart Host** | External SMTP server configuration, authentication |
| **Failover** | Multiple servers, automatic failover |
| **Domain Routing** | Custom routes per domain |
| **Queue Management** | Queue operations, statistics |
| **Priority Queue** | Message prioritization |
| **MX Routing** | DNS-based routing |
| **Load Balancing** | Traffic distribution |
| **Authentication** | Access control, security |
| **Custom Config** | Advanced configuration |

## 🔧 Configuration Options

### Smart Host Configuration
```csharp
config.DefaultSmartHost = new SmartHostConfiguration
{
    Host = "smtp.example.com",
    Port = 587,
    Credentials = new NetworkCredential("user", "password"),
    UseTls = true,
    UseStartTls = true,
    MaxConnections = 10,
    MaxMessagesPerConnection = 100
};
```

### Domain Routing
```csharp
config.DomainRouting["gmail.com"] = new SmartHostConfiguration
{
    Host = "smtp.gmail.com",
    Port = 587,
    Credentials = new NetworkCredential("user", "app_password")
};
```

### Load Balancing
```csharp
// Multiple smart hosts with same priority but different weights
config.SmartHosts.Add(new SmartHostConfiguration
{
    Host = "smtp1.example.com",
    Priority = 10,
    Weight = 50  // 50% of traffic
});
```

## 📊 Monitoring

All examples include monitoring capabilities:

- Real-time queue statistics
- Message delivery tracking
- Smart host distribution
- Priority queue visualization
- Load balancing metrics

## 🛠️ Troubleshooting

### Common Issues

1. **Port Already in Use**
   - Change the port number in the example
   - Stop other services using the same port

2. **Authentication Failed**
   - Verify credentials are correct
   - Check authentication mechanism compatibility

3. **Connection Timeout**
   - Verify smart host addresses
   - Check firewall settings
   - Increase timeout values

4. **Relay Access Denied**
   - Ensure proper authentication
   - Check relay network configuration
   - Verify domain routing rules