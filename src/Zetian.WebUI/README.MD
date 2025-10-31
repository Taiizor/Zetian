# Zetian.WebUI - Web Management Interface for SMTP

[![NuGet-Version](https://img.shields.io/nuget/v/Zetian.WebUI.svg?label=NuGet)](https://www.nuget.org/packages/Zetian.WebUI)
[![NuGet-Download](https://img.shields.io/nuget/dt/Zetian.WebUI?label=Download)](https://www.nuget.org/api/v2/package/Zetian.WebUI)
[![License](https://img.shields.io/github/license/Taiizor/Zetian.svg?label=License)](https://github.com/Taiizor/Zetian/blob/develop/LICENSE)

A modern, responsive web-based management interface for Zetian SMTP Server with real-time monitoring, REST API, and comprehensive administration features.

## ⚡ Features

- 📊 **Real-time Dashboard** - Live server metrics and statistics
- 📨 **Message Management** - View, search, and manage email queue
- 👥 **Session Monitoring** - Track active SMTP sessions
- 🔐 **Authentication Management** - Manage users and credentials
- 📈 **Performance Metrics** - Charts and graphs for server performance
- 🛠️ **Configuration Editor** - Modify server settings on-the-fly
- 📝 **Log Viewer** - Real-time log streaming with filtering
- 🔄 **WebSocket Support** - Real-time updates via SignalR
- 🌐 **REST API** - Comprehensive API for automation
- 🎨 **Modern UI** - Responsive design with dark mode support
- 🔒 **Secure Access** - JWT authentication and role-based access

## 📦 Installation

```bash
# Install Zetian SMTP Server (required)
dotnet add package Zetian

# Install WebUI Extension
dotnet add package Zetian.WebUI
```

## 🚀 Quick Start

### Basic Setup

```csharp
using Zetian.Server;
using Zetian.WebUI.Extensions;

// Create SMTP server
var server = SmtpServerBuilder.CreateBasic();

// Enable WebUI on port 8080
var webUI = server.EnableWebUI(8080);

// Start both services
await server.StartAsync();
await webUI.StartAsync();

// Access at http://localhost:8080
Console.WriteLine("WebUI available at http://localhost:8080");
```

### With Authentication

```csharp
// Enable WebUI with authentication
var webUI = server.EnableWebUI(options =>
{
    options.Port = 8080;
    options.RequireAuthentication = true;
    options.AdminUsername = "admin";
    options.AdminPassword = "secure_password";
    options.EnableApiKey = true;
    options.ApiKey = "your-api-key-here";
});

await webUI.StartAsync();
```

### Custom Configuration

```csharp
// Advanced WebUI configuration
var webUI = server.EnableWebUI(options =>
{
    options.Port = 8443;
    options.UseHttps = true;
    options.Certificate = certificate;
    options.BasePath = "/admin";
    options.EnableSwagger = true;
    options.EnableCors = true;
    options.CorsOrigins = new[] { "https://example.com" };
    options.MaxLogEntries = 10000;
    options.EnableMetricsEndpoint = true;
    options.SessionTimeout = TimeSpan.FromHours(2);
});
```

## 🎯 Dashboard Features

### Real-time Metrics
- Active connections count
- Messages per second
- Total messages processed
- Error rate monitoring
- Memory and CPU usage
- Network throughput

### Session Management
```csharp
// Monitor sessions via WebUI API
GET /api/sessions          // List all active sessions
GET /api/sessions/{id}     // Get specific session details
POST /api/sessions/{id}/disconnect  // Disconnect a session
```

### Message Queue
```csharp
// Manage messages via API
GET /api/messages          // List messages
GET /api/messages/{id}     // Get message details
DELETE /api/messages/{id}  // Delete message
POST /api/messages/resend  // Resend message
```

## 📊 REST API

### Authentication
```bash
# Login to get JWT token
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"password"}'

# Use token in subsequent requests
curl -X GET http://localhost:8080/api/server/status \
  -H "Authorization: Bearer {token}"
```

### Server Control
```csharp
POST /api/server/start     // Start SMTP server
POST /api/server/stop      // Stop SMTP server
POST /api/server/restart   // Restart server
GET /api/server/status     // Get server status
GET /api/server/config     // Get configuration
PUT /api/server/config     // Update configuration
```

### Statistics
```csharp
GET /api/stats/overview    // General statistics
GET /api/stats/messages    // Message statistics
GET /api/stats/errors      // Error statistics
GET /api/stats/performance // Performance metrics
GET /api/stats/history     // Historical data
```

## 🔄 WebSocket Events

### Real-time Updates via SignalR

```javascript
// Connect to SignalR hub
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/smtp")
    .build();

// Subscribe to events
connection.on("SessionCreated", (session) => {
    console.log("New session:", session);
});

connection.on("MessageReceived", (message) => {
    console.log("New message:", message);
});

connection.on("MetricsUpdate", (metrics) => {
    updateDashboard(metrics);
});

// Start connection
await connection.start();
```

### Available Events
- `SessionCreated`
- `SessionCompleted`
- `MessageReceived`
- `ErrorOccurred`
- `MetricsUpdate`
- `ConfigurationChanged`
- `AuthenticationAttempt`

## 🛠️ Configuration Options

```csharp
public class WebUIOptions
{
    // Server settings
    public int Port { get; set; } = 8080;
    public string? HostName { get; set; }
    public string BasePath { get; set; } = "/";
    public bool UseHttps { get; set; } = false;
    public X509Certificate2? Certificate { get; set; }
    
    // Authentication
    public bool RequireAuthentication { get; set; } = true;
    public string AdminUsername { get; set; } = "admin";
    public string AdminPassword { get; set; } = "admin";
    public bool EnableApiKey { get; set; } = false;
    public string? ApiKey { get; set; }
    public TimeSpan SessionTimeout { get; set; } = TimeSpan.FromHours(1);
    
    // Features
    public bool EnableSwagger { get; set; } = true;
    public bool EnableSignalR { get; set; } = true;
    public bool EnableMetricsEndpoint { get; set; } = true;
    public bool EnableLogViewer { get; set; } = true;
    
    // CORS
    public bool EnableCors { get; set; } = false;
    public string[]? CorsOrigins { get; set; }
    
    // Limits
    public int MaxLogEntries { get; set; } = 5000;
    public int MaxSessionHistory { get; set; } = 1000;
    public int MetricsRetentionDays { get; set; } = 7;
}
```

## 🎨 UI Customization

### Themes
```csharp
// Configure UI theme
webUI.ConfigureUI(ui =>
{
    ui.Theme = "dark";           // "light", "dark", "auto"
    ui.PrimaryColor = "#007bff";
    ui.LogoUrl = "/assets/logo.png";
    ui.Title = "My SMTP Server";
    ui.ShowFooter = true;
});
```

### Custom Pages
```csharp
// Add custom pages
webUI.AddCustomPage("/custom", "Custom Page", async context =>
{
    await context.Response.WriteAsync("Custom content");
});
```

## 📱 Responsive Design

The WebUI is fully responsive and works on:
- 🖥️ Desktop browsers
- 📱 Mobile devices
- 📱 Tablets
- 📺 Large displays

## 🔒 Security Features

- JWT-based authentication
- API key support
- HTTPS/TLS support
- CORS configuration
- Rate limiting
- Session management
- Role-based access control

## 📈 Monitoring Endpoints

### Prometheus Metrics
```
GET /metrics           # Prometheus-compatible metrics
```

### Health Check
```
GET /health           # Basic health check
GET /health/ready     # Readiness probe
GET /health/live      # Liveness probe
```

## 🌐 API Documentation

When Swagger is enabled, access the API documentation at:
```
http://localhost:8080/swagger
```

## 📚 Advanced Usage

### Custom Middleware
```csharp
webUI.ConfigureApp(app =>
{
    app.UseMiddleware<CustomMiddleware>();
});
```

### Custom Controllers
```csharp
webUI.AddController<CustomSmtpController>();
```

### Event Handlers
```csharp
webUI.OnClientConnected += (sender, args) =>
{
    Console.WriteLine($"Client connected: {args.ClientId}");
};
```

## 🏗️ Architecture

```
Zetian.WebUI/
├── Api/                 # REST API controllers
├── Hubs/               # SignalR hubs
├── Services/           # Business logic
├── Middleware/         # Custom middleware
├── Models/             # Data models
├── Options/           # Configuration
├── wwwroot/           # Static files
│   ├── css/          # Stylesheets
│   ├── js/           # JavaScript
│   └── index.html    # SPA entry point
└── Extensions/        # Extension methods
```

## 📋 Requirements

- .NET 8.0, 9.0, or 10.0
- Zetian SMTP Server package
- Modern web browser
- (Optional) Reverse proxy for production

## 🚀 Production Deployment

### With Docker
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY . .
EXPOSE 25 8080
ENTRYPOINT ["dotnet", "YourApp.dll"]
```

### With Nginx
```nginx
server {
    listen 80;
    server_name smtp.example.com;
    
    location / {
        proxy_pass http://localhost:8080;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
    }
}
```

## 📚 Documentation & Support

- **Issues**: [GitHub Issues](https://github.com/Taiizor/Zetian/issues)
- **Examples**: [GitHub Examples](https://github.com/Taiizor/Zetian/tree/develop/examples)
- **Discussions**: [GitHub Discussions](https://github.com/Taiizor/Zetian/discussions)
- **Documentation**: [Zetian Documentation](https://zetian.soferity.com)

## 📄 License

MIT License - see [LICENSE](https://github.com/Taiizor/Zetian/blob/develop/LICENSE)