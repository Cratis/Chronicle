---
title: Configure server TLS in Cratis Chronicle
description: Configure TLS for Chronicle Server, which serves gRPC, the Workbench, REST API, OAuth and health endpoints over HTTPS on one port.
---
By default, the Chronicle port serves gRPC (HTTP/2) and the Workbench, REST API, OAuth and health endpoints (HTTP/1.1) over HTTPS. TLS uses ALPN to select the protocol per connection. You can explicitly disable TLS **only** when a TLS-terminating reverse proxy fronts the private backend port and forwards **all** traffic using cleartext HTTP/2 (h2c). The cleartext port does not negotiate HTTP/1.1; do not expose it directly to browsers or the internet. Clients and browsers connect to the proxy over HTTPS.

For client-side TLS configuration, see [TLS Configuration (Client)](../../configuration/tls).

:::danger[The kernel certificate needs both server and client authentication]
From 19.172.0, nodes in a MongoDB or SQL cluster use the kernel TLS certificate to talk to each other over mutual TLS, and every node presents it both as a server and as a client. If the certificate has an Extended Key Usage (EKU) extension, it must list **both** `serverAuth` (OID `1.3.6.1.5.5.7.3.1`) and `clientAuth` (OID `1.3.6.1.5.5.7.3.2`). A certificate that lists only `serverAuth`, which is what many issuers produce by default, serves clients fine but stops the nodes from connecting to each other. See [Certificate requirements](#certificate-requirements) to provision it and [Troubleshooting](#nodes-cannot-connect-to-each-other-after-upgrading) for the symptom.
:::

## Certificates the kernel uses

Chronicle uses several certificates, each for one purpose. Check this table before you provision any of them.

| Certificate | Used for | Required usages and names | Configuration key |
| --- | --- | --- | --- |
| Kernel TLS certificate | The main port (gRPC, Workbench, REST API, OAuth) and the dedicated health port when `health.tls` is true | PFX with a private key. EKU, if present, includes `serverAuth`. DNS names in the subject alternative names (SAN) for every name clients connect with | `tls.certificatePath`, `tls.certificatePassword` |
| Kernel TLS certificate | Silo-to-silo and Orleans gateway traffic, from 19.172.0, when [cluster traffic is secured](#cluster-traffic) | EKU, if present, includes **both** `serverAuth` and `clientAuth`. The certificate name is not checked. Every node presents the same certificate, or certificates issued by an authority stored in the same PFX | `tls.certificatePath`, `clustering.tlsEnabled` |
| Development certificate | The main port of the development image when no file is configured | Generated in memory with `serverAuth` only and SANs `localhost`, `chronicle`, `127.0.0.1` and `::1`. Never used for cluster traffic | None |
| Federation listener certificate | The separate federation port. Peers pin its public key, so its chain, name and usages are not checked | PKCS#12 file. There is no fallback to the kernel TLS certificate. With hosted federation and no file, the kernel issues it | `federation.certificatePath`, `federation.certificatePasswordPath`. See [Federation](federation.md) |
| Encryption certificate | Protecting Data Protection keys and webhook credentials, and signing and encrypting OAuth tokens. Not used for any connection | RSA with a private key. See [Data Protection Key Encryption](../encryption-certificate.md) | `encryptionCertificate.certificatePath`, `encryptionCertificate.certificatePassword` |
| Identity provider certificate | Nothing. The legacy setting is ignored | None. See [Identity Provider Certificate Configuration](identity-provider-certificate.md) | `identityProvider.certificate` |

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

## Cluster traffic

The certificate that secures the main port also secures the traffic between Chronicle nodes: the silo protocol on port 11111 and the Orleans client gateway on port 30000. It works the same way as the main port: when a certificate file is configured it is used, and without one the traffic is not encrypted, as before.

Chronicle secures cluster traffic when all of these hold:

- Clustering is `MongoDB` or `Sql`. Localhost clustering is a single node and has no silo traffic to protect.
- `tls.certificatePath` points at a certificate file. The development self-signed certificate is not used, because every node would generate a different one.
- `clustering.tlsEnabled` is true, which is the default.

Nodes authenticate each other with the certificate, and both sides must present one. A node accepts a peer that presents the same certificate, or one issued by an authority stored in the same PFX file, such as the CA a cert-manager keystore carries. Silos connect by IP address, so the certificate name is not checked. Certificate renewals are picked up without a restart, as on the main port.

What is checked:

- **Usages.** The TLS transport Chronicle uses between nodes refuses a certificate whose EKU extension does not list `clientAuth` when the node connects out, or `serverAuth` when it accepts a connection. This applies to the certificate a node presents and to the one it receives. A certificate with no EKU extension at all passes this check.
- **Trust.** A peer is trusted when its thumbprint matches the local certificate, or its chain builds to the local certificate's chain or to a certificate stored in the same PFX. The validity period is part of that check; revocation, the certificate name and the key usage bits are not checked by Chronicle.

An Orleans client that connects through the gateway must use TLS with a certificate the node accepts, and the node requires the client to present one with `clientAuth`. Set `clustering.gatewayEnabled` to `false` when only Chronicle gRPC and HTTP clients connect.

:::caution
A node that encrypts cannot talk to one that does not. Turning cluster TLS on or off, including by upgrading a cluster that already has a certificate configured, means stopping every node and starting them again. A rolling upgrade splits the cluster for as long as both kinds of node run.
:::

Set `clustering.tlsEnabled` to `false` to leave encryption to the network, for instance a service mesh with mutual TLS or an encrypting network plugin. Chronicle then logs that it is not encrypting cluster traffic.

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

## Certificate renewal

Chronicle reloads the TLS certificate when the file at `certificatePath` changes, so a renewed certificate needs no restart. The file is checked on a TLS handshake, at most once every 30 seconds, and a changed file is read with the configured `certificatePassword`. The log records `TLS certificate reloaded from disk` with the subject, thumbprint and expiry. New connections use the new certificate; connections already open keep the one they started with.

- **A broken file never replaces a good one.** If the changed file cannot be read, is half written or has the wrong password, Chronicle logs `The TLS certificate file changed but could not be loaded` once and keeps serving the current certificate. The next change to the file is tried again.
- **The password is not reloaded.** It is read at startup, so a renewal must keep the same password. A new password needs a restart.
- **Kubernetes Secrets.** A Secret mounted as a directory or with `items` updates in place, and Chronicle sees it after the kubelet syncs it, which can take a minute or so. Before version 19.172.11 Chronicle did not see a renewed Secret, because it compared the write time and length of the symbolic link instead of the file behind it, so those versions need a restart to pick up a renewed certificate. A Secret mounted with `subPath` is never updated by Kubernetes, so mount the whole Secret or use `items` without `subPath`.
- **The encryption certificate is not reloaded.** It is deliberately fixed for the life of the data it protects. See [Encryption certificate](../encryption-certificate.md).
- **A renewed certificate must keep its usages.** A renewal that drops `clientAuth` is loaded without complaint but breaks every cluster connection opened after the swap. Keep `client auth` in the issuer's `usages` for the lifetime of the certificate.
- **The development certificate** is generated in memory and does not change.

## Certificate requirements

When TLS is enabled, Chronicle requires certificates in PFX (PKCS#12) format that include a private key and, if applicable, the certificate chain. Beyond the format, the kernel TLS certificate must satisfy all of these:

- **Both extended key usages**, `serverAuth` (`1.3.6.1.5.5.7.3.1`) and `clientAuth` (`1.3.6.1.5.5.7.3.2`), when the certificate has an EKU extension. The client-facing port needs only `serverAuth`; cluster traffic needs both.
- **Subject alternative names** for every DNS name clients use to reach port 35000. The .NET client checks the name when you set `skipTlsValidation=false`. Silo-to-silo traffic ignores the name, so you do not need to list node addresses.
- **One certificate for every node**, or a PFX that also holds the issuing authority so each node can validate the others. A cert-manager PKCS#12 keystore carries the authority when the issuer provides one.
- **The same usages after every renewal.**

### Provision the certificate with cert-manager

List the usages explicitly. cert-manager's defaults are `digital signature`, `key encipherment` and `server auth`, which are not enough for cluster traffic.

```yaml
apiVersion: cert-manager.io/v1
kind: Certificate
metadata:
  name: chronicle-tls
spec:
  secretName: chronicle-tls
  issuerRef:
    name: example-issuer
    kind: ClusterIssuer
  dnsNames:
    - chronicle.example.com
    - chronicle.chronicle.svc.cluster.local
  usages:
    - digital signature
    - key encipherment
    - server auth
    - client auth
  keystores:
    pkcs12:
      create: true
      passwordSecretRef:
        name: chronicle-tls-password
        key: password
```

Mount the Secret and point `tls.certificatePath` at its `keystore.p12` entry, with `tls.certificatePassword` set to the password in `chronicle-tls-password`. See [Kubernetes](../kubernetes.md) for a complete manifest.

### Check a certificate before you deploy it

Print the usages and names of the certificate, from the PEM `tls.crt` of the Secret:

```bash
kubectl get secret chronicle-tls -o jsonpath='{.data.tls\.crt}' | base64 -d \
  | openssl x509 -noout -ext extendedKeyUsage,subjectAltName
```

Or from the PFX file Chronicle loads:

```bash
openssl pkcs12 -in keystore.p12 -clcerts -nokeys -passin env:CERT_PASSWORD \
  | openssl x509 -noout -ext extendedKeyUsage,subjectAltName
```

The output must list both usages:

```text
X509v3 Extended Key Usage:
    TLS Web Server Authentication, TLS Web Client Authentication
```

If the extension is missing from the output, the certificate has no EKU extension and passes the usage check.

## Upgrading to 19.172.0 or later

Cluster traffic was not encrypted before 19.172.0. From that version a kernel with a configured certificate file, MongoDB or SQL clustering and the default `clustering.tlsEnabled` of `true` secures it, so a certificate that worked for the main port can stop the nodes from connecting. Before you upgrade:

1. Check the certificate's usages as shown above and reissue it with `client auth` if it is missing.
2. Stop every node and start them again together. A secured node cannot talk to an unsecured one, so a rolling upgrade splits the cluster.
3. To keep encryption in the network instead, for instance with a service mesh, set `clustering.tlsEnabled` to `false` on every node.

See also [Upgrade Cratis Chronicle](../../upgrading/index.md#cluster-traffic-tls-needs-client-authentication).

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

### Nodes cannot connect to each other after upgrading

**Symptom**: after upgrading to 19.172.0 or later, nodes of a MongoDB or SQL cluster do not form a cluster, and the logs contain the exception message `Invalid client certificate for client authentication: <thumbprint>`. The Orleans TLS transport raises it; it can be reported as an `Authentication failed` warning from `Orleans.Connections.Security.TlsClientConnectionMiddleware`. The thumbprint is the kernel TLS certificate's. A certificate that lacks `serverAuth` produces `Invalid server certificate for server authentication: <thumbprint>` instead. Search the logs for `for client authentication`.

**Cause**: the certificate has an EKU extension without `clientAuth`. Chronicle does not add its own message for this case.

**Solution**: reissue the certificate with both `server auth` and `client auth` usages, as in [Certificate requirements](#certificate-requirements), then restart every node. Compare the thumbprint in the message with the certificate you deployed. If the certificate cannot be changed, set `clustering.tlsEnabled` to `false` on every node and encrypt the traffic some other way.

### Cluster traffic is not encrypted

**Symptom**: the log reads `Silo and gateway traffic is not encrypted because no TLS certificate file is configured (Tls:CertificatePath)`.

**Solution**: configure `tls.certificatePath`. The in-memory development certificate is not used for cluster traffic. The log lines `Silo and gateway traffic is encrypted and authenticated with the TLS certificate` and `Silo and gateway traffic is not encrypted by Chronicle because Clustering:TlsEnabled is false` report the other two states.
