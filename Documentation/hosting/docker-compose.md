---
title: Cratis Chronicle local hosting with Docker Compose
description: Run Cratis Chronicle locally with bundled MongoDB and an Aspire OpenTelemetry dashboard using Docker Compose, with service ports bound to loopback.
---
Run Cratis Chronicle with Docker Compose when you want a local event store and a dashboard for its telemetry without creating an Aspire AppHost. This setup uses the development image, which bundles a single-node MongoDB replica set and generates a self-signed TLS certificate. You do not need a separate database service or certificate file.

Save the following as `docker-compose.yml`. The `aspire-dashboard` service runs the [Microsoft Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview) dashboard; it does not run your application or an AppHost.

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

Start the services with `docker compose up -d`. Once Chronicle is ready, open the Workbench at `https://localhost:35000` and the telemetry dashboard at `http://localhost:18888`. Your browser may warn about Chronicle's self-signed certificate. Chronicle sends telemetry to the dashboard's OTLP endpoint on the Compose network; host applications can send telemetry to `http://localhost:4317`.

Every published port binds to loopback because the Workbench has well-known development credentials, bundled MongoDB has no authentication, and the dashboard allows anonymous access. Keep these bindings when sharing the Compose file. The Orleans cluster ports (`11111`, `30000`) remain unpublished because this setup runs one local node.

The `sysctls` setting reserves the cluster and Chronicle ports from ephemeral outbound allocation inside the container. If your container runtime rejects it, remove the `sysctls` block and retry; Chronicle also retries port binding a bounded number of times.

## Private cluster ports

Keep the Orleans silo (11111) and client gateway (30000) off host `ports` mappings and public load balancers. They do not use Chronicle's HTTP authentication. A container's unpublished ports are still reachable by other containers on the same network, so use a dedicated network containing only trusted cluster nodes.

For deployments whose clients use only HTTP/gRPC, add this environment setting to the Chronicle service:

```yaml
services:
  chronicle:
    environment:
      Cratis__Chronicle__Clustering__GatewayEnabled: "false"
```

The gateway remains enabled unless you make this change. Multi-node deployments still need the silo port reachable between nodes. If you set `Cratis__Chronicle__Clustering__AdvertisedIP` with MongoDB or SQL clustering, use the node's private interface or container IP that peers can reach, not a host port mapping or external NAT address. See [Clustering](configuration/clustering.md#listener-and-network-boundaries) for binding and Kubernetes NetworkPolicy guidance.

When you move beyond local development, follow [Production hosting](production.md). Changing the image tag alone is not enough: the production image needs external storage and configured certificates.

