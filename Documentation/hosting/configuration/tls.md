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

For a private h2c backend behind an HTTPS-terminating reverse proxy, set `"enabled": false` instead. Configure the proxy to forward gRPC, Workbench and REST traffic over h2c to the Chronicle port; a proxy that forwards HTTP/1.1 will not work. Restrict direct access to the backend port. With authentication enabled, configure an explicit HTTPS external `authentication.authority`: the internal OAuth authority and Chronicle's `/connect/token` endpoint are **not available** in this topology. Authenticated h2c currently supports callers that supply bearer tokens obtained independently and Workbench cookie login; the .NET SDK's built-in ClientCredentials mode cannot use an external authority. Forward the public HTTPS scheme using `X-Forwarded-Proto` and [trust only the immediate proxy](#trusted-reverse-proxies). See [Authentication](authentication.md).

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

## Trusted reverse proxies

Chronicle accepts `X-Forwarded-For` and `X-Forwarded-Proto` only from loopback by default. To trust a proxy connecting over another address (for example, an ingress pod or a proxy on a Docker network), configure its **immediate** IP address or CIDR range under top-level `forwardedHeaders`:

```json
{
  "forwardedHeaders": {
    "knownProxies": ["10.20.0.5"],
    "knownNetworks": ["10.42.0.0/24"]
  }
}
```

Or use environment variables with array indexes:

```bash
Cratis__Chronicle__ForwardedHeaders__KnownProxies__0=10.20.0.5
Cratis__Chronicle__ForwardedHeaders__KnownNetworks__0=10.42.0.0/24
```

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| forwardedHeaders.knownProxies | IP address list | empty (loopback still trusted) | Addresses of immediate proxies allowed to set the effective scheme and client IP |
| forwardedHeaders.knownNetworks | CIDR range list | empty (loopback still trusted) | Networks containing immediate proxies allowed to set the effective scheme and client IP |

An untrusted peer's forwarded headers are ignored, including when `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`. Configure only the proxy's source addresses, not the client networks; do not use a wildcard CIDR. The proxy **must replace**, not pass through, client-supplied `X-Forwarded-For` and `X-Forwarded-Proto` headers. Otherwise clients can still forge values by sending those headers to the trusted proxy. Use firewall or network policy to prevent direct access to an h2c backend, including any nonexclusive plaintext health port.

The documented Docker Compose recipe publishes Chronicle directly and needs no forwarded headers. Aspire Composition runs YARP in a container and exposes HTTP at `localhost:9876`. Do **not** trust its forwarded `http` scheme while it fronts Chronicle's TLS listener: accepting it would make Workbench cookie mutations fail antiforgery validation. If the public side of a proxy is HTTPS, configure `knownProxies` for its actual source IP or `knownNetworks` for its restricted subnet. On managed platforms where source IPs change, use a restricted infrastructure subnet rather than trusting all peers. With a TLS main listener, an untrusted proxy changes only the client IP in logs and telemetry; HTTPS checks still pass. With h2c or a non-exclusive cleartext health port, forwarded HTTPS from a trusted proxy is needed by HTTPS-sensitive middleware. This trust policy applies regardless of whether `ASPNETCORE_FORWARDEDHEADERS_ENABLED` is set.

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
