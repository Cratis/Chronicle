---
title: Define event sources
description: Define registered event sources and append events through their streams from the .NET client.
---

An event source definition names a kind of event source and the streams it owns. Chronicle registers the definitions discovered by the .NET client when the event store connects. An event source is optional: existing append APIs remain valid and append events without a definition.

```csharp
using Cratis.Chronicle.EventSources;

[EventSource("ShoppingCart", Description = "A customer's cart", Concurrency = ConcurrencyDimensions.EventSourceId)]
[EventStream("Items", Description = "Cart item changes")]
[EventStream("Payment")]
public class ShoppingCartEventSource : IEventSource;
```

The name becomes the event source type stored with events appended through the definition. Stream names become event stream types. Chronicle does not impose a format for an event stream ID; the caller supplies it.

Append through the definition with the generic extensions:

```csharp
await eventStore.EventLog.Append<ShoppingCartEventSource>(
    cartId,
    new ItemAdded(productId),
    eventStream: "Items",
    eventStreamId: cartId.ToString());
```

Chronicle rejects an undeclared stream and routing that contradicts the definition. It records the definition name in `EventContext.EventSource`, while `EventSourceType` and `EventStreamType` contain the resolved names.

## Concurrency

When an append does not supply a concurrency scope, Chronicle narrows the optimistic scope to the dimensions declared on the stream. A stream without its own dimensions inherits the event source dimensions. `ConcurrencyDimensions.None` preserves the ordinary append behavior.

Build an explicit scope from the same declaration when needed:

```csharp
var scope = await eventStore.EventLog.ConcurrencyScope()
    .ForEventSource<ShoppingCartEventSource>("Items", cartId.ToString())
    .Build();
```

### Batches that need different guards for one event source id

An `AppendMany` call sends one concurrency scope per event source id. When a batch goes through event source definitions and you do not pass a scope for an id, Chronicle resolves the guard for every event with that id and compares them by what they check: event source id, event source type, event stream type, event stream id and event types. The expected sequence number is not part of the comparison, because it is read at a point in time.

- Events whose guards check the same thing share one scope, and the first guard is kept.
- An event whose strategy produces no guard, such as `ConcurrencyScope.None`, never hides a later event that does have one.
- Events that need different guards cannot be sent, for example items for the streams `2025-01` and `2025-02` of the same cart, or two different event sources sharing an id. Chronicle throws `IncompatibleConcurrencyScopesForEventSource` before anything is appended instead of guarding one event and leaving the other unguarded.

:::caution[Fail closed, not broadened]
Chronicle never widens a guard to cover the difference. Pass an explicit `ConcurrencyScope` for the event source id that suits every event in the batch (an explicit scope always takes precedence), or append the events in separate batches. Batches that do not use event source definitions behave as before.
:::
