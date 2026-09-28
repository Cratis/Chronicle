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
| 35000 | Chronicle | By default, gRPC (HTTP/2) and Workbench, REST API, OAuth and health (HTTP/1.1) over TLS; HTTP/2-only h2c if `tls.enabled=false` |

TLS is enabled by default. In development, when no certificate is configured, Chronicle generates a self-signed certificate automatically. Explicitly set `tls.enabled=false` only behind an HTTPS proxy forwarding all backend requests using h2c; restrict direct access to the cleartext port. See [TLS](tls.md).

## Optional dedicated health port

The health endpoint can additionally be published on a dedicated HTTP/1.1 port with TLS optionally disabled — useful for orchestrator and load-balancer probes that cannot validate a self-signed certificate. It is off by default. See [Health Endpoint](health-endpoint.md).

