---
title: Coming from CRUD and Entity Framework
description: If you build apps with EF Core and think in tables, rows, and SaveChanges, here's how those ideas translate to Chronicle — and what genuinely changes.
---

If your instinct is to add a table, map an entity, and call `SaveChanges()`, you already have the muscle memory you need — it just points at a different target. This page translates the CRUD/EF Core model into Chronicle so you can stop translating in your head.

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
- **Reads are eventually consistent.** A projection updates the read model *after* the event is appended, so a read immediately after a write may lag by a moment. Usually fine; occasionally something to design around. See [Read Models](/chronicle/read-models/).
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

Chronicle: append the fact; the read model reflects it.

```csharp
await eventStore.EventLog.Append(customerId, new AddressChanged(newAddress));
// A projection updates the `Customer` read model; query it like any collection.
```

## Not sure it's worth it?

Read [When to use event sourcing](/chronicle/concepts/when-to-use-event-sourcing/) for an honest take — there are domains where plain CRUD is the right answer, and that's fine.

## Next

- [Get started](/chronicle/get-started/) — scaffold and run in minutes.
- [Tutorial](/chronicle/tutorial/) — build a small system from events up.
