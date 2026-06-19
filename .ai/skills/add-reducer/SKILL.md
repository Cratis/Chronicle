---
name: add-reducer
description: Use this skill when a read model genuinely needs an IReducerFor<T> — when model-bound projection attributes and fluent IProjectionFor<T> cannot express the state transition. Reducers are the last-resort escape hatch, not a default. Covers the admission checklist, method signatures, nullable-current handling, passivity, and pitfalls.
---

# Adding a Reducer (`IReducerFor<T>`)

A reducer is the **last-resort** way to build a read model — "current state + event → next state" with real C# control flow. Reach for it only when the projection ladder can't express the shape. A reducer that *could* have been a projection is a projection that was never written, and will be flagged for conversion in review.

## Admission checklist — all three must be true before writing a reducer

1. **Model-bound attributes can't express it** — not `[FromEvent<T>]`, `[SetFrom<T>]`, `[SetFromContext<T>]`, `[ChildrenFrom<T>]`, `[RemovedWith<T>]`, `[Nested]`, `[ClearWith<T>]`, counters (`[Increment<T>]`/`[Count<T>]`), or `[FromAll]`.
2. **Fluent `IProjectionFor<T>` can't express it** — not `UsingKey`/`UsingParentKey`/`UsingCompositeKey`/`UsingConstantKey`, `Join`, `.NotRewindable()`, or a supported conditional setter.
3. **The remainder needs real control flow** — branching on prior state, loops, or accumulated-state calculations the declarative APIs don't have.

If all three hold, write the reducer and **carry an inline justification comment** on the class naming the specific limitation that ruled out a projection:

```csharp
// Reducer required: next balance depends on prior balance with a per-event cap that
// no counter/projection attribute expresses.
public class AccountBalanceReducer : IReducerFor<AccountBalance>
{
    public AccountBalance Opened(DebitAccountOpened @event, AccountBalance? current, EventContext context)
        => new(0m, context.Occurred);

    public AccountBalance Deposited(FundsDeposited @event, AccountBalance? current, EventContext context)
        => (current ?? new(0m, context.Occurred)) with { Balance = (current?.Balance ?? 0m) + @event.Amount };
}
```

## Method signatures

Each public method's **first parameter** is the event type it handles (dispatch is by type, not name):

| Signature | Meaning |
|---|---|
| `T On(TEvent @event, T? current)` | next state from current (null on first event) |
| `T On(TEvent @event, T current)` | non-nullable current when the model is always seeded first |
| `+ EventContext context` | add the context as a trailing parameter when you need `Occurred`/metadata |
| `Task<T> On(...)` | async when the computation awaits |
| return `T?`/`null` | treat the event as a no-op for this model |

- Return the **complete new state** — never mutate `current` in place (use `with { }`).
- `current` is `null` on the first event for a given event source; for a standing model seeded by one event then updated by many, return `null` when the seed state is absent so replay and `ReadModelScenario<T>` ordering survive.
- **Never store mutable state on the reducer class** — it must be stateless.
- **Don't throw for a no-op event** — return `current` unchanged; throwing pauses the reducer's partition.

## Passivity, filtering, and identity

- `[Passive]` on the read model makes any reducer for it passive automatically (command-side reads only, not materialized); `[Reducer(isActive: false)]` is the per-reducer form. `[Reducer(id: ..., eventSequence: ...)]` overrides identity/source.
- Filter which events the reducer observes with `[FilterEventsByTag]`, `[EventSourceType]`, `[EventStreamType]` (these filter; `[Tag]`/`[Tags]` only label).

## Specs

Use `ReadModelScenario<TReadModel>` (it auto-detects reducer vs projection). Drive a sequence of events through `Given.ForEventSource(id).Events(...)` and assert on `_scenario.Instance`. Cover: first-event creation, accumulation across multiple events, and any branch in the reducer. See [specs.scenarios.csharp.md](../../rules/specs.scenarios.csharp.md) and the `write-specs-readmodels` skill.

## See also

- [vertical-slices.md](../../rules/vertical-slices.md) — the projection→fluent→reducer ladder.
- `add-projection` — the model-bound and fluent options to exhaust first.
- `cratis-readmodel` — read model + query method basics.
