# Concepts

This area contains explanations and guidance on the concepts in Cratis.

| Topic | Description |
| ------- | ----------- |
| [Event](./event.md) | What constitutes an event |
| [Event Type](./event-type.md) | What is an event type |
| [Modeling events well](./modeling-events.md) | How to design events that are clear today and still make sense in five years |
| [Event Type Migrations](./event-type-migrations.md) | How event schemas evolve without breaking existing events |
| [Event Source](./event-source.md) | What is an event source |
| [Subject](./subject.md) | The identity an event is *about* — used to key compliance concerns |
| [Event Store](./event-store.md) | Where your events live — durably persisted in a database |
| [Namespaces](./namespaces.md) | What are namespaces within an event store |
| [Event Sequence](./event-sequence.md) | What is an event sequence |
| [Observer](./observers.md) | What is an observer |
| [Projections, reducers, and reactors](./observer-patterns.md) | The three observer patterns side by side — deriving state from events vs causing side effects |
| [Projection](./projection.md) | How a projection declaratively maps events onto a read model instance |
| [Designing read models](./designing-read-models.md) | Specialize each read model for the query it serves, not for reuse |
| [Deep dive: consistency](./consistency.md) | Immediate vs eventual consistency, and how to choose |
| [Constraints](./constraints.md) | What are constraints |
| [Event Metadata Tags](./event-metadata-tags.md) | Metadata tags attached to events |
| [Tagging](./tagging.md) | How to tag events for organization and filtering |
| [Tagging Reactors](./tagging-reactors.md) | How to use tags with reactors |
| [Geospatial Types](./geospatial.md) | Built-in geospatial types for events and read models |
| [When to use event sourcing](./when-to-use-event-sourcing.md) | An honest look at where event sourcing pays off — and where it doesn't |
| [Glossary](./glossary.md) | One definition for each core Chronicle term |

> [!NOTE]
> Where these pages say *database*, you have a choice: Chronicle can persist the
> [event store](./event-store.md) and its read models in MongoDB, PostgreSQL,
> Microsoft SQL Server, or SQLite.

One nuance worth carrying with you: [projections and reducers](./observer-patterns.md) are not only
factories for materialized, queryable views. The state they derive from events can be stored in a
database for fast queries — or computed on demand, with strong consistency, for anything that must
trust the current state: validation rules, command handlers, or
[aggregate roots in Arc](/arc/backend/chronicle/aggregates/aggregate-root.md).
