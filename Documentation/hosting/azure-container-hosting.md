---
title: Host Chronicle on Container Apps, App Service or Cloud Run
description: Run one Chronicle kernel on Azure Container Apps, App Service or Google Cloud Run with HTTP/2 ingress, separate health probes and always-on CPU.
---

To run a single Chronicle kernel on Azure Container Apps (ACA), Azure App Service or Google Cloud Run, keep the instance running between requests and forward gRPC traffic over HTTP/2. This guide covers ingress, health probes and the settings that keep background event processing active. Configure storage and identity with [Run Chronicle on Azure](azure.md), and use [Production deployment](production.md) for the wider production checklist.

## Run one instance

The configuration in this guide runs one kernel instance without failover. A restart or revision change interrupts appends and event delivery while the new instance starts and loads its state. If you need several kernel nodes for availability, use a clustering provider (MongoDB or SQL) on Kubernetes or virtual machines; see [Clustering](configuration/clustering.md).

- Set the minimum and maximum instance count to 1.
- Do not scale to zero. Time-driven work (failed-partition retries, occurrence schedules, captures) and event delivery to observers run only while the process runs, so keep CPU allocated at all times, not only during requests.
- Make sure the old instance has stopped before a new one starts against the same store. Two kernels on one store block each other.
- An App Service sidecar scales with the web app. Keep the app at one instance.

The kernel opens its listeners before silo startup finishes. A listening port does not mean it is ready to process events: use liveness for startup probes and readiness to decide when to send traffic, as described below.

The kernel waits up to 10 seconds for a graceful shutdown, so size the platform's termination grace period accordingly. Observer progress that is not saved before the process stops is not lost: those events are delivered again after the restart (at-least-once).

### Applications that connect to the kernel

An application that hosts reactors or reducers holds a stream to the kernel and must stay connected. The kernel sends a keep-alive every second and treats a client it has not heard from for 5 seconds as gone (`ConnectedClients:KeepAliveIntervalSeconds` and `ConnectedClients:StaleThresholdSeconds`). Run such applications as always-on instances with CPU allocated outside requests too; an instance that scales to zero or has its CPU throttled between requests is disconnected.

## Choose how TLS terminates

Chronicle serves gRPC and the Workbench on port 35000.

| Mode | Setting | Use when |
| --- | --- | --- |
| TLS in the kernel | default | The platform passes TCP through, or you mount a certificate |
| TLS at the platform ingress | `Cratis__Chronicle__Tls__Enabled=false` | The platform terminates TLS and forwards plain HTTP/2 |

With TLS disabled, the kernel listens for HTTP/2 without TLS (h2c) only on 35000; it does not accept HTTP/1.1 there. In ACA, set ingress transport to `http2` and the target port to 35000. Clients connect on port 443. Before relying on the Workbench through the platform's ingress, check that browser requests reach port 35000 as HTTP/2.

When TLS is off, `Cratis__Chronicle__Authentication__Authority` must be an absolute HTTPS URL. See [Authentication](configuration/authentication.md) and [Workbench](configuration/workbench.md).

## Move health checks to their own port

ACA probes support TCP and HTTP only. They cannot run a command or a gRPC check. Serve the health endpoints on a separate plain HTTP port:

```text
Cratis__Chronicle__Health__Port=8080
Cratis__Chronicle__Health__Tls=false
Cratis__Chronicle__Health__Exclusive=true
```

Point startup and liveness probes at `/health/live` on port 8080. Use `/health` or `/health/ready` on the same port for readiness; these remain unhealthy until kernel startup completes and become unhealthy when shutdown begins. Keeping probes off the gRPC listener avoids the ACA rollout deadlock described in [Production deployment](production.md). The [health endpoint reference](configuration/health-endpoint.md) explains which storage checks readiness includes.

## Keep internal ports private

Under localhost clustering, the Orleans silo and gateway ports listen on loopback. Expose only port 35000 and the health port.

## Certificates

If the kernel terminates TLS, supply the certificate as a file or from a secret store. See [Production deployment](production.md) for the certificate settings. How you mount the file differs per platform; check your platform's documentation.

## Storage

Use a managed database: PostgreSQL, SQL Server or MongoDB. For MongoDB, the kernel needs a replica set: whichever MongoDB service you choose must run as a replica set and support transactions and change streams. Do not use SQLite on these platforms; the container file system is lost on restart. SQL options are in [Run Chronicle on Azure](azure.md).

## App Service sidecar

- App Service forwards external traffic only to the port it is configured to send requests to, not to every port the kernel listens on.
- For gRPC, [configure gRPC on App Service](https://learn.microsoft.com/en-us/azure/app-service/configure-grpc): set the HTTP version to 2.0, set the HTTP 2.0 Proxy option to "gRPC only", and set the `HTTP20_ONLY_PORT` app setting. That setting names a port on which the app listens for HTTP/2 requests only, in addition to its HTTP/1.1 port. With TLS disabled, set it to 35000. The kernel's only HTTP/1.1 listener is then the dedicated health port, which serves the health endpoints and nothing else, so point App Service's HTTP/1.1 port at it and check how you will reach the Workbench before relying on it.
- App Service gRPC requires Linux (or App Service Environment v3), accepts only secure calls from clients, does not support client certificates, and times out a gRPC request after 20 minutes of inactivity. Clients connected to the kernel through App Service must tolerate that limit.
- The production image is based on a chiseled Ubuntu base. It has no shell, so you cannot open a console in the container.

## Google Cloud Run

Use the single-instance, TLS and health-port settings above on Cloud Run, with these platform-specific settings:

- Deploy with `--use-http2`. Cloud Run otherwise downgrades requests to HTTP/1, except native gRPC traffic, and the kernel answers only HTTP/2 on 35000 when TLS is disabled. See [Use HTTP/2 for services](https://cloud.google.com/run/docs/configuring/http2).
- Use instance-based billing (formerly "CPU always allocated") with a minimum and maximum of one instance. With the default request-based billing, CPU is allocated only while a request is processed. See [Billing settings for services](https://cloud.google.com/run/docs/configuring/billing-settings).
- Send all traffic to the latest revision, and make sure the old instance has stopped before the new one serves the store, as in [Run one instance](#run-one-instance).

## See also

- [Run Chronicle on Azure](azure.md)
- [Production deployment](production.md)
- [Health endpoint](configuration/health-endpoint.md)
- [Clustering](configuration/clustering.md)
