# Model-Bound Projections

Model-bound projections allow you to define projections using C# attributes directly on your read model types, eliminating the need to implement `IProjectionFor<T>` and rather put in metadata directly into your read model.

## Overview

Instead of creating a separate projection class, you decorate your read model properties with attributes that describe how they should be populated from events. The projection system automatically discovers these attributes and builds the projection definition.

## Basic Example

```csharp
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

public record AccountInfo(
    [Key]
    Guid Id,

    [SetFrom<AccountOpened>(nameof(AccountOpened.Name))]
    string Name,

    [SetFrom<AccountOpened>(nameof(AccountOpened.InitialBalance))]
    decimal Balance);
```

In this example:

- `[Key]` marks the property as the key for the read model (using `KeyAttribute` from `Cratis.Chronicle.Keys`)
- `[SetFrom<TEvent>]` maps properties from specific events

## Discovery

Types are automatically discovered by the presence of:

- Any class/record level projection mapping attributes (FromEvent, Passive, NotRewindable, etc.)
- Any projection mapping attribute (SetFrom, AddFrom, Join, etc.)

No explicit marker attribute is required on the type itself.

## Key Features

Model-bound projections support all projection engine capabilities:

- **Property Mapping**: SetFrom, AddFrom, SubtractFrom, SetFromContext
- **FromEvery**: Set properties from all events
- **Counters**: Increment, Decrement, Count
- **Relationships**: Join, Children (with unlimited nesting depth)
- **Removal**: RemovedWith, RemovedWithJoin
- **Convention-based**: FromEvent for automatic property matching
- **Configuration**: FromEventSequence, NotRewindable, Passive
- **Fully Recursive**: All attributes work at any nesting level - children, grandchildren, and beyond

See the following pages for detailed information on each feature:

- [Basic Mapping](./basic-mapping.md) - SetFrom, AddFrom, SubtractFrom, SetFromContext
- [Convention-Based](./convention-based.md) - Automatic property mapping with FromEvent (equivalent to AutoMap)
- [FromEvery](./from-every.md) - Update properties from all events
- [Counters](./counters.md) - Increment, Decrement, Count
- [Children](./children.md) - Managing child collections
- [Removal](./removal.md) - Removing read models and children with RemovedWith, RemovedWithJoin
- [Joins](./joins.md) - Joining with other events
- [Event Sequence Source](./event-sequence-source.md) - Reading from specific event sequences
- [Not Rewindable](./not-rewindable.md) - Forward-only projections
- [Passive](./passive.md) - On-demand projections

## When to Use

Use model-bound projections when:

- You have simple to moderate projection logic
- The projection mapping is straightforward and declarative
- You prefer a more concise, attribute-based approach

Use fluent projections (`IProjectionFor<T>`) when:

- You prefer to separate the concerns of the representation of a read model and its projection definition
- You want more control over the projection building process
- You need to share projection logic across multiple read models

## Comparison with Fluent Projections

### Explicit Property Mapping

**Fluent Projection:**

```csharp
public class AccountProjection : IProjectionFor<AccountInfo>
{
    public void Define(IProjectionBuilderFor<AccountInfo> builder) => builder
        .From<AccountOpened>(_ => _
            .Set(m => m.Name).To(e => e.Name)
            .Set(m => m.Balance).To(e => e.InitialBalance));
}
```

**Model-Bound Projection:**

```csharp
public record AccountInfo(
    [Key] Guid Id,
    [SetFrom<AccountOpened>(nameof(AccountOpened.Name))] string Name,
    [SetFrom<AccountOpened>(nameof(AccountOpened.InitialBalance))] decimal Balance);
```

### Automatic Property Mapping

**Fluent Projection with AutoMap:**

```csharp
public class AccountProjection : IProjectionFor<AccountInfo>
{
    public void Define(IProjectionBuilderFor<AccountInfo> builder) => builder
        .AutoMap()
        .From<AccountOpened>();
}
```

**Model-Bound Projection with FromEvent:**

```csharp
[FromEvent<AccountOpened>]
public record AccountInfo(
    [Key] Guid Id,
    string Name,        // Automatically mapped from AccountOpened.Name
    decimal Balance);   // Automatically mapped from AccountOpened.Balance
```

Both approaches produce the same result. Model-bound projections with `FromEvent` are particularly concise when property names match between events and read models, providing the same automatic mapping benefits as `.AutoMap()` in fluent projections.

## Reading Your Model-Bound Projections

Once you've defined a model-bound projection, you can retrieve and observe the resulting read models using the `IReadModels` API:

- [Getting a Single Instance](../../read-models/getting-single-instance.md) - Retrieve a specific instance by key with strong consistency
- [Getting a Collection of Instances](../../read-models/getting-collection-instances.md) - Retrieve all instances for reporting and analysis
- [Getting Snapshots](../../read-models/getting-snapshots.md) - Retrieve historical state snapshots grouped by correlation ID
- [Watching Read Models](../../read-models/watching-read-models.md) - Observe real-time changes as events are applied
