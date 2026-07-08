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
| port | number | 35000 | The single Chronicle port — gRPC (HTTP/2) and Workbench/API/OAuth/health (HTTP/1.1) over TLS |
| healthCheckEndpoint | string | /health | Health check endpoint path |

## Health check endpoint

Chronicle exposes the health check endpoint on the main port (35000). You can customize the path if your environment requires a different route.

