---
name: cratis-chronicle-event-type-migration
description: Evolve a Chronicle event schema without breaking replay - add a generation and an EventTypeMigration so stored events upcast into the new shape. Use when an event needs a new required property, a renamed or split property, a structural change, or a changed enum value after events of the prior shape already exist. Do not use for greenfield renames, for read-model changes, or for redacting stored content.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-chronicle-event-type-migration/SKILL.md -->

# Chronicle event type migrations

Chronicle stores events forever. When an event's schema must change, you add a
**generation** and write a **migration** rather than editing the original record.
Chronicle discovers migrations by convention and applies them when reading older
events.

> You need this only once events of the prior shape exist somewhere you cannot
> regenerate. Before that — greenfield development with disposable data — rename
> event types and change schemas freely. A migration written too early is dead
> code that hides the real schema in the history.

## Verified product sources

This skill is verified against these exact sources:

| Package | Version | Purpose |
| --- | --- | --- |
| `Cratis.Chronicle` | `16.45.2` | `Cratis.Chronicle.Events` and `Cratis.Chronicle.Events.Migrations` |
| `Cratis.Chronicle.CodeAnalysis` | `16.45.2` | `CHR0012`, `CHR0037`, `CHR0049`, `CHR0050` |

Reverify product sources before claiming support for another version.

## Generations

`Cratis.Chronicle.Events.EventTypeAttribute` carries the identity and the
generation:

```csharp
public sealed class EventTypeAttribute(string id = "", uint generation = 1) : Attribute
```

`id` defaults to the CLR type name and `generation` starts at `1`. The attribute
is not repeatable, so two generations are always two distinct CLR types.
Chronicle routes a stored event through the migration chain before delivering it
to observers:

```text
Generation 1 (stored) -> Migration 1->2 -> Migration 2->3 -> Current (Generation 3)
```

## Step 1 — Keep the prior record and bump the generation

Keep the old shape available to the migration as `TPrevious`. Bump the current
record's generation, and mark the prior record with
**`[EventTypeGenerationFor<TCurrent>(N-1)]`** rather than giving it its own
`[EventType]`:

```csharp
using Cratis.Chronicle.Events;

[EventType(generation: 2)]
public record <EventName>(<IdType> <IdProperty>, <NewType> <NewProperty>);

[EventTypeGenerationFor<<EventName>>(1)]
public record <EventName>V1(<IdType> <IdProperty>);
```

`EventTypeGenerationForAttribute<TEventType>` takes exactly one argument, the
generation. There is no id argument, which is the point: the event type id is
resolved from the current record's `[EventType]`, so the two generations cannot
end up with mismatched ids.

The older style — giving the prior record its own
`[EventType("<explicit-id>", generation: 1)]` with an id string identical to the
current generation's — is still supported and still works. Prefer
`[EventTypeGenerationFor<T>]` for anything new: with the explicit-id style, an
omitted or mistyped id silently defaults to the CLR type name, Chronicle then
treats the two as unrelated event types, and the migration never applies.

Three diagnostics guard this:

- **CHR0037** (warning) — the generations a migration references must resolve to
  the same event type and differ only by generation.
- **CHR0049** (error) — `[EventTypeGenerationFor<T>]` must reference a type
  marked with `[EventType]`.
- **CHR0050** (error) — a type cannot carry both `[EventType]` and
  `[EventTypeGenerationFor<T>]`.

At runtime the `EventTypeMigration<,>` constructor throws
`MigrationGenerationsMustShareEventTypeId` on an id mismatch, and
`InvalidMigrationGenerationGap` when the generations are not consecutive.

## Step 2 — Write the migration

Derive from `Cratis.Chronicle.Events.Migrations.EventTypeMigration<TUpgrade,
TPrevious>`. `Upcast` and `Downcast` are `public abstract void`, so both must be
implemented. You describe the change declaratively through `builder.Properties`;
you never construct the record by hand.

**The two builders have flipped generic arguments.** The builder is always
`IEventMigrationBuilder<TTarget, TSource>`, so upcasting takes
`<TUpgrade, TPrevious>` and downcasting takes `<TPrevious, TUpgrade>`:

```csharp
using Cratis.Chronicle.Events.Migrations;

public class <EventName>V1To<EventName> : EventTypeMigration<<EventName>, <EventName>V1>
{
    public override void Upcast(IEventMigrationBuilder<<EventName>, <EventName>V1> builder) =>
        builder.Properties(properties => properties
            .DefaultValue(target => target.<NewProperty>, <defaultValue>));

    public override void Downcast(IEventMigrationBuilder<<EventName>V1, <EventName>> builder) =>
        builder.Properties(properties => { });
}
```

`Downcast` may be an empty `builder.Properties(properties => { })` when no
consumer needs the earlier shape, but it must still be declared.

### The property builder

`IEventMigrationPropertyBuilder<TTarget, TSource>` has exactly five fluent
members:

| Member | Use |
| --- | --- |
| `DefaultValue(targetProperty, value)` | supply a value the old events never carried |
| `RenamedFrom(targetProperty, sourceProperty)` | the property moved name |
| `Split(targetProperty, sourceProperty, separator, part)` | one old value becomes several |
| `Combine(targetProperty, separator, params sourceProperties)` | several old values become one |
| `MapValues(targetProperty, sourceProperty, map)` | one direction's value translation |

There is **no `Rename`** — the method is `RenamedFrom`. There is **no `Join`** —
concatenation is `Combine`.

`PropertySeparator` and `SplitPartIndex` convert implicitly from `string` and
`int`, and expose `PropertySeparator.Space`, `SplitPartIndex.First`, and
`SplitPartIndex.Second`. Property expressions may address nested paths.

```csharp
public override void Upcast(IEventMigrationBuilder<<EventName>, <EventName>V1> builder) =>
    builder.Properties(properties => properties
        .Split(target => target.<FirstPart>, source => source.<Combined>, PropertySeparator.Space, SplitPartIndex.First)
        .Split(target => target.<SecondPart>, source => source.<Combined>, PropertySeparator.Space, SplitPartIndex.Second));

public override void Downcast(IEventMigrationBuilder<<EventName>V1, <EventName>> builder) =>
    builder.Properties(properties => properties
        .Combine(target => target.<Combined>, PropertySeparator.Space,
            source => source.<FirstPart>, source => source.<SecondPart>));
```

## Step 3 — When the values changed meaning, declare a value map

The operations above move values between properties. When a *value itself* means
something different in the new generation — an enum renumbered, a status code
set replaced — override **`MapValues`** on the migration. It is `virtual` with an
empty default, so override it only when you need it. It is declared once and
applied forward when upcasting and inverted when downcasting.

`IEventValueMapBuilder<TUpgrade, TPrevious>` has one member, `For`. `Map` lives
on the inner `IValueMapBuilder<TFrom, TTo>` that `For` hands you, and its
arguments read **previous value first, current value second**:

```csharp
public override void MapValues(IEventValueMapBuilder<<EventName>, <EventName>V1> builder) =>
    builder.For(
        current => current.<Property>,
        previous => previous.<Property>,
        map => map
            .Map(<PreviousEnum>.<OldMember>, <CurrentEnum>.<NewMember>)
            .Map(<PreviousEnum>.<OtherOldMember>, <CurrentEnum>.<OtherNewMember>));
```

Values the map does not mention are carried across unchanged. When two values
collapse onto one, the inverse takes the **first pair declared** for that value.
`MapValues` runs *before* `Upcast`/`Downcast`, so a direction that states its own
transformation for the property keeps it — that is also how you express a
deliberately one-way translation, using the property builder's own `MapValues`.

### Enums: what actually needs a migration

Chronicle compares stored and generated schemas by the enum's **underlying
values**, not its member names. So:

- **Adding a member** — compatible, no migration.
- **Renaming a member** — compatible, no migration; the values are unchanged.
- **Removing a member** — incompatible; new generation plus a value map.
- **Renumbering members** — incompatible; new generation plus a value map.

An incompatible change without a new generation fails registration with
`EventTypeSchemaChanged`.

## Step 4 — Chain across generations

For three generations, write two migrations (`1->2` and `2->3`). Each knows only
its adjacent pair; Chronicle chains them. Two migrations claiming the same
`(from, to)` pair for one event type throw
`MultipleMigratorsForSameEventTypeGeneration`.

Migrations are discovered by convention — nothing is registered. They are
activated through the service provider, so a migration may take constructor
dependencies.

## Do not model absence with a nullable property

**CHR0012** (warning) flags a nullable property on an `[EventType]` record.
Model an optional fact as a separate event, or supply a `DefaultValue` in the
migration. Note the diagnostic only inspects types carrying `[EventType]`, so a
prior generation marked with `[EventTypeGenerationFor<T>]` is not checked — that
is expected, since the prior generation records history rather than new intent.

## Common pitfalls

| Pitfall | Why it breaks |
| --- | --- |
| Editing the stored record without bumping `generation` | old events still carry the old schema; nothing migrates them |
| Giving the prior record its own `[EventType]` with no id, or a mismatched id | Chronicle treats the two as unrelated event types and the migration never applies |
| Writing both builders as `IEventMigrationBuilder<TUpgrade, TPrevious>` | `Downcast` takes the flipped pair and will not compile |
| Reaching for `Rename` or `Join` | neither exists; use `RenamedFrom` and `Combine` |
| Calling `Map` on `IEventValueMapBuilder` | `Map` is on the inner `IValueMapBuilder` reached through `For` |
| Adding a nullable property to represent "missing on old events" | CHR0012; supply a `DefaultValue` instead |
| Splitting one event into two inside `Upcast` | a migration produces one event; model a split as a reactor or a command |
| Adding a new generation for a renamed enum member | not needed; only removal or renumbering requires one |

## Verify

- The current record carries the bumped `[EventType(generation: N)]`.
- The prior record carries `[EventTypeGenerationFor<TCurrent>(N-1)]`, or an
  explicit id identical to the current generation's.
- The migration derives from `EventTypeMigration<TUpgrade, TPrevious>` and
  implements both `Upcast` and `Downcast` with the correct generic order.
- `MapValues` is overridden only where a value's meaning changed.
- No nullable property was added to an `[EventType]` record.
- The build is clean with no `CHR0012`, `CHR0037`, `CHR0049`, or `CHR0050`
  outstanding.
- Old-generation events replay into the current shape through the read-model
  specifications for the affected observers.
