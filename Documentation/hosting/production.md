---
title: Cratis Chronicle production hosting
description: Deploy the Cratis Chronicle production image with external storage, TLS and encryption certificates, health probes, and a restorable backup plan.
---

Deploy Cratis Chronicle's production image with external storage and configured certificates before accepting application traffic. Unlike the development image, it has no bundled database, generates no certificates, and creates no default administrator password. The examples below use MongoDB; you can also configure PostgreSQL, SQL Server, or SQLite through [storage configuration](configuration/storage.mdx).

## Docker Image

Chronicle is distributed as a Docker image available on Docker Hub:

```bash
# Latest stable version
docker pull cratis/chronicle:latest

# A specific release (recommended for production) - pin the version you tested
docker pull cratis/chronicle:19.6.1
```

[![Docker](https://img.shields.io/docker/v/cratis/chronicle?label=Chronicle&logo=docker&sort=semver)](https://hub.docker.com/r/cratis/chronicle)

## Configuration

Mount your configuration file at `/app/chronicle.json`, or supply settings through environment variables with the `Cratis__Chronicle__` prefix. Environment variables override the corresponding file settings. The [Configuration](configuration/index.md) reference covers settings beyond the deployment requirements below.

### What makes a build a production build

Certificate generation and development authentication defaults depend on the `DEVELOPMENT` compile-time symbol, not `ASPNETCORE_ENVIRONMENT` or `DOTNET_ENVIRONMENT`. Debug builds define the symbol, and the publishing workflow also passes it explicitly when building the development images in Release configuration. The production image omits it; setting a runtime environment variable cannot enable those development defaults.

Prepare these settings for the default production setup:

| Setting | Environment variable | Why it is required |
| --- | --- | --- |
| Storage connection | `Cratis__Chronicle__Storage__ConnectionDetails` | Defaults to an empty string — there is no built-in fallback to connect to. Pair it with `Cratis__Chronicle__Storage__Type` when the backend is not MongoDB, which is what an unset type resolves to |
| TLS certificate | `Cratis__Chronicle__Tls__CertificatePath` (and `__CertificatePassword`) | Required by default for port 35000 to multiplex gRPC and HTTP/1.1. Without it the server throws "No TLS certificate is configured" at startup. Explicit `Tls__Enabled=false` instead requires a private h2c backend behind an HTTPS proxy |
| Encryption certificate | `Cratis__Chronicle__EncryptionCertificate__CertificatePath` (and `__CertificatePassword`) | Protects the internal OAuth authority's signing and encryption keys. Without a certificate the server throws "An encryption certificate is required in production" at startup — unless the internal authority is not in play, see below |

The internal OAuth authority issues authentication tokens and needs the encryption certificate. Turning the `oAuthAuthority` feature off, or setting `Cratis__Chronicle__Authentication__Authority` to an external authority, removes that startup requirement. See [Features](configuration/features.md). It does not remove the need to protect stored data: without a certificate, Data Protection writes keys unencrypted and webhook encryption throws `EncryptionCertificateNotConfigured` on first use.

A configured encryption `CertificatePath` must name a readable PFX containing its private key. A missing file stops startup with `EncryptionCertificateFileNotFound`; Chronicle does not fall back to running without it. See [What a broken ring does](encryption-certificate.md#what-a-broken-ring-does).

Before changing a Compose file from `latest-development` to `latest`, configure the certificates that the development image supplies or substitutes automatically. Follow [Data Protection Key Encryption](encryption-certificate.md) for encryption keys and [TLS Configuration](configuration/tls.md) for the listener certificate.

## Storage requirements

Chronicle uses MongoDB transactions to append events and change streams to follow storage changes. A standalone `mongod` cannot provide these operations. Use a replica set, even for a single-node installation; MongoDB sharded clusters also support these operations.

The Compose examples initialize a single-node replica set named `rs0`. Their connection strings include `?directConnection=true` so Chronicle connects to the supplied address instead of following the member's advertised hostname, `localhost:27017`, into its own container. This does not disable transactions. For a production database, plan replication and failover with a multi-member replica set or a managed service rather than relying on this single-node example.

## Port Configuration

Chronicle exposes the following ports:

| Port  | Service           | Description                              |
|-------|-------------------|------------------------------------------|
| 11111 | Orleans Silo      | Internal Orleans clustering              |
| 30000 | Orleans Gateway   | Client connections to Orleans cluster    |
| 35000 | Main Service      | gRPC (HTTP/2) plus REST API, Workbench, OAuth, and health checks (HTTP/1.1), multiplexed over one TLS port |

> **Note**: By default port 35000 serves HTTP/2 (gRPC) and HTTP/1.1 over TLS and requires a certificate in production. Alternatively set `Tls:Enabled=false` explicitly for a **private** HTTP/2-only h2c backend behind an HTTPS-terminating reverse proxy. The proxy must forward all traffic using h2c, and clients and browsers must use the public HTTPS endpoint. With authentication enabled, configure an external HTTPS `Authentication:Authority` and forward the public HTTPS scheme. See [TLS Configuration](configuration/tls.md).

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

This command assumes `mongodb` resolves to a reachable replica set from inside the container; it does not create the database or its network. Mount the configuration and certificate files before starting, and supply the two password variables from your secret store. See [What makes a build a production build](#what-makes-a-build-a-production-build) for the TLS and external-authority alternatives.

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
    image: mongo:8.2
    command: ["mongod", "--replSet", "rs0", "--bind_ip_all"]
    # This MongoDB has no authentication. Publish it on loopback at most, for host tools.
    ports:
      - "127.0.0.1:27017:27017"
    volumes:
      - mongodb_data:/data/db
    restart: unless-stopped

  mongodb-init:
    image: mongo:8.2
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

The `mongodb-init` service is a one-shot container: it waits for `mongod` to answer, initiates the `rs0` replica set if it is not already initiated, and exits. This single-node MongoDB is a starting point, not a hardened database: it runs without authentication, so it is published on loopback only. A real deployment enables MongoDB authentication and TLS, or uses a managed replica set, and puts the credentials in the connection string from your secret store. Advertising the member as `localhost:27017` keeps the replica set reachable from host tools such as `mongosh` and Compass, while Chronicle reaches MongoDB over the Compose network with `directConnection=true` so it does not follow that advertised host back into its own container.

If an existing volume has a different replica-set hostname, reconfigure the replica set for the intended network before using this recipe. The initializer does not change an existing configuration. Only for disposable local data, `docker compose down -v` removes the volumes so a new replica set can be initialized; it deletes stored data and is not a production migration procedure.

The remaining settings stay in the mounted `chronicle.json`; the environment variables above override any values it holds for the same keys. Keep the certificate passwords out of the file — supply them from your secret store, as the `${...}` references do here.

## Best Practices

Pin the Chronicle and database image versions you have tested rather than deploying floating `latest` tags. Mount configuration read-only and supply database credentials and certificate passwords through your secret-management system.

Configure probes as described below before enabling automated restarts or rollouts. Set timeouts for your infrastructure using the [Configuration](configuration/index.md) reference, and monitor readiness, storage connectivity, and startup failures.

Size memory and GC for your container as described in [Container resources](container-resources.md).

## Health Checks

Chronicle binds its listeners before starting the Orleans silo. `/health/live` returns 200 while the process is running and accepting requests, including during startup. `/health` and `/health/ready` return 503 until the silo has completed startup and the host has started, and become unready when shutdown begins. Existing storage health checks also contribute to readiness.

While unready, non-health requests receive HTTP 503 with `Retry-After`, or gRPC `Unavailable`. This includes the Workbench and static files. Background catch-up does not delay readiness after startup completes.

> [!IMPORTANT]
> Probe Chronicle from outside the container. The production image uses
> `mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra`, without a shell, `curl`, or `wget`.
> Docker `HEALTHCHECK` and Compose `healthcheck:` commands run inside the container, so probes that
> invoke those tools fail even when Chronicle is healthy.

Use your orchestrator's own HTTP probe instead. The default TLS main port may use a self-signed certificate that some probers reject; a private h2c main port requires an HTTP/2-capable prober. Publish a dedicated plaintext HTTP/1.1 health port when either presents a problem:

```bash
Cratis__Chronicle__Health__Port=8080
Cratis__Chronicle__Health__Tls=false
```

Then point the probe at it:

```yaml
startupProbe:
  tcpSocket:
    port: 35000
livenessProbe:
  httpGet:
    path: /health/live
    port: 8080
    scheme: HTTP
readinessProbe:
  httpGet:
    path: /health
    port: 8080
    scheme: HTTP
```

You can instead use an HTTP startup probe on `/health/live` at port 8080. Do not use a TCP probe for readiness: an open listener does not mean the silo is ready. Raising `failureThreshold` is not the fix for backlog-dependent startup restarts; startup and liveness must probe the listener, not readiness.

Treat that port as internal — it serves the other HTTP/1.1 endpoints too, so do not expose it publicly.

> **Note**: The health check endpoint path is configurable. See [Root Properties](configuration/root-properties.md#health-check-endpoint) for details, and [Health Endpoint](configuration/health-endpoint.md) for the dedicated port.

## Backup and restore

Back up the encryption certificates alongside the database: the database contains protected keys and credentials, not the private keys needed to read them. If you use a separate compliance key store for personal data, include that too. Restore in this order before serving traffic:

| Order | What | Where it lives |
| --- | --- | --- |
| 1 | The encryption-certificate ring — every certificate and its password, in the shape the ring had **when the backup was taken** | Your secret store |
| 2 | The storage backend (MongoDB or SQL) — event store data, the Data Protection key ring, webhook definitions and their encrypted credentials, OAuth applications and tokens | Your database backups |
| 3 | The compliance key store, when one is configured, at the same point in time as (2) | Vault, Azure Key Vault, or the general storage |

The Data Protection key ring lives *inside* the storage backend, so a database backup already carries it. What a database backup cannot carry is the certificate that unlocks it — that is why the certificates come first, and why they have to be the ones that were live when the backup was taken rather than the ones live today.

> [!CAUTION]
> **A certificate has to stay in the backup set for longer than it stays in the ring** — for at least as long as the oldest backup you would still restore. Retiring a certificate from the ring and deleting the file makes every backup taken before that rotation permanently unreadable.

Start one node after a restore and read `GET /diagnostics/encryption-certificates` before starting the rest: keys reported with the role `Retired` mean the ring is missing a certificate the restored data needs. The full procedure, the retention rule and the failure modes are in [Backup and restore ordering](encryption-certificate.md#backup-and-restore-ordering); the PII key store has its own ordering in [Compliance Storage](configuration/compliance-storage.md).

## Security Considerations

Keep Chronicle and its storage on private networks, allow only the ports each caller needs, and use TLS for production connections. Store sensitive configuration outside the Compose file and keep deployed images updated through tested upgrades.

The .NET client connects over TLS but accepts any server certificate unless the connection string sets `skipTlsValidation=false`. Trust the server's certificate authority on client machines and in application containers, or configure a matching `certificatePath` pin, then enable validation for each production connection. The client warns when validation is skipped for a non-loopback target or DNS SRV lookup; localhost, 127.0.0.0/8, and ::1 are exempt. See [Client TLS](../configuration/tls.mdx) and [Server connection strings](../connection-strings/server.md).

Complete administrator setup before making the service reachable by users. The production image creates the administrator without a password; setup requires the token from server logs or `Authentication:InitialAdminSetupToken`. Protect that token and remove it from configuration after setup. To avoid interactive setup, supply the initial password from your secret store through `Authentication:AdminUser`. See [Authentication](configuration/authentication.md).

If one proxy host serves several instances by path, set `Workbench:BasePath` to the instance's prefix. The [Workbench configuration](configuration/workbench.md) explains how its URLs follow that prefix.

## Scaling

Run multiple Chronicle containers as one Orleans cluster when you need multiple active nodes. All nodes must share storage and the same cluster and service identifiers. MongoDB, PostgreSQL, and SQL Server support this topology; SQLite requires a single instance. Put a load balancer in front of API traffic on port 35000.

Allow the silo port (11111) only between trusted nodes and the gateway (30000) only for trusted Orleans clients. HTTP/gRPC-only deployments can set `clustering.gatewayEnabled=false`. Neither Orleans port uses Chronicle's HTTP authentication, so do not publish them publicly. See [Clustering network boundaries](configuration/clustering.md#listener-and-network-boundaries) for interface binding and a Kubernetes NetworkPolicy example, and [Clustering](configuration/clustering.md) for storage-specific settings.

> [!WARNING]
> Every multi-node deployment **must** set the clustering type to `MongoDB` or `Sql` to match its storage.
> The default `Localhost` mode permits only one active node per kernel storage. The single-writer lease
> enforces this by default, including on SQL storage. A replacement waits for the old holder to stop or
> expire; it does not process concurrently. Disabling the lease removes that protection.

For a single-active-node deployment, configure stop-before-start replacement. In Kubernetes use
`strategy: Recreate`, or `RollingUpdate` with `maxSurge: 0` and `maxUnavailable: 1`. The default one-replica
rolling update (`maxSurge: 1`, `maxUnavailable: 0`) cannot converge: the new node waits for the lease while
the platform waits for it to become ready before stopping the old holder.

**Azure Container Apps single-revision deployments and Cloud Run have the same readiness deadlock.**
They wait for the replacement to be ready before retiring the active revision. There is currently no
supported zero-downtime handover for Localhost mode on those platforms. Explicitly stop the old revision
before starting its replacement (accepting downtime), or use `MongoDB` or `Sql` clustering for multiple active nodes.
Do not disable the lease to bypass this rollout failure. A replacement waiting for the single-writer
lease does not report ready.

A lost renewal for half the TTL causes Critical logging and process shutdown with a nonzero exit code;
a stuck shutdown is force-terminated before another holder can acquire the lease. Configure the restart
policy to restart failed processes and allow the acquisition timeout to cover the previous holder's TTL.
See [Single-writer lease](configuration/clustering.md#single-writer-lease) for settings and clock caveats.

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
    image: mongo:8.2
    command: ["mongod", "--replSet", "rs0", "--bind_ip_all"]
    # This MongoDB has no authentication. Publish it on loopback at most, for host tools.
    ports:
      - "127.0.0.1:27017:27017"
    volumes:
      - mongodb_data:/data/db
    restart: unless-stopped

  mongodb-init:
    image: mongo:8.2
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
every instance must be able to decrypt what any other instance wrote. The same holds for the whole
certificate ring during a rotation — give every node the same `Previous` list, or a node will fail to read
what another one wrote. See [Data Protection Key Encryption](encryption-certificate.md) and
[Rotating the certificate](encryption-certificate.md#rotating-the-certificate).

Each container reaches the others over the Compose network by service name, so the default advertised
address works. When you instead run multiple nodes directly on one host, set
`Cratis__Chronicle__Clustering__AdvertisedIP` and distinct `SiloPort`/`GatewayPort` values per node - see
[Clustering](configuration/clustering.md).

## See also

- [Run Chronicle on Azure with Microsoft Entra ID](azure.md) for Azure SQL, Azure Database for PostgreSQL and managed identities.
