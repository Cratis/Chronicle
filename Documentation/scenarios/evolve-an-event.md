---
title: Evolve an event's shape in Cratis Chronicle
description: Evolve a Chronicle event type with schema generations and migrations, check both transformation directions, and rebuild affected read models.
---

Use event type migrations in Cratis Chronicle to introduce a new shape for an existing [event type](../concepts/event-type.md), such as renaming a field or supplying a default for a new property. Each shape has a generation number, so you can describe how the payload changes without replacing the event's identity.

## Why you don't edit history

Changing the event class alone does not tell Chronicle how an old field maps to the new one. An [event type migration](../concepts/event-type-migrations) supplies that relationship between consecutive generations: an upcast produces the newer shape, and a downcast produces the older one.

Chronicle applies migrations during append and stores the registered generations together. For existing history, a backfill adds missing generations without overwriting those already stored. Migration operations split or combine property values within one event type; they do not turn one event into several events.

## Do it

1. Define the next generation of the event type, keeping the same event type id. For example, generation 2 follows generation 1; a different id creates an unrelated event type rather than a migration.
2. Declare a migrator for that consecutive pair with both an upcast and a downcast. Use the [migration operations](../concepts/event-type-migrations) to map renamed properties, split or combine values, or supply defaults. Follow the linked client examples rather than translating an API from another language.
3. Check both directions with representative payloads before deployment. A full name split on spaces, for example, can lose part of the name. Decide what each consumer needs and whether a default or lossy transformation preserves that meaning.
4. Upgrade every kernel node before registering the new generation; older workers can still overwrite generations during backfill. Connect the application so its client registers the event types and migrations, then write new events using the intended generation.
5. Check the historical data your consumers need. Registration starts a backfill for the default namespace's event log only. Do not assume events in other namespaces or sequences have gained the new generation; follow the [migration and backfill boundaries](../concepts/event-type-migrations#protected-values).
6. Update consumers to use the intended generation and [rebuild affected read models](./rebuild-a-read-model.md). Query an instance built from older events as well as one built from a new append, and confirm that both have the expected values.

## Guidance

Use a new event type when the business fact changes meaning, rather than making one type stand for unrelated facts. Use a generation when the same fact needs a different payload shape.

Keep one migrator for every consecutive pair: generation 3 needs both 1→2 and 2→3. Changing a migrator later does not rewrite generations already stored. Consumer delivery also depends on its generation-delivery mode; use [pinned observer delivery](../concepts/event-type-migrations#opt-in-to-pinned-observer-delivery) when a supported consumer must receive the generation it subscribes to, including during replay.

## See also

- [Event Type Migrations](../concepts/event-type-migrations) — the full migration API and operations.
- [Rebuild a read model](./rebuild-a-read-model.md) — apply the new shape to existing read models.
