---
title: Run Chronicle locally
description: Get the Chronicle kernel running on your machine with Docker — the embedded-database dev image for the fastest start, or your own MongoDB, PostgreSQL, SQL Server, or SQLite when you need it. Every host guide starts here.
---

Chronicle runs as a small **kernel** — a Docker image your application talks to over the network. Before a single line of your host code can append an event, that kernel has to be up and reachable. This is the one page that covers getting it there, so the [console](./console.md), [worker service](./worker.md), and [ASP.NET Core](./aspnetcore.md) guides can all point here instead of repeating it.

You only need to do this once per machine. Leave it running in the background and come back to your code.

## Prerequisites

- [.NET 8 or higher](https://dot.net) — the SDK you build and run your app with.
- [Docker Desktop or compatible](https://www.docker.com/products/docker-desktop/) — the kernel ships as a container.
- *Optional:* a MongoDB client such as [MongoDB Compass](https://www.mongodb.com/products/tools/compass), handy for peeking at the read models a projection builds. You don't need it to follow the guides — the built-in [workbench](#see-it-in-the-workbench) shows you the events.

## The fastest start: the development image

The `latest-development` image bundles MongoDB, so there's nothing else to install or wire up — one command and the kernel is running:

```shell
docker run -d -p 27017:27017 -p 8080:8080 -p 35000:35000 cratis/chronicle:latest-development
```

Three ports, three jobs:

| Port | What it is |
| --- | --- |
| `35000` | The kernel endpoint your app connects to (`chronicle://localhost:35000`). |
| `8080` | The [Chronicle workbench](#see-it-in-the-workbench) — a web UI for browsing your event store. |
| `27017` | The bundled MongoDB, where projections write their read models. |

That's everything most local work needs. If you'd rather run against a database you already have — or a different engine entirely — read on; otherwise [skip to picking a host](#next-pick-your-host).

## Bring your own database

The `latest-development-slim` image leaves the database out, so you point Chronicle at one you run yourself. Two environment variables tell it where to go:

- `Cratis__Chronicle__Storage__Type`
- `Cratis__Chronicle__Storage__ConnectionDetails`

The Compose files below bring up the kernel and a database together. Pick the one for your engine.

### MongoDB with Docker Compose

Chronicle uses MongoDB transactions and change streams, so MongoDB must run as a replica set (or a sharded cluster) — a standalone `mongod` won't do. This file initializes a single-node replica set for local development:

```yaml
services:
  chronicle:
    image: cratis/chronicle:latest-development-slim
    depends_on:
      - mongodb
      - mongodb-init
    environment:
      - Cratis__Chronicle__Storage__Type=MongoDB
      - Cratis__Chronicle__Storage__ConnectionDetails=mongodb://mongodb:27017/?directConnection=true
    ports:
      - 8080:8080
      - 35000:35000

  mongodb:
    image: mongo:8
    command: ["mongod", "--replSet", "rs0", "--bind_ip_all"]
    ports:
      - 27017:27017

  mongodb-init:
    image: mongo:8
    depends_on:
      - mongodb
    restart: "no"
    command:
      - /bin/bash
      - -lc
      - |
        until mongosh --host mongodb --quiet --eval "db.adminCommand('ping')" >/dev/null 2>&1; do
          sleep 1
        done
        mongosh --host mongodb --quiet --eval "
        try {
          rs.status();
        } catch (e) {
          rs.initiate({
            _id: 'rs0',
            members: [{ _id: 0, host: 'localhost:27017' }]
          });
        }"
```

Why this setup:

- `host: 'localhost:27017'` makes the replica set topology usable from host tools (for example `mongosh` and Compass) when they connect to `mongodb://localhost:27017/?replicaSet=rs0`.
- Chronicle still reaches MongoDB over the Docker network (`mongodb:27017`) and uses `directConnection=true` to avoid following the advertised host back to `localhost` inside the Chronicle container.
- `directConnection=true` does not disable transactions; transactions still work because MongoDB is running as a replica set.
- If your existing data volume was initialized with a different replica-set host, run `docker compose down -v` (or wipe the MongoDB data volume) before starting again so `rs.initiate()` can apply the new host.

### PostgreSQL with Docker Compose

```yaml
services:
  chronicle:
    image: cratis/chronicle:latest-development-slim
    depends_on:
      - postgres
    environment:
      - Cratis__Chronicle__Storage__Type=PostgreSql
      - Cratis__Chronicle__Storage__ConnectionDetails=Host=postgres;Port=5432;Database=chronicle;Username=postgres;Password=postgres
    ports:
      - 8080:8080
      - 35000:35000

  postgres:
    image: postgres:16
    environment:
      - POSTGRES_DB=chronicle
      - POSTGRES_USER=postgres
      - POSTGRES_PASSWORD=postgres
    ports:
      - 5432:5432
```

### SQL Server with Docker Compose

> [!WARNING]
> The SQL Server credentials in this example are for local development only. For production, use secure credentials and manage secrets through Docker secrets, environment files, or an external secret manager.

```yaml
services:
  chronicle:
    image: cratis/chronicle:latest-development-slim
    depends_on:
      - sqlserver
    environment:
      - Cratis__Chronicle__Storage__Type=MsSql
      - Cratis__Chronicle__Storage__ConnectionDetails=Server=sqlserver,1433;Database=Chronicle;User Id=sa;Password=Your_strong_password123!;TrustServerCertificate=True;Encrypt=False
    ports:
      - 8080:8080
      - 35000:35000

  sqlserver:
    image: mcr.microsoft.com/mssql/server:2025-latest
    environment:
      - ACCEPT_EULA=Y
      - MSSQL_SA_PASSWORD=Your_strong_password123!
    ports:
      - 1433:1433
```

### SQLite with Docker Compose

```yaml
services:
  chronicle:
    image: cratis/chronicle:latest-development-slim
    environment:
      - Cratis__Chronicle__Storage__Type=Sqlite
      - Cratis__Chronicle__Storage__ConnectionDetails=Data Source=/data/chronicle.db
    volumes:
      - chronicle-data:/data
    ports:
      - 8080:8080
      - 35000:35000

volumes:
  chronicle-data:
```

Bring any of these up the usual way, in the background:

```shell
docker compose up -d
```

## Optional: add the Aspire dashboard

If you want local observability while developing — logs, traces, and metrics — run the kernel alongside the [Aspire dashboard](https://learn.microsoft.com/dotnet/aspire/fundamentals/dashboard/overview) and set `OTEL_EXPORTER_OTLP_ENDPOINT` on the Chronicle container. Port `18888` is the dashboard UI in your browser; port `18889` is the OTLP receiver Chronicle exports to inside the Docker network:

```yaml
services:
  chronicle:
    image: cratis/chronicle:latest-development
    environment:
      - OTEL_EXPORTER_OTLP_ENDPOINT=http://aspire-dashboard:18889
    ports:
      - 27017:27017
      - 8080:8080
      - 11111:11111
      - 30000:30000
      - 35000:35000

  aspire-dashboard:
    image: mcr.microsoft.com/dotnet/aspire-dashboard:latest
    environment:
      - DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true
      - ALLOW_UNSECURED_TRANSPORT=true
      - DOTNET_ENVIRONMENT=Development
    ports:
      - 18888:18888
```

## See it in the workbench

Whichever image you ran, it includes the **Chronicle workbench** — a web UI for poking at your event store. Open [http://localhost:8080](http://localhost:8080), pick an event store, and look at **Sequences** to watch events land in order. It's the quickest way to confirm the kernel is up and your app is actually appending.

## Next: pick your host

The kernel is running and listening on `chronicle://localhost:35000`. Now connect your app to it:

- **Just exploring?** The [Get started quickstart](/chronicle/get-started/) scaffolds a ready-to-run app from a template — the fastest way to see the whole loop.
- **[Console](./console.md)** — the bare-bones version, no DI container, every connection explicit.
- **[Worker service](./worker.md)** — a background host for the reacting side of an event-sourced system.
- **[ASP.NET Core](./aspnetcore.md)** — a web API that appends events straight from its endpoints.
