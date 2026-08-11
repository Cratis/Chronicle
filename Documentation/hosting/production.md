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

### What makes a build a production build

The production code paths are selected by the **`DEVELOPMENT` compile-time symbol**, not by `ASPNETCORE_ENVIRONMENT` or `DOTNET_ENVIRONMENT`. The symbol is defined for `Debug` builds only, so `cratis/chronicle:latest` — published from a Release build — always takes the production path, and no environment variable can switch it back. Only the `latest-development` and `latest-development-slim` images are compiled with the symbol.

That matters because the production path needs all three of these before it will run — and two of them stop startup dead:

| Setting | Environment variable | Why it is required |
| --- | --- | --- |
| Storage connection | `Cratis__Chronicle__Storage__ConnectionDetails` | Defaults to an empty string — there is no built-in fallback to connect to. Pair it with `Cratis__Chronicle__Storage__Type` when the backend is not MongoDB, which is what an unset type resolves to |
| TLS certificate | `Cratis__Chronicle__Tls__CertificatePath` (and `__CertificatePassword`) | Port 35000 multiplexes gRPC and HTTP/1.1 over one TLS port. Without a certificate the server throws "No TLS certificate is configured" at startup |
| Encryption certificate | `Cratis__Chronicle__EncryptionCertificate__CertificatePath` (and `__CertificatePassword`) | Protects the internal OAuth authority's signing and encryption keys. Without a certificate the server throws "An encryption certificate is required in production" at startup |

The development images generate a self-signed TLS certificate and fall back to ephemeral OAuth keys instead of throwing, which is exactly why a Compose file that works against `latest-development` fails against `latest`. See [Data Protection Key Encryption](encryption-certificate.md) and [TLS Configuration](configuration/tls.md) for generating and mounting the certificates.

## Storage requirements

Chronicle opens MongoDB transactions when it appends events, and transactions are only served by a replica set (or a sharded cluster) — a standalone `mongod` rejects them. **Every MongoDB deployment behind Chronicle must be a replica set**, including a single-node one. The Compose recipes below initialize a single-node replica set named `rs0`; a real production deployment uses a multi-member replica set or a managed service such as MongoDB Atlas, which is already one.

Chronicle's connection string carries `?directConnection=true` so the driver talks to the member it was given instead of following the replica set's advertised host name. This does not disable transactions — they keep working because MongoDB is running as a replica set.

## Port Configuration

Chronicle exposes the following ports:

| Port  | Service           | Description                              |
|-------|-------------------|------------------------------------------|
| 11111 | Orleans Silo      | Internal Orleans clustering              |
| 30000 | Orleans Gateway   | Client connections to Orleans cluster    |
| 35000 | Main Service      | gRPC (HTTP/2) plus REST API, Workbench, OAuth, and health checks (HTTP/1.1), multiplexed over one TLS port |

> **Note**: Port 35000 serves both HTTP/2 (gRPC) and HTTP/1.1 over TLS, so it **requires** a certificate. In production you must supply one via `Tls:CertificatePath` (and `Tls:CertificatePassword` if the certificate is protected). See [TLS Configuration](configuration/tls.md).

## Docker Deployment

### Basic Docker Run

```bash
docker run -d \
  --name chronicle \
  -p 35000:35000 \
  -v /path/to/chronicle.json:/app/chronicle.json:ro \
  -v /path/to/certs:/certs:ro \
  -e Cratis__Chronicle__Storage__ConnectionDetails='mongodb://mongodb:27017/?directConnection=true' \
  -e Cratis__Chronicle__Tls__CertificatePath=/certs/chronicle.pfx \
  -e Cratis__Chronicle__Tls__CertificatePassword="$CHRONICLE_CERTIFICATE_PASSWORD" \
  -e Cratis__Chronicle__EncryptionCertificate__CertificatePath=/certs/encryption-cert.pfx \
  -e Cratis__Chronicle__EncryptionCertificate__CertificatePassword="$ENCRYPTION_CERTIFICATE_PASSWORD" \
  cratis/chronicle:latest
```

Both certificates and the storage connection are mandatory — see [What makes a build a production build](#what-makes-a-build-a-production-build). The MongoDB instance this points at must be a replica set.

### Docker Compose

```yaml
services:
  chronicle:
    image: cratis/chronicle:latest
    ports:
      - "35000:35000"
    environment:
      Cratis__Chronicle__Storage__Type: "MongoDB"
      Cratis__Chronicle__Storage__ConnectionDetails: "mongodb://mongodb:27017/?directConnection=true"
      Cratis__Chronicle__Tls__CertificatePath: "/certs/chronicle.pfx"
      Cratis__Chronicle__Tls__CertificatePassword: "${CHRONICLE_CERTIFICATE_PASSWORD}"
      Cratis__Chronicle__EncryptionCertificate__CertificatePath: "/certs/encryption-cert.pfx"
      Cratis__Chronicle__EncryptionCertificate__CertificatePassword: "${ENCRYPTION_CERTIFICATE_PASSWORD}"
    volumes:
      - ./chronicle.json:/app/chronicle.json:ro
      - ./certs:/certs:ro
    depends_on:
      - mongodb
      - mongodb-init
    restart: unless-stopped

  mongodb:
    image: mongo:8
    command: ["mongod", "--replSet", "rs0", "--bind_ip_all"]
    ports:
      - "27017:27017"
    volumes:
      - mongodb_data:/data/db
    restart: unless-stopped

  mongodb-init:
    image: mongo:8
    depends_on:
      - mongodb
    restart: "no"
    command:
      - /bin/bash
      - -lc
      - |
        until mongosh --host mongodb --quiet --eval "db.adminCommand('ping')" >/dev/null 2>&1; do
          sleep 1
        done
        mongosh --host mongodb --quiet --eval "
        try {
          rs.status();
        } catch (e) {
          rs.initiate({
            _id: 'rs0',
            members: [{ _id: 0, host: 'localhost:27017' }]
          });
        }"

volumes:
  mongodb_data:
```

The `mongodb-init` service is a one-shot container: it waits for `mongod` to answer, initiates the `rs0` replica set if it is not already initiated, and exits. Advertising the member as `localhost:27017` keeps the replica set reachable from host tools such as `mongosh` and Compass, while Chronicle reaches MongoDB over the Compose network with `directConnection=true` so it does not follow that advertised host back into its own container.

If an existing MongoDB data volume was initialized with a different replica-set host, wipe it (`docker compose down -v`) before starting again so `rs.initiate()` can apply the new host.

The remaining settings stay in the mounted `chronicle.json`; the environment variables above override any values it holds for the same keys. Keep the certificate passwords out of the file — supply them from your secret store, as the `${...}` references do here.

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

Chronicle exposes a health check endpoint at `/health` by default.

> [!IMPORTANT]
> **Probe Chronicle from outside the container.** The production image is built on `mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled`, which ships no shell and no `curl` or `wget`. A Docker `HEALTHCHECK` — or a Compose `healthcheck:` — runs *inside* the container, so any command you give it fails immediately and the container is reported unhealthy no matter how healthy Chronicle is.

Use your orchestrator's own HTTP probe instead. The main port always requires TLS, and probers that validate certificates reject a self-signed one, so publish the health endpoint on a dedicated plaintext port:

```bash
Cratis__Chronicle__Health__Port=8080
Cratis__Chronicle__Health__Tls=false
```

Then point the probe at it:

```yaml
livenessProbe:
  httpGet:
    path: /health
    port: 8080
    scheme: HTTP
readinessProbe:
  httpGet:
    path: /health
    port: 8080
    scheme: HTTP
```

Treat that port as internal — it serves the other HTTP/1.1 endpoints too, so do not expose it publicly.

> **Note**: The health check endpoint path is configurable. See [Root Properties](configuration/root-properties.md#health-check-endpoint) for details, and [Health Endpoint](configuration/health-endpoint.md) for the dedicated port.

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

> [!WARNING]
> Every multi-node deployment **must** set the clustering type to `MongoDB`. The default is `Localhost`,
> which is single-node membership. Two containers left on the default that share one MongoDB do **not**
> join as one cluster - each forms its own isolated single-node cluster over the same data, a split-brain
> topology reported by no error at startup. The server logs a warning when it detects localhost clustering
> against non-local storage, but it does not refuse to start.

Set the clustering type explicitly on every node, and give every node the same cluster and service id:

```bash
Cratis__Chronicle__Clustering__Type=MongoDB
Cratis__Chronicle__Clustering__ClusterId=chronicle
Cratis__Chronicle__Clustering__ServiceId=chronicle
```

See [Clustering](configuration/clustering.md) for the full set of clustering properties.

### Two-node Docker Compose

Both nodes run the same image, connect to the same MongoDB, and set `Cratis__Chronicle__Clustering__Type=MongoDB`
so they form a single cluster over the shared storage:

```yaml
services:
  chronicle-1:
    image: cratis/chronicle:latest
    ports:
      - "35001:35000"
    environment:
      Cratis__Chronicle__Storage__ConnectionDetails: "mongodb://mongodb:27017/?directConnection=true"
      Cratis__Chronicle__Clustering__Type: "MongoDB"
      Cratis__Chronicle__Clustering__ClusterId: "chronicle"
      Cratis__Chronicle__Clustering__ServiceId: "chronicle"
      # Port 35000 requires a TLS certificate - see the TLS Configuration guide.
      Cratis__Chronicle__Tls__CertificatePath: "/certs/chronicle.pfx"
      Cratis__Chronicle__Tls__CertificatePassword: "${CHRONICLE_CERTIFICATE_PASSWORD}"
      # Every node must share the same encryption certificate.
      Cratis__Chronicle__EncryptionCertificate__CertificatePath: "/certs/encryption-cert.pfx"
      Cratis__Chronicle__EncryptionCertificate__CertificatePassword: "${ENCRYPTION_CERTIFICATE_PASSWORD}"
    volumes:
      - ./certs:/certs:ro
    depends_on:
      - mongodb
      - mongodb-init
    restart: unless-stopped

  chronicle-2:
    image: cratis/chronicle:latest
    ports:
      - "35002:35000"
    environment:
      Cratis__Chronicle__Storage__ConnectionDetails: "mongodb://mongodb:27017/?directConnection=true"
      Cratis__Chronicle__Clustering__Type: "MongoDB"
      Cratis__Chronicle__Clustering__ClusterId: "chronicle"
      Cratis__Chronicle__Clustering__ServiceId: "chronicle"
      Cratis__Chronicle__Tls__CertificatePath: "/certs/chronicle.pfx"
      Cratis__Chronicle__Tls__CertificatePassword: "${CHRONICLE_CERTIFICATE_PASSWORD}"
      Cratis__Chronicle__EncryptionCertificate__CertificatePath: "/certs/encryption-cert.pfx"
      Cratis__Chronicle__EncryptionCertificate__CertificatePassword: "${ENCRYPTION_CERTIFICATE_PASSWORD}"
    volumes:
      - ./certs:/certs:ro
    depends_on:
      - mongodb
      - mongodb-init
    restart: unless-stopped

  mongodb:
    image: mongo:8
    command: ["mongod", "--replSet", "rs0", "--bind_ip_all"]
    ports:
      - "27017:27017"
    volumes:
      - mongodb_data:/data/db
    restart: unless-stopped

  mongodb-init:
    image: mongo:8
    depends_on:
      - mongodb
    restart: "no"
    command:
      - /bin/bash
      - -lc
      - |
        until mongosh --host mongodb --quiet --eval "db.adminCommand('ping')" >/dev/null 2>&1; do
          sleep 1
        done
        mongosh --host mongodb --quiet --eval "
        try {
          rs.status();
        } catch (e) {
          rs.initiate({
            _id: 'rs0',
            members: [{ _id: 0, host: 'localhost:27017' }]
          });
        }"

volumes:
  mongodb_data:
```

Both nodes share one encryption certificate on purpose: Data Protection keys live in the shared storage, so
every instance must be able to decrypt what any other instance wrote. See
[Data Protection Key Encryption](encryption-certificate.md).

Each container reaches the others over the Compose network by service name, so the default advertised
address works. When you instead run multiple nodes directly on one host, set
`Cratis__Chronicle__Clustering__AdvertisedIP` and distinct `SiloPort`/`GatewayPort` values per node - see
[Clustering](configuration/clustering.md).
