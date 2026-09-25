# Docker Configuration

When running Chronicle in Docker, you can configure it using either mounted configuration files or environment variables.

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

## Using Configuration File

The commands below show how configuration reaches the container, not a complete production start. The production image also needs a TLS certificate and an encryption certificate, and stops at startup without them — see [Production hosting](../production.md).


Mount the `chronicle.json` file as a read-only volume:

```bash
docker run -d \
  --name chronicle \
  -p 35000:35000 \
  -v /path/to/chronicle.json:/app/chronicle.json:ro \
  cratis/chronicle:latest
```

## Using Environment Variables

Pass configuration via environment variables using the `-e` flag:

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

For Docker Compose setups, see [Running with Docker Compose](../docker-compose.md).

