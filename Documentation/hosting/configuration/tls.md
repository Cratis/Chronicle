# TLS Configuration (Server)

The Chronicle port serves gRPC (HTTP/2) and the Workbench, REST API, OAuth and health endpoints (HTTP/1.1) on a single port. Kestrel can only multiplex the two protocols on one port over TLS, where ALPN negotiates the protocol per connection — so **the port always uses TLS and requires a certificate**.

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
| certificatePath | string | null | Path to the TLS certificate file (PFX format) |
| certificatePassword | string | null | Password for the certificate file |

## TLS behavior

- **A certificate is provided** (`certificatePath` set): Chronicle serves the port with that certificate.
- **No certificate is provided, development**: Chronicle generates an in-memory self-signed certificate so the port works out of the box. Clients accept it automatically in development, and browsers show a certificate warning you can bypass.
- **No certificate is provided, production**: the server fails to start — a certificate is required.

When TLS is terminated upstream by an ingress or reverse proxy, re-encrypt the connection to Chronicle (the backend port is always TLS) and provide a certificate for it.

## Health probes and self-signed certificates

Because the main port always uses TLS — and in development that certificate is self-signed — health probes can struggle to reach it. The kubelet's own HTTPS probes skip certificate verification, but load balancers, ingress controllers and managed health checks that front the pod commonly validate the certificate and reject a self-signed one. To keep probes off the certificate entirely, publish the health endpoint on a dedicated plaintext port — see [Health Endpoint](health-endpoint.md).

## Related TLS and certificate pages

- [Identity Provider Certificate Configuration](identity-provider-certificate.md) for internal OAuth authority certificates.

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

**Solution**: Provide `certificatePath` and `certificatePassword` in the top-level `tls` configuration. In development the server generates a self-signed certificate automatically, so this error only occurs in production.
