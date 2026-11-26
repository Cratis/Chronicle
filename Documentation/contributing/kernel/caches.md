---
title: Kernel caches keyed by namespace, event store or observer
description: Every in-memory table of the Chronicle kernel that grows with event stores, namespaces or observers, what bounds it and what releases it.
---

A kernel node holds one table per cache below for as long as it runs. This page records what each one is keyed by, what bounds its size, and what removes an entry, so a new cache can be judged against the same questions: what makes it grow, and what makes it shrink.

Two kinds of key matter. A table keyed by event store or namespace is bounded by how many of those exist, which an operator decides. A table keyed by anything that comes and goes while the kernel runs, such as a subscriber, an observer or a partition, must release its entries itself, or it grows for the life of the process.

## Keyed by observer or subscriber

| Cache | Key | Bound | Released by |
| --- | --- | --- | --- |
| Unpartitioned delivery gates (`UnpartitionedGates` in `ObserverSubscriberExtensions`) | Subscriber grain id | The subscribers delivering right now | The last caller to leave. A gate is reference counted and its entry is removed when no caller holds or awaits it. |

The gate table used to keep one `SemaphoreSlim` for every subscriber that had ever delivered an oversized batch to an unpartitioned subscriber, and never removed it. Delivery to an unpartitioned subscriber still runs one call at a time per silo; only the lifetime of the gate changed.

## Keyed by event store

These grow with the number of event stores. The registries added for event types and observer definitions belong here: they are created once by the event store's storage and are shared by every namespace of that store, so adding a namespace adds nothing to them.

| Cache | Key | Notes |
| --- | --- | --- |
| `EventTypeRegistry` and its snapshot | One per event store | An immutable snapshot, replaced rather than grown. Not keyed by namespace. |
| `ObserverDefinitionRegistry` and its snapshot | One per event store | The same shape. Not keyed by namespace. |
| `UniqueConstraintKeyedEncoders`, `EventSequenceMutationDigestKeys`, `KernelProjectionRegistrationRetries`, `ForwardedEventGuard` | Event store | One small entry per event store. |
| Storage providers' event store tables (`Storage`, `EventStoreStorages`, MongoDB `Database._eventStoreDatabases`) | Event store | One handle per event store. |

## Keyed by namespace

These are bounded by the number of namespaces that exist, and each entry is a small handle or flag.

| Cache | Key | Notes |
| --- | --- | --- |
| Event store storage namespace tables (`EventStoreStorage._namespaces`, MongoDB, SQL and in-memory) | Namespace | The MongoDB table removes the entry when the namespace is removed. The in-memory provider, used for tests and benchmarks, keeps it. |
| MongoDB `EventStoreDatabase`, `Database._readModelDatabases`, `NamedSinkDatabases` | Namespace, or event store and namespace, or sink, event store and namespace | A database handle each. Kept after a namespace is removed until the process restarts. |
| SQL `CachedDbContextOptions` for event stores and namespaces | Connection string | One entry per distinct connection. |
| `DormantObserverGauge` groups | Event store and namespace | Keeps a combination once it has reported, so its series reads zero rather than disappearing. |
| `ChronicleServerStartupTask` recovered jobs | Event store and namespace | A flag per namespace recovered at startup. |
| `ComplianceAuthorization` last gap | Event store, namespace and operation | One timestamp per combination. |
| `Projection` per-namespace projection cache | Namespace, in each projection grain | Grows with the namespaces the projection has served. Covered by the projections work, not here. |

An entry in a namespace table costs bytes, not kilobytes, so none of them is evicted today. If you add one whose entry holds a connection, a compiled model or a buffer, give it an owner that removes it when the namespace is removed.

## Where the `JsonSerializerOptions` instances come from

A heap dump of a production node showed a 4.2 MB `JsonSerializerOptions` tracking table, which means a very large number of options instances had been alive at some point. The kernel creates its own options in one of three ways:

- A static field, once per process.
- A singleton such as `EventTypes`, once per process.
- `JsonSchema.FromType(Type)`, which builds a new options instance on every call. Its only caller in the kernel is `KernelProjectionDeclarations`, which runs when a kernel projection is declared, a bounded number of times per event store.

None of those can account for a hundred thousand instances. The source is not established from the kernel code. The GC experiment harness (see [Size the kernel for a container](../../hosting/container-resources.md)) reports how many options instances the runtime still tracks after its load window and after a forced collection, so a build that creates them per call shows up as a count that grows with the load.
