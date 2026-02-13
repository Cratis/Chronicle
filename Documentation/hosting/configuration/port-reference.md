# Port Reference

Chronicle Server exposes the following ports:

## Example configuration

```json
{
  "managementPort": 8080,
  "port": 35000
}
```

| Port | Service | Description |
| --- | --- | --- |
| 8080 | Management API | REST API, Workbench, and well-known endpoints |
| 11111 | Orleans Silo | Internal Orleans clustering |
| 30000 | Orleans Gateway | Client connections to Orleans cluster |
| 35000 | Main Service | Primary Chronicle gRPC service port |

