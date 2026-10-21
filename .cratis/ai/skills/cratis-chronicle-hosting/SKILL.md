---
name: cratis-chronicle-hosting
description: Run the Chronicle kernel with production storage, certificates, TLS or a private h2c proxy, health probes, clustering and observer recovery configuration. Use when deploying the kernel, moving from development images to production, configuring trusted proxies, or diagnosing kernel startup and topology. Do not use for wiring a .NET application client, implementing Orleans grains, or mutating a live store.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-chronicle-hosting/SKILL.md -->

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Host the Chronicle kernel

A working development Compose file is not a production deployment. The kernel's
build, storage, certificates and network topology determine whether it starts
and whether several nodes actually form one cluster. Inspect configuration
without exposing secrets; a hosting request does not authorize deleting data,
rotating keys, clearing quarantine or deploying to an unnamed environment.

## Verified product sources

Source: `Cratis/Chronicle`:

| Source | Contract |
| --- | --- |
| `Documentation/hosting/production.md` | images, DEVELOPMENT, prerequisites, replica sets, ports, backup order, clustering |
| `Documentation/hosting/configuration/tls.md` | TLS/h2c, authentication limits, immediate proxy trust |
| `Documentation/hosting/configuration/health-endpoint.md` | dedicated probe port and exclusive routing |
| `Documentation/hosting/configuration/observers.md` | retries, quarantine, definition evolution |
| `Source/Kernel/Core/Configuration/{Events,Alerts}.cs` | causation retention and incident metrics |
| `Source/Kernel/Core/Setup/Authentication/AuthenticationService.cs` | persisted development application |

Reverify these sources before recommending another kernel version. This is the
operator/kernel surface, not an application-profile layout rule.

## Choose the build and storage

- `cratis/chronicle:latest` is production; pin a tested release for deployment.
  `latest-development` bundles MongoDB; `latest-development-slim` does not.
- **`DEVELOPMENT` is a compile-time symbol defined for Debug builds**, not
  `ASPNETCORE_ENVIRONMENT` or `DOTNET_ENVIRONMENT`. Environment variables cannot
  turn the Release image into a development kernel.
- Production storage defaults to MongoDB but its connection string is empty.
  Set `Cratis__Chronicle__Storage__ConnectionDetails`, and `__Storage__Type` for
  a different backend. Configuration mounts at `/app/chronicle.json`; environment
  keys use the `Cratis__Chronicle__` prefix. `storage.databaseNamePrefix` changes
  database naming; it is not a migration of existing data.
- MongoDB **must be a replica set**, including single-node development: appends
  use transactions. The current Compose/Aspire default is MongoDB 8.2. Publish
  development database ports on loopback only; a host application needing MongoDB
  must be able to reach port 27017. Do not expose unauthenticated MongoDB.

## Supply certificates before starting production

Default TLS needs a PFX certificate with a private key:
`Tls__CertificatePath` / `Tls__CertificatePassword`. Missing/invalid certificates
fail startup; there is no silent HTTP downgrade. Development generates an
in-memory self-signed TLS certificate.

`EncryptionCertificate__CertificatePath` / `__CertificatePassword` protect the
internal OAuth authority's keys. Production requires this unless `oAuthAuthority`
is disabled or an external authority replaces it. Value encryption is separate
and still requires a certificate on first use. Development falls back to
ephemeral OAuth keys. Supply passwords from the approved secret mechanism,
never commit them. Every cluster node needs the same certificate ring.

The .NET client uses known development credentials when ClientCredentials has
both username and password absent (`ChronicleClient.cs`,
`Connections/ServiceCollectionExtensions.cs`). The development
kernel creates the application **directly in persistent storage**
(`AuthenticationService.cs`). Changing to a production
image does not revoke it. Remove the application and any configuration declaration
that recreates it before reusing that storage. See **cratis-chronicle-client-dotnet**.

## Pick a network topology

Default main port **35000** serves gRPC HTTP/2 plus Workbench, REST, OAuth and
health HTTP/1.1 over TLS using ALPN. Orleans silo **11111** and gateway **30000**
are internal cluster ports, not public browser endpoints.

Explicit `Tls.Enabled=false` is only for a **private HTTP/2-only h2c backend**
behind an HTTPS-terminating proxy. The kernel warns at startup. Forward **all**
traffic over h2c; HTTP/1.1 upstream fails. Restrict direct access. With auth enabled,
use an external HTTPS `authentication.authority`: the internal authority and
`/connect/token` are unavailable; .NET SDK ClientCredentials cannot use the
external authority. Independently obtained bearer tokens and Workbench cookie
login are supported. These claims come from
`Documentation/hosting/configuration/tls.md`, "Configuration file"/"TLS behavior".

The same file's "Trusted reverse proxies" section specifies:

- `X-Forwarded-For/Proto` is trusted from loopback only by default. Add the
  **immediate proxy** IP to `forwardedHeaders.knownProxies` or a restricted
  infrastructure CIDR to `knownNetworks`; env arrays use
  `Cratis__Chronicle__ForwardedHeaders__KnownProxies__0` / `__KnownNetworks__0`.
- `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` does **not** widen trust. Never
  configure a wildcard CIDR or client networks as trusted proxy networks.
- The proxy must **replace**, not relay, client-supplied forwarded headers.
  Forward public HTTPS for HTTPS-sensitive middleware. Do not trust a public
  HTTP scheme while fronting a TLS listener: cookie mutations fail antiforgery.

Requires Chronicle 19.55.0 for configurable kernel gRPC receive/send limits.
Receive defaults to 100 MiB; send is unlimited unless configured. Set
`Cratis__Chronicle__MaxReceiveMessageSize` / `__MaxSendMessageSize` in bytes.
Check the deployed kernel version; older kernels' 4 MiB receive limit can cause
`ResourceExhausted` even when the client permits 100 MiB. Bound batches rather
than increasing limits without accounting for memory.

## Probe without opening another API entrance

Set `health.port` for a dedicated HTTP/1.1 probe listener. `health.tls=false`
removes TLS only there; `health.exclusive=true` restricts it to the configured
health path (`/health` by default). Exclusive is **false by default**, so an
ordinary plaintext probe port also exposes other HTTP/1.1 endpoints. Keep it
internal. `health.tls=true` needs the top-level TLS certificate even when main
TLS is disabled. The production chiseled container has no shell/curl: use an
orchestrator/external probe, not an in-container shell healthcheck.

## Form one cluster, not several over shared data

Every node must explicitly set `clustering.type=MongoDB` and share the cluster
and service ids and MongoDB storage. Default `Localhost` forms isolated
single-node clusters: sharing a database does not join them. Only MongoDB
supports multi-node storage; PostgreSQL, SQL Server and SQLite are single-node.
`Clustering:ResponseTimeout` bounds Orleans transport waiting; increasing observer
subscriber timeout cannot exceed that transport deadline.

Read [recovery and upgrades](references/operations.md) when changing observer
retry/quarantine settings, definition evolution, retention, alerts, or restoring
and upgrading a cluster. See **cratis-chronicle-web-workbench** for browser
exposure and **cratis-chronicle-cli-operations** for authorized live operations.
Several services share one kernel with one event store each: see **cratis-multi-service-architecture**, and **cratis-multi-service-local-orchestration** for an AppHost or compose file.

## Verify

- The build/image and intended environment agree; no default development
  application remains in reused production storage.
- Storage is reachable and MongoDB is a replica set; multiple nodes share MongoDB
  membership, cluster id, service id and certificate ring.
- Certificates load; clients validate production TLS (`skipTlsValidation=false`).
- A probe observes health without publishing a plaintext Workbench/API port.
- Proxy upstream protocol and trust are tested from the actual immediate peer.
- Registration, observer progress and known protected data are checked separately
  from process startup. Report skipped runtime checks; a config review is not a deployment.
