---
title: Configure the Cratis Chronicle Kernel in Docker
description: Configure Chronicle in Docker with a read-only chronicle.json mount and environment overrides, then supply the production image's required certificates.
---
# Docker Configuration

Configure the Chronicle Kernel in Docker by mounting `chronicle.json` at `/app/chronicle.json`, passing `Cratis__Chronicle__` environment variables, or combining both. Environment variables override matching file settings, so you can keep shared configuration in the file and supply deployment-specific values separately.

The commands below show how configuration reaches the container, not a complete production start. With the default TLS settings, the production image needs a TLS certificate as well as an encryption certificate and stops at startup without them. Configure and mount those certificates before starting; [Production hosting](../production.md) covers the requirements and the supported TLS alternatives.

## Example configuration

```json
{
  "port": 35000,
  "storage": {
    "type": "MongoDB",
    "connectionDetails": "mongodb://mongo:27017"
  }
}
```

| Property | Type | Description |
| --- | --- | --- |
| port | number | The single Chronicle port exposed by the container — gRPC (HTTP/2) and HTTP/1.1 |
| storage.type | string | Storage provider type |
| storage.connectionDetails | string | MongoDB connection string used in the container network |

The hostname `mongo` must resolve to a reachable MongoDB instance from inside the container. These examples do not create MongoDB or attach the container to its network.

## Using Configuration File

Save the example as `chronicle.json`, then mount it as a read-only volume. Replace `/path/to/chronicle.json` with its host path:

```bash
docker run -d \
  --name chronicle \
  -p 35000:35000 \
  -v /path/to/chronicle.json:/app/chronicle.json:ro \
  cratis/chronicle:latest
```

## Using Environment Variables

Use `-e` for each environment variable. Nested JSON properties become double-underscore-separated names; for example, `storage.connectionDetails` becomes `Cratis__Chronicle__Storage__ConnectionDetails`:

```bash
docker run -d \
  --name chronicle \
  -e Cratis__Chronicle__Port=35000 \
  -e Cratis__Chronicle__HealthCheckEndpoint=/health \
  -e Cratis__Chronicle__Storage__Type=MongoDB \
  -e Cratis__Chronicle__Storage__ConnectionDetails=mongodb://mongo:27017 \
  -e Cratis__Chronicle__Observers__SubscriberTimeout=10 \
  -p 35000:35000 \
  cratis/chronicle:latest
```

When using both sources, check the `-e` values first if a file change appears to have no effect. [Configuration Precedence](configuration-precedence.md) explains the override order.

For a repeatable deployment, replace `latest` with a specific image version and supply credentials through your platform's secret mechanism. Continue with [Running with Docker Compose](../docker-compose.md) to configure the services together.

