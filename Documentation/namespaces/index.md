---
title: Manage Cratis Chronicle namespaces
uid: Chronicle.Namespaces
description: Use Cratis Chronicle namespaces to separate tenant data within one event store, choose namespace resolution, and remove a namespace when retiring a tenant.
---

Cratis Chronicle namespaces separate tenant data within a single event store. Each namespace has its own events, observer progress, and read models while sharing the Chronicle deployment. Use one namespace per tenant when tenants share an application but need separate data; choose how the client selects that namespace before handling tenant operations.

A namespace can also separate environments or business domains that share an event store. This is data separation, not a separate deployment or an authorization policy.

If you do not specify a namespace or configure a resolver that selects another one, the .NET client uses `EventStoreNamespaceName.Default`. A resolved event store remains bound to its namespace; changing tenant context does not retarget an instance you already obtained.

For database naming and the effect on read-model queries, see [Namespaces and data isolation](../concepts/namespaces.md).

## Namespaces and tenancy

Map an authorized tenant id to a namespace so the event store instance operates on that tenant's data. Namespace selection does not check whether the caller may access that tenant. Your application must validate tenant access and decide whether a missing tenant may use the default namespace.

The Cratis Arc Chronicle integration registers a resolver that maps Arc's current tenant to a namespace. You do not need a second resolver for the same tenant context. See [Arc Chronicle tenancy](/arc/backend/csharp/chronicle/tenancy/) for the mapping and its default-tenant behavior.

For the full path from selecting a tenant to deciding which database stores its data, continue with [Tenancy](../tenancy/index.md).

## Removing a namespace

When retiring a tenant, stop requests and background clients that use its namespace before calling `RemoveNamespace` with its name on the event store (`IEventStore.RemoveNamespace` in the .NET client). Otherwise, continued use can create the namespace again. The default namespace cannot be removed.

Chronicle performs removal in this order:

1. Removes the namespace from the listing and attempts to append a `NamespaceRemoved` event to the system event sequence. Removing it from the listing first prevents periodic subscription setup from selecting it again.
2. Unsubscribes and removes its observers, including their persistent reminders. Those reminders would otherwise wake observers after a restart.
3. Calls the storage provider to drop namespace data, then deletes the namespace record from storage. MongoDB drops the database containing the namespace's events, observer state, and indexes. With default database naming, this is `<event store>+es+<namespace>`.

Namespace removal does not drop the separate read-model data written by projections and reducers. Remove that application-owned data as part of retiring the tenant. Other storage providers leave stored namespace data in place, so plan to remove it separately as well.

An interrupted removal can be repeated. Removal is not a permanent ban on the namespace name: using that name again can create a new namespace.

## Topics

- [DotNET client usage](dotnet-client): select a namespace in a direct client or worker service.
- [ASP.NET Core namespace resolution](aspnetcore): select a namespace from an HTTP request and enforce tenant access.

