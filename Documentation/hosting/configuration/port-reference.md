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

With TLS enabled (the default), the port requires a certificate. In development, when no certificate is configured, Chronicle generates a self-signed certificate automatically so the port works out of the box.

## Running without TLS

When TLS is disabled (`tls.enabled` set to `false`), cleartext cannot multiplex both protocols on one port, so they split across two ports:

| Port | Service | Description |
| --- | --- | --- |
| 35000 (`port`) | gRPC | Cleartext gRPC over h2c (HTTP/2 with prior knowledge) |
| 8080 (`managementPort`) | Workbench, API, OAuth, health | Plain HTTP/1.1 |

See [TLS Configuration](tls.md) for the full disable-TLS behavior.

## Dedicated health port (optional)

Set `healthPort` to serve only the health-check endpoint on a separate plaintext port, independent of TLS on the main port. It is disabled by default. Use it for orchestrator or load-balancer probes that cannot speak TLS while keeping the data plane on TLS.

| Port | Service | Description |
| --- | --- | --- |
| `healthPort` | Health | Plain HTTP/1.1, serves `/health` only |
