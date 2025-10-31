# Zetian SMTP Cluster Deployment Guide

This guide provides instructions for deploying a Zetian SMTP cluster in various environments.

## Quick Start

### Local Development with Docker Compose

```bash
# Start a 3-node cluster with monitoring
docker-compose up -d

# Check cluster status
docker-compose ps

# View logs
docker-compose logs -f smtp-node-1

# Scale up/down
docker-compose up -d --scale smtp-node=5

# Stop cluster
docker-compose down
```

### Kubernetes Deployment

```bash
# Create namespace and deploy
kubectl apply -f kubernetes-deployment.yaml

# Check deployment status
kubectl get pods -n zetian-smtp

# View cluster logs
kubectl logs -n zetian-smtp -l app=zetian-smtp -f

# Scale the cluster
kubectl scale statefulset zetian-smtp -n zetian-smtp --replicas=5

# Access SMTP service
kubectl get service -n zetian-smtp zetian-smtp-service
```

## Architecture

### Components

1. **SMTP Nodes**: Clustered Zetian SMTP servers
2. **Load Balancer**: HAProxy for traffic distribution
3. **State Store**: Redis for distributed state (optional)
4. **Monitoring**: Prometheus + Grafana for metrics
5. **Health Checks**: Built-in health endpoints

### Network Topology

```
Internet
    |
[Load Balancer]
    |
    +--- [Node 1] <---> [Node 2] <---> [Node 3]
    |                       |
[Prometheus]            [Redis]
    |
[Grafana]
```

## Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| NODE_ID | Unique node identifier | Generated |
| SMTP_PORT | SMTP service port | 25 |
| CLUSTER_PORT | Cluster communication port | 7946 |
| HEALTH_PORT | Health check port | 8080 |
| CLUSTER_SECRET | Shared secret for authentication | Required |
| DISCOVERY_METHOD | Node discovery method | Static |
| SEED_NODES | Comma-separated seed nodes | Empty |
| LOG_LEVEL | Logging level | Information |
| ENABLE_METRICS | Enable Prometheus metrics | true |

### Cluster Options

```csharp
var cluster = await server.EnableClusteringAsync(options =>
{
    // Basic Settings
    options.NodeId = "node-1";
    options.ClusterPort = 7946;
    
    // Discovery
    options.DiscoveryMethod = DiscoveryMethod.Kubernetes;
    
    // Replication
    options.ReplicationFactor = 3;
    options.MinReplicasForWrite = 2;
    
    // Load Balancing
    options.LoadBalancing.Strategy = LoadBalancingStrategy.LeastConnections;
    options.LoadBalancing.Affinity.Enabled = true;
    
    // Health Checks
    options.HealthCheck.CheckInterval = TimeSpan.FromSeconds(10);
    options.HealthCheck.FailureThreshold = 3;
});
```

## Monitoring

### Prometheus Metrics

Access metrics at `http://node:8080/metrics`

Key metrics:
- `zetian_cluster_nodes_total`: Total nodes in cluster
- `zetian_cluster_sessions_active`: Active sessions
- `zetian_cluster_messages_total`: Total messages processed
- `zetian_cluster_node_health`: Node health status
- `zetian_cluster_leader_elections_total`: Leader elections

### Grafana Dashboards

1. Access Grafana at `http://localhost:3000`
2. Default credentials: admin/admin
3. Import dashboard from `grafana-dashboards/` directory

### Health Endpoints

- `/health` - Overall health status
- `/health/livez` - Liveness probe
- `/health/readyz` - Readiness probe
- `/metrics` - Prometheus metrics

## High Availability

### Leader Election

The cluster uses Raft consensus for leader election:
- Automatic leader election on startup
- Re-election on leader failure
- Configurable election timeout

### Session Migration

Sessions are automatically migrated when:
- A node enters maintenance mode
- A node fails unexpectedly
- Manual rebalancing is triggered

### Failover

Automatic failover features:
- Detection time: < 10 seconds
- Migration time: < 5 seconds per session
- Zero message loss with replication

## Load Balancing

### Available Strategies

1. **Round Robin**: Even distribution
2. **Least Connections**: Route to least loaded
3. **Weighted Round Robin**: Capacity-based routing
4. **IP Hash**: Client IP-based affinity
5. **Resource Based**: CPU/memory aware routing

### Session Affinity

Configure sticky sessions:
```csharp
options.LoadBalancing.Affinity = new AffinityOptions
{
    Enabled = true,
    Method = AffinityMethod.SourceIp,
    SessionTimeout = TimeSpan.FromMinutes(30)
};
```

## Maintenance Operations

### Rolling Updates

```bash
# Put node in maintenance mode
kubectl exec -n zetian-smtp zetian-smtp-0 -- curl -X POST http://localhost:8080/maintenance/enter

# Perform update
kubectl set image statefulset/zetian-smtp smtp-server=zetian/smtp-cluster:v2 -n zetian-smtp

# Exit maintenance mode
kubectl exec -n zetian-smtp zetian-smtp-0 -- curl -X POST http://localhost:8080/maintenance/exit
```

### Backup & Restore

```bash
# Backup cluster state
kubectl exec -n zetian-smtp zetian-smtp-0 -- /app/backup.sh

# Restore from backup
kubectl exec -n zetian-smtp zetian-smtp-0 -- /app/restore.sh backup-20240101.tar.gz
```

## Troubleshooting

### Common Issues

#### Nodes not joining cluster
- Check network connectivity between nodes
- Verify CLUSTER_SECRET matches
- Check firewall rules for cluster port

#### High memory usage
- Adjust replication factor
- Enable state compression
- Implement state TTL

#### Leader election issues
- Ensure odd number of nodes
- Check network latency
- Adjust election timeout

### Debug Mode

Enable debug logging:
```csharp
cluster.EnableDebugLogging(options =>
{
    options.LogLevel = LogLevel.Debug;
    options.IncludeHeartbeats = true;
    options.LogToFile = "/var/log/cluster-debug.log";
});
```

### Diagnostics

```bash
# Check cluster status
curl http://node:8080/cluster/status

# View node list
curl http://node:8080/cluster/nodes

# Get cluster metrics
curl http://node:8080/metrics | grep zetian_cluster
```

## Performance Tuning

### Network Optimization
- Use dedicated network for cluster traffic
- Enable compression for cross-region deployments
- Tune TCP keep-alive settings

### Resource Allocation
- Minimum: 256MB RAM, 0.25 CPU per node
- Recommended: 1GB RAM, 1 CPU per node
- Production: 2-4GB RAM, 2+ CPU per node

### Scaling Guidelines
- Up to 10 nodes: Single region deployment
- 10-50 nodes: Multi-region with regional leaders
- 50+ nodes: Hierarchical clustering

## Security

### TLS Configuration

```csharp
options.EnableEncryption = true;
options.TlsCertificate = LoadCertificate("cluster-cert.pfx");
```

### Network Policies

Kubernetes NetworkPolicy restricts traffic:
- SMTP: Public access
- Cluster: Internal only
- Metrics: Monitoring namespace only

### Secrets Management

Use external secret stores:
- Kubernetes Secrets
- HashiCorp Vault
- AWS Secrets Manager
- Azure Key Vault

## Support

- **Documentation**: https://zetian.soferity.com
- **Issues**: https://github.com/Taiizor/Zetian/issues
- **Discussions**: https://github.com/Taiizor/Zetian/discussions