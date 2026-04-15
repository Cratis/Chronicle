# TLS Configuration (Server)

Chronicle Server supports TLS for secure communication. TLS is required in production and optional in development.

For client-side TLS configuration, see [TLS Configuration (Client)](../../configuration/tls.md).

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

## Environment variables

```bash
Cratis__Chronicle__Tls__Enabled=true
Cratis__Chronicle__Tls__CertificatePath=/path/to/certificate.pfx
Cratis__Chronicle__Tls__CertificatePassword=your-password
```

## Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| enabled | boolean | true | Whether TLS is enabled |
| certificatePath | string | null | Path to the TLS certificate file (PFX format) |
| certificatePassword | string | null | Password for the certificate file |

## gRPC TLS enforcement

gRPC **always** requires TLS in production. This is non-negotiable and cannot be disabled. The top-level `tls` configuration is used for the gRPC listener.

## Workbench-specific TLS

The Workbench (admin UI) can have its own TLS configuration, independent of gRPC. This is useful for deployments behind an ingress or reverse proxy (e.g., Azure Container Apps, Nginx, Azure App Gateway) that terminates TLS upstream.

See [Workbench TLS](workbench-tls.md) for details.

## Development vs production

- **Development**: The server can start without TLS in Debug builds.
- **Production**: The server will fail to start if TLS is not configured for gRPC. Workbench TLS can be explicitly disabled.

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

**Error**: "No TLS certificate is configured for gRPC"

**Solution**: Provide `certificatePath` and `certificatePassword` in the top-level `tls` configuration.

**Error**: "No TLS certificate is configured for the Workbench"

**Solution**: Either provide a certificate path, or set `workbench.tls.enabled` to `false` if TLS is terminated upstream.
