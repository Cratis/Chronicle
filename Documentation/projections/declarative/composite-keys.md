# Projection with composite keys

When a single property isn't sufficient to uniquely identify a projection instance, you can use composite keys made up of multiple values. This is useful for multi-tenant scenarios, hierarchical data, or when you need complex keys.

## Defining a composite key

Use `UsingCompositeKey<>()` to define a key made up of multiple properties:

```csharp
public class OrderProjection : IProjectionFor<Order>
{
    public void Define(IProjectionBuilderFor<Order> builder) => builder
        .AutoMap()
        .From<OrderCreated>(_ => _
            .UsingCompositeKey<OrderKey>(_ => _
                .Set(k => k.CustomerId).To(e => e.CustomerId)
                .Set(k => k.OrderNumber).To(e => e.OrderNumber)))
        .From<OrderShipped>(_ => _
            .UsingCompositeKey<OrderKey>(_ => _
                .Set(k => k.CustomerId).To(e => e.CustomerId)
                .Set(k => k.OrderNumber).To(e => e.OrderNumber)));
}
```

## Composite key type

Define a record or class to represent your composite key:

```csharp
public record OrderKey(string CustomerId, string OrderNumber);
```

## Read model with composite key

The read model's `Id` property should match your composite key type:

```csharp
public record Order(
    OrderKey Id,
    string CustomerName,
    DateTimeOffset OrderDate,
    DateTimeOffset? ShippedDate);
```

## Composite keys with event context

You can combine event properties with event context properties in composite keys:

```csharp
public class AuditProjection : IProjectionFor<AuditEntry>
{
    public void Define(IProjectionBuilderFor<AuditEntry> builder) => builder
        .AutoMap()
        .From<UserAction>(_ => _
            .UsingCompositeKey<AuditKey>(_ => _
                .Set(k => k.UserId).To(e => e.UserId)
                .Set(k => k.Timestamp).ToEventContextProperty(c => c.Occurred)));
}
```

The corresponding key and read model:

```csharp
public record AuditKey(string UserId, DateTimeOffset Timestamp);

public record AuditEntry(
    AuditKey Id,
    string Action,
    string Details);
```

## Composite keys in child collections

Composite keys work with child collections too:

```csharp
.Children(m => m.OrderItems, children => children
    .IdentifiedBy(e => e.ItemId)
    .AutoMap()
    .From<ItemAddedToOrder>(_ => _
        .UsingCompositeKey<ItemKey>(_ => _
            .Set(k => k.ProductId).To(e => e.ProductId)
            .Set(k => k.Variant).To(e => e.Variant))))
```

## Joins with composite keys

Composite keys can be used in join scenarios:

```csharp
.AutoMap()
.Join<ProductUpdated>(j => j
    .On(m => m.ProductKey))  // Join on composite key property
```

## Event definitions

```csharp
[EventType]
public record OrderCreated(
    string CustomerId,
    string OrderNumber,
    string CustomerName,
    DateTimeOffset OrderDate);

[EventType]
public record OrderShipped(
    string CustomerId,
    string OrderNumber,
    DateTimeOffset ShippedDate);

[EventType]
public record UserAction(
    string UserId,
    string ActionType,
    string Details);
```

## Key composition rules

1. **Consistent structure**: All events that target the same projection must use the same composite key structure
2. **Immutable parts**: Key components should not change during the lifetime of a projection instance
3. **Uniqueness**: The combination of all key parts must uniquely identify each projection instance
4. **Type safety**: Key components are strongly typed and validated at compile time

## Performance considerations

- **Index efficiency**: Composite keys create complex indexes in the underlying storage
- **Query patterns**: Consider how you'll query the data when designing key structure
- **Key size**: Larger composite keys use more storage and may impact performance
- **Sort order**: The order of properties in the composite key affects index efficiency

Composite keys provide powerful flexibility for complex identification scenarios while maintaining type safety and performance.
