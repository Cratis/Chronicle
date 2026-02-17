# Root Properties

These properties live at the root of `chronicle.json`.

## Example configuration

```json
{
  "managementPort": 8080,
  "port": 35000,
  "healthCheckEndpoint": "/health"
}
```

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| managementPort | number | 8080 | Port for the Management API, Workbench, and well-known endpoints |
| port | number | 35000 | Main gRPC service port |
| healthCheckEndpoint | string | /health | Health check endpoint path |

## Health check endpoint

Chronicle exposes the health check endpoint on the management port. You can customize the path if your environment requires a different route.

