---
uid: Chronicle.Namespaces
---
# Namespaces

Namespaces provide logical segregation within a single event store. They allow Chronicle to keep events, projections, reducers, and read models isolated per namespace while still sharing the same physical infrastructure.

Use namespaces to:

- Separate tenant data in multi-tenant systems
- Partition environments like development, staging, and production
- Split business domains that share an event store

Namespaces are resolved for every operation. If no namespace is explicitly provided, the default namespace is used.

## Namespaces and tenancy

Namespaces are a practical way to implement tenant isolation. Each tenant can map to a namespace, ensuring that all event store operations remain scoped to that tenant. This keeps data separated without requiring separate event stores per tenant.

If you use Cratis Arc with the Chronicle extension, tenancy is automatically configured for namespaces. See [Arc Chronicle tenancy](/arc/backend/csharp/chronicle/tenancy/) for details.

For the whole picture, from selecting a tenant to deciding which database it lives in, see [Tenancy](../tenancy/index.md).

## Removing a namespace

Removing a tenant means removing its namespace. Stop every client that uses the namespace, then call `RemoveNamespace` with its name on the event store (`IEventStore.RemoveNamespace` in the .NET client).

Chronicle then, in order:

1. Unsubscribes and removes every observer in the namespace. Removing an observer also removes the reminders that would otherwise wake it at the next start and bring the namespace back.
2. Drops the data it stores for the namespace - the event store namespace database with its event log, observer state and indexes. With the MongoDB storage provider that is the `<event store>+es+<namespace>` database.
3. Forgets the namespace, so it is no longer listed, and appends a `NamespaceRemoved` event to the system event sequence.

Things to know:

- The default namespace cannot be removed.
- Removing a namespace that does not exist does nothing, and an interrupted removal can be repeated.
- Read model data written by projections and reducers lives in its own database. Chronicle does not drop it - your application owns that data, so remove it as part of retiring the tenant.
- A client that still has the namespace open will bring it back the moment it appends an event, because namespaces are created on first use.
- Other storage providers do not yet drop the stored data; the observers are removed and the namespace is forgotten.

## Topics

- [DotNET client usage](dotnet-client)
- [ASP.NET Core namespace resolution](aspnetcore)

