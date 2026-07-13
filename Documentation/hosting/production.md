# Production Hosting

Chronicle is designed for production deployment using Docker containers with MongoDB as the primary storage backend. The production setup provides a scalable, reliable event store suitable for enterprise workloads.

## Docker Image

Chronicle is distributed as a Docker image available on Docker Hub:

```bash
# Latest stable version
docker pull cratis/chronicle:latest

# Specific version (recommended for production)
docker pull cratis/chronicle:1.0.0
```

[![Docker](https://img.shields.io/docker/v/cratis/chronicle?label=Chronicle&logo=docker&sort=semver)](https://hub.docker.com/r/cratis/chronicle)

## Configuration

Chronicle requires configuration to define its runtime behavior. For complete configuration details, see the [Configuration](configuration/index.md) guide.

The configuration file must be mounted into the container at `/app/chronicle.json`, or you can use environment variables with the `Cratis__Chronicle__` prefix.

## Port Configuration

Chronicle exposes the following ports:

| Port  | Service           | Description                              |
|-------|-------------------|------------------------------------------|
| 11111 | Orleans Silo      | Internal Orleans clustering              |
| 30000 | Orleans Gateway   | Client connections to Orleans cluster    |
| 35000 | Main Service      | gRPC (HTTP/2) plus REST API, Workbench, OAuth, and health checks (HTTP/1.1), multiplexed over one TLS port |

> **Note**: With TLS enabled (the default), port 35000 serves both HTTP/2 (gRPC) and HTTP/1.1 over TLS, so it **requires** a certificate. In production you must supply one via `Tls:CertificatePath` (and `Tls:CertificatePassword` if the certificate is protected). To run in cleartext instead — for example when TLS is terminated upstream by a load balancer or ingress — set `Tls:Enabled` to `false`; gRPC then moves to h2c on port 35000 and the HTTP/1.1 surface to the `ManagementPort` (8080 by default). See [TLS Configuration](configuration/tls.md).

## Docker Deployment

### Basic Docker Run

```bash
docker run -d \
  --name chronicle \
  -p 35000:35000 \
  -v /path/to/chronicle.json:/app/chronicle.json:ro \
  cratis/chronicle:latest
```

### Docker Compose

```yaml
version: '3.8'

services:
  chronicle:
    image: cratis/chronicle:latest
    ports:
      - "35000:35000"
    volumes:
      - ./chronicle.json:/app/chronicle.json:ro
    depends_on:
      - mongodb
    restart: unless-stopped

  mongodb:
    image: mongo:7
    ports:
      - "27017:27017"
    volumes:
      - mongodb_data:/data/db
    restart: unless-stopped

volumes:
  mongodb_data:
```

## Best Practices

1. **Use specific version tags** instead of `latest` for production deployments
2. **Mount configuration as read-only** (`-v /path/to/chronicle.json:/app/chronicle.json:ro`)
3. **Use environment-specific connection strings** for MongoDB
4. **Configure appropriate timeouts** based on your infrastructure (see [Configuration](configuration/index.md))
5. **Enable health checks** for container orchestration
6. **Set up monitoring** for all exposed ports
7. **Use secrets management** for sensitive configuration values
8. **Enable TLS** with proper certificates (see [TLS Configuration](configuration/tls.md))

## Health Checks

Chronicle exposes a health check endpoint at `/health` by default. Add health checks to your Docker deployment:

```dockerfile
HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
  CMD curl -fk https://localhost:35000/health || exit 1
```

> **Warning**: A **plain-HTTP** health check against the TLS port fails at the TLS record layer with `"Wrong version number"` — the port speaks TLS, not cleartext HTTP. Probe the TLS port over HTTPS (as above, `-k` skips CA verification) or with a TCP-connect check. When TLS is terminated upstream (`Tls:Enabled=false`), probe `/health` over plain HTTP on the `ManagementPort` instead. To keep the data plane on TLS while still offering a cleartext probe, set `HealthPort` to expose `/health` on a dedicated plaintext port. See [TLS Configuration](configuration/tls.md#load-balancer-and-health-check-probes).
>
> **Note**: The health check endpoint path is configurable. See [Root Properties](configuration/root-properties.md#health-check-endpoint) for details.

## Security Considerations

- **Network Isolation**: Run Chronicle in a private network with MongoDB
- **Connection Encryption**: Use TLS for all connections in production
- **Access Control**: Implement proper firewall rules for exposed ports
- **Secrets Management**: Use external secret management for sensitive configuration
- **Regular Updates**: Keep Chronicle and MongoDB images updated
- **TLS Certificates**: Configure valid TLS certificates for production (see [TLS Configuration](configuration/tls.md))

## Scaling

Chronicle supports horizontal scaling through Orleans clustering:

1. **Multiple Instances**: Deploy multiple Chronicle containers
2. **Load Balancing**: Use a load balancer for API traffic (port 35000)
3. **Orleans Clustering**: Ensure Orleans ports (11111, 30000) are accessible between instances
4. **Shared Storage**: All instances must connect to the same MongoDB cluster
