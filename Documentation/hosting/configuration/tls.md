# TLS Configuration (Server)

By default, the Chronicle port serves gRPC (HTTP/2) and the Workbench, REST API, OAuth and health endpoints (HTTP/1.1) over HTTPS. TLS uses ALPN to select the protocol per connection. You can explicitly disable TLS **only** when a TLS-terminating reverse proxy fronts the private backend port and forwards **all** traffic using cleartext HTTP/2 (h2c). The cleartext port does not negotiate HTTP/1.1; do not expose it directly to browsers or the internet. Clients and browsers connect to the proxy over HTTPS.

For client-side TLS configuration, see [TLS Configuration (Client)](../../configuration/tls).

## Configuration file

```json
{
  "tls": {
    "enabled": true,
    "certificatePath": "/path/to/certificate.pfx",
    "certificatePassword": "your-password"
  }
}
```

For a private h2c backend behind an HTTPS-terminating reverse proxy, set `"enabled": false` instead. Configure the proxy to forward gRPC, Workbench, REST and OAuth traffic over h2c to the Chronicle port; a proxy that forwards HTTP/1.1 will not work. Restrict direct access to the backend port. With authentication enabled, configure an explicit HTTPS external `authentication.authority`; the internal OAuth authority is not supported with this cleartext topology. Forward the public HTTPS scheme using `X-Forwarded-Proto` so authenticated requests are evaluated as HTTPS. See [Authentication](authentication.md).

## Environment variables

```bash
Cratis__Chronicle__Tls__Enabled=true
Cratis__Chronicle__Tls__CertificatePath=/path/to/certificate.pfx
Cratis__Chronicle__Tls__CertificatePassword=your-password
```

## Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| enabled | boolean | true | Use HTTPS for the main listener; false opts into a private HTTP/2-only h2c listener |
| certificatePath | string | null | Path to the TLS certificate file (PFX format) |
| certificatePassword | string | null | Password for the certificate file |

## TLS behavior

- **TLS enabled with a certificate**: Chronicle serves HTTP/1.1 and HTTP/2 over HTTPS with that certificate.
- **TLS enabled without a certificate, development**: Chronicle generates an in-memory self-signed certificate. Clients accept it automatically in development, and browsers show a certificate warning you can bypass.
- **TLS enabled without a certificate, production**: the server fails to start. A missing or invalid certificate never causes a silent downgrade to HTTP.
- **TLS explicitly disabled**: Chronicle warns at startup and serves HTTP/2-only cleartext on the main port. Use an HTTPS-terminating reverse proxy and restrict backend access. A configured TLS certificate is not used on that port.

The dedicated health port has independent `health.tls` configuration. When `health.tls` is true it still requires a TLS certificate even if the main port has TLS disabled. Set `health.tls` to false for a private plaintext probe port. See [Health Endpoint](health-endpoint.md).

## Health probes and self-signed certificates

With default TLS, health probes may have trouble validating a self-signed certificate. To keep probes off the certificate, use a dedicated plaintext port — see [Health Endpoint](health-endpoint.md). With h2c on the main port, probes on that port must support HTTP/2; a dedicated HTTP/1.1 health port is often easier.

## Related TLS and certificate pages

- [Identity Provider Certificate Configuration](identity-provider-certificate.md) explains why the legacy `identityProvider.certificate` setting is ignored.

## Certificate requirements

When TLS is enabled, Chronicle requires certificates in PFX (PKCS#12) format that include a private key and, if applicable, the certificate chain.

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

For an h2c backend, omit the certificate mount, set `Cratis__Chronicle__Tls__Enabled=false`, and publish only the HTTPS reverse proxy. Configure `health.tls=false` if a dedicated health port is used without a certificate.

## Troubleshooting

### Server fails to start

**Error**: "No TLS certificate is configured. The Chronicle port ... requires a certificate."

**Solution**: Provide `certificatePath` and `certificatePassword` in the top-level `tls` configuration. In development, TLS-enabled servers generate a self-signed certificate automatically. To deliberately terminate TLS at an HTTPS reverse proxy, set `tls.enabled=false` and configure its private upstream for h2c; see the authentication requirements above.
