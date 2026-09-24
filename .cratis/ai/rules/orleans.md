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

In the Cratis organization, Orleans is the Chronicle kernel's actor runtime (`Source/Kernel/`). These conventions describe how that kernel is shaped, verified against `Cratis/Chronicle` **v18.3.0**; follow them in any Cratis repository that hosts Orleans grains.

## General

- Do **not** split grain interfaces into a separate project. Grain interfaces and their implementations live together in `Kernel/Core`; the `Contracts` project holds the gRPC contracts, not grain interfaces.
- Clustering is chosen by the host, not the grain code: the server uses `UseLocalhostClustering(siloPort, gatewayPort, serviceId, clusterId)` for local development and the MongoDB storage integration switches on `UseMongoDBClustering` (`MongoDBMembershipStrategy.Multiple`) when clustering is enabled. A silo pointed at non-local shared storage while still on localhost clustering is logged as a **warning** at startup (each such node forms its own single-node cluster over the same data); `Cratis__Chronicle__Clustering__Type=MongoDB` on every node is what joins them.
- Do **not** add `[Alias]` to grain interfaces or grain methods — rely on the default alias Orleans generates. `[Alias(nameof(Type))]` is used only on `[GenerateSerializer]` **state and definition records** in `Kernel/Concepts` (`ReadModelDefinition`, `IndexDefinition`, `ReadModelChangeContext`, `ProjectionFuture`), where a stable serialization alias must survive a rename.

## Grain storage providers

- Storage provider names are constants in `WellKnownGrainStorageProviders` (`Kernel/Core`), **one name per concern** — 25 of them at v18.3.0 (`Namespaces`, `EventSequences`, `ObserverDefinitions`, `ObserverState`, `FailedPartitions`, `Jobs`, `JobSteps`, `Recommendations`, `Projections`, `ProjectionsManager`, `ProjectionFutures`, `Reactors`, `Reducers`, `ReducersManager`, `ReadModels`, `ReadModelsManager`, `ReadModelReplayManager`, `Constraints`, `EventStoreSubscriptionsManager`, `EventSeeding`, `Webhooks`, `WebhooksManager`, `DataProtectionKeys`, `PatchManager`, `System`). Reference a provider only through its constant — never a magic string, and there is no "default" provider.
- Each concern has its own `IGrainStorage` implementation (`JobGrainStorageProvider`, `EventSequencesStorageProvider`, `NamespacesStateStorageProvider`, …) taking the kernel's `IStorage` abstraction — **not** a MongoDB collection. The storage backend (MongoDB, SQL, in-memory) is selected behind `IStorage`, so a grain storage provider never references a database driver.
- Every provider is registered once, in `StorageProviderExtensions.AddStorageProviders(this ISiloBuilder)`, as a keyed singleton wrapped in `ResilientGrainStorage` (a Polly resilience pipeline configured from `Chronicle:ResilientStorage`). A new concern adds a constant, an `IGrainStorage` over `IStorage`, and one `AddKeyedSingleton(WellKnownGrainStorageProviders.<Name>, CreateResilientStorageFor<…>)` line there — nothing else.
- Grains declare their state with `[PersistentState(nameof(<StateType>), WellKnownGrainStorageProviders.<Name>)]`.

```csharp
public class JobGrainStorageProvider(IStorage storage) : IGrainStorage
{
    // ReadStateAsync / WriteStateAsync / ClearStateAsync over
    // storage.GetEventStore(key.EventStore).GetNamespace(key.Namespace).Jobs
}

services.AddKeyedSingleton(WellKnownGrainStorageProviders.Jobs, CreateResilientStorageFor<JobGrainStorageProvider>);
```
