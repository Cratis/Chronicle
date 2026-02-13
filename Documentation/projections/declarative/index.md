# Declarative Projections

Declarative projections let you define read models with a fluent, code-first projection builder. They are ideal when you need explicit mapping, joins, or hierarchical projections that go beyond simple attribute mapping.

## Overview

Declarative projections implement `IProjectionFor<TReadModel>` and use an `IProjectionBuilderFor<TReadModel>` to define how events map to read model properties. The builder gives you control over mapping, relationships, and event context while keeping the projection definition separate from the read model type.

## Basic Example

```csharp
using Cratis.Chronicle.Projections;

[EventType]
public record UserRegistered(string Name, string Email, DateTimeOffset RegisteredAt);

public record UserProfile(string Name, string Email, DateTimeOffset RegisteredAt);

public class UserProfileProjection : IProjectionFor<UserProfile>
{
  public void Define(IProjectionBuilderFor<UserProfile> builder) => builder
    .From<UserRegistered>();
}
```

Auto-mapping is enabled by default at the top level, so matching properties are mapped automatically. When you need explicit mappings, you can use `.Set()`, `.Add()`, `.Subtract()`, and other builder operations.

## Discovery

Projection types are discovered by implementing `IProjectionFor<TReadModel>`. Event types used in projection definitions must be marked with `[EventType]`.

## Key Features

- **Auto mapping by default** with the option to turn it off or override per event
- **Explicit property mappings** for custom transformations
- **Hierarchies** with child collections and nested projections
- **Joins** across streams for richer read models
- **FromEvery** for applying mappings to all events
- **Event context** access for timestamps, sequence numbers, and IDs
- **Event sequence selection** for sourcing from non-default sequences
- **Passive and not rewindable projections** for specialized observation behavior

## When to Use

Use declarative projections when:

- You need control over mapping logic and transformations
- You need relationships or hierarchical read models
- You want to share a projection definition across multiple read models

Use model-bound projections when:

- The mapping is straightforward and attribute-based
- You want to keep projection metadata close to the read model
- You prefer concise, declarative attributes over fluent definitions

## Projection recipes

| Recipe | Description |
| ------ | ----------- |
| [Simple projection](simple-projection.md) | Basic projection using AutoMap() |
| [AutoMap](auto-map.md) | Automatic property mapping at different levels |
| [Passive](passive.md) | In-memory projections for on-demand lookups |
| [Set properties](set-properties.md) | Explicit property mapping and transformations |
| [Children](children.md) | Hierarchical data models with child collections |
| [Joins](joins.md) | Cross-stream projections using joins |
| [Functions](functions.md) | Arithmetic and other functions |
| [Composite keys](composite-keys.md) | Multi-property key identification |
| [Event context](event-context.md) | Using event metadata in projections |
| [FromEvery](from-every.md) | Setting properties for all events in a projection |
| [Initial values](initial-values.md) | Default values for read model properties |
| [RemoveWithJoin](remove-with-join.md) | Cross-stream child removal |
| [FromEventSequence](from-event-sequence.md) | Sourcing events from specific event sequences |
| [NotRewindable](not-rewindable.md) | Forward-only projections that cannot be replayed |

## Reading Your Declarative Projections

Once you've defined a declarative projection, you can retrieve and observe the resulting read models using the `IReadModels` API:

- [Getting a Single Instance](../../read-models/getting-single-instance.md) - Retrieve a specific instance by key with strong consistency
- [Getting a Collection of Instances](../../read-models/getting-collection-instances.md) - Retrieve all instances for reporting and analysis
- [Getting Snapshots](../../read-models/getting-snapshots.md) - Retrieve historical state snapshots grouped by correlation ID
- [Watching Read Models](../../read-models/watching-read-models.md) - Observe real-time changes as events are applied


## Key concepts

### Auto-mapping vs explicit mapping

- **Auto-mapping**: Automatically maps properties with matching names between events and read models
  - Use `.AutoMap()` in fluent projections (`IProjectionFor<T>`)
  - Use `[FromEvent<TEvent>]` in model-bound projections for the same functionality
- **Explicit mapping**: Gives you full control over property transformations and mappings
  - Use `.Set()`, `.Add()`, etc. in fluent projections
  - Use `[SetFrom<TEvent>]`, `[AddFrom<TEvent>]`, etc. in model-bound projections

### Event handling

- Projections can handle multiple event types
- Each event type can have its own property mappings
- Properties are updated incrementally as events are processed

### Keys and identification

- **EventSourceId** is used as the default key for both read models and parent identification in child collections
- **Child identifiers**: Use `.IdentifiedBy()` to specify how child items are uniquely identified within collections
- **Parent key resolution**:
  - By default, the EventSourceId is used to identify the parent when adding children
  - Use `.UsingParentKey(e => e.Property)` when the parent identifier is in the event content
  - Use `.UsingParentKeyFromContext(ctx => ctx.EventSourceId)` to explicitly document default behavior
- **Child key specification**: Use `.UsingKey(e => e.Property)` to extract the child identifier from event content
- **Joins**: Use keys to link data from different event streams using `.On(m => m.Property)`

### Performance

- Projections are automatically maintained as events are appended
- The system handles indexing and query optimization
- Consider join complexity and update frequency when designing projections
