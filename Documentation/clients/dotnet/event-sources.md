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
