# Zetian Health Check Examples

This project contains examples demonstrating the health check functionality of Zetian SMTP Server.

## Examples

### 1. Basic Health Check
- Simple health check setup on localhost
- All three endpoints: `/health/`, `/livez`, `/readyz`
- Custom health checks (disk space, database simulation)

### 2. Health Check with Custom Options
- Memory usage tracking
- Custom HTTP status codes
- System resource monitoring
- Custom degraded/unhealthy thresholds

### 3. Health Check with IP/Hostname Binding
- IPv6 support
- Hostname-based binding
- All interfaces binding (0.0.0.0)
- Binding to specific IP addresses

### 4. Health Check with Custom Path
- Uptime tracking
- Self-test functionality
- Performance metrics collection
- Custom base path `/status/` instead of `/health/`

### 5. Readiness Check Example
- Connection capacity monitoring
- Gradual service startup simulation
- Kubernetes-style readiness probes
- Separate readiness vs health checks
- Database, cache, and warmup checks
- Shows difference between liveness and readiness

## Running the Examples

```bash
# Build the project
dotnet build

# Run the examples
dotnet run

# Or run with admin privileges (Windows)
# Right-click on your terminal and select "Run as Administrator"
dotnet run
```

## Important Notes

### Windows Users

Some binding options require administrator privileges:

1. **Works without admin:**
   - `localhost` binding
   - `127.0.0.1` binding

2. **Requires admin or URL ACL:**
   - `0.0.0.0` (all interfaces)
   - Specific IP addresses
   - Custom hostnames

### Adding URL ACL (Windows)

To allow non-admin access to specific URLs, run this command as administrator:

```cmd
# For all interfaces on port 8080
netsh http add urlacl url=http://+:8080/health/ user=Everyone

# For specific IP
netsh http add urlacl url=http://192.168.1.100:8080/health/ user=Everyone

# To remove a reservation
netsh http delete urlacl url=http://+:8080/health/
```

### Linux/macOS Users

On Linux, you may need to allow binding to ports below 1024:

```bash
# Allow specific capability
sudo setcap cap_net_bind_service=+ep /usr/bin/dotnet

# Or run with sudo
sudo dotnet run
```

## Common Issues

### "Access Denied" Error
- **Solution:** Run as administrator or add URL ACL reservation

### "Cannot create a file when that file already exists"
- **Solution:** Another application is using the port. Change the port number.

### "The request is not supported" 
- **Solution:** The binding type may not be supported. Try using localhost instead.

## Testing the Endpoints

Once running, you can test the health check endpoints:

```bash
# Full health check
curl http://localhost:8080/health/

# Liveness probe
curl http://localhost:8080/health/livez

# Readiness probe  
curl http://localhost:8080/health/readyz
```

Or use a web browser:
- http://localhost:8080/health/
- http://localhost:8080/health/livez
- http://localhost:8080/health/readyz

## Example Output

```json
{
  "status": "Healthy",
  "timestamp": "2024-10-22T19:30:00Z",
  "checks": {
    "smtp_server": {
      "status": "Healthy",
      "description": "SMTP server is healthy",
      "data": {
        "status": "running",
        "uptime": "0d 0h 2m 15s",
        "activeSessions": 0,
        "maxSessions": 50,
        "utilizationPercent": 0.0
      }
    },
    "disk_space": {
      "status": "Healthy",
      "description": "Disk space is healthy: 45.2%",
      "data": {
        "totalSpace": 500000000000,
        "freeSpace": 226000000000,
        "freeSpacePercent": 45.2
      }
    },
    "database": {
      "status": "Healthy", 
      "description": "Database is responding well: 25ms",
      "data": {
        "responseTimeMs": 25,
        "connectionString": "simulated"
      }
    }
  }
}
```