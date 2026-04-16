# TLS Configuration (Server)

Chronicle Server supports TLS for secure communication. TLS is enabled by default, but can be explicitly disabled when TLS is terminated upstream.

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
| enabled | boolean | true | Whether TLS is enabled. Set to `false` to run Chronicle without local HTTPS when TLS is terminated upstream. |
| certificatePath | string | null | Path to the TLS certificate file (PFX format) |
| certificatePassword | string | null | Password for the certificate file |

## gRPC TLS behavior

The top-level `tls` configuration controls the gRPC listener:

- `tls.enabled=true` (default): Chronicle expects a certificate and uses HTTPS.
- `tls.enabled=false`: Chronicle runs gRPC without HTTPS.

Use `tls.enabled=false` only when TLS is terminated by upstream infrastructure such as ingress or reverse proxies.

## Related TLS and certificate pages

- [Workbench TLS Configuration](workbench-tls.md) for Workbench-specific TLS and certificates.
- [Identity Provider Certificate Configuration](identity-provider-certificate.md) for internal OAuth authority certificates.

## Development vs production

- **Development**: The server can run with or without TLS.
- **Production**: The server can run with or without TLS based on `tls.enabled`. When `tls.enabled=true`, a certificate is required. Workbench TLS can be configured independently.

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

**Error**: "No TLS certificate is configured for gRPC while TLS is enabled"

**Solution**: Provide `certificatePath` and `certificatePassword` in the top-level `tls` configuration, or set `tls.enabled` to `false` when TLS is terminated upstream.

**Error**: "No TLS certificate is configured for the Workbench"

**Solution**: Either provide a certificate path, or set `workbench.tls.enabled` to `false` if TLS is terminated upstream.
