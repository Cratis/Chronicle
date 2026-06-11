# Model-Bound Projections

Model-bound projections let the read model describe how it is built. Instead of creating a separate `IProjectionFor<T>` class, you put the projection metadata directly on the read model type and Chronicle builds the projection definition from those attributes.

## Overview

Start with convention-based mapping and add explicit attributes only where the event and read model use different names. That keeps the common case small while still making the exceptions visible.

## Basic Example

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record AccountOpened(string Name, decimal InitialBalance);

[FromEvent<AccountOpened>]
public record AccountInfo(
    [Key]
    Guid Id,

    string Name,

    [SetFrom<AccountOpened>(nameof(AccountOpened.InitialBalance))]
    decimal Balance);
```

In this example:

- `[FromEvent<AccountOpened>]` creates or updates an `AccountInfo` instance when an `AccountOpened` event is processed
- `[Key]` marks the read model identifier
- `Name` maps by convention because both the event and the read model use the same property name
- `Balance` uses `[SetFrom<TEvent>]` because the event property is called `InitialBalance`

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
- [Set Constant Value](./set-value.md) - Set a property to a fixed value with SetValue
- [Convention-Based](./convention-based.md) - Automatic property mapping with FromEvent (equivalent to AutoMap)
- [FromEvery](./from-every.md) - Update properties from all events
- [FromAll](./from-all.md) - Subscribe to all event types without filtering
- [Counters](./counters.md) - Increment, Decrement, Count
- [Children](./children.md) - Managing child collections
- [Removal](./removal.md) - Removing read models and children with RemovedWith, RemovedWithJoin
- [Joins](./joins.md) - Joining with other events
- [Event Sequence Source](./event-sequence-source.md) - Reading from specific event sequences
- [Not Rewindable](./not-rewindable.md) - Forward-only projections
- [Passive](./passive.md) - On-demand projections

## When to Use

Prefer model-bound projections by default:

- They keep projection metadata next to the read model
- They cover the common Chronicle projection capabilities without a separate class
- They make the happy path concise and consistent across features

Use fluent projections (`IProjectionFor<T>`) as a fallback when the projection cannot be expressed cleanly with model-bound attributes:

- You need projection construction that is more dynamic or procedural
- You intentionally want to separate the read model from the projection definition
- You need specialized builder features that do not map naturally to attributes

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
