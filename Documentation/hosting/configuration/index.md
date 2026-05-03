# Configuration

Chronicle Server can be configured using a `chronicle.json` file or environment variables. Environment variables take precedence over file-based configuration, which is useful for containerized deployments.

## Example configuration

```json
{
  "managementPort": 8080,
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
| Observers | Retry and timeout settings |
| Events | Event queue configuration |
| Authentication | External authority and default admin settings |
| TLS | gRPC TLS certificate configuration |
| Workbench TLS | Dedicated Workbench TLS and certificate configuration |
| Identity Provider Certificate | Dedicated certificate configuration for internal OAuth authority |

## Topics

- [Configuration File](configuration-file.md) - Structure and location of `chronicle.json`.
- [Root Properties](root-properties.md) - Ports and health check settings.
- [Features](features.md) - Toggle API, Workbench, and OAuth authority.
- [Storage](storage.md) - Configure the storage provider and connection details.
- [Observers](observers.md) - Retry and timeout settings for observer subscriptions.
- [Events](events.md) - Configure event queues.
- [Authentication](authentication.md) - External authority and default admin settings.
- [TLS](tls.md) - Configure top-level gRPC TLS certificates.
- [Workbench TLS](workbench-tls.md) - Configure Workbench TLS and certificates.
- [Identity Provider Certificate](identity-provider-certificate.md) - Configure internal OAuth authority certificates.
- [Environment Variables](environment-variables.md) - Configure with `Cratis__Chronicle__` settings.
- [Open Telemetry](open-telemetry.md) - Export metrics, traces, and logs via OTLP.
- [Docker Configuration](docker.md) - Configure Chronicle in Docker.
- [Configuration Precedence](configuration-precedence.md) - How sources override each other.
- [Best Practices](best-practices.md) - Recommended configuration guidelines.
- [Port Reference](port-reference.md) - Ports exposed by Chronicle Server.
- [Job Throttling](job-throttling.md) - Limit parallel job steps to control CPU usage.

