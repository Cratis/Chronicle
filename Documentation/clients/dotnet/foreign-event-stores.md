---
title: Append to a foreign event store without registering application artifacts
description: Use a per-store registration mode to append to another application's Chronicle event store or outbox while keeping your own store's registration unchanged.
---

An application can append events to another application's event store or outbox without registering its own projections, read models, reactors, reducers, constraints, subscriptions, or seed events there. Open the foreign store with a registration mode on `GetEventStore`; the client keeps its normal mode for your own store and shares one connection between both handles.

## Choose who owns registration

This example assumes the local kernel is running and your application references `Cratis.Chronicle`. Use `EventTypesOnly` when your application may ensure that the foreign store exists and register its local event catalog there:

```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Events;

using var client = new ChronicleClient(ChronicleConnectionString.Development);
var ownStore = await client.GetEventStore("Orders");
var foreignStore = await client.GetEventStore(
    "Shipping",
    @namespace: null,
    registrationMode: RegistrationMode.EventTypesOnly);

var result = await foreignStore.GetEventSequence(EventSequenceId.Outbox).Append(
    "order-123",
    new OrderReadyForShipping("order-123"));

if (!result.IsSuccess)
{
    throw new InvalidOperationException("The event could not be appended to Shipping's outbox.");
}

[EventType]
public record OrderReadyForShipping(string OrderId);
```

The client discovers and serializes `OrderReadyForShipping` locally. With `EventTypesOnly`, it ensures `Shipping` exists and registers the **entire local event catalog**, not just that event. The foreign store's owner must agree to those identities, schemas, generations, and visibility. This mode is not a way to resolve incompatible event definitions.

`Orders` still uses the client's default mode, `Inline`, so its application artifacts register normally. Appending through `foreignStore.EventLog` instead targets the foreign event log; the example explicitly selects its outbox.

Use `AtDeployment` when the foreign store's owner provisions the store and registers the necessary event identities and schemas:

```csharp
var foreignStore = await client.GetEventStore(
    "Shipping",
    @namespace: null,
    registrationMode: RegistrationMode.AtDeployment);
```

This handle still discovers the local artifacts needed for event identity lookup and serialization, but performs no automatic kernel registration or store provisioning. The append can fail if the owner has not provisioned a compatible store and event catalog. It does not require this client to host an observer callback endpoint just to append.

## Handle lifetime and explicit calls

The client caches handles by event-store name, resolved namespace, and registration mode. Repeated calls with the same effective mode return the same handle, including when one call uses the original overload and another specifies the matching mode. Different modes for the same store and namespace return separate handles sharing the connection. An inline handle can therefore still register artifacts for that store; a restricted handle is not a store-wide protection.

The mode is captured when a handle is created and remains in effect for reconnects and `RegisterAll()` calls. Changing `ChronicleOptions.RegistrationMode` later changes the default for subsequent lookups, not existing handles. Passing `null` for the namespace uses the same observer-delivery context and namespace resolver as the original overload; pass an explicit namespace to target a particular tenant.

These modes govern the automatic registration pipeline, including manual `RegisterAll()`, not authorization. They do not block deliberate calls such as `foreignStore.Projections.Register(...)`. They also do not turn on discovery or automatic registration when `AutoDiscoverAndRegister` is `false`.

For a process that has only one registration policy, see [short-lived processes](./short-lived-processes.md). To configure artifacts shared by the client's stores, see [explicit artifact registration](./explicit-artifacts.md).
