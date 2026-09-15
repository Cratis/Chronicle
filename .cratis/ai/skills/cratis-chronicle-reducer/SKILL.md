---
name: cratis-chronicle-reducer
description: Write a Chronicle IReducerFor<T> when model-bound projection attributes and the fluent IProjectionFor<T> builder cannot express a read-model state transition. Covers the admission test, the exact accepted method signatures, the nullable-current requirement, deletion, passivity and event filtering. Use only after the projection options are genuinely exhausted; do not use for ordinary event-to-property mapping.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-chronicle-reducer/SKILL.md -->

# Chronicle reducers

A reducer builds a read model as "current state plus event produces next state",
with real C# control flow. It is the **last resort** on the projection ladder,
not a default. A reducer that could have been a projection is a projection that
was never written, and it will be flagged for conversion in review.

## Verified product sources

This skill is verified against this exact source:

| Package | Version | Purpose |
| --- | --- | --- |
| `Cratis.Chronicle` | `16.45.2` | `Cratis.Chronicle.Reducers`, `Cratis.Chronicle.ReadModels`, `Cratis.Chronicle.Events` |

Reverify product sources before claiming support for another version.

## Admission test — all three must hold

1. **Model-bound attributes cannot express it.** Not `[FromEvent<T>]`,
   `[SetFrom<T>]`, `[SetValue<T>]`, `[SetFromContext<T>]`, `[AddFrom<T>]`,
   `[SubtractFrom<T>]`, `[ChildrenFrom<T>]`, `[Join<T>]`, `[RemovedWith<T>]`,
   `[RemovedWithJoin<T>]`, `[Nested]`, `[ClearWith<T>]`, `[Increment<T>]`,
   `[Decrement<T>]`, `[Count<T>]`, `[FromAll]`, or `[FromEvery]`.
2. **The fluent `IProjectionFor<T>` builder cannot express it.** Not
   `UsingKey`, `UsingKeyFromContext`, `UsingParentKey`, `UsingCompositeKey`,
   `UsingConstantKey`, `Join`, `Children`, `Nested`, `WithInitialValues`,
   `NotRewindable`, or a `Set(...).To(...)` form.
3. **The remainder needs real control flow** — branching on prior state, loops,
   or an accumulation the declarative APIs have no operator for.

If all three hold, write the reducer and carry an inline justification naming the
specific limitation that ruled out a projection.

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

// Reducer required: the next value depends on the prior value through a
// per-event rule that no counter or projection operator expresses.
public class <ReadModelName>Reducer : IReducerFor<<ReadModelName>>
{
    public <ReadModelName> <Opened>(<OpenedEvent> @event, <ReadModelName>? current, EventContext context) =>
        new(<initialValue>, context.Occurred);

    public <ReadModelName> <Changed>(<ChangedEvent> @event, <ReadModelName>? current) =>
        current is null
            ? new(<initialValue>, default)
            : current with { <Property> = <computedFrom(current, @event)> };
}
```

`IReducerFor<TReadModel>` is a marker with no members, constrained to
`where TReadModel : class`.

## Method signatures — exactly what is accepted

Dispatch is by the **first parameter's type**, never by the method name. Name the
method for the reader.

| Signature | Meaning |
| --- | --- |
| `TReadModel M(TEvent @event, TReadModel? current)` | next state, computed synchronously |
| `TReadModel M(TEvent @event, TReadModel? current, EventContext context)` | same, with event metadata |
| `Task<TReadModel> M(...)` | the same two shapes, asynchronously |
| returns `TReadModel?` / `Task<TReadModel?>` / bare `Task` | see deletion below |

The rules the invoker enforces:

- **The current-state parameter must be declared nullable.** A non-nullable
  `TReadModel current` **throws** when the reducer's methods are indexed. This
  is not a silent skip; it fails.
- At most three parameters. If a third is present it must be exactly
  `Cratis.Chronicle.Events.EventContext`.
- Public and non-public instance methods are both scanned, and **one method wins
  per event type** — do not write two handlers for the same event.
- Return the complete next state. Never mutate `current` in place; use `with { }`.
- The reducer must be **stateless**. Do not hold mutable state on the class.

### Returning null deletes the instance

A `null` return — and equally a bare `Task` return — sets the read-model state
to null, which removes the instance. That is the deletion mechanism, so do not
reach for `null` to mean "ignore this event". To ignore an event, return
`current` unchanged.

**Do not throw to skip an event.** A throw fails the event-source partition and
stops the reducer for that partition until an operator intervenes.

`current` is `null` on the first event for a given event source. For a model
seeded by one event and then updated by many, decide deliberately what an update
arriving before its seed should do — returning `null` keeps replay and
specification ordering honest, returning a synthesized instance hides an ordering
bug.

## Identity, passivity, and event sequence

`Cratis.Chronicle.Reducers.ReducerAttribute` takes three arguments, all
optional:

```csharp
public sealed class ReducerAttribute(string id = "", string? eventSequence = default, bool isActive = true)
```

- `id` overrides the generated reducer identity.
- `eventSequence` selects the source sequence.
- `isActive: false` makes this one reducer passive.

`Cratis.Chronicle.ReadModels.PassiveAttribute` (`[Passive]`) goes on the **read
model type** and makes it passive as a whole: no sink, nothing materialized, and
the instance is computed on demand when it is read. Reach for it when a
command-side decision needs the model strongly consistent at the moment it is
read.

## Filtering which events reach the reducer

Three attributes genuinely filter a reducer's input, and they correspond to
metadata supplied at append time:

| Attribute | Filters on |
| --- | --- |
| `[FilterEventsByTag("<tag>")]` | an appended tag; repeatable, and any one match admits the event |
| `[EventSourceType("<value>")]` | the appended event source type |
| `[EventStreamType("<value>")]` | the appended event stream type |

`[Tag]` and `[Tags]` **label** the artifact; they do not filter. Note also that a
**projection cannot be filtered at all** — its definition has no field for it.
When you need metadata-based selection, a reducer or a reactor is where it can
happen.

The matching append supplies that metadata:

```csharp
await eventStore.EventLog.Append(
    <eventSourceId>,
    new <EventName>(<arguments>),
    eventStreamType: "<stream-type>",
    eventSourceType: "<source-type>",
    tags: ["<tag>"]);
```

Mind the parameter order on `Append`: `eventStreamType` comes before
`eventStreamId`, which comes before `eventSourceType`.

## Specifications

Drive a sequence of events through the read-model scenario helper for the read
model and assert on the resulting instance. Cover, at minimum:

- the first event, where `current` is `null`;
- accumulation across several events on one event source;
- every branch the reducer actually has;
- deletion, if the reducer ever returns `null`.

## Verify

- The admission test is satisfied and the justification names the specific
  limitation.
- The current-state parameter is nullable on every handler method.
- No handler method has more than three parameters, and any third is
  `EventContext`.
- Exactly one handler exists per event type.
- The reducer holds no mutable state and never mutates `current`.
- `null` is returned only where deletion is intended.
- No handler throws to skip an event.
- The project builds clean and the read-model specifications pass against the
  verified package version.
