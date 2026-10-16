<!-- cratis-ai-managed: skills/cratis-multi-service-architecture/references/publish-and-consume.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Publishing and consuming a contract between two stores

## Contents

- The example system
- Publish with an event-target projection
- Publish from a reactor
- Consume through implicit routing
- Consume through an explicit subscription
- Subscribe from the CLI
- Make a consumer side effect idempotent
- Checks

## The example system

`Ordering` and `Billing` are two services, each with its own event store of the
same name on one kernel. `Ordering` records a private `OrderPlaced`, publishes the
public `OrderAccepted`, and `Billing` turns it into its private `InvoiceRequested`.
Each service's configuration names its own store:

```json
{
  "Cratis": {
    "Chronicle": {
      "EventStore": "Ordering"
    }
  }
}
```

The contracts project (`Ordering.Contracts`, referencing only `Cratis.Chronicle`)
carries `[assembly: EventStore("Ordering")]` through the `AssemblyAttribute` item
shown in the skill and holds `OrderAccepted`. `Billing` references the contracts
project or its package, never `Ordering` itself. The `AssemblyAttribute` item needs
`GenerateAssemblyInfo` left at its default `true`.

## Publish with an event-target projection

The default. A projection whose target is an `[EventType]` record publishes the
folded state as that event, to the outbox unless the event type carries
`[PublishTo("<sequence>")]`. The key is the event source id, so the published event
keeps the order's event source id and the consumer reads it from `EventContext`.

```csharp
// Ordering/Placing/OrderPlaced.cs
using Cratis.Chronicle.Events;

namespace Ordering.Placing;

[EventType]
public record OrderPlaced(decimal Total);
```

```csharp
// Ordering/Placing/OrderAcceptedPublisher.cs
using Cratis.Chronicle.Projections;
using Ordering.Contracts;

namespace Ordering.Placing;

public class OrderAcceptedPublisher : IProjectionFor<OrderAccepted>
{
    public void Define(IProjectionBuilderFor<OrderAccepted> builder) => builder
        .From<OrderPlaced>(_ => _
            .Set(m => m.Total).To(e => e.Total));
}
```

What the event-sequence sink guarantees: each change is one append whose
publication identity is deterministic, so a retried or redelivered step returns
the event already committed instead of a second one. A step that changes nothing
publishes nothing. A change to the projection's mapping triggers a replay that
publishes new instances for the same source events; consumers see them as new
facts. Registration fails on the client with
`PrivateEventTypeCannotBePublishedToOutbox` when the target is not public, with
`EventLogIsNotAPublicationTarget` for the event log, with
`EventSequenceTargetCarriesCompliance` for a `[PII]` target, and with
`EventPublisherConflict` for a second publisher of the same type to the same
sequence.

## Publish from a reactor

Use this only when a fold cannot express the public event. The reactor appends to
the outbox and must fail its partition when the append is refused; a refused append
returns errors and throws nothing, so ignoring the result silently drops the
publication. Recovery re-runs the handler, so the same contract fact can be
appended again.

```csharp
// Ordering/Placing/PublishOrderAccepted.cs
using Cratis.Chronicle;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Ordering.Contracts;

namespace Ordering.Placing;

public class OrderAcceptedNotPublished(AppendResult result)
    : Exception($"Publishing OrderAccepted failed with {result.Errors.Count()} error(s) and {result.ConstraintViolations.Count()} constraint violation(s).");

public class PublishOrderAccepted(IEventStore eventStore) : IReactor
{
    [OnceOnly]
    public async Task Placed(OrderPlaced @event, EventContext context)
    {
        var result = await eventStore
            .GetEventSequence(EventSequenceId.Outbox)
            .Append(context.EventSourceId, new OrderAccepted(@event.Total));
        if (!result.IsSuccess) throw new OrderAcceptedNotPublished(result);
    }
}
```

`[OnceOnly]` keeps a replay of this reactor from publishing again; it does not
stop recovery from doing so.

## Consume through implicit routing

The default for a .NET consumer. Because `OrderAccepted`'s assembly names the
`Ordering` store, the client routes this reactor to `inbox-Ordering` and registers
a kernel subscription for the handled types after registering its observers. The
returned event is appended to `Billing`'s event log under the incoming event
source id.

```csharp
// Billing/Invoicing/InvoiceRequested.cs
using Cratis.Chronicle.Events;

namespace Billing.Invoicing;

[EventType]
public record InvoiceRequested(decimal Total);
```

```csharp
// Billing/Invoicing/AcceptOrders.cs
using Cratis.Chronicle.Reactors;
using Ordering.Contracts;

namespace Billing.Invoicing;

public class AcceptOrders : IReactor
{
    [OnceOnly]
    public InvoiceRequested Accepted(OrderAccepted @event) => new(@event.Total);
}
```

A reducer or projection handling contract types is routed the same way, but keep
read models on the consumer's private events (see the skill). When the contract
types carry no `[EventStore]`, put `[EventStore("Ordering")]` on the observer class
instead, never together with `[EventSequence]`.

## Consume through an explicit subscription

Use this when the consumer is not .NET, when the contract types carry no store, or
when types are chosen at run time. Register it at startup through the singleton
client, then observe the inbox explicitly:

```csharp
// Billing/Program.cs (excerpt, after builder.Build())
var client = app.Services.GetRequiredService<IChronicleClient>();
var billing = await client.GetEventStore("Billing");
await billing.Subscriptions.Subscribe(
    "ordering-to-billing",
    "Ordering",
    subscription => subscription.WithEventType<OrderAccepted>());
```

```csharp
// Billing/Invoicing/AcceptOrders.cs
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;
using Ordering.Contracts;

namespace Billing.Invoicing;

[EventSequence(EventSequenceId.InboxPrefix + "Ordering")]
public class AcceptOrders : IReactor
{
    [OnceOnly]
    public InvoiceRequested Accepted(OrderAccepted @event) => new(@event.Total);
}
```

A class-level `[EventSequence]` takes priority over the contract's store for
routing, and the client registers no implicit subscription for such an observer.
`Subscribe` is idempotent per subscription id, so calling it on every start is
safe. Without `WithEventType`, the subscription covers every event type the client
knows when it registers; types it does not know are not forwarded. `Unsubscribe`
stops forwarding and keeps the inbox events; subscribing again with the same id
resumes from that point, not from the producer's beginning.

## Subscribe from the CLI

```bash
cratis chronicle subscriptions add ordering-to-billing Ordering <OrderAccepted-type-id> -e Billing
cratis chronicle subscriptions list -e Billing
```

`add` takes comma-separated event type ids (list them with
`cratis chronicle event-types list -e Ordering`) and does not prompt; `remove`
prompts unless given `--yes`. Both mutate a running store: follow the
authorization discipline in `cratis-chronicle-cli-operations`.

## Make a consumer side effect idempotent

For an external side effect in a consumer reactor, take a `ReactorDelivery`
parameter and keep a receipt under `delivery.Id` in storage the application owns:
check it before the effect and record it after. The identity is the same across
retry, recovery and replay of that inbox event. It differs for a second copy of the
same contract fact in the inbox, so when duplicates would cause harm, key the
receipt (or a unique constraint on the local fact) on a business identity the
contract carries. Chronicle documents the receipt pattern under reactors, delivery
identity: <https://www.cratis.io/chronicle/reactors/delivery-identity/>.

## Checks

- Producer specification: appending the private fact publishes exactly one contract
  event with the expected content; an identical repeat publishes nothing.
- Consumer specification: one contract event yields one private fact; a repeated
  delivery does not produce a second side effect.
- `cratis chronicle observers list -e Billing` shows the consumer observer on
  `inbox-Ordering`, and `cratis chronicle failed-partitions list -e Billing` is
  empty after an end-to-end run.
