# Event Seeding

Event seeding is a Chronicle Server capability that lets you provide a predefined set of events when the event store connects. It is useful when you need your deployment to include baseline data that the system depends on, or when you want developers to start from a known state with ready-to-use test data.

## Why seed events

- Ensure required domain data exists the first time a system starts in a new environment.
- Bootstrap development and testing with realistic data after clearing the event store.
- Provide a repeatable, versioned starting point for demos and CI environments.

## When to use it

Use event seeding when you need deterministic, append-only data to be part of the system from the beginning. Chronicle tracks seeded events so it is safe to start applications multiple times without duplicating data.

## Next steps

- [Seeding with C Sharp](./seeding-with-csharp.md)

