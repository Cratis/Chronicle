---
title: Host the kernel on Azure Container Apps and App Service
description: Run the Chronicle kernel as a single instance on Azure Container Apps or an App Service sidecar, with h2c ingress, TCP or HTTP probes and the settings that differ from a Kubernetes deployment.
---

This page lists what changes when you run the Chronicle kernel on Azure Container Apps (ACA) or Azure App Service instead of Kubernetes. For storage and identity, see [Run Chronicle on Azure](azure.md). For the general production checklist, see [Production deployment](production.md).

## Run one instance

Run the kernel as a single, non-HA instance.

- Set the minimum and maximum replica count to 1.
- Do not scale to zero. The kernel holds observer state and open client streams, so keep CPU allocated at all times.
- With a single active revision, start the new revision only after the old one has stopped. Two kernels on one store block each other.
- An App Service sidecar scales with the web app. Keep the app at one instance.

The kernel waits up to 10 seconds for a graceful shutdown, so size the platform's termination grace period accordingly.

## Choose how TLS terminates

Chronicle serves gRPC and the Workbench on port 35000.

| Mode | Setting | Use when |
| --- | --- | --- |
| TLS in the kernel | default | The platform passes TCP through, or you mount a certificate |
| TLS at the platform ingress | `Cratis__Chronicle__Tls__Enabled=false` | ACA terminates TLS and forwards plain HTTP/2 |

With TLS disabled, the kernel listens for HTTP/2 without TLS (h2c) only on 35000. In ACA, set ingress transport to `http2` and the target port to 35000. Clients connect on port 443.

When TLS is off, `Cratis__Chronicle__Authentication__Authority` must be an absolute HTTPS URL. See [Authentication](configuration/authentication.md) and [Workbench](configuration/workbench.md).

## Move health checks to their own port

ACA probes support TCP and HTTP only. They cannot run a command or a gRPC check. Serve the health endpoints on a separate plain HTTP port:

```text
Cratis__Chronicle__Health__Port=8080
Cratis__Chronicle__Health__Tls=false
Cratis__Chronicle__Health__Exclusive=true
```

The kernel exposes `/health`, `/health/ready` and `/health/live` on that port. Point the ACA startup, liveness and readiness probes at it. If the readiness probe depends on the gRPC listener, ACA can deadlock the rollout; see [Production deployment](production.md). See also [Health endpoint](configuration/health-endpoint.md).

## Keep internal ports private

Under localhost clustering, the Orleans silo and gateway ports listen on loopback. Expose only port 35000 and the health port.

## Certificates

If the kernel terminates TLS, supply the certificate as a file or from a secret store. See [Production deployment](production.md) for the certificate settings. How you mount the file differs per platform; check your platform's documentation.

## Storage

Use a managed database. For MongoDB, the kernel needs a replica set: whichever MongoDB service you choose must run as a replica set and support change streams. SQL options are in [Run Chronicle on Azure](azure.md).

## App Service sidecar

- App Service forwards external traffic only to the port it is configured to send requests to, not to every port the kernel listens on.
- For gRPC, [configure gRPC on App Service](https://learn.microsoft.com/en-us/azure/app-service/configure-grpc): set the HTTP version to 2.0, set the HTTP 2.0 Proxy option to "gRPC only", and set the `HTTP20_ONLY_PORT` app setting. That setting names a port on which the app listens for HTTP/2 requests only, in addition to its HTTP/1.1 port, so the kernel must answer gRPC on the `HTTP20_ONLY_PORT` port and HTTP/1.1 (Workbench, API, health) on the port App Service forwards to. Configure the kernel's listening ports to match.
- App Service gRPC requires Linux (or App Service Environment v3), accepts only secure calls from clients, does not support client certificates, and times out a gRPC request after 20 minutes of inactivity. Clients connected to the kernel through App Service must tolerate that limit.
- The production image is based on a chiseled Ubuntu base. It has no shell, so you cannot open a console in the container.

## See also

- [Run Chronicle on Azure](azure.md)
- [Production deployment](production.md)
- [Health endpoint](configuration/health-endpoint.md)
