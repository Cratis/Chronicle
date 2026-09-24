---
name: cratis-chronicle-projection
description: Add Chronicle projection behavior to an existing read model - model-bound attributes first, the fluent IProjectionFor<T> builder when they cannot express the shape. Covers AutoMap, keys, children and nested types, counters, event-sequence selection, variants ([VariantOf<T>]/.VariantOf<T>() and [EntersOn<T>]/.EntersOn<T>()) for an entity with mutually exclusive lifecycle shapes such as a work item that is a backlog entry then a pull request then closed, and the startup-crash traps. Use when populating a read model from events; do not use to create the read model and its query surface, and do not reach for a reducer before exhausting these options.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-chronicle-projection/SKILL.md -->

# Chronicle projections

A projection consumes **events** and produces read-model state. It is pure: no
side effects, and it never reads another read model. Prefer model-bound
attributes on the read model; fall back to the fluent builder only when the
attributes cannot express the shape; reach for a reducer only after both fail.

## Verified product sources

This skill is verified against this exact source:

| Package | Version | Purpose |
| --- | --- | --- |
| `Cratis.Chronicle` | `18.3.0` | `Cratis.Chronicle.Projections`, `Cratis.Chronicle.Projections.ModelBound`, `Cratis.Chronicle.Keys`, `Cratis.Chronicle.EventSequences` |

Reverify product sources before claiming support for another version.

> `[ReadModel]` and `[Path]` are **Arc** types (`Cratis.Arc.Queries.ModelBound`),
> not Chronicle types. A Chronicle read model that exists only as a projection
> target needs no `[ReadModel]` at all — that attribute is what gives it an Arc
> query surface.

## Model-bound projections — the default

Put the projection on the read model with attributes. No separate class.

```csharp
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[FromEvent<<EventName>>]
public record <ReadModelName>(
    [property: Key] <IdType> Id,
    <PropertyType> <PropertyName>);
```

Matching property names map automatically. **AutoMap is on by default** — both
for model-bound types and for the fluent builder — so never call `.AutoMap()`
except inside a scope where you disabled it.

`[Key]` is `Cratis.Chronicle.Keys.KeyAttribute`. It has no arguments and targets
a property or a record constructor parameter, never the class.

Read [references/model-bound-attributes.md](references/model-bound-attributes.md)
for the complete attribute list with exact arities, parameter names, and valid
targets. Two things there are worth knowing before you write anything:

- `[Nested]`, `[FromAll]`, and `[FromEvery]` are **non-generic**.
- `ConstantKey` on `[Count<T>]`, `[Increment<T>]`, `[Decrement<T>]`, and
  `[FromEvent<T>]` is an **object-initializer property**, never a constructor
  argument: `[Count<TEvent>(ConstantKey = "<key>")]`.

## Fluent projections — the fallback

Use `Cratis.Chronicle.Projections.IProjectionFor<TReadModel>` when the attributes
cannot express the shape — for example when two events feed one child collection
through different key properties.

```csharp
using Cratis.Chronicle.Projections;

public class <ReadModelName>Projection : IProjectionFor<<ReadModelName>>
{
    public void Define(IProjectionBuilderFor<<ReadModelName>> builder) =>
        builder
            .From<<EventName>>(from => from.UsingKey(@event => @event.<KeyProperty>))
            .RemovedWith<<RemovalEventName>>();
}
```

`Define` returns `void`. **There is no `Identifier` or `ProjectionId` member on
the interface** — do not add one. Use `[Projection(id: "<id>")]` when the
identity must be explicit.

Read [references/fluent-builder.md](references/fluent-builder.md) for the real
member list. The trap that costs the most time: **`UsingKey`, `UsingParentKey`,
`UsingCompositeKey`, and `UsingConstantKey` are not on the projection builder.**
They live on the per-event builder that `From<TEvent>` hands to your callback.

## Joins are on events, never on read models

Both `[Join<T>]` and the fluent `Join<TEvent>` take an **event** type. Joining on
a read model type is not supported and is not what the API means. If a read model
needs a field that only another read model has, the model is missing an event —
fix the event model rather than cross-reading at runtime.

### A joined property always wins — do not latch on it

A projection re-resolves its joins every time it handles one of the read model's
own events and applies the joined values **after** the local mappings, so a
property written both by a `From`/`[SetFrom]` and by a `Join` ends up with the
joined value regardless of the order the events arrived. A local write can
therefore never reset or clear a joined property. The natural latch shape — a
local event sets `IsBlocked = false`, a joined event sets it `true` — compiles,
reads correctly and silently sticks on the joined value. Give each fact its own
property (`BlockedAt`, `UnblockedAt`) and derive the outcome from both at the
query edge. Chronicle's analyzer **`CHR0042` (`JoinOverridesLocalWrite`)** reports
the overlap; suppress it only when the join is deliberately keeping that property
fresh.

## Variants — mutually exclusive shapes for one entity's lifecycle

Some entities do not have one shape for their whole lifetime — a work item is a
backlog entry until a pull request exists for it, then it is a pull request
until it merges. Modeling that as one read model with a `Status` property and
an ever-growing set of nullable columns makes every query filter on state
before it can trust a field. **Variants** let you declare several mutually
exclusive read models for the same logical entity instead — entering one
variant automatically removes the entity from every other variant in the
group, so at any point an entity exists in exactly one of them. Reach for
variants only when the shapes genuinely diverge; a single read model with a
`Status` flag is still right when every stage shares almost all its
properties.

> Variants require `Cratis.Chronicle` `19.1.0` or later — newer than this
> skill's `18.3.0` baseline. Reverify before claiming support.

Both approaches use the identity type only to anchor the group — it does not
need to be a read model itself, and does not need a common CLR base type with
any of the variants.

**Model-bound:** `[VariantOf<TIdentity>]` on each variant type, `[EntersOn<TEvent>]`
naming the event that activates it, and an optional `[GlobalFor<TIdentity>]` type
carrying mappings shared across every variant.

```csharp
[VariantOf<WorkItem>]
[EntersOn<IssueCreated>]
public record BacklogItem([property: Key] Guid Id, string Title);

[VariantOf<WorkItem>]
[EntersOn<PullRequestCreated>]
public record PullRequestItem([property: Key] Guid Id, [property: SetFrom<PullRequestCreated>] string PullRequestUrl);
```

**Fluent:** `.VariantOf<TIdentity>(keyAccessor)` and `.EntersOn<TEvent>()` are
members of `IProjectionBuilderFor<TReadModel>` itself — there is no
`[GlobalFor<T>]` equivalent, so a shared mapping is declared with `.From<TEvent>()`
on each variant's builder.

```csharp
public void Define(IProjectionBuilderFor<PullRequestItem> builder) => builder
    .VariantOf<WorkItem>(_ => _.Id)
    .EntersOn<PullRequestCreated>();
```

Only the entering event can create or resurrect a variant. Every other event a
variant handles — declared exactly like an ordinary `.From<TEvent>()` /
`[SetFrom<T>]` mapping — is automatically reclassified into an update-only
join, so an out-of-order or replayed event can never resurrect a variant the
entity has since left. A variant that declares no entering event at all throws
`VariantMustDeclareEntersOnEvent` at startup.

Read [references/variants.md](references/variants.md) for the complete group
declaration, the update-only reclassification in full, `[GlobalFor<T>]` rules
and its declaration-error trap, and best practices for picking an identity
type.

## Selecting the source

Class-level `[EventSequence("<name>")]`, `[EventLog]`, or `[EventStore("<name>")]`
choose where a model-bound projection reads from.

> **`[FromEventSequence]` no longer exists.** It was removed. The replacement on a
> model-bound read model is `[EventSequence("<name>")]` or `[EventLog]`. The
> fluent builder still has a `FromEventSequence(EventSequenceId)` **method** —
> that one is alive; do not confuse the two.

## Projections cannot filter by appended metadata

Chronicle correlates appended metadata in two different ways:

- **Projections** select their input through event types, joins, and event
  sequence configuration — and nothing else. A projection's definition has **no
  field for a filter at all**.
- **Reducers and reactors** can additionally filter by appended tag, event source
  type, and event stream type, using `[FilterEventsByTag]`, `[EventSourceType]`,
  and `[EventStreamType]`.

`[Tag]` and `[Tags]` on a projection or read model label the artifact; they never
filter. If you need metadata-based selection, pair the projection with a reducer
or a reactor rather than annotating the projection.

## Startup-crash traps

- **Duplicate class-level `[FromEvent<T>]`.** Declaring `[FromEvent<T>]` with no
  `key:` on **both** a parent and a nested or child type for the **same** event
  throws a duplicate-key exception at startup. Keep `[FromEvent<T>]` on the
  nested type only, or switch the nested type to property-level `[SetFrom<T>]`.
- **Setting the same read-model property twice for one event.** The per-event
  builder refuses a second `Set`/`Increment`/`Decrement`/`Add`/`Subtract` on a
  property it already wrote for that event (`DuplicatePropertyInProjection`,
  thrown while the projection is built at startup). Model-bound, that is two
  attributes for the same `<TEvent>` on one property that both target it — for
  example `[SetFrom<E>]` and `[Count<E>]` together. Several `[SetFromContext<E>]`
  on one member is a different case: it is accepted and **all but the last are
  discarded** — analyzer `CHR0040` reports it.
- **Chaining after `AutoMap()`/`NoAutoMap()`.** Those two return the base builder
  interface, so `.NotRewindable()`, `.Passive()`, `.ContainerName()`, and
  `.FromEventSequence()` will not compile after them. Put them last.
- **A variant with no `[EntersOn<T>]` / `.EntersOn<T>()`.** Throws
  `VariantMustDeclareEntersOnEvent` at startup — a variant that could never be
  entered could never be written to at all. See
  [references/variants.md](references/variants.md).

## `[FromAll]` versus `[FromEvery]`

- `[FromAll]` subscribes to **every** event type in the system. It is for audit
  and log models; pair it with `[NotRewindable]`. It targets a **property only**,
  so it cannot go on a record positional parameter.
- `[FromEvery]` captures across the events the model **already** declares through
  its `[FromEvent<T>]` attributes — for example to stamp `EventContext` data. It
  subscribes to nothing new.

Their two optional arguments are in **opposite order**: `[FromAll(contextProperty,
property)]` and `[FromEvery(property, contextProperty)]`. Name them.

## After creating

Build. Fix every error before completing. Then drive the contributing events
through the read-model specification for this model and assert the projected
state — seed each contributing stream with its own event-source setup when the
projection spans streams.

## Verify

- Model-bound attributes were exhausted before the fluent builder, and the
  fluent builder before any reducer.
- `.AutoMap()` is called only inside a `.NoAutoMap()` scope.
- Every join is on an event type.
- No `[FromEventSequence]` attribute appears anywhere.
- No projection carries a filter attribute expecting it to filter.
- `ConstantKey` is written as a property initializer.
- The same event does not carry class-level `[FromEvent<T>]` on both a parent and
  a nested or child type.
- Every variant group has at least one `[EntersOn<T>]` / `.EntersOn<T>()` per
  variant, and every `[GlobalFor<T>]` member exists on every variant it targets.
- The project builds clean and the read-model specifications pass against the
  verified package version.
