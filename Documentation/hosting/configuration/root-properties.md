# Root Properties

These properties live at the root of `chronicle.json`.

## Example configuration

```json
{
  "port": 35000,
  "bindTimeout": "00:00:30",
  "healthCheckEndpoint": "/health"
}
```

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| port | number | 35000 | By default, gRPC (HTTP/2) and Workbench/API/OAuth/health (HTTP/1.1) over TLS; HTTP/2-only h2c when `tls.enabled=false` |
| bindTimeout | time span | 00:00:30 | How long to retry a port bind that fails with EADDRINUSE |
| healthCheckEndpoint | string | /health | Health check endpoint path |

## Bind timeout

`bindTimeout` defaults to 30 seconds. Chronicle retries any port bind that fails with EADDRINUSE until this timeout expires, including one blocked by a persistent listener; it delays startup failure rather than resolving a persistent conflict. Set it to `"00:00:00"` to disable retries and fail on the first bind attempt. The environment-variable form is `Cratis__Chronicle__BindTimeout=00:00:30`.

## Health check endpoint

Chronicle exposes the health check endpoint on the main port (35000). You can customize the path if your environment requires a different route.

To expose the health endpoint on a dedicated port — with TLS optionally disabled for orchestrator and load-balancer probes — see [Health Endpoint](health-endpoint.md).

