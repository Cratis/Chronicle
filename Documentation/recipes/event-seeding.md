# Event Seeding

Event seeding allows you to populate your event store with predefined data. This can be useful for production scenarios were you want to
ship your solution with events predefined that needs to be there for the system to work.

For local development this can be particularly useful, as you can quickly restore a known state after clearing your event store.

## Basic Usage

Chronicle provides a fluent API for seeding events through declarative classes that implement `ICanSeedEvents`. These are automatically discovered and invoked when your application starts.

### Declarative Seeding with ICanSeedEvents

Implement the `ICanSeedEvents` interface to define your seed data. Chronicle will automatically discover and invoke these seeders on startup:

```csharp
public class UserSeeding : ICanSeedEvents
{
    public void Seed(IEventSeedingBuilder builder)
    {
        builder
            .For<UserRegistered>("user-123", [
                new("john@example.com", "John")
            ])
            .ForEventSource("user-456", [
                new UserRegistered("jane@example.com", "Jane"),
                new EmailVerified("jane@example.com")
            ]);
    }
}
```

### Seeding Events by Type

When you want to seed multiple events of the same type for a specific event source:

```csharp
builder.For<UserRegistered>("user-123", [
    new("john@example.com", "John"),
    new("jane@example.com", "Jane")
]);
```

### Seeding Mixed Event Types

When you want to seed different event types for the same event source:

```csharp
builder.ForEventSource("user-123", [
    new UserRegistered("john@example.com", "John"),
    new EmailVerified("john@example.com"),
    new ProfileUpdated("John Doe")
]);
```

## Development-Only Seeding

If you have seeding data that should only be used during development.
You can use conditional compilation to ensure seeding code doesn't end up in production builds:

```csharp
#if DEBUG
public class DevelopmentSeeding : ICanSeedEvents
{
    public void Seed(IEventSeedingBuilder builder)
    {
        builder
            .For<UserRegistered>("dev-user-1", [
                new("dev@example.com", "Dev User")
            ]);
    }
}
#endif
```

> Note: Cratis Chronicle does not distinguish between development or production purpose events
> you need to be conscious about this yourself. You can of course chose to leverage other mechanisms
> than the conditional compilation by injecting things you have accessible at runtime to decide,
> or an environment variable.

### Organizing Events

For larger applications, organize your development seed events by feature or domain:

```csharp
public class UserDevelopmentSeeding : ICanSeedEvents
{
    public void Seed(IEventSeedingBuilder builder)
    {
        // Seed test users
        builder.For<UserRegistered>("test-user-1", [
            new("test1@example.com", "Test User 1")
        ]);
    }
}

public class OrderDevelopmentSeeding : ICanSeedEvents
{
    public void Seed(IEventSeedingBuilder builder)
    {
        // Seed test orders
        builder.For<OrderPlaced>("test-order-1", [
            new("test-user-1", 100.00m)
        ]);
    }
}
#endif
```

> Note: These can of course be in separate files in separate namespaces, they will be discovered and
> seeded individually.

## How It Works

1. **Automatic Discovery**: Classes implementing `ICanSeedEvents` are automatically discovered using the same mechanism as projections and reactors during application startup.

2. **Automatic Registration**: Discovered seeders are automatically invoked when the event store connects, and the seed data is sent to the server.

3. **Deduplication**: Chronicle tracks which events have been seeded and won't seed them again, making it safe to run seeding multiple times.

4. **Batch Operations**: All events are appended in a single batch operation for optimal performance.

5. **Per Namespace**: Seeding data is tracked per event store and namespace, so different namespaces can have different seed data.

## Best Practices

- **Use `#if DEBUG`**: Always wrap seeding code in conditional compilation directives to prevent it from running in production.
- **Keep it Simple**: Seed only the minimum data needed for development and testing.
- **Organize by Feature**: Group related seed data together in separate `ICanSeedEvents` implementations.
- **Use Meaningful IDs**: Use descriptive event source IDs (e.g., "test-user-admin") to make debugging easier.
- **Document Scenarios**: Add comments explaining what scenario each seed data represents.

## Example: Complete Seeding Setup

```csharp
namespace MyApp.Development;

/// <summary>
/// Seeds initial admin user for development.
/// </summary>
public class AdminUserSeeding : ICanSeedEvents
{
    public void Seed(IEventSeedingBuilder builder)
    {
        builder
            .For<UserRegistered>("admin-user", [
                new("admin@dev.local", "Admin User")
            ])
            .ForEventSource("admin-user", [
                new EmailVerified("admin@dev.local"),
                new RoleAssigned("Administrator")
            ]);
    }
}

/// <summary>
/// Seeds sample products for testing catalog features.
/// </summary>
public class ProductCatalogSeeding : ICanSeedEvents
{
    public void Seed(IEventSeedingBuilder builder)
    {
        builder
            .For<ProductCreated>("product-1", [
                new("Laptop", 999.99m)
            ])
            .For<ProductCreated>("product-2", [
                new("Mouse", 29.99m)
            ]);
    }
}
#endif
```

That's it! No additional code is needed in your application startup - the seeding happens automatically when the event store is registered and connected.
