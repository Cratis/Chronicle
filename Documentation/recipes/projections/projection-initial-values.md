# Projection with initial values

The `WithInitialValues()` method allows you to set default values for properties in your read models. This is particularly useful when you want to ensure properties have meaningful default values instead of being null or uninitialized.

## Basic initial values usage

Use `WithInitialValues()` to provide a factory function that creates a read model instance with default values:

```csharp
public class UserProfileProjection : IProjectionFor<UserProfile>
{
    public void Define(IProjectionBuilderFor<UserProfile> builder) => builder
        .WithInitialValues(() => new UserProfile(
            UserId: string.Empty,
            Name: "Unknown User",
            Email: string.Empty,
            Status: UserStatus.Inactive,
            CreatedAt: DateTimeOffset.MinValue,
            LastLogin: null,
            LoginCount: 0,
            IsVerified: false))
        .AutoMap()
        .From<UserCreated>(_ => _
            .Set(m => m.Status).ToValue(UserStatus.Active)
            .Set(m => m.CreatedAt).ToEventContextProperty(c => c.Occurred))
        .From<UserLoggedIn>(_ => _
            .Set(m => m.LastLogin).ToEventContextProperty(c => c.Occurred)
            .Increment(m => m.LoginCount))
        .From<UserEmailVerified>(_ => _
            .Set(m => m.IsVerified).ToValue(true));
}
```

## Why use initial values

Without initial values, properties that aren't set by events will be left as their default .NET values:

- Reference types: `null`
- Numbers: `0`
- Booleans: `false`
- DateTime: `DateTime.MinValue`

Initial values allow you to:

### Provide meaningful defaults

Instead of `null` or `0`, set business-appropriate defaults:

```csharp
.WithInitialValues(() => new OrderSummary(
    OrderId: string.Empty,
    Status: OrderStatus.Draft,
    TotalAmount: 0.0m,
    ItemCount: 0,
    CreatedDate: DateTimeOffset.MinValue,
    Notes: "No notes"))
```

### Avoid null reference exceptions

Ensure collections and strings are never null:

```csharp
.WithInitialValues(() => new CustomerRecord(
    CustomerId: string.Empty,
    Name: string.Empty,
    Addresses: new List<Address>(),
    Orders: new List<Order>(),
    Tags: new HashSet<string>(),
    Metadata: new Dictionary<string, string>()))
```

### Set computed or calculated defaults

Initialize properties with calculated values:

```csharp
.WithInitialValues(() => new ReportData(
    ReportId: string.Empty,
    GeneratedAt: DateTimeOffset.UtcNow,
    ExpiresAt: DateTimeOffset.UtcNow.AddDays(30),
    Version: "1.0",
    Status: ReportStatus.Pending))
```

## Initial values with concepts

When using Cratis concepts, provide appropriate default concept values:

```csharp
.WithInitialValues(() => new ProductCatalog(
    ProductId: ProductId.Empty,
    Name: ProductName.NotSet,
    Price: Price.Zero,
    Category: Category.Uncategorized,
    InStock: Quantity.Zero,
    IsActive: false))
```

## Working with complex types

For read models with nested objects or collections:

```csharp
.WithInitialValues(() => new OrderDetails(
    OrderId: string.Empty,
    Customer: new CustomerInfo(
        CustomerId: string.Empty,
        Name: "Guest Customer",
        Email: string.Empty),
    Items: new List<OrderItem>(),
    Shipping: new ShippingInfo(
        Address: "Not provided",
        Method: ShippingMethod.Standard,
        Cost: 0.0m),
    Payment: new PaymentInfo(
        Method: PaymentMethod.Unknown,
        Status: PaymentStatus.Pending,
        Amount: 0.0m)))
```

## Initial values with events that don't cover all properties

This is especially useful when events only update specific properties:

```csharp
public class InventoryProjection : IProjectionFor<InventoryItem>
{
    public void Define(IProjectionBuilderFor<InventoryItem> builder) => builder
        .WithInitialValues(() => new InventoryItem(
            ProductId: string.Empty,
            CurrentStock: 0,
            ReservedStock: 0,
            AvailableStock: 0,
            LastUpdated: DateTimeOffset.MinValue,
            MinimumLevel: 10,  // Business default
            MaximumLevel: 1000,  // Business default
            ReorderPoint: 20))  // Business default
        .AutoMap()
        .From<StockReceived>(_ => _
            .Add(m => m.CurrentStock).With(e => e.Quantity)
            .Set(m => m.LastUpdated).ToEventContextProperty(c => c.Occurred))
        .From<StockReserved>(_ => _
            .Add(m => m.ReservedStock).With(e => e.Quantity)
            .Set(m => m.LastUpdated).ToEventContextProperty(c => c.Occurred));
}
```

In this example, `MinimumLevel`, `MaximumLevel`, and `ReorderPoint` are set by initial values since no events modify them.

## Read model examples

```csharp
public record UserProfile(
    string UserId,
    string Name,
    string Email,
    UserStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLogin,
    int LoginCount,
    bool IsVerified);

public record OrderSummary(
    string OrderId,
    OrderStatus Status,
    decimal TotalAmount,
    int ItemCount,
    DateTimeOffset CreatedDate,
    string Notes);

public record InventoryItem(
    string ProductId,
    int CurrentStock,
    int ReservedStock,
    int AvailableStock,
    DateTimeOffset LastUpdated,
    int MinimumLevel,
    int MaximumLevel,
    int ReorderPoint);
```

## Event definitions

```csharp
[EventType]
public record UserCreated(string Name, string Email);

[EventType]
public record UserLoggedIn(string UserId);

[EventType]
public record UserEmailVerified(string UserId);

[EventType]
public record StockReceived(string ProductId, int Quantity);

[EventType]
public record StockReserved(string ProductId, int Quantity);
```

## Best practices

### Use factory functions

Always provide a factory function rather than a static instance:

```csharp
// ✅ Good - creates fresh instance each time
.WithInitialValues(() => new MyModel(...))

// ❌ Bad - would reuse same instance
.WithInitialValues(() => someStaticInstance)
```

### Set business-meaningful defaults

Choose defaults that make sense in your domain:

```csharp
// ✅ Good - meaningful business defaults
.WithInitialValues(() => new Account(
    Balance: 0.0m,
    Status: AccountStatus.PendingActivation,
    OpenedDate: DateTimeOffset.UtcNow))

// ❌ Less helpful - just .NET defaults
.WithInitialValues(() => new Account())
```

### Initialize collections

Always initialize collections to avoid null reference exceptions:

```csharp
.WithInitialValues(() => new ShoppingCart(
    Items: new List<CartItem>(),
    Coupons: new List<Coupon>(),
    Tags: new HashSet<string>()))
```

The `WithInitialValues()` method ensures your read models have consistent, meaningful default values, improving the reliability and usability of your projection data.
