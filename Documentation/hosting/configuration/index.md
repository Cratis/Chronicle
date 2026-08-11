# Configuration

Chronicle Server can be configured using a `chronicle.json` file or environment variables. Environment variables take precedence over file-based configuration, which is useful for containerized deployments.

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
| Identity Provider Certificate | Dedicated certificate configuration for internal OAuth authority |
| Clients | Bootstrap client registrations created on startup |

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
- [Identity Provider Certificate](identity-provider-certificate.md) - Configure internal OAuth authority certificates.
- [Client Bootstrap](client-bootstrap.md) - Register client applications on startup with hashed secrets.
- [Environment Variables](environment-variables.md) - Configure with `Cratis__Chronicle__` settings.
- [Open Telemetry](open-telemetry.md) - Export metrics, traces, and logs via OTLP.
- [Docker Configuration](docker.md) - Configure Chronicle in Docker.
- [Configuration Precedence](configuration-precedence.md) - How sources override each other.
- [Best Practices](best-practices.md) - Recommended configuration guidelines.
- [Port Reference](port-reference.md) - Ports exposed by Chronicle Server.
- [Job Throttling](job-throttling.md) - Limit parallel job steps to control CPU usage.
