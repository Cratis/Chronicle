---
title: CRUD, EF Core, and Chronicle
description: If you build apps with EF Core and think in tables, rows, and SaveChanges, here's how those ideas translate to Chronicle.
---

If your instinct is to add a table, map an entity, and call `SaveChanges()`, you already have useful muscle memory. This page maps the CRUD/EF Core model onto Chronicle so the differences are explicit.

## The mental shift in one sentence

In CRUD you **store the current state and overwrite it**; in Chronicle you **store what happened and derive the current state** from those facts. The current state still exists — it's a [read model](/chronicle/read-models/) — you just build it from events instead of editing it in place.

## How the pieces map

| You know (CRUD / EF Core) | In Chronicle |
|---|---|
| A table / entity | An [event source](/chronicle/concepts/event-source.md) and its stream of events |
| `INSERT` a row | Append a "created" [event](/chronicle/concepts/event.md) |
| `UPDATE` a column | Append an event describing *what changed* (e.g. `AddressChanged`) |
| `DELETE` a row | Append a "removed/closed" event — history is never erased |
| `DbContext.SaveChanges()` | `EventLog.Append(...)` |
| `SELECT` / LINQ query | A query over a [read model](/chronicle/read-models/) built by a [projection](/chronicle/concepts/projection.md) |
| A computed/denormalized view | A purpose-built read model — make as many as you need |
| `ALTER TABLE` / EF migration | [Event type migration](/chronicle/concepts/event-type-migrations.md) + replay the projection |
| Optimistic concurrency token | Constraints and the event stream's ordering |

## What stays the same

- You still use C# records and dependency injection.
- You still query data and render it — the read side looks like querying a collection.
- You can still keep a relational/document database for the genuinely-CRUD parts of your app; Chronicle doesn't demand all-or-nothing.

## What changes (and why)

- **You model verbs, not just nouns.** Instead of one mutable `Customer` row, you record `CustomerRegistered`, `AddressChanged`, `AccountClosed`. Each is an immutable fact. This is the part that feels new — and it's where the value (audit, history, replay) comes from.
- **Reads are eventually consistent — by default.** A projection materializes the read model *after* the event is appended, so a stored read immediately after a write may lag by a moment. Usually fine; occasionally something to design around. When read-after-write matters, Chronicle can also compute a read model on demand with strong consistency — see [Read model consistency](/chronicle/read-models/consistency.md).
- **You don't write update statements.** A projection *declares* how events map onto a read model; Chronicle keeps it current. No `UPDATE`, no merge logic.
- **You don't delete history.** "Delete" becomes an event. For real erasure obligations (GDPR), see [Compliance](/chronicle/compliance/).

## Side by side

CRUD: update a row, then read it back.

```csharp
// EF Core
customer.Address = newAddress;
await db.SaveChangesAsync();
var current = await db.Customers.FindAsync(id);
```

Chronicle: the same change is a fact you append and a read side you declare. First, define the verbs as event types:

```csharp
[EventType]
public record CustomerRegistered(string Name, string Address);

[EventType]
public record AddressChanged(string Address);
```

Then declare the read side. This is the [projection](/chronicle/concepts/projection.md) — and notice it is *not* a 1:1 copy of the CRUD `Customer` entity. It's shaped for the screen that reads it, and it answers something the overwritten row never could:

```csharp
[FromEvent<CustomerRegistered>]
[FromEvent<AddressChanged>]
public record CustomerCard(
    [Key] Guid Id,
    string Name,
    string Address,
    [Count<AddressChanged>] int TimesRelocated);
```

`[FromEvent<T>]` maps event properties onto the record by name — `Name` arrives with `CustomerRegistered`, and `Address` is kept current by every `AddressChanged`. `[Count<AddressChanged>]` counts the moves. There is no `UPDATE` statement anywhere: Chronicle derives the card from the facts.

Now the write and the read-back:

```csharp
await eventStore.EventLog.Append(customerId, new AddressChanged(newAddress));
var card = await eventStore.ReadModels.GetInstanceById<CustomerCard>(customerId);
```

`GetInstanceById` computes the card from its events on demand, so this read already reflects the append above — `TimesRelocated` included, a fact the CRUD row lost the moment `SaveChanges` ran.

## Not sure it's worth it?

Read [When to use event sourcing](/chronicle/concepts/when-to-use-event-sourcing/) for an honest take — there are domains where plain CRUD is the right answer, and that's fine.

## Next

- [Get started](/chronicle/get-started/) — scaffold and run in minutes.
- [Tutorial](/chronicle/tutorial/) — build a small system from events up.
