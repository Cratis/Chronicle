---
name: cratis-chronicle-reducer
description: Write a Chronicle IReducerFor reducer when model-bound projection attributes and the fluent IProjectionFor builder cannot express a read-model state transition. Covers the admission test, the exact accepted method signatures, the nullable-current requirement, deletion, passivity and event filtering. Use only after the projection options are genuinely exhausted; do not use for ordinary event-to-property mapping.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-chronicle-reducer/SKILL.md -->

# Chronicle reducers

A reducer builds a read model as "current state plus event produces next state",
with real C# control flow. Start with model-bound attributes, use fluent
`IProjectionFor<T>` when mappings need structure, and choose a reducer when
branching on prior state, calculations or guards read better as code. A reducer
is a normal choice, not a last resort. For the application-code ladder and PDL's
outside-code role, read [choosing a style](../cratis-chronicle-read-model/SKILL.md#step-3--choose-a-projection-or-a-reducer).

Requires Chronicle 19.58.0 for self-reducing read models.

For new .NET reducers, default to **self-reducing read models**: public static
`Reduce` methods beside the state, discovered by shape without a marker. Use a
separate `IReducerFor<T>` for fold-time DI, replay-time instance state or useful
separation; nominate a plain class only when its assembly cannot reference
Chronicle. Self reduction is .NET-only; other SDKs retain
their own contracts. This guidance does not claim Stage renders the self form.

## Verified product sources

Sources:

Read [references/product-sources.md](references/product-sources.md) when you need the source files behind discovery and ownership.

Reverify product sources before claiming support for another version.

## Choose the form

- Direct property mapping, counters and sums usually read best as projections.
  Fluent definitions add mapping structure, not arbitrary per-event code.
- Prior-state branches and guarded calculations often read best as reducers.
  Explain the state-transition rule, rather than proving both projection APIs
  incapable. Reducers fold per event source: no native cross-source joins,
  custom aggregation keys or variant-group wiring.
- Keep the state and transition together by default. Choose `IReducerFor<T>`
  when constructor dependencies, replay instance behavior or a separate data
  contract make the extra type useful. Prefer event-recorded rates/times over
  consulting today's service during replay. Never hide durable state on a
  reducer service instance.
- When the assembly cannot reference Chronicle, nominate a plain instance class
  through `ChronicleOptions.ExplicitArtifacts.RegisterReducer<PlainReducer>()`
  before creating stores. Supported instance handlers must infer exactly one
  model. See [discovery and nomination](../cratis-chronicle-client-dotnet/references/discovery.md).

## Default — a self-reducing read model

This example's debit guard derives a view; it does not reject the already
appended debit. Missing state stays absent until a credit creates it.

```csharp
using Cratis.Chronicle.Events;
using Cratis.Concepts;

public record Money(decimal Value) : ConceptAs<decimal>(Value);

/// <summary>Funds were credited to the account.</summary>
[EventType]
public record FundsCredited(Money Amount);

/// <summary>A debit was recorded for the account.</summary>
[EventType]
public record FundsDebited(Money Amount);

/// <summary>The balance view was cleared.</summary>
[EventType]
public record BalanceCleared;

public record AccountBalance(Money Total)
{
    public static AccountBalance Reduce(FundsCredited @event, AccountBalance? current) =>
        new(new Money((current?.Total.Value ?? 0m) + @event.Amount.Value));

    public static AccountBalance? Reduce(FundsDebited @event, AccountBalance? current) =>
        current is null || current.Total.Value < @event.Amount.Value
            ? current
            : current with { Total = new Money(current.Total.Value - @event.Amount.Value) };

    public static AccountBalance? Reduce(BalanceCleared @event, AccountBalance? current) => null;
}
```

No model activation, parameterless constructor or reducer marker is needed.
`[Reducer]` is optional configuration, not a discovery requirement.

### Arc on the same record

In Arc applications keep `[ReadModel]`, `Reduce` methods and static queries on
one record. On an Arc older than 22.90.0, use
`[ReadModel(RequireQueryAttribute = true)]` with `[Query]` on intended endpoints;
see [query discovery](../cratis-chronicle-read-model/references/queries.md#discovery-rules).
Requires Arc 22.90.0 for the automatic behavior: Arc excludes methods taking the model itself from query
discovery, and Arc.Chronicle claims self-reducing models for command injection.
Check the consuming Arc version; Chronicle's discovery does not fix older Arc.

## Separate instance reducer

When the fold needs the separation described above, use the classic form:

Read [references/instance-reducer-example.md](references/instance-reducer-example.md) when you need a worked `IReducerFor<T>` instance reducer.

`IReducerFor<TReadModel>` is a marker with no members, constrained to
`where TReadModel : class`.

## Preserve admission and ordering when converting

A handler that ends with `current with { Status = ... }` is not necessarily a
plain setter. First inspect guards: does it require a creation event, ignore an
older attempt, preserve a terminal state, or compare prior values? Replacing it
with unconditional `[SetValue<T>]` can create state too early or resurrect a
finished entity. Keep the reducer when the projection APIs cannot express those
semantics; the style choice is about behavior, not handler length.

Evaluate variants for mutually exclusive lifecycle shapes (see the projection
skill). Their non-entering events are update-only, but that does not replace
arbitrary guards on attempt identity, ordering, or prior state. Before conversion,
keep specifications for update-before-create, late events after completion,
repeated events, and each guarded branch, as applicable. Assert the same outcomes
with the replacement, not only the happy-path final status.

## Method signatures — split by form

### Self-reducing model

The containing type must be concrete, non-generic and not implement `IReducer`.
Each handler is public static, non-generic and named **`Reduce`**.

| Signature | Meaning |
| --- | --- |
| `TSelf Reduce(TEvent @event, TSelf? current)` | complete next state |
| `TSelf Reduce(TEvent @event, TSelf? current, EventContext context)` | next state with metadata |
| `TSelf?` / `Task<TSelf>` / `Task<TSelf?>` with those parameters | nullable and/or awaited next state |
| bare `Task`, `ValueTask`, `Task<object>` | invalid self result, not deletion |

The event dispatch target is a non-`object` class or interface; current is exactly
the model type, nullable or nullable-oblivious. No by-ref, optional, `params`,
reordered or extra service arguments: **no DI in `Reduce`**. Return `null` to
leave absent state or delete; return `current` to ignore. Awaited results become
current state before the next event.

Dispatch selects from events registered in the owning client: exact type,
then nearest base class, then uniquely most-derived interface. Equal-specificity
ties (including overloads with/without context for one event) throw
`AmbiguousSelfReducerMethod`. Names and declaration order do not break ties.

### Separate or nominated instance reducer

Dispatch is by the **first parameter's type**, never by the method name. Name the
instance method for the reader.

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
- Keep durable state in the model. Instance replay bookkeeping is not durable
  model state and must not make the fold depend on a previous activation.
- An instance reducer with no dispatchable methods is not registered. Logs name skipped
  reducer-like methods; CHR0006/CHR0007 flag them. CHR0051 checks nullable current
  state, and CHR0057 rejects multiple `IReducerFor<T>` targets on one class.

### Returning null deletes the instance

In either form, `null` sets the state to absent. In the **instance form only**,
a bare `Task` also sets the read-model state
to null, which removes the instance. That is the deletion mechanism, so do not
reach for `null` to mean "ignore this event". To ignore an event, return
`current` unchanged.

**Do not throw to skip an event.** A throw fails the event-source partition and
stops the reducer for that partition until an operator intervenes.

`current` is `null` on the first event for a given event source. For a model
seeded by one event and then updated by many, decide deliberately what an update
arriving before its seed should do. Returning `current` preserves absence;
synthesizing a partial instance is valid only when it is the deliberate view
policy. Specify that policy rather than hiding a missing-creation bug.

## Identity, passivity, and event sequence

`Cratis.Chronicle.Reducers.ReducerAttribute` takes three arguments, all
optional:

```csharp
public sealed class ReducerAttribute(string id = "", string? eventSequence = default, bool isActive = true)
```

- `id` overrides the generated reducer identity.
- `eventSequence` selects the source sequence.
- `isActive: false` disables active maintenance; it does **not** make reads passive.

For a self reducer, place configuration on the model; for a separate reducer,
place `[Reducer]` on its class. Keep `[Passive]` on the model in both forms.

`Cratis.Chronicle.ReadModels.PassiveAttribute` (`[Passive]`) goes on the **read
model type** and makes it passive as a whole: no sink, nothing materialized, and
the instance is computed on demand when it is read. Reach for it when a
command-side read needs the model current at the moment it is read. Passive is
fresher, not safer: a passive reducer read captures no position and guards nothing,
and a plain read of a reducer guards nothing. An unfiltered reducer without
constructor dependencies (other than `ILogger<T>`; self-reducing models are exempt)
is admitted by decision reads and by Arc read protection `On` (Chronicle 19.68.0);
a filtered reducer or one with dependencies is refused, and no reducer is admitted in
a stream-scoped read. For a rule that must hold under concurrent writers read an
admitted model under `On`; see
[choosing how a rule holds](../cratis-chronicle-decision-reads/references/choosing-how-a-rule-holds.md).

## Discovery, upgrades and ownership

Requires Chronicle 19.58.0: complete marker-free static shapes become reducers on upgrade,
including existing helpers. Review them before upgrading. Near-miss static
`Reduce` declarations whose current/result refers to the model warn (CHR0058)
and are skipped by implicit discovery; explicit nomination throws
`InvalidSelfReducerSignature`. Once one valid shape exists, invalid sibling
`Reduce` declarations and malformed hooks fail validation. No registered matching
event means no observer subscription; assembly membership alone is not event
membership. Types implementing `IReducer` stay classic even with a self-shaped
static helper.

Requires Chronicle 19.72.0 for CHR0065 to warn on fold-like methods that are
misnamed (`Reducer`/`reduce`) or not static and therefore never discovered.
Check `dotnet list package`; older analyzers can leave those methods silent.

Use the existing generic or Type `RegisterReducer` API for explicit self-model
nomination too; there is no special self API. A self model cannot share ownership
with a separate reducer, fluent projection or genuine model-bound mappings in the
same store catalog: `AmbiguousReadModelBuilder` rejects it. `[Passive]`, sequence
selection, filters and `[Reducer]` alone do not establish projection ownership.
Remove the old builder when switching; registration is not a data migration.

## Replay

Read [references/replay-hooks-and-definition-changes.md](references/replay-hooks-and-definition-changes.md) when a self model needs `BeginReplay`/`EndReplay` hooks (they return exactly `Task`; do not assume delivery is exactly once) or when a code, key or filter change must replay (`observers.definitionEvolution`). Requires Chronicle 19.29.4 for method-fingerprint change detection; assembly versions are ignored.

## Filtering

Read [references/filtering.md](references/filtering.md) when restricting which events reach a reducer (tag, event source type, stream type or declared source). `[Tag]` and `[Tags]` label and do not filter; a projection cannot be filtered at all.

## Specifications

Start with `Specification` plus a direct call for pure reducer decisions:
pass the event and current state explicitly and assert the returned next state.
Add `ReadModelScenario<TReadModel>` only where event dispatch, filtering,
metadata or read-model wiring contribute proof; drive the event sequence and
assert on the resulting instance there. For the self example:

Read [references/specification-example.md](references/specification-example.md) when you need a worked direct-call and `ReadModelScenario` spec.

Import `Cratis.Chronicle.Testing.ReadModels` for the scenario and the project's
specification assertions. Test deletion alone as absence, then recreation;
cover empty history and every event without current state. Scenario discovery
includes self models and nominated plain classes; see
[read-model specifications](../cratis-chronicle-read-model-specifications/SKILL.md).
In-process scenarios substitute storage, lifecycle and compliance: they do not
prove transport, replay, encryption, erasure or sink migration. Cover, at minimum:

- the first event, where `current` is `null`;
- accumulation across several events on one event source;
- every branch the reducer actually has;
- deletion, if the reducer ever returns `null`.

## Route near misses

- An accepted `.play` model under the model root covers the behavior, or the
  repository is opted in (per `general.md` "Model first"; where `cratis-screenplay-modeling-lifecycle` is installed, run its `scripts/model-first-status.mjs`): change the model first with
  `cratis-screenplay-event-modeling`. If the Screenplay skills are not installed,
  say so and do not author `.play` from memory.
  Edit code here only for infrastructure, clients, adapters, Screenplay code
  attachments, or gap-fill scope (`cratis-screenplay-render-and-gap-fill`);
  never edit Stage-managed output.

## Verify

- The style and placement fit the transition; no projection feature is rebuilt
  unnecessarily and admission/ordering guards are preserved.
- Every handler accepts nullable current state (CHR0051 for instance methods,
  CHR0058 for self near misses).
- Logs contain no skipped reducer-like method warning (CHR0006/CHR0007);
  CHR0055–CHR0061 are clean, including hooks, ties and ownership.
- No handler method has more than three parameters, and any third is
  `EventContext`.
- Exactly one handler exists per event type.
- Durable state lives in the model, not the reducer instance; never mutate `current`.
- `null` represents deliberately absent or deleted state, never a skip of existing state.
- No handler throws to skip an event.
- The project builds clean and the read-model specifications pass against the
  verified package version.
