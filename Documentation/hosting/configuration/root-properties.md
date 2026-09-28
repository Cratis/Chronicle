# Root Properties

These properties live at the root of `chronicle.json`.

## Example configuration

```json
{
  "port": 35000,
  "healthCheckEndpoint": "/health"
}
```

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| port | number | 35000 | By default, gRPC (HTTP/2) and Workbench/API/OAuth/health (HTTP/1.1) over TLS; HTTP/2-only h2c when `tls.enabled=false` |
| healthCheckEndpoint | string | /health | Health check endpoint path |

## Health check endpoint

Chronicle exposes the health check endpoint on the main port (35000). You can customize the path if your environment requires a different route.

To expose the health endpoint on a dedicated port — with TLS optionally disabled for orchestrator and load-balancer probes — see [Health Endpoint](health-endpoint.md).

