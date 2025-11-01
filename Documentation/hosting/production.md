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

## Port Configuration

Chronicle exposes the following ports:

| Port  | Service           | Description                              |
|-------|-------------------|------------------------------------------|
| 8080  | API Server        | REST API for client interactions        |
| 11111 | Orleans Silo      | Internal Orleans clustering              |
| 30000 | Orleans Gateway   | Client connections to Orleans cluster   |
| 35000 | Main Service      | Primary Chronicle service port           |

## Configuration File

Chronicle uses a `chronicle.json` configuration file to define runtime behavior. This file must be mounted into the container at `/app/chronicle.json`.

### Basic Configuration

```json
{
    "apiPort": 8080,
    "port": 35000,
    "features": {
        "api": true,
        "workbench": true,
        "changesetStorage": false
    },
    "storage": {
        "type": "MongoDB",
        "connectionDetails": "mongodb://mongodb:27017"
    },
    "observers": {
        "subscriberTimeout": 5,
        "maxRetryAttempts": 10,
        "backoffDelay": 1,
        "exponentialBackoffDelayFactor": 2,
        "maximumBackoffDelay": 600
    }
}
```

### Configuration Properties

#### Root Properties

| Property | Type   | Required | Description                    |
|----------|--------|----------|--------------------------------|
| apiPort  | number | Yes      | Port for the REST API server, and the Workbench, if enabled |
| port     | number | Yes      | Main service port              |

#### Features

| Property        | Type    | Default | Description                                    |
|-----------------|---------|---------|------------------------------------------------|
| api             | boolean | true    | Enable REST API endpoint                       |
| workbench       | boolean | true    | Enable web-based management interface         |
| changesetStorage| boolean | false   | Enable changeset storage functionality        |

#### Storage Configuration

| Property          | Type   | Required | Description                           |
|-------------------|--------|----------|---------------------------------------|
| type              | string | Yes      | Storage type (currently "MongoDB")    |
| connectionDetails | string | Yes      | MongoDB connection string             |

#### Observer Settings

| Property                      | Type   | Default | Description                                  |
|-------------------------------|--------|---------|----------------------------------------------|
| subscriberTimeout             | number | 5       | Timeout for observer subscriptions (seconds)|
| maxRetryAttempts              | number | 10      | Maximum retry attempts for failed events    |
| backoffDelay                  | number | 1       | Initial backoff delay (seconds)             |
| exponentialBackoffDelayFactor | number | 2       | Exponential backoff multiplier              |
| maximumBackoffDelay           | number | 600     | Maximum backoff delay (seconds)             |

## Docker Deployment

### Basic Docker Run

```bash
docker run -d \
  --name chronicle \
  -p 8080:8080 \
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
      - "8080:8080"
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

## Mounting Configuration

The `chronicle.json` file should be mounted as a read-only volume to ensure configuration consistency:

```bash
# Mount configuration file
-v /host/path/chronicle.json:/app/chronicle.json:ro
```

### Configuration Best Practices

1. **Use specific version tags** instead of `latest` for production deployments
2. **Mount configuration as read-only** to prevent accidental modifications
3. **Use environment-specific connection strings** for MongoDB
4. **Configure appropriate timeouts** based on your infrastructure
5. **Enable health checks** for container orchestration
6. **Set up monitoring** for all exposed ports

## Health Checks

Add health checks to your Docker deployment:

```dockerfile
HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1
```

## Security Considerations

- **Network Isolation**: Run Chronicle in a private network with MongoDB
- **Connection Encryption**: Use TLS for MongoDB connections in production
- **Access Control**: Implement proper firewall rules for exposed ports
- **Secrets Management**: Use external secret management for sensitive configuration
- **Regular Updates**: Keep Chronicle and MongoDB images updated

## Scaling

Chronicle supports horizontal scaling through Orleans clustering:

1. **Multiple Instances**: Deploy multiple Chronicle containers
2. **Load Balancing**: Use a load balancer for API traffic (port 8080)
3. **Orleans Clustering**: Ensure Orleans ports (11111, 30000) are accessible between instances
4. **Shared Storage**: All instances must connect to the same MongoDB cluster
