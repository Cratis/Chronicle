---
title: Configure the Cratis Chronicle Kernel
description: Configure the Chronicle Kernel with chronicle.json and environment overrides for storage, authentication, certificates, and observer processing.
---
Configure the Chronicle Kernel with a `chronicle.json` file, `Cratis__Chronicle__` environment variables, or both. Environment variables override matching file settings, letting you share a configuration file while supplying deployment-specific connections and credentials separately.

Start with [Configuration File](configuration-file.md) to place the file, or [Docker Configuration](docker.md) to mount it in a container. Use [Configuration Precedence](configuration-precedence.md) when a setting does not match the value you expected.

## Example configuration

```json
{
  "port": 35000,
  "storage": {
    "type": "MongoDB",
    "connectionDetails": "mongodb://localhost:27017"
  }
}
```

| Section | Description |
| --- | --- |
| Root properties | Ports and health check endpoint |
| Features | API, Workbench, and OAuth authority toggles |
| Workbench | The path the Workbench is served under |
| Storage | Storage provider configuration |
| Compliance | Dedicated storage for compliance encryption keys |
| Clustering | Orleans clustering provider, cluster identity, and ports |
| Observers | Retry and timeout settings |
| Read models | Replay retention for replay-generated read model versions |
| Events | Event queue configuration |
| Jobs | Parallel job step throttling |
| Authentication | External authority, default admin, and initial admin user bootstrap |
| TLS | TLS certificate configuration for the Chronicle port |
| Encryption certificate | Certificate protecting OAuth keys, webhook credentials, and Data Protection keys |
| Health | Dedicated port for the health endpoint |
| Identity Provider Certificate | Ignored legacy setting; migrate to the shared TLS certificate |
| Clients | Bootstrap client registrations created on startup |

The example selects MongoDB storage and port `35000`; it is not a complete production configuration. Before starting the production image, configure the [TLS certificate](tls.md) and [Data Protection Key Encryption](../encryption-certificate.md), and review [Production hosting](../production.md) for the requirements and supported alternatives.

If your deployment provisions storage before starting replicas, use [SQL schema operations](sql-schema-operations.md) or [MongoDB index operations](mongodb-index-operations.md). Their verification modes have different startup behavior and runtime permission requirements.

## Topics

- [Configuration File](configuration-file.md) - Structure and location of `chronicle.json`.
- [Root Properties](root-properties.md) - Ports and health check settings.
- [Features](features.md) - Toggle API, Workbench, and OAuth authority.
- [Storage](storage) - Configure the storage provider and connection details.
- [Compliance Storage](compliance-storage.md) - Give compliance encryption keys their own storage backend.
- [Clustering](clustering.md) - Orleans clustering provider, cluster identity, and silo/gateway ports.
- [Observers](observers.md) - Retry and timeout settings for observer subscriptions.
- [Read Models](read-models.md) - Configure replay retention for replay-generated read model versions.
- [Events](events.md) - Configure event queues.
- [Authentication](authentication.md) - External authority and default admin settings.
- [TLS](tls.md) - Configure the TLS certificate for the Chronicle port.
- [Data Protection Key Encryption](../encryption-certificate.md) - Configure the encryption certificate, required in production.
- [Health Endpoint](health-endpoint.md) - Expose the health endpoint on a dedicated port with optional TLS.
- [Workbench](workbench.md) - Serve the Workbench under a path when a reverse proxy routes by path.
- [Identity Provider Certificate](identity-provider-certificate.md) - Migrate away from the ignored legacy certificate setting.
- [Client Bootstrap](client-bootstrap.md) - Register client applications on startup with hashed secrets.
- [Environment Variables](environment-variables.md) - Configure with `Cratis__Chronicle__` settings.
- [Open Telemetry](open-telemetry.md) - Export metrics, traces, and logs via OTLP.
- [Docker Configuration](docker.md) - Configure Chronicle in Docker.
- [Configuration Precedence](configuration-precedence.md) - How sources override each other.
- [Best Practices](best-practices.md) - Recommended configuration guidelines.
- [Port Reference](port-reference.md) - Ports exposed by Chronicle Server.
- [Job Throttling](job-throttling.md) - Limit parallel job steps to control CPU usage.
- [SQL schema operations](sql-schema-operations.md) - Provision and verify SQL schemas before starting replicas.
- [MongoDB index operations](mongodb-index-operations.md) - Provision indexes offline and inspect index readiness.
