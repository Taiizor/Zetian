# Zetian.HealthCheck Project Structure

## 📁 Folder Organization

```
src/Zetian.HealthCheck/
│
├── 📁 Abstractions/              # Interfaces & Abstract Classes
│   └── IHealthCheck.cs          # Main health check interface
│
├── 📁 Models/                    # Data Models & DTOs
│   ├── HealthCheckResult.cs     # Result data model
│   └── HealthStatus.cs          # Health status enumeration
│
├── 📁 Options/                   # Configuration & Options
│   ├── HealthCheckServiceOptions.cs    # Service configuration
│   └── SmtpHealthCheckOptions.cs       # SMTP-specific options
│
├── 📁 Services/                  # Core Services
│   └── HealthCheckService.cs    # Main HTTP health check service
│
├── 📁 Checks/                    # Concrete Health Check Implementations
│   └── SmtpServerHealthCheck.cs # SMTP server health check implementation
│
├── 📁 Extensions/                # Extension Methods
│   └── HealthCheckExtensions.cs # Extension methods for ISmtpServer
│
├── 📁 Properties/                # Assembly Properties
│
├── 📁 Resources/                 # Resource Files
│
├── Zetian.HealthCheck.csproj    # Project File
└── README.md                     # Project Documentation
```

## 🏗️ Architecture Overview

### Abstractions Layer
- Clean separation of contracts from implementations
- **IHealthCheck**: Core interface that all health checks must implement

### Models Layer
- **HealthStatus**: Enum defining health states (Healthy, Degraded, Unhealthy)
- **HealthCheckResult**: Result model containing status, description, and additional data

### Options Layer
- Follows the Options Pattern for configuration
- **HealthCheckServiceOptions**: HTTP listener configuration
- **SmtpHealthCheckOptions**: SMTP-specific thresholds and settings

### Services Layer
- Supports custom path prefixes
- Handles routing for /health, /livez, /readyz endpoints
- **HealthCheckService**: HTTP endpoint service using HttpListener

### Checks Layer
- Additional health checks can be added here
- Reports server status, active sessions, memory usage, etc.
- **SmtpServerHealthCheck**: Concrete implementation for SMTP server monitoring

### Extensions Layer
- Multiple overloads for different binding scenarios
- **HealthCheckExtensions**: Fluent API for enabling health checks

## 📝 Namespace Organization

```csharp
// Abstractions
namespace Zetian.HealthCheck.Abstractions

// Concrete Implementations
namespace Zetian.HealthCheck.Checks

// Extension Methods
namespace Zetian.HealthCheck.Extensions

// Models
namespace Zetian.HealthCheck.Models

// Options
namespace Zetian.HealthCheck.Options

// Services
namespace Zetian.HealthCheck.Services
```

## 🎯 Design Principles

1. **Testability**: All components are easily unit testable
2. **Options Pattern**: Configuration separated from logic
3. **Interface Segregation**: Clean interfaces in Abstractions
4. **Separation of Concerns**: Each folder has a specific responsibility
5. **Extensibility**: Easy to add new health checks by implementing IHealthCheck
6. **Dependency Inversion**: Services depend on abstractions, not concrete implementations

## 🔧 Usage Example

```csharp
using Zetian.HealthCheck.Options;
using Zetian.HealthCheck.Extensions;

// Basic usage
var healthCheck = server.EnableHealthCheck(8080);

// With custom options
var serviceOptions = new HealthCheckServiceOptions
{
    Prefixes = new() { "http://localhost:8080/api/health/" }
};

var smtpOptions = new SmtpHealthCheckOptions
{
    CheckMemoryUsage = true,
    DegradedThresholdPercent = 60,
    UnhealthyThresholdPercent = 85
};

var healthCheck = server.EnableHealthCheck(serviceOptions, smtpOptions);
await healthCheck.StartAsync();
```

## 🚀 Benefits of This Structure

- ✅ **Documentation**: Self-documenting structure
- ✅ **Professional**: Follows enterprise-level best practices
- ✅ **Testability**: Each component can be tested in isolation
- ✅ **Reusability**: Components can be reused across different projects
- ✅ **Maintainability**: Clear organization makes code easy to navigate
- ✅ **Scalability**: Easy to add new health checks or extend functionality