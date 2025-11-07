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

- `KeyAttribute` on any property, OR
- Any projection mapping attribute (SetFrom, AddFrom, Join, etc.)

No explicit marker attribute is required on the type itself.

## Key Features

Model-bound projections support all projection engine capabilities:

- **Property Mapping**: SetFrom, AddFrom, SubtractFrom
- **FromEvery**: Set properties from all events
- **Counters**: Increment, Decrement, Count
- **Relationships**: Join, Children
- **Removal**: RemovedWith, RemovedWithJoin
- **Convention-based**: FromEvent for automatic property matching
- **Configuration**: FromEventSequence, NotRewindable, Passive
- **Recursive**: All attributes work on child types and joined types

See the following pages for detailed information on each feature:

- [Basic Mapping](./basic-mapping.md) - SetFrom, AddFrom, SubtractFrom
- [FromEvery](./from-every.md) - Update properties from all events
- [Counters](./counters.md) - Increment, Decrement, Count
- [Children](./children.md) - Managing child collections
- [Joins](./joins.md) - Joining with other events
- [Convention-Based](./convention-based.md) - Automatic property matching
- [Configuration](./configuration.md) - Event sequence, rewindable, and active settings

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

**Fluent Projection:**

```csharp
public class AccountProjection : IProjectionFor<AccountInfo>
{
    public void Define(IProjectionBuilderFor<AccountInfo> builder) => builder
        .From<AccountOpened>()
        .Set(m => m.Name).To(e => e.Name)
        .Set(m => m.Balance).To(e => e.InitialBalance);
}
```

**Model-Bound Projection:**

```csharp
public record AccountInfo(
    [Key] Guid Id,
    [SetFrom<AccountOpened>(nameof(AccountOpened.Name))] string Name,
    [SetFrom<AccountOpened>(nameof(AccountOpened.InitialBalance))] decimal Balance);
```

Both approaches produce the same result, but model-bound projections are more concise for simple cases.
