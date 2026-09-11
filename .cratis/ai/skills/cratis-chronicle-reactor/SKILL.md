---
name: cratis-chronicle-reactor
description: Implement a Chronicle IReactor - an automation that causes an external side effect, or a translation that appends follow-up events. Covers handler dispatch and parameter resolution, every supported return type, targeting another event source, replay handling with [OnceOnly] and [Replay], event filtering, failure and quarantine behavior, and the analyzer rules. Use for event-driven side effects; do not use to populate a read model.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-chronicle-reactor/SKILL.md -->

# Chronicle reactors

A reactor observes events and **does things**: calls an external system, or
appends follow-up events. A projection or reducer builds state; a reactor causes
effects. If the answer is "populate a queryable model", it is not a reactor.

| Need | Use |
| --- | --- |
| populate a queryable read model from events | a projection |
| a current-state-plus-event transition | a reducer |
| trigger a side effect outside the system | a reactor (**automation**) |
| append follow-up events elsewhere in the system | a reactor (**translation**) |
| both a model and an effect | one projection *and* one reactor |

## Verified product sources

This skill is verified against these exact sources:

| Package | Version | Purpose |
| --- | --- | --- |
| `Cratis.Chronicle` | `16.45.2` | `Cratis.Chronicle.Reactors`, `Cratis.Chronicle.Reactors.SideEffects`, `Cratis.Chronicle.EventSequences`, `Cratis.Chronicle.Events` |
| `Cratis.Chronicle.CodeAnalysis` | `16.45.2` | `CHR0004`, `CHR0005`, `CHR0008`, `CHR0013`, `CHR0022`, `CHR0031`, `CHR0032` |

Reverify product sources before claiming support for another version.

> **There is no `ReactorSideEffect` type.** It does not exist in Chronicle, in
> any version. The real return types are listed below; the per-event metadata
> control it was imagined to provide is what `EventForEventSourceId` actually
> gives you. Never write it into code or an example.

## Write the reactor

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

/// <summary>
/// <what this automation does and why>.
/// </summary>
public class <ReactorName>(<IServiceType> <serviceName>) : IReactor
{
    /// <summary>Reacts to <see cref="<EventName>"/>.</summary>
    public async Task <MethodName>(<EventName> @event, EventContext context) =>
        await <serviceName>.<Operation>(@event.<Property>);
}
```

`Cratis.Chronicle.Reactors.IReactor` is a **marker interface with no members**.
Reactors are discovered by that interface — the `[Reactor]` attribute is optional
and is not what makes a class a reactor. Generic types are not discovered.

## Dispatch and parameters

**Dispatch is by the first parameter's type.** The method name is for the reader
and plays no part. When two methods claim the same event type, the winner is
chosen by public before non-public, then more parameters before fewer, then
ordinal name order — so do not rely on it; write one handler per event type.

A handler takes **at least one** parameter. There is no upper limit. Everything
after the first is resolved by type, position-independently:

| Parameter type | Resolved as |
| --- | --- |
| `EventContext` | the event context |
| `ReactorDelivery` | reactor id, event store, namespace, event sequence, partition, sequence number |
| a type that has a projection or a reducer | that **read model**, materialized for this event |
| anything else | from the service provider |

An unresolvable parameter throws at invocation, and analyzer **CHR0004**
(warning) flags it at build time: after the event, a reactor method may take the
event context, a read model, or a service — not a primitive, a value type, or a
string.

The first parameter's type must carry `[EventType]`, or analyzer **CHR0005**
(error) rejects it.

### Injecting a read model

A read-model parameter is materialized using the event context's event-source id
by default. When the model's key differs from the triggering event's source —
the event carries a related entity's id — implement `ICanResolveReadModelKey` on
the reactor:

```csharp
public class <ReactorName> : IReactor, ICanResolveReadModelKey
{
    public ReadModelKey Resolve(object @event, EventContext context) =>
        ((<EventName>)@event).<RelatedIdProperty>;

    public Task <MethodName>(<EventName> @event, <ReadModelName> <readModel>) =>
        Task.CompletedTask;
}
```

The resolved key applies to every read-model parameter across all of the
reactor's handlers.

A **materialized** read model is read from its sink, so it is eventually
consistent — as current as its own observer. Mark the read model `[Passive]` when
the reactor needs it strongly consistent: a passive model has no sink and is
computed on demand at the point the reactor runs.

Prefer the event's own data. Analyzer **CHR0032** (warning) rejects injecting a
storage primitive such as `IMongoCollection<T>` directly — read keyed state
through a read-model parameter or `IReadModels.GetInstanceById` instead.

## Return types

| Return | Effect |
| --- | --- |
| `void`, `Task` | no side-effect append |
| `TEvent`, `Task<TEvent>` | append one event, with reactor-level metadata |
| `IEnumerable<object>`, `Task<IEnumerable<object>>` | append several, in one transaction |
| `EventForEventSourceId`, `Task<...>` | append one event to an explicit event source |
| `IEnumerable<EventForEventSourceId>`, `Task<...>` | append to several event sources, in one transaction |
| `EventsWithConcurrencyScopes`, `Task<...>` | append with explicit concurrency scopes |

A collection may **mix** bare events and `EventForEventSourceId` items; each is
appended with its own metadata, all in one transaction.

A custom side-effect type handled by your own `IReactorSideEffectHandler` is
only recognised when returned as `Task<T>`.

### Returning events instead of injecting `IEventLog`

Return the events. Do not inject `Cratis.Chronicle.EventSequences.IEventLog` into
a reactor. **This is a convention, not a framework contract** — no analyzer
enforces it — but returning the events keeps the reactor free of the event store
and keeps the append inside Chronicle's own transaction.

```csharp
// One event, on the triggering event's own event source.
public Task<<ResultEvent>> <MethodName>(<EventName> @event, EventContext context) =>
    Task.FromResult(new <ResultEvent>(@event.<Property>));

// Several events on that same event source.
public Task<IEnumerable<object>> <MethodName>(<EventName> @event, EventContext context) =>
    Task.FromResult<IEnumerable<object>>([new <ResultEvent>(), new <OtherEvent>()]);
```

### Targeting another event source

`Cratis.Chronicle.EventSequences.EventForEventSourceId` is the cross-stream
wrapper:

```csharp
public record EventForEventSourceId(EventSourceId EventSourceId, object Event, Causation? Causation = default)
{
    public Subject? Subject { get; init; }
    public EventStreamType EventStreamType { get; init; }
    public EventStreamId EventStreamId { get; init; }
    public EventSourceType EventSourceType { get; init; }
    public DateTimeOffset? Occurred { get; init; }
    public IEnumerable<string> Tags { get; init; }
}
```

```csharp
public Task<IEnumerable<EventForEventSourceId>> <MethodName>(<EventName> @event, EventContext context) =>
    Task.FromResult<IEnumerable<EventForEventSourceId>>(
    [
        new(@event.<RelatedId>, new <ResultEvent>(@event.<Property>)),
        new(@event.<OtherId>, new <OtherEvent>()) { EventStreamType = new("<stream-type>") },
    ]);
```

It does **not** carry an event sequence id; side effects are appended to the
event log.

### Where the metadata for a bare event comes from

For a **bare** event return, Chronicle resolves the append metadata from the
reactor itself: the `[EventSourceType]`, `[EventStreamType]`, and
`[EventStreamId]` attributes, and the `ICanProvideEventSourceId`,
`ICanProvideEventStreamId`, and `ICanProvideSubject` interfaces (all in
`Cratis.Chronicle.Events`, each with one method).

An `EventForEventSourceId` is **self-describing** — reactor-level metadata is not
applied to it. In a mixed collection, bare items get the reactor metadata and
wrapper items keep their own.

## Replay

A reactor sees the same event twice for different reasons: when it happens, and
again when its observer is replayed.

`Cratis.Chronicle.Reactors.OnceOnlyAttribute` (`[OnceOnly]`) targets a **class or
a method** and means **replay exclusion**:

- on the class, the observer is marked not replayable and the replay never
  starts;
- on a method, that handler is skipped while the delivery is a replay.

**`[OnceOnly]` is not per-event-source deduplication.** There is no ledger of
which events a handler has already seen. Recovering a failed partition
re-delivers the event as an ordinary observation, and a `[OnceOnly]` handler runs
again. Design the side effect to be idempotent regardless — an external
idempotency key, an upsert, a conditional write.

Analyzer **CHR0022** (warning) requires `[OnceOnly]` on a handler that returns
event side effects, because a replay would otherwise append duplicates.

`Cratis.Chronicle.Reactors.ReplayAttribute` (`[Replay]`) is the other half, and
targets a method only. Mark a **second** handler for the same event type with it
when replay needs to do something *different* rather than nothing:

```csharp
public Task <MethodName>(<EventName> @event) => <serviceName>.<Operation>(@event.<Property>);

[Replay]
public Task <MethodName>DuringReplay(<EventName> @event) => Task.CompletedTask;
```

With a `[Replay]` handler present, only it runs during replay. Without one, the
ordinary handler runs during replay as before. An event type handled *only* by a
`[Replay]` handler is still subscribed to.

## Identity and event sequence

`Cratis.Chronicle.Reactors.ReactorAttribute` takes two optional arguments:

```csharp
public sealed class ReactorAttribute(string id = "", string? eventSequence = default) : Attribute
```

**There is no `isActive` argument** — that belongs to `[Reducer]`. Without an
explicit `id`, the reactor's identity is its full type name.

The source sequence resolves in this order: an `[EventStore]` attribute (which
selects the inbox for that store), then `[EventSequence]`/`[EventLog]`, then
`Reactor(eventSequence: ...)`, then the event store inferred from the handled
event types, then the event log.

**`[EventStore]` cannot be combined** with `[EventSequence]`, `[EventLog]`, or
`Reactor(eventSequence: ...)` — analyzer **CHR0013** (error), and a runtime
exception. All of a reactor's event types must come from one event store —
analyzer **CHR0008** (error).

### Cross-service events

Cross-service facts route through the outbox and inbox rather than a shared log.
The producer appends its public contract event to the outbox sequence; the
consuming observer listens on the implicit inbox sequence for the source store.
Chronicle creates or reuses that subscription from the observer's `[EventStore]`
metadata, or from `[EventStore]` on the event type or its assembly. Put
`[assembly: EventStore("<source-store>")]` in a contracts project when every
event there originates from one service.

## Filtering which events reach the reactor

| Attribute | Effect |
| --- | --- |
| `[FilterEventsByTag("<tag>")]` | **filters** by appended tag; repeatable, and any one match admits the event |
| `[EventSourceType("<value>")]` | **filters** by the appended event source type |
| `[EventStreamType("<value>")]` | **filters** by the appended event stream type |
| `[EventStreamId("<value>")]` | **does not filter**; it is append metadata for side effects |
| `[Tag]`, `[Tags]` | **label** the reactor; they never filter |

Filter categories combine with AND; tags within the tag category combine with OR.
These correspond to metadata supplied at append time:

```csharp
await eventStore.EventLog.Append(
    <eventSourceId>,
    new <EventName>(<arguments>),
    eventStreamType: "<stream-type>",
    eventSourceType: "<source-type>",
    tags: ["<tag>"]);
```

A **projection cannot be filtered at all** — if you need metadata-based
selection, a reactor or a reducer is where it happens.

## Translation via a command

A translation that adapts one area's events into another's intent runs a command
rather than appending directly. `ICommandPipeline` is an **Arc** type
(`Cratis.Arc.Commands`), not a Chronicle one:

```csharp
using Cratis.Arc.Commands;

public class <ReactorName>(ICommandPipeline commandPipeline) : IReactor
{
    public async Task <MethodName>(<EventName> @event, EventContext context) =>
        await commandPipeline.Execute(new <CommandName>(@event.<Property>));
}
```

## Failure behavior

If a handler throws, or a returned side-effect event fails to append — a
constraint violation, a concurrency violation, or an error — the failing
event-source partition **pauses** until the cause is resolved. Repeated failures
can **quarantine** the observer, which stops retries and suppresses automatic
recovery. **A quarantined observer does not resume on reconnect**; an operator
must clear the quarantine explicitly.

Do not throw to reject a malformed inbound event. A reactor is not a data-quality
gate; invalid payloads belong at the command or append site. When a malformed
cross-service fact arrives, append a clear failure or dead-letter event, or
surface it through the operational failure path, and skip the partial side
effect. Throwing just to reject it pauses the partition and can quarantine the
whole observer.

## Keep the reactor stateless

Inject collaborators through the primary constructor; hold no mutable state on
the class. Analyzer **CHR0031** (warning) flags mutable reactor state. Keep each
reactor focused on one automation concern; several handlers in one class are fine
when they serve that concern.

## Specifications

Drive events through the reactor scenario helper with a service provider of
substitutes, and assert on those substitutes for non-event side effects. For
handlers that return events, assert the resulting appends through the scenario's
event store. Cover the replay path separately when the reactor has `[OnceOnly]`
or `[Replay]` handlers.

## Verify

- The class implements `IReactor` and is not generic.
- Every handler's first parameter is an `[EventType]` record, and there is one
  handler per event type.
- Every additional parameter resolves to the event context, `ReactorDelivery`, a
  read model, or a service.
- No handler returns or references `ReactorSideEffect`.
- No `IEventLog` is injected; side-effect events are returned instead.
- Handlers returning events carry `[OnceOnly]`.
- Side effects are idempotent even with `[OnceOnly]`, because recovery
  re-delivers.
- `[EventStore]` is not combined with an explicit event sequence.
- No filter attribute is placed where it is inert.
- The reactor holds no mutable state and injects no storage primitive.
- The build is clean with no `CHR0004`, `CHR0005`, `CHR0008`, `CHR0013`,
  `CHR0022`, `CHR0031`, or `CHR0032` outstanding, and the reactor
  specifications pass.
