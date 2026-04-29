# Chronicle Projection API

---

## Model-Bound Projections (preferred)

Apply attributes directly to read model types. No separate projection class needed.

### Class-level attributes

#### `[FromEvent<T>]`
Auto-maps all properties with matching names from event `T` to the read model.
Equivalent to `.AutoMap().From<T>()` in the fluent API.

```csharp
[ReadModel]
[FromEvent<AccountOpened>]
public record Account(
    [Key] Guid Id,
    string Name,          // auto-mapped from AccountOpened.Name
    decimal Balance);     // auto-mapped from AccountOpened.Balance
```

#### `[FromEvent<T>(key: nameof(T.PropertyName))]`
Same as above, but uses the specified event property as the read model key instead of EventSourceId.

```csharp
[ReadModel]
[FromEvent<OrderPlaced>(key: nameof(OrderPlaced.OrderId))]
public record Order([Key] Guid Id, decimal Total);
```

Multiple `[FromEvent<T>]` attributes are supported for different events:

```csharp
[FromEvent<AccountOpened>]
[FromEvent<AccountRenamed>]
public record Account([Key] Guid Id, string Name, decimal Balance);
```

---

### Property-level attributes

#### `[Key]`
Marks the primary key property.

#### `[SetFrom<T>]` / `[SetFrom<T>(nameof(T.Prop))]`
Maps a specific property from event `T`. Omit the property name if the names match.

#### `[SetFrom<T>]` with multiple events
Apply multiple attributes to update the same property from different events.

```csharp
[SetFrom<AccountOpened>(nameof(AccountOpened.AccountName))]
[SetFrom<AccountRenamed>(nameof(AccountRenamed.NewName))]
string Name
```

#### `[AddFrom<T>(nameof(T.Prop))]`
Adds the event property value to the read model property (accumulates).

#### `[SubtractFrom<T>(nameof(T.Prop))]`
Subtracts the event property value.

#### `[ChildrenFrom<T>(key: nameof(T.ChildId))]`
Projects into a nested child collection. The child type also supports all attributes recursively.

```csharp
public record Order(
    [Key] Guid Id,
    [ChildrenFrom<LineItemAdded>(key: nameof(LineItemAdded.ItemId))]
    IEnumerable<LineItem> Items);

public record LineItem(
    [Key] Guid Id,
    string ProductName,   // auto-mapped
    int Quantity,         // auto-mapped
    decimal Price);       // auto-mapped
```

#### `[Join<T>(on: nameof(Prop), eventPropertyName: nameof(T.EProp))]`
Joins data from a related event (keyed by a different entity).

```csharp
[Join<CustomerRegistered>(
    on: nameof(CustomerId),
    eventPropertyName: nameof(CustomerRegistered.Name))]
string CustomerName
```

#### `[RemovedWith<T>]`
Marks the instance removed when event `T` is appended.

---

## Fluent Projections (alternative for complex cases)

Use `IProjectionFor<T>` when model-bound attributes don't fit the complexity of the projection.

### Basic structure

```csharp
public class MyProjection : IProjectionFor<MyReadModel>
{
    public void Define(IProjectionBuilderFor<MyReadModel> builder) =>
        builder
            .From<SomeEvent>(...);  // AutoMap is on by default
}
```

> **There is NO `Identifier`/`ProjectionId` property** — do not add one.

---

### `.AutoMap()`

Maps all event properties to read model properties with matching names automatically.
**AutoMap is on by default — you only need to call it explicitly if you previously used `.NoAutoMap()`.**

---

### `.From<TEvent>(Action<IFromBuilder<TReadModel, TEvent>>)`

Handles an event type. Builder methods:

- `.UsingKey(e => e.PropertyOnEvent)` — sets the key from an event property
- `.UsingParentKey(e => e.PropertyOnEvent)` — sets key from parent (child projections)
- `.Set(m => m.Target).To(e => e.Source)` — explicit property mapping
- `.Set(m => m.Prop).ToValue(literal)` — set to constant value
- `.Add(m => m.Counter).With(1)` — increment by fixed value
- `.Subtract(m => m.Counter).With(1)` — decrement by fixed value
- `.Count(m => m.TotalCount)` — increment count by 1

---

### `.Children<TChild>(m => m.ChildCollection, childBuilder => ...)`

Projects into a nested collection.

```csharp
builder
    .Children<LineItem>(m => m.LineItems, child =>  // AutoMap is on by default
        child
            .From<LineItemAdded>(b =>
                b.UsingKey(e => e.LineItemId)
                 .Set(li => li.Description).To(e => e.Description))
            .RemovedWith<LineItemRemoved>());
```

---

### `.RemovedWith<TEvent>()`

Marks the read model as removed when the specified event is appended.

---

### `.Join<TEvent>(Action<IJoinBuilder<TReadModel, TEvent>>)`

Joins data from a different event. The join is always on the **event**, never on the read model.

```csharp
builder
    .From<ProjectRegistered>()                      // AutoMap is on by default
    .Join<OwnerAssigned>(b =>
        b.On(e => e.ProjectId)
         .Set(m => m.OwnerName).To(e => e.OwnerName));
```
