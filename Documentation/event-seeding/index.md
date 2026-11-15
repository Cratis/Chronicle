---
title: Seed an event store in Cratis Chronicle
description: Event seeding populates a Cratis Chronicle event store with initial events for baseline data, development, tests, and repeatable environment setup.
---

<a id="event-seeding"></a>

Event seeding populates a Cratis Chronicle event store with initial events when the client connects. Use it to supply baseline data your application depends on or give developers a known starting history for testing. The events go into the log, so they can feed the same read models and event handlers as events recorded by your application.

## Why seed events

Declare the history a new environment needs alongside your application code. That gives you a versioned starting point for demos and CI environments, and a way to repopulate a development store after clearing it.

Choose the scope before registering seeds. Global seeds reach every existing namespace in the event store and namespaces created later. A namespace is a separate data scope within an event store. Use namespace-scoped seeds for sample data that belongs only in one scope.

## When to use it

Use seeding for a repeatable starting history, not for editing events already recorded. Chronicle tracks successful seed entries, so rerunning the same definitions skips events already appended. Removing a seed definition never removes events from an existing namespace's log.

Seeding does not guarantee that the data is available before your application's first write. In particular, stored global seeds are applied asynchronously when a namespace is created. Keep that timing in mind if application startup depends on the data.

## Seed identity and routing

An event source identifier says which entity's history an event belongs to; stream routing places it within that history. Chronicle compares the event source identifier, event type, content, tag set, event source type, stream type, and stream identifier to decide what has already been seeded.

The number of identical entries matters: yielding an entry twice seeds two events. Rerunning that same list skips both successful occurrences. Identical payloads routed to different streams are distinct seeds. Entries that omit routing use the same defaults as legacy seed definitions, so omitting it does not cause existing seeds to be appended again.

## Next steps

Choose your client's guide to define seeds and register them. Each guide covers that client's APIs and behavior.

- [Seeding with C#](./seeding-with-csharp)
- [Seeding with Kotlin](/chronicle/clients/kotlin/guides/seeding/)
- [Seeding with Elixir](/chronicle/clients/elixir/seeding/)
- [Seeding with TypeScript](/chronicle/clients/typescript/seeding/)

