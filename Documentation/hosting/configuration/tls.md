# TLS Configuration (Server)

With TLS enabled (the default), the Chronicle port serves gRPC (HTTP/2) and the Workbench, REST API, OAuth and health endpoints (HTTP/1.1) on a single port. Kestrel can only multiplex the two protocols on one port over TLS, where ALPN negotiates the protocol per connection — so with TLS on, **the port requires a certificate**.

TLS can be **disabled** to run in cleartext — for example when TLS is terminated upstream by a load balancer, ingress or service mesh, or for local development. Because cleartext cannot multiplex both protocols on one port, disabling TLS splits them across two ports (see [TLS behavior](#tls-behavior) below).

For client-side TLS configuration, see [TLS Configuration (Client)](../../configuration/tls).

## Configuration file

```json
{
  "tls": {
    "certificatePath": "/path/to/certificate.pfx",
    "certificatePassword": "your-password"
  }
}
```

## Environment variables

```bash
Cratis__Chronicle__Tls__CertificatePath=/path/to/certificate.pfx
Cratis__Chronicle__Tls__CertificatePassword=your-password
```

## Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| enabled | bool | true | Whether TLS is enabled. Set to `false` to serve cleartext across two ports |
| certificatePath | string | null | Path to the TLS certificate file (PFX format) |
| certificatePassword | string | null | Password for the certificate file |

## TLS behavior

**TLS enabled (default), a certificate is provided** (`certificatePath` set): Chronicle serves the single multiplexed port with that certificate.

**TLS enabled, no certificate, development**: Chronicle generates an in-memory self-signed certificate so the port works out of the box. Clients accept it automatically in development, and browsers show a certificate warning you can bypass.

**TLS enabled, no certificate, production**: the server fails to start — a certificate is required.

**TLS disabled** (`enabled` set to `false`): no certificate is used or required. Since cleartext cannot multiplex HTTP/1.1 and HTTP/2 on one port, the two protocols split across two cleartext ports:

- `port` (35000 by default) serves cleartext gRPC over h2c (HTTP/2 with prior knowledge).
- `managementPort` (8080 by default) serves plain HTTP/1.1 — the Workbench, REST API, OAuth and health endpoints.

```json
{
  "tls": { "enabled": false },
  "port": 35000,
  "managementPort": 8080
}
```

This mirrors the pre-16.0 topology and is the setup to use when a load balancer or ingress terminates TLS and forwards cleartext to Chronicle. Clients connect with TLS disabled — see [connection string with TLS disabled](../../configuration/tls).

## Load balancer and health-check probes

A **plain-HTTP health check against a TLS port fails at the TLS record layer** with `"Wrong version number"` — the port speaks TLS, not cleartext HTTP. Choose a probe that matches how the port is configured:

- **TLS on the main port** — probe with a **TCP-connect** check on the port, or an **HTTPS** probe that trusts the certificate's CA (or skips verification) against a host listed in the certificate's SAN.
- **TLS terminated upstream** (`enabled` set to `false`) — probe `/health` over plain HTTP on the `managementPort`.
- **Either topology** — expose a dedicated plaintext health port (see below) and probe `/health` over plain HTTP there, while the data plane stays on TLS.

There is no separate always-plaintext management port bound by default; when TLS is enabled everything is served on the single TLS `port`.

### Dedicated plaintext health port

Set `healthPort` to expose only the health-check endpoint on a separate cleartext port, independent of TLS on the main port. It is disabled by default (`0`). Bind it to the internal or cluster network only — it is a tiny, anonymous, health-only surface.

```json
{
  "healthPort": 8090
}
```

```bash
Cratis__Chronicle__HealthPort=8090
```

An orchestrator or load balancer can then probe `http://<host>:8090/health` with no TLS, ALPN, CA trust or SAN matching, while gRPC, the API and OAuth remain fully on TLS.

## Related TLS and certificate pages

- [Identity Provider Certificate Configuration](identity-provider-certificate.md) for internal OAuth authority certificates.
- [Port Reference](port-reference.md) for the full list of ports Chronicle exposes.

## Certificate requirements

Chronicle requires certificates in PFX (PKCS#12) format that include a private key and, if applicable, the certificate chain.

## Docker deployment

Mount the certificate and set configuration via environment variables:

```yaml
services:
  chronicle:
    image: cratis/chronicle:latest
    volumes:
      - ./chronicle.json:/app/chronicle.json:ro
      - ./certs/production.pfx:/app/certs/production.pfx:ro
    environment:
      - Cratis__Chronicle__Tls__CertificatePath=/app/certs/production.pfx
      - Cratis__Chronicle__Tls__CertificatePassword=${CERT_PASSWORD}
```

## Troubleshooting

### Server fails to start

**Error**: "No TLS certificate is configured. The Chronicle port ... requires a certificate."

**Solution**: Provide `certificatePath` and `certificatePassword` in the top-level `tls` configuration, or set `tls.enabled` to `false` to run in cleartext. In development the server generates a self-signed certificate automatically, so this error only occurs in production with TLS enabled.

### Health check reports "Wrong version number"

The probe is speaking plain HTTP to a TLS port. Use a TCP-connect or HTTPS probe on the TLS port, probe the `managementPort` when TLS is terminated upstream, or expose a dedicated plaintext `healthPort`. See [Load balancer and health-check probes](#load-balancer-and-health-check-probes).
