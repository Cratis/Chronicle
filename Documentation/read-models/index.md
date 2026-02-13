---
uid: Chronicle.ReadModels
---
# Read Models

Read models in Chronicle represent the current state of your application derived from events stored in the event log. They provide a denormalized, queryable view of your data optimized for reading, making them essential for building responsive applications.

## What are Read Models?

A read model is a projection of events into a structured format that's optimized for querying. Unlike traditional databases where you store current state directly, Chronicle builds read models by applying events from the event log, ensuring your data is always in sync with what actually happened in your system.

Read models serve several purposes:

- **Query optimization**: Denormalized views designed for specific read patterns
- **Strong consistency**: Can be retrieved with the exact current state
- **Audit capability**: Can track how state evolved through event snapshots
- **Flexibility**: Multiple read models can be created from the same events

## How Read Models are Produced

Read models are produced by either projections or reducers, both exposed through the `IReadModels` API. Use the overview pages to choose the approach and then drill into the projection style you need:

- [Projections overview](../projections/index.md)
- [Reducers overview](../reducers/index.md)
- [Declarative projections](../projections/declarative/index.md)
- [Model-bound projections](../projections/model-bound/index.md)

## Read Model Access

Chronicle provides an API for working with read models regardless of how they are produced. The next steps show the different ways to get instances, collections, and snapshots, and how to observe changes.

## Key Characteristics

### Strong Consistency

When you retrieve a read model instance, Chronicle ensures you get the most up-to-date state by applying all relevant events from the event log. This provides strong consistency guarantees.

### On-Demand Computation

Read models are computed on-demand by replaying events. This ensures accuracy but comes with performance considerations for read models with long event histories.

### Type Safety

The API provides full type safety through generic methods, ensuring compile-time checking when working with read models.

## Next Steps

- [Getting a Single Instance](getting-single-instance.md) - Learn how to retrieve a specific read model instance
- [Getting a Collection of Instances](getting-collection-instances.md) - Learn how to retrieve all instances of a read model
- [Getting Snapshots](getting-snapshots.md) - Understand how to retrieve historical snapshots of read model state
- [Watching Read Models](watching-read-models.md) - Learn how to observe real-time changes to read models
