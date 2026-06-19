---
name: event-type-migrations
description: Evolve a Cratis Chronicle event schema without breaking replay — add a new generation and an EventTypeMigration so old stored events upcast into the new shape. Use when an event needs a new required property, a renamed property, or a structural change after events of the prior shape already exist.
---

# Event Type Migrations

Chronicle stores events forever. When an event's schema must change, you write a **migration** rather than editing the original record — Chronicle auto-discovers migrations and applies them when reading old events.

> **You only need this once events of the prior shape exist somewhere you can't regenerate** (a real environment's event log). Before that — in greenfield development with disposable data — rename event types and change schemas freely; a migration just adds dead code that hides the real schema in `git log`.

## When you need it

- An `[EventType]` needs a new required property (adding it would break observers reading old events).
- A property is renamed; old events carry the old name.
- The event shape changes structurally.

> **Never add a nullable value type to an `[EventType]` to represent "absent on old events"** — Chronicle's analyzer warns on nullable event members. Add a migration with a default instead.

## Generations

Every `[EventType]` has a **generation** (starts at `1`). Each schema change increments it. Chronicle routes stored events through the migration chain before delivering them to projections/reducers.

```
Generation 1 (stored) → Migration 1→2 → Migration 2→3 → Current (Generation 3)
```

## Steps

### 1. Keep the prior record and bump the generation on the new one

Keep the old shape available to the migration as `TPrevious`, and mark the current record's generation:

```csharp
[EventType(generation: 2)]
public record OrderPlaced(OrderId OrderId, Currency Currency);   // generation 2 (current)
```

### 2. Write the migration

Implement `EventTypeMigration<TUpgrade, TPrevious>` — `TUpgrade` is the current shape, `TPrevious` the prior. Chronicle extracts the generations, validates they're consecutive, and discovers the migration automatically (no registration).

`Upcast` and `Downcast` are both `public abstract void` and take an `IEventMigrationBuilder<TTarget, TSource>` — you describe the change declaratively through `builder.Properties(...)`, you do not construct the record by hand:

```csharp
public class OrderPlacedV1ToV2 : EventTypeMigration<OrderPlaced, OrderPlacedV1>
{
    public override void Upcast(IEventMigrationBuilder<OrderPlaced, OrderPlacedV1> builder) =>
        builder.Properties(p => p.DefaultValue(_ => _.Currency, Currency.From("NOK")));   // new field's default

    public override void Downcast(IEventMigrationBuilder<OrderPlacedV1, OrderPlaced> builder) =>
        builder.Properties(_ => { });   // map back for any consumers still on gen 1
}
```

The property builder exposes `DefaultValue`, `RenamedFrom`, `Split`, and `Combine` — use them to express the change declaratively. Both `Upcast` and `Downcast` are abstract on the base, so both must be implemented (`Downcast` may be a no-op `builder.Properties(_ => { })` when no consumer needs the gen-1 shape).

### 3. Chain across generations

For three generations, write two migrations (`1→2`, `2→3`) — each only knows its adjacent pair; Chronicle chains them.

## Common pitfalls

| Pitfall | Why it breaks |
|---|---|
| Editing the stored event record without bumping `generation` | Old events still carry the old schema; Chronicle won't migrate them |
| Adding a nullable value type to handle "missing old data" | Analyzer-flagged anti-pattern; use a migration default |
| A migration that throws on a null/missing old field | Old events may lack fields entirely — null-coalesce / default |
| Splitting one event into two inside `Upcast` | `Upcast` returns one event; model a split as a reactor/command, not a schema migration |

## Quality gate

- [ ] Build is clean.
- [ ] Old-generation events upcast to the current shape when replayed through a `ReadModelScenario<T>`.
- [ ] No nullable value types introduced on `[EventType]` records.

## See also

- `vertical-slices.md` — event type rules (non-nullable, naming).
- `event-modeling` — deciding when a fact is a new event vs a migration.
