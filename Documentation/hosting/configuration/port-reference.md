# Port Reference

Chronicle Server exposes the following ports:

## Example configuration

```json
{
  "port": 35000
}
```

| Port | Service | Description |
| --- | --- | --- |
| 11111 | Orleans Silo | Internal Orleans clustering |
| 30000 | Orleans Gateway | Client connections to Orleans cluster |
| 35000 | Chronicle | gRPC (HTTP/2) and Workbench, REST API, OAuth and health (HTTP/1.1), multiplexed over TLS |

The port requires TLS. In development, when no certificate is configured, Chronicle generates a self-signed certificate automatically so the port works out of the box.

## Optional dedicated health port

The health endpoint can additionally be published on a dedicated HTTP/1.1 port with TLS optionally disabled — useful for orchestrator and load-balancer probes that cannot validate a self-signed certificate. It is off by default. See [Health Endpoint](health-endpoint.md).

