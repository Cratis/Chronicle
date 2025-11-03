# Model-Bound Projections

Model-bound projections allow you to define projections using C# attributes directly on your read model types, eliminating the need to implement `IProjectionFor<T>` for simple cases. This approach uses C# 11's generic attributes feature to provide a declarative way to configure projections.

## Overview

Instead of creating a separate projection class, you decorate your read model properties with attributes that describe how they should be populated from events. The projection system automatically discovers these attributes and builds the projection definition.

## Basic Example

```csharp
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

public record AccountInfo(
    [Key, FromEventSourceId]
    Guid Id,
    
    [SetFrom<AccountOpened>(nameof(AccountOpened.Name))]
    string Name,
    
    [SetFrom<AccountOpened>(nameof(AccountOpened.InitialBalance))]
    decimal Balance);
```

In this example:
- `[Key]` marks the property as the key for the read model (using `KeyAttribute` from `Cratis.Chronicle.Keys`)
- `[FromEventSourceId]` indicates the key comes from the event source ID
- `[SetFrom<TEvent>]` maps properties from specific events

## Discovery

Types are automatically discovered by the presence of:
- `KeyAttribute` on any property, OR
- Any projection mapping attribute (SetFrom, AddFrom, Join, etc.)

No explicit marker attribute is required on the type itself.

## Key Features

Model-bound projections support all projection engine capabilities:

- **Property Mapping**: SetFrom, AddFrom, SubtractFrom
- **Counters**: Increment, Decrement, Count
- **Relationships**: Join, Children
- **Removal**: RemovedWith, RemovedWithJoin
- **Convention-based**: FromEvent for automatic property matching
- **Recursive**: All attributes work on child types and joined types

See the following pages for detailed information on each feature:

- [Basic Mapping](./basic-mapping.md) - SetFrom, AddFrom, SubtractFrom
- [Counters](./counters.md) - Increment, Decrement, Count
- [Children](./children.md) - Managing child collections
- [Joins](./joins.md) - Joining with other events
- [Convention-Based](./convention-based.md) - Automatic property matching

## When to Use

Use model-bound projections when:
- You have simple to moderate projection logic
- The projection mapping is straightforward and declarative
- You prefer a more concise, attribute-based approach

Use fluent projections (`IProjectionFor<T>`) when:
- You need complex, imperative logic in your projections
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
    [Key, FromEventSourceId] Guid Id,
    [SetFrom<AccountOpened>(nameof(AccountOpened.Name))] string Name,
    [SetFrom<AccountOpened>(nameof(AccountOpened.InitialBalance))] decimal Balance);
```

Both approaches produce the same result, but model-bound projections are more concise for simple cases.
