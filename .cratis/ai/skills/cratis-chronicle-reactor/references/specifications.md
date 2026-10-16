<!-- cratis-ai-managed: skills/cratis-chronicle-reactor/references/specifications.md -->
# Reactor specifications

## Contents

- Specifications
- Routing and produced events
- Read-model setup
- Exceptions and occurrence time
- Contract coverage

## Specifications

Start with `Specification` plus a direct call for pure reactor decisions,
passing the trigger and supplied values explicitly and asserting the returned
command or event. Add `ReactorScenario<TReactor>` only where invocation,
dependency wiring or side effects contribute proof. In those scenarios, drive
events with a service provider of substitutes and assert on the substitutes for
non-event side effects; for handlers that return events, assert the resulting
appends through the scenario's event store. Cover the replay path separately
when the reactor has `[OnceOnly]` or `[Replay]` handlers.

## Routing and produced events

Scenarios implement `IDisposable`; dispose in `Destroy()`.
`Given.ForEventSource(id).OnStream(eventSourceType, eventStreamType, eventStreamId).Events(...)`
seeds routing for class-based and fluent reactors. Source/stream/typed definition
filters are honored: unqualified seeds do not reach a handler filtered to a
specific stream. Specify both a matching event and an excluded event.

`ProducedEvents` records each `ProducedEvent` **with its destination**.
`ShouldHaveProduced<TEvent>(eventSourceId)` checks routing; the
`ShouldHaveProduced<TEvent>((produced, @event) => ...)` overload checks metadata
and payload together. Do not assert only the payload when a cross-stream target
is part of the contract. Per-run `Defaults(clientArtifactsProvider, serviceProvider,
options)` supports `InMemoryClientArtifactsProvider` and `ExplicitArtifacts`;
non-generic `ReactorScenario(reactorId, defaults)` exercises a runtime/fluent reactor.
Sources: `Source/Clients/Testing/Reactors/` and `Source/Clients/Testing/Defaults.cs`


## Read-model setup

Requires Chronicle 19.46.0 or later for class-based reactor read-model seeding, scenario exception unwrapping and the UTC `EventContext.From` default.

Class-based `ReactorScenario<TReactor>` does not build read models from `Given`
events, **including passive models**. Seed a pre-built instance in `Establish()`:

```csharp
void Establish() => _scenario.Given.ForEventSourceId(_id).ReadModel(_order);
async Task Because() => await _scenario.Given.ForEventSource(_id).Events(_orderPlaced);
```

For a **separate absence specification**, use this setup instead, so an `Order?`
handler parameter deliberately receives `null`:

```csharp
void Establish() => _scenario.Given.ForEventSourceId(_id).NoReadModel<Order>();
```

The builders live in `Cratis.Chronicle.Testing.Reactors`. Exact signatures on
`ReactorSourceGivenBuilder<TReactor>` (`where TReactor : class`; plain reactors require explicit nomination):

```csharp
public ReactorSourceGivenBuilder<TReactor> ReadModel<TReadModel>(TReadModel readModel)
    where TReadModel : class;
public ReactorSourceGivenBuilder<TReactor> NoReadModel<TReadModel>()
    where TReadModel : class;
public Task Events(params object[] events);
```

Both `Given.ForEventSource(EventSourceId)` and
`Given.ForEventSourceId(EventSourceId)` return this builder. Seed or declare
absence using the triggering source id by default, or the key returned by
`ICanResolveReadModelKey.Resolve` when implemented. Declarations are shared
across the scenario's builders, match the **exact type and key**, and do not
remove a seeded instance. Do not declare absence to hide forgotten setup.

If a recognized read-model parameter resolves to `null` without declared
absence, `Events(...)` throws `ReadModelNotSeededForReactorScenario` from
`Cratis.Chronicle.Testing.Reactors`, **even for nullable parameters**. Its message
names the full read-model type and key, tells you to seed via `ReadModel(...)`
or declare `NoReadModel<T>()`, and lists other seeded keys for that type when
any exist. Check the key first if the fixture already seeds that type. This
guard applies only when the scenario uses `EventStoreForTesting` (including an
explicit instance); an explicitly supplied store of another type and production
read-model resolution are unchanged.

## Exceptions and occurrence time

`Events(...)` rethrows a synchronously throwing handler's **own exception**,
unwrapping the `TargetInvocationException` previously surfaced by reflection,
and preserves the handler stack. Capture with
`await Catch.Exception(() => _scenario.Given.ForEventSource(_id).Events(_orderPlaced))`
and assert the handler's exception type, not a reflection wrapper.

`EventContext.From(...)` defaults omitted `occurred` to `DateTimeOffset.UtcNow`
(previously `DateTimeOffset.Now`). `ReactorScenario` uses this helper, as do
`EventContext.Empty` and `EmptyWithEventSourceId(...)`. Explicit timestamps keep
their supplied offset; the positional constructor does not default `Occurred`.
For deterministic time-dependent plain-call specs, supply a fixed occurrence
time rather than asserting an exact current instant.

Sources:

- `Source/Clients/Testing/Reactors/ReactorSourceGivenBuilder.cs` and
  `ReactorScenarioGivenBuilder.cs`: seeding, absence and builder signatures.
- `Source/Clients/Testing/Reactors/ScenarioReactorMethodArgumentsResolver.cs` and
  `ReadModelNotSeededForReactorScenario.cs`: guard scope, resolved key and message.
- `Source/Clients/Testing/Reactors/ReactorScenario.cs`: synchronous exception
  unwrapping and context creation.
- `Source/Clients/DotNET/Events/EventContext.cs`: UTC default and explicit time.
- `Source/Clients/Testing.Specs/Reactors/for_ReactorScenario/when_resolving_read_model_dependencies/`:
  seeded, absent, nullable, passive, custom-key and explicit-store examples.
- `Source/Clients/Testing.Specs/Reactors/for_ReactorScenario/when_a_reactor_handler_throws_synchronously.cs`
  and `when_a_reactor_reads_the_occurrence_time.cs`: handler exception and UTC specs.

## Contract coverage

Specify the contract, not only the happy path:

- **Field lineage.** For each returned command or event, a specification asserts
  every field value against the trigger event, the injected read model, or the
  mapping the contract states. Use distinct values per source so a swapped or
  defaulted field fails.
- **Contract-authorized filtering.** A reactor skips an event only for a
  condition the contract states. Give each stated condition a specification
  that shows the skip, and one showing the neighbouring case that does act. Do
  not add a skip nobody specified.
- **Repeated delivery.** Recovery re-delivers the same event even with
  `[OnceOnly]`, so specify what the reactor does the second time (one effect, an
  idempotent write, or a receipt keyed by `ReactorDelivery`). The scenario gives
  every event passed through `Given.Events` its own sequence number, so firing
  the same event twice there is two occurrences, not redelivery. Requires
  Chronicle 19.61.0 for `History` and `Redeliver`: record without invoking using
  `var occurrence = await scenario.Given.ForEventSource(id).History(@event);`,
  then `await scenario.Redeliver(occurrence);` twice. The original context,
  stream routing and `ReactorDelivery` identity are preserved, with ordinary
  filters, explicit read-model setup and produced-event recording. Both
  class-based and fluent scenarios support it; `History` does not append to the
  event store. Check the testing package version; on older packages call the
  handler twice with the same context/delivery or test the receipt collaborator. Map a contract redelivery specification
  one to one: deliver the same trigger with the same delivery identity, or call directly with the
  command already applied, and assert its disposition (no new events, the named fact, or refusal
  surfaced as failure). The refusing-command procedure is in `slice-delivery-checklist.md`.
- **Read-model timing.** An injected materialized model has its own observer; resolving it does
  not pin its state to the trigger's sequence number. It can lag the trigger or include later
  events. Seed the state the contract means with `Given.ForEventSourceId(id).ReadModel(instance)`:
  matching state dispatches, non-matching state does not, and a separate specification uses the
  state before the setup event (or declared absence). These are decision specifications, not
  proof of production ordering. For a decision exactly as of the trigger, prefer values carried
  on that event. A `[Passive]` model computes on demand but is not an as-of snapshot; a
  state-dependent invariant requiring atomic protection belongs in the invoked command through
  `cratis-chronicle-decision-reads`.
- **Per-item fan-out.** Seed two matching entries and one non-matching entry; assert exactly two
  effects, each with its entry's values. When the contract requires independent progress,
  specify an acknowledged duplicate refusal for one entry and prove the other entry still
  completes. Unexpected refusals remain failures; successful entries must tolerate retry.
- **Unrelated events.** `WithStrictEventSubscription()` belongs to
  `ReadModelScenario<T>` and only fails a specification that *seeds* an event the
  projection does not subscribe to; it does not prove a subscription set, and
  `ReactorScenario<T>` has no equivalent. For a reactor, show with a
  specification that an event outside its handlers causes no effect.
