# Configuration

Chronicle can be configured using either a `chronicle.json` file or environment variables. Environment variables take precedence over the JSON file configuration, making them ideal for containerized deployments and different environments.

## Configuration File

Chronicle looks for a `chronicle.json` file in the application root directory. Here's a complete example configuration:

```json
{
    "managementPort": 8080,
    "port": 35000,
    "healthCheckEndpoint": "/health",
    "features": {
        "api": true,
        "workbench": true,
        "changesetStorage": false
    },
    "storage": {
        "type": "MongoDB",
        "connectionDetails": "mongodb://localhost:27017"
    },
    "observers": {
        "subscriberTimeout": 5,
        "maxRetryAttempts": 10,
        "backoffDelay": 1,
        "exponentialBackoffDelayFactor": 2,
        "maximumBackoffDelay": 600
    },
    "events": {
        "queues": 8
    }
}
```

### Configuration Properties

#### Root Properties

| Property           | Type   | Default  | Description                                      |
|--------------------|--------|----------|--------------------------------------------------|
| managementPort     | number | 8080     | Port for the Management API, Workbench, and well-known endpoints |
| port               | number | 35000    | Main gRPC service port                           |
| healthCheckEndpoint| string | /health  | Health check endpoint path                       |

#### Features

| Property         | Type    | Default | Description                                    |
|------------------|---------|---------|------------------------------------------------|
| api              | boolean | true    | Enable REST API endpoint                       |
| workbench        | boolean | true    | Enable web-based management interface          |
| changesetStorage | boolean | false   | Enable changeset storage functionality         |

> **Note**: If the API is disabled, the Workbench is also disabled as it depends on the API.

#### Storage Configuration

| Property          | Type   | Required | Description                           |
|-------------------|--------|----------|---------------------------------------|
| type              | string | Yes      | Storage type (currently "MongoDB")    |
| connectionDetails | string | Yes      | MongoDB connection string             |

#### Observer Settings

| Property                       | Type   | Default | Description                                           |
|--------------------------------|--------|---------|-------------------------------------------------------|
| subscriberTimeout              | number | 5       | Timeout in seconds for observer subscriber calls      |
| maxRetryAttempts               | number | 10      | Maximum retry attempts for failed partitions (0 = infinite) |
| backoffDelay                   | number | 1       | Initial backoff delay in seconds                      |
| exponentialBackoffDelayFactor  | number | 2       | Exponential backoff multiplier                        |
| maximumBackoffDelay            | number | 600     | Maximum backoff delay in seconds                      |

#### Events Configuration

| Property | Type   | Default | Description                          |
|----------|--------|---------|--------------------------------------|
| queues   | number | 8       | Number of appended event queues to use |

## Environment Variables

All configuration options can be set using environment variables with the prefix `Cratis__Chronicle__`. Use double underscores (`__`) to represent nested configuration sections.

### Port Configuration

```bash
# gRPC port (default: 35000)
Cratis__Chronicle__Port=35000

# Management API port (default: 8080)
Cratis__Chronicle__ManagementPort=8080
```

### Health Check Endpoint

```bash
# Health check endpoint path (default: /health)
Cratis__Chronicle__HealthCheckEndpoint=/health
```

### Feature Toggles

```bash
# Enable/disable API (default: true)
Cratis__Chronicle__Features__Api=true

# Enable/disable Workbench (default: true)
Cratis__Chronicle__Features__Workbench=true

# Enable/disable Changeset Storage (default: false)
Cratis__Chronicle__Features__ChangesetStorage=false
```

### Storage

```bash
# Storage type (e.g., "MongoDB")
Cratis__Chronicle__Storage__Type=MongoDB

# MongoDB connection string
Cratis__Chronicle__Storage__ConnectionDetails=mongodb://localhost:27017
```

### Observers

```bash
# Timeout in seconds for observer subscriber calls (default: 5)
Cratis__Chronicle__Observers__SubscriberTimeout=5

# Maximum retry attempts for failed partitions (0 = infinite, default: 10)
Cratis__Chronicle__Observers__MaxRetryAttempts=10

# Initial backoff delay in seconds (default: 1)
Cratis__Chronicle__Observers__BackoffDelay=1

# Exponential backoff factor (default: 2)
Cratis__Chronicle__Observers__ExponentialBackoffDelayFactor=2

# Maximum backoff delay in seconds (default: 600)
Cratis__Chronicle__Observers__MaximumBackoffDelay=600
```

### Events

```bash
# Number of appended event queues to use (default: 8)
Cratis__Chronicle__Events__Queues=8
```

## Docker Configuration

When running Chronicle in Docker, you can configure it using either mounted configuration files or environment variables.

### Using Configuration File

Mount the `chronicle.json` file as a read-only volume:

```bash
docker run -d \
  --name chronicle \
  -p 8080:8080 \
  -p 35000:35000 \
  -v /path/to/chronicle.json:/app/chronicle.json:ro \
  cratis/chronicle:latest
```

### Using Environment Variables

Pass configuration via environment variables using the `-e` flag:

```bash
docker run -d \
  --name chronicle \
  -e Cratis__Chronicle__Port=35000 \
  -e Cratis__Chronicle__ManagementPort=8080 \
  -e Cratis__Chronicle__HealthCheckEndpoint=/health \
  -e Cratis__Chronicle__Storage__Type=MongoDB \
  -e Cratis__Chronicle__Storage__ConnectionDetails=mongodb://mongo:27017 \
  -e Cratis__Chronicle__Observers__SubscriberTimeout=10 \
  -p 8080:8080 \
  -p 35000:35000 \
  cratis/chronicle:latest
```

### Docker Compose

Configuration in a docker-compose file:

```yaml
version: '3.8'

services:
  chronicle:
    image: cratis/chronicle:latest
    environment:
      - Cratis__Chronicle__Port=35000
      - Cratis__Chronicle__ManagementPort=8080
      - Cratis__Chronicle__HealthCheckEndpoint=/health
      - Cratis__Chronicle__Storage__Type=MongoDB
      - Cratis__Chronicle__Storage__ConnectionDetails=mongodb://mongodb:27017
      - Cratis__Chronicle__Features__Api=true
      - Cratis__Chronicle__Features__Workbench=true
      - Cratis__Chronicle__Observers__SubscriberTimeout=10
      - Cratis__Chronicle__Observers__MaxRetryAttempts=5
      - Cratis__Chronicle__Events__Queues=8
    ports:
      - "8080:8080"
      - "35000:35000"
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

## Configuration Precedence

Configuration values are resolved in the following order (later sources override earlier ones):

1. **Default values** - Defined in code
2. **chronicle.json file** - File-based configuration
3. **Environment variables** - Variables with `Cratis__Chronicle__` prefix

This allows you to set baseline configuration in `chronicle.json` and override specific values per environment using environment variables.

## Configuration Best Practices

1. **Use specific version tags** instead of `latest` for production deployments
2. **Mount configuration as read-only** to prevent accidental modifications
3. **Use environment-specific connection strings** for MongoDB
4. **Configure appropriate timeouts** based on your infrastructure
5. **Use environment variables** for sensitive configuration like connection strings
6. **Use secrets management** for production environments
7. **Set appropriate observer retry policies** based on your reliability requirements
8. **Configure event queues** based on your event throughput needs

## Port Reference

Chronicle exposes the following ports:

| Port  | Service           | Description                              |
|-------|-------------------|------------------------------------------|
| 8080  | Management API    | REST API, Workbench, and well-known endpoints |
| 11111 | Orleans Silo      | Internal Orleans clustering              |
| 30000 | Orleans Gateway   | Client connections to Orleans cluster    |
| 35000 | Main Service      | Primary Chronicle gRPC service port      |

Ensure these ports are properly configured in your firewall and container orchestration setup.
