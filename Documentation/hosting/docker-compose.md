---
description: Run Chronicle with MongoDB and optional telemetry services using Docker Compose for more control than the development image offers.
---
# Running with Docker Compose

Using the development image of Chronicle is convenient for getting started quickly. For more control over the runtime environment, you can run Chronicle with MongoDB and other services such as telemetry collectors.

The following configures a `docker-compose.yml` with Chronicle and [Microsoft Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview).

```yaml
services:
  chronicle:
    image: cratis/chronicle:latest-development
    sysctls:
      net.ipv4.ip_local_reserved_ports: "11111,30000,35000"
    environment:
      - OTEL_EXPORTER_OTLP_ENDPOINT=http://aspire-dashboard:18889
    ports:
      - 127.0.0.1:35000:35000
      - 127.0.0.1:27017:27017

  aspire-dashboard:
    image: mcr.microsoft.com/dotnet/aspire-dashboard:latest
    environment:
      - DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true
      - ALLOW_UNSECURED_TRANSPORT=true
      - DOTNET_ENVIRONMENT=Development
    ports:
      - 127.0.0.1:18888:18888
      - 127.0.0.1:4317:18889
```

Every port is published on this machine only: the development image's Workbench accepts well-known credentials, its bundled MongoDB has no authentication, and the dashboard allows anonymous access. The Orleans ports (`11111`, `30000`) are left unpublished because a single local node does not need them. The sysctl reserves those ports and the Chronicle port from ephemeral outbound allocation inside the container. If your container runtime rejects this sysctl, remove the `sysctls` block and retry; Chronicle's bounded bind retry still applies.

With this setup, Chronicle runs alongside Microsoft Aspire, which provides a dashboard for OpenTelemetry.

The development image generates a self-signed TLS certificate. A .NET client on the host using `chronicle://localhost:35000` accepts it through the loopback-only development exception when no validation setting is supplied. A client in another container connecting to `chronicle://chronicle:35000` validates by default: configure and trust a certificate, configure a matching pin, or explicitly set `skipTlsValidation=true` for development only. Container service names are not loopback, and the opt-out must not be carried into production. See [client TLS configuration](../configuration/tls).

## Private cluster ports

Keep the Orleans silo (11111) and client gateway (30000) off host `ports` mappings and public load balancers. They do not use Chronicle's HTTP authentication. A container's unpublished ports are still reachable by other containers on the same network, so use a dedicated network containing only trusted cluster nodes.

For deployments whose clients use only HTTP/gRPC, add this environment setting to the Chronicle service:

```yaml
services:
  chronicle:
    environment:
      Cratis__Chronicle__Clustering__GatewayEnabled: "false"
```

This is an opt-in change; the gateway remains enabled by default. Multi-node deployments still need the silo port reachable between nodes. If you set `Cratis__Chronicle__Clustering__AdvertisedIP` with MongoDB or SQL clustering, it must be the node's actual private interface/container IP, reachable from peers, not a host port mapping or external NAT address. See [Clustering](configuration/clustering.md#listener-and-network-boundaries) for binding and Kubernetes NetworkPolicy guidance.

