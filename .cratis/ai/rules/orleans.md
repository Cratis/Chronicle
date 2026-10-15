---
applyTo: "**/*.cs"
profile: framework
paths:
  - "**/*.cs"
---
<!-- cratis-ai-managed: rules/orleans.md -->

# Orleans Conventions

> **⚠️ APPLIES ONLY TO PROJECTS USING MICROSOFT ORLEANS**
> If your project does not reference `Microsoft.Orleans` or any Orleans packages, **ignore this entire file**. These rules are irrelevant outside of Orleans contexts.

In the Cratis organization, Orleans is the Chronicle kernel's actor runtime (`Source/Kernel/`). The kernel conventions below are verified against `Cratis/Chronicle` **v19.50.0**. Shared hosting and jobs come from `Cratis/Orleans` **v1.16.3**; use its setup for other services that co-host a silo.

## General

- Do **not** split grain interfaces into a separate project. Grain interfaces and their implementations live together in `Kernel/Core`; the `Contracts` project holds the gRPC contracts, not grain interfaces.
- Clustering is chosen by the host, not the grain code: the server uses `UseLocalhostClustering(siloPort, gatewayPort, serviceId, clusterId)` for local development and the MongoDB storage integration switches on `UseMongoDBClustering` (`MongoDBMembershipStrategy.Multiple`) when clustering is enabled. A silo pointed at non-local shared storage while still on localhost clustering is logged as a **warning** at startup (each such node forms its own single-node cluster over the same data); `Cratis__Chronicle__Clustering__Type=MongoDB` on every node is what joins them.
- Do **not** add `[Alias]` to grain interfaces or grain methods — rely on the default alias Orleans generates. `[Alias(nameof(Type))]` is used only on `[GenerateSerializer]` **state and definition records** in `Kernel/Concepts` (`ReadModelDefinition`, `IndexDefinition`, `ReadModelChangeContext`, `ProjectionFuture`, `ClearPartitionQuarantineResult`), where a stable serialization alias must survive a rename.

## Grain storage providers

- Storage provider names are constants in `WellKnownGrainStorageProviders` (`Kernel/Core`), **one name per concern** — 23 of them at v19.50.0 (`Namespaces`, `EventSequences`, `ObserverDefinitions`, `ObserverState`, `FailedPartitions`, `Recommendations`, `Projections`, `ProjectionsManager`, `ProjectionFutures`, `Reactors`, `Reducers`, `ReducersManager`, `ReadModels`, `ReadModelsManager`, `ReadModelReplayManager`, `Constraints`, `EventStoreSubscriptionsManager`, `EventSeeding`, `Webhooks`, `WebhooksManager`, `DataProtectionKeys`, `PatchManager`, `System`). Reference a provider only through its constant — never a magic string, and there is no "default" provider.
- Each concern has its own `IGrainStorage` implementation (`EventSequencesStorageProvider`, `NamespacesStateStorageProvider`, …) taking the kernel's `IStorage` abstraction — **not** a MongoDB collection. The storage backend (MongoDB, SQL, in-memory) is selected behind `IStorage`, so a grain storage provider never references a database driver.
- Every provider is registered once, in `StorageProviderExtensions.AddStorageProviders(this ISiloBuilder)`, as a keyed singleton wrapped in `ResilientGrainStorage` (a Polly resilience pipeline configured from `Chronicle:ResilientStorage`). A new concern adds a constant, an `IGrainStorage` over `IStorage`, and one `AddKeyedSingleton(WellKnownGrainStorageProviders.<Name>, CreateResilientStorageFor<…>)` line there — nothing else.
- Grains declare their state with `[PersistentState(nameof(<StateType>), WellKnownGrainStorageProviders.<Name>)]`.

Registration excerpt inside Chronicle's `StorageProviderExtensions`; the provider
is `Cratis.Chronicle.EventSequences.EventSequencesStorageProvider(IStorage)`:

```csharp
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

services.AddKeyedSingleton(WellKnownGrainStorageProviders.EventSequences, CreateResilientStorageFor<Cratis.Chronicle.EventSequences.EventSequencesStorageProvider>);
```

Jobs are the exception to the kernel's `IStorage` shape: `JobGrainStorageProvider`
and `JobStepGrainStorageProvider` live in `Cratis.Orleans.Jobs` and take
`Cratis.Orleans.Storage.IJobsStorage`. Chronicle registers them under
`Cratis.Orleans.WellKnownGrainStorageProviders.Jobs` and `.JobSteps`, still wrapped
in its resilient storage pipeline.

## Co-host a silo with Cratis.Orleans

For job/step authoring, stage barriers, recovery, storage scopes and state-machine
APIs, load **cratis-orleans-jobs** from `cratis/engineering/csharp`. These APIs are
framework/service guidance; they do not impose application slice architecture.

For service authors, `Cratis.Orleans` supplies hosting, grain serializers, and the
shared job system without requiring Chronicle. With `Cratis.Orleans` and
`Cratis.Orleans.Storage.MongoDB` 1.16.3, this host uses durable MongoDB membership
and jobs storage (a local MongoDB server is required):

```csharp
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Hosting;
using Cratis.Orleans.Setup;
using Microsoft.AspNetCore.Builder;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);
const string connectionString = "mongodb://localhost:27017";
builder.AddCratisOrleans(new CratisOrleansOptions
{
    ClusterId = "my-service",
    ServiceId = "my-service",
    Clustering = ClusteringMode.MongoDB,
    DatabaseName = "my-service-orleans",
    ConnectionString = connectionString
});
builder.Services.AddCratisOrleansMongoDBJobsStorage(new MongoClient(connectionString));

var app = builder.Build();
app.Run();
```

- Register jobs storage separately: `AddCratisOrleansMongoDBJobsStorage`,
  `AddCratisOrleansSqlJobsStorage` (`Cratis.Orleans.Setup`), or
  `AddCratisOrleansInMemoryJobsStorage` (`Cratis.Orleans.Storage`).
- Give grain implementations a unique `[GrainType("unique-name")]`. Orleans'
  default type name derives from the class name alone; a collision across
  assemblies prevents silo startup.
- Reminders deliberately use Orleans' in-memory service in both clustering
  modes; they do not survive a full restart. Jobs rehydrate from durable jobs
  storage and do not use reminders. Do not switch to the incompatible MongoDB
  reminder table merely because clustering uses MongoDB.
- `AddCratisOrleans` enables activity propagation itself. If a host keeps an
  explicit `AddActivityPropagation()` call, it must be **before**, never after,
  `AddCratisOrleans`, to avoid duplicate Orleans propagation filters/spans.
- Keep Orleans package versions aligned: the Release-build guard rejects a
  `Microsoft.Orleans.Reminders`/`Microsoft.Orleans.Server` version mismatch.
- `Enabled = false` makes `AddCratisOrleans` a no-op for specification hosts;
  it does not register the silo or its collaborators.
