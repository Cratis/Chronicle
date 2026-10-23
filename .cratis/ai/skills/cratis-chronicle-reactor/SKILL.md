---
name: cratis-chronicle-reactor
description: Implement a Chronicle IReactor - an automation that causes an external side effect or appends a follow-up event or command, or the adapter side of a translation of outside data into our own facts. Covers handler dispatch and parameter resolution, every supported return type, targeting another event source, replay handling with [OnceOnly] and [Replay], event filtering, failure and quarantine behavior, and the analyzer rules. Use for event-driven side effects; do not use to populate a read model.
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
| append a follow-up event or run a follow-up command in response to our own event | a reactor (**automation**: Screenplay `produces` / `invokes`) |
| record data from an outside system or service as our own facts | an adapter at the boundary, often a reactor (**translation**) |
| both a model and an effect | one projection *and* one reactor |

> **Model first.** If an accepted model under the model root covers this scope, or
> the repository is opted in (per `general.md` "Model first"; where `cratis-screenplay-modeling-lifecycle` is installed, run its `scripts/model-first-status.mjs`), the automation is
> a modeled slice: find it (`Automation` or `Translate`), treat its `produces` /
> `invokes`, conditions and fields as the contract, and write the reactor only as
> gap-fill for what the model leaves to code (Stage renders supported event-triggered Automation;
> Translate/captures and refused reactions remain gap-fill; Stage skill `references/admission.md`). Use `cratis-screenplay-automations-and-translations` to
> change the model; never change the model to fit the reactor, and never leave a
> modeled rule living only in the reactor. If the repository is not opted in
> (an empty directory, install output, a `.play` file outside the root or an untracked or uncommitted draft is not consent), stay code-first here
> (`cratis-chronicle-event-modeling` for a new, unsettled flow); if the Screenplay skills are not installed, say so and do not author `.play` from memory.

## Product sources

| Package | Purpose |
| --- | --- |
| `Cratis.Chronicle` | `Cratis.Chronicle.Reactors`, `Cratis.Chronicle.Reactors.SideEffects`, `Cratis.Chronicle.EventSequences`, `Cratis.Chronicle.Events` |
| `Cratis.Chronicle.CodeAnalysis` | handler, side-effect and filter diagnostics, including CHR0054–CHR0056 |

Scenario guards: see [specifications](references/specifications.md).

> **There is no `ReactorSideEffect` return-value wrapper.** Use the supported
> returns below; the `[ReactorSideEffect]` marker does not wrap an effect.

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
Requires Chronicle 19.52.0 for plain-class nomination; read
[discovery](../cratis-chronicle-client-dotnet/references/discovery.md) when a
contract assembly cannot reference Chronicle.

## Dispatch and parameters

**Dispatch is by the first parameter's type.** The method name is for the reader
and plays no part. When two methods claim the same event type, the winner is
chosen by public before non-public, then more parameters before fewer, then
ordinal name order — so do not rely on it; write one handler per event type.

A handler takes at least one parameter. Further parameters resolve by type:

| Parameter type | Resolved as |
| --- | --- |
| `EventContext` | the event context |
| `ReactorDelivery` | reactor id, event store, namespace, event sequence, partition, sequence number |
| a type that has a projection or a reducer | that **read model**, materialized for this event |
| anything else | from the service provider |

An unresolvable parameter throws. CHR0004 warns against primitive, value-type
or string dependencies after the event: use context, a read model or a service.

The first parameter must be a registered event type (CHR0005 error); explicit
registration evidence needs Chronicle 19.52.0 (see discovery above).

A materialized read-model parameter is only as current as its observer; prefer the event's own data, and use `[Passive]` when the reactor needs a current read. Read [references/read-model-injection.md](references/read-model-injection.md) whenever a reactor injects a read model: how it is keyed (including a related entity's key through `ICanResolveReadModelKey`), when it is current, and which storage access CHR0032 flags.

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

Arrays and lists of registered events or `EventForEventSourceId` are also valid
synchronous returns. Custom `IReactorSideEffectHandler` implementations can
claim synchronous types through `CanHandleReturnType(Type)`; they are not
restricted to `Task<T>`. An unclaimed return value **fails the partition** with
`UnhandledReactorReturnValue`, naming its type. CHR0054 flags an unsupported
return type at build time. Read references/return-event-metadata.md for custom returns (`[ReactorSideEffect]` requires Chronicle 19.63.0).
Source: `Reactors/EventHandlerMethods.cs`,
`Reactors/SideEffects/IReactorSideEffectHandler.cs`, `Reactors/UnhandledReactorReturnValue.cs`.

Read [references/return-event-metadata.md](references/return-event-metadata.md) when returning events instead of injecting `IEventLog`, targeting another event source with `EventForEventSourceId`, or controlling the append metadata of a returned event.

## Replay

A reactor sees the same event twice for different reasons: when it happens, and
again when its observer is replayed.

`Cratis.Chronicle.Reactors.OnceOnlyAttribute` (`[OnceOnly]`) targets a **class or
a method** and means **replay exclusion**:

- on the class, the observer is marked not replayable and the replay never
  starts;
- on a method, that handler is skipped while the delivery is a replay.

**`[OnceOnly]` is not per-event-source deduplication.** There is no record of
which events a handler has already seen. Recovering a failed partition
re-delivers the event as an ordinary observation, and a `[OnceOnly]` handler runs
again. Catch-up and crash redelivery are not replay either; every ordinary
matching event can run, even on the same source. Keep effects idempotent.

**A new reactor catches up history; `[OnceOnly]` does not stop that.**
Requires Chronicle 19.70.0 for `[StartAtTail]` to start only its first registration
at the tail; existing positions remain. Upgrade the server first: older kernels
ignore it. Read references/starting-and-replaying.md when avoiding pre-registration
side effects or choosing alternate replay behavior.

Analyzer **CHR0022** (warning) requires `[OnceOnly]` on a handler that returns
event side effects, because a replay would otherwise append duplicates.

## Identity and event sequence

`Cratis.Chronicle.Reactors.ReactorAttribute` takes two optional arguments:

```csharp
public sealed class ReactorAttribute(string id = "", string? eventSequence = default) : Attribute
```

**There is no `isActive` argument** — that belongs to `[Reducer]`. Without an
explicit `id`, the reactor's identity is its full type name.

Give side-effecting reactors a **stable explicit id**. Discovery
warns on likely renames/moves: a new id catches up from the start and repeats
side effects; `[OnceOnly]` does not prevent that. `ArtifactIdentityDrift = Fail`
rejects Warning-level reactor/event-type findings with
`ArtifactIdentityDriftDetected`; projection/reducer drift is Information.
Inspect `RegistrationOutcome.IdentityDrift`.

Requires Chronicle 19.47.0 or later for identity-drift warnings and `ArtifactIdentityDrift`. After stopping all subscriptions,
`IEventStore.Observers.Remove` can remove an orphaned observer; it refuses while
running or subscribed anywhere. This is an authorized operator action, not a
rename cleanup to perform automatically.

The source sequence resolves in this order: an `[EventStore]` attribute (which
selects the inbox for that store), then `[EventSequence]`/`[EventLog]`, then
`Reactor(eventSequence: ...)`, then the event store inferred from the handled
event types, then the event log.

**`[EventStore]` cannot be combined** with `[EventSequence]`, `[EventLog]`, or
`Reactor(eventSequence: ...)` — analyzer **CHR0013** (error), and a runtime
exception. All of a reactor's event types must come from one event store —
analyzer **CHR0008** (error).

Read [references/cross-service-events.md](references/cross-service-events.md) when consuming events from another service through the inbox and outbox.

## Filtering

Read [references/filtering.md](references/filtering.md) when restricting which events reach a reactor by tag, event source type, stream type or declared source; `[EventStreamId]` does not filter, and a projection cannot be filtered at all.

## Invoking a command from a reactor

An automation that turns another area's facts into intent runs a command
(Screenplay `invokes`). For the Arc integration's return shapes, execution
identity and imperative pipeline alternative, use **cratis-arc-command-execution**
and **cratis-arc-command** rather than reimplementing command dispatch here.
A reactor has no caller principal: choose the authorized identity explicitly.
Keep `[OnceOnly]` and idempotency. Expected refusals use `Invoke.Command` branches
in **cratis-arc-command-execution**; handling stops remaining invocations.
Unhandled refusals and framework failures remain partition failures. Inline operations can run again on reactor retry and do not
replace durable delivery; see **cratis-arc-command-operation**.
Mixed event-and-command returns and their ordering are owned by the Arc skills.

## Failure behavior

If a handler throws, or a returned side-effect event fails to append — a
constraint violation, a concurrency violation, or an error — the failing
event-source partition **pauses** until the cause is resolved. Repeated failures
can **quarantine** the observer, which stops retries and suppresses automatic
recovery. **A quarantine ends when an operator clears it
(`ClearObserverQuarantine()`), or when the observer is subscribed again** — for an
application observer that is when the client connects again, for example after a
redeploy; Kernel-owned reactors subscribe again at startup; automatic event-store
subscription reconciliation does not end quarantine. Read
[recovery](../cratis-chronicle-hosting/references/operations.md) for the
fail-closed lookup and observer-kind-specific quarantine rules.

Do not throw to reject a malformed inbound event. A reactor is not a data-quality
gate; invalid payloads belong at the command or append site. When a malformed
cross-service fact arrives, append a clear failure or dead-letter event, or
surface it through the operational failure path, and skip the partial side
effect. Throwing just to reject it pauses the partition and can quarantine the
whole observer.

`RetryFailedPartitionFor<TReactor>(partition)` returns a bounded
`ReactorPartitionRetryOutcome`. `ClearFailedPartitionQuarantineFor<TReactor>(partition,
retryImmediately)` clears **one partition**, preserves attempt history and starts
a fresh retry budget (`maxRetryAttempts`). Observer clear is not partition clear.
Quarantine survives deactivation; watchdog ticks and replay/catch-up completion
do not end it. Only an operator clear or fresh subscription does. Kernel alert
incidents surface repeated failures; see **cratis-chronicle-hosting**.

A returned append into a closed scope fails with `StreamClosed`, so the reactor
must not blindly retry it forever. Model an authorized alternate fact/route or
operator recovery; see **cratis-chronicle-event-constraints**.

### Domain failures must leave a visible state

A terminal failure needs an owner that moves the workflow out of "running".
Do not add a failure reactor when a state machine already owns that transition.

Read [references/domain-failures.md](references/domain-failures.md) when handling a domain failure event or deciding whether a failure needs its own reactor or watchdog.

## Keep the reactor stateless

Inject through the primary constructor; hold no mutable state (CHR0031 warning).
Keep one automation concern per reactor; several related handlers are fine.

## Specifications

Start with `Specification` plus a direct call for pure reactor decisions; add `ReactorScenario<TReactor>` only where invocation, dependency wiring or side effects contribute proof. Specify the contract, not only the happy path: field lineage, contract-authorized filtering, repeated delivery, and unrelated events.

Read [specifications](references/specifications.md) before writing scenario
specs: explicit read-model seeding/absence, stream-filtered seeds, produced-event
destinations, per-run discovery and disposal. `Given` never builds read models,
including passive ones; nullable parameters still need explicit absence.

## Verify

- The class implements `IReactor` and is not generic.
- Every handler's first parameter is an `[EventType]` record, and there is one
  handler per event type.
- Every additional parameter resolves to the event context, `ReactorDelivery`, a
  read model, or a service.
- No handler returns an invented `ReactorSideEffect` wrapper.
- No `IEventLog` is injected; side-effect events are returned instead.
- Handlers returning events carry `[OnceOnly]`.
- Side effects are idempotent even with `[OnceOnly]`, because recovery
  re-delivers, and a specification covers repeated delivery.
- Every field of a returned command or event comes from the trigger event, an
  injected read model, or a mapping the contract states; none is invented or
  defaulted.
- Every condition that skips an event is stated in the contract; none is
  invented.
- A modeled automation (an opted-in scope) matches its `.play` slice (events, `produces` /
  `invokes`, conditions); the model was not edited to match the reactor.
- `[EventStore]` is not combined with an explicit event sequence.
- No filter attribute is placed where it is inert.
- The reactor holds no mutable state and injects no storage primitive.
- The build is clean with no `CHR0004`, `CHR0005`, `CHR0008`, `CHR0013`,
  `CHR0022`, `CHR0031`, `CHR0032`, `CHR0054`, `CHR0055`, or `CHR0056` outstanding, and the reactor
  specifications pass.

## Lineage

Attribution and verification sources: `references/provenance.md`.
Read `references/slice-delivery-checklist.md` when delivering a reactor from a slice contract: build order, handler patterns, closing checklist.
