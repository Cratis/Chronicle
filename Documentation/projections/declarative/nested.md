# Projection with a Nested Object

Projections can populate a single nullable child object on a read model using the `Nested()` method. Unlike [children collections](children.md), which manage an array of items identified by a key, `Nested()` targets a scalar nullable property that is set from an event and cleared (set to null) by another event.

## Defining a nested object projection

Use the `Nested()` method with `ClearWith<TEvent>()` to define the nested relationship:

```csharp
using Cratis.Chronicle.Projections;

public class SliceProjection : IProjectionFor<Slice>
{
    public void Define(IProjectionBuilderFor<Slice> builder) => builder
        .AutoMap()
        .From<SliceCreated>()
        .Nested(_ => _.Command, nested => nested
            .From<CommandSetForSlice>()
            .ClearWith<CommandClearedForSlice>());
}
```

## Read model with a nested property

The nested property must be nullable on the read model:

```csharp
public record Slice(
    string Name,
    CommandItem? Command);   // null until CommandSetForSlice is appended

public record CommandItem(
    string Name,
    string Schema);
```

## Event definitions

```csharp
[EventType]
public record SliceCreated(string Name);

[EventType]
public record CommandSetForSlice(string Name, string Schema);

[EventType]
public record CommandClearedForSlice;
```

## How nested objects work

1. When `CommandSetForSlice` is appended the `Command` property is populated on the parent
2. Subsequent `CommandSetForSlice` events replace the nested object with new values
3. When `CommandClearedForSlice` is appended the `Command` property is set to `null`

## Multiple from events

Call `From<TEvent>()` multiple times to update the nested object from several event types:

```csharp
.Nested(_ => _.Command, nested => nested
    .From<CommandSetForSlice>()
    .From<CommandRenamed>(b => b
        .Set(m => m.Name).To(e => e.NewName))
    .From<CommandSchemaUpdated>(b => b
        .Set(m => m.Schema).To(e => e.UpdatedSchema))
    .ClearWith<CommandClearedForSlice>())
```

Each `From<TEvent>()` call updates only the properties it explicitly maps or auto-maps — it does not replace the entire nested object.

## AutoMap on nested objects

AutoMap is enabled on the nested builder and inherits from the parent. Properties on the nested read model that share a name with properties on the event are mapped automatically:

```csharp
.Nested(_ => _.Command, nested => nested
    .AutoMap()
    .From<CommandSetForSlice>()       // Name, Schema auto-mapped
    .From<CommandUpdated>()           // Schema auto-mapped
    .ClearWith<CommandClearedForSlice>())
```

## Multiple nested objects

A single projection can have multiple independent nested properties:

```csharp
public class SliceProjection : IProjectionFor<Slice>
{
    public void Define(IProjectionBuilderFor<Slice> builder) => builder
        .AutoMap()
        .From<SliceCreated>()
        .Nested(_ => _.Command, nested => nested
            .From<CommandSetForSlice>()
            .ClearWith<CommandClearedForSlice>())
        .Nested(_ => _.Validation, nested => nested
            .From<ValidationConfigured>()
            .ClearWith<ValidationRemoved>());
}

public record Slice(
    string Name,
    CommandItem? Command,
    ValidationConfig? Validation);
```

## Nested within children

You can call `Nested()` from within a `Children()` builder to define a nested object on each child item:

```csharp
public class ProjectProjection : IProjectionFor<Project>
{
    public void Define(IProjectionBuilderFor<Project> builder) => builder
        .AutoMap()
        .From<ProjectCreated>()
        .Children(_ => _.Tasks, tasks => tasks
            .IdentifiedBy(t => t.TaskId)
            .From<TaskAdded>(b => b.UsingKey(e => e.TaskId))
            .Nested(_ => _.Assignee, assignee => assignee
                .From<TaskAssigned>()
                .ClearWith<TaskUnassigned>()));
}

public record Project(string Name, IEnumerable<Task> Tasks);
public record Task(Guid TaskId, string Title, Assignee? Assignee);
public record Assignee(string Name, string Email);
```

## Examples

### Employee with optional active contract

```csharp
public class EmployeeProjection : IProjectionFor<Employee>
{
    public void Define(IProjectionBuilderFor<Employee> builder) => builder
        .AutoMap()
        .From<EmployeeHired>()
        .Nested(_ => _.ActiveContract, contract => contract
            .AutoMap()
            .From<ContractStarted>()
            .From<ContractExtended>(b => b
                .Set(m => m.EndDate).To(e => e.NewEndDate))
            .ClearWith<ContractEnded>());
}

public record Employee(string Name, string Department, Contract? ActiveContract);
public record Contract(Guid ContractId, DateOnly StartDate, DateOnly EndDate, string Type);
```

Events:

```csharp
[EventType]
public record EmployeeHired(string Name, string Department);

[EventType]
public record ContractStarted(Guid ContractId, DateOnly StartDate, DateOnly EndDate, string Type);

[EventType]
public record ContractExtended(DateOnly NewEndDate);

[EventType]
public record ContractEnded;
```

### Product with optional promotion

```csharp
public class ProductProjection : IProjectionFor<Product>
{
    public void Define(IProjectionBuilderFor<Product> builder) => builder
        .AutoMap()
        .From<ProductListed>()
        .Nested(_ => _.Promotion, promotion => promotion
            .AutoMap()
            .From<PromotionApplied>()
            .ClearWith<PromotionRemoved>());
}

public record Product(string Name, decimal BasePrice, Promotion? Promotion);
public record Promotion(string Label, int DiscountPercent, DateTimeOffset ValidUntil);
```

## See Also

- [Children](children.md) — collections of items managed independently within a parent
- [Simple projection](simple-projection.md) — getting started with projections
- [AutoMap](auto-map.md) — automatic property mapping
