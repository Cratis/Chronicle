---
name: cratis-multi-service-local-orchestration
description: Run several Cratis services, one Chronicle kernel and their frontends together from one repository - layout with contracts projects, a .NET Aspire AppHost with Cratis.Chronicle.Aspire, the Docker Compose alternative with a MongoDB replica set, explicit per-service connection and event-store settings, health-gated startup, a cratis CLI context per store, and a frontend per service. Use when laying out a multi-service repository, writing an AppHost or compose file for several services, or when a service locally reaches the wrong kernel or store. Not for how services communicate (use cratis-multi-service-architecture) or production kernel hosting (use cratis-chronicle-hosting).
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-multi-service-local-orchestration/SKILL.md -->

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Run several Cratis services locally

Locally, a multi-service Cratis system is **one** Chronicle kernel hosting one
event store per service, one MongoDB replica set behind it, every service as its
own process with its own store name, and one frontend per service. Cross-store
forwarding only works inside one kernel, so never start a kernel per service.
`cratis-multi-service-architecture` owns why the services are split this way.

## Lay out the repository

```text
Shop/
├── Shop.slnx
├── AppHost/                    # .NET Aspire AppHost (or docker-compose.yml at the root)
├── Ordering/                   # service: ASP.NET host, slices, Ordering.csproj
│   ├── Ordering.Contracts/     # public events only, references Cratis.Chronicle
│   ├── Screenplay/             # this service's model folder, when modeled
│   └── Web/                    # this service's frontend
└── Billing/
    ├── Screenplay/
    └── Web/
```

- One service per folder, each with its own project, `appsettings.json` naming its
  own `Cratis:Chronicle:EventStore`, and its own frontend.
- A `<Service>.Contracts` project per producer, holding public events and the
  assembly-level `[EventStore("<producer store>")]`. A consumer references the
  contracts project (or its package), never the producer's service project.
- One solution may list every project; nothing shares source between services
  except contracts projects.
- One model folder per service (`<Service>/Screenplay/`), never one folder above all
  services: a folder of `.play` files is one application. Read
  `cratis-screenplay-streams-and-consistency` (multi-service models) before modeling.

## Choose the orchestrator

| Default | Use instead when |
| --- | --- |
| **.NET Aspire AppHost** with `Cratis.Chronicle.Aspire` | Docker Compose: the team does not run Aspire, or the services must start as containers |

Both need the same five decisions per service: kernel address, store name,
read-model database, startup order and a health endpoint.

## Aspire AppHost

Add the `Cratis.Chronicle.Aspire` package to the AppHost only. `AddCratisChronicle`
runs the development image in run mode (bundled MongoDB, self-signed TLS, the
well-known development client) and the production image in publish mode.

```csharp
// AppHost/Program.cs
using Aspire.Hosting.ApplicationModel;
using Cratis.Chronicle.Aspire;

var builder = DistributedApplication.CreateBuilder(args);

var mongo = builder.AddCratisChronicleMongoDB();
var chronicle = builder.AddCratisChronicle("chronicle", c => c.WithMongoDB(mongo));
var grpc = chronicle.GetEndpoint("grpc");

AddService<Projects.Ordering>("ordering", "Ordering");
AddService<Projects.Billing>("billing", "Billing");

builder.Build().Run();

void AddService<TProject>(string name, string eventStore)
    where TProject : IProjectMetadata, new() =>
    builder.AddProject<TProject>(name)
        .WithEnvironment("Cratis__Chronicle__ConnectionString",
            $"chronicle://chronicle-dev-client:chronicle-dev-secret@{grpc.Property(EndpointProperty.HostAndPort)}")
        .WithEnvironment("Cratis__Chronicle__EventStore", eventStore)
        .WithEnvironment("Cratis__MongoDB__Server", mongo.Resource.ConnectionStringExpression)
        .WithEnvironment("Cratis__MongoDB__Database", eventStore)
        .WaitFor(chronicle);
```

Rules behind each line:

- **Set `Cratis__Chronicle__ConnectionString` explicitly.** `WithReference(chronicle)`
  supplies the resource's connection string as `ConnectionStrings__chronicle`, a
  setting the Chronicle client does not read; it binds `Cratis:Chronicle` and
  otherwise falls back to `localhost:35000`. A service that appears to work with
  only `WithReference` is reaching a kernel on port 35000 by accident.
- **Set `Cratis__Chronicle__EventStore` per service.** It is required, and it is the
  origin name the other services' contracts use.
- **MongoDB must be a replica set.** `AddCratisChronicleMongoDB()` starts a
  single-node replica set and hands back a connection string with
  `directConnection=true`. Aspire's own `AddMongoDB` is a standalone server:
  Chronicle's observers and observable queries then silently do nothing.
- **Point Arc's read models at the same server.** Each service's `Cratis:MongoDB`
  `Server` is the replica set and `Database` is the service's event-store name,
  the pairing the Stage-rendered scaffold uses.
- **`WaitFor(chronicle)`** orders startup; the client also reconnects on its own.
- The development client and secret are for a local kernel only. Publish mode
  selects the production image, which needs storage, a TLS certificate and an
  encryption certificate (`cratis-chronicle-hosting`).

To reach the kernel at a fixed address (the CLI default, a browser bookmark), pin
the host port: `chronicle.WithEndpoint("grpc", endpoint => endpoint.Port = 35000);`.

## Docker Compose

Order the containers MongoDB → replica-set initiation → Chronicle → services, and
gate each step on health. Chronicle publishes 35000 (clients, Workbench, health over
TLS) to `127.0.0.1` only; its dedicated health port (8080 here, HTTP without TLS)
stays inside the network for the health check; the Orleans ports 11111 and 30000
stay unpublished for a single node. Services reach the kernel at
`chronicle://chronicle-dev-client:chronicle-dev-secret@chronicle:35000`. Read
[references/docker-compose.md](references/docker-compose.md) for the complete file
and the per-service settings.

## A `cratis` CLI context per store

A context stores one server plus a default event store and namespace, so create
one per service on the same kernel:

```bash
cratis context create ordering --server chronicle://localhost:35000 -e Ordering
cratis context create billing --server chronicle://localhost:35000 -e Billing
cratis context set billing
cratis chronicle subscriptions list
```

`-e <store>` on a single command overrides the context. Under Aspire without a
pinned port, use the host port the dashboard shows for the `grpc` endpoint.
`cratis-chronicle-cli-operations` owns the rest of the CLI.

## Frontends

- One frontend per service. Its generated Arc proxies call routes relative to the
  origin, so serve each frontend from its own service (static files plus a
  fallback, as the Stage-rendered scaffold does) or from a dev server that proxies
  `/api` to that service. One frontend calling several services needs an ingress
  that routes by the `microservice` metadata; nested `<Arc>` providers do not
  isolate identity.
- Types several services share are generated once: map their assembly to an npm
  package with `<AssemblyToPackageMapping Include="shared" Assembly="Shop.Shared" Package="@shop/shared" />`
  in each service project.
- Ports: give each service and dev server a fixed, distinct port, and keep
  27017 and 35000 for MongoDB and Chronicle. Two Stage-rendered services each ship a
  compose file binding 27017 and 35000; run one kernel from the orchestrator instead
  and point both at it.

## Verify the local system

1. The orchestrator shows MongoDB, Chronicle and every service healthy;
   `cratis chronicle event-stores list` lists exactly one store per service.
2. Each service's effective `Cratis:Chronicle:EventStore` matches its contracts'
   `[EventStore]`, and no service logs a connection to `localhost:35000` it was not
   given.
3. `cratis chronicle subscriptions list -e <consumer>` shows each producer, and an
   action in one service's frontend produces the consumer's private event.
4. Restarting the kernel alone: services reconnect and forwarding resumes without
   restarting them.

## Route near misses

- Contracts, subscriptions, delivery and idempotency between services:
  `cratis-multi-service-architecture`.
- Kernel storage, TLS, certificates, clustering and production images:
  `cratis-chronicle-hosting`.
- Bootstrapping one Arc service: `cratis-arc-hosting`.
- Rendering a service from its model: `cratis-screenplay-render-and-gap-fill`.
