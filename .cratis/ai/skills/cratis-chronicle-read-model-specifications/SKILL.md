---
name: cratis-chronicle-read-model-specifications
description: Specify declarative projections and reducer wiring with ReadModelScenario from Cratis.Chronicle.Testing — seeding events through Given and asserting on the materialized instance; start with Specification plus a direct call for pure reducer decisions. Use when the behavior under specification is how events become read-model state. Do not use to create a read model and do not use for raw event append behavior.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-chronicle-read-model-specifications/SKILL.md -->

# Chronicle read-model specifications

`ReadModelScenario<TReadModel>` runs a projection or a reducer in-process
against a sequence of events. No Chronicle server, no database, no network — the
events go in, the materialized instance comes out.

Pure reducer decisions use `Specification` plus a direct method call by
default, with the event and current state supplied explicitly (for a self model,
`Model.Reduce(@event, current)`). Add
`ReadModelScenario<TReadModel>` only where declarative projection behavior,
event dispatch, filtering, metadata or read-model wiring contribute proof.

For a read-model scenario specification the **`Given` *is* the act**. There
is no separate `When`: seeding the events is what makes the projection run.

## Product sources

| Package | Purpose |
| --- | --- |
| `Cratis.Chronicle.Testing` | `Source/Clients/Testing/ReadModels/ReadModelScenario.cs`: scenario selection, self and nominated reducers |
| `Cratis.Chronicle` | `Source/Clients/DotNET/Reducers/{SelfReducerMethods,RegisteredReducerReadModel}.cs`, `ReadModels/ReadModelBuilderOwnership.cs`: supported forms and ownership |
| `Cratis.Specifications.XUnit` | The `Specification` base and the `ShouldXxx` assertions |

Source: `Source/Clients/Testing/ReadModels/` in `Cratis/Chronicle`.

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
- The scope is model-first (its read-model `specification` blocks and `then
  readmodel` rows are the contract). Decide this with the decision rule in
  `cratis-screenplay-modeling-lifecycle`: model-first only when an accepted
  model under the model root covers this scope, or the repository is opted in
  (opted in per `general.md` "Model first"; where `cratis-screenplay-modeling-lifecycle` is installed, run its `scripts/model-first-status.mjs`); framework, infrastructure, client, adapter and not-opted-in
  brownfield work stays code-first. Opted in but no model for this scope: model
  it first (`cratis-screenplay-discovery`, then `cratis-screenplay-slice-design`)
  before writing specifications. If those Screenplay skills are not installed,
  say so and stop; never author `.play` from memory. Never edit Stage-managed
  output, and never change a model specification to match existing code. Author model
  specifications with `cratis-screenplay-specifications` and
  `cratis-screenplay-scenario-coverage`; check finished code against them with
  `cratis-application-slice-conformance`.

## When you need this

- A reducer's event dispatch, filtering or read-model wiring needs proof.
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
Scenarios implement `IDisposable`; call `_scenario.Dispose()` in `Destroy()`.
`WithProjection(Action<IProjectionBuilderFor<T>>)` supplies an inline projection
and wins over discovery, **including a discovered reducer**. Per-run `Defaults`
can take a client artifacts provider, service provider and options;
`InMemoryClientArtifactsProvider` and `ChronicleOptions.ExplicitArtifacts` let a
specification isolate registrations without global discovery. Nominate a plain
instance reducer with `options.ExplicitArtifacts.RegisterReducer<PlainReducer>()`
before constructing per-run `Defaults(artifacts, serviceProvider, options)`.
The same API accepts a self model; default discovery needs no marker. Do not
mutate `Defaults.Instance` across independent runs.

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

For a filtered reducer, seed the routing metadata as well:
`Given.ForEventSource(id).OnStream(eventSourceType, eventStreamType, eventStreamId).Events(...)`.
`[EventSourceType]`, `[EventStreamType]` and `[FromEventSource<T>]` filters are honored;
unrouted seeds no longer reach a handler for another stream. Child-before-parent
writes are retried when the parent later arrives; decimal seeds retain exact values.

Requires Chronicle 19.53.1 for nested child `IdentifiedBy` paths to update/remove
the existing child rather than duplicate it, and 19.53.2 for in-memory child joins
to preserve the root and all matching siblings. Check the testing/client versions;
older in-memory outcomes can diverge from persistent sinks. Cover root properties,
sibling preservation and nested child update/removal, not only one joined value.
Requires Chronicle 19.58.0 for reducer tag filters and seeded tags/source/stream
metadata to match production; include matching and excluded events.

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

`WithProjection` overrides discovery, including a reducer. Otherwise:

1. An **explicit projection recipe** for `TReadModel` suppresses reducer selection.
2. A **registered reducer handler** targeting it: a self-reducing model, a
   separate `IReducerFor<TReadModel>`, or an explicitly nominated plain instance
   class. The scenario uses the store's handler catalog, not its own interface
   scan. Self signatures and registered event membership still matter.
3. A **fluent projection** implementing `IProjectionFor<TReadModel>`.
4. **Model-bound attributes** directly on `TReadModel`, then the matching
   model-bound entry in the artifacts provider.

If no handler exists, processing throws `NoReadModelHandlerFound`. Classic
reducer precedence does not authorize competing self-model builders: the store
rejects those with `AmbiguousReadModelBuilder`. An inline scenario override can
exercise a different path than production; do not mistake it for registration
proof. See [reducers](../cratis-chronicle-reducer/SKILL.md) for signatures and
static direct-call/scenario examples.

For every reducer form, cover empty history, first-event null current, guarded
branches, deletion and recreation. Scenarios substitute storage, observer
lifecycle and compliance; they do not prove real-kernel registration, replay,
transport, encryption, erasure or migration.

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

Wrap files in `#if DEBUG … #endif` only when they compile into the application
assembly, so specification code ships only in Debug. Dedicated spec projects
need no wrapper. Keep one outcome per `should_` fact.

## Step 6 — Cover the read model, not just one event

For `.play` setup, assertions and case tables, read `cratis-application-slice-specifications`, "Reading a `.play` specification".

One summation example proves one event. Take the inventory from the contract
(the `.play` read model and its specifications, or the agreed outline); without
one, derive it from the projection and say so. Then check each line:

- **Every property is asserted** in at least one specification.
- **Every subscribed event has a specification.** Reconcile three lists in both
  directions: the contract's event types, the projection's actual subscriptions,
  and the events your specifications seed. Report any event in one list missing
  from another; route that reconciliation to
  `cratis-application-slice-conformance`. `WithStrictEventSubscription()` is
  narrower: it fails a specification that *seeds* an event the projection does
  not subscribe to. It does not reveal an extra subscription that no
  specification seeds, nor a subscribed event lacking a specification.
- **Every removal or clearing event has a specification asserting absence**:
  `Instance` is `null`, or the child is gone from the collection. Absence is a
  result, not a skipped case.
- **A later event that updates only some properties** has a specification showing
  the other properties are unchanged.
- **Use the contract's example values** so the code and the contract describe the
  same example; substitute fresh identifiers only where isolation needs it,
  keeping the example's relationships. A read-model rule found in the projection with no contract
  specification is a proposal for the contract (the `.play` model when one covers the slice, otherwise the agreed outline), not silent coverage.

## Lineage

Step 6 is adapted in our own words from ideas recorded in `references/provenance.md`.
Read `references/view-lifecycle-checks.md` when delivering a read model from a contract: population, update and removal checks per property and event.

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
- **A removal is never exercised.** The suite is green because every
  specification only adds. Seed the event that removes or clears, then assert the
  instance is `null` or the child is absent.
- **The specification passes but production does not.** The specification pinned
  a read-model instance instead of seeding events, so the projection under
  question never ran.

## How it is proven

`dotnet build` in Debug and `dotnet test`, both clean. A projection
specification is only meaningful once it has been seen to fail: drop one seeded
event and confirm the assertion goes red before trusting it green.
