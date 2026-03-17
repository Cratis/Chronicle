# Seeding with C Sharp

This page shows how to seed events using the Chronicle .NET client. Seeding is sent to the Chronicle Server when the event store connects, and the server applies it once per namespace.

## Define events

Use `record` types for event definitions in examples:

```csharp
public record UserRegistered(string Email, string DisplayName);
public record EmailVerified(string Email);
public record ProfileUpdated(string DisplayName);
public record OrderPlaced(string UserId, decimal Amount);
```

## Implement a seeder

Implement `ICanSeedEvents` and use `IEventSeedingBuilder` to define events to append:

```csharp
public sealed class UserSeeding : ICanSeedEvents
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

## Seed multiple events of the same type

```csharp
builder.For<UserRegistered>("user-123", [
    new("john@example.com", "John"),
    new("jane@example.com", "Jane")
]);
```

## Seed mixed event types

```csharp
builder.ForEventSource("user-123", [
    new UserRegistered("john@example.com", "John"),
    new EmailVerified("john@example.com"),
    new ProfileUpdated("John Doe")
]);
```

## Development-only seeding

If seed data should only run in development, use conditional compilation or runtime configuration:

```csharp
#if DEBUG
public sealed class DevelopmentSeeding : ICanSeedEvents
{
    public void Seed(IEventSeedingBuilder builder)
    {
        builder.For<UserRegistered>("dev-user-1", [
            new("dev@example.com", "Dev User")
        ]);
    }
}
#endif
```

Chronicle does not distinguish between development and production seed data. Decide when to seed based on build configuration or runtime settings.

## Organize seeders by feature

For larger solutions, split seeders by domain or feature:

```csharp
public sealed class UserDevelopmentSeeding : ICanSeedEvents
{
    public void Seed(IEventSeedingBuilder builder)
    {
        builder.For<UserRegistered>("test-user-1", [
            new("test1@example.com", "Test User 1")
        ]);
    }
}

public sealed class OrderDevelopmentSeeding : ICanSeedEvents
{
    public void Seed(IEventSeedingBuilder builder)
    {
        builder.For<OrderPlaced>("test-order-1", [
            new("test-user-1", 100.00m)
        ]);
    }
}
```

## Namespace-scoped seed data

By default, seed data applies to all namespaces in the event store. To target a specific namespace, use `ForNamespace` to get a scoped builder:

```csharp
public sealed class TenantSeeding : ICanSeedEvents
{
    public void Seed(IEventSeedingBuilder builder)
    {
        // Global seed data — applied to every namespace
        builder.For<ProductCreated>("product-1", [
            new("Laptop", 1299.00m)
        ]);

        // Namespace-scoped seed data — applied only to the "acme" namespace
        builder.ForNamespace("acme")
            .For<UserRegistered>("user-1", [
                new("admin@acme.com", "Acme Admin")
            ]);

        // A second namespace with different seed data
        builder.ForNamespace("contoso")
            .For<UserRegistered>("user-1", [
                new("admin@contoso.com", "Contoso Admin")
            ])
            .ForEventSource("org-1", [
                new OrganizationCreated("Contoso"),
                new BillingSetUp("contoso@billing.com")
            ]);
    }
}
```

The scoped builder supports the same `For<TEvent>` and `ForEventSource` methods as the global builder. Each namespace receives only its own scoped events in addition to any global events.

## How it runs

- Seeders are automatically discovered at application startup.
- Seed batches are sent to the Chronicle Server when the event store connects.
- The server deduplicates seeded events and applies them once per namespace.
- Events are appended in a single batch for efficient startup.

## Best practices

- Keep seed data minimal and deterministic.
- Use clear event source IDs to make debugging easier.
- Group seeders by scenario so you can remove or adjust them easily.
- Use build flags or runtime settings to prevent seeding in production.
- Use `ForNamespace` when seed data is tenant-specific or environment-specific to avoid polluting other namespaces.

