<!-- cratis-ai-managed: skills/cratis-chronicle-configuration/references/development-inner-loop.md -->
# Development inner loop

## Contents

- Start the development kernel
- Connect an application
- First Workbench sign-in
- TLS switches
- Leaving development

## Start the development kernel

Use the development image, which bundles MongoDB and is built with the
`DEVELOPMENT` symbol:

```yaml
services:
  chronicle:
    image: cratis/chronicle:latest-development
    ports:
      - "127.0.0.1:35000:35000"
      - "127.0.0.1:27017:27017"
```

`latest-development-slim` omits MongoDB; supply a replica-set MongoDB yourself
(appends use transactions, so a standalone server fails). Publish ports on
loopback only.

The development image generates a self-signed certificate on every start,
generates ephemeral keys, honors a default administrator password and creates a
development client application. None of this is available in the production
image, and environment variables cannot turn the production image into a
development kernel.

## Connect an application

The kernel listens on port 35000 over HTTPS. The default and development
connection strings target `chronicle://localhost:35000`. With client
credentials unset the .NET client falls back to the development client
credentials, and certificate validation is skipped by default, so a development
pair connects with no setup. Do not copy that default to a deployed client.

The Workbench is served from the same port: `https://localhost:35000`. A browser
warns about the self-signed certificate; accept it for local work.

## First Workbench sign-in

The Workbench requires sign-in before any view loads; unauthenticated requests
return 401 and the app redirects to `/login`. On the development image sign in
as `Admin` with the development default password, which the Chronicle
repository's local-development rule and the development documentation state.
These credentials are valid only on a developer machine. A production kernel
instead uses the setup token from its startup log, `initialAdminSetupToken`, or
the `adminUser` bootstrap (see [kernel options](kernel-options.md)).

## TLS switches

| Need | Switch | Notes |
| --- | --- | --- |
| Local client against the self-signed development certificate | Nothing; validation is skipped by default | Set `skipTlsValidation=false` to test real verification. |
| Reach the kernel through a port-forward or private proxy that forwards h2c | Kernel `tls.enabled=false` and `authentication.enabled=false` (isolated development only); client `disableTls=true` | Example connection string: `chronicle://localhost:35001/?disableTls=true&auth=none`. With authentication on, TLS off refuses to start unless `authentication.authority` is an external HTTPS issuer. Explicit credentials are refused with `disableTls`. |
| Token from an external identity provider over a proxy | `disableTls=true` with `apiKey=<token>` | The token travels in the clear on that hop; keep it on a private link. |
| Private CA | `caCertificatePath=<file>` | Client trust only; the kernel still needs its own certificate. |

## Leaving development

Before a shared or deployed environment:

- Switch to a production image, pinned to a tested release.
- Remove the persisted development client application and any configuration that
  recreates it; switching storage to a production kernel does not remove it.
- Set `skipTlsValidation=false` and configure a real certificate.
- Replace development credentials and the default administrator password.
- Review `Webhooks` and `ExternalServices` `Validation`, which differ between
  images.

Production topology and certificates are in `cratis-chronicle-hosting`.
