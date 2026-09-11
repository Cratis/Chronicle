---
name: cratis-chronicle-read-model-specifications
description: Specify projection and reducer behavior with ReadModelScenario from Cratis.Chronicle.Testing — seeding events through Given and asserting on the materialized instance. Use when the behavior under specification is how events become read-model state. Do not use to create a read model and do not use for raw event append behavior.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-chronicle-read-model-specifications/SKILL.md -->

# Chronicle read-model specifications

`ReadModelScenario<TReadModel>` runs a projection or a reducer in-process
against a sequence of events. No Chronicle server, no database, no network — the
events go in, the materialized instance comes out.

For a read-model specification the **`Given` *is* the act**. There is no
separate `When`: seeding the events is what makes the projection run.

## Verified product sources

| Package | Version | Purpose |
| --- | --- | --- |
| `Cratis.Chronicle.Testing` | `16.45.2` | `ReadModelScenario<TReadModel>` and its `Given` builder |
| `Cratis.Specifications.XUnit` | `4.x` | The `Specification` base and the `ShouldXxx` assertions |

```bash
dotnet add package Cratis.Chronicle.Testing
```

Reverify against the Chronicle repository before claiming support for another
version.

## Route near misses

- Event appending, constraints, or the event log's own behavior: use
  `cratis-chronicle-event-specifications`.
- A command running through validators, `Provide()` and `Handle()`: use
  `cratis-application-slice-specifications`.
- Deciding what the read model or projection should *be*: use
  `cratis-chronicle-read-model`, `cratis-chronicle-projection`, or
  `cratis-chronicle-reducer`.

## When you need this

- A reducer builds state from a sequence of events.
- A fluent `IProjectionFor<T>` maps event properties onto read-model properties.
- A model-bound projection — `[FromEvent<T>]`, `[SetFrom<T>]`, `[Key]` — maps
  correctly.
- A boundary matters: the first event, several events, several event sources.

## Step 1 — Create the scenario

A new scenario per specification, never shared state.

```csharp
// Default, empty initial state
var scenario = new ReadModelScenario<MyReadModel>();

// With a baseline
var scenario = new ReadModelScenario<CartSummary>(new CartSummary { ItemCount = 10 });
```

Further constructors take a service provider, a `Defaults`, or both, alongside
the initial state. Use the named-parameter form when you need only one of them:
`new ReadModelScenario<T>(initialState: null, serviceProvider: services)`.

## Step 2 — Seed the events

```csharp
await scenario.Given
    .ForEventSource(myId)
    .Events(new SomeEvent("value"), new SomeOtherEvent(42));
```

Chain a second `ForEventSource` for another event source — a cross-stream
projection is seeded by giving each contributing stream its own call:

```csharp
await scenario.Given.ForEventSource(orderId).Events(new OrderCreated("order-1"), new ItemAdded(9.99m));
await scenario.Given.ForEventSource(anotherOrderId).Events(new OrderCreated("order-2"));
```

`ForEventSourceId(...)` is an alias for `ForEventSource(...)`. The builder also
offers `.ReadModel(instance)` to pin a materialized instance for code that calls
`IReadModels.GetInstanceById` rather than deriving it from events.

Events are processed in the order supplied. Seed before asserting, and do not
try to separate a "setup" `Given` from an "act" `Given` — for a read model they
are the same phase.

## Step 3 — Assert on the instance

```csharp
_scenario.Instance!.Total.ShouldEqual(14.49m);
_scenario.Instance!.Name.ShouldEqual("Widget");
```

- `Instance` materializes lazily on first access and is `null` when nothing was
  produced. It throws `MultipleInstancesMaterialized` when the seeded events
  produced more than one — a single answer would be ambiguous.
- `Instances` is the whole dictionary keyed by event-source id, and
  `InstanceForEventSourceId(id)` picks one. Use those when the specification
  deliberately seeds several sources.
- **There are no `Should*` assertions on `ReadModelScenario`**, as members or as
  extensions. Assert on `Instance` with the ordinary `ShouldXxx` assertions —
  and note the shipped name is `ShouldEqual`, not `ShouldBe`.

Do not pre-emptively skip an assertion. Assume scalar concept, enum, and
identifier properties populate; when one does not, investigate the projection
rather than the harness. Skip only on a reproduced harness gap, and put the
specific reason in the skip message.

## Step 4 — Know what the scenario picked

The scenario finds a handler for `TReadModel` in this order:

1. A **reducer** — a class implementing `IReducerFor<TReadModel>`.
2. A **fluent projection** — a class implementing `IProjectionFor<TReadModel>`.
3. **Model-bound projection attributes** on `TReadModel` itself.
4. A separate **model-bound projection** type registered for it.

If none is found it throws `NoReadModelHandlerFound`. When a read model has both
a reducer and a projection, the reducer wins — which is worth knowing when a
specification exercises a path the running application does not.

For stricter runs, `WithStrictEventSubscription()` and `WithStrictFidelity()`
turn silent mismatches into failures.

## Step 5 — Write the specification

```csharp
#if DEBUG
namespace MyApp.Ordering.Orders.when_items_are_added;

public class and_two_items_are_priced : Specification
{
    ReadModelScenario<OrderSummary> _scenario;
    static readonly OrderId TheOrder = OrderId.New();

    async Task Establish()
    {
        _scenario = new ReadModelScenario<OrderSummary>();
        await _scenario.Given
            .ForEventSource(TheOrder)
            .Events(
                new OrderCreated(),
                new ItemAdded(9.99m),
                new ItemAdded(4.50m));
    }

    [Fact] void should_sum_the_item_prices() => _scenario.Instance!.Total.ShouldEqual(14.49m);
}
#endif
```

Wrap every file in `#if DEBUG … #endif` so specification code ships only in
Debug, and keep one outcome per `should_` fact.

## What breaks

- **`NoReadModelHandlerFound`.** Nothing in the loaded assemblies handles
  `TReadModel` — the reducer or projection type is not where the scenario looks.
- **`MultipleInstancesMaterialized` on `Instance`.** The seeded events produced
  more than one instance. Use `InstanceForEventSourceId(id)`, or seed one source.
- **`Instance` is `null`.** No event in the seeded set matched the projection, so
  nothing was created. Check the key resolution before the property mapping.
- **A property is silently the type default.** AutoMap wired a different event's
  identically named property over the explicit setter, or the explicit setter
  never fired. This looks like a specification problem and is a projection
  problem.
- **The specification passes but production does not.** The specification pinned
  a read-model instance instead of seeding events, so the projection under
  question never ran.

## How it is proven

`dotnet build` in Debug and `dotnet test`, both clean. A projection
specification is only meaningful once it has been seen to fail: drop one seeded
event and confirm the assertion goes red before trusting it green.
