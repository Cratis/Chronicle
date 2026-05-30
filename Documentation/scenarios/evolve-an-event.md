---
title: Evolve an event's shape
description: Change an event type over time without rewriting the history that's already been written.
---

**Goal:** an [event type](../concepts/event-type.md) needs to change — a renamed field, a split into two events, a new value with a sensible default — but you have years of old events already in the log that you must not corrupt.

## Why you don't edit history

Events are **immutable facts**. Rewriting old events to a new shape would be falsifying the record — and it breaks the guarantee everything else depends on. Instead, you describe how to *read* old events as the new shape: an [event type migration](../concepts/event-type-migrations.md). The stored events stay exactly as written; Chronicle upgrades them on the way out.

## Do it

1. Define the new shape of the event type.
2. Register an **event type migration** that maps the previous generation to the new one — renaming, splitting, joining, or supplying a default for a new field (see [Event Type Migrations](../concepts/event-type-migrations.md) for the typed builder API).
3. Deploy. New events are written in the new shape; old events are migrated to it transparently when read and when projections replay.
4. [Rebuild affected read models](./rebuild-a-read-model.md) so they reflect the migrated shape.

## Guidance

- **Add, don't mutate.** Prefer adding a new event type or a new optional concept over overloading an existing event — it keeps each event a single, clear fact.
- **Migrations are cumulative.** Each generation maps to the next, so events written under any past version can be read as the current one.

## See also

- [Event Type Migrations](../concepts/event-type-migrations.md) — the full migration API and operations.
- [Rebuild a read model](./rebuild-a-read-model.md) — apply the new shape to existing read models.
