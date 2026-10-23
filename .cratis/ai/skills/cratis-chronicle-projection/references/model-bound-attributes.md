<!-- cratis-ai-managed: skills/cratis-chronicle-projection/references/model-bound-attributes.md -->
# Model-bound projection attributes

## Contents

- Namespaces that are not `Projections.ModelBound`
- Source and shape
- Setting values
- Counters
- Variants
- Relationships
- Source selection
- Labels, not filters
- What makes a type a model-bound projection
- AutoMap

Unless noted, every attribute below lives in
`Cratis.Chronicle.Projections.ModelBound`. Generic arity is stated exactly:
`<T>` means one generic parameter naming the event type; "non-generic" means the
attribute takes no generic parameter at all.

## Namespaces that are not `Projections.ModelBound`

| Attribute | Namespace |
| --- | --- |
| `[Key]` | `Cratis.Chronicle.Keys` |
| `[Index]` | `Cratis.Chronicle.ReadModels` |
| `[Passive]` | `Cratis.Chronicle.ReadModels` |
| `[NoAutoMap]` | `Cratis.Chronicle.Projections` |
| `[Projection]` | `Cratis.Chronicle.Projections` |
| `[EventSequence]`, `[EventLog]` | `Cratis.Chronicle.EventSequences` |
| `[EventStore]` | `Cratis.Chronicle.Events` |

`[ReadModel]` and `[Path]` are **Arc** types in `Cratis.Arc.Queries.ModelBound`.

## Source and shape

| Attribute | Arity | Parameters | Targets |
| --- | --- | --- | --- |
| `[FromEvent<T>]` | `<T>` | `(string? key = default, string? parentKey = default)`, plus `ConstantKey { get; init; }` | class, struct; repeatable |
| `[RemovedWith<T>]` | `<T>` | `(string? key = default, string? parentKey = default)` | class, struct, property, parameter; repeatable |
| `[RemovedWithJoin<T>]` | `<T>` | `(string? key = default)` | class, struct, property, parameter; repeatable |
| `[Nested]` | non-generic | none | property, parameter; single |
| `[FromAll]` | non-generic | `(string? contextProperty = default, string? property = default)` | **property only**; single |
| `[FromEvery]` | non-generic | `(string? property = default, string? contextProperty = default)` | property, parameter; single |
| `[NotProjected]` | non-generic | none | property, parameter |
| `[NoAutoMap]` | non-generic | none | class, struct, property, parameter; inherited |
| `[NotRewindable]` | non-generic | none | class, struct; single |
| `[Passive]` | non-generic | none | class, struct; single |
| `[Key]` | non-generic | none | property, parameter |
| `[Index]` | non-generic | none | property, parameter |

Note that `[FromAll]` and `[FromEvery]` take their two optional arguments in
**opposite order**. Always pass them by name.

`[FromEvery]` includes child-only and nested-child
contributing events in the owning parent's mappings (no attribute opt-out).
`[FromAll]` subscribes to every sequence event type except join-only types;
pair it with `[NotRewindable]`. `[NotProjected]` excludes a query-assembled member
from AutoMap and CHR0024. Sources: `Projections/ModelBound/NotProjectedAttribute.cs`,
`Projections/FromEveryBuilder.cs`, and `Documentation/projections/model-bound/`.

Requires Chronicle 19.78.1 for `[FromAll]`/`[FromEvery]` properties under
`[ChildrenFrom<T>]` or `[Nested]` to be declared on the child itself, including
the event creating it. Parent all-event mappings are not copied into children.
Older client definitions with root-declared child mappings work only when the
root has no property with the same name; otherwise the mapping stays on the
root until the client is upgraded. Check client/kernel versions and replay
affected projections to rebuild already materialized child values.

## Setting values

| Attribute | Arity | Parameters | Targets |
| --- | --- | --- | --- |
| `[SetFrom<T>]` | `<T>` | `(string? eventPropertyName = default)` | property, parameter; repeatable |
| `[SetValue<T>]` | `<T>` | `(object? value)` — **required**; pass `null` to clear | property, parameter; repeatable |
| `[SetFromContext<T>]` | `<T>` | `(string? contextPropertyName = default)` | property, parameter; repeatable |
| `[AddFrom<T>]` | `<T>` | `(string? eventPropertyName = default)` | property, parameter; repeatable |
| `[SubtractFrom<T>]` | `<T>` | `(string? eventPropertyName = default)` | property, parameter; repeatable |
| `[ClearWith<T>]` | `<T>` | **none** | class, property, parameter; repeatable |

Requires Chronicle 19.51.1 for dotted CLR member paths in model-bound attributes
and naming-policy conversion of every nested path segment. Check the client/kernel
versions; earlier nested mappings can remain empty. A corrected definition follows
`observers.definitionEvolution`: Automatic rebuilds it, while PartialOnly/Manual
can recommend replay. Do not assume upgrading repaired stored state.

## Counters

| Attribute | Arity | Parameters | Targets |
| --- | --- | --- | --- |
| `[Count<T>]` | `<T>` | none; `ConstantKey { get; init; }` | property, parameter; repeatable |
| `[Increment<T>]` | `<T>` | none; `ConstantKey { get; init; }` | property, parameter; repeatable |
| `[Decrement<T>]` | `<T>` | none; `ConstantKey { get; init; }` | property, parameter; repeatable |

`ConstantKey` is an **object-initializer property**, never a constructor
argument:

```csharp
[Count<<EventName>>(ConstantKey = "<key>")]
```

Setting it routes every matching event into a single aggregating document at
that key. That is a different mechanism from the fluent builder's
`UsingConstantKey("<key>")`, even though the effect is similar.

## Variants

| Attribute | Arity | Parameters | Targets |
| --- | --- | --- | --- |
| `[VariantOf<TIdentity>]` | `<TIdentity>` | none | class, struct; single |
| `[EntersOn<T>]` | `<T>` | `(string? key = default)` | class, struct; repeatable |
| `[GlobalFor<TIdentity>]` | `<TIdentity>` | none | class, struct; single |

Requires `Cratis.Chronicle` 19.1.0 or later for variants. See
[variants.md](variants.md) for the full mechanics, the update-only
reclassification of non-entering events, and the `VariantMustDeclareEntersOnEvent`
startup trap.

## Relationships

| Attribute | Arity | Parameters | Targets |
| --- | --- | --- | --- |
| `[ChildrenFrom<T>]` | `<T>` | `(string? key = default, string? identifiedBy = default, string? parentKey = default)` — three parameters, `identifiedBy` in the middle | property, parameter; repeatable |
| `[Join<T>]` | `<T>` | `(string? on = default, string? eventPropertyName = default)` | property, parameter; repeatable |

`[Join<T>]` joins on an **event**. Joining on a read model is not supported.

## Source selection

| Attribute | Parameters | Targets |
| --- | --- | --- |
| `[EventSequence("<name>")]` | `(string sequence)`; exposes `Sequence` | class |
| `[EventLog]` | none; equivalent to `[EventSequence]` for the event log | class |
| `[EventStore("<name>")]` | `(string eventStore)` | class, assembly; single |

**`[FromEventSequence]` was removed and does not exist.** The fluent builder's
`FromEventSequence(EventSequenceId)` method is unrelated and still present.

## Labels, not filters

| Attribute | Namespace | Parameters | Effect on a projection |
| --- | --- | --- | --- |
| `[Tag]` | `Cratis.Chronicle` | `(params string[] tags)`; repeatable | label only |
| `[Tags]` | `Cratis.Chronicle` | `(params string[] tags)`; repeatable | label only |

A projection's definition carries `Tags` but has **no filter field at all**.
`[FilterEventsByTag]`, `[EventSourceType]`, and `[EventStreamType]` only filter
for reducers and reactors.

## What makes a type a model-bound projection

See "What makes a type a model-bound projection" in SKILL.md.

## AutoMap

AutoMap is enabled by default for both model-bound types and the fluent builder.
`[NoAutoMap]` disables it, is inherited, and applies at class, struct, property,
or parameter level. Child and nested builders inherit the enclosing setting.
Call `.AutoMap()` only to re-enable inside a scope you disabled.
