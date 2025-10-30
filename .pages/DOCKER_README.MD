# Docker Deployment Guide for Zetian Documentation

## Prerequisites

- Docker Engine 20.10+ installed
- Docker Compose 2.0+ installed
- (Optional) SSL certificates for HTTPS

## Quick Start

### 1. Production Deployment

Build and run the production container:

```bash
# Build the image
docker-compose build app

# Start the service
docker-compose up -d app

# Check logs
docker-compose logs -f app

# Stop the service
docker-compose down
```

The application will be available at `http://localhost:6745`

### 2. Production with Nginx (HTTPS)

To run with Nginx reverse proxy and SSL:

```bash
# Create SSL directory and add your certificates
mkdir ssl
cp /path/to/your/cert.pem ssl/
cp /path/to/your/key.pem ssl/

# Start with Nginx
docker-compose --profile with-nginx up -d

# The application will be available at https://your-domain.com
```

## Docker Commands

### Build Commands

```bash
# Build production image
docker build -t zetian-docs:latest .

# Build with specific tag
docker build -t zetian-docs:v1.0.0 .

# Build without cache
docker build --no-cache -t zetian-docs:latest .
```

### Run Commands

```bash
# Run standalone container
docker run -p 6745:6745 zetian-docs:latest

# Run with environment variables
docker run -p 6745:6745 \
  -e NODE_ENV=production \
  -e NEXT_TELEMETRY_DISABLED=1 \
  zetian-docs:latest

# Run in background
docker run -d -p 6745:6745 --name zetian-docs zetian-docs:latest
```

### Management Commands

```bash
# View running containers
docker ps

# View all containers
docker ps -a

# Stop container
docker stop zetian-docs

# Remove container
docker rm zetian-docs

# View logs
docker logs zetian-docs

# Follow logs
docker logs -f zetian-docs

# Execute command in container
docker exec -it zetian-docs sh

# View image size
docker images | grep zetian-docs
```

## Docker Compose Commands

```bash
# Start all services
docker-compose up -d

# Stop all services
docker-compose down

# Restart services
docker-compose restart

# View service logs
docker-compose logs -f [service-name]

# Rebuild and restart
docker-compose up -d --build

# Remove all containers and volumes
docker-compose down -v

# Scale service
docker-compose up -d --scale app=3
```

## Environment Variables

Create a `.env` file for environment variables:

```env
# Application
NODE_ENV=production
PORT=6745
NEXT_TELEMETRY_DISABLED=1

# Custom variables
API_URL=https://api.example.com
```

## Health Check

The application includes a health check endpoint:

```bash
# Check health from host
curl http://localhost:6745/api/health

# Check health inside container
docker exec zetian-docs curl http://localhost:6745/api/health
```

## Troubleshooting

### Container won't start

```bash
# Check logs
docker-compose logs app

# Check container status
docker ps -a

# Inspect container
docker inspect zetian-docs
```

### Port already in use

```bash
# Find process using port 6745
netstat -ano | findstr :6745  # Windows
lsof -i :6745                  # Linux/Mac

# Use different port
docker run -p 6746:6745 zetian-docs:latest
```

### Build failures

```bash
# Clean Docker cache
docker system prune -a

# Remove node_modules and rebuild
rm -rf node_modules
docker-compose build --no-cache
```

### Out of space

```bash
# Check Docker disk usage
docker system df

# Clean unused resources
docker system prune -a --volumes
```

## Performance Optimization

### 1. Multi-stage Build
The Dockerfile uses multi-stage build to minimize image size:
- Stage 1: Install dependencies
- Stage 2: Build application
- Stage 3: Production runtime (minimal)

### 2. Layer Caching
Dependencies are copied separately to leverage Docker layer caching.

### 3. Standalone Mode
Next.js standalone output reduces the final image size significantly.

### 4. Health Checks
Built-in health checks ensure container reliability.

## Security Best Practices

1. **Run as non-root user**: The container runs as `nextjs` user (UID 1001)
2. **Minimal base image**: Using Alpine Linux for smaller attack surface
3. **No unnecessary packages**: Only required dependencies are installed
4. **Environment isolation**: Sensitive data via environment variables
5. **Regular updates**: Keep base images and dependencies updated

## CI/CD Integration

### GitHub Actions Example

```yaml
name: Docker Build and Push

on:
  push:
    branches: [main]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      
      - name: Build and push Docker image
        uses: docker/build-push-action@v2
        with:
          context: ./pages
          push: true
          tags: zetian-docs:latest
```

### GitLab CI Example

```yaml
docker-build:
  image: docker:latest
  services:
    - docker:dind
  script:
    - docker build -t zetian-docs:latest ./pages
    - docker push zetian-docs:latest
```

## Monitoring

### Container Metrics

```bash
# CPU and Memory usage
docker stats zetian-docs

# Detailed resource usage
docker top zetian-docs
```

### Logging

```bash
# JSON logs
docker logs --details zetian-docs

# Export logs
docker logs zetian-docs > app.log 2>&1
```

## Backup and Recovery

### Backup container state

```bash
# Create image from running container
docker commit zetian-docs zetian-docs-backup:$(date +%Y%m%d)

# Export container
docker export zetian-docs > zetian-docs-backup.tar
```

### Restore from backup

```bash
# Import container
docker import zetian-docs-backup.tar zetian-docs:restored

# Run restored container
docker run -d -p 6745:6745 zetian-docs:restored
```

## Support

For issues or questions:
- Documentation: https://zetian.soferity.com
- GitHub Issues: https://github.com/Taiizor/Zetian/issues