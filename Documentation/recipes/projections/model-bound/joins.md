# Joins

Model-bound projections support joining data from different events using the `Join` attribute. This allows you to enrich your read models with data from related events.

## Basic Join

The `Join` attribute maps properties from events that are related through a common key:

```csharp
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

public record OrderSummary(
    [Key, FromEventSourceId]
    Guid OrderId,
    
    [SetFrom<OrderPlaced>]
    decimal Amount,
    
    [Join<CustomerCreated>(
        on: nameof(CustomerId),
        eventPropertyName: nameof(CustomerCreated.Name))]
    string CustomerName);
```

### Parameters

- **on** (optional): Property on the read model to join on. For root projections, this is typically required unless joining within children
- **eventPropertyName** (optional): Property name on the event. If not specified, uses the read model property name

## Join in Children

Joins work within child collections. When used in children, the `on` parameter is optional if the child has an `identifiedBy` property:

```csharp
public record Order(
    [Key, FromEventSourceId]
    Guid OrderId,
    
    [ChildrenFrom<LineItemAdded>]
    IEnumerable<OrderLine> Lines);

public record OrderLine(
    [Key] Guid Id,
    
    [SetFrom<LineItemAdded>]
    int Quantity,
    
    [Join<ProductUpdated>(eventPropertyName: nameof(ProductUpdated.ProductName))]
    string ProductName,
    
    [Join<ProductUpdated>(eventPropertyName: nameof(ProductUpdated.CurrentPrice))]
    decimal Price);
```

## Multiple Joins

You can join with multiple different events:

```csharp
public record EnrichedOrder(
    [Key, FromEventSourceId]
    Guid OrderId,
    
    [Join<CustomerCreated>(on: nameof(CustomerId))]
    string CustomerName,
    
    [Join<CustomerUpdated>(on: nameof(CustomerId))]
    string CustomerEmail,
    
    [Join<ShippingAddressSet>(on: nameof(OrderId))]
    string ShippingAddress);
```

## Recursive Join Processing

Join attributes on related types are processed recursively:

```csharp
public record Order(
    [Key, FromEventSourceId]
    Guid OrderId,
    
    [ChildrenFrom<LineItemAdded>]
    IEnumerable<OrderLine> Lines);

public record OrderLine(
    [Key] Guid Id,
    
    [Join<ProductCatalogUpdated>(
        eventPropertyName: nameof(ProductCatalogUpdated.Name))]
    string ProductName,
    
    [Join<ProductCatalogUpdated>(
        eventPropertyName: nameof(ProductCatalogUpdated.Description))]
    string Description,
    
    [Join<PricingUpdated>(
        eventPropertyName: nameof(PricingUpdated.CurrentPrice))]
    decimal UnitPrice);
```

## Complete Example

Here's a comprehensive example showing joins at multiple levels:

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

// Events
[EventType]
public record OrderPlaced(Guid CustomerId, DateTimeOffset PlacedAt);

[EventType]
public record CustomerRegistered(string Name, string Email);

[EventType]
public record CustomerProfileUpdated(string PhoneNumber);

[EventType]
public record LineItemAdded(Guid ProductId, int Quantity);

[EventType]
public record ProductCreated(string Name, decimal Price);

[EventType]
public record ProductPriceChanged(decimal NewPrice);

// Read Models
public record OrderDetails(
    [Key, FromEventSourceId]
    Guid OrderId,
    
    [SetFrom<OrderPlaced>]
    DateTimeOffset PlacedAt,
    
    // Join customer information
    [Join<CustomerRegistered>(
        on: nameof(CustomerId),
        eventPropertyName: nameof(CustomerRegistered.Name))]
    string CustomerName,
    
    [Join<CustomerRegistered>(
        on: nameof(CustomerId),
        eventPropertyName: nameof(CustomerRegistered.Email))]
    string CustomerEmail,
    
    [Join<CustomerProfileUpdated>(
        on: nameof(CustomerId),
        eventPropertyName: nameof(CustomerProfileUpdated.PhoneNumber))]
    string CustomerPhone,
    
    [ChildrenFrom<LineItemAdded>(key: nameof(LineItemAdded.ProductId))]
    IEnumerable<LineItemDetails> Items);

public record LineItemDetails(
    [Key] Guid ProductId,
    
    [SetFrom<LineItemAdded>]
    int Quantity,
    
    // Join product information
    [Join<ProductCreated>(eventPropertyName: nameof(ProductCreated.Name))]
    string ProductName,
    
    [Join<ProductCreated>(eventPropertyName: nameof(ProductCreated.Price))]
    [Join<ProductPriceChanged>(eventPropertyName: nameof(ProductPriceChanged.NewPrice))]
    decimal Price);
```

## Event Processing Flow

1. **CustomerRegistered** - Customer data becomes available for joining
2. **ProductCreated** - Product data becomes available for joining
3. **OrderPlaced** - Order is created, joins pull in customer data
4. **LineItemAdded** - Line item is added, joins pull in product data
5. **ProductPriceChanged** - Updates price on all relevant line items through join
6. **CustomerProfileUpdated** - Updates phone number on all relevant orders through join

## Join vs SetFrom

**SetFrom:**
- Maps properties from events directly related to the entity
- Event is "about" the entity (same event source ID)
- Direct parent-child relationship

**Join:**
- Maps properties from events about related entities
- Event is about a different entity but shares a common key
- Used for enrichment and denormalization

## Best Practices

1. **Use meaningful join keys** - Ensure the `on` parameter clearly identifies the relationship
2. **Handle missing data** - Joins may not find matching data; consider nullable properties
3. **Be mindful of updates** - Joined data updates when the source event changes
4. **Avoid circular joins** - Don't create circular dependencies between projections
5. **Consider cardinality** - Joins work best for one-to-one and many-to-one relationships
